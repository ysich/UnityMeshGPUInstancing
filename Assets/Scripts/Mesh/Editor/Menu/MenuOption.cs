using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

public static partial class MenuOption
{
    [MenuItem("Assets/Create/CreateMeshConfigData",false,0)]
    public static void CreateMeshConfigData()
    {
        SpriteMeshConfigData meshConfigData = ScriptableObject.CreateInstance<SpriteMeshConfigData>();
        ProjectWindowUtil.CreateAsset(meshConfigData,"MeshConfigData.asset");
    }

    [MenuItem("Assets/Create/CreateNormalMesh",false,1)]
    public static void CreatNormalMesh()
    {
        UnityEngine.Mesh mesh = new UnityEngine.Mesh();
        //三角面数据
        List<Vector3> verts = new List<Vector3>();
        //顶点数据
        List<int> indices = new List<int>();

        AddMeshData2();
        mesh.SetVertices(verts);
        mesh.SetIndices(indices,MeshTopology.Triangles,0);
        //计算法线
        mesh.RecalculateNormals();
        //计算物体的整体边界
        mesh.RecalculateBounds();
        
        // ExportSpriteMeshHelper.SaveMeshForSprite(mesh, "Mesh");
        
        void AddMeshData()
        {
            verts.Add(new Vector3(0, 0, 0));
            verts.Add(new Vector3(0, 0, 1));
            verts.Add(new Vector3(1, 0, 1));
            indices.Add(0); indices.Add(1); indices.Add(2);
        }
        
        void AddMeshData2()
        {
            verts.Add(new Vector3(0, 0, 0));//0
            verts.Add(new Vector3(0, 1, 0));//1
            verts.Add(new Vector3(1, 1, 0));//2
            verts.Add(new Vector3(1, 0, 0));//3

            verts.Add(new Vector3(0, 1, 0));//4
            verts.Add(new Vector3(0, 1, 1));//5
            verts.Add(new Vector3(1, 1, 1));//6
            verts.Add(new Vector3(1, 1, 0));//7

            verts.Add(new Vector3(1, 0, 0));//8
            verts.Add(new Vector3(1, 1, 0));//9
            verts.Add(new Vector3(1, 1, 1));//10
            verts.Add(new Vector3(1, 0, 1));//11

            indices.Add(0); indices.Add(1); indices.Add(2); indices.Add(0); indices.Add(2); indices.Add(3);
            indices.Add(1); indices.Add(4); indices.Add(5); indices.Add(1); indices.Add(5); indices.Add(2);
            indices.Add(2); indices.Add(5); indices.Add(3); indices.Add(3); indices.Add(5); indices.Add(6);
        }
    }

    [MenuItem("Assets/ExportSpriteMesh", false, 0)]
    public static void SelectSpriteToMesh()
    {
        EditorHelper.ExecuteSelection("SpriteToMesh", obj =>
        {
            ExportSpriteMeshHelper.ExportSpriteMesh(obj);
        }, SelectionMode.Assets);
    }

    [MenuItem("Tools/TestProfilerSample", false, 0)]
    public static void TestProfiler()
    {
        Profiler.enabled = true;
        Profiler.BeginSample("Test!!!!Test!!!!!");
        for (int i = 0; i < 1000; i++)
        {
            List<int> alist = new List<int>(){1,2,3,4,5,6,7,8};
        }
        Debug.Log(0);
        Profiler.EndSample();
    }
}
