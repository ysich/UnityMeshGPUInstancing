/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 14:52:19
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor;

namespace OnemtEditor.AssetBundle.Settings
{
    internal class AssetBundleAssetTree
    {
        internal class TreeNode
        {
            internal string Path { get; set; }
            internal bool IsAddressable { get; set; }
            public bool IsFolder { get; set; }
            internal bool HasEnumerated { get; set; }

            internal Dictionary<string, TreeNode> Children { get; set; }

            internal TreeNode(string path)
            {
                Path = path;
            }

            internal bool GetChild(string p, out TreeNode node)
            {
                if (Children != null)
                    return Children.TryGetValue(p, out node);
                node = null;
                return false;
            }

            internal void AddChild(string p, TreeNode node)
            {
                if (Children == null)
                    Children = new Dictionary<string, TreeNode>();
                Children.Add(p, node);
            }
        }

        TreeNode m_Root;

        internal AssetBundleAssetTree()
        {
            m_Root = new TreeNode("");
            m_Root.IsFolder = true;
        }

        IEnumerable<string> EnumAddressables(TreeNode node, bool recursive, string relativePath)
        {
            if (node.Children != null)
            {
                List<TreeNode> curDirectory = new List<TreeNode>(node.Children.Values.Where(x => !x.IsAddressable));
                curDirectory.Sort((x, y) => { return string.Compare(x.Path, y.Path); });
                string pathPrepend = string.IsNullOrEmpty(relativePath) ? "" : $"{relativePath}/";
                foreach (var v in curDirectory)
                {
                    if (!v.IsFolder)
                        yield return pathPrepend + v.Path;
                }

                if (recursive)
                {
                    foreach (var v in curDirectory)
                        if (v.Children != null)
                            foreach (string v2 in EnumAddressables(v, true, pathPrepend + v.Path))
                                yield return v2;
                }
            }
        }

        internal IEnumerable<string> Enumerate(string path, bool recursive)
        {
            TreeNode node = FindNode(path, false);
            if (node == null)
                throw new Exception($"Path {path} was not in the enumeration tree");
            if (!node.HasEnumerated)
                throw new Exception($"Path {path} cannot be enumerated because the file system has not enumerated them yet");

            return EnumAddressables(node, recursive, path);
        }

        internal TreeNode FindNode(string path, bool shouldAdd)
        {
            TreeNode it = m_Root;
            foreach (string p in path.Split('/'))
            {
                if (!it.GetChild(p, out TreeNode it2))
                {
                    if (!shouldAdd)
                        return null;
                    it2 = new TreeNode(p);
                    it.AddChild(p, it2);
                    it.IsFolder = true;
                }
                it = it2;
            }
            return it;
        }
    }

    internal class AddressablesFileEnumeration
    {
        internal static AssetBundleAssetTree BuildAddressableTree(AssetBundleAssetSettings settings, IBuildLogger logger = null)
        {
            using (logger.ScopedStep(LogLevel.Verbose, "BuildAddressableTree"))
            {
                if (!ExtractAddressablePaths(settings, out HashSet<string> paths))
                    return null;

                AssetBundleAssetTree tree = new AssetBundleAssetTree();
                foreach (string path in paths)
                {
                    AssetBundleAssetTree.TreeNode node = tree.FindNode(path, true);
                    node.IsAddressable = true;
                }
                return tree;
            }
        }

        static bool ExtractAddressablePaths(AssetBundleAssetSettings settings, out HashSet<string> paths)
        {
            paths = new HashSet<string>();
            bool hasAddrFolder = false;
            foreach (AssetGroup group in settings.assetGroups)
            {
                if (group == null)
                    continue;
                foreach (AssetEntry entry in group.entries)
                {
                    string convertedPath = entry.assetPath;
                    if (!hasAddrFolder && AssetDatabase.IsValidFolder(convertedPath))
                        hasAddrFolder = true;
                    paths.Add(convertedPath);
                }
            }
            return hasAddrFolder;
        }

        static void AddLocalFilesToTreeIfNotEnumerated(AssetBundleAssetTree tree, string path, IBuildLogger logger)
        {
            AssetBundleAssetTree.TreeNode pathNode = tree.FindNode(path, true);

            if (pathNode == null || pathNode.HasEnumerated) // Already enumerated
                return;

            pathNode.HasEnumerated = true;
            using (logger.ScopedStep(LogLevel.Info, $"Enumerating {path}"))
            {
                foreach (string filename in Directory.EnumerateFileSystemEntries(path, "*.*", SearchOption.AllDirectories))
                {
                    if (!AssetBundleUtility.IsPathValidForEntry(filename) || string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(filename)))
                        continue;
                    string convertedPath = filename.Replace('\\', '/');
                    var node = tree.FindNode(convertedPath, true);
                    node.IsFolder = AssetDatabase.IsValidFolder(filename);
                    node.HasEnumerated = true;
                }
            }
        }

        internal class AddressablesFileEnumerationScope : IDisposable
        {
            AssetBundleAssetTree m_PrevTree;
            internal AddressablesFileEnumerationScope(AssetBundleAssetTree tree)
            {
                m_PrevTree = m_PrecomputedTree;
                m_PrecomputedTree = tree;
            }

            public void Dispose()
            {
                m_PrecomputedTree = m_PrevTree;
            }
        }

        internal class AddressablesFileEnumerationCache : IDisposable
        {
            internal AddressablesFileEnumerationCache(AssetBundleAssetSettings settings, bool prepopulateAssetsFolder, IBuildLogger logger)
            {
                BeginPrecomputedEnumerationSession(settings, prepopulateAssetsFolder, logger);
            }

            public void Dispose()
            {
                EndPrecomputedEnumerationSession();
            }
        }

        internal static AssetBundleAssetTree m_PrecomputedTree;

        static void BeginPrecomputedEnumerationSession(AssetBundleAssetSettings settings, bool prepopulateAssetsFolder, IBuildLogger logger)
        {
            using (logger.ScopedStep(LogLevel.Info, "AddressablesFileEnumeration.BeginPrecomputedEnumerationSession"))
            {
                m_PrecomputedTree = BuildAddressableTree(settings, logger);
                if (m_PrecomputedTree != null && prepopulateAssetsFolder)
                    AddLocalFilesToTreeIfNotEnumerated(m_PrecomputedTree, "Assets", logger);
            }
        }

        static void EndPrecomputedEnumerationSession()
        {
            m_PrecomputedTree = null;
        }

        public static List<string> EnumerateAddressableFolder(string path, AssetBundleAssetSettings settings, bool recurseAll, IBuildLogger logger = null)
        {
            if (!AssetDatabase.IsValidFolder(path))
                throw new Exception($"Path {path} cannot be enumerated because it does not exist");

            AssetBundleAssetTree tree = m_PrecomputedTree != null ? m_PrecomputedTree : BuildAddressableTree(settings, logger);
            if (tree == null)
                return new List<string>();

            AddLocalFilesToTreeIfNotEnumerated(tree, path, logger);

            List<string> files = new List<string>();
            using (logger.ScopedStep(LogLevel.Info, $"Enumerating Addressables Tree {path}"))
            {
                foreach (string file in tree.Enumerate(path, recurseAll))
                {
                    if (BuiltinSceneCache.Contains(file))
                        continue;
                    files.Add(file);
                }
            }
            return files;
        }
    }
}
