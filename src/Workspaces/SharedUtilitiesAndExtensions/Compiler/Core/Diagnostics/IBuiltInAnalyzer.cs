// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.Diagnostics;

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
    /// If this analyzer is privileged and should run with higher priority than other analyzers.
    /// </summary>
    bool IsHighPriority { get; }
}
