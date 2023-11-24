/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 15:45:48
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using OnemtEditor.AssetBundle.Settings;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using static OnemtEditor.AssetBundle.Settings.AddressablesFileEnumeration;

namespace OnemtEditor.AssetBundle.GUI
{
    public class AssetBundleAssetEntryTreeView : TreeView
    {
        enum ColumnId
        {
            Id,
            Type,
            Path
        }
        ColumnId[] m_SortOptions =
        {
            ColumnId.Id,
            ColumnId.Type,
            ColumnId.Path
        };
        internal class AssetEntryTreeViewItem : TreeViewItem
        {
            public AssetEntry assetEntry;

            public AssetGroup assetGroup;
            public string folderPath;
            public Texture2D assetIcon;
            public bool isRenaming;
            public bool checkedForChildren = true;

            public AssetEntryTreeViewItem(AssetEntry entry, int d)
                : base(entry == null ? 0 : (entry.assetName + entry.guid).GetHashCode(), d, entry == null ? "[Missing Reference]" : entry.assetName)
            {
                assetEntry = entry;
                assetGroup = null;
                folderPath = string.Empty;
                assetIcon = entry == null ? null : AssetDatabase.GetCachedIcon(entry.assetPath) as Texture2D;
                isRenaming = false;
            }

            public AssetEntryTreeViewItem(AssetGroup group, int d)
                : base(group == null ? 0 : group.guid.GetHashCode(), d, group == null ? "[Missing Reference]" : group.name)
            {
                assetEntry = null;
                assetGroup = group;
                folderPath = string.Empty;
                assetIcon = null;
                isRenaming = false;
            }

            public AssetEntryTreeViewItem(string folder, int d, int id) : base(id, d, string.IsNullOrEmpty(folder) ? "missing" : folder)
            {
                assetEntry = null;
                assetGroup = null;
                folderPath = folder;
                assetIcon = null;
                isRenaming = false;
            }

            public bool isGroup => assetGroup != null && assetEntry == null;

            public override string displayName
            {
                get
                {
                    if (!isRenaming && assetGroup != null && assetGroup.isDefault)
                        return base.displayName + "( Default)";

                    return base.displayName;
                }
                set => base.displayName = value;
            }
        }


        internal string customSearchString = string.Empty;

        private AssetBundleSettingsGroupEditor m_GroupEditor;

        private readonly Dictionary<AssetEntryTreeViewItem, bool> m_SearchedEntries = new Dictionary<AssetEntryTreeViewItem, bool>();

        private bool m_ForceSelectionClear = false;

        internal AssetBundleAssetEntryTreeView(AssetBundleAssetSettings settings)
            : this(new TreeViewState(), CreateDefaultMultiColumnHeaderState(),
                    new AssetBundleSettingsGroupEditor(ScriptableObject.CreateInstance<AssetBundleAssetsWindow>()))
        {
            m_GroupEditor.settings = settings;
        }

        public AssetBundleAssetEntryTreeView(TreeViewState state, MultiColumnHeaderState mchs, AssetBundleSettingsGroupEditor groupEditor)
            : base(state, new MultiColumnHeader(mchs))
        {
            showBorder = true;
            m_GroupEditor = groupEditor;
            columnIndexForTreeFoldouts = 0;
            multiColumnHeader.sortingChanged += OnSortingChanged;
        }

        private void OnSortingChanged(MultiColumnHeader mch)
        {
            SortChildren(rootItem);
            Reload();
        }

        private void SortChildren(TreeViewItem root)
        {
            if (!root.hasChildren)
                return;

            foreach (var child in root.children)
            {
                if (child != null && IsExpanded(child.id))
                    SortHierarchical(child.children);
            }
        }

        void SortHierarchical(IList<TreeViewItem> children)
        {
            if (children == null)
                return;

            var sortedColumns = multiColumnHeader.state.sortedColumns;
            if (sortedColumns.Length == 0)
                return;

            List<AssetEntryTreeViewItem> kids = new List<AssetEntryTreeViewItem>();
            List<TreeViewItem> copy = new List<TreeViewItem>(children);
            children.Clear();
            foreach (var c in copy)
            {
                var child = c as AssetEntryTreeViewItem;
                if (child != null && child.assetEntry != null)
                    kids.Add(child);
                else
                    children.Add(c);
            }

            ColumnId col = m_SortOptions[sortedColumns[0]];
            bool ascending = multiColumnHeader.IsSortedAscending(sortedColumns[0]);

            IEnumerable<AssetEntryTreeViewItem> orderedKids = kids;
            switch (col)
            {
                case ColumnId.Type:
                    break;
                case ColumnId.Path:
                    orderedKids = kids.Order(l => l.assetEntry.assetPath, ascending);
                    break;
                default:
                    orderedKids = kids.Order(l => l.displayName, ascending);
                    break;
            }

            foreach (var o in orderedKids)
                children.Add(o);


            foreach (var child in children)
            {
                if (child != null && IsExpanded(child.id))
                    SortHierarchical(child.children);
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1);

            using (new AddressablesFileEnumerationScope(BuildAddressableTree(m_GroupEditor.settings)))
            {
                foreach (var group in m_GroupEditor.settings.assetGroups)
                    AddGroupChildrenBuild(group, root);
            }
            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            if (!string.IsNullOrEmpty(searchString))
            {
                var rows = base.BuildRows(root);
                SortHierarchical(rows);
                return rows;
            }
            if (!string.IsNullOrEmpty(customSearchString))
            {
                SortChildren(root);
                return Search(base.BuildRows(root));
            }

            SortChildren(root);
            return base.BuildRows(root);
        }

