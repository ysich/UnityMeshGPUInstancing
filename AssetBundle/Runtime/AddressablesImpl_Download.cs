/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-04-03 17:19:12
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Onemt.Framework.Task;
using Onemt.Framework.Config;
using Onemt.Core.Util;
using System.IO;
using Onemt.ResourceManagement.ResourceLocations;
using Onemt.Core.Define;
using System;

namespace Onemt.AddressableAssets
{
    public partial class AddressablesImpl
    {
        private DownloadAssetbundleFilesTask m_DownloadAssetbundleFileTask;

        private bool m_AutoDownload;
        public bool autoDownload
        {
            get => m_AutoDownload;
            set
            {
                m_AutoDownload = value;
                m_DownloadAssetbundleFileTask.autoDownload = value;
            }
        }

        private ulong m_SizeDownloadTotal;
        public ulong sizeDownloadTotal => m_SizeDownloaded;

        private ulong m_SizeDownloaded;
        public ulong sizeDownloaded => m_SizeDownloaded;

        private void InitDownload()
        {
            if (!GameConfig.instance.enableRemoteRes)
                return;

            m_SizeDownloadTotal = 0;
            m_SizeDownloaded = 0;
            FileHelper.CreateDirectory(GameConfig.instance.persistentTempPath);
            FileHelper.CreateDirectory(GameConfig.instance.persistentBasePath);

            List<ABFileInfo> downloadABFileInfos = new List<ABFileInfo>();

            var abFileInfos = m_ABFileInfos.Values;
            foreach (var abFileInfo in abFileInfos)
            {
                if (!abFileInfo.isRemote)
                    continue;

                string assetbundlePath = Path.Combine(GameConfig.instance.persistentAssetPath, abFileInfo.assetbundleName);
                if (string.IsNullOrEmpty(Md5Helper.CheckFileMd5(assetbundlePath, abFileInfo.md5)))
                {
                    continue;
                }

                downloadABFileInfos.Add(abFileInfo);
            }

            m_DownloadAssetbundleFileTask = new DownloadAssetbundleFilesTask(downloadABFileInfos, 1, OnDownloadComplete, OnProgress);
            m_SizeDownloadTotal = m_DownloadAssetbundleFileTask.sizeDownloadTotal;
        }

        public void OnUpdateDownload()
        {
            m_DownloadAssetbundleFileTask?.Update();
        }

        public void Add(ResourceLocationBase location, DownloadPriority priority, Action<ErrorCode, string, byte[]> callback)
        {
            if (m_ABFileInfos.TryGetValue(location.assetbundleName, out var abFileInfo))
                m_DownloadAssetbundleFileTask.Add(abFileInfo, priority, callback);
            else
                UnityEngine.Debug.LogErrorFormat("AddDownloadABssetFile Error with abName: {0}", location.assetbundleName);
        }

        private void OnDownloadComplete(ErrorCode errorCode, string errorMessage, byte[] bytes)
        { }

        private void OnProgress(float progress, ulong downloadedLength, ulong totalLength, int receiveLength)
        {
            m_SizeDownloaded = downloadedLength;
        }
    }
}
