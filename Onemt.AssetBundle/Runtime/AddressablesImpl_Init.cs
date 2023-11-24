/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-04-03 16:26:31
-- 概述:
---------------------------------------------------------------------------------------*/

using UnityEngine;
using Onemt.Framework.Config;
using Onemt.ResourceManagement.ResourceProviders;
using System.Reflection;
using Onemt.ResourceManagement.ResourceLocations;
using System.IO;
using Onemt.Core.Define;
using Onemt.Core.Util;
using System.Collections.Generic;

namespace Onemt.AddressableAssets
{
    public partial class AddressablesImpl
    {
        private ResourceLocationMap m_ResLocationMap;
        public ResourceLocationMap locationMap => m_ResLocationMap;

        private Dictionary<string, ABFileInfo> m_ABFileInfos;

        private ResourceLocationMap m_ResLocationMapOld;

        public void Initialization()
        {
            if (GameConfig.instance.enableAssetBundle)
                InitRuntime();
            else
            {
#if UNITY_EDITOR
                InitEditor();
#else
                InitRuntime();
#endif // UNITY_EDITOR
            }

            m_OnHandleCompleteAction = OnHandleCompleted;
            m_OnSceneHandleCompleteAction = OnSceneHandleCompleted;
            m_OnHandleDestroyedAction = OnHandleDestroyed;

            m_SceneProvider = new SceneProvider();
            m_InstanceProvider = new InstanceProvider();
        }

#if UNITY_EDITOR
        private void InitEditor()
        {
            if (!GameConfig.instance.enableAssetBundle)
                InitEditorNormal();
            else
                InitEditorAssetBundle();
        }

        /// <summary>
        ///   <para> 编辑器 database 模式. </para>
        ///   <para> 根据 assetbundleAssetSettings 生成ResLocationMap. </para>
        /// </summary>
        private void InitEditorNormal()
        {
            var assembly = Assembly.Load("Assembly-CSharp-Editor");
            var settingsType = assembly.GetType("OnemtEditor.AssetBundle.Settings.AssetBundleAssetSettings");
            var settingsPath = "Assets/AssetBundleAssetData/AssetBundleAssetSettings.asset";
            var settingsSetupMethod = settingsType.GetMethod("CreateLocationMap", BindingFlags.Instance | BindingFlags.NonPublic);
            var assetBundleAssetSettingsObj = UnityEditor.AssetDatabase.LoadAssetAtPath(settingsPath, settingsType);
            m_ResLocationMap = (ResourceLocationMap)settingsSetupMethod.Invoke(assetBundleAssetSettingsObj, null);
        }
#endif // UNITY_EDITOR

