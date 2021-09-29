using System.Threading;

namespace CSharp_training.ThreadPool.SimpleThreadPool
{
    public interface IThreadPool
    {
        void QueueUserWorkItem(WaitCallback work);
        void QueueUserWorkItem(WaitCallback work, object obj);
    }
}
