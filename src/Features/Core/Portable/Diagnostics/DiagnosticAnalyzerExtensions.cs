// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static class DiagnosticAnalyzerExtensions
{
    public static bool IsWorkspaceDiagnosticAnalyzer(this DiagnosticAnalyzer analyzer)
        => analyzer is DocumentDiagnosticAnalyzer;

    public static bool IsBuiltInAnalyzer(this DiagnosticAnalyzer analyzer)
        => analyzer is IBuiltInAnalyzer || analyzer.IsWorkspaceDiagnosticAnalyzer() || analyzer.IsCompilerAnalyzer();

    public static ReportDiagnostic GetEffectiveSeverity(this DiagnosticDescriptor descriptor, CompilationOptions options)
    {
        return options == null
            ? descriptor.DefaultSeverity.ToReportDiagnostic()
            : descriptor.GetEffectiveSeverity(options);
    }

    public static string GetAnalyzerAssemblyName(this DiagnosticAnalyzer analyzer)
        => analyzer.GetType().Assembly.GetName().Name ?? throw ExceptionUtilities.Unreachable();

    public static void AppendAnalyzerMap(this Dictionary<string, DiagnosticAnalyzer> analyzerMap, ImmutableArray<DiagnosticAnalyzer> analyzers)
    {
        foreach (var analyzer in analyzers)
        {
            // user might have included exact same analyzer twice as project analyzers explicitly. we consider them as one
            analyzerMap[analyzer.GetAnalyzerId()] = analyzer;
        }
    }

    public static ImmutableArray<AnalyzerPerformanceInfo> ToAnalyzerPerformanceInfo(this IDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> analysisResult, DiagnosticAnalyzerInfoCache analyzerInfo)
        => analysisResult.SelectAsArray(kv => new AnalyzerPerformanceInfo(kv.Key.GetAnalyzerId(), analyzerInfo.IsTelemetryCollectionAllowed(kv.Key), kv.Value.ExecutionTime));

    public static Task<ImmutableArray<DiagnosticDescriptor>> GetDiagnosticDescriptorsAsync(
        this Project project,
        AnalyzerReference analyzerReference,
        CancellationToken cancellationToken)
    {
        var diagnosticAnalyzerService = project.Solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
        return diagnosticAnalyzerService.GetDiagnosticDescriptorsAsync(
            project.Solution, project.Id, analyzerReference, project.Language, cancellationToken);
    }
}
