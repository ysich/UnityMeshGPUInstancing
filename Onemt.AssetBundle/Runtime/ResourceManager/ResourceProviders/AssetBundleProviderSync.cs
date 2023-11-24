/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 11:06:53
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using UnityEngine;
using Onemt.ResourceManagement.ResourceLocations;
using Onemt.Core.Define;
using System.IO;
using Onemt.AddressableAssets;

namespace Onemt.ResourceManagement.ResourceProviders
{
    internal class AssetBundleResourceSync : IAssetBundleResource//, IUpdateReceiver
    {
        private AssetBundle m_AssetBundle;
        internal ProvideHandle m_ProvideHandle;
        //private bool m_Completed = false;

        private string m_AssetbundlePath;

        public AssetBundle GetAssetBundle()
        {
            return m_AssetBundle;
        }

        internal void Start(ProvideHandle provideHandle)
        {
            m_AssetBundle = null;
            m_ProvideHandle = provideHandle;

            BeginOperation();
        }

        private void BeginOperation()
        {
            // 远程资源下载
            m_AssetbundlePath = m_ProvideHandle.resourceManager.TransformInternalId(m_ProvideHandle.location);
            if (!File.Exists(m_AssetbundlePath))
            {
                UnityEngine.Debug.LogErrorFormat("AssetBundleProviderSync AssetbundlePath not exist:  {0}", m_AssetbundlePath);
                Addressables.Add(m_ProvideHandle.location as ResourceLocationBase, DownloadPriority.High, DownloadAssetbundleComplete);
            }
            else
                LoadAssetbundle();
        }

        private void DownloadAssetbundleComplete(ErrorCode errorCode, string errorMessage, byte[] bytes)
        {
            if (errorCode == ErrorCode.Success)
                LoadAssetbundle();
            else
                CompleteBundleLoad(null);
        }

        private void LoadAssetbundle()
        {
            // 获取路径
            var path = m_ProvideHandle.resourceManager.TransformInternalId(m_ProvideHandle.location);
            var assetbundle = AssetBundle.LoadFromFile(path, 0, ConstDefine.kAssetbundleOffset);
            CompleteBundleLoad(assetbundle);
        }

        void AddCallbackInvokeIfDone(UnityEngine.AsyncOperation operation, Action<UnityEngine.AsyncOperation> callback)
        {
            if (operation.isDone)
                callback?.Invoke(operation);
            else
                operation.completed += callback;
        }

        private void RequestCompleted(UnityEngine.AsyncOperation op)
        {
            CompleteBundleLoad((op as AssetBundleCreateRequest).assetBundle);
        }

        private void CompleteBundleLoad(AssetBundle assetBundle)
        {
            m_AssetBundle = assetBundle;
            if (m_AssetBundle != null)
                m_ProvideHandle.Complete(this, true, null);
            else
                m_ProvideHandle.Complete<AssetBundleResource>(null, false,
                    new Exception(string.Format("Invalid path in AssetBundleProvider: '{0}'.", m_ProvideHandle.location.assetbundleName)));

            //m_Completed = true;
        }

        public AssetBundleRequest GetAssetPreloadRequest()
        {
            return null;
        }

        public void Unload()
        {
            if (m_AssetBundle != null)
            {
                //UnityEngine.Debug.LogErrorFormat("Unload Assetbundle    : {0}", m_AssetBundle.name);
                m_AssetBundle.Unload(true);
                m_AssetBundle = null;
            }
        }
    }

    public class AssetBundleProviderSync : ResourceProviderBase
    {
        public override void Provide(ProvideHandle provideHandle)
        {
            new AssetBundleResourceSync().Start(provideHandle);
        }

        public override void Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new ArgumentException("location");

            if (asset == null)
            {
                UnityEngine.Debug.LogErrorFormat("[AssetBundleProvider.Release] location : {0}  Error with empty asset.");
                return;
            }

            if (asset is AssetBundleResourceSync assetBundleResource)
            {
                assetBundleResource.Unload();
                return;
            }
        }
    }
}
