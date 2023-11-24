/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 10:37:48
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Onemt.ResourceManagement.ResourceLocations
{
    [Serializable]
    public class ResourceLocationBase : IResourceLocation
    {
        // 加载的资源名称
        [FormerlySerializedAs("m_AssetName")]
        [SerializeField]
        private string m_AssetName;
        public string assetName { get => m_AssetName; set => m_AssetName = value; }

        /// <summary>
        ///   <para> ab 名. </para>
        /// </summary>
        [FormerlySerializedAs("m_AssetBundleName")]
        [SerializeField]
        private string m_AssetBundleName;
        public string assetbundleName { get => m_AssetBundleName; }

        ///// <summary>
        /////   <para> md5 值. </para>
        ///// </summary>
        //[FormerlySerializedAs("m_MD5")]
        //[SerializeField]
        //private string m_MD5;
        //public string md5
        //{
        //    get => m_MD5;
        //    set => m_MD5 = value;
        //}

        ///// <summary>
        /////   <para> lzma 压缩格式下的大小. </para>
        ///// </summary>
        //[FormerlySerializedAs("m_SizeCompress")]
        //[SerializeField]
        //private ulong m_SizeCompress;
        //public ulong sizeCompress
        //{
        //    get => m_SizeCompress;
        //    set => m_SizeCompress = value;
        //}

        ///// <summary>
        /////   <para> assetbundle 对应的版本. </para>
        ///// </summary>
        //[FormerlySerializedAs("m_Version")]
        //[SerializeField]
        //private string m_Version;
        //public string version
        //{
        //    get => m_Version;
        //    set
        //    {
        //        m_Version = value;
        //        hotfixVersionCode = CVersion.GetLastVersionNum(value);
        //    }//=> m_Version = value;
        //}

        ///// <summary>
        /////   <para> 热更版本号. </para>
        ///// </summary>
        //private int m_HotfixVersionCode;
        //public int hotfixVersionCode
        //{
        //    get => m_HotfixVersionCode;
        //    set => m_HotfixVersionCode = value;
        //}

        ///// <summary>
        /////   <para> 补丁匹配的assetbundle 版本. </para>
        ///// </summary>
        //[FormerlySerializedAs("m_PatchMatchVersion")]
        //[SerializeField]
        //private string m_PatchMatchVersion;
        //public string patchMatchVersion
        //{
        //    get => m_PatchMatchVersion;
        //    set => m_PatchMatchVersion = value;
        //}

        ///// <summary>
        /////   <para> 补丁对应的 assetbundle 大小. </para>
        ///// </summary>
        //[FormerlySerializedAs("m_PatchABSize")]
        //[SerializeField]
        //private ulong m_PatchABSize;
        //public ulong patchABSize
        //{
        //    get => m_PatchABSize;
        //    set => m_PatchABSize = value;
        //}

        ///// <summary>
        /////   <para> 补丁文件的大小. </para>
        ///// </summary>
        //[FormerlySerializedAs("m_PatchFileSize")]
        //[SerializeField]
        //private ulong m_PatchFileSize;
        //public ulong patchFileSize
        //{
        //    get => m_PatchFileSize;
        //    set => m_PatchFileSize = value;
        //}

        /// <summary>
        ///   <para> 是否为远程文件. </para>
        /// </summary>
        [FormerlySerializedAs("m_IsRemote")]
        [SerializeField]
        private bool m_IsRemote;
        public bool isRemote
        {
            get => m_IsRemote;
            set => m_IsRemote = value;
        }

        /// <summary>
        ///   <para> 是否为本地文件. </para>
        /// </summary>
        public bool isLocal => !m_IsRemote;

        ///// <summary>
        /////   <para> lz4 压缩格式下的大小. </para>
        ///// </summary>
        //[FormerlySerializedAs("m_SizeLz4")]
        //[SerializeField]
        //private ulong m_SizeLz4;
        //public ulong sizeLz4
        //{
        //    get => m_SizeLz4;
        //    set => m_SizeLz4 = value;
        //}

        /// <summary>
        ///   <para> 包含的资源. </para>
        /// </summary>
        [FormerlySerializedAs("m_AssetNames")]
        [SerializeField]
        private List<string> m_AssetNames;
        public List<string> assetNames { get => m_AssetNames; }

        [FormerlySerializedAs("m_Dependencies")]
        [SerializeField]
        private List<string> m_Dependencies;
        public List<string> dependencies { get => m_Dependencies; set => m_Dependencies = value; }

        //[FormerlySerializedAs("m_DepLocations")]
        //[SerializeField]
        private List<IResourceLocation> m_DepLocations;
        public List<IResourceLocation> depLocations { get => m_DepLocations; set => m_DepLocations = value; }

        public bool hasDependencies { get => (m_Dependencies != null && m_Dependencies.Count > 0) || (m_DepLocations != null && m_DepLocations.Count > 0); }

        public ResourceLocationBase(string assetbundleName, string[] assetNames, List<string> dependencies, string assetName = "", List<IResourceLocation> deps = null)
        {
            m_AssetBundleName = assetbundleName;
            m_AssetNames = assetNames == null ? new List<string>() : new List<string>(assetNames);
            m_Dependencies = dependencies;
            m_DepLocations = deps;
            m_AssetName = assetName;
        }

        //public bool HavePatch()
        //{
        //    return m_PatchFileSize > 0;
        //}

        public int Hash()
        {
            if (m_Dependencies == null)
                return m_AssetBundleName.GetHashCode() * 31;

            return m_AssetBundleName.GetHashCode() * 31 + m_Dependencies.GetHashCode() * 31;
        }

        ///// <summary>
        /////   <para> 是否需要热更. </para>
        ///// </summary>
        ///// <returns></returns>
        //public bool CheckNeedHotfix()
        //{
        //    return m_HotfixVersionCode > GameConfig.instance.hotfixVersionCode && !isRemote;
        //}

        public override string ToString()
        {
            return string.Format("location assetbundleName : {0}", m_AssetBundleName);
        }
    }
}
