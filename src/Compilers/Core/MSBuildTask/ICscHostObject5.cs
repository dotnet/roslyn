// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        bool SetErrorLog(string errorLogFile);
        bool SetReportAnalyzer(bool reportAnalyzerInDiagnosticOutput);
    }
}
