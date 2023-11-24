/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 15:48:34
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Pipeline.Tasks;
using Onemt.AddressableAssets;
using OnemtEditor.AssetBundle.Settings;
using OnemtEditor.AssetBundle.Build.Tasks;
using OnemtEditor.AssetBundle.Build.Utility;
using Onemt.ResourceManagement.ResourceLocations;
using Onemt.Core.Define;
using Onemt.Core.Util;

namespace OnemtEditor.AssetBundle.Build.DataBuilders
{
    public class BuildScriptPackedMode : BuildScriptBase
    {
        /// <summary>
        ///   <para> assetbundle 打包数据信息. </para>
        ///   <para> bundle name / asset Path / asset name</para>
        /// </summary>
        private List<AssetBundleBuild> m_AllBundleInputs;

        /// <summary>
        ///   <para> 输出的所有 bundle 名称. </para>
        /// </summary>
        private List<string> m_OutputAssetBundleNames;

        /// <summary>
        ///     <para> key -> assetbundle name </para>
        ///     <para> value -> assetgroup guid </para>
        /// </summary>
        /// <param name="buildInput"></param>
        private Dictionary<string, string> m_BundleToAssetGroup;

        struct SBPSettingsOverwriterScope : IDisposable
        {
            bool m_PrevSlimResults;
            public SBPSettingsOverwriterScope(bool forceFullWriteResults)
            {
                m_PrevSlimResults = ScriptableBuildPipeline.slimWriteResults;
                if (forceFullWriteResults)
                    ScriptableBuildPipeline.slimWriteResults = false;
            }

            public void Dispose()
            {
                ScriptableBuildPipeline.slimWriteResults = m_PrevSlimResults;
            }
        }

        /// <summary>
        ///   <para> 初始化构建输入数据. </para>
        ///   <para> 1.处理所有的分组数据 生成AssetBundleBuild. </para>
        ///   <para> 2.lua 脚本处理. (lua、tolua、多语言)</para>
        ///   <para> 2.冗余处理,冗余的资源 都单独打成一个. </para>
        /// </summary>
        /// <param name="buildInput"></param>
        internal void InitializeAssetBundleInput(AddressableDataBuildInput buildInput)
        {
            var aaSettings = buildInput.settings;
            m_AllBundleInputs = new List<AssetBundleBuild>();
            m_OutputAssetBundleNames = new List<string>();
            m_BundleToAssetGroup = new Dictionary<string, string>();

            // 处理所有分组资源
            ProcessAllGroup(aaSettings);

            // 处理 lua 脚本.
            ProcessLua(m_AllBundleInputs);

            // 冗余资源处理
            ProcessRedundant(buildInput, m_AllBundleInputs);
        }

        /// <summary>
        ///   <para> 初始化 AssetBundleBuild </para>
        /// </summary>
        /// <param name="settings"></param>
        protected virtual void ProcessAllGroup(AssetBundleAssetSettings settings)
        {
            for (int i = 0; i < settings.assetGroups.Count; ++i)
            {
                var assetGroup = settings.assetGroups[i];
                ProcessGroup(assetGroup);
            }

            foreach (var assetBundleBuild in m_AllBundleInputs)
            {
                m_OutputAssetBundleNames.Add(assetBundleBuild.assetBundleName);
            }
        }

        protected virtual void ProcessGroup(AssetGroup assetGroup)
        {
            // TODO zm.      打包处理的时候忽略 Built In Data(resources\EditorSceneList) 分组
            if (assetGroup.ignorePacking)
                return;

            // 一个分组打一起 all
            if (assetGroup.packingMode == BundlePackingMode.PackTogether)
            {
                GenerateAssetBundleBuild(assetGroup.assetEntries, m_AllBundleInputs, m_BundleToAssetGroup, assetGroup.guid, "all");
            }
            else  // 分组里面每个asset打一个   使用asset名称
            {
                foreach (var entry in assetGroup.assetEntries)
                {
                    var allEntries = new List<AssetEntry>();
                    allEntries.Add(entry);
                    GenerateAssetBundleBuild(allEntries, m_AllBundleInputs, m_BundleToAssetGroup, assetGroup.guid, entry.assetName);
                }
            }
        }

