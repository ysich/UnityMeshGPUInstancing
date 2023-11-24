/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 11:06:53
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Serialization;

namespace Onemt.ResourceManagement.ResourceLocations
{
    [Serializable]
    public class ResourceLocationMap : ScriptableObject, ISerializationCallbackReceiver
    {
        [FormerlySerializedAs("m_Version")]
        [SerializeField]
        private string m_Version;
        public string version => m_Version;

        [FormerlySerializedAs("m_Locations")]
        [SerializeField]
        private List<ResourceLocationBase> m_Locations = new List<ResourceLocationBase>();
        public List<ResourceLocationBase> locations => m_Locations;

        private Dictionary<string, ResourceLocationBase> m_DicABNameMapLocation;
        public Dictionary<string, ResourceLocationBase> abNameMapLocation => m_DicABNameMapLocation;

        private Dictionary<string, ResourceLocationBase> m_DicLocations = new Dictionary<string, ResourceLocationBase>();

        public static ResourceLocationMap Create(string configFolder, string configName)
        {
            return default;
        }

        public void AddLocation(ResourceLocationBase location)
        {
            foreach (var assetName in location.assetNames)
            {
                m_DicLocations[assetName] = location;
                if (!m_Locations.Contains(location))
                    m_Locations.Add(location);
            }
        }

        public void RemoveLocation(string assetName)
        {
            if (m_DicLocations.TryGetValue(assetName, out var location))
            {
                m_DicLocations.Remove(assetName);
                m_Locations.Remove(location);
            }
        }

        /// <summary>
        ///   <para> 获取资源对应的 assetbundle location. </para>
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        public bool TryGetLocation(string assetName, out ResourceLocationBase location)
        {
            return m_DicLocations.TryGetValue(assetName, out location);
        }

        public bool TryGetLocationByABName(string assetbundleName, out ResourceLocationBase location)
        {
            location = default;
            foreach (var loc in m_Locations)
            {
                if (string.Equals(assetbundleName, loc.assetbundleName))
                {
                    location = loc;
                    return true;
                }
            }

            return false;
        }

        public void OnBeforeSerialize()
        { }

        public void OnAfterDeserialize()
        {
            m_DicLocations.Clear();

            m_DicABNameMapLocation = new Dictionary<string, ResourceLocationBase>(m_Locations.Count);


            foreach (var location in m_Locations)
            {
                m_DicABNameMapLocation[location.assetbundleName] = location;

                foreach (var assetName in location.assetNames)
                {
                    m_DicLocations[assetName] = location;
                }

                location.depLocations = new List<IResourceLocation>(location.dependencies.Count);
                foreach (var depName in location.dependencies)
                {
                    foreach (var loc in m_Locations)
                    {
                        if (string.Equals(depName, loc.assetbundleName, StringComparison.Ordinal))
                        {
                            location.depLocations.Add(loc);
                            break;
                        }
                    }
                }
            }
        }

        public HashSet<string> GetAllAssetbundles()
        {
            HashSet<string> assetbundleNames = new HashSet<string>(m_Locations.Count);
            for (int i = 0; i < m_Locations.Count; ++ i)
            {
                assetbundleNames.Add(m_Locations[i].assetbundleName);
            }

            return assetbundleNames;
        }

        //public void RemoveLocationByAssetbundle(string assetbundleName)
        //{
        //    int index = -1;
        //    for (int i = 0; i < m_Locations.Count; ++ i)
        //    {
        //        if (string.Equals(m_Locations[i].assetbundleName, assetbundleName))
        //        {
        //            index = i;
        //            break;
        //        }
        //    }

        //    if (index != -1)
        //        m_Locations.RemoveAt(index);
        //}
        //public void DealDepLocation()
        //{
        //    foreach (var loc in m_DicLocations.Values)
        //    {
        //        loc.depLocations = new List<IResourceLocation>();
        //        foreach (var assetBundleName in loc.dependencies)
        //        {
        //            foreach (var l in m_DicLocations.Values)
        //            {
        //                if (l.assetbundleName == assetBundleName)
        //                {
        //                    loc.depLocations.Add(l);
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //}

        //private bool CheckDicLocations()
        //{
        //    return m_DicLocations != null;
        //}
    }
}
