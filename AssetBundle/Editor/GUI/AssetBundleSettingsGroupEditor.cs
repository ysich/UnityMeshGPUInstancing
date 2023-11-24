/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 15:45:33
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Serialization;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using OnemtEditor.AssetBundle.Settings;
using OnemtEditor.AssetBundle.Build.DataBuilders;
using UnityEditor.Build.Pipeline.Utilities;

namespace OnemtEditor.AssetBundle.GUI
{
    [Serializable]
    public class AssetBundleSettingsGroupEditor
    {
        [FormerlySerializedAs("mchs")]
        [SerializeField]
        MultiColumnHeaderState m_Mchs;

        private AssetBundleAssetSettings m_Settings;
        internal AssetBundleAssetSettings settings
        {
            get
            {
                if (m_Settings == null)
                {
                    m_Settings = AssetBundleAssetSettingsDefaultObject.settings;
                }

                return m_Settings;
            }

            set { m_Settings = value; }
        }

        private AssetBundleAssetsWindow m_AssetsWindow;
        private AssetBundleAssetEntryTreeView m_EntryTreeView;

        private TreeViewState m_TreeState;

        private SearchField m_SearchField;
        const int k_SearchHeight = 20;

        bool m_ResizingVerticalSplitter;
        Rect m_VerticalSplitterRect = new Rect(0, 0, 10, k_SplitterWidth);
        [SerializeField]
        float m_VerticalSplitterPercent;
        const int k_SplitterWidth = 3;

        [NonSerialized]
        List<GUIStyle> m_SearchStyles;

        public AssetBundleSettingsGroupEditor(AssetBundleAssetsWindow window)
        {
            m_AssetsWindow = window;

            OnEnable();
        }

        public bool OnGUI(Rect pos)
        {
            if (settings == null)
                return false;

            if (!m_ModificationRegistered)
            {
                m_ModificationRegistered = true;
                settings.OnModification -= OnSettingsModification; //just in case...
                settings.OnModification += OnSettingsModification;
            }

            if (m_EntryTreeView == null)
                InitEntryTreeView();

            var inRectY = pos.height;
            var searchRect = new Rect(pos.xMin, pos.yMin, pos.width, k_SearchHeight);
            var treeRect = new Rect(pos.xMin, pos.yMin + k_SearchHeight, pos.width, inRectY - k_SearchHeight);

            TopToolbar(searchRect);
            m_EntryTreeView.OnGUI(treeRect);
            return m_ResizingVerticalSplitter;
        }

        private AssetBundleAssetEntryTreeView InitEntryTreeView()
        {
            if (m_TreeState == null)
                m_TreeState = new TreeViewState();

            var headerState = AssetBundleAssetEntryTreeView.CreateDefaultMultiColumnHeaderState();
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_Mchs, headerState))
                MultiColumnHeaderState.OverwriteSerializedFields(m_Mchs, headerState);
            m_Mchs = headerState;

