/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-04-10 16:42:21
-- 概述:
        1. 清理compress文件夹中无用的ab资源
        2.
            i. 未开启压缩模式，直接将enctypt拷贝到compress文件夹中
            ii.开启压缩模式，
---------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Onemt.AddressableAssets;
using Onemt.Core.Define;
using Onemt.Core.Util;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;

namespace OnemtEditor.AssetBundle.Build.Tasks
{
    public class CompressAssetbundleTask : IBuildTask
    {
        public int Version => 1;

        /// <summary>
        ///   <para> {0}  -> 文件名称. </para>
        ///   <para> {1}  -> md5 值. </para>
        ///   <para> {2}  -> lz4原文件大小. </para>
        ///   <para> {3}  -> lzma gip 压缩文件大小. </para>
        ///   <para> {4}  -> 补丁匹配的assetbundle 文件大小(sizeLz4). </para>
        ///   <para> {5}  -> 是否远程资源
        /// </summary>
        private const string kMD5Format = "{0}:{1}:{2}:{3}:new:0:{4}:0:{5}\n";

        public ReturnCode Run()
        {
            string encryptPath = Addressables.encryptPath;
            string encryptPathLocal = Addressables.encryptPathLocal;
            string encryptPathRemote = Addressables.encryptPathRemote;
            string compressPath = Addressables.compressPath;
            string compressPathLocal = Addressables.compressPathLocal;
            string compressPathRemote = Addressables.compressPathRemote;


            string encryptMD5Path = Path.Combine(encryptPath, "md5");
            string compressMD5Path = Path.Combine(compressPath, "md5");

            FileHelper.CreateDirectory(compressPath);

            // 移除compress 文件夹下 废弃的assetbundle文件
            FileHelper.RidOfUnNecessaryFile(encryptPath, compressPath);

            // 未开启压缩模式
            if (!AppConfigSetting.EnableCompress())
            {
                UnityEngine.Debug.LogError("!!!!!!!!!!!!!!    UnCompress");

                FileHelper.DeleteDirectory(compressPathLocal);
                FileHelper.CreateDirectory(compressPathLocal);
                FileHelper.CopyDirectory(encryptPathLocal, compressPathLocal);
                FileHelper.DeleteDirectory(compressPathRemote);
                FileHelper.CreateDirectory(compressPathRemote);
                FileHelper.CopyDirectory(encryptPathRemote, compressPathRemote);

                StringBuilder sb = GenNoCompressMd5Text(encryptPathLocal, encryptPathRemote);

                // 保存最新的md5到temp路径下
                FileHelper.SaveTextToFile(sb.ToString(), encryptMD5Path);

                ExportEnctyptMD5ToCompressPath();
            }
            else
            {
                Dictionary<string, ABFileInfo> abFileInfosEncrypt = ABFileHelper.ReadAbMD5Info(encryptMD5Path);
                if (abFileInfosEncrypt.Count == 0) // encrypt 文件夹下不存在md5文件.  -> 第一次打包
                    FileHelper.DeleteDirectory(compressPath);

                StringBuilder sb = new StringBuilder();
                //string[] files = FileHelper.GetAllChildFiles(encryptPath, ConstDefine.kSuffixAssetbundle);

                string[] localFiles = FileHelper.GetAllChildFiles(encryptPathLocal, ConstDefine.kSuffixAssetbundle);
                string[] remoteFiles = FileHelper.GetAllChildFiles(encryptPathRemote, ConstDefine.kSuffixAssetbundle);
                string[] files = new string[localFiles.Length + remoteFiles.Length];
                Array.Copy(localFiles, files, localFiles.Length);
                Array.Copy(remoteFiles, 0, files, localFiles.Length, remoteFiles.Length);

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                ThreadParam param = new ThreadParam();
                param.pathSrc = encryptPath;
                param.pathDst = compressPath;
                param.files = files;
                param.index = 0;
                param.abFileInfos = abFileInfosEncrypt;
                param.sb = sb;
                param.lockd = new object();
                param.buildPathRemote = Addressables.buildPathRemote;

                List<Thread> threads = new List<Thread>();
                for (int i = 0; i < SystemInfo.processorCount; ++i)
                {
                    Thread thread = new Thread(new ParameterizedThreadStart(OnThreadCompress));
                    thread.Start(param);
                    threads.Add(thread);
                }

                // 循环等待 压缩完成.
                while (true)
                {
                    EditorUtility.DisplayProgressBar("压缩中...", string.Format("{0}/{1}", param.index, param.files.Length), Mathf.InverseLerp(0, param.files.Length, param.index));
                    bool hasAlive = false;
                    foreach (Thread thread in threads)
                    {
                        if (thread.IsAlive)
                        {
                            hasAlive = true;
                            break;
                        }
                    }

                    if (!hasAlive)
                    {
                        break;
                    }

                    Thread.Sleep(10);
                }

                UnityEngine.Debug.LogFormat("-->Compress UseTime: {0}s", stopwatch.ElapsedMilliseconds * 0.001f);

                // 保存最新的 md5 到temp路径下.
                FileHelper.SaveTextToFile(sb.ToString(), encryptMD5Path);

                ExportEnctyptMD5ToCompressPath();
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();

            return ReturnCode.Success;
        }

        private StringBuilder GenNoCompressMd5Text(string pathLocal, string pathRemote)
        {
            string[] localFiles = FileHelper.GetAllChildFiles(pathLocal, ConstDefine.kSuffixAssetbundle);
            string[] remoteFiles = FileHelper.GetAllChildFiles(pathRemote, ConstDefine.kSuffixAssetbundle);
            string[] files = new string[localFiles.Length + remoteFiles.Length];
            Array.Copy(localFiles, files, localFiles.Length);
            Array.Copy(remoteFiles, 0, files, localFiles.Length, remoteFiles.Length);
            StringBuilder sb = new StringBuilder();

            foreach (string file in files)
            {
                string md5 = Md5Helper.GetFileMd5(file);
                string shortPath = Path.GetFileName(file);
                ulong rawSize = FileHelper.GetFileSize(file);
                bool isRemote = Array.IndexOf(remoteFiles, file) != -1;
                sb.AppendFormat(kMD5Format, shortPath, md5, rawSize, rawSize, rawSize, isRemote);
            }
            return sb;
        }

        /// <summary>
        ///   <para> 导出 md5 文件到 Compress 路径下. </para>
        /// </summary>
        private void ExportEnctyptMD5ToCompressPath()
        {
            string encryptMD5Path = Path.Combine(Addressables.encryptPath, "md5");
            string compressMD5Path = Path.Combine(Addressables.compressPath, "md5");

            Dictionary<string, ABFileInfo> abFileInfosEncrypt = ABFileHelper.ReadAbMD5Info(encryptMD5Path);
            Dictionary<string, ABFileInfo> abFileInfosCompress = ABFileHelper.ReadAbMD5Info(compressMD5Path);

            // 修改compress 中的abfileinfo 信息 ：  1. 新增 2.md5不一致
            foreach (var infoEncrypt in abFileInfosEncrypt.Values)
            {
                string assetbundleName = infoEncrypt.assetbundleName;

                // 1. 新增 2.md5不一致
                if (!abFileInfosCompress.TryGetValue(assetbundleName, out var infoOld) ||
                    infoEncrypt.md5 != infoOld.md5)
                    abFileInfosCompress[assetbundleName] = infoEncrypt;
            }

            // 移除无用的 assetbundle 
            List<string> removeABNames = new List<string>();
            foreach (var pair in abFileInfosCompress)
            {
                if (!abFileInfosEncrypt.ContainsKey(pair.Key))
                    removeABNames.Add(pair.Key);
            }

            foreach (var name in removeABNames)
            {
                abFileInfosCompress.Remove(name);
            }

            ABFileHelper.SaveABMD5InfoToFile(abFileInfosCompress, compressMD5Path);
        }

        private void OnThreadCompress(object arg)
        {
            ThreadParam param = arg as ThreadParam;
            while (true)
            {
                string file = "";
                string assetbundleName = "";
                string md5;

                lock (param.lockd)
                {
                    if (param.index >= param.files.Length)
                    {
                        // 完成
                        break;
                    }
                    file = param.files[param.index];
                    assetbundleName = file.Replace(param.pathSrc, "").Substring(1);
                    if (assetbundleName.StartsWith(ConstDefine.kPathLocal))
                        assetbundleName = assetbundleName.Replace(ConstDefine.kPathLocal, "").Substring(1);
                    else if (assetbundleName.StartsWith(ConstDefine.kPathRemote))
                        assetbundleName = assetbundleName.Replace(ConstDefine.kPathRemote, "").Substring(1);

                    ++param.index;
                }

                md5 = Md5Helper.GetFileMd5(file);
                string fileDst = file.Replace(param.pathSrc, param.pathDst);
                if (!IsSameFile(assetbundleName, md5, param.abFileInfos))    // 文件 md5值不一致才进行压缩处理. 1.ab文件修改 2.新增ab文件
                {
                    string strRst = Compress(file, fileDst);
                    if (!string.IsNullOrEmpty(strRst))
                    {
                        lock (param.strLog)
                        {
                            param.strLog += strRst; param.strLog += ";\n";
                        }
                    }
                }
                ulong sizeLz4 = FileHelper.GetFileSize(file);
                ulong sizeCompress = FileHelper.GetFileSize(fileDst);
                lock (param.sb)
                {
                    string path = Path.Combine(param.buildPathRemote, assetbundleName);
                    bool isRemote = FileHelper.FileExists(path);
                    param.sb.AppendFormat(kMD5Format, assetbundleName, md5, sizeLz4, sizeCompress, sizeLz4, isRemote);
                }
            }
        }

        static bool IsSameFile(string assetbundleName, string md5, Dictionary<string, ABFileInfo> fileInfos)
        {
            if (!fileInfos.TryGetValue(assetbundleName, out var abInfo))
                return false;

            return string.Equals(abInfo.md5, md5);
        }

        /// <summary>
        ///   <para> 压缩文件. </para>
        /// </summary>
        /// <param name="fileSrc"></param>
        /// <param name="fileDst"></param>
        /// <returns></returns>
        private string Compress(string fileSrc, string fileDst)
        {
            FileHelper.CreateDirectoryFromFile(fileDst);
            FileHelper.DeleteFile(fileDst);

            //调用liblzma
            try
            {
                GzipHelper.Compress(fileSrc, fileDst);
            }
            catch (System.Exception e)
            {
                return e.Message;
            }

            return null;
        }

        class ThreadParam
        {
            public string pathSrc;
            public string pathDst;

            public string[] files;
            public int index;
            public StringBuilder sb;
            public Dictionary<string, ABFileInfo> abFileInfos;

            public object lockd;
            public string strLog;

            public string buildPathRemote;
        }
    }
}
