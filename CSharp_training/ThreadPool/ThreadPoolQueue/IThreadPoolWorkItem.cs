using System.Threading;

namespace CSharp_training.ThreadPool.ThreadPoolQueue
{
    public interface IThreadPoolWorkItem
    {
        void ExecuteWorkItem();
        void MarkAborted(ThreadAbortException tae);
    }
}
