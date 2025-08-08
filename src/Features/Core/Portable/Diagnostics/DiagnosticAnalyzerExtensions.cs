// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;

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

    public static IEnumerable<AnalyzerPerformanceInfo> ToAnalyzerPerformanceInfo(this IDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> analysisResult, DiagnosticAnalyzerInfoCache analyzerInfo)
        => analysisResult.Select(kv => new AnalyzerPerformanceInfo(kv.Key.GetAnalyzerId(), analyzerInfo.IsTelemetryCollectionAllowed(kv.Key), kv.Value.ExecutionTime));

    public static ImmutableArray<DiagnosticDescriptor> GetDiagnosticDescriptors(
        this Project project,
        AnalyzerReference analyzerReference)
    {
        var diagnosticAnalyzerService = project.Solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();

        var descriptors = analyzerReference
            .GetAnalyzers(project.Language)
            .SelectManyAsArray(a => diagnosticAnalyzerService.AnalyzerInfoCache.GetDiagnosticDescriptors(a));

        return descriptors;
    }
}
