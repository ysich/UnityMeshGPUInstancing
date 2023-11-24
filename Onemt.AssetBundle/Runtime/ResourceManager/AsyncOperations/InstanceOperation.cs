/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 11:51:03
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Onemt.ResourceManagement.ResourceProviders;

namespace Onemt.ResourceManagement.AsyncOperation
{
    internal class InstanceOperation : AsyncOperationBase<GameObject>
    {
        AsyncOperationHandle<GameObject> m_Dependency;
        InstantiationParameters m_InstantiationParameters;
        IInstanceProvider m_instanceProvider;
        GameObject m_Instance;
        Scene m_Scene;

        [UnityEngine.Scripting.Preserve]
        public InstanceOperation() { }

        public void Init(ResourceManager rm, IInstanceProvider instanceProvider, InstantiationParameters instantiationParams,
            AsyncOperationHandle<GameObject> dependency)
        {
            m_ResourceManager = rm;
            m_Dependency = dependency;
            m_instanceProvider = instanceProvider;
            m_InstantiationParameters = instantiationParams;
            m_Scene = default(Scene);
        }

        internal override DownloadStatus GetDownloadStatus(HashSet<object> visited)
        {
            return m_Dependency.IsValid() ? m_Dependency.InternalGetDownloadStatus(visited) : new DownloadStatus() { isDone = isDone };
        }

        protected override void GetDependencies(List<AsyncOperationHandle> deps)
        {
            deps.Add(m_Dependency);
        }

        public Scene InstanceScene() => m_Scene;

        protected override void Destroy()
        {
            m_instanceProvider.ReleaseInstance(m_ResourceManager, m_Instance);
        }

        protected override float progress
        {
            get
            {
                return m_Dependency.percentComplete;
            }
        }

        protected override void Execute()
        {
            if (m_Dependency.status == AsyncOperationStatus.Succeeded)
            {
                m_Instance = m_instanceProvider.ProvideInstance(m_ResourceManager, m_Dependency, m_InstantiationParameters);
                if (m_Instance != null)
                    m_Scene = m_Instance.scene;
                Complete(m_Instance, true, null);
            }
            else
            {
                Complete(m_Instance, false, string.Format("Dependency operation failed."));
            }
        }
    }
}
