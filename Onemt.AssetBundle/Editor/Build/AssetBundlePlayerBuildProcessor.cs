/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 15:47:15
-- 概述:
        构建assetbundle，post 处理
---------------------------------------------------------------------------------------*/

using UnityEngine;
using UnityEditor.Build;
using UnityEditor;
using System.IO;
using UnityEditor.Build.Reporting;
using Onemt.AddressableAssets;

namespace OnemtEditor.AssetBundle.Build
{
    public class AssetBundlePlayerBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 1;

        public void OnPostprocessBuild(BuildReport report)
        {
            CleanTemporaryPlayerBuildData();
        }

        [InitializeOnLoadMethod]
        internal static void CleanTemporaryPlayerBuildData()
        {
            if (Directory.Exists(Addressables.playerBuildDataPath))
            {
                DirectoryUtility.DirectoryMove(Addressables.playerBuildDataPath, Addressables.buildPath);
                DirectoryUtility.DeleteDirectory(Application.streamingAssetsPath, onlyIfEmpty: true);
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            CopyTemporaryPlayerBuildData();
        }

        internal static void CopyTemporaryPlayerBuildData()
        {
            if (Directory.Exists(Addressables.buildPath))
            {
                if (Directory.Exists(Addressables.playerBuildDataPath))
                {
                    Debug.LogWarning($"Found and deleting directory \"{Addressables.playerBuildDataPath}\", directory is managed through Addressables.");
                    DirectoryUtility.DeleteDirectory(Addressables.playerBuildDataPath, false);
                }

                string parentDir = Path.GetDirectoryName(Addressables.playerBuildDataPath);
                if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                    Directory.CreateDirectory(parentDir);
                Directory.Move(Addressables.buildPath, Addressables.playerBuildDataPath);
            }
        }
    }
}
