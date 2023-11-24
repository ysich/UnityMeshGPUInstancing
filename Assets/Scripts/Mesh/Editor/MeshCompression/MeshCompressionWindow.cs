/*---------------------------------------------------------------------------------------
-- 负责人: onemt
-- 创建时间: 2023-11-17 15:35:49
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using UnityEditor;
using UnityEngine;

public class MeshCompressionWindow:EditorWindow
{
    public static void ShowWindow()
    {
        MeshCompressionWindow window = GetWindow<MeshCompressionWindow>();
        window.titleContent.text = "MeshCompressionWindow";
        window.minSize = new Vector2(300, 300);
        window.Show();
    }

    private Mesh m_Mesh;
    
    private void OnGUI()
    {
        m_Mesh = (Mesh)EditorGUILayout.ObjectField("导入压缩Mesh", m_Mesh, typeof(Mesh));
        if (GUILayout.Button("压缩"))
        {
            if (m_Mesh == null)
            {
                throw new Exception("mesh为空");
            }
            MeshCompressionHelper.MeshCompression(m_Mesh);
        }
    }
}