/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 14:07:23
-- 概述:
        AssetBundle 导入设置配置
---------------------------------------------------------------------------------------*/

using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OnemtEditor.AssetBundle.Import
{
    [CreateAssetMenu(fileName = "AssetBundleImportSettings", menuName = "AssetBundle Assets/Import Settings", order = 50)]
    public class AssetBundleImportSettings : ScriptableObject
    {
        public const string kDefaultConfigObjectName = "assetbundleimportsettings";
        public const string kDefaultPath = "Assets/AssetBundleAssetData/AssetBundleImportSettings.asset";

        public List<AssetBundleImportRule> rules;

        [ButtonMethod]
        private void Save()
        {
            AssetDatabase.SaveAssets();
        }

        [ButtonMethod]
        private void CleanEmptyGroup()
        {
            var settings = AssetBundleAssetSettingsDefaultObject.settings;
            if (settings == null)
            {
                return;
            }

            var dirty = false;
            var emptyGroups = settings.assetGroups.Where(x => x.entries.Count == 0 && !x.isDefault).ToArray();
            for (int i = 0; i < emptyGroups.Length; ++i)
            {
                dirty = true;
                settings.RemoveGroup(emptyGroups[i]);
            }

            if (dirty)
                AssetDatabase.SaveAssets();
        }

        public static AssetBundleImportSettings instance
        {
            get
            {
                AssetBundleImportSettings settings;
                if (EditorBuildSettings.TryGetConfigObject(kDefaultConfigObjectName, out settings))
                    return settings;
                // 由菜单中进行创建，暂时注释代码 
                //else
                //{
                //    settings = CreateInstance<AssetBundleImportSettings>();
                //    AssetDatabase.CreateAsset(settings, kDefaultPath);
                //    AssetDatabase.SaveAssets();
                //    EditorBuildSettings.AddConfigObject(kDefaultConfigObjectName, settings, true);
                //}

                settings = AssetDatabase.LoadAssetAtPath<AssetBundleImportSettings>(kDefaultPath);
                if (settings != null)
                    EditorBuildSettings.AddConfigObject(kDefaultConfigObjectName, settings, true);
                return settings;
            }
        }
    }
}
