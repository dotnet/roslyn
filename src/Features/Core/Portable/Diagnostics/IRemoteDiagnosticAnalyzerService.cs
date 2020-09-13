// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal interface IRemoteDiagnosticAnalyzerService
    {
        ValueTask CalculateDiagnosticsAsync(PinnedSolutionInfo solutionInfo, DiagnosticArguments arguments, Stream outputStream, CancellationToken cancellationToken);
        ValueTask ReportAnalyzerPerformanceAsync(ImmutableArray<AnalyzerPerformanceInfo> snapshot, int unitCount, CancellationToken cancellationToken);
    }

    internal readonly struct AnalyzerPerformanceInfo
    {
        public readonly string AnalyzerId;
        public readonly bool BuiltIn;
        public readonly TimeSpan TimeSpan;

        public AnalyzerPerformanceInfo(string analyzerid, bool builtIn, TimeSpan timeSpan)
        {
            AnalyzerId = analyzerid;
            BuiltIn = builtIn;
            TimeSpan = timeSpan;
        }
    }
}
