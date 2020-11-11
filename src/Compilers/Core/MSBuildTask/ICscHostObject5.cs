// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Build.Tasks.Hosting;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /*
     * Interface:       ICscHostObject5
     * Owner:           
     *
     * Defines an interface for the Csc task to communicate with the IDE.  In particular,
     * the Csc task will delegate the actual compilation to the IDE, rather than shelling
     * out to the command-line compilers.
     *
     */
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    [Guid("E113A674-3F6C-4514-B7AD-1E59226A1C50")]
    public interface ICscHostObject5 : ICscHostObject4
    {
        bool SetErrorLog(string? errorLogFile);
        bool SetReportAnalyzer(bool reportAnalyzerInDiagnosticOutput);
    }
}
