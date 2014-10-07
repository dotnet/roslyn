using System;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.CodeAnalysis.Host.Esent
{
    internal partial class EsentKeyValueStorage
    {
        private class EsentInstanceHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public EsentInstanceHandle(IntPtr handle)
                : base(ownsHandle: true)
            {
                SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                // try to close database in a nice way
                var result = NativeMethods.JetTerm2(handle, TermGrbit.Complete);
                if (result.ReturnCode == EsentReturnCode.TooManyActiveUsers)
                {
                    // there are active sessions - close the instance leaving it in a dirty state (it will be recovered next time during JetInit)
                    return NativeMethods.JetTerm2(handle, TermGrbit.Dirty).ReturnCode == EsentReturnCode.Success;
                }
                else
                {
                    return result.ReturnCode == EsentReturnCode.Success;
                }
            }
        }
    }
}
