/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-04-04 09:20:31
-- 概述:
        lua 脚本打包处理：
            1. jit 加密(Only lua 5.1)
            2. lua、tolua、多语言 脚本打包分组
---------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Onemt.Core.Define;
using OnemtEditor.Define;
using Onemt.Core.Util;
using System.IO;
using System.Text.RegularExpressions;
using OnemtEditor.Helper;

namespace OnemtEditor.AssetBundle.Build.DataBuilders
{
    public class LuaBuilder
    {
        private static string pathLua = string.Format("{0}/{1}", Application.dataPath, ConstDefine.kPathLua);
        private static string pathTolua = string.Format("{0}/{1}", Application.dataPath, ConstDefine.kPathTolua);
        private static string pathTemp = string.Format("{0}/{1}", Application.dataPath, EditorDefine.kPathTemp);

        private static List<string> s_LuaFileNames;

        public static void Process(List<AssetBundleBuild> assetbundleBuilds)
        {
            s_LuaFileNames = new List<string>();

#if LUAC_5_3
            CopyLuaFile();
            Import(assetbundleBuilds);
#else
            BuildJit();
            Import(assetbundleBuilds);
#endif // LUAC_5_3

        }

#if LUAC_5_3
        private static void CopyLuaFile()
        {
            FileHelper.DeleteDirectory(pathTemp);
            FileHelper.CreateDirectory(pathTemp);

            string pathTempLua = Path.Combine(pathTemp, ConstDefine.kPathLua);
            var files = FileHelper.GetAllChildFiles(pathLua, "lua");
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var dstPath = Path.Combine(pathTempLua, fileName + ".bytes");
                FileHelper.CopyFileTo(file, dstPath);
                //AssetDatabase.ImportAsset(dstPath);
            }

            string pathTempTolua = Path.Combine(pathTemp, ConstDefine.kPathTolua);
            files = FileHelper.GetAllChildFiles(pathTolua, "lua");
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var dstPath = Path.Combine(pathTempTolua, fileName + ".bytes");
                FileHelper.CopyFileTo(file, dstPath);
                //AssetDatabase.ImportAsset(dstPath);
            }

            AssetDatabase.Refresh();
        }

        private static void Import(List<AssetBundleBuild> assetbundleBuilds)
        {
            string tempPath = string.Format("Assets/{0}", EditorDefine.kPathTemp);

            // 1. tolua import
            string pathTempTolua = Path.Combine(tempPath, ConstDefine.kPathTolua);
            ImportFolder(pathTempTolua, ConstDefine.kToluaPackageName, assetbundleBuilds);

            // 2. lua import
            string pathTempLua = Path.Combine(tempPath, ConstDefine.kPathLua);
            ImportFolder(pathTempLua, ConstDefine.kLuaPackageName, assetbundleBuilds);

            // 3. language import
            string languageFolder = string.Format("Assets/{0}/language", EditorDefine.kPathTemp);
            FileHelper.DeleteDirectory(languageFolder);
            FileHelper.CreateDirectory(languageFolder);
            string[] paths = Directory.GetFiles(pathTemp, "*", SearchOption.AllDirectories);
            Regex reg = new Regex(@"language_[0-9a-zA-Z]+_(\w+).lua.bytes");    //language_local_en.lua.bytes
            foreach (var path in paths)
            {
                string fileName = Path.GetFileName(path);
                Match match = reg.Match(fileName);
                if (match.Success)
                {
                    string strNewPath = Path.Combine(languageFolder, fileName);
                    Debug.LogFormat("oldPath:{0},newPath:{1}", path, strNewPath);
                    if (!FileHelper.FileExists(path))
                    {
                        Debug.LogFormat("文件不存在：{0}", path);
                    }
                    FileHelper.MoveFileTo(path, strNewPath);
                    AssetDatabase.ImportAsset(strNewPath);
                    string lang = match.Groups[1].Value;
                    string abName = ConstDefine.kLuaPackageName + ConstDefine.kLuaLanguageFlag + lang;
                    ImportSingleFile(strNewPath, abName, assetbundleBuilds);
                    Debug.LogFormat("{0} 设置语言配置ab：{1}", path, abName);
                }
            }
        }
