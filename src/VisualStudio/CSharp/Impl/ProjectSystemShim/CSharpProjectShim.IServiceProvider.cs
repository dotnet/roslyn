// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
