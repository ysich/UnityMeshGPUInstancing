/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 15:46:55
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Build.Pipeline;
using UnityEditor;
using OnemtEditor.AssetBundle.Settings;

namespace OnemtEditor.AssetBundle.Build
{
    public class AssetBundleBuildParameters : BundleBuildParameters
    {
        // assetbundle name  -> groupGUID
        private Dictionary<string, string> m_BundlToAssetGroup;
        private AssetBundleAssetSettings m_Settings;

        public AssetBundleBuildParameters(AssetBundleAssetSettings settings, Dictionary<string, string> bundleToAssetGroup,
            BuildTarget buildTarget, BuildTargetGroup buildTargetGroup, string outputFolder)
            : base(buildTarget, buildTargetGroup, outputFolder)
        {
            UseCache = true;
#if UNITY_2021_1_OR_NEWER
            ContiguousBundles = true;
            NonRecursiveDependencies = true;
#else
            ContiguousBundles = false;
            NonRecursiveDependencies = false;
#endif
            DisableVisibleSubAssetRepresentations = false;

            m_BundlToAssetGroup = bundleToAssetGroup;
            m_Settings = settings;

            WriteLinkXML = true;   //生成AssetBundle使用的Link文件，便于代码裁剪
        }

        /// <summary>
        ///   <para> 用于获取每个group的压缩格式. </para>
        /// </summary>
        /// <param name="assetbundleName"></param>
        /// <returns></returns>
        public override UnityEngine.BuildCompression GetCompressionForIdentifier(string assetbundleName)
        {
            // TODO zm. 冗余资源压缩格式处理
            // 默认使用 lzma
            if (!m_BundlToAssetGroup.TryGetValue(assetbundleName, out var groupGUID))
                return BuildCompression.LZMA;
                //return base.GetCompressionForIdentifier(assetbundleName);

            var assetGroup = m_Settings.GetAssetGroup(groupGUID);
            if (assetGroup == null)
                UnityEngine.Debug.LogErrorFormat(" Get AssetGroup Empty with groupGUID : {0}", groupGUID);

            switch (assetGroup.buildCompressionMode)
            {
                case BundleCompressionMode.LZ4:
                    return BuildCompression.LZ4;
                case BundleCompressionMode.LZMA:
                    return BuildCompression.LZMA;
                case BundleCompressionMode.UnCompressed:
                    return BuildCompression.Uncompressed;
            }
                

            return default(BuildCompression);
        }

        /// <summary>
        ///   <para> 获取 assetbundle 输出路径. </para>
        /// </summary>
        /// <param name="assetbundleName"></param>
        /// <returns></returns>
        public override string GetOutputFilePathForIdentifier(string assetbundleName)
        {
            return base.GetOutputFilePathForIdentifier(assetbundleName);
        }

        /// <summary>
        ///   <para> 判断 assetbundle 是否为远程资源. </para>
        ///   <para> 1. 根据group中配置  2. 冗余的 assetbundle 默认为本地资源.</para>
        /// </summary>
        /// <param name="assetbundleName"></param>
        /// <returns></returns>
        public bool CheckIsRemote(string assetbundleName)
        {
            if (!m_BundlToAssetGroup.TryGetValue(assetbundleName, out var groupGUID))
                return false;

            var assetGroup = m_Settings.GetAssetGroup(groupGUID);
            if (assetGroup == null)
                UnityEngine.Debug.LogErrorFormat(" Get AssetGroup Empty with groupGUID : {0}", groupGUID);

            return assetGroup.isRemote;
        }
    }
}