#else
        private static void BuildJit()
        {
            FileHelper.DeleteDirectory(pathTemp);
            FileHelper.CreateDirectory(pathTemp);

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows)
            {
                BuildJit(pathLua, pathTemp, 0, false);
                BuildJit(pathTolua, pathTemp, 0, true);
            }
            else
            {
                for (int arch = 1; arch < 3; ++ arch)
                {
                    BuildJit(pathLua, pathTemp, arch, false);
                    BuildJit(pathTolua, pathTemp, arch, true);
                }
            }

            AssetDatabase.Refresh();
        }

        private static void Import(List<AssetBundleBuild> assetbundleBuilds)
        {
            for (int arch = 0; arch < 3; ++ arch)
            {
                string tempPath = string.Format("Assets/{0}/{1}{2}/{3}", EditorDefine.kPathTemp, ConstDefine.kLuajitDir, arch, ConstDefine.kToluaPackageName);
                if (!Directory.Exists(tempPath))
                    continue;

                // 1. tolua import
                ImportFolder(tempPath, ConstDefine.kToluaPackageName + arch, assetbundleBuilds);

                tempPath = string.Format("Assets/{0}/{1}{2}/{3}", EditorDefine.kPathTemp, ConstDefine.kLuajitDir, arch, ConstDefine.kLuaPackageName);
                string languageFolder = string.Format("Assets/{0}/language_{1}", EditorDefine.kPathTemp, arch);
                if (!Directory.Exists(tempPath))
                    continue;

                // 2. lua import
                ImportFolder(tempPath, ConstDefine.kLuaPackageName + arch, assetbundleBuilds);

                // 3. language import
                string[] paths = Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories);
                Regex reg = new Regex(@"game_Data_Table_language_[0-9a-zA-Z]+_(\w+).lua.bytes");    //game_Data_Table_language_local_en.lua.bytes
                foreach (var path in paths)
                {
                    string fileName = Path.GetFileName(path);
                    Match match = reg.Match(fileName);
                    if (match.Success)
                    {
                        string strNewPath = languageFolder + fileName;
                        Debug.LogFormat("oldPath:{0},newPath:{1}", path, strNewPath);
                        if (!FileHelper.FileExists(path))
                        {
                            Debug.LogFormat("文件不存在：{0}", path);
                        }
                        FileHelper.MoveFileTo(path, strNewPath);
                        AssetDatabase.ImportAsset(strNewPath);
                        string lang = match.Groups[1].Value;
                        string abName = ConstDefine.kLuaPackageName + arch + ConstDefine.kLuaLanguageFlag + lang;
                        ImportSingleFile(strNewPath, abName, assetbundleBuilds);
                        Debug.LogFormat("{0} 设置语言配置ab：{1}", path, abName);
                    }
                }
            }

            AssetDatabase.Refresh();
        }

        private static void BuildJit(string orgPath, string dstPath, int arch, bool tolua)
        {
            dstPath = string.Format("{0}/{1}", dstPath, ConstDefine.kLuajitDir);

            string command = "python3";
            string args = string.Empty;
            if (tolua)
                args = string.Format("{0}/Tools/LuaJit/builder.py {1} {2}{3}/{4} {3}", Directory.GetCurrentDirectory(), orgPath, dstPath, arch, ConstDefine.kToluaPackageName);
            else
                args = string.Format("{0}/Tools/LuaJit/builder.py {1} {2}{3}/{4} {3}", Directory.GetCurrentDirectory(), orgPath, dstPath, arch, ConstDefine.kLuaPackageName);

            ShellHelper.ProcessCommandEx(command, args);
        }
#endif  // LUAC_5_3


        private static void ImportFolder(string path, string assetbundleName, List<AssetBundleBuild> assetbundleBuilds)
        {
            List<string> assetPaths = new List<string>();
            List<string> assetNames = new List<string>();
            string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (file.EndsWith(".meta"))
                    continue;

                assetPaths.Add(file);
                string fileName = Path.GetFileNameWithoutExtension(file);
                assetNames.Add(fileName);

                if (s_LuaFileNames.Contains(fileName))
                    UnityEngine.Debug.LogErrorFormat("Error Repeat Lua File: {0}", fileName);
                else
                    s_LuaFileNames.Add(fileName);
            }

            if (assetPaths.Count > 0)
            {
                assetbundleBuilds.Add(new AssetBundleBuild()
                {
                    assetBundleName = assetbundleName + ConstDefine.kSuffixAssetbundleWithDot,
                    //assetBundleVariant = ".unity3d",
                    assetNames = assetPaths.ToArray(),
                    addressableNames = assetNames.ToArray()
                });
            }
        }

        private static void ImportSingleFile(string path, string assetbundleName, List<AssetBundleBuild> assetbundleBuilds)
        {
            if (File.Exists(path))
            {
                assetbundleBuilds.Add(new AssetBundleBuild()
                {
                    assetBundleName = assetbundleName + ConstDefine.kSuffixAssetbundleWithDot,
                    //assetBundleVariant = ".unity3d",
                    assetNames = new string[] { path },
                    addressableNames = new string[] { Path.GetFileNameWithoutExtension(path) },
                });

                var fileName = Path.GetFileNameWithoutExtension(path);
                if (s_LuaFileNames.Contains(fileName))
                    UnityEngine.Debug.LogErrorFormat("Error Repeat Lua File: {0}", fileName);
                else
                    s_LuaFileNames.Add(fileName);
            }
        }
    }
}
