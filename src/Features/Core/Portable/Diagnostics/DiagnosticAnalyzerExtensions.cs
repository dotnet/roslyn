// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static class DiagnosticAnalyzerExtensions
{
    public static bool IsWorkspaceDiagnosticAnalyzer(this DiagnosticAnalyzer analyzer)
        => analyzer is DocumentDiagnosticAnalyzer
        || analyzer is ProjectDiagnosticAnalyzer
        || analyzer == FileContentLoadAnalyzer.Instance
        || analyzer == GeneratorDiagnosticsPlaceholderAnalyzer.Instance;

    public static bool IsBuiltInAnalyzer(this DiagnosticAnalyzer analyzer)
        => analyzer is IBuiltInAnalyzer || analyzer.IsWorkspaceDiagnosticAnalyzer() || analyzer.IsCompilerAnalyzer();

    public static bool IsOpenFileOnly(this DiagnosticAnalyzer analyzer, SimplifierOptions? options)
        => analyzer is IBuiltInAnalyzer builtInAnalyzer && builtInAnalyzer.OpenFileOnly(options);

    public static ReportDiagnostic GetEffectiveSeverity(this DiagnosticDescriptor descriptor, CompilationOptions options)
    {
        return options == null
            ? descriptor.DefaultSeverity.ToReportDiagnostic()
            : descriptor.GetEffectiveSeverity(options);
    }

    public static (string analyzerId, VersionStamp version) GetAnalyzerIdAndVersion(this DiagnosticAnalyzer analyzer)
    {
        // Get the unique ID for given diagnostic analyzer.
        // note that we also put version stamp so that we can detect changed analyzer.
        var typeInfo = analyzer.GetType().GetTypeInfo();
        return (analyzer.GetAnalyzerId(), GetAnalyzerVersion(typeInfo.Assembly.Location));
    }

    private static VersionStamp GetAnalyzerVersion(string path)
    {
        if (path == null || !File.Exists(path))
        {
            return VersionStamp.Default;
        }

        return VersionStamp.Create(File.GetLastWriteTimeUtc(path));
    }

    public static string GetAnalyzerAssemblyName(this DiagnosticAnalyzer analyzer)
        => analyzer.GetType().Assembly.GetName().Name ?? throw ExceptionUtilities.Unreachable();

    public static void AppendAnalyzerMap(this Dictionary<string, DiagnosticAnalyzer> analyzerMap, IEnumerable<DiagnosticAnalyzer> analyzers)
    {
        foreach (var analyzer in analyzers)
        {
            // user might have included exact same analyzer twice as project analyzers explicitly. we consider them as one
            analyzerMap[analyzer.GetAnalyzerId()] = analyzer;
        }
    }

    public static IEnumerable<AnalyzerPerformanceInfo> ToAnalyzerPerformanceInfo(this IDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> analysisResult, DiagnosticAnalyzerInfoCache analyzerInfo)
        => analysisResult.Select(kv => new AnalyzerPerformanceInfo(kv.Key.GetAnalyzerId(), analyzerInfo.IsTelemetryCollectionAllowed(kv.Key), kv.Value.ExecutionTime));
}
