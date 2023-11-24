using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Onemt.Core.Util
{
    public static class FileHelper
    {
        static AndroidJavaClass ms_FileHelper;
        static AndroidJavaClass AndroidFileHelper
        {
            get
            {
                if (ms_FileHelper == null)
                {
                    ms_FileHelper = new AndroidJavaClass("com.onemt.utils.FileHelper");
                }

                return ms_FileHelper;
            }
        }

        public static bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public static ulong GetFileSize(string path, bool noCheck = false)
        {
            if (noCheck || FileHelper.FileExists(path))
            {
                System.IO.FileInfo info = new System.IO.FileInfo(path);
                return (ulong)info.Length;
            }

            return 0;
        }

        public static string ReadTextFromFile(string path, string defaultValue = "")
        {
            string result = defaultValue;

            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists)
            {
                using (StreamReader reader = fileInfo.OpenText())
                {
                    result = reader.ReadToEnd();
                    reader.Close();
                }
            }

            return result;
        }

        public static void SaveTextToFile(string text, string path)
        {
            CreateDirectoryFromFile(path);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
            SaveBytesToPath(path, bytes);
        }

        public static void SaveBytesToPath(string path, string fileName, byte[] bytes)
        {
            CreateDirectory(path);
            try
            {
                var filePath = Path.Combine(path, fileName);
                FileStream stream = new FileStream(path, FileMode.Create);
                stream.Write(bytes, 0, bytes.Length);
                stream.Close();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e.Message);
            }
            finally
            {
            }
        }

        public static void SaveBytesToPath(string path, byte[] bytes)
        {
            CreateDirectoryFromFile(path);

            try
            {
                FileStream stream = new FileStream(path, FileMode.Create);
                stream.Write(bytes, 0, bytes.Length);
                stream.Close();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e.Message);
            }
            finally
            {
            }
        }

        public static void CreateDirectoryFromFile(string path)
        {
            path = path.Replace("\\", "/");
            int index = path.LastIndexOf("/");
            string dir = path.Substring(0, index);
            CreateDirectory(dir);
        }

        public static void CreateDirectory(string path)
        {
            if (Directory.Exists(path)) return;

            Directory.CreateDirectory(path);
        }

        public static void ClearDirectory(string dir)
        {
            if (!Directory.Exists(dir)) return;

            var files = Directory.GetFiles(dir);
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        public static bool IsDirectoryExist(string path)
        {
            return Directory.Exists(path);
        }

        public static bool RenameDirectory(string pathSrc, string pathDst)
        {
            if (!Directory.Exists(pathSrc) || Directory.Exists(pathDst))
            {
                return false;
            }

            Directory.Move(pathSrc, pathDst);
            return true;
        }

        public static void DeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(path);
                    directoryInfo.Delete(true);
                }
            }
            catch (Exception e)
            {
                bool isExist = Directory.Exists(path);
                // Debugger.LogError("DeleteDirectory,是否删除成功:{0}, msg:{1}", isExist, e.Message);
            }
        }

        public delegate bool CopyFilter(string file);
        public static void CopyDirectory(string sourcePath, string destinationPath, string suffix = "", CopyFilter onFilter = null)
        {
            if (onFilter != null && onFilter(sourcePath))
            {
                return;
            }

            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }

            foreach (string file in Directory.GetFileSystemEntries(sourcePath))
            {
                if (File.Exists(file))
                {
                    FileInfo info = new FileInfo(file);
                    if (string.IsNullOrEmpty(suffix) || file.ToLower().EndsWith(suffix.ToLower()))
                    {
                        string destName = Path.Combine(destinationPath, info.Name);
                        if (!(onFilter != null && onFilter(file)))
                        {
                            File.Copy(file, destName);
                        }
                    }
                }

                if (Directory.Exists(file))
                {
                    DirectoryInfo info = new DirectoryInfo(file);
                    string destName = Path.Combine(destinationPath, info.Name);
                    CopyDirectory(file, destName, suffix, onFilter);
                }
            }
        }

        //copy assets目录下的文件到指定路径，仅限Android使用
        public static bool CopyAssetsFileTo(string strFrom, string strTo)
        {
            bool ret = false;
#if UNITY_ANDROID && !UNITY_EDITOR
		    strFrom = strFrom.Replace (GameConfig.instance.streamingAssetPath + "/", "");
		    ret = AndroidFileHelper.CallStatic<bool>("CopyFileTo", strFrom, strTo);
#else
            FileHelper.CopyFileTo(strFrom, strTo);
            ret = true;
#endif

            return ret;
        }

        public static void CopyFileTo(string pathSource, string pathDest)
        {
            DeleteFile(pathDest);
            CreateDirectoryFromFile(pathDest);
            File.Copy(pathSource, pathDest);
        }

        public static void MoveFileTo(string pathSource, string pathDest)
        {
            if (!File.Exists(pathSource))
            {
                return;
            }
            DeleteFile(pathDest);
            File.Move(pathSource, pathDest);
        }

        public static void DeleteFile(string path, bool noCheck = false)
        {
            try
            {
                if (noCheck || File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception e)
            {
                // Debugger.LogError(e.Message);
            }
        }

        public static bool TryDeleteFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return false;

                File.Delete(path);
                return true;
            }
            catch (Exception e)
            {
                // Debugger.LogError(e.Message);
                return false;
            }
        }

        //获取所有子文件
        public static string[] GetAllChildFiles(string path, string suffix = "", SearchOption option = SearchOption.AllDirectories)
        {
            string strPattner = "*";
            if (suffix.Length > 0 && suffix[0] != '.')
            {
                strPattner += "." + suffix;
            }
            else
            {
                strPattner += suffix;
            }

            string[] files = Directory.GetFiles(path, strPattner, option);
            var count = 0;
            for (int i = 0; i < files.Length; i++)
            {
                var name = files[i];
                if (name.Contains(".DS_Store"))
                {
                    files[i] = null;
                    continue;
                }
                else
                {
                    count++;
                }
            }

            string[] filesWithoutDSStore = new string[count];
            var index = 0;
            foreach (var name in files)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    filesWithoutDSStore[index] = name;
                    index++;
                }
            }
            if (index != count)
            {
                // Debugger.LogError("错啦！");
            }
            return filesWithoutDSStore;
        }

        /// <summary>
        ///   <para> 移除目标文件夹中存在但是在源文件夹中不存在的的文件. </para>
        /// </summary>
        /// <param name="srcPath"> 源文件夹. </param>
        /// <param name="checkPath"> 目标文件夹. </param>
        public static void RidOfUnNecessaryFile(string srcPath, string checkPath)
        {
            // Debugger.Log("RidOfUnNecessaryFile, srcPath:{0}, checkPath:{1}", srcPath, checkPath);
            if (!Directory.Exists(checkPath))
            {
                // Debugger.Log("目标路径不存在");
                return;
            }
            if (!Directory.Exists(srcPath))
            {
                // Debugger.Log("参考路径不存在，删除目标路径整个目录");
                DeleteDirectory(checkPath);
                CreateDirectory(checkPath);
                return;
            }


            string[] allSrcFiles = Directory.GetFiles(srcPath);
            string[] allCheckFiles = Directory.GetFiles(checkPath);
            Dictionary<string, bool> dicSrcFiles = new Dictionary<string, bool>();
            for (int i = 0; i < allSrcFiles.Length; i++)
            {
                string strFile = Path.GetFileName(allSrcFiles[i]);
                dicSrcFiles.Add(strFile, true);
            }
            for (int i = 0; i < allCheckFiles.Length; i++)
            {
                string strFile = allCheckFiles[i];
                string filename = Path.GetFileName(strFile);
                if (!dicSrcFiles.ContainsKey(filename))
                {
                    // Debugger.Log("删除多余文件：{k0}", strFile);
                    DeleteFile(strFile);
                }
            }
            // Debugger.Log("清除完成！");
        }

        public static string GetWWWPath(string path)
        {
            bool addFileHead = true;

#if UNITY_ANDROID && !UNITY_EDITOR || UNITY_EDITOR_WIN
        // 如果是读取apk里的资源,不需要加file:///,其它情况都要加
        if (path.Contains (Application.streamingAssetsPath)) {
            addFileHead = false;
        }
#endif

            if (addFileHead)
            {
                bool isTwoSlash = false;
#if UNITY_IOS
                isTwoSlash = true;
#else
            if(System.Environment.OSVersion.Platform == System.PlatformID.Unix)
            {
                isTwoSlash = true;
            }
#endif

                if (isTwoSlash)
                {
                    path = string.Format("file://{0}", path);
                }
                else
                {
                    path = string.Format("file:///{0}", path);
                }
            }

            return path;
        }
    }
}
