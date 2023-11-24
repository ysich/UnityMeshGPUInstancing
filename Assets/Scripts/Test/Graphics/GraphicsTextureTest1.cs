using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphicsTextureTest1 : MonoBehaviour
{
    public Texture texture;
    public RenderTexture renderTexture;

    private void Start()
    {
        Rect rect = new Rect(0,0,1000,1000);
        Graphics.DrawTexture(rect,renderTexture);
        Graphics.Blit(texture,renderTexture);
    }
}
