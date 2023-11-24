/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-06-02 16:30:25
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OnemtEditor.TexturePacker
{
    /// <summary>
    ///   <para> 裁剪模式. </para>
    /// </summary>
    public enum TrimMode
    {
        None = 0,
        Trim = 1,           // 删除周围的透明度，使用起来和原来大小差不多
        CropKeepPos = 2,    // 删除周围的透明度，使用起来较小，原始位置被保存
        Crop = 3,           // 删除周围的透明度，使用起来较小，丢弃原始位置
        Polygon,            // 追踪轮廓并生成近似的多边形
    }
}
