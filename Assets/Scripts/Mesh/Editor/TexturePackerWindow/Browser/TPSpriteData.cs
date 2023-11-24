using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;


namespace OnemtEditor.TexturePacker.Browser
{
	public enum TPSpriteStatus
	{
		Normal,			// 正常状态
		SpriteNew,		// 新增碎图
		SprteMiss,		// 只在图集目录 碎图丢失
	}

	public class TPSpriteData 
	{
		// sprite名字
		public string SpriteName{get{return m_spriteName;}}
		public Rect rect{get{return m_spriteInfo.rect;}}
		public TPSpriteStatus SpriteStatus{get{return m_spriteSatus;}}
		public string OriginPath { get{return m_originPath; }}

		// 图集名字
		private string m_atlasName;
		private string m_spriteName;
		private SpriteMetaData m_spriteInfo;
		private string m_originPath;
		private TPSpriteStatus m_spriteSatus = TPSpriteStatus.Normal;
		private Texture2D m_originTexture;

		private static Texture2D s_iconNoral;
		private static Texture2D s_iconMiss;
		private static Texture2D s_iconNew;

		

		public TPSpriteData(string atlasName, string spriteName, SpriteMetaData info)
		{
			this.m_atlasName = atlasName;
			this.m_spriteName = spriteName;
			this.m_spriteInfo = info;
			m_originPath = string.Format("{0}/{1}/{2}.png", TpPathDef.SpritePath, m_atlasName, SpriteName);
			if(!File.Exists(m_originPath))
			{
				m_spriteSatus = TPSpriteStatus.SprteMiss;
			}
		}

		public TPSpriteData(string atlasName, string spriteName)
		{
			this.m_atlasName = atlasName;
			this.m_spriteName = spriteName;
			m_originPath = string.Format("{0}/{1}/{2}.png", TpPathDef.SpritePath, m_atlasName, SpriteName);
			m_spriteSatus = TPSpriteStatus.SpriteNew;
		}

		public string GetFolderPathOfSprite()
		{
			return string.Format("{0}/{1}", TpPathDef.SpritePath, m_atlasName);
		}

		public Texture2D LoadOriginTexture()
		{
			// 图片只在图集下有，碎图目录不存在
			if(m_spriteSatus == TPSpriteStatus.SprteMiss)
			{
				return null;
			}
			if(m_originTexture != null)
			{
				return m_originTexture;
			}
			m_originTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(m_originPath);
			return m_originTexture;
		}

		public bool HaveAlphaChannel()
		{
			TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(m_originPath);
			if(importer == null)
			{
				return false;
			}
			return importer.DoesSourceTextureHaveAlpha();
		}

		// 获取图片的状态图标
		public Texture2D GetStatusIcon()
		{
			if(s_iconMiss == null)
			{
				s_iconMiss = EditorGUIUtility.FindTexture("lightMeter/redLight");
				s_iconNew = EditorGUIUtility.FindTexture("lightMeter/greenLight");
			}
			switch(m_spriteSatus)
			{
				case TPSpriteStatus.Normal:
					return null;
				case TPSpriteStatus.SpriteNew:
					return s_iconNew;
				case TPSpriteStatus.SprteMiss:
					return s_iconMiss;
			}
			return null;
		}
	}

}
