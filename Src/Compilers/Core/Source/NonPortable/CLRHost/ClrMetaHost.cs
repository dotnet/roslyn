// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Runtime.Hosting.Interop;

namespace Microsoft.CodeAnalysis
{
    using Microsoft.Runtime.Hosting;
    //This and all of the COM interop signatures for using the native CLR activation APIs
    //were pulled from the CLR Activation Team's page on codeplex.

    /// <summary>
    /// Managed abstraction of the functionality provided by ICLRMetaHost.
    /// </summary>
    internal static class ClrMetaHost
    {

        /// <summary>
        /// Gets the <see cref="ClrRuntimeInfo"/> corresponding to the current runtime.
        /// That is, the runtime executing currently.
        /// </summary>
        public static ClrRuntimeInfo CurrentRuntime
        {
            get
            {
                IClrMetaHost m = HostingInteropHelper.GetClrMetaHost<IClrMetaHost>();
                return new ClrRuntimeInfo((IClrRuntimeInfo)m.GetRuntime(RuntimeEnvironment.GetSystemVersion(), typeof(IClrRuntimeInfo).GUID));
            }
        }
    }
}
