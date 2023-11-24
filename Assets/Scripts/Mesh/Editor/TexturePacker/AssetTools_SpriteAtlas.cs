/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-05-08 16:52:45
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.U2D;
using UnityEditor.U2D;
using Onemt.Core;

public partial class AssetTools
{
    private const string kAtlasIconPath = "Assets/BundleAssets/UI/AtlasIcon";
    private const string kAtlasPath = "Assets/BundleAssets/UI/Atlas";

    //[MenuItem("Tools/图集工具/打包所有图集并且刷新 SpriteAtlasMapData")]
    public static void PackAtlasAndRefreshSpriteAtlasMapData()
    {
        RefreshSpriteAtlasMapData(true);
    }

    public static void RefreshSpriteAtlasMapData(bool shouldPackAllAtlas)
    {
        SpriteAtlasMapData.CreateSpriteAtlasMapData(shouldPackAllAtlas);
        //SpriteAtlasMapData.CreateSpriteAtlasMapData_TP();
    }

    //[MenuItem("Assets/图集工具/创建 SpriteAtlas")]
    public static void GenerateSpriteAtlas()
    {
        var subFolders = Directory.GetDirectories(kAtlasIconPath);
        foreach (var subFolder in subFolders)
        {
            var atlasFolders = Directory.GetDirectories(subFolder);
            foreach (var atlasFolder in atlasFolders)
            {
                GenerateSpriteAtlasByPath(atlasFolder);
            }
        }
    }

    private static void GenerateSpriteAtlasByPath(string path)
    {
        var fileName = Path.GetFileName(path);
        CreateSpriteAtlasMaster(fileName, path);
    }

    private static void CreateSpriteAtlasMaster(string spriteAtlasFileName, string includePath)
    {
        string spriteAtlasPath = Path.Combine(kAtlasPath, spriteAtlasFileName + ".spriteatlas");
        if (CheckSpriteAtlasFileExist(spriteAtlasPath))
            return;

        SpriteAtlas spriteAtlas = new SpriteAtlas();
        SpriteAtlasPackingSettings spriteAtlasPackingSettings = spriteAtlas.GetPackingSettings();
        spriteAtlasPackingSettings.enableRotation = false;
        spriteAtlasPackingSettings.enableTightPacking = false;
        spriteAtlasPackingSettings.padding = 2;
        spriteAtlas.SetPackingSettings(spriteAtlasPackingSettings);

        SpriteAtlasTextureSettings spriteAtlasTextureSettings = spriteAtlas.GetTextureSettings();
        spriteAtlasTextureSettings.readable = false;
        spriteAtlasTextureSettings.generateMipMaps = false;
        spriteAtlasTextureSettings.sRGB = false;
        spriteAtlasTextureSettings.filterMode = FilterMode.Bilinear;
        spriteAtlas.SetTextureSettings(spriteAtlasTextureSettings);

        SetPlatformSettings(spriteAtlas, spriteAtlasFileName);

        Object texture = AssetDatabase.LoadMainAssetAtPath(includePath);
        spriteAtlas.Add(new Object[] { texture });

        spriteAtlas.SetIncludeInBuild(true);

        AssetDatabase.CreateAsset(spriteAtlas, spriteAtlasPath);

        //CreateSpriteAtlasVariant(spriteAtlasFileName, spriteAtlas);
    }

    private static void SetPlatformSettings(SpriteAtlas spriteAtlas, string atlasName)
    {
        SetPlatformSettingsAndroid(spriteAtlas, atlasName);

        SetPlatformSettingsiOS(spriteAtlas, atlasName);
    }

    private static void SetPlatformSettingsAndroid(SpriteAtlas spriteAtlas, string atlasName)
    {
        TextureImporterPlatformSettings platformSettings = spriteAtlas.GetPlatformSettings("Android");
        if (atlasName.ToLower().Contains("_c_") || atlasName.ToLower().EndsWith("_c"))
            platformSettings.format = TextureImporterFormat.ASTC_8x8;
        else
            platformSettings.format = TextureImporterFormat.ASTC_6x6;

        platformSettings.maxTextureSize = 2048;
        platformSettings.overridden = true;
        platformSettings.compressionQuality = (int)TextureCompressionQuality.Normal;
        spriteAtlas.SetPlatformSettings(platformSettings);
    }

    private static void SetPlatformSettingsiOS(SpriteAtlas spriteAtlas, string atlasName)
    {
        TextureImporterPlatformSettings platformSettings = spriteAtlas.GetPlatformSettings("iPhone");
        platformSettings.maxTextureSize = 2048;
        if (atlasName.ToLower().Contains("_c_") || atlasName.ToLower().EndsWith("_c"))
            platformSettings.format = TextureImporterFormat.ASTC_8x8;
        else
            platformSettings.format = TextureImporterFormat.ASTC_6x6;
        platformSettings.overridden = true;
        platformSettings.compressionQuality = (int)TextureCompressionQuality.Normal;
        spriteAtlas.SetPlatformSettings(platformSettings);
    }

    private static bool CheckSpriteAtlasFileExist(string path)
    {
        SpriteAtlas spriteAtlas = UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(SpriteAtlas)) as SpriteAtlas;
        if (spriteAtlas != null)
        {
            //UnityEngine.Debug.LogErrorFormat("SpriteAtlas Already Exist  : {0}", path);
            return true;
        }

        return false;
    }
}
