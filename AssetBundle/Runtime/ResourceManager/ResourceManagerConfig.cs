/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-06-08 16:33:36
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Onemt.ResourceManagement
{
    public static class ResourceManagerConfig
    {
        /// <summary>
        /// Used to create an operation result that has multiple items.
        /// </summary>
        /// <param name="type">The type of the result.</param>
        /// <param name="allAssets">The result objects.</param>
        /// <returns>Returns Array object with result items.</returns>
        public static Array CreateArrayResult(Type type, Object[] allAssets)
        {
            var elementType = type.GetElementType();
            if (elementType == null)
                return null;
            int length = 0;
            foreach (var asset in allAssets)
            {
                if (elementType.IsAssignableFrom(asset.GetType()))
                    length++;
            }
            var array = Array.CreateInstance(elementType, length);
            int index = 0;

            foreach (var asset in allAssets)
            {
                if (elementType.IsAssignableFrom(asset.GetType()))
                    array.SetValue(asset, index++);
            }

            return array;
        }

        /// <summary>
        /// Used to create an operation result that has multiple items.
        /// </summary>
        /// <param name="type">The type of the result objects.</param>
        /// <param name="allAssets">The result objects.</param>
        /// <returns>An IList of the resulting objects.</returns>
        public static IList CreateListResult(Type type, Object[] allAssets)
        {
            var genArgs = type.GetGenericArguments();
            var listType = typeof(List<>).MakeGenericType(genArgs);
            var list = Activator.CreateInstance(listType) as IList;
            var elementType = genArgs[0];
            if (list == null)
                return null;
            foreach (var a in allAssets)
            {
                if (elementType.IsAssignableFrom(a.GetType()))
                    list.Add(a);
            }
            return list;
        }
    }
}
