/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-04-10 19:25:29
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using UnityEngine;
using System;

namespace Onemt.Core.Util
{
    public class ABFileHelper
    {
        public static Dictionary<string, ABFileInfo> ReadAbMD5Info(string path)
        {
            string text = FileHelper.ReadTextFromFile(path);
            return ReadABMD5FromText(text);
        }

        private static Dictionary<string, ABFileInfo> ReadABMD5FromText(string text)
        {
            Dictionary<string, ABFileInfo> abFileInfos = new Dictionary<string, ABFileInfo>();
            string line = "";
            try
            {
                string[] lines = text.Split(new[] { "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < lines.Length; ++i)
                {
                    line = lines[i];
                    if (string.IsNullOrEmpty(line))
                        continue;

                    ABFileInfo abInfo = new ABFileInfo();
                    if (abInfo.Parse(line))
                    {
                        abFileInfos.Add(abInfo.assetbundleName, abInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogErrorFormat("ReadABMD5FromText Exception,msg:{0}, rec:{1}, stack:\n{2}", ex.Message, line, ex.StackTrace);
            }

            return abFileInfos;
        }

        public static void SaveABMD5InfoToFile(Dictionary<string, ABFileInfo> fileInfos, string path)
        {
            string text = "";
            foreach (var info in fileInfos.Values)
            {
                text += string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}\n",
                    info.assetbundleName, info.md5, info.sizeLZ4, info.sizeCompress, info.version, info.patchMatchVersion,
                    info.patchABSize, info.patchFileSize, info.isRemote);
            }

            FileHelper.SaveTextToFile(text, path);
        }
    }
}
