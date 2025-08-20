// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal readonly struct DiagnosticAnalysisResults(
    ImmutableArray<(string analyzerId, DiagnosticMap diagnosticMap)> diagnostics,
    ImmutableArray<(string analyzerId, AnalyzerTelemetryInfo)> telemetry)
{
    public static readonly DiagnosticAnalysisResults Empty = default;

    internal readonly ImmutableArray<(string analyzerId, DiagnosticMap diagnosticMap)> Diagnostics => diagnostics.NullToEmpty();
    internal readonly ImmutableArray<(string analyzerId, AnalyzerTelemetryInfo telemetry)> Telemetry => telemetry.NullToEmpty();
}

internal readonly struct DiagnosticMap(
    ImmutableArray<(DocumentId, ImmutableArray<DiagnosticData>)> syntax,
    ImmutableArray<(DocumentId, ImmutableArray<DiagnosticData>)> semantic,
    ImmutableArray<(DocumentId, ImmutableArray<DiagnosticData>)> nonLocal,
    ImmutableArray<DiagnosticData> other)
{
    public readonly ImmutableArray<(DocumentId, ImmutableArray<DiagnosticData>)> Syntax = syntax;
    public readonly ImmutableArray<(DocumentId, ImmutableArray<DiagnosticData>)> Semantic = semantic;
    public readonly ImmutableArray<(DocumentId, ImmutableArray<DiagnosticData>)> NonLocal = nonLocal;
    public readonly ImmutableArray<DiagnosticData> Other = other;
}
