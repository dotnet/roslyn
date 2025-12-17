// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;
    using Windows.Win32;
    using IMessageFilter = Windows.Win32.Media.Audio.IMessageFilter;

    internal sealed class MessageFilterSafeHandle : SafeHandleMinusOneIsInvalid
    {
        private readonly IMessageFilter _oldFilter;

        private MessageFilterSafeHandle(IMessageFilter filter, IntPtr handle)
            : base(true)
        {
            SetHandle(handle);

            try
            {
                if (PInvoke.CoRegisterMessageFilter(filter, out _oldFilter) != VSConstants.S_OK)
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
            return new MessageFilterSafeHandle(messageFilter, handle);
        }

        protected override bool ReleaseHandle()
        {
            if (PInvoke.CoRegisterMessageFilter(_oldFilter, out _) == VSConstants.S_OK)
            {
                Marshal.Release(handle);
            }

            return true;
        }
    }
}
