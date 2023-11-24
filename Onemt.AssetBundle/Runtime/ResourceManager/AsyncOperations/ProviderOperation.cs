/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 11:51:15
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using UnityEngine;
using Onemt.ResourceManagement.ResourceProviders;
using Onemt.ResourceManagement.ResourceLocations;
using Onemt.ResourceManagement.Util;

namespace Onemt.ResourceManagement.AsyncOperation
{
    public interface IGenericProviderOperation
    {
        void Init(ResourceManager rm, IResourceProvider provider, IResourceLocation location, AsyncOperationHandle<IList<AsyncOperationHandle>> depOp);
        void Init(ResourceManager rm, IResourceProvider provider, IResourceLocation location, AsyncOperationHandle<IList<AsyncOperationHandle>> depOp, bool releaseDependenciesOnFailure);
        IResourceLocation location { get; }
        int dependencyCount { get; }
        Type requestedType { get; }
        void GetDependencies(IList<object> dstList);
        void ProviderCompleted<T>(T result, bool status, Exception e);
    }

    internal class ProviderOperation<TObject> : AsyncOperationBase<TObject>, IGenericProviderOperation, ICachable
    {
        private IResourceLocation m_Location;
        public IResourceLocation location { get => m_Location; }
        private IResourceProvider m_Provider;
        public int dependencyCount { get => (!m_DepOp.IsValid() || m_DepOp.result == null) ? 0 : m_DepOp.result.Count; }
        internal AsyncOperationHandle<IList<AsyncOperationHandle>> m_DepOp;
        private bool m_ReleaseDependenciesOnFailure;
        private Func<DownloadStatus> m_GetDownloadProgressCallback;
        private Func<float> m_GetProgressCallback;
        private DownloadStatus m_DownloadStatus;
        private const float k_OperationWaitingToCompletePercentComplete = 0.99f;
        private bool m_NeedRelease;
        //private ResourceManager m_ResourceManager;
        public IOperationCacheKey key { get; set; }
        public Type requestedType { get => typeof(TObject); }
        protected override float progress
        {
            get
            {
                try
                {
                    float numOfOps = 1f;
                    float total = 0f;
                    if (m_GetProgressCallback != null)
                        total += m_GetProgressCallback();

                    if (!m_DepOp.IsValid() || m_DepOp.result == null || m_DepOp.result.Count == 0)
                    {
                        ++numOfOps;
                        ++total;
                    }
                    else
                    {
                        foreach (var handle in m_DepOp.result)
                        {
                            total += handle.percentComplete;
                            ++numOfOps;
                        }
                    }

                    float percent = total / numOfOps;
                    return Mathf.Min(k_OperationWaitingToCompletePercentComplete, percent);
                }
                catch
                {
                    return 0.0f;
                }
            }
        }

        [UnityEngine.Scripting.Preserve]
        public ProviderOperation() { }

        public void Init(ResourceManager rm, IResourceProvider provider, IResourceLocation location, AsyncOperationHandle<IList<AsyncOperationHandle>> depOp)
        {
            Init(rm, provider, location, depOp, true);
        }

        public void Init(ResourceManager rm, IResourceProvider provider, IResourceLocation location, AsyncOperationHandle<IList<AsyncOperationHandle>> depOp, bool releaseDependenciesOnFailure)
        {
            m_ResourceManager = rm;
            m_Provider = provider;
            m_Location = location;
            m_DepOp = depOp;
            if (m_DepOp.IsValid())
                m_DepOp.Acquire();
            m_ReleaseDependenciesOnFailure = releaseDependenciesOnFailure;
        }

        protected override void Execute()
        {
            Debug.Assert(m_DepOp.isDone);

            if (m_DepOp.IsValid() && m_DepOp.status == AsyncOperationStatus.Failed)
            {
                ProviderCompleted(default(TObject), false, new Exception("Dependency Exception"));
            }
            else
            {
                try
                {
                    m_Provider.Provide(new ProvideHandle(m_ResourceManager, this));
                }
                catch (Exception e)
                {
                    ProviderCompleted(default(TObject), false, e);
                }
            }
        }

        public void SetGetDownloadProgressCallback(Func<DownloadStatus> getDownloadProgressCallback)
        {
            m_GetDownloadProgressCallback = getDownloadProgressCallback;
        }

        public void SetGetProgressCallback(Func<float> getProgressCallback)
        {
            m_GetProgressCallback = getProgressCallback;
        }

        internal override DownloadStatus GetDownloadStatus(HashSet<object> visited)
        {
            var depDownloadStatus = m_DepOp.IsValid() ? m_DepOp.InternalGetDownloadStatus(visited) : default;

            if (m_GetDownloadProgressCallback != null)
                m_DownloadStatus = m_GetDownloadProgressCallback.Invoke();

            if (status == AsyncOperationStatus.Succeeded)
                m_DownloadStatus.downloadedBytes = m_DownloadStatus.totalBytes;

            return new DownloadStatus()
            {
                totalBytes = depDownloadStatus.totalBytes + m_DownloadStatus.totalBytes,
                downloadedBytes = depDownloadStatus.downloadedBytes + m_DownloadStatus.downloadedBytes,
                isDone = this.isDone
            };
        }

        public void GetDependencies(IList<AsyncOperationHandle> deps)
        {
            if (m_DepOp.IsValid())
                deps.Add(m_DepOp);
        }

        public void GetDependencies(IList<object> dstList)
        {
            dstList.Clear();

            if (!m_DepOp.IsValid())
                return;

            if (m_DepOp.result == null)
                return;

            for (int i = 0; i < m_DepOp.result.Count; ++i)
                dstList.Add(m_DepOp.result[i].result);
        }

        internal override void ReleaseDependencies()
        {
            if (m_DepOp.IsValid())
                m_DepOp.Release();
        }

        public void ProviderCompleted<T>(T result, bool status, Exception e)
        {
            m_GetDownloadProgressCallback = null;
            m_GetProgressCallback = null;
            m_NeedRelease = status;

            ProviderOperation<T> top = this as ProviderOperation<T>;
            if (top != null)
                top.result = result;
            else if (result != null && typeof(TObject).IsAssignableFrom(result.GetType()))
                this.result = (TObject)(object)result;
            else
            {
                string errorMsg = string.Format("Provider of type {0}  has provided a result of type {1} which cannot be converted to requested type {2}. The operation will be marked as failed.", m_Provider.GetType().ToString(), typeof(T), typeof(TObject));
                Complete(this.result, false, errorMsg);
                throw new Exception(errorMsg);
            }

            Complete(this.result, status, e, m_ReleaseDependenciesOnFailure);
        }

        protected override void Destroy()
        {
            if (m_NeedRelease)
                m_Provider.Release(m_Location, result);

            if (m_DepOp.IsValid())
                m_DepOp.Release();

            result = default(TObject);
        }

        public AssetBundle GetAssetBundle()
        {
            if (result is AssetBundleResource abRes)
                return abRes.GetAssetBundle();

            return null;
        }
    }
}
