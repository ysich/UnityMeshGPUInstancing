/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 09:43:22
-- 概述:
---------------------------------------------------------------------------------------*/

using System;

namespace Onemt.ResourceManagement.Util
{
    public interface IAllocationStrategy
    {
        object New(Type type);

        void Release(int typeHash, object obj);
    }
}
