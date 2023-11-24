/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 15:48:07
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor;

namespace OnemtEditor.AssetBundle.Build.Utility
{
    public static class BuildUtility
    {
        public static bool CheckModifiedScenesAndAskToSave()
        {
            var dirtyScenes = new List<Scene>();

            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty)
                {
                    dirtyScenes.Add(scene);
                }
            }

            if (dirtyScenes.Count > 0)
            {
                if (EditorUtility.DisplayDialog("Unsaved Scenes", "Modified Scenes must be saved to continue.",
                    "Save and Continue", "Cancel"))
                {
                    EditorSceneManager.SaveScenes(dirtyScenes.ToArray());
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
