/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 11:06:53
-- 概述:
---------------------------------------------------------------------------------------*/

using UnityEngine.SceneManagement;
using Onemt.ResourceManagement.AsyncOperation;
using Onemt.ResourceManagement.ResourceLocations;

namespace Onemt.ResourceManagement.ResourceProviders
{
    public struct SceneInstance
    {
        private Scene m_Scene;
        public Scene scene { get => m_Scene; set => m_Scene = value; }
        internal UnityEngine.AsyncOperation m_Operation;

        /// <summary>
        ///   <para> 场景加载完成时 直接激活. </para>
        /// </summary>
        /// <returns></returns>
        public UnityEngine.AsyncOperation ActivateAsync()
        {
            m_Operation.allowSceneActivation = true;
            return m_Operation;
        }

        public override int GetHashCode()
        {
            return scene.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SceneInstance sceneInstance))
                return false;

            return scene.Equals(sceneInstance.scene);
        }
    }

    public interface ISceneProvider
    {
        AsyncOperationHandle<SceneInstance> ProvideScene(ResourceManager resourceManager, IResourceLocation location, 
            LoadSceneMode loadMode, bool activateOnLoad, int priority);
        
        AsyncOperationHandle<SceneInstance> ReleaseScene(ResourceManager resourceManager, AsyncOperationHandle<SceneInstance> sceneLoadHandle);
    }
}


