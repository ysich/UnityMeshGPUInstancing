/*---------------------------------------------------------------------------------------
-- 负责人: onemt
-- 创建时间: 2023-11-06 16:57:56
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ExportSpriteMeshWindow:EditorWindow
{
    public static void ShowWindow()
    {
        ExportSpriteMeshWindow window = GetWindow<ExportSpriteMeshWindow>();
        window.titleContent.text = "SpriteMeshWindow";
        window.minSize = new Vector2(500, 650);
        window.Show();
    }

    private string m_AtlasIconPath;
    // private bool m_IsCompression;
    private const string k_spriteMeshPath = "Assets/BundleAssets/Mesh/SpriteMesh/";
    private void OnGUI()
    {
        DragAndDropObj();
        DrawView();
    }

    void DrawView()
    {
        m_AtlasIconPath = EditorGUILayout.TextField("需要生成的路径", m_AtlasIconPath);
        GUILayout.Space(10);
        // m_IsCompression = GUILayout.Toggle(m_IsCompression, "是否压缩");
        GUILayout.Space(10);
        GUI.color = Color.green;
        if (GUILayout.Button("生成",GUILayout.Height(50)))
        {
            if (string.IsNullOrWhiteSpace(m_AtlasIconPath))
            {
                return;
            }

            if (!m_AtlasIconPath.Contains(ExportSpriteMeshHelper.kAnimationAtlasIconPath) && Directory.Exists(m_AtlasIconPath))
            {
                throw new Exception("ExportSpriteMeshWindow:路径不符合！");
            }

            ExportSpriteMeshHelper.ExportSpriteMesh(m_AtlasIconPath);
        }
    }
    void DragAndDropObj()
    {
        if (mouseOverWindow == this)
        {
            if (Event.current.type == EventType.DragUpdated)
            {
                //改变鼠标的外表
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
            }
            if (Event.current.type == EventType.DragExited)
            {
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    m_AtlasIconPath = DragAndDrop.paths[0];
                }
            }
        }
    }
}