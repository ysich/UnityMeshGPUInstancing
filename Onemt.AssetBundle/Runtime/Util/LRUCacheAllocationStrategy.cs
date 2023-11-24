/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 10:06:57
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using System;

namespace Onemt.ResourceManagement.Util
{
    public class LRUCacheAllocationStrategy : IAllocationStrategy
    {
        // 缓存 m_PoolCache 最大数量
        private int m_PoolMaxSize;

        // List<object> 缓存列表初始化容量
        private int m_PoolInitialCapacity;

        // 缓存对象(List<object>)最大数量
        private int m_PoolCacheMaxSize;

        private List<List<object>> m_PoolCache;// = new List<List<object>>();

        private Dictionary<int, List<object>> m_DicCache = new Dictionary<int, List<object>>();

        public LRUCacheAllocationStrategy(int poolMaxSize, int poolCapacity, int poolCacheMaxSize, int initialPoolCacheCapacity)
        {
            m_PoolMaxSize = poolMaxSize;
            m_PoolInitialCapacity = poolCapacity;
            m_PoolCacheMaxSize = poolCacheMaxSize;

            m_PoolCache = new List<List<object>>(initialPoolCacheCapacity);
            for (int i = 0; i < initialPoolCacheCapacity; ++i)
                m_PoolCache.Add(new List<object>(m_PoolInitialCapacity));
        }

        private List<object> GetPool()
        {
            int count = m_PoolCache.Count;
            if (count == 0)
                return new List<object>(m_PoolInitialCapacity);

            var index = count - 1;
            var pool = m_PoolCache[index];
            m_PoolCache.RemoveAt(index);
            return pool;
        }

        private void ReleasePool(List<object> pool)
        {
            if (m_PoolCache.Count < m_PoolMaxSize)
                m_PoolCache.Add(pool);
        }

        public object New(Type type)
        {
            int typeHash = type.GetHashCode();
            if (m_DicCache.TryGetValue(typeHash, out var pool))
            {
                var count = pool.Count;
                var obj = pool[count - 1];
                pool.RemoveAt(count - 1);
                if (count == 1)
                {
                    m_DicCache.Remove(typeHash);
                    ReleasePool(pool);
                }

                return obj;
            }

            return Activator.CreateInstance(type);
        }

        public void Release(int typeHash, object obj)
        {
            if (!m_DicCache.TryGetValue(typeHash, out var pool))
                m_DicCache.Add(typeHash, pool = GetPool());

            if (pool.Count < m_PoolMaxSize)
                pool.Add(obj);
        }
    }
}
 