// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;

/// <remarks>
/// A redefinition of Microsoft.VisualStudio.Shell.Interop.IVsInvisibleEditorManager. One critical difference
/// here is we declare the ppEditor retval argument as IntPtr instead of IVsInvisibleEditor. Since the
/// invisible editor is saved and closed when the last reference is Released(), it's critical we have precise
/// control when the COM object goes away. By default, the COM marshaller will return a non-unique RCW, which
/// means we have no control over when the RCW will call Release(). To have control, we need a unique RCW, but
/// the only way we can (correctly) get this is if we have the native IntPtr right from the start.
/// </remarks>
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("14439CDE-B6CF-4DD6-9615-67E8B3DF380D")]
internal interface IIntPtrReturningVsInvisibleEditorManager
{
    int RegisterInvisibleEditor(
        [MarshalAs(UnmanagedType.LPWStr)] string pszMkDocument,
        IVsProject? pProject,
        uint dwFlags,
        IVsSimpleDocFactory? pFactory,
        out IntPtr ppEditor);
}
