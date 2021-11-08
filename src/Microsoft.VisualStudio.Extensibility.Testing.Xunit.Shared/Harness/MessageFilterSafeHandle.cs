// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable disable

namespace Xunit.Harness
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;
    using IMessageFilter = Microsoft.VisualStudio.OLE.Interop.IMessageFilter;

    internal sealed class MessageFilterSafeHandle : SafeHandleMinusOneIsInvalid
    {
        private readonly IntPtr _oldFilter;

        private MessageFilterSafeHandle(IntPtr handle)
            : base(true)
        {
            SetHandle(handle);

            try
            {
                if (NativeMethods.CoRegisterMessageFilter(handle, out _oldFilter) != VSConstants.S_OK)
                {
                    throw new InvalidOperationException("Failed to register a new message filter");
                }
            }
            catch
            {
                SetHandleAsInvalid();
                throw;
            }
        }

        public static MessageFilterSafeHandle Register<T>(T messageFilter)
            where T : IMessageFilter
        {
            var handle = Marshal.GetComInterfaceForObject<T, IMessageFilter>(messageFilter);
            return new MessageFilterSafeHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            if (NativeMethods.CoRegisterMessageFilter(_oldFilter, out _) == VSConstants.S_OK)
            {
                Marshal.Release(handle);
            }

            return true;
        }
    }
}
