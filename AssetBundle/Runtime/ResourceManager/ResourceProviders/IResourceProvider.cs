/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 11:06:53
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using Onemt.ResourceManagement.AsyncOperation;
using Onemt.ResourceManagement.ResourceLocations;
using System.Collections.Generic;

namespace Onemt.ResourceManagement.ResourceProviders
{
    public interface IResourceProvider
    {
        void Provide(ProvideHandle provideHandle);
        void SetDownloadProgressCallbacks(Func<DownloadStatus> callback);
        void Release(IResourceLocation location, object obj);
    }

    public struct ProvideHandle
    {
        private IGenericProviderOperation m_AsyncOperation;
        public IGenericProviderOperation asyncOperation { get => m_AsyncOperation; }
        private ResourceManager m_ResourceManager;
        public ResourceManager resourceManager { get => m_ResourceManager; }

        public IResourceLocation location { get => asyncOperation.location; }

        public Type requestedType { get => m_AsyncOperation.requestedType; }

        //public string assetName { get; set; }

        public ProvideHandle(ResourceManager resourceManager, IGenericProviderOperation op)
        {
            m_ResourceManager = resourceManager;
            m_AsyncOperation = op;
        }

        public void GetDependencies(IList<object> list)
        {
            m_AsyncOperation.GetDependencies(list);
        }

        public void Complete<T>(T result, bool status, Exception exception)
        {
            asyncOperation.ProviderCompleted<T>(result, status, exception);
        }
    }
}
