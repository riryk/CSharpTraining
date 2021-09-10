using System;
using System.Threading;

namespace CSharp_training.ThreadPool.ThreadPoolQueue
{
    public class SparseArray<T> where T : class
    {
        private volatile T[] m_array;

        internal SparseArray(int initialSize)
        {
            m_array = new T[initialSize];
        }

        internal T[] Current
        {
            get { return m_array; }
        }

        internal int Add(T e)
        {
            while (true)
            {
                T[] array = m_array;
                lock (array)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (array[i] == null)
                        {
                            Volatile.Write(ref array[i], e);
                            return i;
                        }
                        else if (i == array.Length - 1)
                        {
                            if (array != m_array)
                                continue;

                            T[] newArray = new T[array.Length * 2];
                            Array.Copy(array, newArray, i + 1);
                            newArray[i + 1] = e;
                            m_array = newArray;
                            return i + 1;
                        }
                    }
                }
            }
        }

        internal void Remove(T e)
        {
            T[] array = m_array;
            lock (array)
            {
                for (int i = 0; i < m_array.Length; i++)
                {
                    if (m_array[i] == e)
                    {
                        Volatile.Write(ref m_array[i], null);
                        break;
                    }
                }
            }
        }
    }
}
