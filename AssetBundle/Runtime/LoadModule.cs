///*---------------------------------------------------------------------------------------
//-- 负责人: ming.zhang
//-- 创建时间: 2023-03-30 15:19:39
//-- 概述:
//---------------------------------------------------------------------------------------*/

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Onemt.ResourceManagement.AsyncOperation;
//using Onemt.ResourceManagement.Util;
//using System;

//namespace Onemt.ResourceManagement
//{
//    public class LoadModule
//    {
//        private IAllocationStrategy m_Alloctor;
//        internal IAllocationStrategy allocator { get => m_Alloctor; }

//        private Action<IAsyncOperation> m_ReleaseOpNonCached;
//        private Action<IAsyncOperation> m_ReleaseOpCached;
//        private Action<IAsyncOperation> m_ReleaseInstanceOp;

//        private Dictionary<string, IAsyncOperation> m_CacheAsyncOperations = new Dictionary<string, IAsyncOperation>();

//        public LoadModule(IAllocationStrategy alloctor)
//        {
//            m_Alloctor = alloctor;

//            m_ReleaseOpNonCached = OnOperationDestroyNonCached;
//            m_ReleaseOpCached = OnOperationDestroyCached;
//            m_ReleaseInstanceOp = OnInstanceOperationDestroy;

//            m_UpdateReceivers.OnElementAdded += x => RegisterForCallbacks();
//        }

//        public AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(string assetName)
//        {
//            return ProvideAsset(assetName).Convert<TObject>();
//        }

//        private AsyncOperationHandle ProvideAsset(string assetName)
//        {
//            if (m_CacheAsyncOperations.TryGetValue(assetName, out var op))
//            {
//                op.IncrementReferenceCount();
//                return new AsyncOperationHandle(op);
//            }
//        }

//        private string GetAssetBundleName(string assetName)
//        {
//            return "";
//        }

//        private List<string> GetDependencyAssetBundleNames(string assetbundleName)
//        {
//            return default;
//        }

//        internal void OnOperationDestroyCached(IAsyncOperation asyncOp)
//        {
//            allocator.Release(asyncOp.GetType().GetHashCode(), asyncOp);
//            if ((asyncOp is ICachable cache) && cache.key != null)
//                RemoveOpearationFromCache(cache.key);
//        }

//        void OnInstanceOperationDestroy(IAsyncOperation o)
//        {
//            m_TrackedInstanceOperations.Remove(o as InstanceOperation);
//            allocator.Release(o.GetType().GetHashCode(), o);
//        }

//        void OnOperationDestroyNonCached(IAsyncOperation o)
//        {
//            allocator.Release(o.GetType().GetHashCode(), o);
//        }

//        internal void RemoveOpearationFromCache(IOperationCacheKey cacheKey)
//        {
//            m_CacheAsyncOperations.Remove(cacheKey);
//        }
//    }
//}
