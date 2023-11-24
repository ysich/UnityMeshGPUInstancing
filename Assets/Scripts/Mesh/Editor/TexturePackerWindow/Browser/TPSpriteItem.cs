using UnityEngine;

namespace OnemtEditor.TexturePacker.Browser
{
	public class TPSpriteItem
	{
		private TPSpriteData m_spriteData;
		private int m_index;
		private TPSpriteTreeView m_spriteTreeView;
		
		private static Shader shader = Shader.Find("Hidden/Internal-Colored");
		private static Material m_lineMat = new Material(shader);

		public TPSpriteItem(int index, TPSpriteData spriteData, TPSpriteTreeView spriteTreeView)
		{
			m_spriteData = spriteData;
			m_index = index;
			m_spriteTreeView = spriteTreeView;
		}

		public void OnGUI(Rect rect, Texture2D texture)
		{
			if (m_spriteData != null && m_spriteData.SpriteStatus!=TPSpriteStatus.SpriteNew)
			{
				GUI.color = Color.clear;
				int tWidth = texture.width;
				int tHeight = texture.height;
				float ratioWidth = rect.width*1.0f/tWidth;
				float ratioHeight = rect.height*1.0f/tHeight;
				Rect sRect = m_spriteData.rect;
				sRect.x *= ratioWidth;
				sRect.width *= ratioWidth;
				sRect.y *= ratioHeight;
				sRect.height *= ratioHeight;
				sRect.x += rect.x;
				sRect.y = rect.height - sRect.y + rect.y - sRect.height;
				if (GUI.Button(sRect, "Button"))
				{
					Event e = Event.current;
					#if UNITY_EDITOR_OSX
					bool isLeftCtrlDown = e.command;
					#else
					bool isLeftCtrlDown = e.control;
					#endif
					var selectIds = m_spriteTreeView.state.selectedIDs;
					if (isLeftCtrlDown)
					{
						if (m_spriteTreeView.IsSelected(m_index))
							selectIds.Remove(m_index);
						else
							selectIds.Add(m_index);
					}
					else
					{
						selectIds.Clear();
						selectIds.Add(m_index);
					}
					m_spriteTreeView.SetSelection(selectIds);
					m_spriteTreeView.FrameItem(m_index);
				}
				DrawSelectedBound(sRect);
				GUI.color = GUI.backgroundColor;
			}
		}

		private void DrawSelectedBound(Rect sRect)
		{
			if (m_spriteTreeView.IsSelected(m_index))
			{
				m_lineMat.SetPass(0);
				GL.Begin(GL.LINE_STRIP);
				GL.Color(Color.green);
				GL.Vertex3(sRect.x, sRect.y,0);
				GL.Vertex3(sRect.x + sRect.width, sRect.y,0);
				GL.Vertex3(sRect.x + sRect.width, sRect.y + sRect.height,0);
				GL.Vertex3(sRect.x, sRect.y + sRect.height,0);
				GL.Vertex3(sRect.x, sRect.y,0);
				GL.End();
			}
		}
	}
}