        /// <summary>
        ///   <para> 非编辑器下 assetbundle 模式. </para>
        /// </summary>
        private void InitRuntime()
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(runtimePath, ConstDefine.kNameLocMap), 0, ConstDefine.kAssetbundleOffset);
            m_ResLocationMap = assetBundle.LoadAsset<ResourceLocationMap>("locMap");
            assetBundle.Unload(true);
            m_ABFileInfos = ABFileHelper.ReadAbMD5Info(Path.Combine(runtimePath, ConstDefine.kNameMD5));
            //m_ResLocationMap.DealDepLocation();
        }

        /// <summary>
        ///   <para> 编辑器 assetbundle 模式. </para>
        /// </summary>
        private void InitEditorAssetBundle()
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(runtimePath, ConstDefine.kNameLocMap), 0, ConstDefine.kAssetbundleOffset);
            m_ResLocationMap = assetBundle.LoadAsset<ResourceLocationMap>("locMap");
            //m_ResLocationMap.DealDepLocation();
        }






        //public void Init(List<ITask> taskList, CompleteCallback callback = null, System.Action initCompleteCallback = null)
        //{
        //    string locMapPathOld = Path.Combine(GameConfig.instance.persistentAssetPath, ConstDefine.kNameLocMap);
        //    string locMapPathNew = Path.Combine(GameConfig.instance.persistentDataPath, ConstDefine.kNameLocMap);
        //    // data 下不存在旧的 md5 文件，清理所有文件
        //    //  1. 新安装包 不存在旧的 md5 文件  2. 新包所有资源都在本地
        //    if (!FileHelper.FileExists(locMapPathOld) || !GameConfig.instance.enableRemoteRes)
        //    {
        //        Debugger.Log("不存在旧版 配置 文件");
        //        FileHelper.DeleteDirectory(GameConfig.instance.persistentAssetPath);
        //        FileHelper.DeleteDirectory(GameConfig.instance.persistentBasePath);
        //        FileHelper.DeleteDirectory(GameConfig.instance.persistentTempPath);
        //    }

        //    GameApp.instance.StartCoroutine(Clear(locMapPathOld, locMapPathNew, taskList, callback, initCompleteCallback));
        //}

        //public IEnumerator Clear(string locMapPathOld, string locMapPathNew, List<ITask> taskList,
        //    CompleteCallback taskCompleteCallback, System.Action initCompleteCallback)
        //{
        //    // TODO zm.
        //    AssetBundle assetBundleOld = AssetBundle.LoadFromFile(locMapPathOld);
        //    m_ResLocationMapOld = assetBundleOld.LoadAsset("locMap") as ResourceLocationMap;
        //    AssetBundle assetBundleNew = AssetBundle.LoadFromFile(locMapPathNew);
        //    m_ResLocationMap = assetBundleNew.LoadAsset("locMap") as ResourceLocationMap;

        //    var abMapLocationsNew = m_ResLocationMap.abNameMapLocation;
        //    var abMapLocationsOld = m_ResLocationMapOld.abNameMapLocation;

        //    foreach (var oldLocation in abMapLocationsOld.Values)
        //    {
        //        string oldAbFileName = oldLocation.assetbundleName;
        //        string dataPath = Path.Combine(GameConfig.instance.persistentDataPath, oldAbFileName);
        //        string basePath = Path.Combine(GameConfig.instance.persistentBasePath, oldAbFileName);
        //        string tempPath = Path.Combine(GameConfig.instance.persistentTempPath, oldAbFileName);
        //        string tempPatchPath = tempPath + ConstDefine.kSuffixPatch;

        //        string tempPathCache = tempPath + ConstDefine.kSuffixDownload;
        //        string tempPatchPathCache = tempPatchPath + ConstDefine.kSuffixDownload;

        //        bool deleteBaseFile = false;
        //        bool deleteTempFile = false;
        //        bool deleteDataFile = false;
        //        bool deleteTempPatchFile = false;

        //        // 新包与本地的 assetbundle 相同
        //        if (abMapLocationsNew.TryGetValue(oldAbFileName, out var newLocation) &&
        //            string.Compare(oldLocation.md5, newLocation.md5, StringComparison.Ordinal) > 0)
        //        {
        //            // 最终文件不存在，且为远程资源。 则看是否已经下载好但是未进行解压、合并处理的
        //            if (!FileHelper.FileExists(dataPath) &&
        //                newLocation.isRemote)
        //            {
        //                // TODO zm.
        //                // Note : 后续是否需要校验 temp 和 patch 文件.
        //                if (FileHelper.FileExists(tempPath))
        //                {
        //                    if (oldLocation.HavePatch())
        //                    {
        //                        // TODO zm.  匿名函数处理
        //                        // patch 文件存在则直接 解压压缩、合并
        //                        if (FileHelper.FileExists(tempPatchPath))
        //                            taskList.Add(new UnpackLzmaAndMergeTask(tempPath, dataPath, basePath, tempPatchPath, newLocation.md5, (code, msg, bytes) =>
        //                            {
        //                                if (FileHelper.FileExists(tempPath))
        //                                    FileHelper.DeleteFile(tempPath);

        //                                if (FileHelper.FileExists(tempPatchPath))
        //                                    FileHelper.DeleteFile(tempPatchPath);

        //                                taskCompleteCallback?.Invoke(code, msg, bytes);
        //                            }));
        //                    }
        //                    else
        //                        // TODO zm.  匿名函数处理
        //                        taskList.Add(new UnpackLzmaTask(tempPath, dataPath, newLocation.md5, (code, msg, bytes) =>
        //                        {
        //                            if (FileHelper.FileExists(tempPath))
        //                                FileHelper.DeleteFile(tempPath);

        //                            taskCompleteCallback?.Invoke(code, msg, bytes);
        //                        }));
        //                }
        //            }
        //            else if (FileHelper.FileExists(dataPath))
        //            {
        //                // TODO zm. 为什么使用 size，而不是md5校验
        //                var fileSize = FileHelper.GetFileSize(dataPath);
        //                if (fileSize != newLocation.sizeCompress)
        //                {
        //                    FileHelper.DeleteFile(dataPath);

        //                    // 资源存在，但是校验失败
        //                    if (FileHelper.FileExists(tempPath))
        //                    {
        //                        if (oldLocation.HavePatch())
        //                        {
        //                            // patch 文件存在则直接 重压缩、合并
        //                            if (FileHelper.FileExists(tempPatchPath))
        //                            {
        //                                taskList.Add(new UnpackLzmaAndMergeTask(tempPath, dataPath, basePath, tempPatchPath, newLocation.md5, (code, msg, bytes) =>
        //                                {
        //                                    if (FileHelper.FileExists(tempPath))
        //                                        FileHelper.DeleteFile(tempPath);

        //                                    if (FileHelper.FileExists(tempPatchPath))
        //                                        FileHelper.DeleteFile(tempPatchPath);

        //                                    taskCompleteCallback?.Invoke(code, msg, bytes);
        //                                }));
        //                            }
        //                        }
        //                        else
        //                        {
        //                            taskList.Add(new UnpackLzmaTask(tempPath, dataPath, newLocation.md5, (code, msg, bytes) =>
        //                            {
        //                                if (FileHelper.FileExists(tempPath))
        //                                    FileHelper.DeleteFile(tempPath);

        //                                taskCompleteCallback?.Invoke(code, msg, bytes);
        //                            }));
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    deleteTempFile = true;
        //                    deleteTempPatchFile = true;
        //                }
        //            }
        //        }
        //        // 移除 assetbundle 相关的所有文件
        //        // 1. 新的配置文件不存在 assetbundle 文件   2. 新的与旧的 assetbundld md5 值不一致
        //        else
        //        {
        //            deleteDataFile = true;
        //            deleteBaseFile = true;
        //            deleteTempFile = true;
        //            deleteTempPatchFile = true;
        //        }

        //        string log = string.Empty;
        //        if (deleteDataFile)
        //        {
        //            if (FileHelper.TryDeleteFile(dataPath))
        //                log += "data 目录文件, ";
        //        }
        //        if (deleteBaseFile)
        //        {
        //            if (FileHelper.TryDeleteFile(basePath))
        //                log += "base 目录文件, ";
        //        }
        //        if (deleteTempFile)
        //        {
        //            if (FileHelper.TryDeleteFile(tempPathCache))
        //                log += ".temp 文件, ";
        //        }
        //        if (deleteTempPatchFile)
        //        {
        //            if (FileHelper.TryDeleteFile(tempPatchPathCache))
        //                log += ".tempPatch 文件, ";
        //        }

        //        if (!string.IsNullOrEmpty(log))
        //            Debugger.Log("移除过期 ab 文件 : {0}    assetbundle name : {1}", log, oldAbFileName);
        //    }

        //    initCompleteCallback?.Invoke();
        //    yield return null;
        //}
    }
}
