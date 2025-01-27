// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("9A37496A-F2CB-49A8-A684-7DEAAD2B0F07")]
internal interface IAnalyzerHost
{
    void AddAnalyzerReference([MarshalAs(UnmanagedType.LPWStr)] string analyzerAssemblyFullPath);
    void RemoveAnalyzerReference([MarshalAs(UnmanagedType.LPWStr)] string analyzerAssemblyFullPath);
    void SetRuleSetFile([MarshalAs(UnmanagedType.LPWStr)] string ruleSetFileFullPath);
    void AddAdditionalFile([MarshalAs(UnmanagedType.LPWStr)] string additionalFilePath);
    void RemoveAdditionalFile([MarshalAs(UnmanagedType.LPWStr)] string additionalFilePath);
}
