using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace InstanceSprite
{
    public class InstanceSpriteRenderer : MonoBehaviour
    {
        public Mesh mesh;
        [SerializeField]
        private Sprite sprite;

        public Sprite Sprite => sprite;
        public Material material;
        
        public Vector4 pivot;
        public Vector4 newUV;
        
        private MeshFilter m_MeshFilter;
        private MeshRenderer m_MeshRenderer;
        private MaterialPropertyBlock m_Props;
        private int m_TextureID;
        
        private int _Pivot_ID;
        private int _NewUV_ID;

        private void Start()
        {
            if (m_Props == null)
            {
                m_Props = new MaterialPropertyBlock();
            }

            m_MeshFilter = this.GetComponent<MeshFilter>();
            m_MeshRenderer = this.GetComponent<MeshRenderer>();
            m_MeshRenderer.sharedMaterial = material;
            m_MeshFilter.sharedMesh = mesh;
            _Pivot_ID = Shader.PropertyToID("_Pivot");
            _NewUV_ID = Shader.PropertyToID("_NewUV");
        }

        /// <summary>
        /// 设置Sprite的方法
        /// </summary>
        /// <param name="sprite"></param>
        public void SetSprite(Sprite tempSprite)
        {
            if (sprite.Equals(tempSprite))
            {
                return;
            }

            sprite = tempSprite;
            // string texName = sprite.texture.name;
            SpriteMeshInfo spriteMeshInfo = SpriteMeshMgr.Instance.GetSpriteMeshInfo(tempSprite);
            
            m_Props.SetVector(_Pivot_ID,spriteMeshInfo.pivot);
            m_Props.SetVector(_NewUV_ID,spriteMeshInfo.uv);
            m_MeshRenderer.SetPropertyBlock(m_Props);
        }
    }
}
