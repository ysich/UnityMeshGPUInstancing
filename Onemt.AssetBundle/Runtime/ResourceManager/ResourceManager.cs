/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 14:00:45
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Onemt.ResourceManagement.Util;
using Onemt.ResourceManagement.AsyncOperation;
using Onemt.ResourceManagement.ResourceLocations;
using Onemt.ResourceManagement.ResourceProviders;
using Onemt.AddressableAssets;
using System.IO;
using Onemt.Framework.Config;

namespace Onemt.ResourceManagement
{
    public class ResourceManager : IDisposable
    {
        private IAllocationStrategy m_Alloctor;
        internal IAllocationStrategy allocator { get => m_Alloctor; }

        /// <summary>
        ///   <para> 已加载的 AsyncOperation </para>
        ///   <para> key : AssetBundle Name </para>
        /// </summary>
        private Dictionary<IOperationCacheKey, IAsyncOperation> m_CacheAsyncOperations = new Dictionary<IOperationCacheKey, IAsyncOperation>();
        private HashSet<InstanceOperation> m_TrackedInstanceOperations = new HashSet<InstanceOperation>();
        internal int instanceOperationCount { get { return m_TrackedInstanceOperations.Count; } }

        private Action<IAsyncOperation> m_ReleaseOpNonCached;
        private Action<IAsyncOperation> m_ReleaseOpCached;
        private Action<IAsyncOperation> m_ReleaseInstanceOp;

        ListWithEvents<IUpdateReceiver> m_UpdateReceivers = new ListWithEvents<IUpdateReceiver>();
        bool m_UpdatingReceivers = false;
        internal bool CallbackHooksEnabled = true;
        bool m_RegisteredForCallbacks = false;
        List<IUpdateReceiver> m_UpdateReceiversToRemove = null;

        public ResourceManager(IAllocationStrategy alloctor)
        {
            m_Alloctor = alloctor;

            m_ReleaseOpNonCached = OnOperationDestroyNonCached;
            m_ReleaseOpCached = OnOperationDestroyCached;
            m_ReleaseInstanceOp = OnInstanceOperationDestroy;

            m_UpdateReceivers.OnElementAdded += x => RegisterForCallbacks();
        }

        internal void Update(float unscaledDeltaTime)
        {
            m_UpdatingReceivers = true;
            for (int i = 0; i < m_UpdateReceivers.Count; i++)
                m_UpdateReceivers[i].Update(unscaledDeltaTime);
            m_UpdatingReceivers = false;

            if (m_UpdateReceiversToRemove != null)
            {
                foreach (var r in m_UpdateReceiversToRemove)
                    m_UpdateReceivers.Remove(r);
                m_UpdateReceiversToRemove = null;
            }
        }

        internal void RegisterForCallbacks()
        {
            if (CallbackHooksEnabled && !m_RegisteredForCallbacks)
            {
                m_RegisteredForCallbacks = true;
                MonoBehaviourCallbackHooks.Instance.OnUpdateDelegate += Update;
            }
        }

        public void AddUpdateReceiver(IUpdateReceiver receiver)
        {
            if (receiver == null)
                return;
            m_UpdateReceivers.Add(receiver);
        }

        public void RemoveUpdateReciever(IUpdateReceiver receiver)
        {
            if (receiver == null)
                return;

            if (m_UpdatingReceivers)
            {
                if (m_UpdateReceiversToRemove == null)
                    m_UpdateReceiversToRemove = new List<IUpdateReceiver>();
                m_UpdateReceiversToRemove.Add(receiver);
            }
            else
            {
                m_UpdateReceivers.Remove(receiver);
            }
        }

        public AsyncOperationHandle StartOperation(IAsyncOperation operation, AsyncOperationHandle dependency)
        {
            operation.Start(this, dependency);

            return operation.handle;
        }

        public AsyncOperationHandle<TObject> StartOperation<TObject>(AsyncOperationBase<TObject> operation, AsyncOperationHandle dependency)
        {
            operation.Start(this, dependency);

            return operation.handle;
        }

        public AsyncOperationHandle<TObject> ProvideResource<TObject>(IResourceLocation location, ResourceProviderBase provider, bool async)
        {
            AsyncOperationHandle handle = ProvideResource(typeof(ProviderOperation<TObject>), location, provider, async);
            return handle.Convert<TObject>();
        }

