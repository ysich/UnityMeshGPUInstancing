/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 15:48:24
-- 概述:
---------------------------------------------------------------------------------------*/

using UnityEngine;
using UnityEditor.Build.Pipeline.Interfaces;
using System;
using UnityEditor.Build.Pipeline.Utilities;
using System.IO;
using Onemt.AddressableAssets;

namespace OnemtEditor.AssetBundle.Build.DataBuilders
{
    public class BuildScriptBase : ScriptableObject, IDataBuilder
    {
        [NonSerialized]
        internal IBuildLogger m_Log;
        public IBuildLogger log { get => m_Log; }

        public new virtual string name { get => "Undefined"; }

        internal static void WriteBuildLog(BuildLog log, string directory)
        {
            // Directory.CreateDirectory(directory);
            // PackageManager.PackageInfo info = PackageManager.PackageInfo.FindForAssembly(typeof(BuildScriptBase).Assembly);
            // log.AddMetaData(info.name, info.version);
            // File.WriteAllText(Path.Combine(directory, "AddressablesBuildTEP.json"), log.FormatForTraceEventProfiler());
        }

        public virtual bool CanBuildData<T>() where T : IDataBuilderResult
        {
            return false;
        }

        public TResult BuildData<TResult>(AddressableDataBuildInput builderInput) where TResult : IDataBuilderResult
        {
            if (!CanBuildData<TResult>())
            {
                string errMessage = string.Format("Data Builder  {0}    Cannot build requested type : {1}", name, typeof(TResult));
                UnityEngine.Debug.LogError(errMessage);
                return AddressableAssetBuildResult.CreateResult<TResult>(null, 0, errMessage);
            }

            m_Log = builderInput.log != null ? builderInput.log : new BuildLog();

            TResult result = default;

            using (m_Log.ScopedStep(LogLevel.Warning, $"Build   {this.name}"))
            {
                try
                {
                    result = BuildDataImplementation<TResult>(builderInput);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e.Message);
                    return AddressableAssetBuildResult.CreateResult<TResult>(null, 0, e.Message);
                }
            }

            if (builderInput.log == null && m_Log != null)
                WriteBuildLog((BuildLog)m_Log, Path.Combine(Path.GetDirectoryName(Application.dataPath), Addressables.kLibraryPath));

            return result;
        }

        protected virtual TResult BuildDataImplementation<TResult>(AddressableDataBuildInput buildInput) where TResult : IDataBuilderResult
        {
            return default(TResult);
        }

        public virtual void ClearCacheData()
        { }
    }
}
