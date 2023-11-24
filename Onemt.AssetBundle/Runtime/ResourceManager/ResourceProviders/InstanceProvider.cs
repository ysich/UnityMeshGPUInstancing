/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 11:06:53
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Onemt.ResourceManagement.AsyncOperation;

namespace Onemt.ResourceManagement.ResourceProviders
{
    public struct InstantiationParameters
    {
        private Vector3 m_Position;
        public Vector3 position { get => m_Position; }

        private Quaternion m_Rotation;
        public Quaternion rotation { get => m_Rotation; }
        
        private Transform m_Parent;
        public Transform parent { get => m_Parent; }

        private bool m_InstantiateInWorldPosition;
        public bool instantiateInWorldPosition { get => m_InstantiateInWorldPosition; }

        private bool m_SetPositionRotation;
        public bool setPositionRotation { get => m_SetPositionRotation; }

        public InstantiationParameters(Transform parent, bool instantiateInWorldPosition)
        {
            m_Position = Vector3.one;
            m_Rotation = Quaternion.identity;
            m_Parent = parent;
            m_InstantiateInWorldPosition = instantiateInWorldPosition;
            m_SetPositionRotation = false;
        }

        public InstantiationParameters(Vector3 position, Quaternion rotation, Transform parent)
        {
            m_Position = position;
            m_Rotation = rotation;
            m_Parent = parent;
            m_InstantiateInWorldPosition = false;
            m_SetPositionRotation = true;
        }

        public TObject Instantiate<TObject>(TObject source) where TObject : Object
        {
            TObject result;
            if (m_Parent == null)
            {
                if (m_SetPositionRotation)
                    result = Object.Instantiate(source, m_Position, m_Rotation);
                else
                    result = Object.Instantiate(source);
            }
            else
            {
                if (m_SetPositionRotation)
                    result = Object.Instantiate(source, m_Position, m_Rotation, m_Parent);
                else
                    result = Object.Instantiate(source, m_Parent, m_InstantiateInWorldPosition);
            }

            return result;
        }
    }

    public interface IInstanceProvider
    {
        GameObject ProvideInstance(ResourceManager resourceManager, AsyncOperationHandle<GameObject> prefabHandle, InstantiationParameters parameters);

        void ReleaseInstance(ResourceManager resourceManager, GameObject instance);
    }

    public class InstanceProvider : IInstanceProvider
    {
        private Dictionary<GameObject, AsyncOperationHandle<GameObject>> m_InstanceObjectToPrefabHandle = new Dictionary<GameObject, AsyncOperationHandle<GameObject>>();

        public GameObject ProvideInstance(ResourceManager resourceMamager, AsyncOperationHandle<GameObject> prefabHandle, InstantiationParameters instantiationParameters)
        {
            GameObject result = instantiationParameters.Instantiate(prefabHandle.result);
            m_InstanceObjectToPrefabHandle.Add(result, prefabHandle);
            return result;
        }

        public void ReleaseInstance(ResourceManager resourceManager, GameObject instance)
        {
            if (!m_InstanceObjectToPrefabHandle.TryGetValue(instance, out var handle))
                UnityEngine.Debug.LogErrorFormat("[InstanceProvider.ReleaseInstance()]  Releasing unknown GameObject {0}", instance);
            else 
            {
                resourceManager.Release(handle);
                m_InstanceObjectToPrefabHandle.Remove(instance);
            }

            if (instance != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(instance);
                else
                    Object.DestroyImmediate(instance);
            }
        }
    }
}
