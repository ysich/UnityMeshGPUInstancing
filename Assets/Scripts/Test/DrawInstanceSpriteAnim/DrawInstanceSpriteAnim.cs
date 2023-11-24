/*---------------------------------------------------------------------------------------
-- 负责人: onemt
-- 创建时间: 2023-11-21 17:10:51
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using InstanceSprite;
using UnityEngine;
using Random = UnityEngine.Random;

public struct InstanceSpriteInfoData
{
    public Vector4 uv;
    public Vector4 pivot;
}
public class DrawInstanceSpriteAnim:MonoBehaviour
{
    public int instanceCount;
    public Mesh instanceMesh;
    public Material instanceMaterial;
    public string texturePath;
    public int subMeshIndex = 0;
    
    public float range;
    private int cachedInstanceCount = -1;
    private int cachedSubMeshIndex = -1;
    private Bounds m_bounds;
    
    private ComputeBuffer SpriteInfoBuffer;
    private InstanceSpriteInfoData[] InstanceSpriteInfoDatas;
    private ComputeBuffer argsBuffer;
    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    private List<Sprite> m_Sprites;


    #region 定时器相关

    /// <summary>
    /// 帧/秒，每秒播放多少张图片
    /// </summary>
    public float _fps = 5f;
    private float m_CurTime;
    private float m_PerTime = -1;
    private int m_CurFrame = 0;
    float perTime
    {
        get
        {
            if (m_PerTime == -1)
            {
                m_PerTime = 1 / _fps;
            }
            return m_PerTime;
        }
    }

    #endregion
    private void Start()
    {
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        m_bounds = new Bounds(transform.position, Vector3.one * (range + 1));
        m_Sprites = MeshHelper.GetAllSpritesByAtlasPath(texturePath);
    }

    private void LateUpdate()
    {
        if (cachedInstanceCount != instanceCount|| cachedSubMeshIndex != subMeshIndex)
        {
            UpdateBuffers();
        }
        
        m_CurTime += Time.deltaTime;
        if (m_CurTime > perTime)
        {
            m_CurTime = 0;
            UpdateFrame();
        }
        Graphics.DrawMeshInstancedIndirect(instanceMesh,subMeshIndex,instanceMaterial,m_bounds,argsBuffer);
    }

    void UpdateBuffers() {
        // Ensure submesh index is in range
        if (instanceMesh != null)
            subMeshIndex = Mathf.Clamp(subMeshIndex, 0, instanceMesh.subMeshCount - 1);

        //spriteMeshInfo
        if (SpriteInfoBuffer != null)
        {
            SpriteInfoBuffer.Release();
        }
        SpriteInfoBuffer = new ComputeBuffer(instanceCount,32);
        InstanceSpriteInfoDatas = new InstanceSpriteInfoData[instanceCount];
        // SpriteInfoBuffer.SetData(InstanceSpriteInfoDatas);
        // instanceMaterial.SetBuffer("_spriteInfoBuffer",SpriteInfoBuffer);

        // Indirect args
        if (instanceMesh != null) {
            args[0] = (uint)instanceMesh.GetIndexCount(subMeshIndex);
            args[1] = (uint)instanceCount;
            args[2] = (uint)instanceMesh.GetIndexStart(subMeshIndex);
            args[3] = (uint)instanceMesh.GetBaseVertex(subMeshIndex);
        }
        else
        {
            args[0] = args[1] = args[2] = args[3] = 0;
        }
        argsBuffer.SetData(args);

        cachedInstanceCount = instanceCount;
        cachedSubMeshIndex = subMeshIndex;
    }

    private void UpdateFrame()
    {
        m_CurFrame++;
        if (m_CurFrame >= m_Sprites.Count)
        {
            m_CurFrame = 0;
        }
        UpdateInstanceData();
    }
    private void UpdateInstanceData()
    {
        Sprite sprite = m_Sprites[m_CurFrame];
        SpriteMeshInfo spriteMeshInfo = 
            SpriteMeshMgr.Instance.GetSpriteMeshInfo(sprite);

        InstanceSpriteInfoData InstanceSpriteInfoData =
            new InstanceSpriteInfoData() { uv = spriteMeshInfo.uv, pivot = spriteMeshInfo.pivot };
        for (int i = 0; i < instanceCount; i++)
        {
            InstanceSpriteInfoDatas[i] = InstanceSpriteInfoData;
        }
        SpriteInfoBuffer.SetData(InstanceSpriteInfoDatas);
        instanceMaterial.SetBuffer("_spriteInfoBuffer",SpriteInfoBuffer);
    }

    private void OnDisable()
    {
        if (SpriteInfoBuffer!=null)
            SpriteInfoBuffer.Release();
        SpriteInfoBuffer = null;
        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;
    }
}