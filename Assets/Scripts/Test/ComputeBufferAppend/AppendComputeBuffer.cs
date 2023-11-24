using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppendComputeBuffer : MonoBehaviour
{
    private int count;
    private void Start()
    {
        var buffer = new ComputeBuffer(count, sizeof(float), ComputeBufferType.Append);
        
        buffer.SetCounterValue(0);//计算器的值为0 
    }
}
