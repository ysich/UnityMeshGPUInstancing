/*---------------------------------------------------------------------------------------
-- 负责人: onemt
-- 创建时间: 2023-11-17 15:24:01
-- 概述:
---------------------------------------------------------------------------------------*/

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MeshCompressionHelper
{
    public static void MeshCompression(Mesh mesh)
    {
        var layout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float16,2),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0,VertexAttributeFormat.Float16,2),
        };
        var vts = mesh.vertices;
        mesh.SetVertexBufferParams(vts.Length,layout);
        AssetDatabase.Refresh();
    }
}