/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 11:06:53
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using Onemt.ResourceManagement.AsyncOperation;
using UnityEngine.SceneManagement;
using Onemt.ResourceManagement.ResourceLocations;

namespace Onemt.ResourceManagement.ResourceProviders
{
    // TODO zm. 场景的 AsyncOperate  完成回调没处理.
    class SceneOp : AsyncOperationBase<SceneInstance>, IUpdateReceiver
    {
        private bool m_ActivateOnLoad;
        private SceneInstance m_Inst;
        private IResourceLocation m_Location;
        private LoadSceneMode m_LoadMode;
        private int m_Priority;
        private AsyncOperationHandle<IList<AsyncOperationHandle>> m_DepOp;
        //private ResourceManager m_ResourceManager;

        public SceneOp(ResourceManager rm)
        {
            m_ResourceManager = rm;
        }

        internal override DownloadStatus GetDownloadStatus(HashSet<object> visited)
        {
            return m_DepOp.IsValid() ? m_DepOp.InternalGetDownloadStatus(visited) : new DownloadStatus() {isDone = isDone };
        }

        public void Init(IResourceLocation location, LoadSceneMode loadSceneMode, bool activateOnLoad, 
            int priority, AsyncOperationHandle<IList<AsyncOperationHandle>> depOp)
        {
            m_DepOp = depOp;
            if (m_DepOp.IsValid())
                m_DepOp.Acquire();

            m_Location = location;
            m_LoadMode = loadSceneMode;
            m_ActivateOnLoad = activateOnLoad;
            m_Priority = priority;
        }

        protected override void GetDependencies(List<AsyncOperationHandle> dependencies)
        {
            if (m_DepOp.IsValid())
                dependencies.Add(m_DepOp);
        }

        protected override void Execute()
        {
            bool loadingFromBundle = false;
            if (m_DepOp.IsValid())
            {
                foreach (var d in m_DepOp.result)
                {
                    if ((d.result is IAssetBundleResource abResource) &&
                        abResource.GetAssetBundle() != null)
                        loadingFromBundle = true;
                }
            }

            m_Inst = InternalLoadScene(m_Location, loadingFromBundle, m_LoadMode, m_ActivateOnLoad, m_Priority);

            ((IUpdateReceiver)this).Update(0.0f);
            if(!isDone)
                m_ResourceManager.AddUpdateReceiver(this);

            m_HasExecuted = true;
        }

        /// <summary>
        ///   <para> 加载场景 </para>
        /// </summary>
        /// <param name="location"></param>
        /// <param name="loadingFromBundle"></param>
        /// <param name="loadSceneMode"></param>
        /// <param name="activateOnLoad"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        internal SceneInstance InternalLoadScene(IResourceLocation location, bool loadingFromBundle, 
            LoadSceneMode loadSceneMode, bool activateOnLoad, int priority)
        {
            //var path = m_ResourceManager.TransformInternalId(location);
            //var path = Path.Combine(Addressables.buildPath, location.assetName + ".unity");
            var path = string.Empty;
            if (loadingFromBundle)
                path = location.assetName;
            else
                path = location.assetbundleName;
            var asyncOp = InternalLoad(path, loadingFromBundle, loadSceneMode);
            asyncOp.allowSceneActivation = activateOnLoad;
            asyncOp.priority = priority;

            return new SceneInstance() { m_Operation = asyncOp, scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1) };
        }

        /// <summary>
        ///   <para> 加载场景的实现. </para>
        ///   <para> 1. 非编辑器模式加载  2. 编辑器 assetbundle模式加载  3. 编辑器非assetbundle 模式加载. </para>
        /// </summary>
        /// <param name="path"></param>
        /// <param name="loadingFromBundle"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        UnityEngine.AsyncOperation InternalLoad(string path, bool loadingFromBundle, LoadSceneMode mode)
        {
            // return SceneManager.LoadSceneAsync("Login", new LoadSceneParameters() { loadSceneMode = mode });
            // return SceneManager.LoadSceneAsync(path, new LoadSceneParameters() { loadSceneMode = mode });
#if !UNITY_EDITOR
            // 打包模式下的加载
            return SceneManager.LoadSceneAsync(path, new LoadSceneParameters() { loadSceneMode = mode });
#else
            if (loadingFromBundle)
                // 编辑器开启 assetbundle 模式
                return SceneManager.LoadSceneAsync(path, new LoadSceneParameters() { loadSceneMode = mode });
            else
            {
                // 编辑器模式
                if (!path.ToLower().StartsWith("assets/") && !path.ToLower().StartsWith("packages/"))
                    path = "Assets/" + path;
                if (path.LastIndexOf(".unity") == -1)
                    path += ".unity";

                return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters() { loadSceneMode = mode });
            }
