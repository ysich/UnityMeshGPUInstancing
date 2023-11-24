/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 14:51:18
-- 概述:
        AssetEntry:
                assetName : 资源名称，assetbundle 模式在加载资源所使用的名称
                assetPath : 资源路径，Assets/...
                guid
                isScene :  是否为场景
 Note:
        1. Resources 文件夹中的asset
        2. 构建中的 场景列表
---------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.IO;
using UnityEditor;
using UnityEngine.U2D;
using System.Runtime.InteropServices;

namespace OnemtEditor.AssetBundle.Settings
{
    interface IAssetEntryData
    {
        string assetName { get; }
        string assetPath { get; }
        bool isInResources { get; }
        bool isScene { get; }
    }

    [Serializable]
    public class AssetEntry : IAssetEntryData
    {
        public const string kResourceName = "Resources";
        public const string kResourcePath = "*/Resources/";
        public const string kEditorSceneListName = "EditorSceneList";

        [FormerlySerializedAs("m_AssetPath")]
        [SerializeField]
        private string m_AssetPath;
        /// <summary>
        ///   <para> 资源路径. Assets/... </para>
        /// </summary>
        public string assetPath
        {
            get
            {
                if (string.IsNullOrEmpty(m_AssetPath))
                {
                    if (string.IsNullOrEmpty(guid))
                        SetAssetPath(string.Empty);
                    else
                        SetAssetPath(AssetDatabase.GUIDToAssetPath(guid));
                    // else if (guid == kEditorSceneListName)
                    //     SetAssetPath(kEditorSceneListName);
                    // else if (guid == kResourceName)
                    //     SetAssetPath(kResourceName);

                }

                return m_AssetPath;
            }
        }

        [FormerlySerializedAs("m_AssetName")]
        [SerializeField]
        private string m_AssetName;
        /// <summary>
        ///   <para> 资源名称， 从assetbundle 中加载资源时使用. </para>para>
        /// </summary>
        public string assetName
        {
            get
            {
                if (string.IsNullOrEmpty(m_AssetName))
                    m_AssetName = Path.GetFileNameWithoutExtension(assetPath);

                if (string.IsNullOrEmpty(m_AssetName))
                    m_AssetName = m_GUID;

                return m_AssetName;
            }
        }

        [FormerlySerializedAs("m_GUID")]
        [SerializeField]
        private string m_GUID;
        public string guid => m_GUID;

        /// <summary>
        ///   <para> 是否为场景。 </para>
        /// </summary>
        private bool m_IsScene;
        public bool isScene
        {
            get
            {
                m_IsScene = assetPath.EndsWith(".unity");
                return m_IsScene;
            }
        }

        /// <summary>
        ///   <para> 设置路径. </para>
        ///   <para> 主要用于容错处理, 更新资源路径. </para>
        /// </summary>
        /// <param name="path"></param>
        public void SetCachedPath(string path)
        {
            if (m_AssetPath != path)
            {
                m_AssetPath = path;
                m_MainAsset = null;
                parentGroup.SetDirty(ModificationEvent.EntryModified, null, true, false);
            }
        }

        private UnityEngine.Object m_MainAsset;
        public UnityEngine.Object mainAsset
        {
            get
            {
                if (m_MainAsset == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_MainAsset, out string guid, out long localId))
                {
                    AssetEntry e = this;
                    while (string.IsNullOrEmpty(e.assetPath))
                    {
                        if (e.parentEntry == null)
                            return null;
                        e = e.parentEntry;
                    }

                    m_MainAsset = AssetDatabase.LoadMainAssetAtPath(e.assetPath);
                }

                return m_MainAsset;
            }
        }

        internal Type m_AssetType = null;
        public Type assetType
        {
            get
            {
                if (m_AssetType == null)
                {
                    m_AssetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                    if (m_AssetType == null)
                        return typeof(object);
                }

                return m_AssetType;
            }
        }

