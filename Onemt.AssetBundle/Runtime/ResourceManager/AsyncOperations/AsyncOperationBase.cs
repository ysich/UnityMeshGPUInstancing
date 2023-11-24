/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 11:49:35
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using Onemt.ResourceManagement.Util;
using Object = UnityEngine.Object;

namespace Onemt.ResourceManagement.AsyncOperation
{
    internal interface ICachable
    {
        IOperationCacheKey key { get; set; }
    }

    public interface IAsyncOperation
    {
        AsyncOperationHandle handle { get; }

        int referenceCount { get; }

        float percentComplete { get; }

        AsyncOperationStatus status { get; }

        bool isDone { get; }

        bool isRunning { get; }

        string debugName { get; }

        Action<IAsyncOperation> onDestroy { set; }

        void Start(ResourceManager rm, AsyncOperationHandle dependency);

        void DecrementReferenceCount();

        void IncrementReferenceCount();

        void InvokeCompletionEvent();

        object GetResultAsObject();

        void GetDependencies(List<AsyncOperationHandle> deps);

        DownloadStatus GetDownloadStatus(HashSet<object> visited);

        event Action<AsyncOperationHandle> completedCallback;

        event Action<AsyncOperationHandle> destroyCallback;
    }

    public abstract class AsyncOperationBase<TObject> : IAsyncOperation
    {
        AsyncOperationStatus m_Status;

        //Exception m_Error;

        Action<AsyncOperationHandle> m_DependencyCompleteCallback;

        DelegateList<AsyncOperationHandle> m_DestroyedAction;
        DelegateList<AsyncOperationHandle> m_CompletedAction;

        internal event Action<AsyncOperationHandle> completedCallback
        {
            add
            {
                if (m_CompletedAction == null)
                    m_CompletedAction = DelegateList<AsyncOperationHandle>.CreateWithGlobalCache();

                m_CompletedAction.Add(value);

                if (isDone)
                    InvokeCompletionEvent();
            }
            remove
            {
                m_CompletedAction?.Remove(value);
            }
        }

        internal event Action<AsyncOperationHandle> destroyCallback
        {
            add
            {
                if (m_DestroyedAction == null)
                    m_DestroyedAction = DelegateList<AsyncOperationHandle>.CreateWithGlobalCache();

                m_DestroyedAction.Add(value);
            }
            remove
            {
                m_DestroyedAction?.Remove(value);
            }
        }

        event Action<AsyncOperationHandle> IAsyncOperation.completedCallback
        {
            add { completedCallback += value; }
            remove { completedCallback -= value; }
        }

        event Action<AsyncOperationHandle> IAsyncOperation.destroyCallback
        {
            add { destroyCallback += value; }
            remove { destroyCallback -= value; }
        }

        protected internal bool m_HasExecuted = false;

        int m_ReferenceCount = 1;
        internal int referenceCount { get => m_ReferenceCount; }

        protected virtual float progress { get => 0.0f; }

        protected virtual string debugName { get => this.ToString(); }

        public TObject result { get; set; }

        private bool m_IsRunning;
        public bool isRunning { get => m_IsRunning; }

        internal AsyncOperationStatus status { get => m_Status; }

        internal bool isDone { get => status == AsyncOperationStatus.Failed || status == AsyncOperationStatus.Succeeded; }

        internal bool MoveNext() { return !isDone; }

        private Action<IAsyncOperation> m_OnDestroyAction;
        internal Action<IAsyncOperation> onDestroy { set => m_OnDestroyAction = value; }

        internal object Current => null;

        internal ResourceManager m_ResourceManager;
        public ResourceManager resourceManager => m_ResourceManager;

        internal float percentComplete
        {
            get
            {
                if (m_Status == AsyncOperationStatus.None)
                {
                    try
                    {
                        return progress;
                    }
                    catch
                    {
                        return 0.0f;
                    }
                }

                return 1.0f;
            }
        }

        internal AsyncOperationHandle<TObject> handle { get => new AsyncOperationHandle<TObject>(this); }

        float IAsyncOperation.percentComplete => percentComplete;
        AsyncOperationStatus IAsyncOperation.status => status;
        bool IAsyncOperation.isDone => isDone;

        string IAsyncOperation.debugName => debugName;

        int IAsyncOperation.referenceCount => referenceCount;

