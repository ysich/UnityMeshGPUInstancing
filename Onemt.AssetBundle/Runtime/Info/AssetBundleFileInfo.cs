/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-29 09:56:46
-- 概述:
        assetbundle 文件信息
        TODO zm.
                1. 整合到其他文件中 2. 如果未整合 则可能修改为 ScriptableObject
---------------------------------------------------------------------------------------*/

namespace Onemt.ResourceManagement
{
    public class AssetBundleFileInfo
    {
        /// <summary>
        ///   <param> assetbundle 文件名. </param>
        /// </summary>
        public string fileName;

        /// <summary>
        ///   <para> md5 值. </para>
        /// </summary>
        public string md5;

        /// <summary>
        ///   <para> 未解压(lzma) assetbundle 尺寸大小. </para>
        /// </summary>
        public ulong sizeCompress;

        /// <summary>
        ///   <para> 解压缩后(lz4runtime) assetbundle 尺寸大小. </para>
        /// </summary>
        public ulong sizeLz4;

        /// <summary>
        ///   <para> assetbundle 对应的版本. </para>
        /// </summary>
        public string version;

        /// <summary>
        ///   <para> 补丁匹配的 assetbundle 版本号. </para>
        /// </summary>
        public string patchMatchVersion;

        /// <summary>
        ///   <para> 补丁对应的 assetbundle 尺寸大小. </para>
        /// </summary>
        public ulong patchABSize;

        /// <summary>
        ///   <para> 补丁文件的尺寸大小. </para>
        /// </summary>
        public ulong patchFileSize;

        /// <summary>
        ///   <para> 是否为远程资源. </para>
        /// </summary>
        public bool isRemote;

        public AssetBundleFileInfo() { }

        public AssetBundleFileInfo(AssetBundleFileInfo other)
        {
            this.fileName = other.fileName;
            this.sizeCompress = other.sizeCompress;
            this.sizeLz4 = other.sizeLz4;
            this.version = other.version;
            this.patchMatchVersion = other.patchMatchVersion;
            this.patchABSize = other.patchABSize;
            this.patchFileSize = other.patchFileSize;
        }

        public bool TryParse(string content)
        {
            string[] temp = content.Split(':');
            if (temp.Length < 8)
                return false;

            fileName = temp[0];
            md5 = temp[1];
            sizeCompress = ulong.Parse(temp[2]);
            sizeLz4 = ulong.Parse(temp[3]);
            version = temp[4];
            patchMatchVersion = temp[5];
            patchABSize = ulong.Parse(temp[6]);
            patchFileSize = ulong.Parse(temp[7]);
            return true;
        }

        /// <summary>
        ///   <para> 是否有补丁. </para>
        ///   <para> 默认只要补丁文件的 size 大于0，就说明存在补丁. </para>
        /// </summary>
        /// <returns></returns>
        public bool HavePatch()
        {
            return patchFileSize > 0;
        }
    }
}
