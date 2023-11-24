using System.Collections.Generic;
using Onemt.ResourceManagement;
using Onemt.ResourceManagement.Util;
using Onemt.ResourceManagement.AsyncOperation;
using Onemt.ResourceManagement.ResourceLocations;
using System;
using UnityEngine.SceneManagement;
using Onemt.ResourceManagement.ResourceProviders;
using System.Linq;
using System.IO;
using UnityEngine;
using Onemt.Framework.Config;
using Onemt.Core.Define;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Onemt.AddressableAssets
{
    public partial class AddressablesImpl
    {
        public string streamingAsstesSubFolder => "aa";

#if UNITY_EDITOR
        /// <summary>
        ///   <para> assetbundle 构建输出路径. </para>
        /// </summary>
        /// <value></value>
        public string buildPath => Addressables.kLibraryPath + streamingAsstesSubFolder + "/" + EditorUserBuildSettings.activeBuildTarget.ToString() + "/" + "Build";
        public string buildPathLocal => Path.Combine(buildPath, ConstDefine.kPathLocal);
        public string buildPathRemote => Path.Combine(buildPath, ConstDefine.kPathRemote);

        /// <summary>
        ///   <para> 偏移加密路径. </para>
        /// </summary>
        public string encryptPath => buildPath.Replace("Build", "Encrypt");
        public string encryptPathLocal => Path.Combine(encryptPath, ConstDefine.kPathLocal);
        public string encryptPathRemote => Path.Combine(encryptPath, ConstDefine.kPathRemote);

        /// <summary>
        ///   <para> 压缩路径. </para>
        /// </summary>
        public string compressPath => buildPath.Replace("Build", "Compress");
        public string compressPathLocal => Path.Combine(compressPath, ConstDefine.kPathLocal);
        public string compressPathRemote => Path.Combine(compressPath, ConstDefine.kPathRemote);

#endif // UNITY_EDITOR

        /// <summary>
        ///   <para> Editor 模式 assetbundle streaming 路径. </para>
        /// </summary>
        /// <value></value>
        public string playerBuildDataPathEditor
        {
            get => GameConfig.instance.streamingDataPath;
        }

        /// <summary>
        ///   <para> Runtime 模式 assetbundle 路径. </para>
        /// </summary>
        public string playerBuildDataPathRuntime
        {
            get => GameConfig.instance.persistentAssetPath;
        }

        public string runtimePath
        {
            get 
            {
#if UNITY_EDITOR
                return playerBuildDataPathEditor;
#else
                return playerBuildDataPathRuntime;
#endif // UNITY_EDITOR
            }
        }

        private ISceneProvider m_SceneProvider;
        private IInstanceProvider m_InstanceProvider;
        private ResourceManager m_ResourceManager;
        public ResourceManager resourceManager
        {
            get 
            {
                if (m_ResourceManager == null)
                    m_ResourceManager = new ResourceManager(new LRUCacheAllocationStrategy(1000, 1000, 100, 10));

                return m_ResourceManager;
            }
        }

        internal HashSet<AsyncOperationHandle> m_SceneInstances = new HashSet<AsyncOperationHandle>();
        internal int sceneOperationCount { get => m_SceneInstances.Count; }

        /// <summary>
        ///   <para> result To Handle. 目前主要用于释放检索handle </para>
        /// </summary>
        /// <typeparam name="object"></typeparam>
        /// <typeparam name="AsyncOperationHandle"></typeparam>
        /// <returns></returns>
        private Dictionary<object, AsyncOperationHandle> m_ResultToHandle = new Dictionary<object, AsyncOperationHandle>();
        internal int trackedHandleCount { get => m_ResultToHandle.Count; }

        Action<AsyncOperationHandle> m_OnHandleCompleteAction;
        Action<AsyncOperationHandle> m_OnSceneHandleCompleteAction;
        Action<AsyncOperationHandle> m_OnHandleDestroyedAction;

        public AddressablesImpl(IAllocationStrategy allocator)
        {
            m_ResourceManager = new ResourceManager(allocator);
            
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        internal void ReleaseSceneManagerOperation()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        // public AsyncOperationHandle<TObject> LoadAssetSync<TObject>(string assetName)
        // {
        //     return LoadAsset<TObject>(assetName, false);
        // }

        public AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(string assetName)
        {
            return LoadAsset<TObject>(assetName);
        }

        private AsyncOperationHandle<TObject> LoadAsset<TObject>(string assetName, bool async = true)
        {
            if (!TryGetResourceLocation(assetName, out var depLocation))
            {
                UnityEngine.Debug.LogErrorFormat("[AddressablesImpl.TryGetResourceLocation]  Error with AssetName:  {0}", assetName);
                return default;
            }

            // TODO zm, new 操作是否需要改成池子
            ResourceLocationBase location = depLocation;
            if (GameConfig.instance.enableAssetBundle)
                location = new ResourceLocationBase(depLocation.assetbundleName, null, depLocation.dependencies, assetName, new List<IResourceLocation> { depLocation });

            return TrackHandle(resourceManager.ProvideResource<TObject>(location, async ? new BundleAssetProvider() : new BundleAssetProviderSync(), async));
        }

        /// <summary>
        ///   <para> 添加handle 完成回调，存储到 resultToHandle 用于后续的释放. </para>
        /// </summary>
        /// <param name="handle"></param>
        /// <typeparam name="TObject"></typeparam>
        /// <returns></returns>
        private AsyncOperationHandle<TObject> TrackHandle<TObject>(AsyncOperationHandle<TObject> handle)
        {
            handle.completedCallback += m_OnHandleCompleteAction;

            return handle;
        }
        private AsyncOperationHandle TrackHandle(AsyncOperationHandle handle)
        {
            handle.completedCallback += m_OnHandleCompleteAction;

            return handle;
        }

        private bool TryGetResourceLocation(string assetName, out ResourceLocationBase location)
        {
            return m_ResLocationMap.TryGetLocation(assetName, out location);
        } 

        internal void OnSceneUnload(Scene scene)
        {
            foreach (var s in m_SceneInstances)
            {
                if (!s.IsValid())
                {
                    m_SceneInstances.Remove(s);
                }
            }
        }

        private void OnSceneHandleCompleted(AsyncOperationHandle handle)
        {
            if (handle.status != AsyncOperationStatus.Succeeded)
                return;

            m_SceneInstances.Add(handle);
            if (!m_ResultToHandle.ContainsKey(handle.result))
            {
                handle.destroyCallback += m_OnHandleDestroyedAction;
                m_ResultToHandle.Add(handle.result, handle);
            }
        }

        void OnHandleCompleted(AsyncOperationHandle handle)
        {
            if (handle.status != AsyncOperationStatus.Succeeded)
                return;

            if (!m_ResultToHandle.ContainsKey(handle.result))
            {
                handle.destroyCallback += m_OnHandleDestroyedAction;
                m_ResultToHandle.Add(handle.result, handle);
            }
        }

        /// <summary>
        ///   <para> handle 释放回调 -> 移除result To Handle </para>
        /// </summary>
        /// <param name="handle"></param>
        void OnHandleDestroyed(AsyncOperationHandle handle)
        {
            if (handle.status != AsyncOperationStatus.Succeeded)
                return;

            m_ResultToHandle.Remove(handle.result);
        }

        /// <summary>
        ///   <para> handle 完成时自动释放. </para>
        /// </summary>
        /// <param name="handle"></param>
        internal void AutoReleaseHandleOnCompletion(AsyncOperationHandle handle)
        {
            handle.completedCallback += op => Release(op);
        }

        internal void AutoReleaseHandleOnCompletion<TObject>(AsyncOperationHandle<TObject> handle)
        {
            handle.completedCallback += op => Release(op);
        }

        /// <summary>
        ///   <para> 释放对象 </para>
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="TObject"></typeparam>
        public void Release<TObject>(TObject obj)
        {
            if (obj == null)
            {
                UnityEngine.Debug.LogError("[AddressablesImpl.Release()] - Trying to release null object. ");
                return;
            }

            if (m_ResultToHandle.TryGetValue(obj, out var handle))
                m_ResourceManager.Release(handle);
            else
                UnityEngine.Debug.LogErrorFormat("[AddressablesImpl.Release()] - Nothing is beging released.   objName: {0}", obj.ToString());
        }

        public void Release<TObject>(AsyncOperationHandle<TObject> handle)
        {
            if (typeof(TObject) == typeof(SceneInstance))
            {
                SceneInstance sceneInstance = (SceneInstance)Convert.ChangeType(handle.result, typeof(SceneInstance));
                if (sceneInstance.scene.isLoaded && handle.referenceCount == 1)
                {
                    if (sceneOperationCount == 1 && m_SceneInstances.First().Equals(handle))
                        m_SceneInstances.Clear();
                    UnloadSceneAsync(handle);
                }
            }
            m_ResourceManager.Release(handle);
        }

        public void Release(AsyncOperationHandle handle)
        {
            m_ResourceManager.Release(handle);
        }



#region Scene
        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(string sceneName, LoadSceneMode loadsceneMode = LoadSceneMode.Single,
            bool activateOnLoad = true, int priority = 100, bool trackHandle = true)
        {
            if (!m_ResLocationMap.TryGetLocation(sceneName, out var depLocation))
            {
                UnityEngine.Debug.LogErrorFormat("[AddressablesImpl.LoadSceneAsync]  Can not Find Location With Scene : {0}", sceneName);
                return default(AsyncOperationHandle<SceneInstance>);
            }

            ResourceLocationBase location = depLocation;
            if (GameConfig.instance.enableAssetBundle)
            {
                location = new ResourceLocationBase(depLocation.assetbundleName, null, depLocation.dependencies, sceneName, new List<IResourceLocation> { depLocation });
            }

            //location.assetName = sceneName;
            return LoadSceneAsync(location, loadsceneMode, activateOnLoad, priority, trackHandle);
        }

        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(ResourceLocationBase location, LoadSceneMode loadSceneMode = LoadSceneMode.Single, 
            bool activateOnLoad = true, int priority = 100, bool trackHandle = true)
        {
            var handle = resourceManager.ProvideScene(m_SceneProvider, location, loadSceneMode, activateOnLoad, priority);
            if (trackHandle)
                return TrackHandle(handle);

            return handle;
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(SceneInstance sceneInstance, bool autoReleaseHandle = true)
        {
            AsyncOperationHandle handle;
            if (!m_ResultToHandle.TryGetValue(sceneInstance, out handle))
            {                
                string msg = string.Format("Addressables.UnloadSceneAsync() - Cannot find handle for scene {0}", sceneInstance);
                UnityEngine.Debug.LogError(msg);
                return resourceManager.CreateCompletedOperation<SceneInstance>(sceneInstance, msg);
            }

            return UnloadSceneAsync(handle, autoReleaseHandle);
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle handle, bool autoReleaseHandle = true)
        {
            return UnloadSceneAsync(handle.Convert<SceneInstance>(), autoReleaseHandle);
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle<SceneInstance> handle, bool autoReleaseHandle = true)
        {
            return InternalUnloadScene(handle, autoReleaseHandle);
        }

        internal AsyncOperationHandle<SceneInstance> InternalUnloadScene(AsyncOperationHandle<SceneInstance> handle, bool autoReleaseHandle)
        {
            var relOp = resourceManager.ReleaseScene(m_SceneProvider, handle);
            if (autoReleaseHandle)
                AutoReleaseHandleOnCompletion(relOp);

            return relOp;
        }

        internal void OnSceneUnloaded(Scene scene)
        {
            foreach (var s in m_SceneInstances)
            {
                if (!s.IsValid())
                {
                    m_SceneInstances.Remove(s);
                    break;
                }

                var sceneHandle = s.Convert<SceneInstance>();
                if (sceneHandle.result.scene == scene)
                {
                    m_SceneInstances.Remove(s);
                    m_ResultToHandle.Remove(s.result);
                    var op = m_ResourceManager.ReleaseScene(m_SceneProvider, sceneHandle);
                    AutoReleaseHandleOnCompletion(op);
                    break;
                }
            }
            m_ResourceManager.CleanupSceneInstances(scene);
        }
        
#endregion // Scene

#region     Instantiate
        public AsyncOperationHandle<GameObject> InstantiateSync(string assetName, Vector3 position, Quaternion rotation, Transform parent = null, bool trackHandle = true)
        {
            return Instantiate(assetName, new InstantiationParameters(position, rotation, parent), false, trackHandle);
        }

        public AsyncOperationHandle<GameObject> InstantiateAsync(string assetName, Vector3 position, Quaternion rotation, Transform parent = null, bool trackHandle = true)
        {
            return Instantiate(assetName, new InstantiationParameters(position, rotation, parent), true, trackHandle);
        }

        public AsyncOperationHandle<GameObject> InstantiateSync(string assetName, InstantiationParameters instantiateParameters, bool trackHandle = true)
        {
            return Instantiate(assetName, instantiateParameters, false, trackHandle);
        }

        public AsyncOperationHandle<GameObject> InstantiateAsync(string assetName, InstantiationParameters instantiateParameters, bool trackHandle = true)
        {
            return Instantiate(assetName, instantiateParameters, true, trackHandle);
        }

        private AsyncOperationHandle<GameObject> Instantiate(string assetName, InstantiationParameters instantiateParameters, bool async, bool trackHandle = true)
        {
            if (!TryGetResourceLocation(assetName, out var depLocation))
            {
                UnityEngine.Debug.LogErrorFormat("[AddressablesImpl.TryGetResourceLocation]  Error with AssetName:  {0}", assetName);
                return default;
            }

            AsyncOperationHandle<GameObject> handle;
            if (GameConfig.instance.enableAssetBundle)
            {
                ResourceLocationBase location = new ResourceLocationBase(assetName, null, depLocation.dependencies, assetName, new List<IResourceLocation> { depLocation });
                handle = resourceManager.ProvideInstance(m_InstanceProvider, location, instantiateParameters, async);
            }
            else
                handle = resourceManager.ProvideInstance(m_InstanceProvider, depLocation, instantiateParameters, async);

            handle.completedCallback += m_OnHandleCompleteAction;
            return handle;
        }

        public bool ReleaseInstance(GameObject instance)
        {
            if (instance == null)
            {
                UnityEngine.Debug.LogError("[AddressablesImpl.ReleaseInstance()] - Trying to release null object. ");
                return false;
            }

            if (m_ResultToHandle.TryGetValue(instance, out var handle))
                Release(handle);
            else
                return false;

            return true;
        }
#endregion  // Instantiate
    }
}
