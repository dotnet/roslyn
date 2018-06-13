// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    /// <summary>
    /// This is a marker interface to identify <see cref="AutomationRetryWrapper{T}"/> objects via a runtime callable
    /// wrapper (RCW).
    /// </summary>
    [ComImport]
    [Guid("22DA53C1-848E-4BC7-9482-0480CA045563")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IRetryWrapper
    {
        object WrappedObject
        {
            get;
        }
    }
}
