/*---------------------------------------------------------------------------------------
-- 负责人: onemt
-- 创建时间: 2023-11-21 14:23:42
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using UnityEngine;

public class SpriteMeshMgr
{
    private Dictionary<int, Dictionary<int, SpriteMeshInfo>> SpriteMeshInfoMaps =
        new Dictionary<int, Dictionary<int, SpriteMeshInfo>>();

    private static SpriteMeshMgr _Instance;

    public static SpriteMeshMgr Instance
    {
        get
        {
            if (_Instance == null)
            {
                _Instance = new SpriteMeshMgr();
            }

            return _Instance;
        }
    }
    private SpriteMeshMgr()
    {
        SpriteMeshConfigData spriteMeshConfigData = MeshHelper.GetSpriteMeshConfigData();
        var spriteMeshConfigs = spriteMeshConfigData.spriteMeshConfigs;
        foreach (var spriteMeshConfig in spriteMeshConfigs)
        {
            Dictionary<int, SpriteMeshInfo> dict = new Dictionary<int, SpriteMeshInfo>();
            foreach (var spriteMeshInfo in spriteMeshConfig.spriteMeshInfos)
            {
                int hashCode = spriteMeshInfo.hashCode;
                dict[hashCode] = spriteMeshInfo;
            }

            SpriteMeshInfoMaps[spriteMeshConfig.hashCode] = dict;
        }
    }

    public SpriteMeshInfo GetSpriteMeshInfo(Sprite sprite)
    {
        int texHashCode = sprite.texture.GetHashCode();
        if (SpriteMeshInfoMaps.TryGetValue(texHashCode,out Dictionary<int,SpriteMeshInfo> dictionary))
        {
            if(dictionary.TryGetValue(sprite.GetHashCode(),out SpriteMeshInfo spriteMeshInfo))
            {
                return spriteMeshInfo;
            }
        }

        throw new Exception("SpriteMeshInfo is not found!!");
        return null;
    }
}