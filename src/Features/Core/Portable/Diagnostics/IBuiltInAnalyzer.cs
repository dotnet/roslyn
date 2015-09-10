// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// This interface is a marker for all the analyzers that are built in.
    /// We will record non-fatal-watson if any analyzer with this interface throws an exception.
    /// </summary>
    internal interface IBuiltInAnalyzer
    {
        DiagnosticAnalyzerCategory GetAnalyzerCategory();
    }
}
