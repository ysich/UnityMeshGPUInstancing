using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Onemt.Core;
using UnityEditor.IMGUI.Controls;
using OnemtEditor.Helper;
using UnityEngine.SocialPlatforms;

namespace OnemtEditor.TexturePacker.Browser
{
	public class TpPathDef
	{
		public static string AtlasTpPath = "Assets/BundleAssets/UI/Atlas";
		public static string SpritePath = "Assets/BundleAssets/UI/AtlasIcon";
	}
	public class TPBrowserWindow : EditorWindow 
	{
		public static void ShowTPBrowserWindow()
		{
			TPBrowserWindow window = (TPBrowserWindow)EditorWindow.GetWindow(typeof(TPBrowserWindow));
			window.titleContent.text = "TexturePacker图集管理";
			window.Show();
		}
		public List<TPAtlasData> AtlasList {get{return m_atlasDataList;}}
		public TPAtlasData SelectedAtlasData{get; private set;}
		public bool MoveNeedCheck{get{return m_bMoveNeedCheck;}}

		private readonly float leftPanelRatio = 0.2f;
		private readonly float middlePanelRatio = 0.2f;
		private float m_leftPanelWidth = 0;
		private float m_middlePanelWidth = 0;

		private int m_selAtlasIdx = -1;		// 当前选中的图集索引
		private float m_previewScale = 1;
		private bool m_bMoveNeedCheck = true;
		private List<TPAtlasData> m_atlasDataList = new List<TPAtlasData>();
		private HashSet<string> m_hsRemoteList = new HashSet<string>();
		private List<TPSpriteItem> m_spriteItemList = new List<TPSpriteItem>();

		private Vector2 m_scrol1 = Vector2.zero;
		private Vector2 m_scrol2 = Vector2.zero;

		TreeViewState m_atlasTreeState;
		TPAtlasTreeView m_atlasTree;
		TreeViewState m_spriteTreeState;
		TPSpriteTreeView m_spriteTree;

		private Texture2D m_RefreshTexture;
		private Texture2D m_toolbarPlus;
		private Texture2D m_toolbarMinus;

		private Material m_lineMat;
		private GUIContent m_contentBtnRepair;
		private GUIContent m_contentBtnCheck;
		private bool m_forceReload = false;		// 重新加载图集
		private bool m_refreshCurrAtlas = false;	// 刷新当前图集

		private void OnEnable()
		{
			SelectedAtlasData = null;
			m_RefreshTexture = EditorGUIUtility.FindTexture("Refresh");
			m_toolbarPlus = EditorGUIUtility.FindTexture("Toolbar Plus");
        	m_toolbarMinus = EditorGUIUtility.FindTexture("Toolbar Minus");
			m_selAtlasIdx = -1;
			m_forceReload = true;
			m_refreshCurrAtlas = true;

			Shader shader = Shader.Find("Hidden/Internal-Colored");
            m_lineMat = new Material(shader);
			m_contentBtnRepair = new GUIContent("修复预制", "重新打图集之后，点击这里，修复所有预制上面的图集挂载丢失");
			m_contentBtnCheck = new GUIContent("检查图集", "检查图集的利用率和alpha通道必要性！");

			InitRemoteAtlas();
		}

		public void Refresh()
		{
			m_refreshCurrAtlas = true;
			Repaint();
		}

		void OnGUI()
		{
			GUILayout.Space(5);
			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button("重新加载图集") || m_forceReload)
			{
				LoadAtlasData();
				m_forceReload = false;
			}

			if(GUILayout.Button(m_contentBtnCheck))
			{
				CheckAtlas();
			}

			EditorGUILayout.Space();
			m_bMoveNeedCheck = EditorGUILayout.Toggle("图集移动是否需要确认？", m_bMoveNeedCheck, GUILayout.Width(200));

			EditorGUILayout.EndHorizontal();
			if(m_atlasTree == null)
			{
				if(m_atlasTreeState == null)
				{
					m_atlasTreeState = new TreeViewState();
				}
				m_atlasTree = new TPAtlasTreeView(m_atlasTreeState, this);

				if(m_spriteTreeState == null)
				{
					m_spriteTreeState = new TreeViewState();
				}
				m_spriteTree = new TPSpriteTreeView(m_spriteTreeState, this);
			}
			

