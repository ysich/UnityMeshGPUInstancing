using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeShaderDemo : MonoBehaviour
{
    public ComputeShader computeShader;
    public Material material;

    private void Awake()
    {
        RenderTexture mRenderTexture = new RenderTexture(256, 256, 16);
        mRenderTexture.enableRandomWrite = true;
        mRenderTexture.Create();

        material.mainTexture = mRenderTexture;
        int kernelIndex = computeShader.FindKernel("CSMain");
        computeShader.SetTexture(kernelIndex,"Result",mRenderTexture);
        //开线程组处理
        //因为computeShader中定义了一个线程组包含8个线程，一个线程组会同时处理8个像素，所以这里需要/8
        computeShader.Dispatch(kernelIndex, 256 / 8, 256 / 8, 1);
    }
}
