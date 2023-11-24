/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 15:45:17
-- 概述:
---------------------------------------------------------------------------------------*/

using UnityEngine;
using UnityEditor;
using UnityEngine.Serialization;
using OnemtEditor.AssetBundle.Settings;

namespace OnemtEditor.AssetBundle.GUI
{
    public class AssetBundleAssetsWindow : EditorWindow, IHasCustomMenu
    {
        [FormerlySerializedAs("m_GoupEditor")]
        [SerializeField]
        private AssetBundleSettingsGroupEditor m_GoupEditor;

        [MenuItem("Window/AssetBundle Management/AssetBundle/Group")]
        internal static void Init()
        {
            var window = GetWindow<AssetBundleAssetsWindow>();
            window.titleContent = new GUIContent("AssetBundle Groups");
            window.Show();
        }

        public void OnEnable()
        {
            //UnityEngine.Debug.LogError("AssetBundleAssetsWindow     OnEnable");
            m_GoupEditor?.OnEnable();
        }

        public void OnDisable()
        {
            m_GoupEditor?.OnDisable();
        }

        public void OnGUI()
        {
            if (AssetBundleAssetSettingsDefaultObject.settings == null)
            {
                GUILayout.Space(50);
                if (GUILayout.Button("Create AssetBundle Settings"))
                {
                    m_GoupEditor = null;
                    var folder = AssetBundleAssetSettingsDefaultObject.kDefaultConfigFolder;
                    var fileName = AssetBundleAssetSettingsDefaultObject.kDefaultConfigAssetName;
                    AssetBundleAssetSettingsDefaultObject.settings = AssetBundleAssetSettings.Create(folder, fileName, true, true);
                }
            }
            else
            {
                Rect contentRect = new Rect(0, 0, position.width, position.height);

                if (m_GoupEditor == null)
                    m_GoupEditor = new AssetBundleSettingsGroupEditor(this);

                if (m_GoupEditor.OnGUI(contentRect))
                { }
            }
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
        }
    }
}
