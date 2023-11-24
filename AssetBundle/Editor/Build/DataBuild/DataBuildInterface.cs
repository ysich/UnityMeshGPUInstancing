/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 15:48:43
-- 概述:
---------------------------------------------------------------------------------------*/

namespace OnemtEditor.AssetBundle.Build.DataBuilders
{
    public interface IDataBuilderResult
    {
        double duration { get; set; }

        int assetbundleCount { get; set; }

        string error { get; set; }

        string outputPath { get; set; }
    }

    public interface IDataBuilder
    {
        string name { get; }

        bool CanBuildData<T>() where T : IDataBuilderResult;

        TResult BuildData<TResult>(AddressableDataBuildInput builderInput) where TResult : IDataBuilderResult;

        void ClearCacheData();
    }
}
