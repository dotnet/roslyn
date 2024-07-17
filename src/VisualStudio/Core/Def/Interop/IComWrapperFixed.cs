// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

#pragma warning disable RS0030 // Do not used banned APIs
/// <inheritdoc cref="IComWrapper"/>
#pragma warning restore RS0030 // Do not used banned APIs
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("CBD71F2C-6BC5-4932-B851-B93EB3151386")]
internal interface IComWrapperFixed
{
    /// <inheritdoc cref="IComWrapper.GCHandlePtr"/>
    /// <returns>An <see cref="IntPtr"/> which can be passed to <see cref="GCHandle.FromIntPtr(IntPtr)"/> to obtain
    /// a handle to the managed object.</returns>
    /// <remarks>
    /// The original native interface defined this as
    /// <see href="https://docs.microsoft.com/en-us/windows/win32/winprog64/avoiding-polymorphism"><c>INT_PTR</c></see>
    /// when it was actually being implemented as <see cref="IntPtr"/> for callers within the current process. This
    /// definition more faithfully reflects the original code.
    /// </remarks>
    [ComAliasName("VsShell.INT_PTR")]
    IntPtr GCHandlePtr { get; }
}