        [NonSerialized]
        private AssetGroup m_ParentGroup;
        public AssetGroup parentGroup { get => m_ParentGroup; set => m_ParentGroup = value; }

        public AssetEntry parentEntry { get; set; }

        /// <summary>
        ///   <para> 是否为文件夹. </para>
        /// </summary>
        private bool m_IsFolder;
        internal bool isFolder { get => m_IsFolder; set => m_IsFolder = value; }

        [SerializeField]
        [FormerlySerializedAs("m_ReadOnly")]
        private bool m_ReadOnly;
        public bool readOnly { get => m_ReadOnly; set => m_ReadOnly = value; }

        /// <summary>
        ///   <para> 是否为 AssetEntry 的子 entry. </para>
        /// </summary>
        [SerializeField]
        [FormerlySerializedAs("m_IsSubAsset")]
        private bool m_IsSubAsset;
        public bool isSubAsset { get => m_IsSubAsset; set => m_IsSubAsset = value; }

        /// <summary>
        ///   <para> 是否在 构建的场景列表中. </para>
        /// </summary>
        [FormerlySerializedAs("m_IsInSceneList")]
        [SerializeField]
        private bool m_IsInSceneList;
        public bool isInSceneList { get => m_IsInSceneList; set => m_IsInSceneList = value; }

        [NonSerialized]
        private List<AssetEntry> m_ChildAssetEntries;
        public List<AssetEntry> childAssetEntries { get => m_ChildAssetEntries; set => m_ChildAssetEntries = value; }

        [FormerlySerializedAs("m_IsInResources")]
        [SerializeField]
        private bool m_IsInResources;
        public bool isInResources
        {
            get => m_IsInResources;
            set
            {
                m_IsInResources = value;
            }
        }

        public string bundleFileId { get; set; }

        internal Type m_CachedMainAssetType = null;
        internal Type mainAssetType
        {
            get
            {
                if (m_CachedMainAssetType == null)
                {
                    m_CachedMainAssetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                    if (m_CachedMainAssetType == null)
                        return typeof(object); // do not cache a bad type lookup.
                }
                return m_CachedMainAssetType;
            }
        }

