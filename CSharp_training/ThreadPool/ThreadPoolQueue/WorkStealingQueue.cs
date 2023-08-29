using System.Threading;

namespace CSharp_training.ThreadPool.ThreadPoolQueue
{
    // http://joeduffyblog.com/2008/08/11/building-a-custom-thread-pool-series-part-2-a-work-stealing-queue/
    // http://joeduffyblog.com/2008/07/29/building-a-custom-thread-pool-series-part-1/
    // http://www.cs.tau.ac.il/~shanir/multiprocessor-synch-2003/steal/notes/steal.pdf
    // http://supertech.csail.mit.edu/papers/steal.pdf
    // http://supertech.csail.mit.edu/papers/steal.pdf
    public class WorkStealingQueue
    {
        private const int INITIAL_SIZE = 32;
        internal volatile IThreadPoolWorkItem[] m_array = new IThreadPoolWorkItem[INITIAL_SIZE];
        private volatile int m_mask = INITIAL_SIZE - 1;

        private const int START_INDEX = 0;

        private volatile int m_headIndex = START_INDEX;
        private volatile int m_tailIndex = START_INDEX;

        private SpinLock m_foreignLock = new SpinLock(false);

        public void LocalPush(IThreadPoolWorkItem obj)
        {
            int tail = m_tailIndex;

            if (tail == int.MaxValue)
            {
                bool lockTaken = false;

                try
                {
                    m_foreignLock.Enter(ref lockTaken);

                    if (m_tailIndex == int.MaxValue)
                    {
                        m_headIndex = m_headIndex & m_mask;
                        m_tailIndex = tail = m_tailIndex & m_mask;
                    }
                }
                finally
                {
                    if (lockTaken)
                        m_foreignLock.Exit(true);
                }
            }

            if (tail < m_headIndex + m_mask)
            {
                Volatile.Write(ref m_array[tail & m_mask], obj);
                m_tailIndex = tail + 1;
            }
            else
            {
                bool lockTaken = false;
                try
                {
                    m_foreignLock.Enter(ref lockTaken);

                    int head = m_headIndex;
                    int count = m_tailIndex - m_headIndex;

                    if (count >= m_mask)
                    {
                        IThreadPoolWorkItem[] newArray = new IThreadPoolWorkItem[m_array.Length << 1];
                        for (int i = 0; i < m_array.Length; i++)
                            newArray[i] = m_array[(i + head) & m_mask];

                        m_array = newArray;
                        m_headIndex = 0;
                        m_tailIndex = tail = count;
                        m_mask = (m_mask << 1) | 1;
                    }

                    Volatile.Write(ref m_array[tail & m_mask], obj);
                    m_tailIndex = tail + 1;
                }
                finally
                {
                    if (lockTaken)
                        m_foreignLock.Exit(false);
                }
            }
        }

        public bool LocalFindAndPop(IThreadPoolWorkItem obj)
        {
            if (m_array[(m_tailIndex - 1) & m_mask] == obj)
            {
                IThreadPoolWorkItem unused;
                if (LocalPop(out unused))
                {
                    return true;
                }
                return false;
            }

            for (int i = m_tailIndex - 2; i >= m_headIndex; i--)
            {
                if (m_array[i & m_mask] == obj)
                {
                    bool lockTaken = false;
                    try
                    {
                        m_foreignLock.Enter(ref lockTaken);

                        if (m_array[i & m_mask] == null)
                            return false;

                        Volatile.Write(ref m_array[i & m_mask], null);

                        if (i == m_tailIndex)
                            m_tailIndex -= 1;
                        else if (i == m_headIndex)
                            m_headIndex += 1;

                        return true;
                    }
                    finally
                    {
                        if (lockTaken)
                            m_foreignLock.Exit(false);
                    }
                }
            }

            return false;
        }

        public bool LocalPop(out IThreadPoolWorkItem obj)
        {
            while (true)
            {
                int tail = m_tailIndex;
                if (m_headIndex >= tail)
                {
                    obj = null;
                    return false;
                }

                tail -= 1;
                Interlocked.Exchange(ref m_tailIndex, tail);

                if (m_headIndex <= tail)
                {
                    int idx = tail & m_mask;
                    obj = Volatile.Read(ref m_array[idx]);

                    if (obj == null) continue;

                   m_array[idx] = null;
                    return true;
                }
                else
                {
                    bool lockTaken = false;
                    try
                    {
                        m_foreignLock.Enter(ref lockTaken);

                        if (m_headIndex <= tail)
                        {
                            int idx = tail & m_mask;
                            obj = Volatile.Read(ref m_array[idx]);

                            if (obj == null) continue;

                            m_array[idx] = null;
                            return true;
                        }
                        else
                        {
                            m_tailIndex = tail + 1;
                            obj = null;
                            return false;
                        }
                    }
                    finally
                    {
                        if (lockTaken)
                            m_foreignLock.Exit(false);
                    }
                }
            }
        }

        public bool TrySteal(out IThreadPoolWorkItem obj, ref bool missedSteal)
        {
            return TrySteal(out obj, ref missedSteal, 0);
        }

        private bool TrySteal(out IThreadPoolWorkItem obj, ref bool missedSteal, int millisecondsTimeout)
        {
            obj = null;

            while (true)
            {
                if (m_headIndex >= m_tailIndex)
                    return false;

                bool taken = false;
                try
                {
                    m_foreignLock.TryEnter(millisecondsTimeout, ref taken);
                    if (taken)
                    {
                        int head = m_headIndex;
                        Interlocked.Exchange(ref m_headIndex, head + 1);

                        if (head < m_tailIndex)
                        {
                            int idx = head & m_mask;
                            obj = Volatile.Read(ref m_array[idx]);

                            if (obj == null) continue;

                            m_array[idx] = null;
                            return true;
                        }
                        else
                        {
                            m_headIndex = head;
                            obj = null;
                            missedSteal = true;
                        }
                    }
                    else
                    {
                        missedSteal = true;
                    }
                }
                finally
                {
                    if (taken)
                        m_foreignLock.Exit(false);
                }

                return false;
            }
        }
    }
}