        protected virtual void ProcessLua(List<AssetBundleBuild> assetBundleBuilds)
        {
            LuaBuilder.Process(assetBundleBuilds);
        }

        internal static void GenerateAssetBundleBuild(List<AssetEntry> entries, List<AssetBundleBuild> assetBundleBuilds,
            Dictionary<string, string> bundleToAssetGroup, string groupGUID, string address)
        {
            List<AssetEntry> assets = new List<AssetEntry>();
            List<AssetEntry> scenes = new List<AssetEntry>();

            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.assetPath))
                    continue;
                if (entry.isScene)
                    scenes.Add(entry);
                else
                    assets.Add(entry);
            }

            string assetBundleName = string.Empty;
            if (scenes.Count > 0)
            {
                assetBundleName = groupGUID + "_scenes_" + address + ConstDefine.kSuffixAssetbundleWithDot;
                assetBundleBuilds.Add(GenerateAssetBundleBuild(scenes, assetBundleName));
            }
            if (assets.Count > 0)
            {
                assetBundleName = groupGUID + "_assets_" + address + ConstDefine.kSuffixAssetbundleWithDot;
                assetBundleBuilds.Add(GenerateAssetBundleBuild(assets, assetBundleName));
            }

            if (!string.IsNullOrEmpty(assetBundleName))
                bundleToAssetGroup[assetBundleName] = groupGUID;
        }

        internal static AssetBundleBuild GenerateAssetBundleBuild(List<AssetEntry> entries, string name)
        {
            AssetBundleBuild assetBundleBuild = new AssetBundleBuild()
            {
                assetBundleName = name.ToLower().Replace(" ", "").Replace("\\", "/").Replace("//", "/"),
                assetNames = entries.Select(s => s.assetPath).ToArray(),
                addressableNames = entries.Select(s => s.assetName).ToArray()
            };

            return assetBundleBuild;
        }


        private Dictionary<ObjectIdentifier, List<AssetEntry>> m_DicAssetEntrys = new Dictionary<ObjectIdentifier, List<AssetEntry>>();
        /// <summary>
        ///   <para> 冗余处理 </para>
        ///   <para> 冗余的资源每个资源都单独打一个assetbundle.</para>
        ///   <para> Note : 必须显式调用一次打包图集，要不图集的include 和 reference 会取不到新增的数据. </para>
        ///    TODO zm.     需要打印相关的数据，便于分析处理。
        /// </summary>
        internal void ProcessRedundant(AddressableDataBuildInput buildInput, List<AssetBundleBuild> assetBundleBuilds)
        {
            var settings = buildInput.settings;
            List<ObjectIdentifier> inBuildObjs = new List<ObjectIdentifier>();
            List<ObjectIdentifier> referenceObjs = new List<ObjectIdentifier>();
            Dictionary<ObjectIdentifier, int> notInBuildObjs = new Dictionary<ObjectIdentifier, int>();
            m_DicAssetEntrys.Clear();

            foreach (var assetGroup in settings.assetGroups)
            {
                foreach (var entry in assetGroup.assetEntries)
                {
                    if (entry.isScene)
                        continue;

                    var assetGUID = entry.guid;
                    GUID.TryParse(assetGUID, out var guid);
                    var buildTarget = buildInput.buildTarget;
                    var includedObjects = ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(guid, buildTarget);
                    inBuildObjs.AddRange(includedObjects);

                    var referenceObjects = ContentBuildInterface.GetPlayerDependenciesForObjects(includedObjects, buildTarget, null);
                    referenceObjs.AddRange(referenceObjects);

                    foreach (var obj in referenceObjects)
                    {
                        if (!m_DicAssetEntrys.TryGetValue(obj, out var assetEntrys))
                        {
                            assetEntrys = new List<AssetEntry>();
                            m_DicAssetEntrys[obj] = assetEntrys;
                        }
                        assetEntrys.Add(entry);
                    }
                }
            }

            foreach (var referenceObj in referenceObjs)
            {
                // TODO zm, 需要处理： 1. Unity 内置的资源 2. Editor 路径下的资源
                var path = AssetDatabase.GUIDToAssetPath(referenceObj.guid.ToString());
                if (AssetBundleUtility.IsPathValidForEntry(path) &&
                    !inBuildObjs.Contains(referenceObj) &&
                    referenceObj.fileType == UnityEditor.Build.Content.FileType.SerializedAssetType)
                {
                    if (!notInBuildObjs.TryGetValue(referenceObj, out var count))
                        notInBuildObjs[referenceObj] = 1;
                    else
                        notInBuildObjs[referenceObj] = count + 1;
                }
            }

            //for (int i = 0; i < inBuildObjs.Count; ++ i)
            //{
            //    var obj = inBuildObjs[i];
            //    var path = AssetDatabase.GUIDToAssetPath(obj.guid.ToString());
            //    UnityEngine.Debug.LogErrorFormat("  Include AssetPath  :   {0}", path);
            //}
            //for (int i = 0; i < referenceObjs.Count; ++i)
            //{
            //    var obj = referenceObjs[i];
            //    var path = AssetDatabase.GUIDToAssetPath(obj.guid.ToString());
            //    UnityEngine.Debug.LogErrorFormat("  Reference AssetPath  :   {0}", path);
            //}

            // 数量大于2 则说明此资源存在冗余.
            foreach (var notInBuildObj in notInBuildObjs)
            {
                if (notInBuildObj.Value < 2)
                    continue;

                var path = AssetDatabase.GUIDToAssetPath(notInBuildObj.Key.guid.ToString());
                //UnityEngine.Debug.LogErrorFormat(" Redundancy AssetPath :   {0}", path);
                var fileName = Path.GetFileNameWithoutExtension(path);
                string name = "dump_" + fileName;

                AssetBundleBuild assetBundleBuild = new AssetBundleBuild()
                {
                    assetBundleName = name.ToLower().Replace(" ", "").Replace("\\", "/").Replace("//", "/") + ConstDefine.kSuffixAssetbundleWithDot,
                    assetNames = new string[] { path },
                    addressableNames = new string[] { fileName }
                };

                assetBundleBuilds.Add(assetBundleBuild);

                UnityEngine.Debug.LogErrorFormat("-----------------------------------------Redundant Start------------------------------------------------------------");
                UnityEngine.Debug.LogErrorFormat("Redundant Asset:             {0}, ", AssetDatabase.GUIDToAssetPath(notInBuildObj.Key.guid));
                foreach (var entry in m_DicAssetEntrys[notInBuildObj.Key])
                {
                    UnityEngine.Debug.LogErrorFormat("                          Include Asset Path:            {0}", entry.assetPath);
                }
                UnityEngine.Debug.LogErrorFormat("-----------------------------------------Redundant End------------------------------------------------------------");
            }
        }

        public override bool CanBuildData<T>()
        {
            return typeof(T).IsAssignableFrom(typeof(AddressableAssetBuildResult));
        }

        protected override TResult BuildDataImplementation<TResult>(AddressableDataBuildInput buildInput)
        {
            TResult result = default(TResult);
            var timer = new Stopwatch();
            timer.Start();

            InitializeAssetBundleInput(buildInput);

            result = DoBuild<TResult>(buildInput);

            if (result != null)
                result.duration = timer.Elapsed.TotalSeconds;

            return result;
        }

        protected virtual TResult DoBuild<TResult>(AddressableDataBuildInput buildInput) where TResult : IDataBuilderResult
        {
            TResult result = default;

            if (m_AllBundleInputs.Count > 0)
            {
                if (!BuildUtility.CheckModifiedScenesAndAskToSave())
                    return AddressableAssetBuildResult.CreateResult<TResult>(null, 0, "Unsaved scenes");

                var buildTarget = buildInput.buildTarget;
                var buildTargetGroup = buildInput.buildTargetGroup;

                var outputPath = Addressables.buildPath;//Path.GetDirectoryName(Application.dataPath) + "/" + Addressables.LibraryPath;

                AssetBundleBuildParameters buildParams = new AssetBundleBuildParameters(buildInput.settings,
                                                                m_BundleToAssetGroup,
                                                                buildTarget,
                                                                buildTargetGroup, outputPath);

                // TODO zm. 在此Task进行后处理
                var buildTasks = GenerateBuildTasks("", "", false);

                ExtractDataTask extractDataTask = new ExtractDataTask();
                buildTasks.Add(extractDataTask);

                IBundleBuildResults results;
                using (m_Log.ScopedStep(LogLevel.Info, "ContentPipeline.BuildAssetBundles"))
                using (new SBPSettingsOverwriterScope(true))
                {
                    var exitCode = ContentPipeline.BuildAssetBundles(buildParams, new BundleBuildContent(m_AllBundleInputs), out results,
                                                buildTasks, new AssetBundleBuildContent(), m_Log);
                }

                result = AddressableAssetBuildResult.CreateResult<TResult>(outputPath, m_BundleToAssetGroup.Count, string.Empty);

                if (string.IsNullOrEmpty(result.error))
                {
                    ProcessLocationMap(extractDataTask.depencyData, m_AllBundleInputs, buildParams);
                }

                {
                    // TODO zm.
                    buildTasks = GenerateBuildTasks("", "");

                    // TODO zm. 临时处理
                    List<AssetBundleBuild> assetBundleBuilds = new List<AssetBundleBuild>();
                    assetBundleBuilds.Add(new AssetBundleBuild()
                    {
                        assetBundleName = "locMap.unity3d",
                        assetNames = new string[] { "Assets/BundleAssets/LocMap/locMap.asset" },
                        addressableNames = new string[] { "locMap" }
                    });
                    ContentPipeline.BuildAssetBundles(buildParams, new BundleBuildContent(assetBundleBuilds), out results,
                                                buildTasks, new AssetBundleBuildContent(), m_Log);
                }

                //SplitRemoteRes(m_AllBundleInputs, buildParams);
            }

            return result;
        }

        //private void SplitRemoteRes(List<AssetBundleBuild> assetBundleBuilds, AssetBundleBuildParameters buildParameters)
        //{
        //    foreach (var assetBundleBuild in assetBundleBuilds)
        //    {
        //        FileHelper.DeleteDirectory(Addressables.buildPathLocal);
        //        FileHelper.DeleteDirectory(Addressables.buildPathRemote);
        //        FileHelper.CreateDirectory(Addressables.buildPathLocal);
        //        FileHelper.CreateDirectory(Addressables.buildPathRemote);
        //        string assetbundleName = assetBundleBuild.assetBundleName;
        //        string pathAssetbundle = Path.Combine(Addressables.buildPath, assetbundleName);
        //        string dstFolder = buildParameters.CheckIsRemote(assetbundleName) ? Addressables.buildPathRemote : Addressables.buildPathLocal;
        //        FileHelper.CopyFileTo(pathAssetbundle, dstFolder);
        //    }
        //}

        /// <summary>
        ///   <para> 生成asset与assetbundle映射关系、assetbundle之间的依赖 locationmap.asset. </para>
        /// </summary>
        private void ProcessLocationMap(IDependencyData dependencyData, List<AssetBundleBuild> assetBundleBuilds, AssetBundleBuildParameters buildParameters)
        {
            var locMap = CreateInstance<ResourceLocationMap>();


            foreach (var assetBundleBuild in assetBundleBuilds)
            {
                List<string> depAssetBundles = GetDependAssetBundles(dependencyData, assetBundleBuild);

                var assetbundleName = assetBundleBuild.assetBundleName;

                // 忽略 lua 和 tolua
                if (assetbundleName.Contains("luaout") || assetbundleName.Contains("toluaout"))
                    continue;

                ResourceLocationBase loc = new ResourceLocationBase(assetbundleName,
                    assetBundleBuild.addressableNames, depAssetBundles);
                if (assetBundleBuild.assetNames[0].EndsWith(".unity"))
                    loc.assetName = assetBundleBuild.assetNames[0];

                // 生成 MD5
                //var path = UnityEngine.Application.dataPath.Replace("Assets", "");
                //string assetbundlePath = Path.Combine(path, Addressables.buildPath + "/" + assetbundleName);
                //loc.md5 = Md5Helper.GetFileMd5(assetbundlePath);

                loc.isRemote = buildParameters.CheckIsRemote(assetbundleName);
                locMap.AddLocation(loc);
            }
            //locMap.DealDepLocation();

            AssetDatabase.CreateAsset(locMap, "Assets/BundleAssets/LocMap/locMap.asset");
            //locMap = AssetDatabase.LoadAssetAtPath<ResourceLocationMap>("Assets/StreamingAssets/locMap.asset");
            EditorUtility.SetDirty(locMap);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        ///   <para> 获取依赖的assetbundle. </para>
        ///   TODO zm.   获取代码需要修改，性能不好.
        /// </summary>
        /// <param name="dependencyData"></param>
        /// <param name="assetBundleBuild"></param>
        /// <returns></returns>
        private List<string> GetDependAssetBundles(IDependencyData dependencyData, AssetBundleBuild assetBundleBuild)
        {
            List<string> assetBundleNames = new List<string>();
            assetBundleNames.Add(assetBundleBuild.assetBundleName);

            foreach (var assetPath in assetBundleBuild.assetNames)
            {
                List<string> depAssetBundleNames = GetDependAssetBundles(dependencyData, assetPath);
                foreach (var name in depAssetBundleNames)
                {
                    if (!assetBundleNames.Contains(name))
                        assetBundleNames.Add(name);
                }
            }

            assetBundleNames.RemoveAt(0);
            return assetBundleNames;
        }

        private List<string> GetDependAssetBundles(IDependencyData dependencyData, string assetPath)
        {
            AssetLoadInfo assetLoadInfo = GetAssetLoadInfo(dependencyData, assetPath);
            if (assetLoadInfo != null)
            {
                List<ObjectIdentifier> referenceObjs = GetReferenceObjs(assetLoadInfo);
                return GetDependAssetBundles(dependencyData, referenceObjs);
            }

            {
                SceneDependencyInfo sceneDependencyInfo = GetSceneDependencyInfoInfo(dependencyData, assetPath);
                List<ObjectIdentifier> referenceObjs = GetSceneReferenceObjs(sceneDependencyInfo);
                return GetDependAssetBundles(dependencyData, referenceObjs);
            }
        }

        public List<string> GetDependAssetBundles(IDependencyData dependencyData, List<ObjectIdentifier> referencedObjects)
        {
            List<string> assetBundleNames = new List<string>();
            foreach (var objIdentifier in referencedObjects)
            {
                foreach (var assetLoadInfo in dependencyData.AssetInfo)
                {
                    if (assetLoadInfo.Value.includedObjects.Contains(objIdentifier))
                    {
                        var assetBundleName = GetAssetBundleName(assetLoadInfo.Value.address);
                        if (string.IsNullOrEmpty(assetBundleName))
                            UnityEngine.Debug.LogErrorFormat("  GetAssetBundleName Error with  assetPath : {0}", assetLoadInfo.Value.address);

                        assetBundleNames.Add(assetBundleName);
                    }
                }
            }

            return assetBundleNames;
        }

        private AssetLoadInfo GetAssetLoadInfo(IDependencyData dependencyData, string assetPath)
        {
            foreach (var assetLoadInfo in dependencyData.AssetInfo)
            {
                var assetName = Path.GetFileNameWithoutExtension(assetPath);
                if (assetLoadInfo.Value.address == assetName)
                    return assetLoadInfo.Value;
            }

            return null;
        }

        private SceneDependencyInfo GetSceneDependencyInfoInfo(IDependencyData dependencyData, string assetPath)
        {
            foreach (var sceneInfo in dependencyData.SceneInfo)
            {
                var sceneName = Path.GetFileNameWithoutExtension(sceneInfo.Value.scene);
                var assetName = Path.GetFileNameWithoutExtension(assetPath);
                if (sceneName == assetName)
                    return sceneInfo.Value;
            }

            return default;
        }

        private string GetAssetBundleName(string assetPath)
        {
            foreach (var assetBundleBuild in m_AllBundleInputs)
            {
                if (assetBundleBuild.addressableNames.Contains(assetPath))
                    return assetBundleBuild.assetBundleName;
            }

            return string.Empty;
        }

        private List<ObjectIdentifier> GetReferenceObjs(AssetLoadInfo assetLoadInfo)
        {
            // var includedObjects = assetLoadInfo.includedObjects.Where(x => x.fileType == UnityEditor.Build.Content.FileType.SerializedAssetType).ToList();
            // var referencedObjects = assetLoadInfo.referencedObjects.Where(x => x.fileType == UnityEditor.Build.Content.FileType.SerializedAssetType).ToList();
            var includedObjects = assetLoadInfo.includedObjects.ToList();
            var referencedObjects = assetLoadInfo.referencedObjects.ToList();
            for (int i = 0; i < includedObjects.Count; ++i)
            {
                referencedObjects.Remove(includedObjects[i]);
            }

            return referencedObjects;
        }

        private List<ObjectIdentifier> GetSceneReferenceObjs(SceneDependencyInfo sceneDepencyInfo)
        {
            // var includedObjects = assetLoadInfo.includedObjects.Where(x => x.fileType == UnityEditor.Build.Content.FileType.SerializedAssetType).ToList();
            // var referencedObjects = assetLoadInfo.referencedObjects.Where(x => x.fileType == UnityEditor.Build.Content.FileType.SerializedAssetType).ToList();

            return sceneDepencyInfo.referencedObjects.ToList();
        }

        public override void ClearCacheData()
        {
            // 移除构建的 assetbundle
            if (Directory.Exists(Addressables.buildPath))
            {
                try
                {
                    Directory.Delete(Addressables.buildPath, true);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }

            //// 移除加密的 assetbundle
            //if (Directory.Exists(Addressables.encryptPath))
            //{
            //    try
            //    {
            //        Directory.Delete(Addressables.encryptPath, true);
            //    }
            //    catch (Exception e)
            //    {
            //        UnityEngine.Debug.LogException(e);
            //    }
            //}

            //// 移除压缩的 assetbundle
            //if (Directory.Exists(Addressables.compressPath))
            //{
            //    try
            //    {
            //        Directory.Delete(Addressables.compressPath, true);
            //    }
            //    catch (Exception e)
            //    {
            //        UnityEngine.Debug.LogException(e);
            //    }
            //}
        }

        internal virtual IList<IBuildTask> GenerateBuildTasks(string builtinShaderBundleName, string monoScriptBundleName, bool encryptAndCompress = true)
        {
            var buildTasks = new List<IBuildTask>();

            // 设置 ： 切换平台、重构图集
            buildTasks.Add(new SwitchToBuildPlatform());
            buildTasks.Add(new RebuildSpriteAtlasCache());

            // Player Scripts
            buildTasks.Add(new BuildPlayerScripts());
            buildTasks.Add(new PostScriptsCallback());

            // Dependency
            buildTasks.Add(new CalculateSceneDependencyData());
            buildTasks.Add(new CalculateAssetDependencyData());
            buildTasks.Add(new StripUnusedSpriteSources());
            //buildTasks.Add(new CreateBuiltInShadersBundle(builtinShaderBundleName));  // 暂时先注释了 TODO zm.
            if (!string.IsNullOrEmpty(monoScriptBundleName))
                buildTasks.Add(new CreateMonoScriptBundle(monoScriptBundleName));
            buildTasks.Add(new PostDependencyCallback());

            // Packing
            buildTasks.Add(new GenerateBundlePacking());
            buildTasks.Add(new UpdateBundleObjectLayout());
            buildTasks.Add(new GenerateBundleCommands());
            buildTasks.Add(new GenerateSubAssetPathMaps());
            buildTasks.Add(new GenerateBundleMaps());
            buildTasks.Add(new PostPackingCallback());

            // Writing
            buildTasks.Add(new WriteSerializedFiles());
            buildTasks.Add(new ArchiveAndCompressBundles());
            buildTasks.Add(new PostWritingCallback());

            buildTasks.Add(new SpliteAssetbundleTask());
            // 移除 encrypt 文件夹中无用 assetbundle 文件
            buildTasks.Add(new RemoveUnusedAssetbundleTask());

            if (encryptAndCompress)
            {
                // 将build文件夹中的 assetbundle 偏移加密到 encrypt 文件夹中
                buildTasks.Add(new EncryptAssetbundleTask());
                // 将encrypt 文件夹中的 assetbundle Gzip压缩到 compress 文件夹中
                buildTasks.Add(new CompressAssetbundleTask());
            }

            return buildTasks;
        }
    }
}
