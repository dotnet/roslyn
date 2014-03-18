// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.Runtime.Hosting.Interop;

namespace Microsoft.Runtime.Hosting
{
    /// <summary>
    /// Managed abstraction of the functionality provided by ICLRRuntimeInfo.
    /// </summary>
    internal class ClrRuntimeInfo
    {
        private readonly IClrRuntimeInfo _RuntimeInfo;

        /// <summary>
        /// Constructor that wraps an ICLRRuntimeInfo (used internally)
        /// </summary>
        internal ClrRuntimeInfo(IClrRuntimeInfo info)
        {
            System.Diagnostics.Debug.Assert(info != null);
            _RuntimeInfo = info;
        }

        /// <summary>
        /// Gets an interface provided by this runtime, such as ICLRRuntimeHost.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to be returned.  This must be an RCW interface</typeparam>
        /// <param name="clsid">The CLSID to be created</param>
        public TInterface GetInterface<TInterface>(Guid clsid)
        {
            return (TInterface)_RuntimeInfo.GetInterface(clsid, typeof(TInterface).GUID);
        }
    }
}
