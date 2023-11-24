/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 14:06:42
-- 概述:
        asset 应用导入规则进行后处理， 
---------------------------------------------------------------------------------------*/

using System.Linq;
using UnityEditor;
using UnityEngine;
using OnemtEditor.AssetBundle.Settings;

namespace OnemtEditor.AssetBundle.Import
{
    public class AssetBundleImporter : AssetPostprocessor
    {
        private static bool s_IgnorePostprocess;
        public static bool ignorePostprocess { get => s_IgnorePostprocess; set => s_IgnorePostprocess = value; }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (s_IgnorePostprocess)
                return;

            // 忽略 Assets/AssetBundleAssetData 文件夹 
            var isConfigurationPass =
            (importedAssets.Length > 0 && importedAssets.All(x => x.StartsWith("Assets/AssetBundleAssetData"))) &&
            (deletedAssets.Length > 0 && deletedAssets.All(x => x.StartsWith("Assets/AssetBundleAssetData")));
            if (isConfigurationPass)
                return;

            var settings = AssetBundleAssetSettingsDefaultObject.settings;
            if (settings == null)
            {
                Debug.LogWarningFormat("AssetBundleAssetSettings File is not found.");
                return;
            }

            var importSettings = AssetBundleImportSettings.instance;
            if (importSettings == null)
            {
                UnityEngine.Debug.LogError("AssetBundleImportSettings file not found.");
                return;
            }

            if (importSettings.rules == null || importSettings.rules.Count == 0)
                return;

            if (!CheckNullGroups(settings))
            {
                UnityEngine.Debug.LogError("AssetBundleAssetSettings has null group, please reimport AssetGroups folder.");
                return;
            }

            var dirty = false;
            // 获取当前打开的 prefab
            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();

            // 新导入 asset 处理
            foreach (var importedAsset in importedAssets)
            {
                if (prefabStage == null || prefabStage.assetPath != importedAsset)
                    dirty |= ApplyImportRule(importedAsset, null, settings, importSettings);
            }

            // asset 修改路径处理
            for (int i = 0; i < movedAssets.Length; ++i)
            {
                var movedAsset = movedAssets[i];
                var movedFromAssetPath = movedFromAssetPaths[i];
                if (prefabStage == null || prefabStage.assetPath != movedAsset) // Ignore current editing prefab asset.
                    dirty |= ApplyImportRule(movedAsset, movedFromAssetPath, settings, importSettings);
            }

            // 移除 asset 处理
            foreach (var deletedAsset in deletedAssets)
            {
                if (TryGetMatchedRule(deletedAsset, importSettings, out var matchedRule))
                {
                    var guid = AssetDatabase.AssetPathToGUID(deletedAsset);
                    if (!string.IsNullOrEmpty(guid) && settings.RemoveAssetEntry(guid))
                    {
                        dirty = true;
                    }
                }
            }

            if (dirty)
            {
                AssetDatabase.SaveAssets();
            }
        }

        static bool ApplyImportRule(
            string assetPath,
            string movedFromAssetPath,
            AssetBundleAssetSettings settings,
            AssetBundleImportSettings importSettings)
        {
            var dirty = false;
            if (TryGetMatchedRule(assetPath, importSettings, out var matchedRule))
            {
                // 路径匹配规则: 1. 创建assetentry 2. 移动assetentry 分组
                CreateOrUpdateAddressableAssetEntry(settings, importSettings, matchedRule, assetPath);

                dirty = true;
            }
            else
            {
                // 如果某个asset路径改变且原路径有匹配规则 新路径未匹配到导入规则，则需要删除相对应的 assetentry.
                if (!string.IsNullOrEmpty(movedFromAssetPath) && TryGetMatchedRule(movedFromAssetPath, importSettings, out matchedRule))
                {
                    var guid = AssetDatabase.AssetPathToGUID(assetPath);
                    if (settings.RemoveAssetEntry(guid))
                    {
                        dirty = true;
                        Debug.LogFormat("[AddressableImporter] Entry removed for {0}", assetPath);
                    }
                }
            }

            return dirty;
        }

        static bool TryGetMatchedRule(
            string assetPath,
            AssetBundleImportSettings importSettings,
            out AssetBundleImportRule rule)
        {
            foreach (var r in importSettings.rules)
            {
                if (!r.Match(assetPath))
                    continue;
                rule = r;
                return true;
            }

            rule = null;
            return false;
        }

        static AssetEntry CreateOrUpdateAddressableAssetEntry(
            AssetBundleAssetSettings settings,
            AssetBundleImportSettings importSettings,
            AssetBundleImportRule rule,
            string assetPath)
        {
            var groupName = rule.ParseGroupReplacement(assetPath);
            if (!TryGetGroup(settings, groupName, out var assetGroup))
            {
                //TODO Specify on editor which type to create.
                assetGroup = CreateAssetGroup(settings, groupName);
                //settings.AddAssetGroup(assetGroup);
            }

            assetGroup.packingMode = rule.packingMode;
            assetGroup.buildCompressionMode = rule.compressionMode;

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.CreateOrMoveEntry(guid, assetGroup);

            if (entry != null)
            {
                //fix asset path error
                if (entry.assetPath != assetPath)
                {
                    entry.SetCachedPath(assetPath);
                }
            }
            return entry;
        }

        static AssetGroup CreateAssetGroup(AssetBundleAssetSettings settings, string groupName)
        {
            return settings.CreateGroup(groupName, false, false);
        }

        static bool TryGetGroup(AssetBundleAssetSettings settings, string groupName, out AssetGroup group)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                group = settings.defaultGroup;
                return true;
            }

            return ((group = settings.assetGroups.Find(g => string.Equals(g.name, groupName.Trim()))) == null) ? false : true;
        }

        static bool CheckNullGroups(AssetBundleAssetSettings settings)
        {
            foreach (var group in settings.assetGroups)
            {
                if (group == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
