using System;
using System.Diagnostics;
using System.Threading;

namespace CSharp_training.ThreadPool.SimpleThreadPool
{
    public static class SimpleLockThreadPoolTest
    {
        public static void Test(bool separateQueueFromDrain)
        {
            const int warmupRunsPerThreadPool = 100;
            const int realRunsPerThreadPool = 1000000;

            IThreadPool[] threadPools = new IThreadPool[]
            {
                new SimpleLockThreadPool(true),  // Flow EC
                new SimpleLockThreadPool(false), // Don't flow EC
            };

            long[] queueCost = new long[threadPools.Length];
            long[] drainCost = new long[threadPools.Length];

            for (int i = 0; i < threadPools.Length; i++)
            {
                IThreadPool itp = threadPools[i];
                Console.Write("#{0} {1}: ", i, itp.ToString().PadRight(26));

                // Warm up:
                using (CountdownEvent cev = new CountdownEvent(warmupRunsPerThreadPool))
                {
                    WaitCallback wc = delegate
                    {
                        cev.AddCount(-1);
                    };

                    for (int j = 0; j < warmupRunsPerThreadPool; j++)
                    {
                        itp.QueueUserWorkItem(wc, null);
                    }
                }

                // Now do the real thing:
                int g0collects = GC.CollectionCount(0);
                int g1collects = GC.CollectionCount(1);
                int g2collects = GC.CollectionCount(2);

                using (CountdownEvent cev = new CountdownEvent(realRunsPerThreadPool))
                {
                    using (ManualResetEvent gun = new ManualResetEvent(false))
                    {
                        WaitCallback wc = delegate
                        {
                            if (separateQueueFromDrain)
                            {
                                gun.WaitOne();
                            }

                            cev.AddCount(-1);
                        };

                        Stopwatch sw = Stopwatch.StartNew();

                        for (int j = 0; j < realRunsPerThreadPool; j++)
                            itp.QueueUserWorkItem(wc, null);

                        queueCost[i] = sw.ElapsedTicks;

                        if (separateQueueFromDrain)
                        {
                            gun.Set();
                        }

                        cev.Wait();

                        drainCost[i] = sw.ElapsedTicks;
                    }
                }

                g0collects = GC.CollectionCount(0) - g0collects;
                g1collects = GC.CollectionCount(1) - g1collects;
                g2collects = GC.CollectionCount(2) - g2collects;

                Console.WriteLine("q: {0}, d: {1}, t: {2} (collects: 0={3},1={4},2={5})",
                    queueCost[i].ToString("#,##0"),
                    drainCost[i].ToString("#,##0"),
                    (queueCost[i] + drainCost[i]).ToString("#,##0"),
                    g0collects,
                    g1collects,
                    g2collects
                );

                // itp.Dispose();
                GC.Collect(2);
                GC.WaitForPendingFinalizers();
            }
        }
    }
}
