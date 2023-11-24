/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 10:33:38
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using UnityEngine;

namespace Onemt.ResourceManagement.ResourceLocations
{
    [SerializeField]
    public interface IResourceLocation
    {
        string assetName { get; set; }

        string assetbundleName { get; } 

        List<string> assetNames { get; }

        List<string> dependencies { get; }

        bool hasDependencies { get; }

        List<IResourceLocation> depLocations { get; set; }

        int Hash();
    }
}
