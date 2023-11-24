/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 15:49:02
-- 概述:
---------------------------------------------------------------------------------------*/

using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;

namespace OnemtEditor.AssetBundle.Build.Tasks
{
    public class ExtractDataTask : IBuildTask
    {
        public int Version { get { return 1; } }

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IDependencyData m_DependencyData;

        [InjectContext(ContextUsage.In)]
        IBundleWriteData m_WriteData;

        [InjectContext(ContextUsage.In)]
        IBuildCache m_BuildCache;

        [InjectContext(ContextUsage.In)]
        internal IBuildContext m_BuildContext;
#pragma warning restore 649

        public IDependencyData depencyData { get => m_DependencyData; }
        public IBundleWriteData writeData { get => m_WriteData; }
        public IBuildCache buildCache { get => m_BuildCache; }
        public IBuildContext buildContext { get => m_BuildContext; }


        public ReturnCode Run()
        {
            return ReturnCode.Success;
        }
    }
}