			CalculatePanelSize();
			if(m_atlasTreeState.selectedIDs.Count>0)
			{
				int atlasIdx = m_atlasTreeState.selectedIDs[0];
				if(m_selAtlasIdx != atlasIdx || m_refreshCurrAtlas)
				{	
					SetSelectedAtlas(atlasIdx);
					m_spriteTree.Reload();
					m_refreshCurrAtlas = false;
				}
			}

			// 左侧布局
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.BeginVertical(GUILayout.Width(m_leftPanelWidth));
			EditorGUILayout.Space();
        	GUILayout.Label("*图集列表*");
			EditorGUILayout.EndVertical();
			// 中间布局
			EditorGUILayout.BeginVertical(GUILayout.Width(m_middlePanelWidth+10));
			EditorGUILayout.Space();
        	GUILayout.Label("*sprite列表*");
			EditorGUILayout.EndVertical();

			m_spriteTree.OnGUI(new Rect(m_leftPanelWidth+10, 50, m_middlePanelWidth, position.height - 60));
			m_atlasTree.OnGUI(new Rect(0, 50, m_leftPanelWidth, position.height - 60));

			// 图集预览
			DrawRightPanel();

			EditorGUILayout.EndHorizontal();
		}

		private void SetSelectedAtlas(int atlasIdx)
		{
			SelectedAtlasData = m_atlasDataList[atlasIdx];
			m_selAtlasIdx = atlasIdx;
			SelectedAtlasData.Load();
			m_spriteItemList.Clear();
			var spriteList = SelectedAtlasData.SpriteList;
			for (int i = 0; i < spriteList.Count; i++)
			{
				m_spriteItemList.Add(new TPSpriteItem(i, spriteList[i], m_spriteTree));
			}
		}

		private void CalculatePanelSize()
		{
			m_leftPanelWidth = (position.width-20) * leftPanelRatio;
			m_middlePanelWidth = (position.width-20) * middlePanelRatio;
		}

		// 右侧面板显示图片
		private void DrawRightPanel()
		{
			if(SelectedAtlasData == null)
			{
				return;
			}
			EditorGUILayout.BeginVertical();
			// 选中sprite信息
			DrawSelectedSpriteInfo();

			// 图集预览
			//----------------工具区域-----------------------
			GUILayout.Label("图集预览和操作区域");
			DrawAtlasToolbar();

			DrawWarningTips();

			// --------贴图预览--------
			DrawAtlasPreviewArea();


			EditorGUILayout.EndVertical();
		}

		private static Texture2D s_iconWarning = null;
		private void DrawWarningTips()
		{
			EditorGUILayout.BeginHorizontal();

			if(s_iconWarning == null)
				s_iconWarning = EditorGUIUtility.FindTexture("console.warnicon");

			if(!string.IsNullOrEmpty(SelectedAtlasData.WarningTips))
			{
				GUILayout.Label(new GUIContent(s_iconWarning));
				GUILayout.Label(SelectedAtlasData.WarningTips);
			}

			EditorGUILayout.EndHorizontal();
		}

