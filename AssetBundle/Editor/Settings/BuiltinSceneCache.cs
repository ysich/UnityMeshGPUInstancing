/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-14 14:51:42
-- 概述:
        主要用于 内置构建场景的一些获取等.
---------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using UnityEditor;

namespace OnemtEditor.AssetBundle.Settings
{
    internal static class BuiltinSceneCache
    {
        internal static EditorBuildSettingsScene[] r_Scenes;
        static Dictionary<GUID, int> s_GUIDSceneIndexLookup;
        static Dictionary<string, int> s_PathSceneIndexLookup;
        static bool s_IsListening;
        public static event Action sceneListChanged;

        internal static void ClearState(bool clearCallbacks = false)
        {
            InvalidateCache();
            if (s_IsListening)
            {
                EditorBuildSettings.sceneListChanged -= EditorBuildSettings_sceneListChanged;
                s_IsListening = false;
            }
            if (clearCallbacks)
                sceneListChanged = null;
        }

        /// <summary>
        ///   <para> 内置构建在包中的 场景. </para>
        /// </summary>
        /// <value></value>
        public static EditorBuildSettingsScene[] scenes
        {
            get
            {
                if (r_Scenes == null)
                {
                    if (!s_IsListening)
                    {
                        s_IsListening = true;
                        EditorBuildSettings.sceneListChanged += EditorBuildSettings_sceneListChanged;
                    }
                    InvalidateCache();
                    r_Scenes = EditorBuildSettings.scenes;
                }
                return r_Scenes;
            }
            set
            {
                EditorBuildSettings.scenes = value;
            }
        }

        public static Dictionary<GUID, int> GUIDSceneIndexLookup
        {
            get
            {
                if (s_GUIDSceneIndexLookup == null)
                {
                    EditorBuildSettingsScene[] localScenes = scenes;
                    s_GUIDSceneIndexLookup = new Dictionary<GUID, int>();
                    int enabledIndex = 0;
                    for (int i = 0; i < scenes.Length; i++)
                    {
                        if (localScenes[i] != null && localScenes[i].enabled)
                            s_GUIDSceneIndexLookup[localScenes[i].guid] = enabledIndex++;
                    }
                }
                return s_GUIDSceneIndexLookup;
            }
        }

        public static Dictionary<string, int> PathSceneIndexLookup
        {
            get
            {
                if (s_PathSceneIndexLookup == null)
                {
                    EditorBuildSettingsScene[] localScenes = scenes;
                    s_PathSceneIndexLookup = new Dictionary<string, int>();
                    int enabledIndex = 0;
                    for (int i = 0; i < scenes.Length; i++)
                    {
                        if (localScenes[i] != null && localScenes[i].enabled)
                            s_PathSceneIndexLookup[localScenes[i].path] = enabledIndex++;
                    }
                }
                return s_PathSceneIndexLookup;
            }
        }

        private static void InvalidateCache()
        {
            r_Scenes = null;
            s_GUIDSceneIndexLookup = null;
            s_PathSceneIndexLookup = null;
        }

        public static int GetSceneIndex(GUID guid)
        {
            int index = -1;
            return GUIDSceneIndexLookup.TryGetValue(guid, out index) ? index : -1;
        }

        public static bool Contains(GUID guid)
        {
            return GUIDSceneIndexLookup.ContainsKey(guid);
        }

        public static bool Contains(string path)
        {
            return PathSceneIndexLookup.ContainsKey(path);
        }

        public static bool GetSceneFromGUID(GUID guid, out EditorBuildSettingsScene outScene)
        {
            int index = GetSceneIndex(guid);
            if (index == -1)
            {
                outScene = null;
                return false;
            }
            outScene = scenes[index];
            return true;
        }

        private static void EditorBuildSettings_sceneListChanged()
        {
            InvalidateCache();
            if (sceneListChanged != null)
                sceneListChanged();
        }
    }
}
