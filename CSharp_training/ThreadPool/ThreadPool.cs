using CSharp_training.ThreadPool.ThreadPoolQueue;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading;
using IThreadPoolWorkItem = CSharp_training.ThreadPool.ThreadPoolQueue.IThreadPoolWorkItem;

namespace CSharp_training.ThreadPool
{
    public class ThreadPool
    {
        public delegate void WaitOrTimerCallback(Object state, bool timedOut);  // signalled or timed out

        private static RegisteredWaitHandle RegisterWaitForSingleObject(  // throws RegisterWaitException
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             Object state,
             uint millisecondsTimeOutInterval,
             bool executeOnlyOnce,   // NOTE: we do not allow other options that allow the callback to be queued as an APC
             ref StackCrawlMark stackMark,
             bool compressStack
             )
        {
            return null;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable    
        public static bool QueueUserWorkItem(
             WaitCallback callBack,     // NOTE: we do not expose options that allow the callback to be queued as an APC
             Object state
             )
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return QueueUserWorkItemHelper(callBack, state, ref stackMark, true);
        }

        //ThreadPool has per-appdomain managed queue of work-items. The VM is
        //responsible for just scheduling threads into appdomains. After that
        //work-items are dispatched from the managed queue.
        [System.Security.SecurityCritical]  // auto-generated
        private static bool QueueUserWorkItemHelper(WaitCallback callBack, Object state, ref StackCrawlMark stackMark, bool compressStack)
        {
            bool success = true;

            if (callBack != null)
            {
                //The thread pool maintains a per-appdomain managed work queue.
                //New thread pool entries are added in the managed queue.
                //The VM is responsible for the actual growing/shrinking of 
                //threads. 

                EnsureVMInitialized();

                //
                // If we are able to create the workitem, we need to get it in the queue without being interrupted
                // by a ThreadAbortException.
                //
                try { }
                finally
                {
                    QueueUserWorkItemCallback tpcallBack = new QueueUserWorkItemCallback(callBack, state, compressStack, ref stackMark);
                    ThreadPoolGlobals.workQueue.Enqueue(tpcallBack, true);
                    success = true;
                }
            }
            else
            {
                throw new ArgumentNullException("WaitCallback");
            }

            return success;
        }

        [SecurityCritical]
        internal static void UnsafeQueueCustomWorkItem(IThreadPoolWorkItem workItem, bool forceGlobal)
        {
            EnsureVMInitialized();

            //
            // Enqueue needs to be protected from ThreadAbort
            //
            try { }
            finally
            {
                ThreadPoolGlobals.workQueue.Enqueue(workItem, forceGlobal);
            }
        }

        // This method tries to take the target callback out of the current thread's queue.
        [SecurityCritical]
        internal static bool TryPopCustomWorkItem(IThreadPoolWorkItem workItem)
        {
            if (!ThreadPoolGlobals.vmTpInitialized)
                return false; //Not initialized, so there's no way this workitem was ever queued.

            return ThreadPoolGlobals.workQueue.LocalFindAndPop(workItem);
        }

        // Get all workitems.  Called by TaskScheduler in its debugger hooks.
        [SecurityCritical]
        internal static IEnumerable<IThreadPoolWorkItem> GetQueuedWorkItems()
        {
            return EnumerateQueuedWorkItems(ThreadPoolWorkQueue.allThreadQueues.Current, ThreadPoolGlobals.workQueue.queueTail);
        }

        [SecurityCritical]
        internal static IEnumerable<IThreadPoolWorkItem> GetLocallyQueuedWorkItems()
        {
            return EnumerateQueuedWorkItems(new WorkStealingQueue[] { ThreadPoolWorkQueueThreadLocals.threadLocals.workStealingQueue }, null);
        }

        [SecurityCritical]
        internal static IEnumerable<IThreadPoolWorkItem> GetGloballyQueuedWorkItems()
        {
            return EnumerateQueuedWorkItems(null, ThreadPoolGlobals.workQueue.queueTail);
        }

        // This is the method the debugger will actually call, if it ends up calling
        // into ThreadPool directly.  Tests can use this to simulate a debugger, as well.
        [SecurityCritical]
        internal static object[] GetQueuedWorkItemsForDebugger()
        {
            return ToObjectArray(GetQueuedWorkItems());
        }

        private static object[] ToObjectArray(IEnumerable<IThreadPoolWorkItem> workitems)
        {
            int i = 0;
            foreach (IThreadPoolWorkItem item in workitems)
            {
                i++;
            }

            object[] result = new object[i];
            i = 0;
            foreach (IThreadPoolWorkItem item in workitems)
            {
                if (i < result.Length) //just in case someone calls us while the queues are in motion
                    result[i] = item;
                i++;
            }

            return result;
        }

        internal static IEnumerable<IThreadPoolWorkItem> EnumerateQueuedWorkItems(WorkStealingQueue[] wsQueues, QueueSegment globalQueueTail)
        {
            if (wsQueues != null)
            {
                // First, enumerate all workitems in thread-local queues.
                foreach (WorkStealingQueue wsq in wsQueues)
                {
                    if (wsq != null && wsq.m_array != null)
                    {
                        IThreadPoolWorkItem[] items = wsq.m_array;
                        for (int i = 0; i < items.Length; i++)
                        {
                            IThreadPoolWorkItem item = items[i];
                            if (item != null)
                                yield return item;
                        }
                    }
                }
            }

            if (globalQueueTail != null)
            {
                // Now the global queue
                for (QueueSegment segment = globalQueueTail;
                    segment != null;
                    segment = segment.Next)
                {
                    IThreadPoolWorkItem[] items = segment.nodes;
                    for (int i = 0; i < items.Length; i++)
                    {
                        IThreadPoolWorkItem item = items[i];
                        if (item != null)
                            yield return item;
                    }
                }
            }
        }

        [SecurityCritical]
        private static void EnsureVMInitialized()
        {
            if (!ThreadPoolGlobals.vmTpInitialized)
            {
                // ThreadPool.InitializeVMTp(ref ThreadPoolGlobals.enableWorkerTracking);
                ThreadPoolGlobals.vmTpInitialized = true;
            }
        }
    }
}
