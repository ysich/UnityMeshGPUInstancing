///*---------------------------------------------------------------------------------------
//-- 负责人: ming.zhang
//-- 创建时间: 2023-04-12 10:57:36
//-- 概述:
//---------------------------------------------------------------------------------------*/

//using UnityEditor.Build.Pipeline;
//using UnityEditor.Build.Pipeline.Interfaces;
//using Onemt.AddressableAssets;
//using Onemt.Core.Util;
//using System.IO;
//using UnityEngine;
//using OnemtEditor.AssetBundle.Build.Utility;
//using System.Collections.Generic;
//using OnemtEditor.Helper;

//namespace OnemtEditor.AssetBundle.Build.Tasks
//{
//    public class UploadAssetbundleTask : IBuildTask
//    {
//        public int Version => 1;

//        private string m_Version;
//        private CVersion m_CVersion;

//        public UploadAssetbundleTask(string version)
//        {
//            m_Version = version;
//            m_CVersion = CVersion.ToVersion(version);
//        }

//        public ReturnCode Run()
//        {
//            string updatePath = EditorHelper.GetPath_Update(m_CVersion.GetStrOfBase(), m_Version);
//            string updateUncompressPath = EditorHelper.GetPath_UpdateUncompress(m_CVersion.GetStrOfBase(), m_Version);
//            if (FileHelper.IsDirectoryExist(updatePath))
//            {
//                string name = updatePath + "_" + System.DateTime.Now.ToString("yyyyMMdd_HHmm");
//                FileHelper.RenameDirectory(updatePath, name);
//            }

//            FileHelper.CreateDirectory(updatePath);
//            FileHelper.DeleteDirectory(updateUncompressPath);

//            string compressPath = Addressables.compressPath;
//            string encryptPath = Addressables.encryptPath;
//            string md5Path = Path.Combine(Application.streamingAssetsPath, "md5");

//            Dictionary<string, ABFileInfo> abFileInfos = ABFileHelper.ReadAbMD5Info(md5Path);

//            // 更新 streaming 中的md5， 上传热更 assetbundle 完整资源
//            foreach (var pair in abFileInfos)
//            {
//                string assetbundleName = pair.Key;
//                var fileInfo = pair.Value;
//                // 如果 stream 路径中的md5 信息里面  version 为 new，则说明这个文件
//                //      1. 新增加
//                //      2. md5 值与原来的不一致(文件变化)
//                if (string.Equals(fileInfo.version, "new") ||
//                    string.Equals(fileInfo.version, m_Version))
//                {
//                    // 拷贝stream路径中的assetbundle 到update中
//                    fileInfo.version = m_Version;
//                    string streamABPath = Path.Combine(compressPath, assetbundleName);
//                    string updateABPath = Path.Combine(updatePath, assetbundleName);
//                    FileHelper.CopyFileTo(streamABPath, updateABPath);

//                    // 拷贝temp未压缩的 assetbundle 到updateuncompress中
//                    string tempABPath = Path.Combine(encryptPath, assetbundleName);
//                    string updateUncompressABPath = Path.Combine(updateUncompressPath, assetbundleName);
//                    FileHelper.CopyFileTo(tempABPath, updateUncompressABPath);
//                }
//            }

//            // 保存最新的 md5 到stream路径下
//            ABFileHelper.SaveABMD5InfoToFile(abFileInfos, md5Path);

//            return ReturnCode.Success;
//        }
//    }
//}
