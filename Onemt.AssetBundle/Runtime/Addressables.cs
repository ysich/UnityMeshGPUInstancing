using System;
using Onemt.Core.Define;
using Onemt.ResourceManagement;
using Onemt.ResourceManagement.AsyncOperation;
using Onemt.ResourceManagement.ResourceLocations;
using Onemt.ResourceManagement.ResourceProviders;
using Onemt.ResourceManagement.Util;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Onemt.AddressableAssets
{
    public class Addressables
    {
        private static AddressablesImpl s_AddressableImpl = new AddressablesImpl(new LRUCacheAllocationStrategy(1000, 1000, 100, 10));
        internal static AddressablesImpl instance => s_AddressableImpl;

        public static ResourceManager resourceManager => s_AddressableImpl.resourceManager;

        public static ResourceLocationMap locationMap => instance.locationMap;

#if UNITY_EDITOR
        public const string kLibraryPath = "Library/AssetBundle/";

        public static string buildPath => s_AddressableImpl.buildPath;
        public static string buildPathLocal => s_AddressableImpl.buildPathLocal;
        public static string buildPathRemote => s_AddressableImpl.buildPathRemote;

        public static string encryptPath => s_AddressableImpl.encryptPath;
        public static string encryptPathLocal => s_AddressableImpl.encryptPathLocal;
        public static string encryptPathRemote => s_AddressableImpl.encryptPathRemote;

        public static string compressPath => s_AddressableImpl.compressPath;
        public static string compressPathLocal => s_AddressableImpl.compressPathLocal;
        public static string compressPathRemote => s_AddressableImpl.compressPathRemote;

        public static string playerBuildDataPath => s_AddressableImpl.playerBuildDataPathEditor;
#endif // UNITY_EDITOR

        public static string runtimePath => s_AddressableImpl.runtimePath;
        
        public static void Initialization()
        {
            instance.Initialization();
        }

        //public static void NewVersionInitialization(List<ITask> taskList, CompleteCallback callback = null, System.Action initCompleteCallback = null)
        //{

        //    instance.Init(taskList, callback, initCompleteCallback);
        //}

        /// <summary>
        ///   <para> 添加下载资源. </para>
        /// </summary>
        /// <param name="location"></param>
        /// <param name="priority"></param>
        /// <param name="callback"></param>
        public static void Add(ResourceLocationBase location, DownloadPriority priority, Action<ErrorCode, string, byte[]> callback)
        {
            instance.Add(location, priority, callback);
        }

        //public static AsyncOperationHandle<TObject> LoadAssetSync<TObject>(string assetName)
        //{
        //    var handle = s_AddressableImpl.LoadAssetSync<TObject>(assetName);
        //    return handle;
        //}

        /// <summary>
        ///   <para> 异步加载资源. </para>
        /// </summary>
        /// <param name="assetName"> 资源名称. </param>
        /// <typeparam name="TObject"></typeparam>
        /// <returns></returns>
        public static AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(string assetName)
        {
            return s_AddressableImpl.LoadAssetAsync<TObject>(assetName);
        }

        /// <summary>
        ///   <para> 异步加载场景. </para>
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="loadMode"></param>
        /// <param name="activateOnLoad"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static AsyncOperationHandle<SceneInstance> LoadSceneAsync(string sceneName, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
        {
            return s_AddressableImpl.LoadSceneAsync(sceneName, loadMode, activateOnLoad, priority); 
        }

        public static AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle handle, bool autoReleaseHandle = true)
        {
            return s_AddressableImpl.UnloadSceneAsync(handle, autoReleaseHandle);
        }

        public static void Release<TObject>(TObject obj)
        {
            s_AddressableImpl.Release(obj);
        }

        public static void Release<TObject>(AsyncOperationHandle<TObject> handle)
        {
            s_AddressableImpl.Release(handle);
        }

        public static void Release(AsyncOperationHandle handle)
        {
            s_AddressableImpl.Release(handle);
        }

        public static bool ReleaseInstance(GameObject instance)
        {
            return s_AddressableImpl.ReleaseInstance(instance);
        }

        public static bool ReleaseInstance(AsyncOperationHandle handle)
        {
            s_AddressableImpl.Release(handle);
            return true;
        }

        public static bool ReleaseInstance(AsyncOperationHandle<GameObject> handle)
        {
            s_AddressableImpl.Release(handle);
            return true;
        }

        public static AsyncOperationHandle<GameObject> InstantiateSync(string assetName, Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true)
        {
            var handle = s_AddressableImpl.InstantiateSync(assetName, new InstantiationParameters(parent, instantiateInWorldSpace), trackHandle);
            return handle;
        }

        public static AsyncOperationHandle<GameObject> InstantiateAsync(string assetName, Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true)
        {
            return s_AddressableImpl.InstantiateAsync(assetName, new InstantiationParameters(parent, instantiateInWorldSpace), trackHandle);
        }

        public static AsyncOperationHandle<GameObject> InstantiateSync(string assetName, Vector3 position, Quaternion rotation, Transform parent = null, bool trackHandle = true)
        {
            return s_AddressableImpl.InstantiateSync(assetName, position, rotation, parent, trackHandle);
        }

        public static AsyncOperationHandle<GameObject> InstantiateAsync(string assetName, Vector3 position, Quaternion rotation, Transform parent = null, bool trackHandle = true)
        {
            return s_AddressableImpl.InstantiateAsync(assetName, position, rotation, parent, trackHandle);
        }
    }
}


