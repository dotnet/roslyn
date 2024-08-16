// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("AC3B1B70-1108-498b-8655-D88A0833D925")]
internal interface IVsUndoState
{
    [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    int IsEnabled([Out] out int fEnabled);
}
