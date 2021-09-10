using System.Threading;

namespace CSharp_training.ThreadPool.ThreadPoolQueue
{
    internal interface IThreadPoolWorkItem
    {
        void ExecuteWorkItem();
        void MarkAborted(ThreadAbortException tae);
    }
}