        void AddGroupChildrenBuild(AssetGroup group, TreeViewItem root)
        {
            int depth = 0;

            AssetEntryTreeViewItem groupItem = null;
            if (group != null)
            {
                //// dash in name imitates hiearchy.
                TreeViewItem newRoot = root;
                var parts = group.name.Split('-');
                string partialRestore = "";
                for (int index = 0; index < parts.Length - 1; index++)
                {
                    TreeViewItem folderItem = null;
                    partialRestore += parts[index];
                    int hash = partialRestore.GetHashCode();
                    if (!TryGetChild(newRoot, hash, ref folderItem))
                    {
                        folderItem = new AssetEntryTreeViewItem(parts[index], depth, hash);
                        newRoot.AddChild(folderItem);
                    }
                    depth++;
                    newRoot = folderItem;
                }

                groupItem = new AssetEntryTreeViewItem(group, depth);
                newRoot.AddChild(groupItem);
            }
            else
            {
                groupItem = new AssetEntryTreeViewItem(group, 0);
                root.AddChild(groupItem);
            }

            if (group != null && group.entries.Count > 0)
            {
                foreach (var entry in group.entries)
                {
                    AddAndRecurseEntriesBuild(entry, groupItem, depth + 1, IsExpanded(groupItem.id));
                }
            }
        }

        protected IList<TreeViewItem> Search(IList<TreeViewItem> rows)
        {
            if (rows == null)
                return new List<TreeViewItem>();

            m_SearchedEntries.Clear();
            List<TreeViewItem> items = new List<TreeViewItem>(rows.Count);
            foreach (TreeViewItem item in rows)
            {
                if (SearchHierarchical(item, customSearchString))
                    items.Add(item);
            }
            return items;
        }

        bool SearchHierarchical(TreeViewItem item, string search, bool? ancestorMatching = null)
        {
            var aeItem = item as AssetEntryTreeViewItem;
            if (aeItem == null || search == null)
                return false;

            if (m_SearchedEntries.ContainsKey(aeItem))
                return m_SearchedEntries[aeItem];

            if (ancestorMatching == null)
                ancestorMatching = DoesAncestorMatch(aeItem, search);

            bool isMatching = false;
            if (!ancestorMatching.Value)
                isMatching = DoesItemMatchSearch(aeItem, search);

            bool descendantMatching = false;
            if (!ancestorMatching.Value && !isMatching && aeItem.hasChildren)
            {
                foreach (var child in aeItem.children)
                {
                    descendantMatching = SearchHierarchical(child, search, false);
                    if (descendantMatching)
                        break;
                }
            }

            bool keep = isMatching || ancestorMatching.Value || descendantMatching;
            m_SearchedEntries.Add(aeItem, keep);
            return keep;
        }

        private bool DoesAncestorMatch(TreeViewItem aeItem, string search)
        {
            if (aeItem == null)
                return false;

            var ancestor = aeItem.parent as AssetEntryTreeViewItem;
            bool isMatching = DoesItemMatchSearch(ancestor, search);
            while (ancestor != null && !isMatching)
            {
                ancestor = ancestor.parent as AssetEntryTreeViewItem;
                isMatching = DoesItemMatchSearch(ancestor, search);
            }

            return isMatching;
        }


        bool TryGetChild(TreeViewItem root, int childHash, ref TreeViewItem childItem)
        {
            if (root.children == null)
                return false;
            foreach (var child in root.children)
            {
                if (child.id == childHash)
                {
                    childItem = child;
                    return true;
                }
            }

            return false;
        }

        void AddAndRecurseEntriesBuild(AssetEntry entry, AssetEntryTreeViewItem parent, int depth, bool expanded)
        {
            var item = new AssetEntryTreeViewItem(entry, depth);
            parent.AddChild(item);
            if (!expanded)
            {
                item.checkedForChildren = false;
                return;
            }
            RecurseEntryChildren(entry, item, depth);
        }

