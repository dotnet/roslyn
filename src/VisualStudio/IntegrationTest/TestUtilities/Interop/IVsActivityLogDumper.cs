// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop
{
    [ComImport]
    [Guid("4F111D70-F291-428D-8E40-CB1D4B1A7BDE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsActivityLogDumper
    {
        [PreserveSig]
        int GetActivityLogBuffer([MarshalAs(UnmanagedType.BStr)] out string activityLogBuffer);
    }
}