        /// <summary>
        ///   <para> 目标资源. </para>
        ///   <para> 用于在编辑器状态下选中entry, 编辑器指引到Project窗口中. </para>
        /// </summary>
        private UnityEngine.Object m_TargetAsset;
        public UnityEngine.Object targetAsset
        {
            get
            {
                if (m_TargetAsset == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_TargetAsset, out string guid, out long localId))
                {
                    if (!string.IsNullOrEmpty(assetPath) || !isSubAsset)
                    {
                        m_TargetAsset = mainAsset;
                        return m_TargetAsset;
                    }

                    if (parentEntry == null || !string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(parentEntry.assetPath))
                        return null;

                    var asset = parentEntry.mainAsset;
                    if (ExtractKeyAndSubKey(assetName, out string mainKey, out string subObjectName))
                    {
                        if (asset != null && asset.GetType() == typeof(SpriteAtlas))
                        {
                            m_TargetAsset = (asset as SpriteAtlas).GetSprite(subObjectName);
                            return m_TargetAsset;
                        }

                        var subObjects = AssetDatabase.LoadAllAssetRepresentationsAtPath(parentEntry.assetPath);
                        foreach (var s in subObjects)
                        {
                            if (s != null && s.name == subObjectName)
                            {
                                m_TargetAsset = s;
                                break;
                            }
                        }
                    }
                }
                return m_TargetAsset;
            }
        }

        public AssetEntry(string guid, string assetName, AssetGroup parent, bool readOnly)
        {
            m_GUID = guid;
            m_AssetName = assetName;
            m_ParentGroup = parent;
            m_ReadOnly = readOnly;
        }

        internal bool ExtractKeyAndSubKey(object keyObj, out string mainKey, out string subKey)
        {
            var key = keyObj as string;
            if (key != null)
            {
                var i = key.IndexOf('[');
                if (i > 0)
                {
                    var j = key.LastIndexOf(']');
                    if (j > i)
                    {
                        mainKey = key.Substring(0, i);
                        subKey = key.Substring(i + 1, j - (i + 1));
                        return true;
                    }
                }
            }
            mainKey = null;
            subKey = null;
            return false;
        }

        internal void SetSubObjectType(Type type)
        {
            m_CachedMainAssetType = type;
        }

        /// <summary>
        ///   <para> 设置 assetName. </para>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="postEvent"></param>
        public void SetAssetName(string name, bool postEvent = true)
        {
            if (m_AssetName == name) return;

            m_AssetName = name;
            if (string.IsNullOrEmpty(m_AssetName))
                m_AssetName = assetPath;

            if (m_GUID.Length > 0 && m_AssetName.Contains("[") && m_AssetName.Contains("]"))
                Debug.LogErrorFormat("AssetName '{0}' cannot contain '[ ]'.", m_AssetName);
            SetDirty(ModificationEvent.EntryModified, this, postEvent);
        }


        public void SetAssetPath(string path)
        {
            if (m_AssetPath != path)
            {
                m_AssetPath = path;
                m_AssetType = null;
                m_MainAsset = null;
            }
        }

        /// <summary>
        ///   <para> 用于: 1. 更新配置 2. 窗口刷新</para>
        /// </summary>
        /// <param name="e"></param>
        /// <param name="o"></param>
        /// <param name="postEvent"></param>
        internal void SetDirty(ModificationEvent e, object o, bool postEvent)
        {
            if (parentGroup != null)
                parentGroup.SetDirty(e, o, postEvent, true);
        }

        /// <summary>
        ///   <para> 收集此 entry 关联的所有 assetentry. </para>
        /// </summary>
        /// <param name="assets"></param>
        /// <param name="includeSelf"></param>
        /// <param name="recurseAll"></param>
        /// <param name="includeSubObjects"></param>
        /// <param name="entryFilter"></param>
        public void GatherAllAssets(List<AssetEntry> assets, bool includeSelf, bool recurseAll,
             bool includeSubObjects, Func<AssetEntry, bool> entryFilter = null)
        {
            if (assets == null)
                assets = new List<AssetEntry>();

            if (guid == kEditorSceneListName)
            {
                GatherEditorSceneEntries(assets, entryFilter);
            }
            else if (guid == kResourceName)
            {
                GatherResourcesEntries(assets, recurseAll, entryFilter);
            }
            else
            {
                if (string.IsNullOrEmpty(assetPath))
                    return;

                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    isFolder = true;
                    GatherFolderEntries(assets, recurseAll, entryFilter);
                    childAssetEntries = assets;
                }
                else
                {
                    if (includeSelf)
                        if (entryFilter == null || entryFilter(this))
                            assets.Add(this);
                    if (includeSubObjects)
                    {
                        var mainType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                        if (mainType == typeof(SpriteAtlas))
                        {
                            GatherSpriteAtlasEntries(assets, assetPath);
                        }
                        else
                        {
                            GatherSubObjectEntries(assets, assetPath);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///   <para> 收集指定路径的 子assetEntry. </para>
        /// </summary>
        /// <param name="assets"></param>
        /// <param name="path"></param>
        void GatherSubObjectEntries(List<AssetEntry> assets, string path)
        {
            var objs = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
            for (int i = 0; i < objs.Length; i++)
            {
                var o = objs[i];
                var namedAddress = string.Format("{0}[{1}]", assetName, o == null ? "missing reference" : o.name);
                if (o == null)
                {
                    if (string.IsNullOrEmpty(assetPath) && isSubAsset)
                        path = parentEntry.assetPath;
                    Debug.LogWarning(string.Format("NullReference in entry {0}\nAssetPath: {1}\nAddressableAssetGroup: {2}",
                        assetName, path, parentGroup.name));
                    assets.Add(new AssetEntry("", namedAddress, parentGroup, true));
                }
                else
                {
                    //var newEntry = parentGroup.assetBundleAssetSettings.CreateEntry(namedAddress, parentGroup, true);
                    var newEntry = parentGroup.assetBundleAssetSettings.CreateEntry("", namedAddress, parentGroup, true);
                    newEntry.isSubAsset = true;
                    newEntry.parentEntry = this;
                    newEntry.isInResources = isInResources;
                    newEntry.SetSubObjectType(o.GetType());
                    assets.Add(newEntry);
                }
            }
        }

        /// <summary>
        ///   <para> 收集图集的所有 assetentry. </para>
        /// </summary>
        /// <param name="assets"></param>
        /// <param name="path"></param>
        void GatherSpriteAtlasEntries(List<AssetEntry> assets, string path)
        {
            var settings = parentGroup.assetBundleAssetSettings;
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(assetPath);
            var sprites = new Sprite[atlas.spriteCount];
            atlas.GetSprites(sprites);

            for (int i = 0; i < atlas.spriteCount; i++)
            {
                var spriteName = sprites[i] == null ? "missing reference" : sprites[i].name;
                if (sprites[i] == null)
                {
                    if (string.IsNullOrEmpty(assetPath) && isSubAsset)
                        path = parentEntry.assetPath;
                    Debug.LogWarning(string.Format("NullReference in entry {0}\nAssetPath: {1}\nAddressableAssetGroup: {2}",
                        assetName, path, parentGroup.name));
                    assets.Add(new AssetEntry("", spriteName, parentGroup, true));
                }
                else
                {
                    if (spriteName.EndsWith("(Clone)"))
                        spriteName = spriteName.Replace("(Clone)", "");

                    var namedAddress = string.Format("{0}[{1}]", assetName, spriteName);
                    var newEntry = settings.CreateEntry("", namedAddress, parentGroup, true);
                    newEntry.isSubAsset = true;
                    newEntry.parentEntry = this;
                    newEntry.isInResources = isInResources;
                    assets.Add(newEntry);
                }
            }
        }

        /// <summary>
        ///   <para> 收集文件夹中所有的Entry. </para>
        /// </summary>
        /// <param name="assets"></param>
        /// <param name="recurseAll"></param>
        /// <param name="entryFilter"></param>
        void GatherFolderEntries(List<AssetEntry> assets, bool recurseAll, Func<AssetEntry, bool> entryFilter)
        {
            var path = assetPath;
            var settings = parentGroup.assetBundleAssetSettings;
            foreach (var file in AddressablesFileEnumeration.EnumerateAddressableFolder(path, settings, recurseAll))
            {
                var subGuid = AssetDatabase.AssetPathToGUID(file);
                var entry = settings.CreateSubEntryIfUnique(subGuid, assetName + GetRelativePath(file, path), this);

                if (entry != null)
                {
                    entry.isInResources = isInResources; //if this is a sub-folder of Resources, copy it on down
                    if (entryFilter == null || entryFilter(entry))
                        assets.Add(entry);
                }
            }

            if (!recurseAll)
            {
                foreach (var fo in Directory.EnumerateDirectories(path, "*.*", SearchOption.TopDirectoryOnly))
                {
                    var folder = fo.Replace('\\', '/');
                    if (AssetDatabase.IsValidFolder(folder))
                    {
                        var entry = settings.CreateSubEntryIfUnique(AssetDatabase.AssetPathToGUID(folder), assetName + GetRelativePath(folder, path), this);
                        if (entry != null)
                        {
                            entry.isInResources = isInResources; //if this is a sub-folder of Resources, copy it on down
                            entry.isFolder = true;
                            if (entryFilter == null || entryFilter(entry))
                                assets.Add(entry);
                        }
                    }
                }
            }
        }

        string GetRelativePath(string file, string path)
        {
            return file.Substring(path.Length);
        }

        void GatherEditorSceneEntries(List<AssetEntry> assets, Func<AssetEntry, bool> entryFilter)
        {
            var settings = parentGroup.assetBundleAssetSettings;
            foreach (var s in BuiltinSceneCache.scenes)
            {
                if (s.enabled)
                {
                    var entry = settings.CreateSubEntryIfUnique(s.guid.ToString(), Path.GetFileNameWithoutExtension(s.path), this);
                    if (entry != null) //TODO - it's probably really bad if this is ever null. need some error detection
                    {
                        entry.isInSceneList = true;
                        if (entryFilter == null || entryFilter(entry))
                            assets.Add(entry);
                    }
                }
            }
        }

        internal void GatherResourcesEntries(List<AssetEntry> assets, bool recurseAll, Func<AssetEntry, bool> entryFilter)
        {
            var settings = parentGroup.assetBundleAssetSettings;
            foreach (var resourcesDir in GetResourceDirectories())
            {
                foreach (var file in Directory.GetFiles(resourcesDir, "*.*", recurseAll ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                {
                    if (AssetBundleUtility.IsPathValidForEntry(file))
                    {
                        var g = AssetDatabase.AssetPathToGUID(file);
                        var addr = GetResourcesPath(file);
                        var entry = settings.CreateSubEntryIfUnique(g, addr, this);

                        if (entry != null) //TODO - it's probably really bad if this is ever null. need some error detection
                        {
                            entry.isInResources = true;
                            if (entryFilter == null || entryFilter(entry))
                                assets.Add(entry);
                        }
                    }
                }

                if (!recurseAll)
                {
                    foreach (var folder in Directory.GetDirectories(resourcesDir))
                    {
                        if (AssetDatabase.IsValidFolder(folder))
                        {
                            var entry = settings.CreateSubEntryIfUnique(AssetDatabase.AssetPathToGUID(folder), GetResourcesPath(folder), this);
                            if (entry != null) //TODO - it's probably really bad if this is ever null. need some error detection
                            {
                                entry.isInResources = true;
                                if (entryFilter == null || entryFilter(entry))
                                    assets.Add(entry);
                            }
                        }
                    }
                }
            }
        }

        static string GetResourcesPath(string path)
        {
            path = path.Replace('\\', '/');
            int ri = path.ToLower().LastIndexOf("/resources/");
            if (ri >= 0)
                path = path.Substring(ri + "/resources/".Length);
            int i = path.LastIndexOf('.');
            if (i > 0)
                path = path.Substring(0, i);
            return path;
        }

        static IEnumerable<string> GetResourceDirectories()
        {
            string[] resourcesGuids = AssetDatabase.FindAssets("Resources", new string[] { "Assets", "Packages" });
            foreach (string resourcesGuid in resourcesGuids)
            {
                string resourcesAssetPath = AssetDatabase.GUIDToAssetPath(resourcesGuid);
                if (resourcesAssetPath.EndsWith("/resources", StringComparison.OrdinalIgnoreCase) && AssetDatabase.IsValidFolder(resourcesAssetPath) && Directory.Exists(resourcesAssetPath))
                {
                    yield return resourcesAssetPath;
                }
            }

            //UnityEditor.PackageManager.Requests.ListRequest req = AssetBundleUtility.RequestPackageListAsync();

            //foreach (string path in GetResourceDirectoriesatPath("Assets"))
            //{
            //    yield return path;
            //}
            //List<UnityEditor.PackageManager.PackageInfo> packages = AssetBundleUtility.GetPackages(req);
            //foreach (UnityEditor.PackageManager.PackageInfo package in packages)
            //{
            //    foreach (string path in GetResourceDirectoriesatPath(package.assetPath))
            //    {
            //        yield return path;
            //    }
            //}
        }

        static IEnumerable<string> GetResourceDirectoriesatPath(string rootPath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                foreach (string dir in Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories))
                {
                    if (dir.EndsWith("/resources", StringComparison.OrdinalIgnoreCase))
                        yield return dir;
                }
            }
            else
            {
                foreach (string dir in Directory.EnumerateDirectories(rootPath, "Resources", SearchOption.AllDirectories))
                    yield return dir;
            }
        }
    }
}