#endif // !UNITY_EDITOR
        }

        /// <summary>
        ///   <para> 添加到ResourceManager中进行Update判断. </para>
        /// </summary>
        /// <param name="unscaledDeltaTime"></param>
        void IUpdateReceiver.Update(float unscaledDeltaTime)
        {
            if (m_Inst.m_Operation.isDone || (!m_Inst.m_Operation.allowSceneActivation && m_Inst.m_Operation.progress == .9f))
            {
                m_ResourceManager.RemoveUpdateReciever(this);
                Complete(m_Inst, true, null);
            }
        }

        protected override void Destroy()
        {
            if (m_DepOp.IsValid())
                m_DepOp.Release();

            base.Destroy();
        }
    }

    class UnloadSceneOp : AsyncOperationBase<SceneInstance>
    {        
        SceneInstance m_Instance;
        AsyncOperationHandle<SceneInstance> m_sceneLoadHandle;
        public void Init(AsyncOperationHandle<SceneInstance> sceneLoadHandle)
        {
            if (sceneLoadHandle.referenceCount > 0)
            {
                m_sceneLoadHandle = sceneLoadHandle;
                m_Instance = m_sceneLoadHandle.result;
            }
        }

        protected override void Execute()
        {
            if (m_sceneLoadHandle.IsValid() && m_Instance.scene.isLoaded)
            {
                var unloadOp = SceneManager.UnloadSceneAsync(m_Instance.scene);
                if (unloadOp == null)
                    UnloadSceneCompletedNoRelease(null);
                else
                    unloadOp.completed += UnloadSceneCompletedNoRelease;
            }
            else
                UnloadSceneCompleted(null);
        }

        private void UnloadSceneCompleted(UnityEngine.AsyncOperation obj)
        {
            Complete(m_Instance, true, "");
            if (m_sceneLoadHandle.IsValid())
                m_sceneLoadHandle.Release();
        }
        
        private void UnloadSceneCompletedNoRelease(UnityEngine.AsyncOperation obj)
        {
            Complete(m_Instance, true, "");
        }

        protected override float progress
        {
            get { return m_sceneLoadHandle.percentComplete; }
        }
    }

    public class SceneProvider : ISceneProvider
    {
        public AsyncOperationHandle<SceneInstance> ProvideScene(ResourceManager resourceManager, IResourceLocation location, 
            LoadSceneMode loadMode, bool activateOnLoad, int priority)
        {
            AsyncOperationHandle<IList<AsyncOperationHandle>> depOp;
            if (location.hasDependencies)
                depOp = resourceManager.ProvideResourceGroup(location.depLocations, null, true, true);
            else
                depOp = default(AsyncOperationHandle<IList<AsyncOperationHandle>>);

            SceneOp sceneOp = new SceneOp(resourceManager);
            sceneOp.Init(location, loadMode, activateOnLoad, priority, depOp);

            var handle = resourceManager.StartOperation<SceneInstance>(sceneOp, depOp);

            if (depOp.IsValid())
                depOp.Release();

            return handle;
        }
        
        public AsyncOperationHandle<SceneInstance> ReleaseScene(ResourceManager resourceManager, AsyncOperationHandle<SceneInstance> sceneLoadHandle)
        {
            var unloadOp = new UnloadSceneOp();
            unloadOp.Init(sceneLoadHandle);
            return resourceManager.StartOperation(unloadOp, sceneLoadHandle);
        }
    }
}
