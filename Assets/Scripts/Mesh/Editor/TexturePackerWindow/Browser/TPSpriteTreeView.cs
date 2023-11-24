using System.Collections;
using System.Collections.Generic;
using Onemt.Core.Util;
using OnemtEditor.Helper;
using OnemtEditor.TexturePacker;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace OnemtEditor.TexturePacker.Browser
{
	internal class SpriteTreeViewItem : TreeViewItem
    {
		public TPSpriteData SpriteData{get; private set;}
		public Texture2D texture;
        internal SpriteTreeViewItem(int id, int depth, string displayName, TPSpriteData spriteData): 
			base(id, depth, displayName)
        {
			this.SpriteData = spriteData;
			if(spriteData != null)
			{
				this.texture = spriteData.LoadOriginTexture();
			}
        }
    }
	public class TPSpriteTreeView : TreeView
	{
		TPBrowserWindow m_Controller;
		private static int s_RowHeight = 30;
		public TPSpriteTreeView(TreeViewState state, TPBrowserWindow ctrl) : base(state)
        {
            m_Controller = ctrl;
            showBorder = true;
        }
		protected override TreeViewItem BuildRoot()
		{
			SetSelection(new int[0]);
			SpriteTreeViewItem root = new SpriteTreeViewItem(-1, -1, "root", null);
			TPAtlasData atlasData = m_Controller.SelectedAtlasData;
			List<TPSpriteData> spriteList = atlasData.SpriteList;
			
			for(int i=0; i<spriteList.Count; i++)
			{
				TPSpriteData spriteData = spriteList[i];
				root.AddChild(new SpriteTreeViewItem(i, 0, spriteData.SpriteName, spriteData));
			}
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

		protected override float GetCustomRowHeight(int row, TreeViewItem item)
		{
			return s_RowHeight;
		}

		protected override void RowGUI(RowGUIArgs args)
		{
			SpriteTreeViewItem spriteItem = args.item as SpriteTreeViewItem;
			TPSpriteData data = spriteItem.SpriteData;

			// 图标区域
			if(data!=null)
			{
				Rect iconRect = args.rowRect;
				iconRect.width = s_RowHeight - 2;
				iconRect.height = s_RowHeight - 2;
				Texture2D texturePreview = data.LoadOriginTexture();
				if(texturePreview != null)
				{
					GUI.DrawTexture(iconRect, texturePreview);
				}
			}
			
			// 名字区域
			string strDisplayName = spriteItem.displayName;
			Rect nameRect = args.rowRect;
			nameRect.x = s_RowHeight + 3;
			nameRect.width = nameRect.width - nameRect.x - 25;
			GUI.Label(nameRect, strDisplayName);
			
			// 状态区域
			if(data != null)
			{
				Texture2D icon = data.GetStatusIcon();
				if(icon != null)
				{
					var size = 20;
					var right = args.rowRect.xMax;
					Rect messageRect = new Rect(right - size, args.rowRect.yMin, size, size);
					GUI.Label(messageRect, new GUIContent(icon, data.SpriteStatus.ToString()));
				}
			}
		}

		// 允许多选
		protected override bool CanMultiSelect(TreeViewItem item)
		{
			return true;
		}

		protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds == null)
                return;
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
		
		protected override void ContextClickedItem(int id)
		{
			// Debug.Log("----点击Item:"+id);
			TPSpriteData selSprtData = m_Controller.SelectedAtlasData.GetSpriteData(id);
			if(selSprtData == null || selSprtData.SpriteStatus==TPSpriteStatus.SprteMiss)
			{
				EditorUtility.DisplayDialog("提示", "选中的sprite不存在，无法操作!", "确定");
				return;
			}
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("在project视图中定位到它"),  false, PingFolderInProject, selSprtData);
			menu.AddItem(new GUIContent("打开所在的文件夹"),  false, OpenResFolder, selSprtData);
			List<TPSpriteData> lstSprtData = new List<TPSpriteData>();
			foreach (var nodeID in GetSelection())
			{
				TPSpriteData sprtData = m_Controller.SelectedAtlasData.GetSpriteData(nodeID);
				if(selSprtData.SpriteStatus != TPSpriteStatus.SprteMiss)
				{
					lstSprtData.Add(sprtData);
				}
			}
			if(lstSprtData.Count > 0)
			{
				menu.AddItem(new GUIContent(string.Format("删除选中的{0}个碎图", lstSprtData.Count)),  false, DeleteSprites, lstSprtData);
			}
			menu.ShowAsContext();
		}

		

		// 可以拖拽
		protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return true;
        }
		protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
		{
			DragAndDrop.PrepareStartDrag();
			// DragAndDrop.paths = ;
            DragAndDrop.SetGenericData("SelectedSpriteIDs", args.draggedItemIDs);
            DragAndDrop.StartDrag("Sprites");
		}
		protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
		{
			return DragAndDropVisualMode.Rejected;
		}

		void PingFolderInProject(object context)
		{
			TPSpriteData selSprtData = context as TPSpriteData;
			if(selSprtData != null)
			{
				string strFolder = selSprtData.OriginPath;
				Debug.LogFormat("PingFolderInProject, folder:{0}",strFolder);
				EditorHelper.PingPathInProject(strFolder);
			}
		}

		void OpenResFolder(object context)
		{
			TPSpriteData selSprtData = context as TPSpriteData;
			if(selSprtData != null)
			{
				string strFolder = selSprtData.GetFolderPathOfSprite();
				#if UNITY_EDITOR_OSX
				ShellHelper.ProcessCommand("open", strFolder);
				#else
				var path = Application.dataPath +"/../" + strFolder;
				path = path.Replace("/", "\\");
				ShellHelper.ProcessCommand("explorer.exe", path);
				#endif

			}
		}

		void DeleteSprites(object context)
		{
			List<TPSpriteData> lstSprtData = context as List<TPSpriteData>;
			if(lstSprtData != null)
			{
				for(int i=0; i<lstSprtData.Count; i++)
				{
					TPSpriteData spriteData = lstSprtData[i];
					AssetDatabase.DeleteAsset(spriteData.OriginPath);
					Debug.LogFormat("移除资源：{0}", spriteData.OriginPath);
				}
			}
			m_Controller.SelectedAtlasData.Reload();
			m_Controller.Refresh();
		}
	}
}

