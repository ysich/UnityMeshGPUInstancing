/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-04-11 14:24:24
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using Onemt.AddressableAssets;
using Onemt.Core.Util;
using Onemt.Core.Define;
using System.IO;
using UnityEditor;
using OnemtEditor.AssetBundle.Build.Utility;
using System.Collections.Generic;
using OnemtEditor.Helper;
using SimpleJSON;

namespace OnemtEditor.AssetBundle.Build.Tasks
{
    public class ExportHotfixTask : IBuildTask
    {
        public int Version => 1;

        private string m_Version;
        private CVersion m_CVersion;

        public ExportHotfixTask(string version)
        {
            m_Version = version;
            m_CVersion = CVersion.ToVersion(version);
        }

        public ReturnCode Run()
        {
            UnityEngine.Debug.LogFormat("开始导出热更版本   Version : {0}", m_Version);

            BuildPatch();

            EditorUtility.UnloadUnusedAssetsImmediate();
            UnityEngine.Debug.LogErrorFormat("完成导出热更版本.");

            return ReturnCode.Success;
        }

        private void BuildPatch()
        {
            string basePath = EditorHelper.GetPath_Base(m_CVersion.GetStrOfBase());
            string updatePath = EditorHelper.GetPath_Update(m_CVersion.GetStrOfBase(), m_Version);
            string md5PathCompress = Path.Combine(Addressables.compressPath, "md5");
            Dictionary<string, ABFileInfo> abFileInfosCompress = ABFileHelper.ReadAbMD5Info(md5PathCompress);

            string[] allBaseMD5Files = FileHelper.IsDirectoryExist(basePath) ? FileHelper.GetAllChildFiles(basePath) : new string[0];
            Dictionary<string, ulong> patchFileSizes = new Dictionary<string, ulong>();
            HashSet<string> invalidPatchs = new HashSet<string>();   // 大小超过ab 60% 而启用的补丁包

            for (int i = 0; i < allBaseMD5Files.Length; ++i)
            {
                var baseMD5Path = allBaseMD5Files[i];
                string baseMD5FileName = Path.GetFileName(baseMD5Path);

                Dictionary<string, ABFileInfo> dicBaseMD5 = ABFileHelper.ReadAbMD5Info(baseMD5Path);
                foreach (var alFileInfoCompress in abFileInfosCompress.Values)
                {
                    string assetbundleName = alFileInfoCompress.assetbundleName;
                    ABFileInfo baseFileInfo;
                    bool isNewBaseAB = true;

                    // 多语言直接下载完整包, 不走patch
#if LUAC_5_3
                    bool isLangAB = assetbundleName.StartsWith(ConstDefine.kLuaPackageName + ConstDefine.kLuaLanguageFlag) ||
                        assetbundleName.StartsWith(ConstDefine.kLuaPackageName + ConstDefine.kLuaLanguageFlag);
#else
                    bool isLangAB = assetbundleName.StartsWith(ConstDefine.kLuaPackageName + "1" + ConstDefine.kLuaLanguageFlag) ||
                        assetbundleName.StartsWith(ConstDefine.kLuaPackageName + "2" + ConstDefine.kLuaLanguageFlag);
#endif

                    if (!isLangAB && !string.Equals(assetbundleName, "shader.unity3d", StringComparison.Ordinal) &&
                        dicBaseMD5.TryGetValue(assetbundleName, out baseFileInfo))
                    {
                        isNewBaseAB = false;

                        alFileInfoCompress.patchMatchVersion = baseFileInfo.patchMatchVersion;
                        alFileInfoCompress.patchABSize = baseFileInfo.patchABSize;
                        alFileInfoCompress.patchFileSize = baseFileInfo.patchFileSize;

                        // 文件是否有变化
                        if (string.Equals(alFileInfoCompress.version, m_Version, StringComparison.Ordinal))
                        {
                            string patchPath = Path.Combine(updatePath, assetbundleName) + baseFileInfo.patchMatchVersion;
                            if (invalidPatchs.Contains(patchPath))
                            {
                                isNewBaseAB = true;
                            }
                            else
                            {
                                if (!patchFileSizes.ContainsKey(patchPath))
                                {
                                    string baseABPath = Path.Combine(EditorHelper.GetPath_UpdateUncompress(m_CVersion.GetStrOfBase(), baseFileInfo.patchMatchVersion), assetbundleName);
                                    string tempABPath = Path.Combine(Addressables.encryptPath, assetbundleName);
                                    BsDiffHelper.DiffFile(baseABPath, tempABPath, patchPath);

                                    ulong patchSize = FileHelper.GetFileSize(patchPath);
                                    string compressBaseABPath = Path.Combine(updatePath, assetbundleName);
                                    ulong compressBaseABSize = FileHelper.GetFileSize(compressBaseABPath);
                                    if (patchSize < compressBaseABSize * 0.6)
                                    {
                                        patchFileSizes[patchPath] = patchSize;
                                        alFileInfoCompress.patchFileSize = patchSize;
                                    }
                                    else
                                    {
                                        FileHelper.DeleteFile(patchPath);
                                        isNewBaseAB = true;
                                        invalidPatchs.Add(patchPath);
                                    }
                                }
                                else
                                {
                                    alFileInfoCompress.patchFileSize = patchFileSizes[patchPath];
                                }
                            }
                        }
                        else
                        {
                            alFileInfoCompress.md5 = baseFileInfo.md5;
                        }
                    }

                    if (isNewBaseAB)
                    {
                        alFileInfoCompress.patchMatchVersion = alFileInfoCompress.version;
                        alFileInfoCompress.patchABSize = alFileInfoCompress.sizeLZ4;
                        alFileInfoCompress.patchFileSize = 0;
                    }
                }

                string updateMD5Path = string.Format("{0}/{1}", updatePath, baseMD5FileName);
                ABFileHelper.SaveABMD5InfoToFile(abFileInfosCompress, updateMD5Path);

                FileHelper.CopyFileTo(updateMD5Path, baseMD5Path);

                //压缩MD5
                string updateMd5PathTmp = updateMD5Path + "tmp";
                GzipHelper.Compress(updateMD5Path, updateMd5PathTmp);
                FileHelper.MoveFileTo(updateMd5PathTmp, updateMD5Path);
            }

            // 写版本文件
            string strVerPath = updatePath + "/version";
            string strNewVerPath = EditorHelper.GetPath_CurVersion() + "/newversion";
            //http://www.onemt.co/wiki/docs/apis/id/2408 请求示例SF
            JSONClass jsonVersion = new JSONClass();
            jsonVersion.Add("v", m_CVersion.ToString());
            jsonVersion.Add("miniV", EditorHelper.version);
            jsonVersion.Add("tipsV", EditorHelper.version);
            jsonVersion.Add("content", "更新公告");
            jsonVersion.Add("switch", new JSONData(0));
            JSONClass jsonData = new JSONClass();
            string strURL = string.Format("http://mactest.onemt.co:88/sf/updatePackage_TEST/{0}/{1}/{2}/",
                EditorHelper.GetPlatformName(), m_CVersion.GetStrOfBase(), m_CVersion.ToString());
            jsonData.Add("url", strURL);
            jsonVersion.Add("data", jsonData);

            string strVersionInfo = jsonVersion.ToString();

            FileHelper.SaveTextToFile(strVersionInfo, strVerPath);
            FileHelper.SaveTextToFile(strVersionInfo, strNewVerPath);
            UnityEngine.Debug.LogFormat("导出当前Version文件路径：{0}, 数据：{1}", strNewVerPath, strVersionInfo);
        }
    }
}
