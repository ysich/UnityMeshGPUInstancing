using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteMeshConfigData : ScriptableObject
{
    public List<SpriteMeshConfigInfo> spriteMeshConfigs = new List<SpriteMeshConfigInfo>();

    public void AddConfigData(SpriteMeshConfigInfo spriteMeshConfigInfo)
    {
        for (int i = 0; i < spriteMeshConfigs.Count; i++)
        {
            var cfgInfo = spriteMeshConfigs[i];
            if (cfgInfo.textureName == spriteMeshConfigInfo.textureName)
            {
                spriteMeshConfigs[i] = spriteMeshConfigInfo;
                return;
            }
        }
        spriteMeshConfigs.Add(spriteMeshConfigInfo);
    }
}

[Serializable]
public class SpriteMeshConfigInfo
{
    public int hashCode;
    public string textureName;
    public List<SpriteMeshInfo> spriteMeshInfos = new List<SpriteMeshInfo>();
    public SpriteMeshConfigInfo(string textureName,int hashCode)
    {
        this.hashCode = hashCode;
        this.textureName = textureName;
    }

    public void AddInfo(SpriteMeshInfo spriteMeshInfo)
    {
        spriteMeshInfos.Add(spriteMeshInfo);
    }
}

[Serializable]
public class SpriteMeshInfo
{
    public int hashCode;
    public string spriteName;
    /// <summary>
    /// 缩放和偏移值
    /// </summary>
    public Vector4 pivot;
    /// <summary>
    /// uv值
    /// </summary>
    public Vector4 uv;
}