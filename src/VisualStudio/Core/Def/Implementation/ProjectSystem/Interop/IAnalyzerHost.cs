// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop
{
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
}
