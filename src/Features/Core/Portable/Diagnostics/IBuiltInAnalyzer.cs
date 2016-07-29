// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// This interface is a marker for all the analyzers that are built in.
    /// We will record non-fatal-watson if any analyzer with this interface throws an exception.
    /// </summary>
    internal interface IBuiltInAnalyzer
    {
        /// <summary>
        /// This category will be used to run analyzer more efficiently by restricting scope of analysis
        /// </summary>
        DiagnosticAnalyzerCategory GetAnalyzerCategory();

        /// <summary>
        /// This indicates whether this builtin analyzer must run in proc or can be run on remote host such as service hub.
        /// 
        /// if the diagnostic analyzer can run in command line as it is, then it should be able to run in remote host. 
        /// otherwise, it won't unless diagnostic analyzer author make changes in remote host to provide whatever missing
        /// data command line build doesn't provide such as workspace options/services/MEF and etc.
        /// 
        /// at this moment, remote host provide same context as command line build and only that context
        /// </summary>
        bool RunInProcess { get; }
    }
}
