/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-05-30 16:41:53
-- 概述:
        Texture Packer 导出的json.txt中的 图集 数据信息.
---------------------------------------------------------------------------------------*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OnemtEditor.TexturePacker
{
    public class MetaData
    {
        /// <summary>
        ///   <para> 图片名称. </para>
        /// </summary>
        public string image;

        /// <summary>
        ///   <para> 图片格式. </para>
        /// </summary>
        public string format;

        /// <summary>
        ///   <para> 图片尺寸大小. </para>
        /// </summary>
        public Vector2 size;


        /// <summary>
        ///   <para> 图片缩放. </para>
        /// </summary>
        public float scale;

        public string smartupdate;

        public MetaData(Hashtable table)
        {
            this.image = (string)table["image"];
            this.format = (string)table["format"];
            this.size = ((Hashtable)table["size"]).HashtableToVector2();
            this.scale = float.Parse(table["scale"].ToString());
            this.smartupdate = (string)table["smartupdate"];
        }
    }
}
