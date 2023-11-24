/*---------------------------------------------------------------------------------------
-- 负责人: onemt
-- 创建时间: 2023-11-07 15:43:04
-- 概述:
---------------------------------------------------------------------------------------*/

using Onemt.Core.Util;
using UnityEditor;
using UnityEngine;

public class EditorHelper
{
    public static void ExecuteSelection(string name, System.Action<Object> onAction, SelectionMode mode = SelectionMode.DeepAssets)
    {
        Object[] items = Selection.GetFiltered(typeof(Object), mode);
        int total = items.Length;
        for (int i = 0; i < total; ++i)
        {
            if (onAction != null)
            {
                onAction(items[i]);
            }
            UpdateProgress("Generate...", name, i+1, total);
        }
        ClearProgressBar();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(string.Format("{0} Completed", name));
    }
    public static void UpdateProgress (string title,  string info, int current, int total)
    {
        EditorUtility.DisplayProgressBar (title, string.Format("{0} {1}/{2}", info, current, total), Mathf.InverseLerp (0, total, current));
    }

    public static void ClearProgressBar()
    {
        EditorUtility.ClearProgressBar ();
    }
    
    public static void DisplayProgressBar(string title, string info, float progress)
    {
        EditorUtility.DisplayProgressBar(title, info, progress);
    }
    
    public static void PingPathInProject(string selectPath)
    {
        //加载想要选中的文件/文件夹
        Object obj = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(selectPath);
        //在Project面板标记高亮显示
        UnityEditor.EditorGUIUtility.PingObject(obj);
        //在Project面板自动选中，并在Inspector面板显示详情
        UnityEditor.Selection.activeObject = obj;
    }

    /// <summary>
    /// 根据路径读文本数据
    /// </summary>
    /// <param name="strPath"></param>
    /// <returns></returns>
    public static string[] ReadLinesFromFile(string strPath)
    {
        string strContent = FileHelper.ReadTextFromFile(strPath);
        return strContent.Split(new char[]{'\n'});
    }
    
    /// <summary>
    ///   <para> 9分位置. </para>
    /// </summary>
    /// <param name="pivot"></param>
    /// <returns></returns>
    public static int PivotToAlignment(Vector2 pivot)
    {
        SpriteAlignment align = SpriteAlignment.Custom;
        if (pivot.x == 0)
        {
            if (pivot.y == 0)
                align = SpriteAlignment.BottomLeft;
            else if (pivot.y == 0.5f)
                align = SpriteAlignment.LeftCenter;
            else if (pivot.y == 1)
                align = SpriteAlignment.TopLeft;
        }
        else if (pivot.x == 0.5f)
        {
            if (pivot.y == 0)
                align = SpriteAlignment.BottomCenter;
            else if (pivot.y == 0.5f)
                align = SpriteAlignment.Center;
            else if (pivot.y == 1)
                align = SpriteAlignment.TopCenter;
        }
        else if (pivot.x == 1.0f)
        {
            if (pivot.y == 0)
                align = SpriteAlignment.BottomRight;
            else if (pivot.y == 0.5f)
                align = SpriteAlignment.RightCenter;
            else if (pivot.y == 1)
                align = SpriteAlignment.TopRight;
        }    

        return (int)align;
    }
}