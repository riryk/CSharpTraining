using System;
using System.Security;
using System.Threading;

using IThreadPoolWorkItem = CSharp_training.ThreadPool.ThreadPoolQueue.IThreadPoolWorkItem;

namespace CSharp_training.ThreadPool
{
    internal sealed class QueueUserWorkItemCallback : IThreadPoolWorkItem
    {
        [System.Security.SecuritySafeCritical]
        static QueueUserWorkItemCallback() { }

        private WaitCallback callback;
        private ExecutionContext context;
        private Object state;

        [SecurityCritical]
        internal QueueUserWorkItemCallback(WaitCallback waitCallback, Object stateObj, bool compressStack, ref StackCrawlMark stackMark)
        {
            callback = waitCallback;
            state = stateObj;
            if (compressStack && !ExecutionContext.IsFlowSuppressed())
            {
                // clone the exection context
                // context = ExecutionContext.Capture(
                //    ref stackMark,
                //    ExecutionContext.CaptureOptions.IgnoreSyncCtx | ExecutionContext.CaptureOptions.OptimizeDefaultCase);
            }
        }

        internal QueueUserWorkItemCallback(WaitCallback waitCallback, Object stateObj, ExecutionContext ec)
        {
            callback = waitCallback;
            state = stateObj;
            context = ec;
        }

        public void Execute()
        {
            throw new NotImplementedException();
        }

        public void ExecuteWorkItem()
        {
            throw new NotImplementedException();
        }

        public void MarkAborted(ThreadAbortException tae)
        {
            throw new NotImplementedException();
        }
    }
}
