/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-15 15:12:31
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using OnemtEditor.AssetBundle.Build;
using OnemtEditor.AssetBundle.Build.DataBuilders;
using Onemt.AddressableAssets;
using Onemt.Core.Util;
using Onemt.Core.Define;
using Onemt.Framework.Config;

namespace OnemtEditor.AssetBundle.Settings
{
    public partial class AssetBundleAssetSettings
    {
        /// <summary>
        ///   <para> 构建 assetbundle. </para>
        /// </summary>
        public static void BuildPlayerContent()
        {
            BuildPlayerContent(out AddressableAssetBuildResult result);
        }

        public static void BuildPlayerContent(out AddressableAssetBuildResult result)
        {
            var settings = AssetBundleAssetSettingsDefaultObject.settings;
            if (settings == null)
            {
                string error;
                if (EditorApplication.isUpdating)
                    error = "Addressable Asset Settings does not exist.  EditorApplication.isUpdating was true.";
                else if (EditorApplication.isCompiling)
                    error = "Addressable Asset Settings does not exist.  EditorApplication.isCompiling was true.";
                else
                    error = "Addressable Asset Settings does not exist.  Failed to create.";
                Debug.LogError(error);
                result = new AddressableAssetBuildResult();
                result.error = error;
                return;
            }

            // 重置 bundleFileId
            foreach (AssetGroup group in settings.assetGroups)
            {
                if (group == null)
                    continue;
                foreach (AssetEntry entry in group.entries)
                    entry.bundleFileId = null;
            }
            result = settings.BuildPlayerContentImpl();
        }

        internal AddressableAssetBuildResult BuildPlayerContentImpl()
        {
            // 清空构建路径
            if (Directory.Exists(Addressables.buildPath))
            {
                try
                {
                    Directory.Delete(Addressables.buildPath, true);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            Directory.CreateDirectory(Addressables.buildPath);

            var buildContext = new AddressableDataBuildInput(this);

            // TODO 如果需要多种打包模式，此处需要处理
            var result = new BuildScriptPackedMode().BuildData<AddressableAssetBuildResult>(buildContext);
            if (!string.IsNullOrEmpty(result.error))
            {
                Debug.LogError(result.error);
                Debug.LogError($"Addressable content build failure (duration : {TimeSpan.FromSeconds(result.duration).ToString("g")})");
            }
            else
                Debug.Log($"Addressable content successfully built (duration : {TimeSpan.FromSeconds(result.duration).ToString("g")})");
            // AddressableAnalytics.Report(this);
            // if (BuildScript.buildCompleted != null)
            //     BuildScript.buildCompleted(result);
            AssetDatabase.Refresh();
            return result;
        }

        /// <summary>
        ///   <para> 将encrypt 路径下的 assetbundle 拷贝到streamingassets 路径下. </para>
        /// </summary>
        public static void CopyToStreamingAsset()
        {
            string[] files = FileHelper.GetAllChildFiles(Addressables.encryptPath, ConstDefine.kSuffixAssetbundle);
            string streamingDataFolder = Path.Combine(Application.streamingAssetsPath, "data");
            FileHelper.CreateDirectory(streamingDataFolder);
            foreach (var file in files)
            {
                FileHelper.CopyFileTo(file, streamingDataFolder);
            }

            string md5File = Path.Combine(Addressables.encryptPath, ConstDefine.kNameMD5);
            FileHelper.CopyFileTo(md5File, streamingDataFolder);

            AssetDatabase.Refresh();
        }

        /// <summary>
        ///   <para> 清理 streamingassets 路径下的 assetbundle 文件. </para>
        /// </summary>
        public static void CleanStreamingAssets()
        {
            string streamingDataFolder = Path.Combine(Application.streamingAssetsPath, "data");
            //string[] files = FileHelper.GetAllChildFiles(streamingDataFolder, ConstDefine.kSuffixAssetbundle);
            //foreach (var file in files)
            //{
            //    FileHelper.DeleteFile(file);
            //}

            FileHelper.DeleteDirectory(streamingDataFolder);

            AssetDatabase.Refresh();
        }

        /// <summary>
        ///   <para> 清理 assetbundle. </para>
        /// </summary>
        /// <param name="builder"></param>
        public static void CleanPlayerContent(IDataBuilder builder = null)
        {
            var settings = AssetBundleAssetSettingsDefaultObject.settings;
            if (settings == null)
            {
                if (EditorApplication.isUpdating)
                    Debug.LogError("Addressable Asset Settings does not exist.  EditorApplication.isUpdating was true.");
                else if (EditorApplication.isCompiling)
                    Debug.LogError("Addressable Asset Settings does not exist.  EditorApplication.isCompiling was true.");
                else
                    Debug.LogError("Addressable Asset Settings does not exist.  Failed to create.");
                return;
            }
            settings.CleanPlayerContentImpl(builder);
        }

        internal void CleanPlayerContentImpl(IDataBuilder builder = null)
        {
            builder?.ClearCacheData();

            AssetDatabase.Refresh();
        }
    }
}
