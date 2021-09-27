using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace CSharp_training.ThreadPool
{
    internal static class Win32Native
    {
        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
    }

    internal sealed class RegisteredWaitHandleSafe : CriticalFinalizerObject
    {
        private static IntPtr InvalidHandle
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                return Win32Native.INVALID_HANDLE_VALUE;
            }
        }

        private IntPtr registeredWaitHandle;

        private WaitHandle m_internalWaitObject;

        private bool bReleaseNeeded = false;

        internal RegisteredWaitHandleSafe()
        {
            registeredWaitHandle = InvalidHandle;
        }

        internal IntPtr GetHandle()
        {
            return registeredWaitHandle;
        }

        internal void SetHandle(IntPtr handle)
        {
            registeredWaitHandle = handle;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal void SetWaitObject(WaitHandle waitObject)
        {
            // needed for DangerousAddRef
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                m_internalWaitObject = waitObject;
                if (waitObject != null)
                {
                    // m_internalWaitObject.SafeWaitHandle.DangerousAddRef(ref bReleaseNeeded);
                }
            }
        }
    }
}
