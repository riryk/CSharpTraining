using System;
using System.Security;

namespace CSharp_training.ThreadPool.ThreadPoolQueue
{
    public static class ThreadPoolGlobals
    {
        //Per-appDomain quantum (in ms) for which the thread keeps processing
        //requests in the current domain.
        public static uint tpQuantum = 30U;

        public static int processorCount = Environment.ProcessorCount;

        public static volatile bool vmTpInitialized;
        public static bool enableWorkerTracking;

        [SecurityCritical]
        public static ThreadPoolWorkQueue workQueue = new ThreadPoolWorkQueue();

        [System.Security.SecuritySafeCritical] // static constructors should be safe to call
        static ThreadPoolGlobals()
        {
        }
    }
}