        /// <summary>
        ///   <para> provide opeHandle. </para>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="location"></param>
        /// <param name="releaseDependenciesOnFailure"></param>
        /// <returns></returns>
        private AsyncOperationHandle ProvideResource(Type type, IResourceLocation location, ResourceProviderBase provider, bool async, bool releaseDependenciesOnFailure = true)
        {
            if (location == null)
                throw new ArgumentException("location");

            //return ProvideResourceRuntime(type, location, provider);

            if (GameConfig.instance.enableAssetBundle)
                return ProvideResourceRuntime(type, location, provider, async);
            else
            {
#if UNITY_EDITOR
                return ProvideResourceEditor(type, location);
#else
                return ProvideResourceRuntime(type, location, provider, async);
#endif // UNITY_EDITOR
            }
        }

#if UNITY_EDITOR
            /// <summary>
            ///   <para> 编辑器模式下  AssetDatabase 加载资源. </para>
            /// </summary>
            /// <param name="location"></param>
            /// <param name="desiredType"></param>
            /// <param name="releaseDependenciesOnFailure"></param>
            /// <typeparam name="TObject"></typeparam>
            /// <returns></returns>

        private AsyncOperationHandle ProvideResourceEditor(Type type, IResourceLocation location, Type desiredType = null, bool releaseDependenciesOnFailure = true)
        {
            var cacheKey = new LocationCacheKey(location, typeof(AssetDatabaseProvider));
            if (m_CacheAsyncOperations.TryGetValue(cacheKey, out var op))
            {
                op.IncrementReferenceCount();
                return new AsyncOperationHandle(op, location.assetbundleName);
            }

            IResourceProvider resProvider = new AssetDatabaseProvider();
            op = CreateOperation<IAsyncOperation>(type, m_ReleaseOpCached, cacheKey);
            var depOp = default(AsyncOperationHandle<IList<AsyncOperationHandle>>);
            ((IGenericProviderOperation)op).Init(this, resProvider, location, depOp, false);

            var handle = StartOperation(op, depOp);
            if (depOp.IsValid())
                depOp.Release();

            return handle;
        }
#endif //UNITY_EDITOR

        /// <summary>
        ///   <para> 运行时, assetbundle 加载资源. </para>
        /// </summary>
        /// <param name="location"></param>
        /// <param name="desiredType"></param>
        /// <param name="releaseDependenciesOnFailure"></param>
        /// <typeparam name="TObject"></typeparam>
        /// <returns></returns>
        private AsyncOperationHandle ProvideResourceRuntime(Type type, IResourceLocation location, ResourceProviderBase provider,
            bool async = true, bool releaseDependenciesOnFailure = true)
        {
            var providerType = provider.GetType();//typeof(BundleAssetProvider);
            var cacheKey = new LocationCacheKey(location, providerType);
            if (m_CacheAsyncOperations.TryGetValue(cacheKey, out var op))
            {
                op.IncrementReferenceCount();
                return new AsyncOperationHandle(op, location.assetbundleName);
            }

            op = CreateOperation<IAsyncOperation>(type, m_ReleaseOpCached, cacheKey);

            AsyncOperationHandle<IList<AsyncOperationHandle>> depOp;
            //if (location.hasDependencies && (provider is BundleAssetProvider))
            if (location.hasDependencies)
            {
                //List<IResourceLocation> locs = new List<IResourceLocation>();
                //foreach (var name in location.dependencies)
                //{
                //    // TODO zm.  这里有bug 依赖的assetbundle 的依赖没有处理.
                //    locs.Add(new ResourceLocationBase(name, null, null));
                //}

                //depOp = ProvideResourceGroup(locs, null, true);

                depOp = ProvideResourceGroup(location.depLocations, null, async, true);
            }
            else
                depOp = default(AsyncOperationHandle<IList<AsyncOperationHandle>>);

            // var depOp = (location.hasDependencies && (provider is BundleAssetProvider)) ? 
            //     ProvideResourceGroup(locs, null, true) : 
            //     default(AsyncOperationHandle<IList<AsyncOperationHandle>>);

            ((IGenericProviderOperation)op).Init(this, provider, location, depOp, releaseDependenciesOnFailure);
            var handle = StartOperation(op, depOp);

            if (depOp.IsValid())
                depOp.Release();

            return handle;
        }

        // public AsyncOperationHandle<IList<TObject>> ProvideResources<TObject>(IList<IResourceLocation> locations, bool releaseDependenciesOnFailure)
        // {

        // }

