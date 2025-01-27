// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("B420D68F-4CD1-4F5F-8D6C-40658B222ADC")]
internal interface IAnalyzerConfigFileHost
{
    void AddAnalyzerConfigFile([MarshalAs(UnmanagedType.LPWStr)] string filePath);
    void RemoveAnalyzerConfigFile([MarshalAs(UnmanagedType.LPWStr)] string filePath);
}
