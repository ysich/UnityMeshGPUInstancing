/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 14:07:01
-- 概述:
        assetbundle 导入规则：
                            1. 路径匹配规则
                            2. 分组名称规则
                            2. 是否打包一起
---------------------------------------------------------------------------------------*/

using System;
using System.Text.RegularExpressions;

namespace OnemtEditor.AssetBundle.Import
{
    [Serializable]
    public class AssetBundleImportRule
    {
        public string path;

        public string groupName;

        public string cleanedGroupName { get => groupName.Trim().Replace('/', '_').Replace('\\', '_'); }

        // 默认打包一起
        public BundlePackingMode packingMode = BundlePackingMode.PackTogether;

        /// <summary>
        ///   <para> 默认使用 LZ4.</para>
        /// </summary>
        public BundleCompressionMode compressionMode = BundleCompressionMode.LZ4;

        /// <summary>
        ///   <para> 是否与规则匹配. </para>
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public bool Match(string assetPath)
        {
            path = path.Trim();
            if (string.IsNullOrEmpty(path))
                return false;

            return Regex.IsMatch(assetPath, path);
        }

        /// <summary>
        ///   <para> 解析分组名称. </para>
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public string ParseGroupReplacement(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(groupName))
                return null;

            var replacement = AssetBundleImportRegex.ParsePath(assetPath, cleanedGroupName);

            string pathRegex = path;
            replacement = Regex.Replace(assetPath, pathRegex, replacement);

            return replacement;
        }
    }

    static class AssetBundleImportRegex
    {
        const string kPathRegex = @"\$\{PATH\[\-{0,1}\d{1,3}\]\}"; // ie: ${PATH[0]} ${PATH[-1]}

        public static string[] GetPathAtArray(string path)
        {
            return path.Split('/');
        }

        public static string GetPathAtArray(string path, int index)
        {
            return GetPathAtArray(path)[index];
        }

        public static string ParsePath(string assetPath, string replacement)
        {
            var _path = assetPath;
            int i = 0;
            var slashSplit = _path.Split('/');
            var len = slashSplit.Length - 1;
            var matches = Regex.Matches(replacement, kPathRegex);
            string[] parsedMatches = new string[matches.Count];
            foreach (var match in matches)
            {
                string v = match.ToString();
                var sidx = v.IndexOf('[') + 1;
                var eidx = v.IndexOf(']');
                int idx = int.Parse(v.Substring(sidx, eidx - sidx));
                while (idx > len)
                {
                    idx -= len;
                }
                while (idx < 0)
                {
                    idx += len;
                }
                //idx = Mathf.Clamp(idx, 0, slashSplit.Length - 1);
                parsedMatches[i++] = GetPathAtArray(_path, idx);
            }

            i = 0;
            var splitpath = Regex.Split(replacement, kPathRegex);
            string finalPath = string.Empty;
            foreach (var split in splitpath)
            {
                finalPath += splitpath[i];
                if (i < parsedMatches.Length)
                {
                    finalPath += parsedMatches[i];
                }
                i++;
            }
            return finalPath;
        }
    }
}
