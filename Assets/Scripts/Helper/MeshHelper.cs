/*---------------------------------------------------------------------------------------
-- 负责人: onemt
-- 创建时间: 2023-11-21 09:31:36
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class MeshHelper
{
    public static List<Sprite> GetAllSpritesByAtlasPath(string path)
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
    
    public const string kMeshConfigPath = "Assets/BundleAssets/Mesh/SpriteMesh/SpriteMeshConfigData.asset";
    public static SpriteMeshConfigData GetSpriteMeshConfigData()
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