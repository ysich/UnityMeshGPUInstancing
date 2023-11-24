/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-07-03 17:18:51
-- 概述:
---------------------------------------------------------------------------------------*/

using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using Onemt.Core.Util;
using Onemt.AddressableAssets;
using System.IO;

namespace OnemtEditor.AssetBundle.Build.Tasks
{
    public class SpliteAssetbundleTask : IBuildTask
    {
        public int Version => 5;

        [InjectContext(ContextUsage.In)]
        IBuildParameters m_Parameters;

        [InjectContext(ContextUsage.In)]
        IBundleBuildContent m_BuildContent;

        public ReturnCode Run()
        {
            //FileHelper.DeleteDirectory(Addressables.buildPathLocal);
            //FileHelper.DeleteDirectory(Addressables.buildPathRemote);
            //FileHelper.ClearDirectory(Addressables.buildPathLocal);
            //FileHelper.ClearDirectory(Addressables.buildPathRemote);
            FileHelper.CreateDirectory(Addressables.buildPathLocal);
            FileHelper.CreateDirectory(Addressables.buildPathRemote);

            var buildParameters = m_Parameters as AssetBundleBuildParameters;
            foreach (var bundle in m_BuildContent.BundleLayout)
            {
                string assetbundleName = bundle.Key;
                string pathAssetbundle = Path.Combine(Addressables.buildPath, assetbundleName);
                string dstFolder = buildParameters.CheckIsRemote(assetbundleName) ? Addressables.buildPathRemote : Addressables.buildPathLocal;
                string pathDst = Path.Combine(dstFolder, assetbundleName);

                byte[] data = File.ReadAllBytes(pathAssetbundle);
                FileStream fs = File.OpenWrite(pathDst);
                fs.Write(data, 0, data.Length);
                fs.Close();
                FileHelper.DeleteFile(pathAssetbundle);
            }

            return ReturnCode.Success;
        }
    }
}
