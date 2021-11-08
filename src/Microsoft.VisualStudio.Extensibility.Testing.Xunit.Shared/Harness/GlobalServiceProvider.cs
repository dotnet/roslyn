// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable disable

namespace Xunit.Harness
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

    public static class GlobalServiceProvider
    {
        private static IServiceProvider _serviceProvider;

        public static IServiceProvider ServiceProvider
        {
            get
            {
                return _serviceProvider ?? (_serviceProvider = GetGlobalServiceProvider());
            }
        }

        private static IServiceProvider GetGlobalServiceProvider()
        {
            var oleMessageFilterForCallingThread = GetOleMessageFilterForCallingThread();
            var oleServiceProvider = (IOleServiceProvider)oleMessageFilterForCallingThread;
            return new ServiceProvider(oleServiceProvider);
        }

        private static object GetOleMessageFilterForCallingThread()
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA)
            {
                return null;
            }

            if (NativeMethods.CoRegisterMessageFilter(IntPtr.Zero, out var oldMessageFilter) < 0)
            {
                return null;
            }

            if (oldMessageFilter == IntPtr.Zero)
            {
                return null;
            }

            NativeMethods.CoRegisterMessageFilter(oldMessageFilter, out _);

            try
            {
                return Marshal.GetObjectForIUnknown(oldMessageFilter);
            }
            finally
            {
                Marshal.Release(oldMessageFilter);
            }
        }
    }
}
