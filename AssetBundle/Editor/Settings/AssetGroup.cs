/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 14:51:24
-- 概述:
        AssetGroup:
                    name : 分组名称, 基本和assetbundle 名称相对应
                    packingMode :
                            BundlePackingMode.PackTogether      此分组中的所有 asset 打成一个 assetbundle.
                            BundlePackingMode.PackSeparately    此分组中的每个 asset 都单独打成一个 assetbundle.
                    buildCompressionMode : 此分组生成的 assetbundle 使用哪种压缩格式.
                            BundleCompressionMode.LZ4
                            BundleCompressionMode.LZMA
                            BundleCompressionMode.UnCompressed
                    readOnly: 是否只读
                            暂时只有 Built In Data 为true
                    ignorePacking: 是否忽略打包
                            目前主要是 ： 1.Resources 文件夹group. 2.EditorSceneList 中的场景group.
            
---------------------------------------------------------------------------------------*/

using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace OnemtEditor.AssetBundle.Settings
{
    [Serializable]
    public class AssetGroup : ScriptableObject, ISerializationCallbackReceiver
    {
        /// <summary>
        ///   <para> 分组名称. 对应本地的分组文件. </para>
        /// </summary>
        [SerializeField]
        [FormerlySerializedAs("m_Name")]
        private string m_Name;
        public new string name
        {
            get
            {
                if (string.IsNullOrEmpty(m_Name))
                    m_Name = guid;

                return m_Name;
            }
            set
            {
                string newName = value;
                newName = newName.Replace('/', '-');
                newName = newName.Replace('\\', '-');
                if (newName != value)
                    Debug.Log("Group names cannot include '\\' or '/'.  Replacing with '-'. " + m_Name);

                if (m_Name != newName)
                {
                    string previousName = m_Name;

                    string guid;
                    long localId;
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this, out guid, out localId))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(path))
                        {
                            var folder = Path.GetDirectoryName(path);
                            var extension = Path.GetExtension(path);
                            var newPath = $"{folder}/{newName}{extension}".Replace('\\', '/');
                            if (path != newPath)
                            {
                                // 改名时 需要将本地的配置一起改名.
                                var setPath = AssetDatabase.MoveAsset(path, newPath);
                                if (string.IsNullOrEmpty(setPath))
                                    m_Name = newName;
                                else
                                    m_Name = previousName;
                            }
                        }
                    }
                    else
                    {
                        //this isn't a valid asset, which means it wasn't persisted, so just set the object name to the desired display name.
                        base.name = m_Name = newName;
                    }

                    SetDirty(ModificationEvent.GroupRenamed, this, true, true);
                }
                else if (base.name != newName)
                {
                    base.name = m_Name;
                    SetDirty(ModificationEvent.GroupRenamed, this, true, true);
                }
            }
        }

        [SerializeField]
        [FormerlySerializedAs("m_GUID")]
        private string m_GUID;
        public string guid
        {
            get
            {
                if (string.IsNullOrEmpty(m_GUID))
                    m_GUID = GUID.Generate().ToString();

                return m_GUID;
            }
        }

        [SerializeField]
        [FormerlySerializedAs("m_AssetBundleAssetSettings")]
        private AssetBundleAssetSettings m_AssetBundleAssetSettings;
        public AssetBundleAssetSettings assetBundleAssetSettings
        {
            get
            {
                if (m_AssetBundleAssetSettings == null)
                    m_AssetBundleAssetSettings = AssetBundleAssetSettingsDefaultObject.settings;

                return m_AssetBundleAssetSettings;
            }
        }

        [SerializeField]
        [FormerlySerializedAs("m_AssetEntries")]
        private List<AssetEntry> m_AssetEntries = new List<AssetEntry>();
        public List<AssetEntry> assetEntries { get => m_AssetEntries; }

        [SerializeField]
        [FormerlySerializedAs("m_PackingMode")]
        private BundlePackingMode m_PackingMode;
        public BundlePackingMode packingMode { get => m_PackingMode; set => m_PackingMode = value; }

        [SerializeField]
        [FormerlySerializedAs("m_BuildCompressionMode")]
        private BundleCompressionMode m_BuildCompressionMode;
        public BundleCompressionMode buildCompressionMode { get => m_BuildCompressionMode; set => m_BuildCompressionMode = value; }

        [SerializeField]
        [FormerlySerializedAs("m_IsReadOnly")]
        private bool m_ReadOnly;
        public bool readOnly { get => m_ReadOnly; }

        /// <summary>
        ///   <para> 忽略打包. </para>
        ///   <para> 目前主要是 ： 1.Resources 文件夹group. 2.EditorSceneList 中的场景group. </para>
        /// </summary>
        [FormerlySerializedAs("m_IgnorePacking")]
        [SerializeField]
        private bool m_IgnorePacking;
        public bool ignorePacking { get => m_IgnorePacking; }

        /// <summary>
        ///   <para> 是否为远程资源. </para>
        /// </summary>
        [FormerlySerializedAs("m_IsRemote")]
        [SerializeField]
        private bool m_IsRemote;
        public bool isRemote => m_IsRemote;

        public virtual bool isDefault
        {
            get { return guid == assetBundleAssetSettings.defaultGUID; }
        }

        private Dictionary<string, AssetEntry> m_DicEntries = new Dictionary<string, AssetEntry>();

        public virtual ICollection<AssetEntry> entries
        {
            get
            {
                return m_DicEntries.Values;
            }
        }

        // public AssetGroup(string groupName, string guid)
        // {
        //     m_GroupName = groupName;
        //     m_GUID = guid;
        //     m_PackTogether = true;
        //     m_BuildCompression = BuildCompression.LZ4;
        // }

        internal void Initialize(AssetBundleAssetSettings settings, string groupName, string guid, bool isReadOnly, bool ignorePacking = false)
        {
            m_AssetBundleAssetSettings = settings;
            m_Name = groupName;
            m_GUID = guid;
            m_ReadOnly = isReadOnly;
            m_BuildCompressionMode = BundleCompressionMode.LZ4;         // 默认使用 LZ4 压缩格式.
            m_PackingMode = BundlePackingMode.PackTogether;             // 默认一个分组打一个assetbundle.
            m_IgnorePacking = ignorePacking;
        }

        public void AddAssetEntry(AssetEntry entry, bool postEvent = true)
        {
            entry.isSubAsset = false;
            entry.parentGroup = this;
            m_DicEntries[entry.guid] = entry;
            m_AssetEntries.Add(entry);

            SetDirty(ModificationEvent.EntryAdded, entry, postEvent, true);
        }

        public bool TryGetAssetEntry(string guid, out AssetEntry assetEntry)
        {
            return m_DicEntries.TryGetValue(guid, out assetEntry);
        }

        public void RemoveAssetEntry(AssetEntry entry, bool postEvent = true)
        {
            m_DicEntries.Remove(entry.guid);
            entry.parentGroup = null;
            m_AssetEntries = null;   // 直接置null, 在序列化之前再使用 m_DicEntries 进行构造.               
            SetDirty(ModificationEvent.EntryRemoved, entry, postEvent, true);
        }

        public void RemoveAssetEntries(IEnumerable<AssetEntry> entries)
        {
            foreach (var entry in entries)
            {
                RemoveAssetEntry(entry);
            }
        }

        public void OnBeforeSerialize()
        {
            if (m_AssetEntries == null)
            {
                m_AssetEntries = new List<AssetEntry>(m_DicEntries.Count);
                foreach (var entry in m_DicEntries.Values)
                    m_AssetEntries.Add(entry);
            }
        }

        public void OnAfterDeserialize()
        {
            m_DicEntries.Clear();

            foreach (var entry in m_AssetEntries)
            {
                try
                {
                    m_DicEntries[entry.guid] = entry;
                    entry.parentGroup = this;
                    entry.isSubAsset = false;
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogErrorFormat("  AssetGroup.OnAftereserialize()  Exception  : {0}", e.Message);
                }
            }
        }

        internal void SetDirty(ModificationEvent evt, object eventData, bool postEvent, bool groupModified = false)
        {
            if (m_AssetBundleAssetSettings == null) return;

            EditorUtility.SetDirty(this);
            m_AssetBundleAssetSettings?.SetDirty(evt, eventData, postEvent, groupModified);
        }

        public bool CanBeSetAsDefault()
        {
            return !m_ReadOnly;
        }
    }
}
