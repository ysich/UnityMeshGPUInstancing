/*---------------------------------------------------------------------------------------
-- 负责人: onemt
-- 创建时间: 2023-11-18 10:57:01
-- 概述:
---------------------------------------------------------------------------------------*/

using UnityEditor;
using UnityEngine;

namespace InstanceSprite
{
    [CustomEditor(typeof(InstanceSpriteRenderer))]
    public class InstanceSpriteRendererEditor: Editor
    {
        private Sprite m_Sprite;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUI.color = Color.green;
            if (GUILayout.Button("点击预览",GUILayout.Height(30)))
            {
                
            }
        }
    }
}