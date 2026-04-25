// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.RazorExtension.SyntaxVisualizer;

internal static class VSServiceHelpers
{
    private static IServiceProvider? s_globalServiceProvider;

    private static IServiceProvider GlobalServiceProvider
    {
        get
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (s_globalServiceProvider == null)
            {
                s_globalServiceProvider = (IServiceProvider)Package.GetGlobalService(typeof(IServiceProvider));
            }

            return s_globalServiceProvider;
        }
    }

    internal static TServiceInterface GetRequiredMefService<TServiceInterface, TService>()
        where TServiceInterface : class
        where TService : class
    {
        var service = (TServiceInterface?)GetService(GlobalServiceProvider, typeof(TService).GUID, false);
        Assumes.Present(service);
        return service;
    }

    internal static TServiceInterface GetRequiredMefService<TServiceInterface>() where TServiceInterface : class
    {
        var componentModel = GetRequiredMefService<IComponentModel, SComponentModel>();
        Assumes.Present(componentModel);
        return componentModel.GetService<TServiceInterface>();
    }

    private static object? GetService(IServiceProvider serviceProvider, Guid guidService, bool unique)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var guidInterface = VSConstants.IID_IUnknown;
        object? service = null;
        if (serviceProvider.QueryService(ref guidService, ref guidInterface, out var ptr) == 0 &&
            ptr != IntPtr.Zero)
        {
            try
            {
                if (unique)
                {
                    service = Marshal.GetUniqueObjectForIUnknown(ptr);
                }
                else
                {
                    service = Marshal.GetObjectForIUnknown(ptr);
                }
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        return service;
    }
}
