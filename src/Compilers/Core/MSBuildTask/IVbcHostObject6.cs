// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Build.Tasks.Hosting;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// Defines an interface that proffers a free threaded host object that
    /// allows for background threads to call directly (avoids marshalling
    /// to the UI thread.
    /// </summary>
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    [Guid("10617623-DD4E-4E81-B4C3-46F55DC76E52")]
    public interface IVbcHostObject6 : IVbcHostObject5
    {
        bool SetErrorLog(string errorLogFile);
        bool SetReportAnalyzer(bool reportAnalyzerInDiagnosticOutput);
    }
}
