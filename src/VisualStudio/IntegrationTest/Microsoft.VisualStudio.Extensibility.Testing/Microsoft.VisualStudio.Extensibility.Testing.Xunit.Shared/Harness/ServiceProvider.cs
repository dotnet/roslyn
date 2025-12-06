// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Runtime.InteropServices;
    using IObjectWithSite = Microsoft.VisualStudio.OLE.Interop.IObjectWithSite;
    using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

    internal class ServiceProvider : IServiceProvider, IObjectWithSite
    {
        private static readonly Guid IUnknownGuid = new Guid("00000000-0000-0000-C000-000000000046");

        private IOleServiceProvider _serviceProvider;

        public ServiceProvider(IOleServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType is null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            return GetService(serviceType.GUID);
        }

        private object? GetService(Guid serviceGuid)
        {
            if (serviceGuid == typeof(IOleServiceProvider).GUID)
            {
                return _serviceProvider;
            }

            if (serviceGuid == typeof(IObjectWithSite).GUID)
            {
                return this;
            }

            if (_serviceProvider.QueryService(serviceGuid, IUnknownGuid, out var obj) < 0)
            {
                return null;
            }

            if (obj == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                return Marshal.GetObjectForIUnknown(obj);
            }
            finally
            {
                Marshal.Release(obj);
            }
        }

        void IObjectWithSite.SetSite(object pUnkSite)
        {
            if (pUnkSite is IOleServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }
        }

        void IObjectWithSite.GetSite(ref Guid riid, out IntPtr ppvSite)
        {
            var service = GetService(riid);
            if (service == null)
            {
                Marshal.ThrowExceptionForHR(-2147467262);
            }

            var unknown = Marshal.GetIUnknownForObject(service);
            try
            {
                Marshal.ThrowExceptionForHR(Marshal.QueryInterface(unknown, ref riid, out ppvSite));
            }
            finally
            {
                Marshal.Release(unknown);
            }
        }
    }
}
