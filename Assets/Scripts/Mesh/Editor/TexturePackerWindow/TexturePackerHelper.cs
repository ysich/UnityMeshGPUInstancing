/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-05-30 16:36:18
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace OnemtEditor.TexturePacker
{
    public static partial class TexturePackerHelper
    {
        public static List<SpriteMetaData> GenerateSpriteMetaDatas(string text)
        {
            Hashtable hashtable = text.hashtableFromJson();
            MetaData metaData = new MetaData((Hashtable)hashtable["meta"]);

            List<FrameData> frameDatas = ParseFrameData(text);

            List<SpriteMetaData> spriteMetaDatas = new List<SpriteMetaData>();
            foreach (var frameData in frameDatas)
            {
                SpriteMetaData spriteMetaData = frameData.BuildBasicSprite(0.01f, new Color32(128, 128, 128, 128));
                if (string.Equals(spriteMetaData.name, "IGNORE_SPRITE"))
                    continue;

                float left = frameData.spriteSourceSize.x;
                float top = frameData.spriteSourceSize.y;
                float right = frameData.sourceSize.x - frameData.spriteSourceSize.x - frameData.spriteSourceSize.width;
                float bottom = frameData.sourceSize.y - frameData.spriteSourceSize.y - frameData.spriteSourceSize.height;
                Vector4 border = new Vector4(left, bottom, right, top);
                spriteMetaData.border = border;
                spriteMetaDatas.Add(spriteMetaData);
            }

            return spriteMetaDatas;
        }

        /// <summary>
        ///   <para> 解析TexturePacker 生成的json文件中frame数据. </para>
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static List<FrameData> ParseFrameData(string text)
        {
            Hashtable hashtable = text.hashtableFromJson();
            MetaData metaData = new MetaData((Hashtable)hashtable["meta"]);
            List<FrameData> frameDatas = new List<FrameData>();
            Hashtable hashTableFrame = (Hashtable)hashtable["frames"];
            foreach (var frame in hashTableFrame)
            {
                DictionaryEntry entry = (DictionaryEntry)frame;
                frameDatas.Add(new FrameData((string)entry.Key, metaData.size, (Hashtable)entry.Value));
            }
            SortFrames(frameDatas);

            return frameDatas;
        }

        /// <summary>
        ///   <para> 根据sprite文件名排序 framedata. </para>
        /// </summary>
        /// <param name="frameDatas"></param>
        /// <returns></returns>
        private static List<FrameData> SortFrames(List<FrameData> frameDatas)
        {
            for (int i = frameDatas.Count - 1; i > 0; i--)
            {
                for (int j = 0; j < i; j++)
                {
                    bool flag = string.Compare(frameDatas[j + 1].name, frameDatas[j].name) < 0;
                    if (flag)
                    {
                        FrameData value = frameDatas[j + 1];
                        frameDatas[j + 1] = frameDatas[j];
                        frameDatas[j] = value;
                    }
                }
            }
            return frameDatas;
        }
    }
}
