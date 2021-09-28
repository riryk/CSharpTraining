using System.Threading;

namespace CSharp_training.ThreadPool.SimpleThreadPool
{
    struct WorkItem
    {
        internal WaitCallback m_work;
        internal object m_obj;
        internal ExecutionContext m_executionContext;

        internal WorkItem(WaitCallback work, object obj)
        {
            m_work = work;
            m_obj = obj;
            m_executionContext = null;
        }

        internal void Invoke()
        {
            // Run normally (delegate invoke) or under context, as appropriate.
            if (m_executionContext == null)
                m_work(m_obj);
            else
                ExecutionContext.Run(m_executionContext, ContextInvoke, null);
        }

        private void ContextInvoke(object obj)
        {
            m_work(m_obj);
        }
    }
}
