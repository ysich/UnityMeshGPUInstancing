/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 19:23:28
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.U2D;
#endif
using UnityEngine.U2D;

namespace Onemt.Core
{
    public class SpriteAtlasMapData : ScriptableObject
    {
        public List<SpriteAtlasMapInfo> spriteAtlasMapInfos = new List<SpriteAtlasMapInfo>();

#if UNITY_EDITOR
        //private const string kPath = "Assets/";
        private const string kPathAtlas = "Assets/BundleAssets/UI/Atlas/";
        private const string kPathIcon = "Assets/BundleAssets/UI/AtlasIcon/";
        //private const string kPath = "Assets/BundleAssets/UI/AtlasMapData/";
        private const string kPathAtlasMapData = @"Assets/BundleAssets/UI/AtlasMapData/SpriteAtlasMapData.asset";
        public static void CreateSpriteAtlasMapData(bool shouldPackAllAtlas)
        {
            if (shouldPackAllAtlas)
            {
                SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
            }

            SpriteAtlasMapData spriteAtlasMapData = AssetDatabase.LoadAssetAtPath(kPathAtlasMapData, typeof(SpriteAtlasMapData)) as SpriteAtlasMapData;
            bool exist = true;
            if (spriteAtlasMapData == null)
            {
                spriteAtlasMapData = ScriptableObject.CreateInstance<SpriteAtlasMapData>();
                exist = false;
            }
            spriteAtlasMapData.spriteAtlasMapInfos.Clear();
            DirectoryInfo directoryInfo = new DirectoryInfo(kPathAtlas);
            FileInfo[] files = directoryInfo.GetFiles("*.spriteatlas", SearchOption.AllDirectories);
            List<string> listSpriteName = new List<string>();


            for (int i = 0; i < files.Length; ++i)
            {
                SpriteAtlas spriteAtlas = UnityEditor.AssetDatabase.LoadAssetAtPath(Path.Combine(kPathAtlas, files[i].Name), typeof(SpriteAtlas)) as SpriteAtlas;
                if (spriteAtlas == null)
                {
                    continue;
                }
                string spriteAtlasName = Path.GetFileNameWithoutExtension(files[i].Name);

                if (shouldPackAllAtlas)
                {
                    // 如果是强制 Pack Atlases之后则直接取spriteAtlas中的数据进行刷新 mapdata
                    Sprite[] sprites = new Sprite[spriteAtlas.spriteCount];
                    spriteAtlas.GetSprites(sprites);
                    for (int j = 0; j < sprites.Length; ++j)
                    {
                        if (!sprites[j])
                            continue;

                        string spriteName = sprites[j].name.Replace("(Clone)", "");
                        spriteAtlasMapData.spriteAtlasMapInfos.Add(new SpriteAtlasMapInfo(spriteName, spriteAtlasName, Vector4.zero));

                        if (listSpriteName.Contains(spriteName))
                        {
                            UnityEngine.Debug.LogErrorFormat(" 重复的 Texture 名称     ： {0}", spriteName);
                            continue;
                        }
                        listSpriteName.Add(spriteName);
                    }

                }
                else
                {
                    // 未pack atlas，则直接取pack数据进行刷新 mapdata
                    var objs = spriteAtlas.GetPackables();
                    foreach (var obj in objs)
                    {
                        if (obj == null) continue;
                        if (obj is Texture2D tex2D)
                        {
                            string spriteName = obj.name;
                            spriteAtlasMapData.spriteAtlasMapInfos.Add(new SpriteAtlasMapInfo(spriteAtlasName, spriteName, Vector4.zero));

                            if (listSpriteName.Contains(spriteName))
                            {
                                UnityEngine.Debug.LogErrorFormat(" 重复的 Texture 名称     ： {0}", spriteName);
                                continue;
                            }
                            listSpriteName.Add(spriteName);
                        }
                        else
                        {
                            string name = obj.name;
                            //string path = Path.Combine(kPathIcon, name);
                            string path = GetSpriteAtlasPath(name);
                            if (string.IsNullOrEmpty(path))
                            {
                                UnityEngine.Debug.LogErrorFormat("Can not finde atlas Path With Name : {0}", name);
                                continue;
                            }
                            //DirectoryInfo texDirectoryInfo = new DirectoryInfo(path);
                            var texFileInfos = GetAllTexture2DFileInfos(path, s_TextureType);
                            foreach (var texFileInfo in texFileInfos)
                            {
                                // Sprite[] sprites = (Sprite[])AssetDatabase.LoadAllAssetsAtPath(texFileInfo.FullName);
                                // foreach (var sprite in sprites)
                                // {
                                //     string spriteName = sprite.name;
                                //     spriteAtlasMapData.spriteAtlasMapInfos.Add(new SpriteAtlasMapInfo(spriteAtlasName, spriteName));

                                //     if (listSpriteName.Contains(spriteName))
                                //     {
                                //         UnityEngine.Debug.LogErrorFormat(" 重复的 Texture 名称     ： {0}", spriteName);
                                //         continue;
                                //     }
                                //     listSpriteName.Add(spriteName);
                                // }

                                string spriteName = Path.GetFileNameWithoutExtension(texFileInfo.Name);
                                spriteAtlasMapData.spriteAtlasMapInfos.Add(new SpriteAtlasMapInfo(spriteAtlasName, spriteName, Vector4.zero));

                                if (listSpriteName.Contains(spriteName))
                                {
                                    UnityEngine.Debug.LogErrorFormat(" 重复的 Texture 名称     ： {0}", spriteName);
                                    continue;
                                }
                                listSpriteName.Add(spriteName);
                            }
                        }
                    }
                }
            }

            if (!exist)
                AssetDatabase.CreateAsset(spriteAtlasMapData, kPathAtlasMapData);

            EditorUtility.SetDirty(spriteAtlasMapData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        ///   <para> tp 模式下的数据处理. </para>
        /// </summary>
        //public static void CreateSpriteAtlasMapData_TP()
        //{
        //    SpriteAtlasMapData spriteAtlasMapData = AssetDatabase.LoadAssetAtPath(kPathAtlasMapData, typeof(SpriteAtlasMapData)) as SpriteAtlasMapData;
        //    bool exist = true;
        //    if (spriteAtlasMapData == null)
        //    {
        //        spriteAtlasMapData = ScriptableObject.CreateInstance<SpriteAtlasMapData>();
        //        exist = false;
        //    }
        //    spriteAtlasMapData.spriteAtlasMapInfos.Clear();
        //    DirectoryInfo directoryInfo = new DirectoryInfo(kPathAtlas);
        //    FileInfo[] files = directoryInfo.GetFiles("*.png", SearchOption.AllDirectories);
        //    List<string> listSpriteName = new List<string>();

        //    foreach (var file in files)
        //    {
        //        string spriteAtlasName = Path.GetFileNameWithoutExtension(file.Name);
        //        var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(Path.Combine(kPathAtlas, file.Name));
        //        foreach (var sprite in sprites)
        //        {
        //            string spriteName = sprite.name;
        //            spriteAtlasMapData.spriteAtlasMapInfos.Add(new SpriteAtlasMapInfo(spriteName, spriteAtlasName, Vector4.zero));

        //            if (listSpriteName.Contains(spriteName))
        //            {
        //                UnityEngine.Debug.LogErrorFormat(" 重复的 Texture 名称     ： {0}", spriteName);
        //                continue;
        //            }
        //            listSpriteName.Add(spriteName);
        //        }
        //    }

        //    if (!exist)
        //        AssetDatabase.CreateAsset(spriteAtlasMapData, kPathAtlasMapData);

        //    EditorUtility.SetDirty(spriteAtlasMapData);
        //    AssetDatabase.SaveAssets();
        //    AssetDatabase.Refresh();
        //}

        private static string GetSpriteAtlasPath(string atlasName)
        {
            var subFolders = Directory.GetDirectories(kPathIcon);
            foreach (var subFolder in subFolders)
            {
                var atlasFolders = Directory.GetDirectories(subFolder);
                foreach (var atlasFolder in atlasFolders)
                {
                    if (atlasFolder.Contains(atlasName))
                        return atlasFolder;
                }
            }

            return "";
        }

        private static string s_TextureType = "*.jpg,*.png,*.bmp,*.tga";
        private static List<FileInfo> GetAllTexture2DFileInfos(string path, string textureType)
        {
            List<FileInfo> fileInfos = new List<FileInfo>();
            DirectoryInfo texDirectoryInfo = new DirectoryInfo(path);
            string[] textureTypeArray = textureType.Split(',');

            for (int i = 0; i < textureTypeArray.Length; i++)
            {
                FileInfo[] texFileInfos = texDirectoryInfo.GetFiles(textureTypeArray[i], SearchOption.AllDirectories);
                fileInfos.AddRange(texFileInfos);
            }

            return fileInfos;
        }
#endif
    }

    [Serializable]
    public class SpriteAtlasMapInfo
    {
        public string spriteName;

        public string spriteAtlasName;

        public Vector4 padding;

        public SpriteAtlasMapInfo(string spriteName, string spriteAtlasName, Vector4 padding)
        {
            this.spriteName = spriteName;
            this.spriteAtlasName = spriteAtlasName;
            this.padding = padding;
        }
    }
}
