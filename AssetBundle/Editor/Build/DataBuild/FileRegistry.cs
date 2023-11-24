/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 15:48:51
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OnemtEditor.AssetBundle.Build.DataBuilders
{
    public class FileRegistry
    {
        private readonly HashSet<string> r_FilePaths;

        public FileRegistry() => r_FilePaths = new HashSet<string>();

        public IEnumerable<string> GetFilePaths() => new HashSet<string>(r_FilePaths);

        public void AddFile(string path) => r_FilePaths.Add(path);

        public void RemoveFile(string path)
        {
            r_FilePaths.Remove(path);
        }

        public string GetFilePathForBundle(string assetbundleName)
        {
            assetbundleName = Path.GetFileNameWithoutExtension(assetbundleName);
            return r_FilePaths.FirstOrDefault((entry) => entry.Contains(assetbundleName));
        }

        public bool ReplaceBundleEntry(string assetbundleName, string newFileRegistryEntry)
        {
            if (r_FilePaths.Contains(newFileRegistryEntry))
            {
                return false;
            }

            r_FilePaths.RemoveWhere((entry) => entry.Contains(assetbundleName));
            AddFile(newFileRegistryEntry);
            return true;
        }
    }
}
