/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 15:33:45
-- 概述:
---------------------------------------------------------------------------------------*/

using UnityEngine;
using System;

namespace Onemt.ResourceManagement.Util
{
    internal class MonoBehaviourCallbackHooks : ComponentSingleton<MonoBehaviourCallbackHooks>
    {
        internal Action<float> m_OnUpdateDelegate;
        public event Action<float> OnUpdateDelegate
        {
            add
            {
                m_OnUpdateDelegate += value;
            }

            remove
            {
                m_OnUpdateDelegate -= value;
            }
        }

        protected override string GetGameObjectName() => "ResourceManagerCallbacks";

        // Update is called once per frame
        internal void Update()
        {
            m_OnUpdateDelegate?.Invoke(Time.unscaledDeltaTime);
        }
    }
}
