using System;

namespace CSharp_training.ThreadPool
{
    public class WaitHandle : IDisposable
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
