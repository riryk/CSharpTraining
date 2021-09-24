using System;
using System.Security;
using System.Threading;

namespace CSharp_training.ThreadPool.ThreadPoolQueue
{
    public class ThreadPoolWorkQueueThreadLocals
    {
        [ThreadStatic]
        [SecurityCritical]
        public static ThreadPoolWorkQueueThreadLocals threadLocals;

        public readonly ThreadPoolWorkQueue workQueue;
        public readonly WorkStealingQueue workStealingQueue;
        public readonly Random random = new Random(Thread.CurrentThread.ManagedThreadId);

        public ThreadPoolWorkQueueThreadLocals(ThreadPoolWorkQueue tpq)
        {
            workQueue = tpq;
            workStealingQueue = new WorkStealingQueue();
            ThreadPoolWorkQueue.allThreadQueues.Add(workStealingQueue);
        }
    }
}
