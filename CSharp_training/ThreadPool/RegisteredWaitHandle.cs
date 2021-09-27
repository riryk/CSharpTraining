using System;
using System.Collections.Generic;
using System.Text;

namespace CSharp_training.ThreadPool
{
    public sealed class RegisteredWaitHandle : MarshalByRefObject
    {
        private RegisteredWaitHandleSafe internalRegisteredWait;

        internal RegisteredWaitHandle()
        {
            internalRegisteredWait = new RegisteredWaitHandleSafe();
        }
    }
}
