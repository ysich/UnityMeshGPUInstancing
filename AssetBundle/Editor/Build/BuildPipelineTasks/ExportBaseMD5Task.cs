///*---------------------------------------------------------------------------------------
//-- 负责人: ming.zhang
//-- 创建时间: 2023-04-11 15:35:10
//-- 概述:
//        导出工程时才 进行的task
//---------------------------------------------------------------------------------------*/

//using UnityEditor.Build.Pipeline;
//using UnityEditor.Build.Pipeline.Interfaces;
//using Onemt.Core.Util;
//using Onemt.Core.Define;
//using System.IO;
//using UnityEngine;
//using OnemtEditor.AssetBundle.Build.Utility;
//using System.Collections.Generic;
//using OnemtEditor.Helper;

//namespace OnemtEditor.AssetBundle.Build.Tasks
//{
//    public class ExportBaseMD5Task : IBuildTask
//    {
//        public int Version => 1;

//        private CVersion m_CVersion;

//        public ExportBaseMD5Task(string version)
//        {
//            m_CVersion = CVersion.ToVersion(version);
//        }

//        public ReturnCode Run()
//        {
//            string basePath = EditorHelper.GetPath_Base(m_CVersion.GetStrOfBase());
//            Directory.CreateDirectory(basePath);
//            string streamMD5Path = Path.Combine(Application.streamingAssetsPath, ConstDefine.kNameMD5);
//            string baseMD5Path = string.Format("{0}/{1}_{2}", basePath, ConstDefine.kNameMD5, m_CVersion.GetIdxValue(2));

//            Dictionary<string, ABFileInfo> newMd5Dic = ABFileHelper.ReadAbMD5Info(streamMD5Path);
//            foreach (KeyValuePair<string, ABFileInfo> keyval in newMd5Dic)
//            {
//                ABFileInfo abInfo = keyval.Value;
//                abInfo.patchMatchVersion = abInfo.version;
//                abInfo.patchABSize = abInfo.sizeCompress;
//                abInfo.patchFileSize = 0;
//            }
//            ABFileHelper.SaveABMD5InfoToFile(newMd5Dic, streamMD5Path);

//            FileHelper.CopyFileTo(streamMD5Path, baseMD5Path);
//            //备份
//            FileHelper.CopyFileTo(streamMD5Path, baseMD5Path.Replace("base", "base_origin"));

//            UnityEngine.Debug.LogFormat("导出base目录下的md5,from:{0}, to:{1}", streamMD5Path, baseMD5Path);

//            return ReturnCode.Success;
//        }
//    }
//}