        AsyncOperationHandle IAsyncOperation.handle => handle;

        object IAsyncOperation.GetResultAsObject() => result;

        protected abstract void Execute();

        protected virtual void Destroy() { }

        protected virtual void GetDepencies(List<AsyncOperationHandle> dependencies) { }

        public AsyncOperationBase()
        {
            m_DependencyCompleteCallback = o => InvokeExecute();
        }

        internal void Start(ResourceManager rm, AsyncOperationHandle dependency)
        {
            m_ResourceManager = rm;
            m_IsRunning = true;
            m_HasExecuted = false;

            IncrementReferenceCount();
            if (dependency.IsValid() && !dependency.isDone)
                dependency.completedCallback += m_DependencyCompleteCallback;
            else
                InvokeExecute();
        }

        internal void InvokeExecute()
        {
            Execute();
            m_HasExecuted = true;
        }

        internal void IncrementReferenceCount()
        {
            if (m_ReferenceCount == 0)
                throw new Exception("");

            ++m_ReferenceCount;
            // 派发事件
        }

        internal void DecrementReferenceCount()
        {
            if (m_ReferenceCount < 1)
                throw new Exception("");

            --m_ReferenceCount;
            // 派发事件

            if (m_ReferenceCount == 0)
            {
                if (m_OnDestroyAction != null)
                {
                    m_OnDestroyAction(this);
                    m_OnDestroyAction = null;
                }

                Destroy();

                result = default(TObject);
                m_ReferenceCount = 1;
                m_Status = AsyncOperationStatus.None;
                //m_Error = null;

                m_DestroyedAction?.Invoke(handle);
            }
        }

        public void Complete(TObject result, bool success, string errorMsg)
        {
            Complete(result, success, errorMsg, true);
        }

        public void Complete(TObject result, bool success, string errorMsg, bool releaseDependenciesOnFailure)
        {
            Complete(result, success, new Exception(errorMsg), releaseDependenciesOnFailure);
        }

        public void Complete(TObject result, bool success, Exception exception, bool releaseDependenciesOnFailure = true)
        {
            if (isDone)
                return;

            this.result = result;
            m_Status = success ? AsyncOperationStatus.Succeeded : AsyncOperationStatus.Failed;

            if (m_Status == AsyncOperationStatus.Failed)
            {
                if (releaseDependenciesOnFailure)
                    ReleaseDependencies();
            }
            else
            {
                // TODO zm.
                // 是否在这判断 =>  如果此时operation 已经被销毁是否还进行回调，或者回调报错。
                InvokeCompletionEvent();
                DecrementReferenceCount();
            }

            m_IsRunning = false;
        }

        internal void InvokeCompletionEvent()
        {
            if (m_CompletedAction != null)
            {
                m_CompletedAction.Invoke(new AsyncOperationHandle<TObject>(this));
                m_CompletedAction.Clear();
            }
        }

        internal virtual void ReleaseDependencies() { }

        protected virtual void GetDependencies(List<AsyncOperationHandle> dependencies) { }

        DownloadStatus IAsyncOperation.GetDownloadStatus(HashSet<object> visited) => GetDownloadStatus(visited);

        internal virtual DownloadStatus GetDownloadStatus(HashSet<object> visited)
        {
            visited.Add(this);
            return new DownloadStatus() { isDone = isDone };
        }

        public override string ToString()
        {
            var instId = "";
            var or = result as Object;
            if (or != null)
                instId = "(" + or.GetInstanceID() + ")";
            return string.Format("{0}, result='{1}', status='{2}'", base.ToString(), (or + instId), m_Status);
        }

        void IAsyncOperation.GetDependencies(List<AsyncOperationHandle> deps) => GetDependencies(deps);

        /// <inheritdoc/>
        void IAsyncOperation.DecrementReferenceCount() => DecrementReferenceCount();

        /// <inheritdoc/>
        void IAsyncOperation.IncrementReferenceCount() => IncrementReferenceCount();

        /// <inheritdoc/>
        void IAsyncOperation.InvokeCompletionEvent() => InvokeCompletionEvent();

        /// <inheritdoc/>
        void IAsyncOperation.Start(ResourceManager rm, AsyncOperationHandle dependency) => Start(rm, dependency);

        Action<IAsyncOperation> IAsyncOperation.onDestroy { set { onDestroy = value; } }
    }
}