		// 画选中sprite信息
		private void DrawSelectedSpriteInfo()
		{
			// 显示选中sprites的信息
			EditorGUILayout.Space();
			if (m_spriteTreeState.selectedIDs.Count > 0)
			{
				GUILayout.Label("*sprite明细*");
				m_scrol1 = EditorGUILayout.BeginScrollView(m_scrol1, GUILayout.Height(130));
				EditorGUILayout.BeginHorizontal();
				for(int i=0; i<m_spriteTreeState.selectedIDs.Count; i++)
				{
					TPSpriteData spriteData = SelectedAtlasData.GetSpriteData(m_spriteTreeState.selectedIDs[i]);
					if(spriteData == null)
					{
						continue;
					}
					EditorGUILayout.BeginVertical(GUILayout.Width(60));
					
					EditorGUILayout.TextField(spriteData.SpriteName);
					Texture2D oriTexture = spriteData.LoadOriginTexture();
					if(oriTexture != null)
					{
						string strTextureFmt = "源图格式: RGB";
						if(spriteData.HaveAlphaChannel())
						{
							strTextureFmt += "A";
						}
						oriTexture = EditorGUILayout.ObjectField(strTextureFmt, oriTexture, typeof(Texture2D), false) as Texture2D;
						EditorGUILayout.LabelField(string.Format("原尺寸:{0},{1}", oriTexture.width, oriTexture.height));
					}
					else
					{
						EditorGUILayout.Space();
						EditorGUILayout.LabelField("源图不存在!");
						EditorGUILayout.Space();
					}
					Rect rect = spriteData.rect;
					EditorGUILayout.LabelField(string.Format("图集尺寸:{0},{1}",rect.width,rect.height));
					

					EditorGUILayout.EndVertical();
					EditorGUILayout.Space();
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndScrollView();
			}
		}

		// 画图集操作工具栏
		private void DrawAtlasToolbar()
		{
			Texture2D texture = SelectedAtlasData.GetTexture();
			// 第一行
			EditorGUILayout.BeginHorizontal();
        	EditorGUILayout.TextField(SelectedAtlasData.AtlasName);
			if(GUILayout.Button("打图集"))
			{
				SelectedAtlasData.PackAtlas();
				// SelectedAtlasData.Reload();
				m_refreshCurrAtlas = true;
			}
			GUILayout.Space(20);

			if(GUILayout.Button(m_toolbarMinus, GUILayout.Width(25)))	// 缩小
			{
				AddPreviewScale(-0.2f);
			}
			if(GUILayout.Button(m_RefreshTexture, GUILayout.Width(25)))	// 重置
			{
				m_previewScale = 1;
			}
			if(GUILayout.Button(m_toolbarPlus, GUILayout.Width(25)))	// 放大
			{
				AddPreviewScale(0.2f);
			}
			EditorGUILayout.EndHorizontal();
			// 第二行
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(string.Format("图集大小:{0},{1}", texture.width, texture.height));
			string strTextureFmt = "格式: RGB";
			if(SelectedAtlasData.HaveAlphaChannel)
			{
				strTextureFmt += "A";
			}
			GUILayout.Label(strTextureFmt);
			EditorGUILayout.EndHorizontal();
		}

		// 画图集预览区域
		private void DrawAtlasPreviewArea()
		{
			Texture2D texture = SelectedAtlasData.GetTexture();
			if(texture != null)
			{
				Rect rectTmp = GUILayoutUtility.GetRect(100, 100);
				rectTmp.x += 20;
				rectTmp.y += 20;
				float width = position.width - rectTmp.x - 30;
				float heigh =  position.height - rectTmp.y - 30;
				float size = Mathf.Min(width, heigh);
				float showSize = size * m_previewScale;
				// Debug.LogFormat("width:{0}, heigh:{1}, ,showSize:{2}, rect:{3}", width, heigh,showSize,rectTmp);
				rectTmp.width = width+30;
				rectTmp.height = heigh+30;
				Rect rectContent = new Rect(20,20,showSize+10,showSize+10);
				Rect rectTexture = new Rect(20,20,showSize,showSize);
				m_scrol2 = GUI.BeginScrollView(rectTmp, m_scrol2, rectContent, true, true);
				EditorGUI.DrawTextureTransparent(rectTexture, texture, ScaleMode.ScaleToFit);
				// EditorGUI.DrawTextureTransparent(rectTexture, Color.green);
				// DrawChoosedSpriteBound(rectTexture, texture);
				foreach (var spriteItem in m_spriteItemList)
				{
					spriteItem.OnGUI(rectTexture, texture);
				}

				GUI.EndScrollView();
			}
		}

		// 画选中sprite边界
		private void DrawChoosedSpriteBound(Rect rect, Texture2D texture)
		{
			int tWidth = texture.width;
			int tHeight = texture.height;
			float ratioWidth = rect.width*1.0f/tWidth;
			float ratioHeight = rect.height*1.0f/tHeight;
			// Debug.LogFormat("DrawRect:{0}, texWidth:{1}, texHeight:{2}",rect, tWidth, tHeight);
			for(int i=0; i<m_spriteTreeState.selectedIDs.Count; i++)
			{
				int idx = m_spriteTreeState.selectedIDs[i];
				TPSpriteData spriteData = SelectedAtlasData.GetSpriteData(idx);
				if(spriteData.SpriteStatus == TPSpriteStatus.SpriteNew)
				{
					// 新图不在图集里，跳过
					continue;
				}

				Rect sRect = spriteData.rect;
				sRect.x *= ratioWidth;
				sRect.width *= ratioWidth;
				sRect.y *= ratioHeight;
				sRect.height *= ratioHeight;
				sRect.x += rect.x;
				sRect.y = rect.height - sRect.y + rect.y;
				
				m_lineMat.SetPass(0);
				GL.Begin(GL.LINE_STRIP);
				GL.Color(Color.green);
				GL.Vertex3(sRect.x, sRect.y,0);
				GL.Vertex3(sRect.x+sRect.width, sRect.y,0);
				GL.Vertex3(sRect.x+sRect.width, sRect.y-sRect.height,0);
				GL.Vertex3(sRect.x, sRect.y-sRect.height,0);
				GL.Vertex3(sRect.x, sRect.y,0);
				GL.End();
			}
		}



		private void ClearData()
		{
			m_atlasDataList.Clear();
			m_refreshCurrAtlas = true;
		}

		private void AddPreviewScale(float value)
		{
			m_previewScale += value;
			m_previewScale = Mathf.Clamp(m_previewScale, 0.5f, 3);
		}

		// 加载图集数据
		private void LoadAtlasData()
		{
			ClearData();
			DirectoryInfo folder = new DirectoryInfo(TpPathDef.AtlasTpPath);
			FileInfo[] textures = folder.GetFiles("*.png");
			for (int i = 0; i < textures.Length; i++)
			{
				FileInfo texture = textures[i];
				string textureFullPath = texture.FullName;
				string textureName = texture.Name;
				TPAtlasData atlasData = TPAtlasData.CreateFromPath(textureFullPath,textureName);
				if (atlasData != null)
				{
					m_atlasDataList.Add(atlasData);
					atlasData.Load();
				}
			}
			if(m_atlasTree != null)
				m_atlasTree.Reload();
		}

		// 图集检查
		private void CheckAtlas()
		{
			for(int i = 0; i < m_atlasDataList.Count; i++)
			{
				TPAtlasData atlasData = m_atlasDataList[i];
				if(atlasData == null)
					continue;

				atlasData.CheckAtlas();
			}

			if(m_atlasTree != null)
				m_atlasTree.Reload();
		}

		public void DeleteAtlas(int idx)
		{
			if(idx<0 || idx>=m_atlasDataList.Count)
			{
				return;
			}
			TPAtlasData atlasData = m_atlasDataList[idx];
			string atlasName = atlasData.AtlasName;
			DirectoryInfo dir = new DirectoryInfo(TpPathDef.AtlasTpPath);
			FileInfo[] fils = dir.GetFiles();
			foreach (FileInfo fileInfo in fils)
			{
				string pathName =  Path.GetFileNameWithoutExtension(fileInfo.FullName);
				if (pathName.Equals(atlasName))
				{
					string strTPAssetPath = string.Format("{0}/{1}", TpPathDef.AtlasTpPath, fileInfo.Name);
					AssetDatabase.DeleteAsset(strTPAssetPath);
					Debug.LogFormat("移除图集，tp目录：{0}", fileInfo.Name);
				}
			}
			string strSpritePath = string.Format("{0}/{1}", TpPathDef.SpritePath, atlasName);
			Debug.LogFormat("移除图集， sprite目录：{0}", strSpritePath);
			AssetDatabase.DeleteAsset(strSpritePath);

			AssetTools.RefreshSpriteAtlasMapData(true);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			m_forceReload = true;
		}

		public TPAtlasData GetAtlasDataOfId(int idx)
		{
			if(idx<0 || idx>=m_atlasDataList.Count)
			{
				return null;
			}
			TPAtlasData atlasData = m_atlasDataList[idx];
			return atlasData;
		}

		void InitRemoteAtlas()
        {
			string strRemoteResFixPath = Application.dataPath + "/Config/REMOTE_RES_FIX.txt";
			Debug.Log(strRemoteResFixPath);
			strRemoteResFixPath = strRemoteResFixPath.Replace('\\', Path.DirectorySeparatorChar);
			strRemoteResFixPath = strRemoteResFixPath.Replace('/', Path.DirectorySeparatorChar);
			Debug.Log(strRemoteResFixPath);
			string[] arrRemoteFix = EditorHelper.ReadLinesFromFile(strRemoteResFixPath);
			foreach(string str in arrRemoteFix)
            {
				if(str.StartsWith("ui_atlas_tp_"))
                {
					string strName = str.Replace("ui_atlas_tp_", "").Replace(".unity3d", "").Replace("\r",""); //windows会多一个回车符
					m_hsRemoteList.Add(strName);
                }
            }

		}

		public bool RemoteContainsAtlas(string atlasName)
        {
			atlasName = atlasName.ToLower();
			return m_hsRemoteList.Contains(atlasName);
        }
	}
}

