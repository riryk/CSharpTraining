using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CSharp_training.ThreadPool.ThreadPoolQueue
{
    public class QueueSegment
    {
        internal readonly IThreadPoolWorkItem[] nodes;
        private const int QueueSegmentLength = 256;

        public volatile QueueSegment Next;

        // Holds the indexes of the lowest and highest valid elements of the nodes array.
        // The low index is in the lower 16 bits, high index is in the upper 16 bits.
        private volatile int indexes;

        public QueueSegment()
        {
            nodes = new IThreadPoolWorkItem[QueueSegmentLength];
        }

        const int SixteenBits = 0xffff;

        void GetIndexes(out int upper, out int lower)
        {
            int i = indexes;
            upper = (i >> 16) & SixteenBits;
            lower = i & SixteenBits;
        }

        bool CompareExchangeIndexes(ref int prevUpper, int newUpper, ref int prevLower, int newLower)
        {
            int oldIndexes = (prevUpper << 16) | (prevLower & SixteenBits);
            int newIndexes = (newUpper << 16) | (newLower & SixteenBits);
            int prevIndexes = Interlocked.CompareExchange(ref indexes, newIndexes, oldIndexes);
            prevUpper = (prevIndexes >> 16) & SixteenBits;
            prevLower = prevIndexes & SixteenBits;
            return prevIndexes == oldIndexes;
        }

        public bool TryEnqueue(IThreadPoolWorkItem node)
        {
            int upper, lower;
            GetIndexes(out upper, out lower);

            while (true)
            {
                if (upper == nodes.Length)
                    return false;

                if (CompareExchangeIndexes(ref upper, upper + 1, ref lower, lower))
                {
                    Volatile.Write(ref nodes[upper], node);
                    return true;
                }
            }
        }

        public bool TryDequeue(out IThreadPoolWorkItem node)
        {
            int upper, lower;
            GetIndexes(out upper, out lower);

            while (true)
            {
                if (lower == upper)
                {
                    node = null;
                    return false;
                }

                if (CompareExchangeIndexes(ref upper, upper, ref lower, lower + 1))
                {
                    SpinWait spinner = new SpinWait();
                    while ((node = Volatile.Read(ref nodes[lower])) == null)
                        spinner.SpinOnce();

                    nodes[lower] = null;

                    return true;
                }
            }
        }
    }
}
