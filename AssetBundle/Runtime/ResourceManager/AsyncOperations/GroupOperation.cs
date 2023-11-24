/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 11:50:49
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;

namespace Onemt.ResourceManagement.AsyncOperation
{
    public class GroupOperation : AsyncOperationBase<IList<AsyncOperationHandle>>
    {
        private Action<AsyncOperationHandle> m_InternalOnComplete;
        private int m_LoadedCount;

        protected override float progress
        {
            get
            {
                float total = 0.0f;
                for (int i = 0; i < result.Count; ++i)
                {
                    var handle = result[i];
                    if (handle.isDone)
                        ++total;
                    else
                        total += handle.percentComplete;
                }

                return total / result.Count;
            }
        }

        [UnityEngine.Scripting.Preserve]
        public GroupOperation()
        {
            result = new List<AsyncOperationHandle>();
            m_InternalOnComplete = OnOperationCompleted;
        }

        public void Init(List<AsyncOperationHandle> operations, bool releaseDependenciesOnFailure = true)
        {
            result = new List<AsyncOperationHandle>(operations);
        }

        protected override void Execute()
        {
            m_LoadedCount = 0;
            for (int i = 0; i < result.Count; ++i)
            {
                if (result[i].isDone)
                    ++m_LoadedCount;
                else
                    result[i].completedCallback += m_InternalOnComplete;
            }

            CompleteIfDependenciesComplete();
        }

        internal IList<AsyncOperationHandle> GetDependentOps()
        {
            return result;
        }

        protected override void GetDependencies(List<AsyncOperationHandle> deps)
        {
            deps.AddRange(result);
        }

        void OnOperationCompleted(AsyncOperationHandle opHandle)
        {
            ++m_LoadedCount;
            CompleteIfDependenciesComplete();
        }

        private void CompleteIfDependenciesComplete()
        {
            if (m_LoadedCount != result.Count)
                return;

            bool success = true;
            string errorMsg = "";
            for (int i = 0; i < result.Count; ++i)
            {
                if (result[i].status != AsyncOperationStatus.Succeeded)
                {
                    success = false;
                    errorMsg = "GroupOperation failed because one of its dependencies failed.";
                    break;
                }
            }

            Complete(result, success, errorMsg);
        }

        internal override DownloadStatus GetDownloadStatus(HashSet<object> visited)
        {
            var status = new DownloadStatus() { isDone = isDone };
            for (int i = 0; i < result.Count; ++i)
            {
                if (result[i].IsValid())
                {
                    var depDownloadStatus = result[i].InternalGetDownloadStatus(visited);
                    status.downloadedBytes += depDownloadStatus.downloadedBytes;
                    status.totalBytes += depDownloadStatus.totalBytes;
                }
            }

            return status;
        }

        internal override void ReleaseDependencies()
        {
            for (int i = 0; i < result.Count; ++i)
                if (result[i].IsValid())
                    result[i].Release();

            result.Clear();
        }

        protected override void Destroy()
        {
            ReleaseDependencies();
        }
    }
}
