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
    public class BundleAssetProvider : ResourceProviderBase
    {
        internal class InternalOp
        {
            private AssetBundle m_AssetBundle;
            private AssetBundleRequest m_AssetBundleRequest;
            private ProvideHandle m_ProvideHandle;
            private object m_Result;

            public void Start(ProvideHandle provideHandle)
            {
                m_ProvideHandle = provideHandle;
                m_AssetBundleRequest = null;

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
                        m_AssetBundleRequest = m_AssetBundle.LoadAssetWithSubAssetsAsync(assetName, requestedType.GetElementType());
                    else if (requestedType.IsGenericType && typeof(IList<>) == requestedType.GetGenericTypeDefinition())
                        m_AssetBundleRequest = m_AssetBundle.LoadAssetWithSubAssetsAsync(assetName, requestedType.GetGenericArguments()[0]);
                    else
                        m_AssetBundleRequest = m_AssetBundle.LoadAssetAsync(assetName, m_ProvideHandle.requestedType);
                }

                if (m_AssetBundleRequest != null)
                {
                    if (m_AssetBundleRequest.isDone)
                        ActionComplete(m_AssetBundleRequest);
                    else
                        m_AssetBundleRequest.completed += ActionComplete;
                }
            }

            private void ActionComplete(UnityEngine.AsyncOperation asyncOp)
            {
                if (asyncOp != null)
                {
                    if (m_ProvideHandle.requestedType.IsArray)
                        GetArrayResult(m_AssetBundleRequest.allAssets);
                    else if (m_ProvideHandle.requestedType.IsGenericType && typeof(IList<>) == m_ProvideHandle.requestedType.GetGenericTypeDefinition())
                        GetListResult(m_AssetBundleRequest.allAssets);
                    else
                        m_Result = m_AssetBundleRequest.asset;
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

                for (int i = 0; i < results.Count; ++ i)
                {
                    if (results[i] is IAssetBundleResource assetBundleRes)
                    {
                        return assetBundleRes;
                    }
                }

                return null;
            }

            public float ProgressCallback() { return m_AssetBundleRequest != null ? m_AssetBundleRequest.progress : 0.0f; }
        }

        public override void Provide(ProvideHandle provideHandle)
        {
            new InternalOp().Start(provideHandle);
        }
    }
}
