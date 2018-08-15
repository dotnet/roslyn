// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    // Various parts of VS (such as Venus) like to assume our project is an IServiceProvider, and so
    // we must implement it here.
    internal partial class CSharpProjectShim : Microsoft.VisualStudio.OLE.Interop.IServiceProvider
    {
        public int QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
        {
            if (riid == typeof(IVsContainedLanguageFactory).GUID)
            {
                int hr = Shell.ServiceProvider.GlobalProvider.QueryService(guidService, out var serviceObject);
                if (ErrorHandler.Succeeded(hr))
                {
                    ppvObject = Marshal.GetComInterfaceForObject(serviceObject, typeof(IVsContainedLanguageFactory));
                }
                else
                {
                    ppvObject = IntPtr.Zero;
                }

                return hr;
            }
            else
            {
                ppvObject = IntPtr.Zero;
                return VSConstants.E_FAIL;
            }
        }
    }
}