        internal void RecurseEntryChildren(AssetEntry entry, AssetEntryTreeViewItem item, int depth)
        {
            item.checkedForChildren = true;
            var subAssets = new List<AssetEntry>();
            entry.GatherAllAssets(subAssets, false, entry.isInResources, true);
            if (subAssets.Count > 0)
            {
                foreach (var e in subAssets)
                {
                    if (e.guid.Length > 0 && e.assetName.Contains("[") && e.assetName.Contains("]"))
                        Debug.LogErrorFormat("Subasset address '{0}' cannot contain '[ ]'.", e.assetName);
                    AddAndRecurseEntriesBuild(e, item, depth + 1, IsExpanded(item.id));
                }
            }
        }

        protected override void ExpandedStateChanged()
        {
            foreach (var id in state.expandedIDs)
            {
                var item = FindItem(id, rootItem);
                if (item != null && item.hasChildren)
                {
                    foreach (AssetEntryTreeViewItem c in item.children)
                        if (!c.checkedForChildren)
                            RecurseEntryChildren(c.assetEntry, c, c.depth + 1);
                }
            }
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            return new MultiColumnHeaderState(GetColums());
        }

        // TODO zm. 暂时 三列( 1.groupName/assetName、2.Asset Type、 3.Path)
        private static MultiColumnHeaderState.Column[] GetColums()
        {
            var retVal = new[]
            {
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
            };

            int counter = 0;
            retVal[counter].headerContent = new GUIContent("Group Name \\ Asset Name", "AssetName used to load asset at runtime.");
            retVal[counter].minWidth = 100;
            retVal[counter].width = 260;
            retVal[counter].maxWidth = 1000;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = true;
            retVal[counter].autoResize = true;
            ++counter;

            retVal[counter].headerContent = new GUIContent(EditorGUIUtility.FindTexture("FilterByType"), "Asset type");
            retVal[counter].minWidth = 100;
            retVal[counter].width = 260;
            retVal[counter].maxWidth = 1000;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = true;
            retVal[counter].autoResize = true;
            ++counter;

            retVal[counter].headerContent = new GUIContent("Path", "Asset Path");
            retVal[counter].minWidth = 100;
            retVal[counter].width = 260;
            retVal[counter].maxWidth = 1000;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = true;
            retVal[counter].autoResize = true;

            return retVal;
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);

            //TODO - this occasionally causes a "hot control" issue.
            if (m_ForceSelectionClear ||
                (Event.current.type == EventType.MouseDown &&
                 Event.current.button == 0 &&
                 rect.Contains(Event.current.mousePosition)))
            {
                SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
                if (m_ForceSelectionClear)
                    m_ForceSelectionClear = false;
            }
        }

        internal IList<TreeViewItem> Search(string search)
        {
            customSearchString = search;
            Reload();

            return GetRows();
        }

        protected override void BeforeRowsGUI()
        {
            base.BeforeRowsGUI();

            if (Event.current.type == EventType.Repaint)
            {
                var rows = GetRows();
                if (rows.Count > 0)
                {
                    int first;
                    int last;
                    GetFirstAndLastVisibleRows(out first, out last);
                    for (int rowId = first; rowId <= last; rowId++)
                    {
                        var aeI = rows[rowId] as AssetEntryTreeViewItem;
                        if (aeI != null && aeI.assetEntry != null)
                        {
                            DefaultStyles.backgroundEven.Draw(GetRowRect(rowId), false, false, false, false);
                        }
                    }
                }
            }
        }