        /// <summary>
        ///   <para> 提供 GroupOperation. </para>
        /// </summary>
        /// <param name="locations"></param>
        /// <param name="desiredType"></param>
        /// <param name="callback"></param>
        /// <param name="releaseDependenciesOnFailure"></param>
        /// <returns></returns>
        internal AsyncOperationHandle<IList<AsyncOperationHandle>> ProvideResourceGroup(IList<IResourceLocation> locations,
            Action<AsyncOperationHandle> callback, bool async, bool releaseDependenciesOnFailure)
        {
            var groupHash = CalculateLocationsHash(locations);
            var depsKey = new DependenciesCacheKey(locations, groupHash);
            GroupOperation groupOp = AcquireGroupOpFromCache(depsKey);
            AsyncOperationHandle<IList<AsyncOperationHandle>> handle;
            if (groupOp == null)
            {
                groupOp = CreateOperation<GroupOperation>(typeof(GroupOperation), m_ReleaseOpCached, depsKey);
                var depOps = new List<AsyncOperationHandle>(locations.Count);
                foreach (var loc in locations)
                    depOps.Add(ProvideResource(typeof(ProviderOperation<>).MakeGenericType(new Type[] { typeof(IAssetBundleResource) }),
                            loc, async ? new AssetBundleProvider() : new AssetBundleProviderSync(), async, releaseDependenciesOnFailure));

                groupOp.Init(depOps, releaseDependenciesOnFailure);

                handle = StartOperation(groupOp, default(AsyncOperationHandle));
            }
            else
            {
                handle = groupOp.handle;
            }

            if (callback != null)
            {
                var depOpHandles = groupOp.GetDependentOps();
                foreach (var opHandle in depOpHandles)
                {
                    opHandle.completedCallback += callback;
                }
            }

            return handle;
        }

        private GroupOperation AcquireGroupOpFromCache(IOperationCacheKey key)
        {
            IAsyncOperation opGeneric;
            if (m_CacheAsyncOperations.TryGetValue(key, out opGeneric))
            {
                opGeneric.IncrementReferenceCount();
                return (GroupOperation)opGeneric;
            }
            return null;
        }

        private int CalculateLocationsHash(IList<IResourceLocation> locations)
        {
            if (locations == null || locations.Count == 0)
                return 0;
            int hash = 17;
            foreach (var loc in locations)
            {
                hash = hash * 31 + loc.Hash();
            }
            return hash;
        }

        /// <summary>
        ///   <para> 自定义转换资源地址. </para>
        /// </summary>
        public Func<IResourceLocation, string> internalIdTransformFunc { get; set; }
        public string TransformInternalId(IResourceLocation location)
        {

            // return internalIdTransformFunc == null ? 
            //     Path.Combine(Addressables.runtimePath, location.assetbundleName) : 
            //     internalIdTransformFunc(location);

            if (GameConfig.instance.enableAssetBundle)
                return internalIdTransformFunc == null ?
                Path.Combine(Addressables.runtimePath, location.assetbundleName) :
                internalIdTransformFunc(location);
            else
            {
#if UNITY_EDITOR
                return location.assetbundleName;
#else
                return internalIdTransformFunc == null ? 
                    Path.Combine(Addressables.runtimePath, location.assetbundleName) : 
                    internalIdTransformFunc(location);
#endif // UNITY_EDITOR
            }
        }

        /// <summary>
        ///   <para> 创建Op </para>
        /// </summary>
        /// <param name="onDestroyAction"> Op 销毁的回调函数. </param>
        /// <param name="cacheKey"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        internal T CreateOperation<T>(Type actualType, Action<IAsyncOperation> onDestroyAction, IOperationCacheKey cacheKey = null) where T : IAsyncOperation
        {
            var op = (T)allocator.New(actualType);
            if (cacheKey != null && (op is ICachable cache))
            {
                cache.key = cacheKey;
                AddOperationToCache(cacheKey, op);
            }

            op.onDestroy = onDestroyAction;

            return op;
        }

        internal void AddOperationToCache(IOperationCacheKey cacheKey, IAsyncOperation operation)
        {
            if (!IsOperationCached(cacheKey))
                m_CacheAsyncOperations[cacheKey] = operation;
            else
                UnityEngine.Debug.LogErrorFormat("[ResourceManager.AddOperationToCache] Already Cached. ");
        }

        internal void RemoveOpearationFromCache(IOperationCacheKey cacheKey)
        {
            m_CacheAsyncOperations.Remove(cacheKey);
        }



        internal bool IsOperationCached(IOperationCacheKey cacheKey)
        {
            return m_CacheAsyncOperations.ContainsKey(cacheKey);
        }

        internal int GetOperationCachedCount()
        {
            return m_CacheAsyncOperations.Count;
        }

        /// <summary>
        ///   <para> AsyncOperation 销毁回调. 如果有缓存则要移除缓存. </para>
        /// </summary>
        /// <param name="asyncOp"></param>
        internal void OnOperationDestroyCached(IAsyncOperation asyncOp)
        {
            allocator.Release(asyncOp.GetType().GetHashCode(), asyncOp);
            if ((asyncOp is ICachable cache) && cache.key != null)
                RemoveOpearationFromCache(cache.key);
        }

        void OnInstanceOperationDestroy(IAsyncOperation o)
        {
            m_TrackedInstanceOperations.Remove(o as InstanceOperation);
            allocator.Release(o.GetType().GetHashCode(), o);
        }

