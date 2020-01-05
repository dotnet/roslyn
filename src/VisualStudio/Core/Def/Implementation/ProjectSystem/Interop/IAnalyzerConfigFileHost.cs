// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("B420D68F-4CD1-4F5F-8D6C-40658B222ADC")]
    internal interface IAnalyzerConfigFileHost
    {
        void AddAnalyzerConfigFile([MarshalAs(UnmanagedType.LPWStr)] string filePath);
        void RemoveAnalyzerConfigFile([MarshalAs(UnmanagedType.LPWStr)] string filePath);
    }
}
