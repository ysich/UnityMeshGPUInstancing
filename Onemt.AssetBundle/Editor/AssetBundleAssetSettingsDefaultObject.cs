/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 14:44:54
-- 概述:
---------------------------------------------------------------------------------------*/

using UnityEngine;
using UnityEngine.Serialization;
using UnityEditor;
using OnemtEditor.AssetBundle.Settings;

namespace OnemtEditor.AssetBundle
{
    public class AssetBundleAssetSettingsDefaultObject : ScriptableObject
    {
        public const string kDefaultConfigAssetName = "AssetBundleAssetSettings";
        public const string kDefaultConfigFolder = "Assets/AssetBundleAssetData";
        public const string kDefaultConfigObjectName = "com.unity.assetbundleassets";

        [FormerlySerializedAs("m_AssetBundleAssetSettingsGUID")]
        [SerializeField]
        internal string m_AssetBundleAssetSettingsGUID;
        public string assetBundleAssetSettingsGUID { get => m_AssetBundleAssetSettingsGUID; }

        static AssetBundleAssetSettings s_DefaultSettingsObject;

        bool m_LoadingSettingsObject = false;

        public static string defaultAssetPath
        {
            get
            {
                return kDefaultConfigFolder + "/" + kDefaultConfigAssetName + ".asset";
            }
        }

        internal AssetBundleAssetSettings LoadSettingsObject()
        {
            if (m_LoadingSettingsObject)
            {
                UnityEngine.Debug.LogWarning("Detected stack overflow when accessing AssetBundleAssetSettingsDefaultObject.settings object.");
                return null;
            }

            if (string.IsNullOrEmpty(m_AssetBundleAssetSettingsGUID))
            {
                UnityEngine.Debug.LogError("Invalid guid for default AssetBundleAssetSettings object.");
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(m_AssetBundleAssetSettingsGUID);
            if (string.IsNullOrEmpty(path))
            {
                UnityEngine.Debug.LogErrorFormat("Unable to determine path for default AssetBundleAssetSettings object with guid {0}.", m_AssetBundleAssetSettingsGUID);
                return null;
            }

            m_LoadingSettingsObject = true;
            var settings = AssetDatabase.LoadAssetAtPath<AssetBundleAssetSettings>(path);
            m_LoadingSettingsObject = false;
            return settings;
        }

        void SetSettingsObject(AssetBundleAssetSettings settings)
        {
            if (settings == null)
            {
                m_AssetBundleAssetSettingsGUID = null;
                return;
            }

            var path = AssetDatabase.GetAssetPath(settings);
            if (string.IsNullOrEmpty(path))
            {
                UnityEngine.Debug.LogError("SetSettingsObject Error With Empty Path.");
                return;
            }

            m_AssetBundleAssetSettingsGUID = AssetDatabase.AssetPathToGUID(path);
        }

        public static bool settingsExists
        {
            get
            {
                if (EditorBuildSettings.TryGetConfigObject(kDefaultConfigObjectName, out AssetBundleAssetSettingsDefaultObject obj))
                    return !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(obj.assetBundleAssetSettingsGUID));

                return false;
            }
        }

        public static AssetBundleAssetSettings settings
        {
            get
            {
                if (s_DefaultSettingsObject == null)
                {
                    if (EditorBuildSettings.TryGetConfigObject(kDefaultConfigObjectName, out AssetBundleAssetSettingsDefaultObject obj))
                    {
                        s_DefaultSettingsObject = obj.LoadSettingsObject();
                    }
                    else
                    {
                        if (EditorBuildSettings.TryGetConfigObject(kDefaultConfigAssetName, out s_DefaultSettingsObject))
                        {
                            EditorBuildSettings.RemoveConfigObject(kDefaultConfigAssetName);
                            obj = CreateInstance<AssetBundleAssetSettingsDefaultObject>();
                            obj.SetSettingsObject(s_DefaultSettingsObject);
                            AssetDatabase.CreateAsset(obj, kDefaultConfigFolder + "/DefaultObject.asset");
                            EditorUtility.SetDirty(obj);
                            AssetDatabase.SaveAssets();
                            EditorBuildSettings.AddConfigObject(kDefaultConfigObjectName, obj, true);
                        }
                    }
                }

                return s_DefaultSettingsObject;
            }
            set
            {
                if (value != null)
                {
                    var path = AssetDatabase.GetAssetPath(value);
                    if (string.IsNullOrEmpty(path))
                    {
                        Debug.LogErrorFormat("AssetBundleAssetSettings object must be saved to an asset before it can be set as the default.");
                        return;
                    }
                }

                s_DefaultSettingsObject = value;
                if (!EditorBuildSettings.TryGetConfigObject(kDefaultConfigObjectName, out AssetBundleAssetSettingsDefaultObject obj))
                {
                    obj = CreateInstance<AssetBundleAssetSettingsDefaultObject>();
                    AssetDatabase.CreateAsset(obj, kDefaultConfigFolder + "/DefaultObject.asset");
                    AssetDatabase.SaveAssets();
                    EditorBuildSettings.AddConfigObject(kDefaultConfigObjectName, obj, true);
                }
                obj.SetSettingsObject(s_DefaultSettingsObject);
                EditorUtility.SetDirty(obj);
                AssetDatabase.SaveAssets();
            }
        }

        public static AssetBundleAssetSettings GetSettings(bool create)
        {
            if (settings == null && create)
                settings = AssetBundleAssetSettings.Create(kDefaultConfigFolder, kDefaultConfigAssetName, true, true);

            return settings;
        }
    }
}
