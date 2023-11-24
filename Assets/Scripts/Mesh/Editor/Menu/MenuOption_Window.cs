/*---------------------------------------------------------------------------------------
-- 负责人: onemt
-- 创建时间: 2023-11-06 17:03:37
-- 概述:
---------------------------------------------------------------------------------------*/

using UnityEditor;

public static partial class MenuOption
{
    [MenuItem("Tools/Window/ShowExportSpriteMeshWindow", false, 2)]
    static void ShowSpriteMeshWindow()
    {
        ExportSpriteMeshWindow.ShowWindow();
    }
    
    [MenuItem("Tools/Window/ShowMeshCompressionWindow", false, 3)]
    static void ShowMeshCompressionWindow()
    {
        MeshCompressionWindow.ShowWindow();
    }
}