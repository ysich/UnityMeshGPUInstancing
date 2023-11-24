/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 11:06:53
-- 概述:
---------------------------------------------------------------------------------------*/

#if UNITY_EDITOR

using System;
using UnityEditor;
using Onemt.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;
using System.Collections.Generic;

namespace Onemt.ResourceManagement.ResourceProviders
{
    public class AssetDatabaseProvider : ResourceProviderBase
    {
        private float m_LoadDelay = 0.1f;

        public AssetDatabaseProvider()
        {
        }

        public AssetDatabaseProvider(float delay = 0.25f)
        {
            m_LoadDelay = delay;
        }

        public override void Provide(ProvideHandle provideHandle)
        {
            new InternalOp().Start(provideHandle, m_LoadDelay);
        }        

        public override void Release(IResourceLocation location, object obj)
        {
            base.Release(location, obj);
        }

        private static Object[] LoadAllAssetRepresentationsAtPath(string assetPath)
        {
            return AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
        }

        private static Object LoadMainAssetAtPath(string assetPath)
        {
            return AssetDatabase.LoadMainAssetAtPath(assetPath);
        }

        internal static Object[] LoadAssetsWithSubAssets(string assetPath)
        {
            var subObjects = LoadAllAssetRepresentationsAtPath(assetPath);
            var allObjects = new Object[subObjects.Length + 1];
            allObjects[0] = LoadMainAssetAtPath(assetPath);
            for (int i = 0; i < subObjects.Length; i++)
                allObjects[i + 1] = subObjects[i];
            return allObjects;
        }

        class InternalOp
        {
            private ProvideHandle m_ProvideHandle;
            private bool m_Loaded;

            public void Start(ProvideHandle provideHandle, float loadedDelay)
            {
                m_Loaded = false;
                m_ProvideHandle = provideHandle;

                LoadImmediate();
            }

            void LoadImmediate()
            {
                if (m_Loaded)
                    return;
                
                m_Loaded = true;
                string assetPath = m_ProvideHandle.resourceManager.TransformInternalId(m_ProvideHandle.location);

                object result = null;
                if (m_ProvideHandle.requestedType.IsArray)
                    result = ResourceManagerConfig.CreateArrayResult(m_ProvideHandle.requestedType, LoadAssetsWithSubAssets(assetPath));
                else if (m_ProvideHandle.requestedType.IsGenericType && typeof(IList<>) == m_ProvideHandle.requestedType.GetGenericTypeDefinition())
                    result = ResourceManagerConfig.CreateListResult(m_ProvideHandle.requestedType, LoadAssetsWithSubAssets(assetPath));
                else
                    result = LoadAssetAtPath(assetPath, m_ProvideHandle.requestedType);
                m_ProvideHandle.Complete(result, result != null, null);
            }

            internal static object LoadAssetAtPath(string assetPath, Type type)
            {
                Object obj = AssetDatabase.LoadAssetAtPath(assetPath, type);

                return obj;
            }
        }
    }
}

#endif // UNITY_EDITOR
