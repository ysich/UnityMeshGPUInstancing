/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-17 10:13:49
-- 概述:
---------------------------------------------------------------------------------------*/

using UnityEngine;
using System.IO;

namespace OnemtEditor.Define
{
    public class EditorPathDefine
    {
        public const string kApplicationSettings = "Assets/Settings/ApplicationSettings.asset";

        /// <summary>
        ///   <para> 输出目录名称. </para>
        /// </summary>
        public const string kCatelogOutput = "out";

        /// <summary>
        ///   <para> 打包输出目录. </para>
        /// </summary>
        public static string kCatelogPackage = string.Format("{0}/../../{1}", Application.dataPath, kCatelogOutput);

        /// <summary>
        ///   <para> 打包输出路径. </para>
        /// </summary>
        public static string kPathNativeProject = string.Format("{0}/NativeProject", kCatelogPackage);

        /// <summary>
        ///   <para> 包输出路径. </para>
        /// </summary>
        public static string kPathPackage = string.Format("0/../game", System.Environment.CurrentDirectory);
        
        /// <summary>
        ///   <para> 图集包含的贴图路径. </para>
        /// </summary>
        public static string kPathAtlasSource = "Assets/BundleAssets/UI/AtlasIcon";

        /// <summary>
        ///   <para> 图集路径. </para>
        /// </summary>
        public static string kPathAtlas = "Assets/BundleAssets/UI/Atlas";

        /// <summary>
        ///   <para> 图集临时路径. </para>
        /// </summary>
        public static string kPathAtlasTemp = "Assets/AtlasTemp";

        /// <summary>
        ///   <para> protocol 存放路径. </para>
        /// </summary>
        public static string kPathProtocol = "Lua/Game/Protocol";
    }

}
