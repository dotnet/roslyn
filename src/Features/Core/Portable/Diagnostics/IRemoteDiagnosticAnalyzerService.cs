// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    interface IRemoteDiagnosticAnalyzerService
    {
        Task CalculateDiagnosticsAsync(DiagnosticArguments arguments, string streamName, CancellationToken cancellationToken);
        void ReportAnalyzerPerformance(List<AnalyzerPerformanceInfo> snapshot, int unitCount, CancellationToken cancellationToken);
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
