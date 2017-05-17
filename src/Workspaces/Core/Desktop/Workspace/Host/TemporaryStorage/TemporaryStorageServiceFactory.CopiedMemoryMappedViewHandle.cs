// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.CodeAnalysis.Host
{
    internal partial class TemporaryStorageServiceFactory
    {
        /// <summary>
        /// This critical handle increments the reference count of a <see cref="SafeMemoryMappedViewHandle"/>. The view
        /// will be released when it is disposed <em>and</em> all pointers acquired through
        /// <see cref="SafeBuffer.AcquirePointer"/> (which this class uses) are released.
        /// </summary>
        /// <remarks>
        /// <para><see cref="CriticalHandle"/> types are not reference counted, and are thus somewhat limited in their
        /// usefulness. However, this handle class has tightly restricted accessibility and is only used by managed
        /// code which does not rely on it counting references.</para>
        ///
        /// <para>This is a supporting class for <see cref="MemoryMappedInfo"/>. See additional comments on that
        /// class.</para>
        /// </remarks>
        private unsafe sealed class CopiedMemoryMappedViewHandle : CriticalHandleZeroOrMinusOneIsInvalid
        {
            private readonly SafeMemoryMappedViewHandle _viewHandle;

            public CopiedMemoryMappedViewHandle(SafeMemoryMappedViewHandle viewHandle)
            {
                _viewHandle = viewHandle;

                byte* pointer = null;

                // The following code uses a constrained execution region (CER) to ensure that the code ends up in one
                // of the following states, even in the presence of asynchronous exceptions like ThreadAbortException:
                //
                // 1. The pointer is not acquired, and the current handle is invalid (thus will not be released)
                // 2. The pointer is acquired, and the current handle is fully initialized for later cleanup
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    viewHandle.AcquirePointer(ref pointer);
                }
                finally
                {
                    SetHandle((IntPtr)pointer);
                }
            }

            public byte* Pointer => (byte*)handle;

            protected override bool ReleaseHandle()
            {
                // The operating system will release these handles when the process terminates, so do not spend time
                // releasing them manually in that case. Doing so causes unnecessary delays during application shutdown.
                if (!Environment.HasShutdownStarted)
                {
                    _viewHandle.ReleasePointer();
                }

                return true;
            }
        }
    }
}
