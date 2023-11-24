/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 14:52:05
-- 概述:
        contains editor data for the assetbundle system.
        目前只包含所有 AssetGroup 数据信息.    后续需要添加其他相关打包设置数据信息.
---------------------------------------------------------------------------------------*/

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using OnemtEditor.AssetBundle.Build;
using Onemt.ResourceManagement.ResourceLocations;

namespace OnemtEditor.AssetBundle.Settings
{
    public partial class AssetBundleAssetSettings : ScriptableObject
    {
        public const string kNewGroupName = "New Group";
        internal const string kBuiltInGroupName = "Built In Data";
        internal const string kDefaultLocalGroupName = "Default Local Group";

        [SerializeField]
        [FormerlySerializedAs("m_DefaultGroupName")]
        private string m_DefaultGroupGUID;
        public string defaultGUID { get => m_DefaultGroupGUID; }

        /// <summary>
        ///   <para> 默认的分组. </para>
        ///   TODO zm    如果没有需要后续去除.
        /// </summary>
        /// <value></value>
        public AssetGroup defaultGroup
        {
            get
            {
                AssetGroup group = null;
                if (string.IsNullOrEmpty(m_DefaultGroupGUID))
                    group = assetGroups.FirstOrDefault(s => s != null && s.CanBeSetAsDefault());
                else
                {
                    group = assetGroups.FirstOrDefault(x => x != null && x.guid == m_DefaultGroupGUID);
                    if (group == null || !group.CanBeSetAsDefault())
                    {
                        group = assetGroups.FirstOrDefault(s => s != null && s.CanBeSetAsDefault());
                        if (group != null)
                            m_DefaultGroupGUID = group.guid;
                    }
                }

                if (group == null)
                {
                    UnityEngine.Debug.LogError("A valid default group could not be found.  One will be created.");
                    group = CreateDefaultGroup(this);
                }

                return group;
            }
            set
            {
                if (value == null)
                    UnityEngine.Debug.LogError("Unable to set null as the Default Group.  Default Groups must not be ReadOnly.");

                else if (!value.CanBeSetAsDefault())
                    UnityEngine.Debug.LogError("Unable to set " + value.name + " as the Default Group.  Default Groups must not be ReadOnly.");
                else
                    m_DefaultGroupGUID = value.guid;
            }
        }

