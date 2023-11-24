/*---------------------------------------------------------------------------------------
-- 负责人: onemt
-- 创建时间: 2023-11-03 17:10:53
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

//定义和compute里一样的结构
public struct ParticleData
{
    public Vector3 pos;//等价于float3
    public Color color;//等价于float4
}

public class ComputeParticleDemo:MonoBehaviour
{
    public ComputeShader computeShader;
    public Material material;
    
    private const int mParticleCount = 20000;
    private ComputeBuffer mParticleDataBuffer;
    private int kernelId;

    private int TimeId;
    private int ParticleBufferId;
    private void Awake()
    {
        //struct中一共7个float，size=28
        mParticleDataBuffer = new ComputeBuffer(mParticleCount,28);
        ParticleData[] particleDatas = new ParticleData[mParticleCount];
        mParticleDataBuffer.SetData(particleDatas);
        kernelId = computeShader.FindKernel("UpdateParticle");

        TimeId = Shader.PropertyToID("Time");
        ParticleBufferId = Shader.PropertyToID("ParticleBuffer");
    }

    private void Update()
    {
        //因为粒子会移动所以需要每帧去传递粒子的信息
        computeShader.SetBuffer(kernelId,ParticleBufferId,mParticleDataBuffer);
        computeShader.SetFloat(TimeId,Time.time);
        computeShader.Dispatch(kernelId,mParticleCount/1000,1,1);
        //传递computeShader到Shader中
        material.SetBuffer("_particleDataBuffer",mParticleDataBuffer);
    }

    //在摄像机渲染场景后调用
    private void OnRenderObject()
    {
        material.SetPass(0);
        //我们可以用该方法绘制几何，第一个参数是拓扑结构，第二个参数数顶点数。
        Graphics.DrawProceduralNow(MeshTopology.Points,mParticleCount);
    }

    private void OnDestroy()
    {
        mParticleDataBuffer.Release();
        mParticleDataBuffer = null;
    }
}