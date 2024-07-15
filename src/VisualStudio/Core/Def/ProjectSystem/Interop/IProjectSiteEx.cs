// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("38B39ADD-D4D6-47E1-B996-7ED16D0295A5")]
internal interface IProjectSiteEx
{
    void StartBatch();
    void EndBatch();

    void AddFileEx([MarshalAs(UnmanagedType.LPWStr)] string filePath, [MarshalAs(UnmanagedType.LPWStr)] string linkMetadata);

    /// <summary>
    /// Allows the project system to pass along property values not covered by the
    /// compiler's command line arguments.
    /// See <see cref="LanguageServices.ProjectSystem.IWorkspaceProjectContext.SetProperty(string, string)"/>
    /// for the corresponding method for CPS-based projects.
    /// </summary>
    void SetProperty([MarshalAs(UnmanagedType.LPWStr)] string property, [MarshalAs(UnmanagedType.LPWStr)] string value);
}
