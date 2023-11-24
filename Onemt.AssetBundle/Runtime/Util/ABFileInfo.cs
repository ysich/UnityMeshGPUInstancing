/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-04-10 19:12:15
-- 概述:
---------------------------------------------------------------------------------------*/

using Onemt.Framework.Config;
using static UnityEngine.Rendering.DebugUI;

namespace Onemt.Core.Util
{
    public class ABFileInfo
    {
        /// <summary>
        ///   <para> assetbundle 名称. </para>
        /// </summary>
        public string assetbundleName;

        /// <summary>
        ///   <para> md5 值.</para>
        /// </summary>
        public string md5;

        /// <summary>
        ///   <para> lz4 压缩文件大小. </para>
        /// </summary>
        public ulong sizeLZ4;

        /// <summary>
        ///   <para> 压缩完之后的文件大小. </para>
        /// </summary>
        public ulong sizeCompress;

        /// <summary>
        ///   <para> 版本号. </para>
        /// </summary>
        public string version = "new";

        /// <summary>
        ///   <para> 补丁匹配的assetbundle 版本号. </para>
        /// </summary>
        public string patchMatchVersion = "";

        /// <summary>
        ///   <para> 补丁匹配的assetbundle 文件大小. </para>
        /// </summary>
        public ulong patchABSize = 0;

        /// <summary>
        ///   <para> 补丁文件大小. </para>
        /// </summary>
        public ulong patchFileSize = 0;

        public bool isRemote = false;        

        public ABFileInfo()
        {

        }

        public ABFileInfo(ABFileInfo other)
        {
            this.assetbundleName = other.assetbundleName;
            this.md5 = other.md5;
            this.sizeLZ4 = other.sizeLZ4;
            this.sizeCompress = other.sizeCompress;
            this.version = other.version;
            this.patchMatchVersion = other.patchMatchVersion;
            this.patchABSize = other.patchABSize;
            this.patchFileSize = other.patchFileSize;
            this.isRemote = other.isRemote;
        }

        public bool HavePatch()
        {
            return patchFileSize > 0;
        }

        public bool CheckNeedHotfix()
        {
            var versionCode = CVersion.GetLastVersionNum(version);
            return versionCode > GameConfig.instance.hotfixVersionCode && !isRemote;
        }

        /// <summary>
        ///   <para> 字符串解析. </para>
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool Parse(string text)
        {
            string[] contents = text.Split(':');
            if (contents.Length < 8)
                return false;

            assetbundleName = contents[0];
            md5 = contents[1];
            sizeLZ4 = ulong.Parse(contents[2]);
            sizeCompress = ulong.Parse(contents[3]);
            version = contents[4];
            patchMatchVersion = contents[5];
            patchABSize = ulong.Parse(contents[6]);
            patchFileSize = ulong.Parse(contents[7]);
            isRemote = bool.Parse(contents[8]);
            return true;
        }
    }
}
