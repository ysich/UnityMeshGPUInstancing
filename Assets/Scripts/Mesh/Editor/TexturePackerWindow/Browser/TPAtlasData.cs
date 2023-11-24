using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using OnemtEditor.TexturePacker;
using UnityEditor;
using UnityEngine.SocialPlatforms;

namespace OnemtEditor.TexturePacker.Browser
{
	public class TPAtlasData
	{
		public static readonly string TPAtlasCachePath = "Assets/TP/ui";
		// 从tp目录加载图集
		public static TPAtlasData CreateFromPath(string textureFullPath,string textureName)
		{
			string lastStr = ".png";
			int lastIndex = textureName.Length - lastStr.Length;
			string strAtalsName = textureName.Substring(0,lastIndex);
			TPAtlasData atlasData = new TPAtlasData(textureFullPath, strAtalsName);
			return atlasData;
		}

		public string AtlasName{get{return m_strAtlasName;}}
		public List<TPSpriteData> SpriteList{get{return m_spriteList;}}
		public bool HaveAlphaChannel{get;private set;}
		public bool HaveAbnormalStatus{get;private set;}
		public string WarningTips{get;private set; }
		//TODO:ysc,这个要删掉的
		private string m_strAtlasPath;
		private string m_strAtlasName;
		private List<TPSpriteData> m_spriteList = new List<TPSpriteData>();
		private Texture2D m_texture = null;
		
		public TPAtlasData(string strPath, string strAtlasName)
		{
			m_strAtlasPath = strPath;
			m_strAtlasName = strAtlasName;
			HaveAbnormalStatus = false;
			HaveAlphaChannel = true;
		}

		public TPSpriteData GetSpriteData(int idx)
		{
			if(idx<0 || idx>=m_spriteList.Count)
			{
				return null;
			}
			return m_spriteList[idx];
		}

		public string GetAtlasPath()
		{
			string strAtlasPath = String.Format("{0}/{1}.png",TpPathDef.AtlasTpPath,m_strAtlasName);
			return strAtlasPath;
		}

		// 加载图集信息
		public void Load()
		{
			if(m_spriteList.Count>0)
				return;

			// string strAtlasAPath = string.Format("{0}/{1}_a.png", m_strAtlasPath, m_strAtlasName);
			// HaveAlphaChannel = File.Exists(strAtlasAPath);

			HaveAbnormalStatus = false;

			// 加载图集下的
			string strAtlasPath = this.GetAtlasPath();
			TextureImporter importer = AssetImporter.GetAtPath(strAtlasPath) as TextureImporter;
			if(importer == null)
			{
				Debug.LogWarningFormat("加载图集失败，路径：{0}", strAtlasPath);
				return;
			}
			SpriteMetaData[] spriteDatas = importer.spritesheet;
			HashSet<string> setSpriteNames = new HashSet<string>();
			for(int i=0; i<spriteDatas.Length; i++)
			{
				SpriteMetaData sheetData = spriteDatas[i];
				TPSpriteData spriteData = new TPSpriteData(m_strAtlasName, sheetData.name, sheetData);
				m_spriteList.Add(spriteData);
				setSpriteNames.Add(sheetData.name);
				if(spriteData.SpriteStatus != TPSpriteStatus.Normal)
				{
					HaveAbnormalStatus = true;
				}
			}

			// 添加碎图目录下有，图集没有的
			string strSpritePath = TpPathDef.SpritePath + "/" + m_strAtlasName;
			if(!Directory.Exists(strSpritePath))
			{
				Debug.LogWarningFormat("加载碎图目录失败，路径：{0}，说明存在图集，却没有碎图，请检查！", strAtlasPath);
				return;
			}

			string[] files = Directory.GetFiles(strSpritePath, "*.png", SearchOption.TopDirectoryOnly);
			for(int i=0; i<files.Length; i++)
			{
				string spriteName = Path.GetFileNameWithoutExtension(files[i]);
				if(!setSpriteNames.Contains(spriteName))
				{
					TPSpriteData spriteData = new TPSpriteData(m_strAtlasName, spriteName);
					m_spriteList.Add(spriteData);
					HaveAbnormalStatus = true;
				}
			}

			// // 预制中如果有sprite missing也告警
			// string strAtlasPrefabPath = string.Format("{0}/{1}.prefab", m_strAtlasPath, m_strAtlasName);
			// GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(strAtlasPrefabPath);
			// if(prefab != null)
			// {
			// 	AtlasHierarchy ah = prefab.GetComponent<AtlasHierarchy>();
			// 	var sprites = ah.atlas.sprites;
			// 	foreach(var spr in sprites)
			// 	{
			// 		if(spr==null)
			// 		{
			// 			HaveAbnormalStatus = true;
			// 			break;
			// 		}
			// 	}
			// }
		}

		// 卸载
		private void Unload()
		{
			m_spriteList.Clear();
			m_texture = null;
		}

		public void Reload()
		{
			Unload();
			Load();
		}

		// 图集检查
		public void CheckAtlas()
		{
			// 图集利用率低于50%
			string strAtlasPath = this.GetAtlasPath();
			TextureImporter importer = AssetImporter.GetAtPath(strAtlasPath) as TextureImporter;
			float minPercent = 0.5f;
			Texture2D texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(strAtlasPath);
			int totalArea = texture2D.width * texture2D.height;
			float usedArea = 0;
			foreach(var item in m_spriteList)
				usedArea += item.rect.width * item.rect.height;

			float percent = usedArea / totalArea;

			if(percent <= minPercent)
			{
				Debug.Log(string.Format("{0}图集利用率低于{1}：{2}", strAtlasPath,minPercent, percent.ToString()));
				WarningTips = string.Format("图集利用率低于{0}！", minPercent * 100);
				HaveAbnormalStatus = true;
			}

			// 检查A通道是否需要：检查每张碎图，如果alpha值都不为0，那么就不需要A通道,并且原图有alpha通道
			for(int i = 0; i < m_spriteList.Count; i++)
			{
				TPSpriteData tPSpriteData = m_spriteList[i];
				if(!tPSpriteData.HaveAlphaChannel())
				{
					if(importer.DoesSourceTextureHaveAlpha())
					{
						WarningTips = "该图集不需要A通道，请让美术去除A通道！";
						HaveAbnormalStatus = true;
					}
				}
			}
		}

		public void PackAtlas()
		{
			string strSpritePath = TpPathDef.SpritePath + "/" + m_strAtlasName;
			Debug.LogFormat("PackAtlas,path:{0}", strSpritePath);
			TexturePackerHelper.PackerAtlas(strSpritePath, true);
			Reload();
		}
		
		public Texture2D GetTexture()
		{
			if(m_texture != null)
			{
				return m_texture;
			}

			string strAtlasPath = this.GetAtlasPath();
			m_texture = AssetDatabase.LoadAssetAtPath<Texture2D>(strAtlasPath);
			return m_texture;
		}
	}
}

