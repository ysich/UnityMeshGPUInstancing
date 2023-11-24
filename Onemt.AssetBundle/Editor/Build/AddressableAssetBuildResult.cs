/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 15:46:14
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using OnemtEditor.AssetBundle.Build.DataBuilders;

namespace OnemtEditor.AssetBundle.Build
{
    public class AddressableAssetBuildResult : IDataBuilderResult
    {
        public double duration { get; set; }

        public int assetbundleCount { get; set; }

        public string error { get; set; }

        public string outputPath { get; set; }

        public static TResult CreateResult<TResult>(string outputPath, int count, string errMessage) where TResult : IDataBuilderResult
        {
            var result = Activator.CreateInstance<TResult>();
            result.outputPath = outputPath;
            result.assetbundleCount = count;
            result.duration = 0;
            result.error = errMessage;

            return result;
        }
    }
}
