/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 15:46:27
-- 概述:
---------------------------------------------------------------------------------------*/

using OnemtEditor.AssetBundle.Settings;
using OnemtEditor.AssetBundle.Build.DataBuilders;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor;

namespace OnemtEditor.AssetBundle.Build
{
    public class AddressableDataBuildInput
    {
        internal IBuildLogger m_Log;
        public IBuildLogger log { get => m_Log; }

        public BuildTarget buildTarget { get; set; }

        public BuildTargetGroup buildTargetGroup { get; set; }

        public FileRegistry registry { get; private set; }

        public AssetBundleAssetSettings settings;

        public AddressableDataBuildInput(AssetBundleAssetSettings settings)
        {
            if (settings == null)
                UnityEngine.Debug.LogErrorFormat("Attempting to set up AddressableDataBuildInput with null settings.");

            SetAllValues(settings,
                BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget),
                EditorUserBuildSettings.activeBuildTarget);
        }

        internal void SetAllValues(AssetBundleAssetSettings settings, BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            registry = new FileRegistry();
            this.settings = settings;
            this.buildTargetGroup = buildTargetGroup;
            this.buildTarget = buildTarget;
        }
    }
}
