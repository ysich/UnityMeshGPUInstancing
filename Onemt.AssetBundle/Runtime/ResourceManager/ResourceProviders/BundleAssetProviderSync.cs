/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 11:06:53
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Onemt.ResourceManagement.ResourceProviders
{
    public class BundleAssetProviderSync : ResourceProviderBase
    {
        internal class InternalOp
        {
            private AssetBundle m_AssetBundle;
            private ProvideHandle m_ProvideHandle;
            private object m_Result;

            public void Start(ProvideHandle provideHandle)
            {
                m_ProvideHandle = provideHandle;

                List<object> deps = new List<object>();
                m_ProvideHandle.GetDependencies(deps);
                var assetBundleRes = LoadBundleFromDependecies(deps);
                if (assetBundleRes == null)
                {
                    m_ProvideHandle.Complete<AssetBundle>(null, false, new System.Exception("Unable to load Dependent bundle from location : " + m_ProvideHandle.location));
                }
                else
                {
                    m_AssetBundle = assetBundleRes.GetAssetBundle();

                    BeginAssetLoad();
                }
            }

            private void BeginAssetLoad()
            {
                if (m_AssetBundle == null)
                {
                    m_ProvideHandle.Complete<AssetBundle>(null, false, new System.Exception("Unable to load assetbundle from location : " + m_ProvideHandle.location));
                    return;
                }
                else
                {
                    var assetName = m_ProvideHandle.location.assetName;
                    var requestedType = m_ProvideHandle.requestedType;
                    if (requestedType.IsArray)
                    {
                        Object[] result = m_AssetBundle.LoadAssetWithSubAssets(assetName, requestedType.GetElementType());
                        GetArrayResult(result);
                    }
                    else if (requestedType.IsGenericType && typeof(IList<>) == requestedType.GetGenericTypeDefinition())
                    {
                        Object[] result = m_AssetBundle.LoadAssetWithSubAssets(assetName, requestedType.GetGenericArguments()[0]);
                        GetListResult(result);
                    }
                    else
                        m_Result = m_AssetBundle.LoadAsset(assetName, m_ProvideHandle.requestedType);
                }

                CompleteOperation();
            }

            private void GetArrayResult(Object[] allAssets)
            {
                m_Result = ResourceManagerConfig.CreateArrayResult(m_ProvideHandle.requestedType, allAssets);
            }

            private void GetListResult(Object[] allAssets)
            {
                m_Result = ResourceManagerConfig.CreateListResult(m_ProvideHandle.requestedType, allAssets);
            }

            private void CompleteOperation()
            {
                Exception e = m_Result == null
                    ? new Exception($"Unable to load asset of type {m_ProvideHandle.requestedType} from location {m_ProvideHandle.location}.")
                    : null;

                m_ProvideHandle.Complete(m_Result, m_Result != null, e);
            }

            /// <summary>
            ///   <para> 从依赖group中获取 assetbundleResouce, 默认为第一个. </para>
            /// </summary>
            /// <param name="results"></param>
            /// <returns></returns>
            internal static IAssetBundleResource LoadBundleFromDependecies(IList<object> results)
            {
                if (results == null || results.Count == 0)
                    return null;

                for (int i = 0; i < results.Count; ++i)
                {
                    if (results[i] is IAssetBundleResource assetBundleRes)
                    {
                        return assetBundleRes;
                    }
                }

                return null;
            }

            public float ProgressCallback() { return 1.0f; }
        }

        public override void Provide(ProvideHandle provideHandle)
        {
            new InternalOp().Start(provideHandle);
        }
    }
}