        /// <summary>
        ///   <para> settings 资源路径. </para>
        /// </summary>
        /// <value></value>
        public string assetPath
        {
            get
            {
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this, out string guid, out long localID))
                    throw new Exception($"{nameof(AssetBundleAssetSettings)} is not persisted. Unable to determine AsetPath. ");

                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath))
                    throw new Exception($"{nameof(AssetBundleAssetSettings)} - Unable to determine AssetPath from guid {guid}.");

                return assetPath;
            }
        }

        /// <summary>
        ///   <para> settings 存放的文件目录. </para>
        /// </summary>
        /// <returns></returns>
        public string configFolder { get => Path.GetDirectoryName(assetPath); }

        /// <summary>
        ///   <para> group 存放的目录路径. </para>
        ///   <para> Note : 后续不一定会使用.   如果需要生成assetgroup asset 才会创建并使用这个路径. </para>
        /// </summary>
        /// <value></value>
        public string groupFolder { get => configFolder + "/AssetGroups"; }

        [SerializeField]
        [FormerlySerializedAs("m_AssetGroups")]
        private List<AssetGroup> m_AssetGroups = new List<AssetGroup>();
        public List<AssetGroup> assetGroups { get => m_AssetGroups; }

        public Action<AssetBundleAssetSettings, ModificationEvent, object> OnModification { get; set; }

        /// <summary>
        ///   <para> 创建 assetbundlesettings. </para>
        /// </summary>
        /// <param name="configFolder"> 防止的文件目录. </param>
        /// <param name="configName"> 文件名称. </param>
        /// <param name="createDefaultGroup"> 是否创建默认分组. </param>
        /// <param name="isPersisted"> 是否常驻. </param>
        /// <returns></returns>
        public static AssetBundleAssetSettings Create(string configFolder, string configName, bool createDefaultGroup, bool isPersisted)
        {
            AssetBundleAssetSettings settings;
            var path = configFolder + "/" + configName + ".asset";
            settings = isPersisted ? AssetDatabase.LoadAssetAtPath<AssetBundleAssetSettings>(path) : null;
            if (settings == null)
            {
                settings = CreateInstance<AssetBundleAssetSettings>();
                settings.name = configName;

                if (isPersisted)
                {
                    Directory.CreateDirectory(configFolder);
                    AssetDatabase.CreateAsset(settings, path);
                    settings = AssetDatabase.LoadAssetAtPath<AssetBundleAssetSettings>(path);
                    settings.Validate();
                }

                if (createDefaultGroup)
                {
                    settings.AddAssetGroup(CreateBuildtInData(settings));
                    settings.AddAssetGroup(CreateDefaultGroup(settings));
                }

                if (isPersisted)
                {
                    settings.SetDirty(ModificationEvent.InitializationObjectAdded, null);
                }
            }

            return settings;
        }

        /// <summary>
        ///   <para> 添加分组. </para>
        /// </summary>
        /// <param name="assetGroup"></param>
        public void AddAssetGroup(AssetGroup assetGroup)
        {
            if (assetGroup == null)
                return;

            bool exist = false;
            foreach (var group in assetGroups)
            {
                if (group.guid == assetGroup.guid)
                {
                    exist = true;
                    break;
                }
            }

            if (!exist)
            {
                assetGroups.Add(assetGroup);
                SetDirty(ModificationEvent.GroupAdded, null);
            }
        }

        /// <summary>
        ///   <para> 移除分组. </para>
        /// </summary>
        /// <param name="assetGroup"></param>
        public void RemoveGroup(AssetGroup group)
        {
            RemoveGroupInternal(group, true, true);
        }

        internal void RemoveGroupInternal(AssetGroup group, bool deleteAsset, bool postEvent)
        {
            assetGroups.Remove(group);
            SetDirty(ModificationEvent.GroupRemoved, group, postEvent, true);
            // 删除本地group文件
            if (group != null && deleteAsset)
            {
                string guidOfGroup;
                long localId;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(group, out guidOfGroup, out localId))
                {
                    var groupPath = AssetDatabase.GUIDToAssetPath(guidOfGroup);
                    if (!string.IsNullOrEmpty(groupPath))
                        AssetDatabase.DeleteAsset(groupPath);
                }
            }
        }

        /// <summary>
        ///   <para> 获取分组. </para>
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public AssetGroup GetAssetGroup(string guid)
        {
            foreach (var assetGroup in assetGroups)
            {
                if (string.Equals(assetGroup.guid, guid))
                    return assetGroup;
            }

            return null;
        }

        /// <summary>
        ///   <para> 创建分组. </para>
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="setAsDefaultGroup"></param>
        /// <param name="readOnly"></param>
        /// <param name="postEvent"></param>
        /// <returns></returns>
        public AssetGroup CreateGroup(string groupName, bool setAsDefaultGroup, bool readOnly, bool postEvent = false, bool ignorePacking = false)
        {
            if (string.IsNullOrEmpty(groupName))
                groupName = kNewGroupName;
            var validName = FindUniqueGroupName(groupName);
            var group = CreateInstance<AssetGroup>();
            group.Initialize(this, validName, GUID.Generate().ToString(), readOnly, ignorePacking);

            if (!Directory.Exists(groupFolder))
                Directory.CreateDirectory(groupFolder);

            AssetDatabase.CreateAsset(group, groupFolder + "/" + groupName + ".asset");

            AddAssetGroup(group);
            group.SetDirty(ModificationEvent.GroupAdded, null, postEvent, true);


            return group;
        }

        /// <summary>
        ///   <para> 移除 assetentry. </para>
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public bool RemoveAssetEntry(string guid)
        {
            var assetEntry = FindAssetEntry(guid);
            return RemoveAssetEntry(assetEntry);
        }

        public bool RemoveAssetEntry(AssetEntry assetEntry)
        {
            if (assetEntry == null)
                return false;

            if (assetEntry.parentGroup == null)
                return false;

            assetEntry.parentGroup.RemoveAssetEntry(assetEntry);
            return true;
        }

        /// <summary>
        ///   <para> 内置资源内置分组. </para>
        ///   <para> 包含 ： 1. 所有Resources 文件夹内的资源. 2. EditorSceneList (Build Settings 中设置且勾选) </para>
        ///   Note : 内置资源分组. 不打assetbundle
        /// </summary>
        /// <param name="settings"></param>
        private static AssetGroup CreateBuildtInData(AssetBundleAssetSettings settings)
        {
            // TODO zm
            var assetGroup = settings.CreateGroup(kBuiltInGroupName, false, true, true, true);
            var resourceEntry = settings.CreateOrMoveEntry(AssetEntry.kResourceName, assetGroup);
            resourceEntry.isInResources = true;
            settings.CreateOrMoveEntry(AssetEntry.kEditorSceneListName, assetGroup);

            return assetGroup;
        }

        /// <summary>
        ///   <para> 创建默认分组 </para>
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        private static AssetGroup CreateDefaultGroup(AssetBundleAssetSettings settings)
        {
            var localGroup = settings.CreateGroup(kDefaultLocalGroupName, true, false);
            settings.m_DefaultGroupGUID = localGroup.guid;

            return localGroup;
        }

        internal AssetEntry CreateOrMoveEntry(string guid, AssetGroup targetGroup, bool readOnly = false, bool postEvent = true)
        {
            if (targetGroup == null || string.IsNullOrEmpty(guid))
                return null;

            var assetEntry = FindAssetEntry(guid);
            if (assetEntry != null)
            {
                MoveEntry(assetEntry, targetGroup, readOnly, postEvent);
            }
            else
            {
                assetEntry = CreateAndAddEntryToGroup(guid, targetGroup, readOnly, postEvent);
            }

            return assetEntry;
        }

        /// <summary>
        ///   <para> 创建 AssetEntry 的子 AssetEntry. </para>
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="address"></param>
        /// <param name="parentEntry"></param>
        /// <returns></returns>
        internal AssetEntry CreateSubEntryIfUnique(string guid, string address, AssetEntry parentEntry)
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            AssetEntry entry = FindAssetEntry(guid);

            if (entry == null)
            {
                entry = new AssetEntry(guid, "", parentEntry.parentGroup, true);
                entry.isSubAsset = true;
                entry.parentEntry = parentEntry;
                entry.bundleFileId = parentEntry.bundleFileId;
                //parentEntry.parentGroup.AddAssetEntry(entry);
                return entry;
            }

            //if the sub-entry already exists update it's info.  This mainly covers the case of dragging folders around.
            if (entry.isSubAsset)
            {
                entry.parentGroup = parentEntry.parentGroup;
                entry.isInResources = parentEntry.isInResources;
                entry.readOnly = true;
                entry.isInResources = parentEntry.isInResources;
                return entry;
            }
            return null;
        }

        public AssetEntry FindAssetEntry(string guid)
        {
            foreach (var group in m_AssetGroups)
            {
                if (group.TryGetAssetEntry(guid, out var assetEntry))
                    return assetEntry;
            }

            return null;
        }

        public AssetGroup FindGroup(Func<AssetGroup, bool> func)
        {
            return assetGroups.Find(g => g != null && func(g));
        }

        internal AssetEntry CreateEntry(string guid, string assetName, AssetGroup parent, bool readOnly, bool postEvent = true)
        {
            if (!parent.TryGetAssetEntry(guid, out var assetEntry) || assetEntry == null)
                assetEntry = new AssetEntry(guid, assetName, parent, readOnly);

            if (!readOnly)
                SetDirty(ModificationEvent.EntryCreated, assetEntry, postEvent, false);

            return assetEntry;
        }

        /// <summary>
        ///   <para> 移动AssetEntry. </para>
        /// </summary>
        /// <param name="assetEntry"></param>
        /// <param name="targetParent"></param>
        /// <param name="readOnly"></param>
        /// <param name="postEvent"></param>
        public void MoveEntry(AssetEntry assetEntry, AssetGroup targetParent, bool readOnly = false, bool postEvent = true)
        {
            if (assetEntry == null || targetParent == null)
                return;

            if (assetEntry.parentGroup == targetParent)
                return;

            assetEntry.readOnly = readOnly;
            if (assetEntry.parentGroup != null && assetEntry.parentGroup != targetParent)
                assetEntry.parentGroup.RemoveAssetEntry(assetEntry, postEvent);

            targetParent.AddAssetEntry(assetEntry, postEvent);
        }

        public void MoveEntries(List<AssetEntry> entries, AssetGroup targetParent, bool readOnly = false, bool postEvent = true)
        {
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    MoveEntry(entry, targetParent, readOnly, false);
                }

                SetDirty(ModificationEvent.EntryMoved, entries, postEvent, false);
            }
        }

        public bool RemoveAssetEntry(string guid, bool postEvent = true)
            => RemoveAssetEntry(FindAssetEntry(guid), postEvent);

        internal bool RemoveAssetEntry(AssetEntry entry, bool postEvent = true)
        {
            if (entry == null)
                return false;
            if (entry.parentGroup != null)
                entry.parentGroup.RemoveAssetEntry(entry, postEvent);
            return true;
        }

        private AssetEntry CreateAndAddEntryToGroup(string guid, AssetGroup parentGroup, bool readOnly, bool postEvent = true)
        {
            AssetEntry assetEntry = null;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetBundleUtility.IsPathValidForEntry(path))
            {
                assetEntry = CreateEntry(guid, "", parentGroup, readOnly, postEvent);
            }
            else
            {
                assetEntry = CreateEntry(guid, "", parentGroup, true, postEvent);
            }

            parentGroup.AddAssetEntry(assetEntry);

            return assetEntry;
        }

        internal void CreateOrMoveEntries(System.Collections.IEnumerable guids, AssetGroup targetParent, List<AssetEntry> createdEntries, List<AssetEntry> movedEntries, bool readOnly = false, bool postEvent = true)
        {
            if (targetParent == null)
                throw new ArgumentException("targetParent must not be null");

            if (createdEntries == null)
                createdEntries = new List<AssetEntry>();
            if (movedEntries == null)
                movedEntries = new List<AssetEntry>();

            foreach (string guid in guids)
            {
                AssetEntry entry = FindAssetEntry(guid);
                if (entry != null)
                {
                    MoveEntry(entry, targetParent, readOnly, postEvent);
                    movedEntries.Add(entry);
                }
                else
                {
                    entry = CreateAndAddEntryToGroup(guid, targetParent, readOnly, postEvent);
                    if (entry != null)
                        createdEntries.Add(entry);
                }
            }
        }

        private void Validate()
        { }

        /// <summary>
        ///   <para> 查找唯一的 groupName, 如果存在重名则进行后缀累加. 最多不超过1000. </para>
        ///   <para> Note : 如果存在大量累加的groupName, 则需要排查是否出问题. </para>
        /// </summary>
        /// <param name="potentialName"></param>
        /// <returns></returns>
        internal string FindUniqueGroupName(string potentialName)
        {
            var cleanedName = potentialName.Replace('/', '_');
            cleanedName = cleanedName.Replace('\\', '_');
            if (cleanedName != potentialName)
                UnityEngine.Debug.LogWarning("Group names cannot include '\\' or '/'. Replacing with '_'.   " + cleanedName);

            var validName = cleanedName;
            int index = 1;
            bool existing = true;
            while (existing)
            {
                if (index > 1000)
                {
                    UnityEngine.Debug.LogError("Unable to create valid name for new Assets group.");
                    return cleanedName;
                }

                existing = IsNotUniqueGroupName(validName);
                if (existing)
                {
                    validName = cleanedName + index;
                    ++index;
                }
            }

            return validName;
        }

        internal bool RemoveMissingGroupReferences()
        {
            List<int> missingGroupsIndices = new List<int>();
            for (int i = 0; i < assetGroups.Count; i++)
            {
                var g = assetGroups[i];
                if (g == null)
                    missingGroupsIndices.Add(i);
            }
            if (missingGroupsIndices.Count > 0)
            {
                Debug.Log("Addressable settings contains " + missingGroupsIndices.Count + " group reference(s) that are no longer there. Removing reference(s).");
                for (int i = missingGroupsIndices.Count - 1; i >= 0; i--)
                {
                    assetGroups.RemoveAt(missingGroupsIndices[i]);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        ///   <para> 是否唯一的 分组名称. </para>
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        internal bool IsNotUniqueGroupName(string groupName)
        {
            foreach (var group in m_AssetGroups)
                if (group != null && group.name == groupName)
                    return true;

            return false;
        }

        internal void SetDirty(ModificationEvent modificationEvent, object eventData, bool postEvent = false, bool settingsModified = false)
        {
            OnModification?.Invoke(this, modificationEvent, eventData);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        ///   <para> 创建locMap, 用于编辑器模式下. </para>
        /// </summary>
        /// <returns></returns>
        internal ResourceLocationMap CreateLocationMap()
        {
            ResourceLocationMap locMap = new ResourceLocationMap();

            foreach (var group in assetGroups)
            {
                foreach (var entry in group.assetEntries)
                {
                    ResourceLocationBase loc = new ResourceLocationBase(entry.assetPath, new string[] { entry.assetName }, null);
                    locMap.AddLocation(loc);
                }
            }

            return locMap;
        }

        internal void MoveAssetsFromResources(Dictionary<string, string> guidToNewPath, AssetGroup targetParent)
        {
            if (guidToNewPath == null || targetParent == null)
            {
                return;
            }

            var entries = new List<AssetEntry>();
            var createdDirs = new List<string>();
            AssetDatabase.StartAssetEditing();
            foreach (var item in guidToNewPath)
            {
                var dirInfo = new FileInfo(item.Value).Directory;
                if (dirInfo != null && !dirInfo.Exists)
                {
                    dirInfo.Create();
                    createdDirs.Add(dirInfo.FullName);
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.Refresh();
                    AssetDatabase.StartAssetEditing();
                }

                var oldPath = AssetDatabase.GUIDToAssetPath(item.Key);
                var errorStr = AssetDatabase.MoveAsset(oldPath, item.Value);
                if (!string.IsNullOrEmpty(errorStr))
                {
                    UnityEngine.Debug.LogError("Error moving asset: " + errorStr);
                }
                else
                {
                    AssetEntry e = FindAssetEntry(item.Key);
                    if (e != null)
                        e.isInResources = false;

                    var newEntry = CreateOrMoveEntry(item.Key, targetParent, false, false);
                    var index = oldPath.ToLower().LastIndexOf("resources/");
                    if (index >= 0)
                    {
                        var newAddress = oldPath.Substring(index + 10);
                        if (Path.HasExtension(newAddress))
                        {
                            newAddress = newAddress.Replace(Path.GetExtension(oldPath), "");
                        }

                        if (!string.IsNullOrEmpty(newAddress))
                        {
                            newEntry.SetAssetName(newAddress);
                        }
                    }
                    entries.Add(newEntry);
                }
            }

            foreach (var dir in createdDirs)
                DirectoryUtility.DeleteDirectory(dir, onlyIfEmpty: true);

            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
            SetDirty(ModificationEvent.EntryMoved, entries, true, true);
        }
    }

    /// <summary>
    ///   <para> 资源修改事件. </para>
    /// </summary>
    public enum ModificationEvent
    {
        /// <summary>
        /// Use to indicate that a group was added to the settings object.
        /// </summary>
        GroupAdded,
        /// <summary>
        /// Use to indicate that a group was removed from the the settings object.
        /// </summary>
        GroupRemoved,
        /// <summary>
        /// Use to indicate that a group in the settings object was renamed.
        /// </summary>
        GroupRenamed,
        /// <summary>
        /// Use to indicate that a schema was added to a group.
        /// </summary>
        GroupSchemaAdded,
        /// <summary>
        /// Use to indicate that a schema was removed from a group.
        /// </summary>
        GroupSchemaRemoved,
        /// <summary>
        /// Use to indicate that a schema was modified.
        /// </summary>
        GroupSchemaModified,
        /// <summary>
        /// Use to indicate that a group template was added to the settings object.
        /// </summary>
        GroupTemplateAdded,
        /// <summary>
        /// Use to indicate that a group template was removed from the settings object.
        /// </summary>
        GroupTemplateRemoved,
        /// <summary>
        /// Use to indicate that a schema was added to a group template.
        /// </summary>
        GroupTemplateSchemaAdded,
        /// <summary>
        /// Use to indicate that a schema was removed from a group template.
        /// </summary>
        GroupTemplateSchemaRemoved,
        /// <summary>
        /// Use to indicate that an asset entry was created.
        /// </summary>
        EntryCreated,
        /// <summary>
        /// Use to indicate that an asset entry was added to a group.
        /// </summary>
        EntryAdded,
        /// <summary>
        /// Use to indicate that an asset entry moved from one group to another.
        /// </summary>
        EntryMoved,
        /// <summary>
        /// Use to indicate that an asset entry was removed from a group.
        /// </summary>
        EntryRemoved,
        /// <summary>
        /// Use to indicate that an asset label was added to the settings object.
        /// </summary>
        LabelAdded,
        /// <summary>
        /// Use to indicate that an asset label was removed from the settings object.
        /// </summary>
        LabelRemoved,
        /// <summary>
        /// Use to indicate that a profile was added to the settings object.
        /// </summary>
        ProfileAdded,
        /// <summary>
        /// Use to indicate that a profile was removed from the settings object.
        /// </summary>
        ProfileRemoved,
        /// <summary>
        /// Use to indicate that a profile was modified.
        /// </summary>
        ProfileModified,
        /// <summary>
        /// Use to indicate that a profile has been set as the active profile.
        /// </summary>
        ActiveProfileSet,
        /// <summary>
        /// Use to indicate that an asset entry was modified.
        /// </summary>
        EntryModified,
        /// <summary>
        /// Use to indicate that the build settings object was modified.
        /// </summary>
        BuildSettingsChanged,
        /// <summary>
        /// Use to indicate that a new build script is being used as the active build script.
        /// </summary>
        ActiveBuildScriptChanged,
        /// <summary>
        /// Use to indicate that a new data builder script was added to the settings object.
        /// </summary>
        DataBuilderAdded,
        /// <summary>
        /// Use to indicate that a data builder script was removed from the settings object.
        /// </summary>
        DataBuilderRemoved,
        /// <summary>
        /// Use to indicate a new initialization object was added to the settings object.
        /// </summary>
        InitializationObjectAdded,
        /// <summary>
        /// Use to indicate a initialization object was removed from the settings object.
        /// </summary>
        InitializationObjectRemoved,
        /// <summary>
        /// Use to indicate that a new script is being used as the active playmode data builder.
        /// </summary>
        ActivePlayModeScriptChanged,
        /// <summary>
        /// Use to indicate that a batch of asset entries was modified. Note that the posted object will be null.
        /// </summary>
        BatchModification,
        /// <summary>
        /// Use to indicate that the hosting services manager was modified.
        /// </summary>
        HostingServicesManagerModified,
        /// <summary>
        /// Use to indicate that a group changed its order placement within the list of groups in the settings object.
        /// </summary>
        GroupMoved,
        /// <summary>
        /// Use to indicate that a new certificate handler is being used for the initialization object provider.
        /// </summary>
        CertificateHandlerChanged
    }
}
