// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal interface IRemoteDiagnosticAnalyzerService
    {
        ValueTask<SerializableDiagnosticAnalysisResults> CalculateDiagnosticsAsync(PinnedSolutionInfo solutionInfo, DiagnosticArguments arguments, CancellationToken cancellationToken);
        ValueTask ReportAnalyzerPerformanceAsync(ImmutableArray<AnalyzerPerformanceInfo> snapshot, int unitCount, CancellationToken cancellationToken);
        ValueTask StartSolutionCrawlerAsync(CancellationToken cancellationToken);
    }

    [DataContract]
    internal readonly struct AnalyzerPerformanceInfo
    {
        [DataMember(Order = 0)]
        public readonly string AnalyzerId;

        [DataMember(Order = 1)]
        public readonly bool BuiltIn;

        [DataMember(Order = 2)]
        public readonly TimeSpan TimeSpan;

        public AnalyzerPerformanceInfo(string analyzerId, bool builtIn, TimeSpan timeSpan)
        {
            AnalyzerId = analyzerId;
            BuiltIn = builtIn;
            TimeSpan = timeSpan;
        }
    }
}