        GUIStyle m_LabelStyle;
        protected override void RowGUI(RowGUIArgs args)
        {
            if (m_LabelStyle == null)
            {
                m_LabelStyle = new GUIStyle("PR Label");
                if (m_LabelStyle == null)
                    m_LabelStyle = UnityEngine.GUI.skin.GetStyle("Label");
            }

            var item = args.item as AssetEntryTreeViewItem;
            if (item == null || item.assetGroup == null && item.assetEntry == null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    base.RowGUI(args);
                }
            }
            else if (item.assetGroup != null)
            {
                if (item.isRenaming && !args.isRenaming)
                    item.isRenaming = false;
                using (new EditorGUI.DisabledScope(item.assetGroup.readOnly))
                {
                    base.RowGUI(args);
                }
            }
            else if (item.assetEntry != null && !args.isRenaming)
            {
                using (new EditorGUI.DisabledScope(item.assetEntry.readOnly))
                {
                    for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                    {
                        CellGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
                    }
                }
            }
        }

        void CellGUI(Rect cellRect, AssetEntryTreeViewItem item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch ((ColumnId)column)
            {
                case ColumnId.Id:
                    {
                        // The rect is assumed indented and sized after the content when pinging
                        float indent = GetContentIndent(item) + extraSpaceBeforeIconAndLabel;
                        cellRect.xMin += indent;

                        if (Event.current.type == EventType.Repaint)
                            m_LabelStyle.Draw(cellRect, item.assetEntry.assetName, false, false, args.selected, args.focused);
                    }
                    break;
                case ColumnId.Path:
                    if (Event.current.type == EventType.Repaint)
                    {
                        var path = item.assetEntry.assetPath;
                        if (string.IsNullOrEmpty(path))
                            path = item.assetEntry.readOnly ? "" : "Missing File";
                        m_LabelStyle.Draw(cellRect, path, false, false, args.selected, args.focused);
                    }
                    break;
                case ColumnId.Type:
                    if (item.assetIcon != null)
                        UnityEngine.GUI.DrawTexture(cellRect, item.assetIcon, ScaleMode.ScaleToFit, true);
                    break;
            }
        }

        string m_FirstSelectedGroup;
        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count == 1)
            {
                var item = FindItemInVisibleRows(selectedIds[0]);
                if (item != null && item.assetGroup != null)
                {
                    m_FirstSelectedGroup = item.assetGroup.name;
                }
            }

            base.SelectionChanged(selectedIds);

            UnityEngine.Object[] selectedObjects = new UnityEngine.Object[selectedIds.Count];
            for (int i = 0; i < selectedIds.Count; i++)
            {
                var item = FindItemInVisibleRows(selectedIds[i]);
                if (item != null)
                {
                    if (item.assetGroup != null)
                        selectedObjects[i] = item.assetGroup;
                    else if (item.assetEntry != null)
                        selectedObjects[i] = item.assetEntry.targetAsset;
                }
            }

            // Make last selected group the first object in the array
            if (!string.IsNullOrEmpty(m_FirstSelectedGroup) && selectedObjects.Length > 1)
            {
                for (int i = 0; i < selectedObjects.Length - 1; ++i)
                {
                    if (selectedObjects[i] != null && selectedObjects[i].name == m_FirstSelectedGroup)
                    {
                        var temp = selectedObjects[i];
                        selectedObjects[i] = selectedObjects[selectedIds.Count - 1];
                        selectedObjects[selectedIds.Count - 1] = temp;
                    }
                }
            }

            Selection.objects = selectedObjects; // change selection
        }

        AssetEntryTreeViewItem FindItemInVisibleRows(int id)
        {
            var rows = GetRows();
            foreach (var r in rows)
            {
                if (r.id == id)
                {
                    return r as AssetEntryTreeViewItem;
                }
            }
            return null;
        }

        internal void SwapSearchType()
        {
            string temp = customSearchString;
            customSearchString = searchString;
            searchString = temp;
            m_SearchedEntries.Clear();
        }

        internal void CreateNewGroup(object context)
        {
            m_GroupEditor.settings.CreateGroup("New Group", false, false, true);
        }




        protected override void ContextClickedItem(int id)
        {
            List<AssetEntryTreeViewItem> selectedNodes = new List<AssetEntryTreeViewItem>();
            foreach (var nodeId in GetSelection())
            {
                var item = FindItemInVisibleRows(nodeId);
                if (item != null)
                    selectedNodes.Add(item);
            }
            if (selectedNodes.Count == 0)
                return;

            m_ContextOnItem = true;

            bool isGroup = false;
            bool isEntry = false;
            bool hasReadOnly = false;
            int resourceCount = 0;
            bool isResourcesHeader = false;
            bool isMissingPath = false;
            foreach (var item in selectedNodes)
            {
                if (item.assetGroup != null)
                {
                    hasReadOnly |= item.assetGroup.readOnly;
                    isGroup = true;
                }
                else if (item.assetEntry != null)
                {
                    if (item.assetEntry.assetPath == AssetEntry.kResourcePath)
                    {
                        if (selectedNodes.Count > 1)
                            return;
                        isResourcesHeader = true;
                    }
                    else if (item.assetEntry.assetPath == AssetEntry.kEditorSceneListName)
                    {
                        return;
                    }
                    hasReadOnly |= item.assetEntry.readOnly;
                    isEntry = true;
                    resourceCount += item.assetEntry.isInResources ? 1 : 0;
                    isMissingPath |= string.IsNullOrEmpty(item.assetEntry.assetPath);
                }
                else if (!string.IsNullOrEmpty(item.folderPath))
                {
                    hasReadOnly = true;
                }
            }
            if (isEntry && isGroup)
                return;

            GenericMenu menu = new GenericMenu();
            if (isResourcesHeader)
            {
                foreach (var g in m_GroupEditor.settings.assetGroups)
                {
                    if (!g.readOnly)
                        menu.AddItem(new GUIContent("Move ALL Resources to group/" + g.name), false, MoveAllResourcesToGroup, g);
                }
            }
            else if (!hasReadOnly)
            {
                // TODO zm 目前只添加移除分组功能
                if (isGroup)
                {
                    var group = selectedNodes.First().assetGroup;
                    if (!group.isDefault)
                        menu.AddItem(new GUIContent("Remove Group(s)"), false, RemoveGroup, selectedNodes);
                    //menu.AddItem(new GUIContent("Simplify Addressable Names"), false, SimplifyAddresses, selectedNodes);  // 简化名称，
                    //if (selectedNodes.Count == 1)
                    //{
                    //    if (!group.isDefault && group.CanBeSetAsDefault())
                    //        menu.AddItem(new GUIContent("Set as Default"), false, SetGroupAsDefault, selectedNodes);
                    //    menu.AddItem(new GUIContent("Inspect Group Settings"), false, GoToGroupAsset, selectedNodes);
                    //}
                    //foreach (var i in AddressableAssetSettings.CustomAssetGroupCommands)
                    //    menu.AddItem(new GUIContent(i), false, HandleCustomContextMenuItemGroups, new Tuple<string, List<AssetEntryTreeViewItem>>(i, selectedNodes));
                }
                else if (isEntry)
                {
                    foreach (var g in m_GroupEditor.settings.assetGroups)
                    {
                        if (g != null && !g.readOnly)
                            menu.AddItem(new GUIContent("Move Addressables to Group/" + g.name), false, MoveEntriesToGroup, g);
                    }

                    //var groups = new HashSet<AssetGroup>();
                    //foreach (var n in selectedNodes)
                    //    groups.Add(n.assetEntry.parentGroup);
                    //foreach (var g in groups)
                    //    menu.AddItem(new GUIContent("Move Addressables to New Group/With settings from: " + g.Name), false, MoveEntriesToNewGroup, new KeyValuePair<string, AddressableAssetGroup>(AddressableAssetSettings.kNewGroupName, g));


                    menu.AddItem(new GUIContent("Remove Addressables"), false, RemoveEntry, selectedNodes);
                    //menu.AddItem(new GUIContent("Simplify Addressable Names"), false, SimplifyAddresses, selectedNodes);

                    //if (selectedNodes.Count == 1)
                    //    menu.AddItem(new GUIContent("Copy Address to Clipboard"), false, CopyAddressesToClipboard, selectedNodes);

                    //else if (selectedNodes.Count > 1)
                    //    menu.AddItem(new GUIContent("Copy " + selectedNodes.Count + " Addresses to Clipboard"), false, CopyAddressesToClipboard, selectedNodes);

                    //foreach (var i in AddressableAssetSettings.CustomAssetEntryCommands)
                    //    menu.AddItem(new GUIContent(i), false, HandleCustomContextMenuItemEntries, new Tuple<string, List<AssetEntryTreeViewItem>>(i, selectedNodes));
                }
                else
                    menu.AddItem(new GUIContent("Clear missing references."), false, RemoveMissingReferences);
            }
            else
            {
                //if (isEntry && !isMissingPath)
                //{
                //    if (resourceCount == selectedNodes.Count)
                //    {
                //        foreach (var g in m_GroupEditor.settings.assetGroups)
                //        {
                //            if (!g.readOnly)
                //                menu.AddItem(new GUIContent("Move Resources to group/" + g.name), false, MoveResourcesToGroup, g);
                //        }
                //    }
                //    else if (resourceCount == 0)
                //    {
                //        foreach (var g in m_GroupEditor.settings.assetGroups)
                //        {
                //            if (!g.readOnly)
                //                menu.AddItem(new GUIContent("Move Addressables to group/" + g.name), false, MoveEntriesToGroup, g);
                //        }
                //    }

                //    if (selectedNodes.Count == 1)
                //        menu.AddItem(new GUIContent("Copy Address to Clipboard"), false, CopyAddressesToClipboard, selectedNodes);

                //    else if (selectedNodes.Count > 1)
                //        menu.AddItem(new GUIContent("Copy " + selectedNodes.Count + " Addresses to Clipboard"), false, CopyAddressesToClipboard, selectedNodes);
                //}
            }

            if (selectedNodes.Count == 1)
            {
                var label = CheckForRename(selectedNodes.First(), false);
                if (!string.IsNullOrEmpty(label))
                    menu.AddItem(new GUIContent(label), false, RenameItem, selectedNodes);
            }

            PopulateGeneralContextMenu(ref menu);

            menu.ShowAsContext();
        }

        void MoveAllResourcesToGroup(object context)
        {
            var targetGroup = context as AssetGroup;
            var firstId = GetSelection().First();
            var item = FindItemInVisibleRows(firstId);
            if (item != null && item.children != null)
            {
                SafeMoveResourcesToGroup(targetGroup, item.children.ConvertAll(instance => (AssetEntryTreeViewItem)instance));
            }
            else
                Debug.LogWarning("No Resources found to move");
        }

        /// <summary>
        ///   <para> 移除分组。 </para>
        /// </summary>
        /// <param name="context"></param>
        protected void RemoveGroup(object context)
        {
            RemoveGroupImpl(context);
        }
        internal void RemoveGroupImpl(object context, bool forceRemoval = false)
        {
            if (forceRemoval || EditorUtility.DisplayDialog("Delete selected groups?", "Are you sure you want to delete the selected groups?\n\nYou cannot undo this action.", "Yes", "No"))
            {
                List<AssetEntryTreeViewItem> selectedNodes = context as List<AssetEntryTreeViewItem>;
                if (selectedNodes == null || selectedNodes.Count < 1)
                    return;
                var groups = new List<AssetGroup>();
                foreach (var item in selectedNodes)
                {
                    m_GroupEditor.settings.RemoveGroupInternal(item == null ? null : item.assetGroup, true, false);
                    groups.Add(item.assetGroup);
                }
                m_GroupEditor.settings.SetDirty(ModificationEvent.GroupRemoved, groups, true, true);
                AssetBundleUtility.OpenAssetIfUsingVCIntegration(m_GroupEditor.settings);
            }
        }

        void MoveEntriesToGroup(object context)
        {
            var targetGroup = context as AssetGroup;
            var entries = new List<AssetEntry>();
            foreach (var nodeId in GetSelection())
            {
                var item = FindItemInVisibleRows(nodeId);
                if (item != null)
                    entries.Add(item.assetEntry);
            }
            if (entries.Count > 0)
                m_GroupEditor.settings.MoveEntries(entries, targetGroup);
        }

        protected void RemoveEntry(object context)
        {
            RemoveEntryImpl(context);
        }

        internal void RemoveEntryImpl(object context, bool forceRemoval = false)
        {
            if (forceRemoval || EditorUtility.DisplayDialog("Delete selected entries?", "Are you sure you want to delete the selected entries?\n\nYou cannot undo this action.", "Yes", "No"))
            {
                List<AssetEntryTreeViewItem> selectedNodes = context as List<AssetEntryTreeViewItem>;
                if (selectedNodes == null || selectedNodes.Count < 1)
                    return;
                var entries = new List<AssetEntry>();
                HashSet<AssetGroup> modifiedGroups = new HashSet<AssetGroup>();
                foreach (var item in selectedNodes)
                {
                    if (item.assetEntry != null)
                    {
                        entries.Add(item.assetEntry);
                        modifiedGroups.Add(item.assetEntry.parentGroup);
                        m_GroupEditor.settings.RemoveAssetEntry(item.assetEntry.guid, false);
                    }
                }
                foreach (var g in modifiedGroups)
                {
                    g.SetDirty(ModificationEvent.EntryModified, entries, false, true);
                    AssetBundleUtility.OpenAssetIfUsingVCIntegration(g);
                }
                m_GroupEditor.settings.SetDirty(ModificationEvent.EntryRemoved, entries, true, false);
            }
        }

        protected void RemoveMissingReferences()
        {
            RemoveMissingReferencesImpl();
        }

        internal void RemoveMissingReferencesImpl()
        {
            if (m_GroupEditor.settings.RemoveMissingGroupReferences())
                m_GroupEditor.settings.SetDirty(ModificationEvent.GroupRemoved, null, true, true);
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return !string.IsNullOrEmpty(CheckForRename(item, true));
        }

        protected string CheckForRename(TreeViewItem item, bool isActualRename)
        {
            string result = string.Empty;
            var assetItem = item as AssetEntryTreeViewItem;
            if (assetItem != null)
            {
                if (assetItem.assetGroup != null && !assetItem.assetGroup.readOnly)
                    result = "Rename";
                else if (assetItem.assetEntry != null && !assetItem.assetEntry.readOnly)
                    result = "Change Address";
                if (isActualRename)
                    assetItem.isRenaming = !string.IsNullOrEmpty(result);
            }
            return result;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (!args.acceptedRename)
                return;

            var item = FindItemInVisibleRows(args.itemID);
            if (item != null)
            {
                item.isRenaming = false;
            }

            if (args.originalName == args.newName)
                return;

            if (item != null)
            {
                if (args.newName != null && args.newName.Contains("[") && args.newName.Contains("]"))
                {
                    args.acceptedRename = false;
                    Debug.LogErrorFormat("Rename of address '{0}' cannot contain '[ ]'.", args.originalName);
                }
                else if (item.assetEntry != null)
                {
                    item.assetEntry.SetAssetName(args.newName);
                    AssetBundleUtility.OpenAssetIfUsingVCIntegration(item.assetEntry.parentGroup, true);
                }
                else if (item.assetGroup != null)
                {
                    if (m_GroupEditor.settings.IsNotUniqueGroupName(args.newName))
                    {
                        args.acceptedRename = false;
                        UnityEngine.Debug.LogWarning("There is already a group named '" + args.newName + "'.  Cannot rename this group to match");
                    }
                    else
                    {
                        item.assetGroup.name = args.newName;
                        AssetBundleUtility.OpenAssetIfUsingVCIntegration(item.assetGroup, true);
                        AssetBundleUtility.OpenAssetIfUsingVCIntegration(item.assetGroup.assetBundleAssetSettings, true);
                    }
                }
                Reload();
            }
        }

        protected void RenameItem(object context)
        {
            RenameItemImpl(context);
        }

        internal void RenameItemImpl(object context)
        {
            List<AssetEntryTreeViewItem> selectedNodes = context as List<AssetEntryTreeViewItem>;
            if (selectedNodes != null && selectedNodes.Count >= 1)
            {
                var item = selectedNodes.First();
                if (CanRename(item))
                    BeginRename(item);
                else
                    UnityEngine.Debug.LogError("Can't Rename");
            }
        }




        /// <summary>
        ///   <para> 点击窗口背景，弹提示框. </para>
        ///   <para> 目前只有创建group. </para>
        /// </summary>
        bool m_ContextOnItem;
        protected override void ContextClicked()
        {
            if (m_ContextOnItem)
            {
                m_ContextOnItem = false;
                return;
            }

            GenericMenu menu = new GenericMenu();
            PopulateGeneralContextMenu(ref menu);
            menu.ShowAsContext();
        }

        void PopulateGeneralContextMenu(ref GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Create New Group" ), false, CreateNewGroup, null);
        }


        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.None;

            var target = args.parentItem as AssetEntryTreeViewItem;

            if (target != null && target.assetEntry != null && target.assetEntry.readOnly)
                return DragAndDropVisualMode.Rejected;

            if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
            {
                visualMode = HandleDragAndDropPaths(target, args);
            }
            else
            {
                visualMode = HandleDragAndDropItems(target, args);
            }

            return visualMode;
        }

        DragAndDropVisualMode HandleDragAndDropItems(AssetEntryTreeViewItem target, DragAndDropArgs args)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.None;

            var draggedNodes = DragAndDrop.GetGenericData("AssetEntryTreeViewItem") as List<AssetEntryTreeViewItem>;
            if (draggedNodes != null && draggedNodes.Count > 0)
            {
                visualMode = DragAndDropVisualMode.Copy;
                bool isDraggingGroup = draggedNodes.First().parent == rootItem;
                bool dropParentIsRoot = args.parentItem == rootItem || args.parentItem == null;
                bool parentGroupIsReadOnly = target?.assetGroup != null && target.assetGroup.readOnly;

                if (isDraggingGroup && !dropParentIsRoot || !isDraggingGroup && dropParentIsRoot || parentGroupIsReadOnly)
                    visualMode = DragAndDropVisualMode.Rejected;

                if (args.performDrop)
                {
                    if (args.parentItem == null || args.parentItem == rootItem && visualMode != DragAndDropVisualMode.Rejected)
                    {
                        // Need to insert groups in reverse order because all groups will be inserted at the same index
                        for (int i = draggedNodes.Count - 1; i >= 0; i--)
                        {
                            AssetEntryTreeViewItem node = draggedNodes[i];
                            AssetGroup group = node.assetGroup;
                            int index = m_GroupEditor.settings.assetGroups.FindIndex(g => g == group);
                            if (index < args.insertAtIndex)
                                args.insertAtIndex--;

                            m_GroupEditor.settings.assetGroups.RemoveAt(index);

                            if (args.insertAtIndex < 0 || args.insertAtIndex > m_GroupEditor.settings.assetGroups.Count)
                                m_GroupEditor.settings.assetGroups.Insert(m_GroupEditor.settings.assetGroups.Count, group);
                            else
                                m_GroupEditor.settings.assetGroups.Insert(args.insertAtIndex, group);
                        }

                        m_GroupEditor.settings.SetDirty(ModificationEvent.GroupMoved, m_GroupEditor.settings.assetGroups, true, true);
                        Reload();
                    }
                    else
                    {
                        AssetGroup parent = null;
                        if (target.assetGroup != null)
                            parent = target.assetGroup;
                        else if (target.assetEntry != null)
                            parent = target.assetEntry.parentGroup;

                        if (parent != null)
                        {
                            if (draggedNodes.First().assetEntry.isInResources)
                            {
                                SafeMoveResourcesToGroup(parent, draggedNodes);
                            }
                            else
                            {
                                var entries = new List<AssetEntry>();
                                HashSet<AssetGroup> modifiedGroups = new HashSet<AssetGroup>();
                                modifiedGroups.Add(parent);
                                foreach (var node in draggedNodes)
                                {
                                    modifiedGroups.Add(node.assetEntry.parentGroup);
                                    m_GroupEditor.settings.MoveEntry(node.assetEntry, parent, false, false);
                                    entries.Add(node.assetEntry);
                                }
                                foreach (AssetGroup modifiedGroup in modifiedGroups)
                                    AssetBundleUtility.OpenAssetIfUsingVCIntegration(modifiedGroup);
                                m_GroupEditor.settings.SetDirty(ModificationEvent.EntryMoved, entries, true, false);
                            }
                        }
                    }
                }
            }

            return visualMode;
        }

        bool SafeMoveResourcesToGroup(AssetGroup targetGroup, List<AssetEntryTreeViewItem> itemList)
        {
            var guids = new List<string>();
            var paths = new List<string>();
            foreach (AssetEntryTreeViewItem child in itemList)
            {
                if (child != null)
                {
                    guids.Add(child.assetEntry.guid);
                    paths.Add(child.assetEntry.assetPath);
                }
            }
            return AssetBundleUtility.SafeMoveResourcesToGroup(m_GroupEditor.settings, targetGroup, paths, guids);
        }

        DragAndDropVisualMode HandleDragAndDropPaths(AssetEntryTreeViewItem target, DragAndDropArgs args)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.None;

            bool containsGroup = false;
            foreach (var path in DragAndDrop.paths)
            {
                if (PathPointsToAssetGroup(path))
                {
                    containsGroup = true;
                    break;
                }
            }

            bool parentGroupIsReadOnly = target?.assetGroup != null && target.assetGroup.readOnly;
            if (target == null && !containsGroup || parentGroupIsReadOnly)
                return DragAndDropVisualMode.Rejected;

            foreach (String path in DragAndDrop.paths)
            {
                if (!AssetBundleUtility.IsPathValidForEntry(path) && (!PathPointsToAssetGroup(path) && target != rootItem))
                    return DragAndDropVisualMode.Rejected;
            }
            visualMode = DragAndDropVisualMode.Copy;

            if (args.performDrop && visualMode != DragAndDropVisualMode.Rejected)
            {
                if (!containsGroup)
                {
                    AssetGroup parent = null;
                    bool targetIsGroup = false;
                    if (target.assetGroup != null)
                    {
                        parent = target.assetGroup;
                        targetIsGroup = true;
                    }
                    else if (target.assetEntry != null)
                        parent = target.assetEntry.parentGroup;

                    if (parent != null)
                    {
                        var resourcePaths = new List<string>();
                        var nonResourceGuids = new List<string>();
                        foreach (var p in DragAndDrop.paths)
                        {
                            if (AssetBundleUtility.IsInResources(p))
                                resourcePaths.Add(p);
                            else
                                nonResourceGuids.Add(AssetDatabase.AssetPathToGUID(p));
                        }

                        bool canMarkNonResources = true;
                        if (resourcePaths.Count > 0)
                            canMarkNonResources = AssetBundleUtility.SafeMoveResourcesToGroup(m_GroupEditor.settings, parent, resourcePaths, null);

                        if (canMarkNonResources)
                        {
                            if (nonResourceGuids.Count > 0)
                            {
                                var entriesMoved = new List<AssetEntry>();
                                var entriesCreated = new List<AssetEntry>();
                                m_GroupEditor.settings.CreateOrMoveEntries(nonResourceGuids, parent, entriesCreated, entriesMoved, false, false);

                                if (entriesMoved.Count > 0)
                                    m_GroupEditor.settings.SetDirty(ModificationEvent.EntryMoved, entriesMoved, true);
                                if (entriesCreated.Count > 0)
                                    m_GroupEditor.settings.SetDirty(ModificationEvent.EntryAdded, entriesCreated, true);

                                AssetBundleUtility.OpenAssetIfUsingVCIntegration(parent);
                            }

                            if (targetIsGroup)
                            {
                                SetExpanded(target.id, true);
                            }
                        }
                    }
                }
                else
                {
                    bool modified = false;
                    foreach (var p in DragAndDrop.paths)
                    {
                        if (PathPointsToAssetGroup(p))
                        {
                            AssetGroup loadedGroup = AssetDatabase.LoadAssetAtPath<AssetGroup>(p);
                            if (loadedGroup != null)
                            {
                                if (m_GroupEditor.settings.FindGroup(g => g.guid == loadedGroup.guid) == null)
                                {
                                    m_GroupEditor.settings.assetGroups.Add(loadedGroup);
                                    modified = true;
                                }
                            }
                        }
                    }

                    if (modified)
                        m_GroupEditor.settings.SetDirty(ModificationEvent.GroupAdded,
                            m_GroupEditor.settings, true, true);
                }
            }
            return visualMode;
        }

        private bool PathPointsToAssetGroup(string path)
        {
            return AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(AssetGroup);
        }
    }

   

    static class MyExtensionMethods
    {
        // Find digits in a string
        static Regex s_Regex = new Regex(@"\d+", RegexOptions.Compiled);

        public static IEnumerable<T> Order<T>(this IEnumerable<T> items, Func<T, string> selector, bool ascending)
        {
            if (EditorPrefs.HasKey("AllowAlphaNumericHierarchy") && EditorPrefs.GetBool("AllowAlphaNumericHierarchy"))
            {
                // Find the length of the longest number in the string
                int maxDigits = items
                    .SelectMany(i => s_Regex.Matches(selector(i)).Cast<Match>().Select(digitChunk => (int?)digitChunk.Value.Length))
                    .Max() ?? 0;

                // in the evaluator, pad numbers with zeros so they all have the same length
                var tempSelector = selector;
                selector = i => s_Regex.Replace(tempSelector(i), match => match.Value.PadLeft(maxDigits, '0'));
            }

            return ascending ? items.OrderBy(selector) : items.OrderByDescending(selector);
        }
    }
}
