﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// This interface is a marker for all the analyzers that are built in.
    /// We will record non-fatal-watson if any analyzer with this interface throws an exception.
    /// 
    /// also, built in analyzer can do things that third-party analyzer (command line analyzer) can't do
    /// such as reporting all diagnostic descriptors as hidden when it can return different severity on runtime.
    /// 
    /// or reporting diagnostics ID that is not reported by SupportedDiagnostics.
    /// 
    /// this interface is used by the engine to allow this special behavior over command line analyzers.
    /// </summary>
    internal interface IBuiltInAnalyzer
    {
        /// <summary>
        /// This category will be used to run analyzer more efficiently by restricting scope of analysis
        /// </summary>
        DiagnosticAnalyzerCategory GetAnalyzerCategory();

        /// <summary>
        /// This indicates whether this builtin analyzer will only run on opened files.
        /// 
        /// all analyzers that want to run on closed files must be able to run in remote host.
        /// </summary>
        bool OpenFileOnly(Workspace workspace);
    }
}
