﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal interface IRemoteDiagnosticAnalyzerService
{
    ValueTask<SerializableDiagnosticAnalysisResults> CalculateDiagnosticsAsync(Checksum solutionChecksum, DiagnosticArguments arguments, CancellationToken cancellationToken);
    ValueTask<ImmutableArray<DiagnosticData>> GetSourceGeneratorDiagnosticsAsync(Checksum solutionChecksum, ProjectId projectId, CancellationToken cancellationToken);
    ValueTask ReportAnalyzerPerformanceAsync(ImmutableArray<AnalyzerPerformanceInfo> snapshot, int unitCount, bool forSpanAnalysis, CancellationToken cancellationToken);
}

[DataContract]
internal readonly struct AnalyzerPerformanceInfo(string analyzerId, bool builtIn, TimeSpan timeSpan)
{
    [DataMember(Order = 0)]
    public readonly string AnalyzerId = analyzerId;

    [DataMember(Order = 1)]
    public readonly bool BuiltIn = builtIn;

    [DataMember(Order = 2)]
    public readonly TimeSpan TimeSpan = timeSpan;
}
