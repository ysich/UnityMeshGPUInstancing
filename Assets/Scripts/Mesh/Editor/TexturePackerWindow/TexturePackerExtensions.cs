/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-05-30 16:16:51
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OnemtEditor.TexturePacker
{
    public static class TexturePackerExtensions
    {
        public static Rect HashtableToRect(this Hashtable table)
        {
            return new Rect((float)table[(object)"x"], (float)table[(object)"y"], (float)table[(object)"w"], (float)table[(object)"h"]);
        }

        public static Vector2 HashtableToVector2(this Hashtable table)
        {
            return table.ContainsKey((object)"x") && table.ContainsKey((object)"y") ? new Vector2((float)table[(object)"x"], (float)table[(object)"y"]) : new Vector2((float)table[(object)"w"], (float)table[(object)"h"]);
        }

        public static bool IsTexturePackerTable(this Hashtable table)
        {
            return table != null && (table.ContainsKey((object)"meta") && ((Hashtable)table[(object)"meta"]).ContainsKey((object)"app"));
        }

        public static Vector2 Vector3toVector2(this Vector3 vec)
        {
            return new Vector2((float)vec.x, (float)vec.y);
        }
    }
}