        void OnOperationDestroyNonCached(IAsyncOperation o)
        {
            allocator.Release(o.GetType().GetHashCode(), o);
        }

        public void Release(AsyncOperationHandle handle)
        {
            handle.Release();
        }

        public void Acquire(AsyncOperationHandle handle)
        {
            handle.Acquire();
        }

        public void Dispose()
        {
            if (MonoBehaviourCallbackHooks.Exists && m_RegisteredForCallbacks)
            {
                MonoBehaviourCallbackHooks.Instance.OnUpdateDelegate -= Update;
                m_RegisteredForCallbacks = false;
            }
        }


        #region Scene
        public AsyncOperationHandle<SceneInstance> ProvideScene(ISceneProvider sceneProvider, ResourceLocationBase location, LoadSceneMode loadMode,
            bool activateOnLoad, int priority)
        {
            if (sceneProvider == null)
                throw new NullReferenceException(" SceneProvider is null. ");

            return sceneProvider.ProvideScene(this, location, loadMode, activateOnLoad, priority);
        }

        public AsyncOperationHandle<SceneInstance> ReleaseScene(ISceneProvider sceneProvider, AsyncOperationHandle<SceneInstance> sceneLoadHandle)
        {
            if (sceneProvider == null)
                throw new NullReferenceException(" SceneProvder is null. ");

            return sceneProvider.ReleaseScene(this, sceneLoadHandle);
        }
        #endregion //Scene

        #region  Instantiate
        public AsyncOperationHandle<GameObject> ProvideInstance(IInstanceProvider provider, ResourceLocationBase location, InstantiationParameters instantiateParameters, bool async)
        {
            var key = new LocationCacheKey(location, typeof(IInstanceProvider));
            var depOp = ProvideResource<GameObject>(location, async ? new BundleAssetProvider() : new BundleAssetProviderSync(), async);
            var op = CreateOperation<InstanceOperation>(typeof(InstanceOperation), m_ReleaseInstanceOp, key);
            op.Init(this, provider, instantiateParameters, depOp);
            m_TrackedInstanceOperations.Add(op);
            return StartOperation<GameObject>(op, depOp);
        }

        public void CleanupSceneInstances(Scene scene)
        {
            List<InstanceOperation> handlesToRelease = null;
            foreach (var h in m_TrackedInstanceOperations)
            {
                if (h.result == null && scene == h.InstanceScene())
                {
                    if (handlesToRelease == null)
                        handlesToRelease = new List<InstanceOperation>();
                    handlesToRelease.Add(h);
                }
            }
            if (handlesToRelease != null)
            {
                foreach (var h in handlesToRelease)
                {
                    m_TrackedInstanceOperations.Remove(h);
                    h.DecrementReferenceCount();
                }
            }
        }
        #endregion // Instantiate



        public AsyncOperationHandle<TObject> CreateCompletedOperation<TObject>(TObject result, string errorMsg)
        {
            var success = string.IsNullOrEmpty(errorMsg);
            return CreateCompletedOperationInternal(result, success, !success ? new Exception(errorMsg) : null);
        }

        public AsyncOperationHandle<TObject> CreateCompletedOperationWithException<TObject>(TObject result, Exception exception)
        {
            return CreateCompletedOperationInternal(result, exception == null, exception);
        }

        internal AsyncOperationHandle<TObject> CreateCompletedOperationInternal<TObject>(TObject result, bool success, Exception exception, bool releaseDependenciesOnFailure = true)
        {
            var cop = CreateOperation<CompletedOperation<TObject>>(typeof(CompletedOperation<TObject>), m_ReleaseOpNonCached, null);
            cop.Init(result, success, exception, releaseDependenciesOnFailure);
            return StartOperation(cop, default(AsyncOperationHandle));
        }

        class CompletedOperation<TObject> : AsyncOperationBase<TObject>
        {
            bool m_Success;
            Exception m_Exception;
            bool m_ReleaseDependenciesOnFailure;
            public CompletedOperation() { }
            public void Init(TObject result, bool success, string errorMsg, bool releaseDependenciesOnFailure = true)
            {
                Init(result, success, !string.IsNullOrEmpty(errorMsg) ? new Exception(errorMsg) : null, releaseDependenciesOnFailure);
            }

            public void Init(TObject result, bool success, Exception exception, bool releaseDependenciesOnFailure = true)
            {
                this.result = result;
                m_Success = success;
                m_Exception = exception;
                m_ReleaseDependenciesOnFailure = releaseDependenciesOnFailure;
            }

            protected override void Execute()
            {
                Complete(result, m_Success, m_Exception, m_ReleaseDependenciesOnFailure);
            }
        }
    }
}
