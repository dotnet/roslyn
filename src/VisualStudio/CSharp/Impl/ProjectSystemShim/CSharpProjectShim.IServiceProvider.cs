// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    // Various parts of VS (such as Venus) like to assume our project is an IServiceProvider, and so
    // we must implement it here.
    internal partial class CSharpProjectShim : Microsoft.VisualStudio.OLE.Interop.IServiceProvider
    {
        public int QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
        {
            var serviceProvider = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)_serviceProvider;
            return serviceProvider.QueryService(ref guidService, ref riid, out ppvObject);
        }
    }
}
