// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable disable

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
