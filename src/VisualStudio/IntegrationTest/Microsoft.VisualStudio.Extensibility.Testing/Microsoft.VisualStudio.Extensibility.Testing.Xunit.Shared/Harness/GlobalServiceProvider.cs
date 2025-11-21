// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Threading;
    using Windows.Win32;
    using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

    public static class GlobalServiceProvider
    {
        private static IServiceProvider? _serviceProvider;

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
            var oleServiceProvider = (IOleServiceProvider?)oleMessageFilterForCallingThread;
            if (oleServiceProvider is null)
            {
                throw new InvalidOperationException();
            }

            return new ServiceProvider(oleServiceProvider);
        }

        private static object? GetOleMessageFilterForCallingThread()
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA)
            {
                return null;
            }

            if (PInvoke.CoRegisterMessageFilter(null, out var oldMessageFilter) < 0)
            {
                return null;
            }

            if (oldMessageFilter is null)
            {
                return null;
            }

            PInvoke.CoRegisterMessageFilter(oldMessageFilter, out _);
            return oldMessageFilter;
        }
    }
}
