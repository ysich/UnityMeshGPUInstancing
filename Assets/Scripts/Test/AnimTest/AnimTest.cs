using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimTest : MonoBehaviour
{
    public GameObject TargetSpriteGo;
    public GameObject TargetMeshGo;

    public AnimType AnimType ;

    public int GoCount;
    private GameObject SpriteGo;
    private GameObject MeshGo;
    void Start()
    {

        SpriteGo = new GameObject("Sprite");
        SpriteGo.transform.SetParent(this.transform);
        MeshGo = new GameObject("Mesh");
        MeshGo.transform.SetParent(this.transform);
        bool isSprite = AnimType == AnimType.Sprite;
        for (int i = 0; i < GoCount; i++)
        {
            int index = i/40;
            var pos = new Vector3(index*0.5f, 1 + 0.3f * (i % 40), 0);
            var spriteGo = Instantiate(TargetSpriteGo, SpriteGo.transform);
            spriteGo.transform.position = pos;
            var meshGo = Instantiate(TargetMeshGo, MeshGo.transform);
            meshGo.transform.position = pos;
        }
        if(isSprite)
            MeshGo.SetActive(false);
        else
            SpriteGo.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            ChangeAnim();
        }
    }

    public void ChangeAnim()
    {
        if (AnimType == AnimType.SpriteMesh)
        {
            AnimType = AnimType.Sprite;
            SpriteGo.SetActive(true); 
            MeshGo.SetActive(false);
        }
        else
        {
            AnimType = AnimType.SpriteMesh;
            SpriteGo.SetActive(false);
            MeshGo.SetActive(true);
        }   
    }
}

