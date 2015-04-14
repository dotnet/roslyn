// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("91A139C9-8197-42ED-9081-32FB5C75E482")]
    internal interface IAnalyzerDependencyHost
    {
        void AddAnalyzerDependency([MarshalAs(UnmanagedType.LPWStr)] string analyzerDependencyFullPath);
        void RemoveAnalyzerDependency([MarshalAs(UnmanagedType.LPWStr)] string analyzerDependencyFullPath);
    }
}
