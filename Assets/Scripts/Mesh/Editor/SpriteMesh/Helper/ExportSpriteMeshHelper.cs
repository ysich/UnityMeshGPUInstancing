/*---------------------------------------------------------------------------------------
-- 负责人: onemt
-- 创建时间: 2023-11-06 19:57:14
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using OnemtEditor.TexturePacker;
using UnityEditor;
using UnityEngine;
using FileHelper = Onemt.Core.Util.FileHelper;
using Object = UnityEngine.Object;

public static class ExportSpriteMeshHelper
{
    public static void CreateDirectory(string path)
    {
        if (Directory.Exists(path)) return;

        Directory.CreateDirectory(path);
    }
    
    public const string kMeshConfigPath = "Assets/BundleAssets/Mesh/SpriteMesh/SpriteMeshConfigData.asset";
    public const string kAnimationAtlasIconPath = "Assets/BundleAssets/Mesh/SpriteMesh/AtlasIcon";
    public const string kAnimationAtlasPath = "Assets/BundleAssets/Mesh/SpriteMesh/Mesh";

    /// <summary>
    /// 根据序列帧散图路径生成序列帧图集以及配置信息
    /// </summary>
    /// <param name="obj"></param>
    public static void ExportSpriteMesh(Object obj)
    {
        if (!(obj is DefaultAsset))
        {
            EditorUtility.DisplayDialog("Error","选中的路径不对！","ok");
            return;
        }
        string path = AssetDatabase.GetAssetPath(obj);
        ExportSpriteMesh(path);
    }
    public static void ExportSpriteMesh(string path,bool isMeshCompress = true)
    {
        if (!path.Contains(kAnimationAtlasIconPath))
        {
            EditorUtility.DisplayDialog("Error","选中的路径不对！","ok");
            return;
        }
        //打图集
        TexturePackerHelper.PackerAnimationAtlas(path,true);
        GenerateSpriteMeshData(path);
        
        //这里默认使用Quad网格所以不使用压缩
        // if (isMeshCompress)
        // {
        //     MeshCompressionHelper.MeshCompression(mesh);
        // }
    }

    /// <summary>
    /// 创建一个精灵图集的Mesh数据（使用通用Quad网格）
    /// </summary>
    private static void GenerateSpriteMeshData(string path)
    {
        //图集名称
        string name = Path.GetFileNameWithoutExtension(path);
        //图集路径
        string atlasPath = string.Format("{0}/{1}/{1}.png",kAnimationAtlasPath, name);
        List<Sprite> spriteList = GetAllSpritesByAtlasPath(atlasPath);

        SpriteMeshConfigData spriteMeshConfigData = GetSpriteMeshConfigData();
        SpriteMeshConfigInfo spriteMeshConfigInfo = new SpriteMeshConfigInfo(name,spriteList[0].texture.GetHashCode());
        for (int i = 0; i < spriteList.Count; i++)
        {
            Sprite sprite = spriteList[i];
            (Vector4 pivot,Vector4 newUV) = GetPivotAndUV(sprite);
            SpriteMeshInfo spriteMeshInfo = new SpriteMeshInfo()
            {
                hashCode = sprite.GetHashCode(),
                spriteName = sprite.name,
                pivot = pivot,
                uv = newUV
            };
            spriteMeshConfigInfo.AddInfo(spriteMeshInfo);
        }
        spriteMeshConfigData.AddConfigData(spriteMeshConfigInfo);
    }

    /// <summary>
    /// 创建一个Sprite的四边形mesh
    /// </summary>
    /// <param name="spriteList"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    [Obsolete("实验后发现这个mesh好像无意义,如果是四边形的sprite直接全部使用同一个四边形mesh就行了")]
    private static Mesh GenerateSpriteMesh(List<Sprite> spriteList,string name)
    {
        //先创建一个最小的
        Sprite maxSizeSprite = spriteList[0];
        Vector2 maxSize = maxSizeSprite.rect.size;
        for (int i = spriteList.Count - 1; i >= 1; i--)
        {
            Sprite sprite = spriteList[i];
            Vector2 size = sprite.rect.size;
            if (size.x>= maxSize.x && size.y >= maxSize.y)
            {
                maxSize = size;
                maxSizeSprite = sprite;
            }
        }
        //根据mesh记录缩放值，记录偏移值
        Mesh mesh = new Mesh();
        var vts = new Vector3[4];
        float width = maxSizeSprite.rect.width / maxSizeSprite.pixelsPerUnit;
        float height = maxSizeSprite.rect.height / maxSizeSprite.pixelsPerUnit;
        vts[0] = new Vector3(0,height);
        vts[1] = new Vector3(width, height);
        vts[2] = new Vector3(0,0);
        vts[3] = new Vector3(width, 0);
        var tgs = new int[]{2,0,1,1,3,2};
        mesh.vertices = vts;
        mesh.triangles = tgs;
        Vector2 uv00 = new Vector2(0, 0);
        Vector2 uv11 = new Vector2(1, 1);
        var uvs = new Vector2[4]
        {
            new Vector2(uv00.x, uv00.y),
            new Vector2(uv00.x, uv11.y),
            new Vector2(uv11.x, uv11.y),
            new Vector2(uv11.x, uv00.y)
        };
        mesh.uv = uvs;

        string meshDirectoryPath = $"{kAnimationAtlasPath}/{name}";
        string meshPath = string.Format("{0}/{1}.mesh",meshDirectoryPath, name);
        FileHelper.CreateDirectory(meshDirectoryPath);
        AssetDatabase.CreateAsset(mesh, meshPath);
        AssetDatabase.SaveAssets();
        return mesh;
    }

    private static (Vector4, Vector4) GetPivotAndUV(Sprite sprite)
    {
        Vector4 pivot, newUV;
        // Calculate vertices translate and scale value
        pivot.x = sprite.rect.width / sprite.pixelsPerUnit;
        pivot.y = sprite.rect.height / sprite.pixelsPerUnit;
        pivot.z = ((sprite.rect.width / 2) - sprite.pivot.x) / sprite.pixelsPerUnit;
        pivot.w = ((sprite.rect.height / 2) - sprite.pivot.y) / sprite.pixelsPerUnit;

        // Calculate uv translate and scale value
        newUV.x = sprite.uv[1].x - sprite.uv[0].x;
        newUV.y = sprite.uv[0].y - sprite.uv[2].y;
        newUV.z = sprite.uv[2].x;
        newUV.w = sprite.uv[2].y;
        return (pivot, newUV);
    }

    private static List<Sprite> GetAllSpritesByAtlasPath(string path)
    {
        List<Sprite> spriteList = new List<Sprite>();
        Object[] objs = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var obj in objs)
        {
            if (obj is Sprite sprite)
            {
                spriteList.Add(sprite);
            }
        }
        return spriteList;
    }

    private static SpriteMeshConfigData GetSpriteMeshConfigData()
    {
        SpriteMeshConfigData spriteMeshConfigData;
        if (!EditorBuildSettings.TryGetConfigObject(kMeshConfigPath, out spriteMeshConfigData))
        {
            spriteMeshConfigData = AssetDatabase.LoadAssetAtPath<SpriteMeshConfigData>(kMeshConfigPath);
            if (spriteMeshConfigData != null)
            {
                EditorBuildSettings.AddConfigObject(kMeshConfigPath,spriteMeshConfigData,true);
            }
        }
        if (spriteMeshConfigData == null)
        {
            throw new Exception($"not found {kMeshConfigPath}");
        }

        return spriteMeshConfigData;
    }

}