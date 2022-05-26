// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        bool SetErrorLog(string? errorLogFile);
        bool SetReportAnalyzer(bool reportAnalyzerInDiagnosticOutput);
    }
}
