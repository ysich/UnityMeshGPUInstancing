/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 11:50:06
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Onemt.ResourceManagement.AsyncOperation
{
    public struct AsyncOperationHandle<TObject> : IEnumerator
    {
        internal AsyncOperationBase<TObject> m_InternalOp;
        public AsyncOperationBase<TObject> internalOp => m_InternalOp;

        string m_Path;
        internal string path => m_Path;

        public bool isDone => !IsValid() || m_InternalOp.isDone;

        public float percentComplete => m_InternalOp.percentComplete;

        public int referenceCount => m_InternalOp.referenceCount;

        public TObject result => m_InternalOp.result;

        public AsyncOperationStatus status => m_InternalOp.status;

        object IEnumerator.Current => result;

        bool IEnumerator.MoveNext() => !isDone;

        void IEnumerator.Reset() { }

        public event Action<AsyncOperationHandle> completedCallback
        {
            add { m_InternalOp.completedCallback += value; }
            remove { m_InternalOp.completedCallback -= value; }
        }

        public event Action<AsyncOperationHandle> destroyedCallback
        {
            add { m_InternalOp.destroyCallback += value; }
            remove { m_InternalOp.destroyCallback -= value; }
        }

        public string debugName { get => ((IAsyncOperation)m_InternalOp).debugName; }

        static public implicit operator AsyncOperationHandle(AsyncOperationHandle<TObject> obj)
        {
            return new AsyncOperationHandle(obj.m_InternalOp, obj.path);
        }

        internal AsyncOperationHandle(AsyncOperationBase<TObject> op)
        {
            m_InternalOp = op;
            m_Path = null;
        }

        internal AsyncOperationHandle(IAsyncOperation op)
        {
            m_InternalOp = (AsyncOperationBase<TObject>)op;
            m_Path = null;
        }

        internal AsyncOperationHandle(AsyncOperationBase<TObject> op, string path)
        {
            m_InternalOp = op;
            m_Path = path;
        }

        internal AsyncOperationHandle<TObject> Acquire()
        {
            m_InternalOp.IncrementReferenceCount();
            return this;
        }

        public DownloadStatus GetDownloadStatus()
        {
            return InternalGetDownloadStatus(new HashSet<object>());
        }

        internal DownloadStatus InternalGetDownloadStatus(HashSet<object> visited)
        {
            if (visited == null)
                visited = new HashSet<object>();

            return visited.Add(m_InternalOp) ? m_InternalOp.GetDownloadStatus(visited) : new DownloadStatus() { isDone = isDone };
        }

        internal void Release()
        {
            m_InternalOp.DecrementReferenceCount();
            m_InternalOp = null;
        }

        public bool Equals(AsyncOperationHandle<TObject> other)
        {
            return m_InternalOp == other.m_InternalOp;
        }

        public override int GetHashCode()
        {
            return m_InternalOp.GetHashCode() * 17;
        }

        public bool IsValid()
        {
            return m_InternalOp != null;
        }

        public AssetBundle GetAssetBundle()
        {
            if (m_InternalOp is ProviderOperation<TObject> providerOp)
                return providerOp.GetAssetBundle();

            return null;
        }
    }

    public struct AsyncOperationHandle : IEnumerator
    {
        internal IAsyncOperation m_InternalOp;
        IAsyncOperation internalOp
        {
            get
            {
                if (m_InternalOp == null)
                    throw new Exception("Attempting to use an invalid operation handle.");

                return m_InternalOp;
            }
        }

        internal int referenceCount { get => m_InternalOp.referenceCount; }

        [NoToLua]
        public string debugName { get => m_InternalOp.debugName; }

        [NoToLua]
        public object result { get => m_InternalOp.GetResultAsObject(); }

        [NoToLua]
        public AsyncOperationStatus status { get => m_InternalOp.status; }

        object IEnumerator.Current { get => result; }

        [NoToLua]
        public bool isDone { get => m_InternalOp.isDone; }

        string m_Path;
        internal string path { get => m_Path; }

        [NoToLua]
        public float percentComplete { get => m_InternalOp.percentComplete; }

        bool IEnumerator.MoveNext()
        {
            return !isDone;
        }

        void IEnumerator.Reset() { }

        [NoToLua]
        public event Action<AsyncOperationHandle> completedCallback
        {
            add { m_InternalOp.completedCallback += value; }
            remove { m_InternalOp.completedCallback -= value; }
        }
        [NoToLua]
        public event Action<AsyncOperationHandle> destroyCallback
        {
            add { m_InternalOp.destroyCallback += value; }
            remove { m_InternalOp.destroyCallback -= value; }
        }

        internal AsyncOperationHandle(IAsyncOperation asyncOperation)
        {
            m_InternalOp = asyncOperation;
            m_Path = null;
        }

        internal AsyncOperationHandle(IAsyncOperation asyncOperation, string path)
        {
            m_InternalOp = asyncOperation;
            m_Path = path;
        }

        [NoToLua]
        public AsyncOperationHandle<T> Convert<T>()
        {
            return new AsyncOperationHandle<T>(m_InternalOp);
        }

        internal AsyncOperationHandle Acquire()
        {
            m_InternalOp.IncrementReferenceCount();
            return this;
        }

        internal void Release()
        {
            m_InternalOp.DecrementReferenceCount();
            m_InternalOp = null;
        }

        [NoToLua]
        public void GetDependencies(List<AsyncOperationHandle> deps)
        {
            m_InternalOp.GetDependencies(deps);
        }

        [NoToLua]
        public DownloadStatus GetDownloadStatus()
        {
            return InternalGetDownloadStatus(new HashSet<object>());
        }

        internal DownloadStatus InternalGetDownloadStatus(HashSet<object> visited)
        {
            if (visited == null)
                visited = new HashSet<object>();

            return visited.Add(internalOp) ? internalOp.GetDownloadStatus(visited) : new DownloadStatus() { isDone = true };
        }

        [NoToLua]
        public override int GetHashCode()
        {
            return m_InternalOp == null ? 0 : m_InternalOp.GetHashCode() * 17;
        }

        [NoToLua]
        public bool IsValid()
        {
            return m_InternalOp != null;
        }
    }
}
