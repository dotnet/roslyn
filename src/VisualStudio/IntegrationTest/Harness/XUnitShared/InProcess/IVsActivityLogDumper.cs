// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.InProcess
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [ComImport]
    [Guid("4F111D70-F291-428D-8E40-CB1D4B1A7BDE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsActivityLogDumper
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string GetActivityLogBuffer();
    }
}
