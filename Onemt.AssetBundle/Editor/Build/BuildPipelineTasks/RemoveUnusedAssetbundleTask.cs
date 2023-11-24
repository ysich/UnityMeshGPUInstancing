/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-04-10 14:58:12
-- 概述:
         移除 encrypt 文件夹中废弃的 assetbundle
            : 上次打包存在，但此次打包不存在
        // TODO zm.  是否直接清空encry文件夹，不需要比对移除
---------------------------------------------------------------------------------------*/

using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using Onemt.AddressableAssets;
using Onemt.Core.Util;
using UnityEditor.Build.Pipeline.Injector;
using Onemt.Core.Define;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace OnemtEditor.AssetBundle.Build.Tasks
{
    public class RemoveUnusedAssetbundleTask : IBuildTask
    {
        [InjectContext]
        IBundleBuildResults m_Results;

        private HashSet<string> m_Names;

        public int Version { get { return 1; } }

        public ReturnCode Run()
        {
            // 获取此次打包的所有 assetbundle 名字集合
            m_Names = new HashSet<string>();
            foreach (var val in m_Results.BundleInfos.Values)
            {
                m_Names.Add(val.FileName);
            }

            ClearLocal();
            ClearRemote();

            return ReturnCode.Success;
        }

        private void ClearLocal()
        {
            if (!FileHelper.IsDirectoryExist(Addressables.encryptPathLocal))
                return;

            string strCurrPath = System.Environment.CurrentDirectory;
            strCurrPath = strCurrPath.Replace("\\", "/") + "/";
            string[] files = FileHelper.GetAllChildFiles(Addressables.encryptPathLocal, ConstDefine.kSuffixAssetbundleWithDot);
            foreach (var file in files)
            {
                string name = Path.GetFileName(file);
                string strAssetPath = file.Replace(strCurrPath, "");
                if (!m_Names.Contains(name))
                    AssetDatabase.DeleteAsset(strAssetPath);
            }
        }

        private void ClearRemote()
        {
            if (!FileHelper.IsDirectoryExist(Addressables.encryptPathRemote))
                return;

            string strCurrPath = System.Environment.CurrentDirectory;
            strCurrPath = strCurrPath.Replace("\\", "/") + "/";
            string[] files = FileHelper.GetAllChildFiles(Addressables.encryptPathRemote, ConstDefine.kSuffixAssetbundleWithDot);
            foreach (var file in files)
            {
                string name = Path.GetFileName(file);
                string strAssetPath = file.Replace(strCurrPath, "");
                if (!m_Names.Contains(name))
                    AssetDatabase.DeleteAsset(strAssetPath);
            }
        }
    }
}
