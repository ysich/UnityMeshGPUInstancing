using System.Collections;
using System.Collections.Generic;
using OnemtEditor.Helper;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace OnemtEditor.TexturePacker.Browser
{
	class AtlasTreeViewItem : TreeViewItem
    {
		public TPAtlasData AtlasData{get; private set;}
        internal AtlasTreeViewItem(int id, int depth, string displayName, TPAtlasData atlasData) : base(id, depth, displayName)
        {
			this.AtlasData = atlasData;
        }
    }
	public class TPAtlasTreeView : TreeView 
	{
		TPBrowserWindow m_Controller;
		private static Texture2D s_iconWarning = null;
		public TPAtlasTreeView(TreeViewState state, TPBrowserWindow ctrl) : base(state)
        {
            m_Controller = ctrl;
            showBorder = true;
            Reload();
            SetSelection(new int[1]{0});
        }
		protected override TreeViewItem BuildRoot()
		{
			List<TPAtlasData> atlasList = m_Controller.AtlasList;
			AtlasTreeViewItem root = new AtlasTreeViewItem(-1, -1, "root", null);
			for(int i=0; i<atlasList.Count; i++)
			{
				TPAtlasData atlasData = atlasList[i];
				root.AddChild(new AtlasTreeViewItem(i, 0, atlasData.AtlasName, atlasData));
			}

			SetupDepthsFromParentsAndChildren(root);
			return root;
		}

		protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
		{
			return base.BuildRows(root);
		}

		public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
            if(Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
            }
        }

		protected override void RowGUI(RowGUIArgs args)
		{
			base.RowGUI(args);
			AtlasTreeViewItem atlasItem = args.item as AtlasTreeViewItem;
			if(atlasItem == null)
			{
				return;
			}

			TPAtlasData atlasData = atlasItem.AtlasData;
			var size = args.rowRect.height;
			var right = args.rowRect.xMax;
			float useSize = 0;

			if(m_Controller.RemoteContainsAtlas(atlasData.AtlasName))
            {
				Rect nameRect = new Rect(right - (size + useSize), args.rowRect.yMin, size, size);
				useSize += size;
				GUI.Label(nameRect, "R");
            }				

			
			if (atlasData != null && atlasData.HaveAbnormalStatus)
			{
				if (s_iconWarning == null)
				{
					s_iconWarning = EditorGUIUtility.FindTexture("console.warnicon");
				}
				
				Rect messageRect = new Rect(right - (size+useSize), args.rowRect.yMin, size, size);
				GUI.Label(messageRect, new GUIContent(s_iconWarning));
				useSize += size;
			}

			

		}

		// 不允许多选
		protected override bool CanMultiSelect(TreeViewItem item)
		{
			return false;
		}

		// 不允许重命名
		protected override bool CanRename(TreeViewItem item)
        {
            return false;
        }
		protected override void RenameEnded(RenameEndedArgs args)
		{
			// 重命名的处理
		}
		protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
		{
			if(args.dragAndDropPosition != DragAndDropPosition.UponItem)
			{
				return DragAndDropVisualMode.Rejected;
			}
			AtlasTreeViewItem atlasItem = args.parentItem as AtlasTreeViewItem;
			if(atlasItem == null || atlasItem.AtlasData == null)
			{
				return DragAndDropVisualMode.Rejected;
			}
			TPAtlasData fromAtlasData = m_Controller.SelectedAtlasData;
			TPAtlasData toAtlasData = atlasItem.AtlasData;
			if(fromAtlasData == toAtlasData)
			{
				return DragAndDropVisualMode.None;		// 拖动到自己就不算了
			}

			IList<int> seletIDs = DragAndDrop.GetGenericData("SelectedSpriteIDs") as IList<int>;
			if(seletIDs == null)
			{
				return DragAndDropVisualMode.Rejected;
			}
			// Debug.LogFormat("接收到Drag,count：{0}, idx:{1}", seletIDs.Count, seletIDs[0]);
			if(args.performDrop)
			{
				// 处理drop动作
				bool choose = true;
				if(m_Controller.MoveNeedCheck)
				{
					choose = EditorUtility.DisplayDialog("提示", $"是否确定要移动到{toAtlasData.AtlasName}图集目录？", "确定", "取消");
				}
				if(choose)
				{
					for(int i=0; i<seletIDs.Count; i++)
					{
						TPSpriteData spriteData = fromAtlasData.GetSpriteData(seletIDs[i]);
						if(spriteData == null || spriteData.SpriteStatus == TPSpriteStatus.SprteMiss)
						{
							continue;
						}
						string strFromPath = spriteData.OriginPath;
						string strToPath = string.Format("{0}/{1}/{2}.png", TpPathDef.SpritePath, toAtlasData.AtlasName, spriteData.SpriteName);
						AssetDatabase.MoveAsset(strFromPath, strToPath);
					}
					fromAtlasData.Reload();
					toAtlasData.Reload();
					m_Controller.Refresh();
				}

			}
			return DragAndDropVisualMode.Move;
		}

		protected override void ContextClickedItem(int id)
		{
			// Debug.Log("----点击Item:"+id);
			
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("在project视图中定位到它"),  false, PingFolderInProject, id);
			menu.AddItem(new GUIContent("打开所在的文件夹"),  false, OpenResFolder, id);
			menu.AddItem(new GUIContent("删除选中的图集"),  false, DeleteAtlas, id);
			
			menu.ShowAsContext();
		}

		void PingFolderInProject(object context)
		{
			int id = (int)context;
			TPAtlasData atlasData = m_Controller.GetAtlasDataOfId(id);
			if(atlasData != null)
			{
				string strFolder = atlasData.GetAtlasPath();
				EditorHelper.PingPathInProject(strFolder);
			}
		}

		void OpenResFolder(object context)
		{
			int id = (int)context;
			TPAtlasData atlasData = m_Controller.GetAtlasDataOfId(id);
			if(atlasData != null)
			{
				string strFolder = atlasData.GetAtlasPath();
				EditorUtility.RevealInFinder(strFolder);
				#if UNITY_EDITOR_OSX
				ShellHelper.ProcessCommand("open", strFolder);
				#else
				var path = Application.dataPath +"/../" + strFolder;
				path = path.Replace("/", "\\");
				ShellHelper.ProcessCommand("explorer.exe", path);
				#endif
				
			}
		}

		void DeleteAtlas(object context)
		{
			int id = (int)context;
			m_Controller.DeleteAtlas(id);
		}
	}
}

