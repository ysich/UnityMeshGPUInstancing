/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-04-10 16:15:25
-- 概述:
        将构建出来的Build路径下的 assetbundle 加密到 Encrypt 路径下。
---------------------------------------------------------------------------------------*/

using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using Onemt.AddressableAssets;
using Onemt.Core.Util;
using Onemt.Core.Define;
using System.IO;
using System;

namespace OnemtEditor.AssetBundle.Build.Tasks
{
    public class EncryptAssetbundleTask : IBuildTask
    {
        public int Version => 5;

        public ReturnCode Run()
        {
            EncryptDir(Addressables.buildPathLocal, Addressables.encryptPathLocal);
            EncryptDir(Addressables.buildPathRemote, Addressables.encryptPathRemote);

            return ReturnCode.Success;
        }

        private void EncryptDir(string orgDir, string dstDir)
        {
            FileHelper.DeleteDirectory(dstDir);
            FileHelper.CreateDirectory(dstDir);

            string[] files = FileHelper.GetAllChildFiles(orgDir, ConstDefine.kSuffixAssetbundle);
            for (int i = 0; i < files.Length; i++)
            {
                string filePath = files[i];
                EncryptBundle(filePath, orgDir, dstDir);
            }
        }

        private void EncryptBundle(string path, string orgDir, string dstDir)
        {
            byte[] data = File.ReadAllBytes(path);
            int offset = ConstDefine.kAssetbundleOffset;
            int resultLength = offset + data.Length;
            byte[] dstData = new byte[resultLength];

            Array.Copy(data, 0, dstData, 0, offset);
            Array.Copy(data, 0, dstData, offset, data.Length);
            string dstPath = path.Replace(orgDir, dstDir);
            FileHelper.DeleteFile(dstPath);
            FileStream fs = File.OpenWrite(dstPath);
            fs.Write(dstData, 0, resultLength);
            fs.Close();
        }
    }
}
