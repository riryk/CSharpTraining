using System;
using System.Security;
using System.Threading;

namespace CSharp_training.ThreadPool.ThreadPoolQueue
{
    public class ThreadPoolWorkQueue
    {
        internal volatile QueueSegment queueHead;
        internal volatile QueueSegment queueTail;
        internal static SparseArray<WorkStealingQueue> allThreadQueues = new SparseArray<WorkStealingQueue>(16);

        private volatile int numOutstandingThreadRequests = 0;

        public ThreadPoolWorkQueue()
        {
            queueTail = queueHead = new QueueSegment();
        }

        [SecurityCritical]
        internal void EnsureThreadRequested()
        {
            int count = numOutstandingThreadRequests;
            while (count < ThreadPoolGlobals.processorCount)
            {
                int prev = Interlocked.CompareExchange(ref numOutstandingThreadRequests, count + 1, count);
                if (prev == count)
                {
                    // ThreadPool.RequestWorkerThread();
                    break;
                }
                count = prev;
            }
        }

        [SecurityCritical]
        internal void MarkThreadRequestSatisfied()
        {
            int count = numOutstandingThreadRequests;
            while (count > 0)
            {
                int prev = Interlocked.CompareExchange(ref numOutstandingThreadRequests, count - 1, count);
                if (prev == count)
                {
                    break;
                }
                count = prev;
            }
        }

        [SecurityCritical]
        public ThreadPoolWorkQueueThreadLocals EnsureCurrentThreadHasQueue()
        {
            if (null == ThreadPoolWorkQueueThreadLocals.threadLocals)
                ThreadPoolWorkQueueThreadLocals.threadLocals = new ThreadPoolWorkQueueThreadLocals(this);
            return ThreadPoolWorkQueueThreadLocals.threadLocals;
        }

        public void Enqueue(IThreadPoolWorkItem callback, bool forceGlobal)
        {
            ThreadPoolWorkQueueThreadLocals tl = null;
            if (!forceGlobal)
                tl = ThreadPoolWorkQueueThreadLocals.threadLocals;

            if (null != tl)
            {
                tl.workStealingQueue.LocalPush(callback);
            }
            else
            {
                QueueSegment head = queueHead;

                while (!head.TryEnqueue(callback))
                {
                    // if (head.Next == null)
                    //    head.Next = new QueueSegment();
                    Interlocked.CompareExchange(ref head.Next, new QueueSegment(), null);

                    while (head.Next != null)
                    {
                        // if (queueHead == head)
                        //   queueHead = head.Next;
                        Interlocked.CompareExchange(ref queueHead, head.Next, head);
                        head = queueHead;
                    }
                }
            }

            EnsureThreadRequested();
        }

        [SecurityCritical]
        internal bool LocalFindAndPop(IThreadPoolWorkItem callback)
        {
            ThreadPoolWorkQueueThreadLocals tl = ThreadPoolWorkQueueThreadLocals.threadLocals;
            if (null == tl)
                return false;

            return tl.workStealingQueue.LocalFindAndPop(callback);
        }

        [SecurityCritical]
        public void Dequeue(ThreadPoolWorkQueueThreadLocals tl, out IThreadPoolWorkItem callback, out bool missedSteal)
        {
            callback = null;
            missedSteal = false;
            WorkStealingQueue wsq = tl.workStealingQueue;

            if (wsq.LocalPop(out callback))
            {
                if (null != callback)
                {
                    throw new Exception();
                }
            }

            if (null == callback)
            {
                QueueSegment tail = queueTail;
                while (true)
                {
                    if (tail.TryDequeue(out callback))
                    {
                        if (null != callback)
                            throw new Exception();
                        break;
                    }

                    if (null == tail.Next || !tail.IsUsedUp())
                    {
                        break;
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref queueTail, tail.Next, tail);
                        tail = queueTail;
                    }
                }
            }

            if (null == callback)
            {
                WorkStealingQueue[] otherQueues = allThreadQueues.Current;
                int i = tl.random.Next(otherQueues.Length);
                int c = otherQueues.Length;
                while (c > 0)
                {
                    WorkStealingQueue otherQueue = Volatile.Read(ref otherQueues[i % otherQueues.Length]);
                    if (otherQueue != null &&
                        otherQueue != wsq &&
                        otherQueue.TrySteal(out callback, ref missedSteal))
                    {
                        if (null != callback)
                            throw new Exception();
                        break;
                    }
                    i++;
                    c--;
                }
            }
        }

        [SecurityCritical]
        static internal bool Dispatch()
        {
            var workQueue = ThreadPoolGlobals.workQueue;
            int quantumStartTime = Environment.TickCount;

            workQueue.MarkThreadRequestSatisfied();

            bool needAnotherThread = true;
            IThreadPoolWorkItem workItem = null;
            try
            {
                ThreadPoolWorkQueueThreadLocals tl = workQueue.EnsureCurrentThreadHasQueue();

            }
            catch (Exception ex)
            {
                throw;
            }

            return true;
        }
    }
}