            m_SearchField = new SearchField();
            m_EntryTreeView = new AssetBundleAssetEntryTreeView(m_TreeState, m_Mchs, this);
            m_EntryTreeView.Reload();
            return m_EntryTreeView;
        }

        private void TopToolbar(Rect toolbarPos)
        {
            if (m_SearchStyles == null)
            {
                m_SearchStyles = new List<GUIStyle>();
                m_SearchStyles.Add(GetStyle("ToolbarSeachTextFieldPopup")); //GetStyle("ToolbarSeachTextField");
                m_SearchStyles.Add(GetStyle("ToolbarSeachCancelButton"));
                m_SearchStyles.Add(GetStyle("ToolbarSeachCancelButtonEmpty"));
            }

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.FlexibleSpace();

                {
                    var guiBuild = new GUIContent("Build");
                    Rect rBuild = GUILayoutUtility.GetRect(guiBuild, EditorStyles.toolbarDropDown);
                    if (EditorGUI.DropdownButton(rBuild, guiBuild, FocusType.Passive, EditorStyles.toolbarDropDown))
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("New Build"), false, OnBuildScript);
                        menu.AddItem(new GUIContent("Copy To StreamingAssets"), false, OnCopyToStreamingAssets);
                        menu.AddItem(new GUIContent("Clean StreamingAssets"), false, OnCleanStreamingAssets);
                        menu.AddItem(new GUIContent("Clean Build"), false, OnCleanAll);
                        menu.DropDown(rBuild);
                    }
                }

                GUILayout.Space(4);
                Rect searchRect = GUILayoutUtility.GetRect(0, toolbarPos.width * 0.6f, 16f, 16f, m_SearchStyles[0], GUILayout.MinWidth(65), GUILayout.MaxWidth(300));
                Rect popupPosition = searchRect;
                popupPosition.width = 20;

                if (Event.current.type == EventType.MouseDown && popupPosition.Contains(Event.current.mousePosition))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Hierarchical Search"), true, OnHierSearchClick);
                    menu.DropDown(popupPosition);
                }
                else
                {
                    var baseSearch = m_EntryTreeView.customSearchString;
                    var searchString = m_SearchField.OnGUI(searchRect, baseSearch, m_SearchStyles[0], m_SearchStyles[1], m_SearchStyles[2]);
                    if (baseSearch != searchString)
                    {
                        m_EntryTreeView?.Search(searchString);
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        void OnBuildScript()
        {
            OnCleanAddressables(null);

            AssetBundleAssetSettings.BuildPlayerContent();
        }

        void OnCopyToStreamingAssets()
        {
            AssetBundleAssetSettings.CopyToStreamingAsset();
        }

        void OnCleanStreamingAssets()
        {
            AssetBundleAssetSettings.CleanStreamingAssets();
        }

        void OnCleanAll()
        {
            OnCleanAddressables(null);
            OnCleanSBP();
        }

        void OnCleanAddressables(object builder)
        {
            AssetBundleAssetSettings.CleanPlayerContent(new BuildScriptPackedMode());
        }

        void OnCleanSBP()
        {
            BuildCache.PurgeCache(true);
        }

        public void Reload()
        {
            m_EntryTreeView?.Reload();
        }

        void HandleVerticalResize(Rect position)
        {
            m_VerticalSplitterRect.y = (int)(position.yMin + position.height * m_VerticalSplitterPercent);
            m_VerticalSplitterRect.width = position.width;
            m_VerticalSplitterRect.height = k_SplitterWidth;


            EditorGUIUtility.AddCursorRect(m_VerticalSplitterRect, MouseCursor.ResizeVertical);
            if (Event.current.type == EventType.MouseDown && m_VerticalSplitterRect.Contains(Event.current.mousePosition))
                m_ResizingVerticalSplitter = true;

            if (m_ResizingVerticalSplitter)
            {
                var mousePosInRect = Event.current.mousePosition.y - position.yMin;
                m_VerticalSplitterPercent = Mathf.Clamp(mousePosInRect / position.height, 0.20f, 0.90f);
                m_VerticalSplitterRect.y = (int)(position.height * m_VerticalSplitterPercent + position.yMin);

                if (Event.current.type == EventType.MouseUp)
                {
                    m_ResizingVerticalSplitter = false;
                }
            }
            else
                m_VerticalSplitterPercent = Mathf.Clamp(m_VerticalSplitterPercent, 0.20f, 0.90f);
        }

        void OnHierSearchClick()
        {
            m_EntryTreeView.SwapSearchType();
            m_EntryTreeView.Reload();
            m_EntryTreeView.Repaint();
        }

        GUIStyle GetStyle(string styleName)
        {
            GUIStyle s = UnityEngine.GUI.skin.FindStyle(styleName);
            if (s == null)
                s = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(styleName);
            if (s == null)
            {
                UnityEngine.Debug.LogError("Missing built-in guistyle " + styleName);
                s = new GUIStyle();
            }
            return s;
        }

        bool m_ModificationRegistered;
        public void OnEnable()
        {
            if (AssetBundleAssetSettingsDefaultObject.settings == null)
                return;
            if (!m_ModificationRegistered)
            {
                AssetBundleAssetSettingsDefaultObject.settings.OnModification += OnSettingsModification;
                m_ModificationRegistered = true;

                //UnityEngine.Debug.LogError(" Group Editor                 OnEnable");
            }
        }

        public void OnDisable()
        {
            if (AssetBundleAssetSettingsDefaultObject.settings == null)
                return;
            if (m_ModificationRegistered)
            {
                AssetBundleAssetSettingsDefaultObject.settings.OnModification -= OnSettingsModification;
                m_ModificationRegistered = false;

                //UnityEngine.Debug.LogError(" Group Editor                 OnDisable");
            }
        }

        void OnSettingsModification(AssetBundleAssetSettings s, ModificationEvent e, object o)
        {
            if (m_EntryTreeView == null)
                return;

            switch (e)
            {
                case ModificationEvent.GroupAdded:
                case ModificationEvent.GroupRemoved:
                case ModificationEvent.EntryAdded:
                case ModificationEvent.EntryMoved:
                case ModificationEvent.EntryRemoved:
                case ModificationEvent.GroupRenamed:
                case ModificationEvent.EntryModified:
                case ModificationEvent.BatchModification:
                    m_EntryTreeView.Reload();
                    if (m_AssetsWindow != null)
                        m_AssetsWindow.Repaint();
                    break;
            }
        }

    }
}
