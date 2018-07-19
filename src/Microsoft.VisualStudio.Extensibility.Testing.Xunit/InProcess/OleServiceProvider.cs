// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.InProcess
{
    using System;
    using System.Runtime.InteropServices;
    using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
    using IUnknown = stdole.IUnknown;

    internal sealed class OleServiceProvider : IServiceProvider
    {
        private readonly IOleServiceProvider _serviceProvider;

        public OleServiceProvider(IOleServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public OleServiceProvider(EnvDTE.DTE dte)
            : this((IOleServiceProvider)dte)
        {
        }

        public object GetService(Type serviceType)
        {
            if (serviceType is null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (serviceType.GUID == typeof(IOleServiceProvider).GUID)
            {
                return _serviceProvider;
            }

            Marshal.ThrowExceptionForHR(_serviceProvider.QueryService(serviceType.GUID, typeof(IUnknown).GUID, out var service));
            try
            {
                return Marshal.GetObjectForIUnknown(service);
            }
            finally
            {
                Marshal.Release(service);
            }
        }
    }
}
