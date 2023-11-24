/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-05-30 16:09:33
-- 概述:
        Texture Packer 导出的json.txt中的 单个sprite 数据信息.
---------------------------------------------------------------------------------------*/

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace OnemtEditor.TexturePacker
{
    public class FrameData
    {
        /// <summary>
        ///   <para> sprite 名称. </para>
        /// </summary>
        public string name;

        /// <summary>
        ///   <para> sprite 在图集中的位置区域信息. </para>
        /// </summary>
        public Rect frame;

        /// <summary>
        ///   <para> 是否旋转. </para>
        /// </summary>
        public bool rotated;

        /// <summary>
        ///   <para> 是否裁剪. </para>
        /// </summary>
        public bool trimmed;

        /// <summary>
        ///   <para> sprite 边距裁剪信息. </para>
        /// </summary>
        public Rect spriteSourceSize;

        /// <summary>
        ///   <para> sprite 源尺寸大小. </para>
        /// </summary>
        public Vector2 sourceSize;

        /// <summary>
        ///   <para> 图集尺寸大小. </para>
        /// </summary>
        private Vector2 m_AtlasSize;

        public FrameData(string name, Vector2 atlasSize, Hashtable table)
        {
            this.name = name;
            this.m_AtlasSize = atlasSize;
            this.frame = ((Hashtable)table["frame"]).HashtableToRect();
            this.rotated = (bool)table["rotated"];
            this.trimmed = (bool)table["trimmed"];
            this.spriteSourceSize = ((Hashtable)table["spriteSourceSize"]).HashtableToRect();
            this.sourceSize = ((Hashtable)table["sourceSize"]).HashtableToVector2();
        }

        public Mesh BuildBasicMesh(float scale, Color32 defaultColor)
        {
            return BuildBasicMesh(scale, defaultColor, Quaternion.identity);
        }

        public Mesh BuildBasicMesh(float scale, Color32 defaultColor, Quaternion rotation)
        {
            Mesh mesh = new Mesh();
            Vector3[] array = new Vector3[4];
            Vector2[] array2 = new Vector2[4];
            Color32[] array3 = new Color32[4];
            bool flag = !this.rotated;
            if (flag)
            {
                array[0] = new Vector3(this.frame.x, this.frame.y, 0f);
                array[1] = new Vector3(this.frame.x, this.frame.y + this.frame.height, 0f);
                array[2] = new Vector3(this.frame.x + this.frame.width, this.frame.y + this.frame.height, 0f);
                array[3] = new Vector3(this.frame.x + this.frame.width, this.frame.y, 0f);
            }
            else
            {
                array[0] = new Vector3(this.frame.x, this.frame.y, 0f);
                array[1] = new Vector3(this.frame.x, this.frame.y + this.frame.width, 0f);
                array[2] = new Vector3(this.frame.x + this.frame.height, this.frame.y + this.frame.width, 0f);
                array[3] = new Vector3(this.frame.x + this.frame.height, this.frame.y, 0f);
            }
            array2[0] = array[0].Vector3toVector2();
            array2[1] = array[1].Vector3toVector2();
            array2[2] = array[2].Vector3toVector2();
            array2[3] = array[3].Vector3toVector2();
            for (int i = 0; i < array2.Length; i++)
            {
                Vector2[] array4 = array2;
                int num = i;
                array4[num].x = array4[num].x / this.m_AtlasSize.x;
                Vector2[] array5 = array2;
                int num2 = i;
                array5[num2].y = array5[num2].y / this.m_AtlasSize.y;
                array2[i].y = 1f - array2[i].y;
            }
            bool flag2 = this.rotated;
            if (flag2)
            {
                array[3] = new Vector3(this.frame.x, this.frame.y, 0f);
                array[0] = new Vector3(this.frame.x, this.frame.y + this.frame.height, 0f);
                array[1] = new Vector3(this.frame.x + this.frame.width, this.frame.y + this.frame.height, 0f);
                array[2] = new Vector3(this.frame.x + this.frame.width, this.frame.y, 0f);
            }
            for (int j = 0; j < array.Length; j++)
            {
                array[j].y = this.m_AtlasSize.y - array[j].y;
            }
            for (int k = 0; k < array.Length; k++)
            {
                Vector3[] array6 = array;
                int num3 = k;
                array6[num3].x = array6[num3].x - (this.frame.x - this.spriteSourceSize.x + this.sourceSize.x / 2f);
                Vector3[] array7 = array;
                int num4 = k;
                array7[num4].y = array7[num4].y - (this.m_AtlasSize.y - this.frame.y - (this.sourceSize.y - this.spriteSourceSize.y) + this.sourceSize.y / 2f);
            }
            for (int l = 0; l < array.Length; l++)
            {
                array[l] *= scale;
            }
            bool flag3 = rotation != Quaternion.identity;
            if (flag3)
            {
                for (int m = 0; m < array.Length; m++)
                {
                    array[m] = rotation * array[m];
                }
            }
            for (int n = 0; n < array3.Length; n++)
            {
                array3[n] = defaultColor;
            }
            mesh.vertices = array;
            mesh.uv = array2;
            mesh.colors32 = array3;
            mesh.triangles = new int[]
            {
                0,
                3,
                1,
                1,
                3,
                2
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.name = this.name;
            return mesh;
        }

        public SpriteMetaData BuildBasicSprite(float scale, Color32 defaultColor)
        {
            SpriteMetaData spriteMetaData = default(SpriteMetaData);
            bool flag = !this.rotated;
            Rect rect;
            if (flag)
            {
                rect = this.frame;
            }
            else
            {
                rect = new Rect(this.frame.x, this.frame.y, this.frame.height, this.frame.width);
            }
            bool flag2 = this.frame.x + this.frame.width > this.m_AtlasSize.x || this.frame.y + this.frame.height > this.m_AtlasSize.y || this.frame.x < 0f || this.frame.y < 0f;
            SpriteMetaData result;
            if (flag2)
            {
                Debug.Log(this.name + " is outside from texture! Sprite is ignored!");
                spriteMetaData.name = "IGNORE_SPRITE";
                result = spriteMetaData;
            }
            else
            {
                rect.y = this.m_AtlasSize.y - this.frame.y - rect.height;
                spriteMetaData.name = Path.GetFileNameWithoutExtension(this.name);
                spriteMetaData.rect = rect;
                bool flag3 = !this.trimmed;
                if (flag3)
                {
                    spriteMetaData.alignment = 0;
                    spriteMetaData.pivot = this.frame.center;
                }
                else
                {
                    spriteMetaData.alignment = 9;
                    spriteMetaData.pivot = new Vector2((this.sourceSize.x / 2f - this.spriteSourceSize.x) / this.spriteSourceSize.width, 1f - (this.sourceSize.y / 2f - this.spriteSourceSize.y) / this.spriteSourceSize.height);
                }
                result = spriteMetaData;
            }
            return result;
        }
    }
}
