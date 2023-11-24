/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 14:51:51
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using System;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OnemtEditor.AssetBundle.Settings
{
    public static class AssetBundleUtility
    {
        // 忽略的文件类型
        private static HashSet<string> s_ExcludedExtensions = new HashSet<string>(new string[] { ".cs", ".js", ".boo", ".exe", ".dll", ".meta" });

        /// <summary>
        /// <para> 是否为有效的 打包资源 </para>
        /// </summary>
        /// <param name="path"> 资源路径 </param>
        /// <returns></returns>
        internal static bool IsPathValidForEntry(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            path = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            if (!path.StartsWith("assets", StringComparison.OrdinalIgnoreCase) && !IsPathValidPackageAsset(path))
                return false;

            if (path.EndsWith($"{Path.DirectorySeparatorChar}Editor") || path.Contains($"{Path.DirectorySeparatorChar}Editor{Path.DirectorySeparatorChar}")
                || path.EndsWith("/Editor") || path.Contains("/Editor/"))
                return false;

            if (path == "Assets")
                return false;

            return !s_ExcludedExtensions.Contains(Path.GetExtension(path));
        }

        /// <summary>
        ///   <para> 是否为有效的 package 内的资源 </para>
        /// </summary>
        /// <param name="path"> 资源路径 </param>
        /// <returns></returns>
        internal static bool IsPathValidPackageAsset(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string convertPath = path.ToLower().Replace("\\", "/");
            string[] splitPath = convertPath.Split('/');

            if (splitPath.Length < 3)
                return false;

            if (splitPath[0] != "packages")
                return false;

            if (splitPath[2] == "package.json")
                return false;

            return true;
        }

        /// <summary>
        ///   <para> 是否文件位于 resources 文件中</para>
        /// </summary>
        /// <param name="path"> 文件路径 </param>
        /// <returns></returns>
        internal static bool IsInResources(string path)
        {
            return path.Replace("\\", "/").ToLower().Contains("/resources/");
        }

        internal static ListRequest RequestPackageListAsync()
        {
            ListRequest req = null;
#if !UNITY_2021_1_OR_NEWER
            req = Client.List(true);
#endif
            return req;
        }

        /// <summary>
        ///   <para> 获取项目中所有的 package 信息. </para>
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        internal static List<UnityEditor.PackageManager.PackageInfo> GetPackages(ListRequest req)
        {
#if UNITY_2021_1_OR_NEWER
            var packagesList = new List<UnityEditor.PackageManager.PackageInfo>(UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages());
            return packagesList;
#else
            while (!req.IsCompleted)
            {
                Thread.Sleep(5);
            }

            var packages = new List<UnityEditor.PackageManager.PackageInfo>();
            if (req.Status == StatusCode.Success)
            {
                PackageCollection collection = req.Result;
                foreach (UnityEditor.PackageManager.PackageInfo package in collection)
                    packages.Add(package);
            }
            return packages;
#endif
        }

        internal static bool SafeMoveResourcesToGroup(AssetBundleAssetSettings settings, AssetGroup targetGroup, List<string> paths, List<string> guids, bool showDialog = true)
        {
            if (targetGroup == null)
            {
                Debug.LogWarning("No valid group to move Resources to");
                return false;
            }

            if (paths == null || paths.Count == 0)
            {
                Debug.LogWarning("No valid Resources found to move");
                return false;
            }

            if (guids == null)
            {
                guids = new List<string>();
                foreach (var p in paths)
                    guids.Add(AssetDatabase.AssetPathToGUID(p));
            }

            Dictionary<string, string> guidToNewPath = new Dictionary<string, string>();

            var message = "Any assets in Resources that you wish to mark as Addressable must be moved within the project. We will move the files to:\n\n";
            for (int i = 0; i < guids.Count; i++)
            {
                var newName = paths[i].Replace("\\", "/");
                newName = newName.Replace("Resources", "Resources_moved");
                newName = newName.Replace("resources", "resources_moved");
                if (newName == paths[i])
                    continue;

                guidToNewPath.Add(guids[i], newName);
                message += newName + "\n";
            }
            message += "\nAre you sure you want to proceed?";
            if (!showDialog || EditorUtility.DisplayDialog("Move From Resources", message, "Yes", "No"))
            {
                settings.MoveAssetsFromResources(guidToNewPath, targetGroup);
                return true;
            }
            return false;
        }

        internal static bool OpenAssetIfUsingVCIntegration(Object target, bool exitGUI = false)
        {
            if (!IsUsingVCIntegration() || target == null)
                return false;
            return OpenAssetIfUsingVCIntegration(AssetDatabase.GetAssetOrScenePath(target), exitGUI);
        }

        internal static bool OpenAssetIfUsingVCIntegration(string path, bool exitGUI = false)
        {
            if (!IsUsingVCIntegration() || string.IsNullOrEmpty(path))
                return false;

            AssetList assets = GetVCAssets(path);
            var uneditableAssets = new List<Asset>();
            string message = "Check out file(s)?\n\n";
            foreach (Asset asset in assets)
            {
                if (!Provider.IsOpenForEdit(asset))
                {
                    uneditableAssets.Add(asset);
                    message += $"{asset.path}\n";
                }
            }

            if (uneditableAssets.Count == 0)
                return false;

            bool openedAsset = true;
            if (EditorUtility.DisplayDialog("Attempting to modify files that are uneditable", message, "Yes", "No"))
            {
                foreach (Asset asset in uneditableAssets)
                {
                    if (!MakeAssetEditable(asset))
                        openedAsset = false;
                }
            }
            else
                openedAsset = false;

            if (exitGUI)
                GUIUtility.ExitGUI();
            return openedAsset;
        }

        internal static AssetList GetVCAssets(string path)
        {
            UnityEditor.VersionControl.Task op = Provider.Status(path);
            op.Wait();
            return op.assetList;
        }

        internal static bool IsUsingVCIntegration()
        {
            return Provider.isActive && Provider.enabled;
        }

        private static bool MakeAssetEditable(Asset asset)
        {
            if (!AssetDatabase.IsOpenForEdit(asset.path))
                return AssetDatabase.MakeEditable(asset.path);
            return false;
        }

    }
}
