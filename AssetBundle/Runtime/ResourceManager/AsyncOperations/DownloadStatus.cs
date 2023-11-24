/*---------------------------------------------------------------------------------------
-- 负责人: ming.zhang
-- 创建时间: 2023-03-13 11:50:35
-- 概述:
---------------------------------------------------------------------------------------*/

namespace Onemt.ResourceManagement.AsyncOperation
{
    public struct DownloadStatus
    {
        public long totalBytes;

        public long downloadedBytes;

        public bool isDone;

        public float percent => (totalBytes > 0) ? ((float)downloadedBytes / (float)totalBytes) : (isDone ? 1.0f : 0.0f);
    }
}
