// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static class DiagnosticAnalyzerExtensions
{
    extension(DiagnosticAnalyzer analyzer)
    {
        public bool IsWorkspaceDiagnosticAnalyzer()
        => analyzer is DocumentDiagnosticAnalyzer;

        public bool IsBuiltInAnalyzer()
            => analyzer is IBuiltInAnalyzer || analyzer.IsWorkspaceDiagnosticAnalyzer() || analyzer.IsCompilerAnalyzer();

        public (string analyzerId, VersionStamp version) GetAnalyzerIdAndVersion()
        {
            // Get the unique ID for given diagnostic analyzer.
            // note that we also put version stamp so that we can detect changed analyzer.
            var typeInfo = analyzer.GetType().GetTypeInfo();
            return (analyzer.GetAnalyzerId(), GetAnalyzerVersion(typeInfo.Assembly.Location));
        }

        public string GetAnalyzerAssemblyName()
            => analyzer.GetType().Assembly.GetName().Name ?? throw ExceptionUtilities.Unreachable();
    }

    extension(DiagnosticDescriptor descriptor)
    {
        public ReportDiagnostic GetEffectiveSeverity(CompilationOptions options)
        {
            return options == null
                ? descriptor.DefaultSeverity.ToReportDiagnostic()
                : descriptor.GetEffectiveSeverity(options);
        }
    }

    private static VersionStamp GetAnalyzerVersion(string path)
    {
        if (path == null || !File.Exists(path))
        {
            return VersionStamp.Default;
        }

        return VersionStamp.Create(File.GetLastWriteTimeUtc(path));
    }

    extension(Dictionary<string, DiagnosticAnalyzer> analyzerMap)
    {
        public void AppendAnalyzerMap(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            foreach (var analyzer in analyzers)
            {
                // user might have included exact same analyzer twice as project analyzers explicitly. we consider them as one
                analyzerMap[analyzer.GetAnalyzerId()] = analyzer;
            }
        }
    }

    extension(IDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> analysisResult)
    {
        public IEnumerable<AnalyzerPerformanceInfo> ToAnalyzerPerformanceInfo(DiagnosticAnalyzerInfoCache analyzerInfo)
        => analysisResult.Select(kv => new AnalyzerPerformanceInfo(kv.Key.GetAnalyzerId(), analyzerInfo.IsTelemetryCollectionAllowed(kv.Key), kv.Value.ExecutionTime));
    }
}
