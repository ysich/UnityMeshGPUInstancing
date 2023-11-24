/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 11:06:53
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using Onemt.ResourceManagement.AsyncOperation;
using Onemt.ResourceManagement.ResourceLocations;

namespace Onemt.ResourceManagement.ResourceProviders
{
    public abstract class ResourceProviderBase : IResourceProvider
    {
        public virtual void Provide(ProvideHandle provideHandle){}
        public virtual void SetDownloadProgressCallbacks(Func<DownloadStatus> callback){}
        public virtual void Release(IResourceLocation location, object obj) { }
    }
}
