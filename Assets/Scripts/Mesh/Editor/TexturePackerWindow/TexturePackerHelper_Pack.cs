/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-06-02 15:19:42
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OnemtEditor.Define;
using System.IO;
using OnemtEditor.Helper;
using Onemt.Core.Util;

namespace OnemtEditor.TexturePacker
{
    public static partial class TexturePackerHelper
    {
        public static void PackerAnimationAtlas(string path, bool compress)
        {
            //图集名称
            string name = Path.GetFileNameWithoutExtension(path);
            string targetPath = $"{ExportSpriteMeshHelper.kAnimationAtlasPath}/{name}";
            FileHelper.CreateDirectory(targetPath);
            PackerAtlas(path,ExportSpriteMeshHelper.kAnimationAtlasIconPath,targetPath, compress);
        }

        public static void PackerAtlas(string path, string atlasSourcePath, string atlasPath, bool compress)
        {
            if (!path.Contains(atlasSourcePath))
            {
                EditorUtility.DisplayDialog("Error", "请选择图集目录进行操作!", "ok");
                return;
            }

            int stepCount = 3;
            int step = 0;

            // 生成图集 png、txt 文件.
            string name = Path.GetFileNameWithoutExtension(path);
            EditorHelper.DisplayProgressBar(name, "Generate Atlas", (++step) / stepCount);
            GenerateAtlas(name, path, atlasPath, TrimMode.Trim);

            EditorHelper.DisplayProgressBar(name, "Import Atlas", (++step) / stepCount);
            //ImportAtlas(name, path, EditorPathDefine.kPathAtlasTemp, EditorPathDefine.kPathAtlas, compress);
            ImportAtlas(name, path, atlasPath, compress);

            EditorHelper.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        public static void PackerAtlas(string path, bool compress)
        {
            PackerAtlas(path, EditorPathDefine.kPathAtlasSource, EditorPathDefine.kPathAtlas, compress);
        }


        /// <summary>
        ///   <para> 调用shell、bat脚本生成 *.png *.txt 文件. </para>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pathOrg"></param>
        /// <param name="pathDest"></param>
        /// <param name="trimMode"></param>
        public static void GenerateAtlas(string name, string pathOrg, string pathDest, TrimMode trimMode = TrimMode.None)
        {
            if (!Directory.Exists(pathOrg))
            {
                Debug.LogErrorFormat("Directory Not Exists: {0}", pathOrg);
                return;
            }

            string filePath = Path.Combine(pathDest, name);
            string batFilePath = GetTexturePackerBatPath("maker");
            if (string.IsNullOrEmpty(batFilePath))
                return;

            FileHelper.CreateDirectory(pathDest);

            Debug.LogFormat("pathOrg: {0}, dstPath: {1}", pathOrg, filePath);

            int padding = 0;
            string args = string.Format("{0} {1} {2} {3}", pathOrg, filePath, trimMode.ToString(), padding);
            ShellHelper.ProcessCommand(batFilePath, args);

            string pathTxt = string.Format("{0}.txt", filePath);
            if (!File.Exists(pathTxt))
            {
                Debug.LogErrorFormat("TexturePacker Config not exist: {0}", name);
                return;
            }

            string pathPNG = string.Format("{0}.png", filePath);

            AssetDatabase.ImportAsset(pathPNG, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(pathTxt, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        public static void ImportAtlas(string name, string pathAtlasIcon, string pathAtlas, bool compress = true)
        {
            //pathAtlas = Path.Combine(pathAtlas, name);
            //FileHelper.CreateDirectory(pathAtlas);

            string pathTxt = string.Format("{0}/{1}.txt", pathAtlas, name);
            if (!File.Exists(pathTxt))
            {
                Debug.LogErrorFormat("TexturePacker Config Not Exist: {0}", pathTxt);
                return;
            }
            string text = FileHelper.ReadTextFromFile(pathTxt);

            string pathPNG = string.Format("{0}/{1}.png", pathAtlas, name);
            TextureImporter textureImporter = AssetImporter.GetAtPath(pathPNG) as TextureImporter;
            if (textureImporter == null)
            {
                Debug.LogErrorFormat("{0} is null.", pathPNG);
                return;
            }

            bool isAllRGBSPrites = true;
            List<SpriteMetaData> metaDatas = GenerateSpriteMetaDatas(text);
            CorrectSpriteMetaData(pathAtlasIcon, metaDatas, ref isAllRGBSPrites);

            textureImporter.spritesheet = metaDatas.ToArray();
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Multiple;
            textureImporter.sRGBTexture = false;
            textureImporter.filterMode = FilterMode.Bilinear;
            textureImporter.mipmapEnabled = false;
            textureImporter.isReadable = false;
            textureImporter.maxTextureSize = 2048;
            // TODO zm, 这里的alphaSource 需要处理，在生成png时就需要进行处理，
            // 可在这边进行验证是否 命名和 alphaSource 有冲突
            if (isAllRGBSPrites)
                textureImporter.alphaSource = TextureImporterAlphaSource.None;
            else
                textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;

            textureImporter.SaveAndReimport();
            AssetDatabase.ImportAsset(pathPNG, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        /// <summary>
        ///   <para> 矫正 pivot、alignment、border. </para>
        ///   <para> border 需要计算上裁剪区域.  border - ourborder. </para>
        /// </summary>
        /// <param name="pathAtlasIcon"> 图集图片路径. </param>
        /// <param name="spriteMetaDatas"></param>
        private static void CorrectSpriteMetaData(string pathAtlasIcon, List<SpriteMetaData> spriteMetaDatas, ref bool isAllRGBSPrites)
        {
            for (int i = 0; i < spriteMetaDatas.Count; ++ i)
            {
                var spriteMetaData = spriteMetaDatas[i];
                string pathPNG = string.Format("{0}/{1}.png", pathAtlasIcon, spriteMetaData.name);
                TextureImporter importer = AssetImporter.GetAtPath(pathPNG) as TextureImporter;
                if (importer == null)
                {
                    List<string> directorys = new List<string>();
                    GetSubDirectory(pathAtlasIcon, directorys);

                    if (directorys.Count < 1)
                        continue;

                    foreach (var directory in directorys)
                    {
                        pathPNG = string.Format("{0}/{1}/{2}.png", pathAtlasIcon, directory, spriteMetaData.name);
                        importer = AssetImporter.GetAtPath(pathPNG) as TextureImporter;
                        if (importer != null)
                            break;
                    }

                    if (importer == null)
                        continue;
                }

                if (importer.DoesSourceTextureHaveAlpha())
                    isAllRGBSPrites = false;

                Vector4 spriteBorder = importer.spriteBorder;
                Vector4 clipBorder = spriteMetaData.border;         // 这里的border 是经过计算TexturePacker 裁剪的边缘。
                if (spriteBorder != Vector4.zero)
                    spriteBorder -= clipBorder;

                spriteMetaData.border = spriteBorder;
                spriteMetaData.pivot = importer.spritePivot;
                spriteMetaData.alignment = EditorHelper.PivotToAlignment(importer.spritePivot);
                spriteMetaDatas[i] = spriteMetaData;
            }
        }


        private static string GetTexturePackerBatPath(string name)
        {
            string suffix;
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                    suffix = ".sh";
                    break;
                case RuntimePlatform.WindowsEditor:
                    suffix = ".bat";
                    break;
                default:
                    Debug.LogError("Error Platform.");
                    return "";
            }

            string path = Path.Combine(System.Environment.CurrentDirectory, "Tools/TexturePacker/", name + suffix);
            if (Application.platform == RuntimePlatform.OSXEditor)
                ShellHelper.ProcessCommand("chmod", string.Format("+x {0}", path));

            return path;
        }

        private static void GetSubDirectory(string rootPath, List<string> directorys)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(rootPath);
            FileInfo[] fileInfos = directoryInfo.GetFiles("*", SearchOption.TopDirectoryOnly);
            if (fileInfos.Length <= 0)
                return;

            for (int i = 0; i < fileInfos.Length; ++ i)
            {
                directorys.Add(fileInfos[i].Name.Replace(".meta", ""));
            }

            DirectoryInfo[] directoryInfos = directoryInfo.GetDirectories();
            foreach (var info in directoryInfos)
                GetSubDirectory(info.FullName, directorys);
        }
    }
}
