using System;
using System.Collections;
using System.Collections.Generic;
using InstanceSprite;
using UnityEngine;

public class AnimCtrl : MonoBehaviour
{
    public AnimType AnimType;

    private InstanceSpriteRenderer m_InstanceSpriteRenderer;
    private SpriteRenderer m_SpriteRenderer;

    public string path;
    
    /// <summary>
    /// 帧/秒，每秒播放多少张图片
    /// </summary>
    public float _fps = 5f;
    private float m_CurTime;
    private float m_PerTime = -1;

    private int m_CurFrame;
    private List<Sprite> m_Sprites;
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
    private void Start()
    {
        if (AnimType == AnimType.SpriteMesh)
        {
            m_InstanceSpriteRenderer = this.GetComponent<InstanceSpriteRenderer>();
        }
        else
        {
            m_SpriteRenderer = this.GetComponent<SpriteRenderer>();
        }

        m_Sprites = MeshHelper.GetAllSpritesByAtlasPath(path);
    }

    private void LateUpdate()
    {
        m_CurTime += Time.deltaTime;
        if (m_CurTime > perTime)
        {
            m_CurTime = 0;
            UpdateFrame();
        }
    }

    private void UpdateFrame()
    {
        m_CurFrame++;
        if (m_CurFrame >= m_Sprites.Count)
        {
            m_CurFrame = 0;
            SetFrame();
        }
        else
        {
            SetFrame();
        }
    }

    private void SetFrame()
    {
        Sprite sprite = m_Sprites[m_CurFrame];
        if (AnimType == AnimType.SpriteMesh)
        {
            m_InstanceSpriteRenderer.SetSprite(sprite);
        }
        else
        {
            m_SpriteRenderer.sprite = sprite;
        }
    }
}
