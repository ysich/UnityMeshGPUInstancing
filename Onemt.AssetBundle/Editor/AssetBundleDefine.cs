/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 14:38:28
-- 概述:
---------------------------------------------------------------------------------------*/

namespace OnemtEditor.AssetBundle
{
    public enum BundlePackingMode
    {
        PackTogether,

        PackSeparately,

        //PackByFolder
    }

    public enum BundleCompressionMode
    {
        UnCompressed,

        LZ4,

        LZMA
    }
}
