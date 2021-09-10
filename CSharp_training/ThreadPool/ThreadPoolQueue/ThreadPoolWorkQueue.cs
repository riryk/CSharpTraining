using System;
using System.Collections.Generic;
using System.Text;

namespace CSharp_training.ThreadPool.ThreadPoolQueue
{
    public class ThreadPoolWorkQueue
    {
        internal volatile QueueSegment queueHead;
        internal volatile QueueSegment queueTail;

        public void Enqueue(IThreadPoolWorkItem callback, bool forceGlobal)
        {

        }
    }
}
