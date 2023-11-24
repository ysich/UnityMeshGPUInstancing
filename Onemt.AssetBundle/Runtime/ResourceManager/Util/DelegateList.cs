/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 14:15:15
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using System;

namespace Onemt.ResourceManagement.Util
{
    public class DelegateList<T>
    {
        Func<Action<T>, LinkedListNode<Action<T>>> m_AcquireFunc;

        Action<LinkedListNode<Action<T>>> m_ReleaseFunc;

        LinkedList<Action<T>> m_Callbacks;

        bool m_Invoking = false;

        public int count { get { return m_Callbacks == null ? 0 : m_Callbacks.Count; } }

        public DelegateList(Func<Action<T>, LinkedListNode<Action<T>>> acquireFunc, Action<LinkedListNode<Action<T>>> releaseFunc)
        {
            if (acquireFunc == null)
                throw new ArgumentNullException("acquireFunc");
            if (releaseFunc == null)
                throw new ArgumentNullException("releaseFunc");

            m_AcquireFunc = acquireFunc;
            m_ReleaseFunc = releaseFunc;
        }

        public void Add(Action<T> action)
        {
            var node = m_AcquireFunc(action);
            if (m_Callbacks == null)
                m_Callbacks = new LinkedList<Action<T>>();

            m_Callbacks.AddLast(node);
        }

        public void Remove(Action<T> action)
        {
            if (m_Callbacks == null)
                return;

            var node = m_Callbacks.First;
            while (node != null)
            {
                if (node.Value == action)
                {
                    if (m_Invoking)
                    {
                        node.Value = null;
                    }
                    else
                    {
                        m_Callbacks.Remove(node);
                        m_ReleaseFunc(node);
                    }
                }

                node = node.Next;
            }
        }

        public void Invoke(T res)
        {
            if (m_Callbacks == null)
                return;

            m_Invoking = true;
            var node = m_Callbacks.First;
            while (node != null)
            {
                if (node.Value != null)
                {
                    try
                    {
                        node.Value(res);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                }

                node = node.Next;
            }
            m_Invoking = false;
            node = m_Callbacks.First;
            while (node != null)
            {
                var next = node.Next;
                if (node.Value == null)
                {
                    m_Callbacks.Remove(node);
                    m_ReleaseFunc(node);
                }
                node = next;
            }
        }

        public void Clear()
        {
            if (m_Callbacks == null) return;

            var node = m_Callbacks.First;
            while (node != null)
            {
                var next = node.Next;
                m_Callbacks.Remove(node);
                m_ReleaseFunc(node);
                node = next;
            }
        }

        public static DelegateList<T> CreateWithGlobalCache()
        {
            return new DelegateList<T>(GlobalLinkedListNodeCache<Action<T>>.Acquire, GlobalLinkedListNodeCache<Action<T>>.Release);
        }
    }

    internal static class GlobalLinkedListNodeCache<T>
    {
        static LinkedListNodeCache<T> s_GlobalCache;

        public static LinkedListNode<T> Acquire(T val)
        {
            if (s_GlobalCache == null)
                s_GlobalCache = new LinkedListNodeCache<T>();
            return s_GlobalCache.Acquire(val);
        }

        public static void Release(LinkedListNode<T> node)
        {
            if (s_GlobalCache == null)
                s_GlobalCache = new LinkedListNodeCache<T>();
            s_GlobalCache.Release(node);
        }
    }

    public class LinkedListNodeCache<T>
    {
        int m_NodesCreated = 0;
        LinkedList<T> m_NodeCache;

        /// <summary>
        /// Creates or returns a LinkedListNode of the requested type and set the value.
        /// </summary>
        /// <param name="val">The value to set to returned node to.</param>
        /// <returns>A LinkedListNode with the value set to val.</returns>
        public LinkedListNode<T> Acquire(T val)
        {
            if (m_NodeCache != null)
            {
                var n = m_NodeCache.First;
                if (n != null)
                {
                    m_NodeCache.RemoveFirst();
                    n.Value = val;
                    return n;
                }
            }
            m_NodesCreated++;
            return new LinkedListNode<T>(val);
        }

        /// <summary>
        /// Release the linked list node for later use.
        /// </summary>
        /// <param name="node"></param>
        public void Release(LinkedListNode<T> node)
        {
            if (m_NodeCache == null)
                m_NodeCache = new LinkedList<T>();

            node.Value = default(T);
            m_NodeCache.AddLast(node);
        }

        internal int CreatedNodeCount { get { return m_NodesCreated; } }
        internal int CachedNodeCount { get { return m_NodeCache == null ? 0 : m_NodeCache.Count; } }
    }
}
