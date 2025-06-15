// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed class AnalysisResultPair
{
    private readonly AnalysisResult? _projectAnalysisResult;
    private readonly AnalysisResult? _hostAnalysisResult;

    private ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>>? _lazyMergedSyntaxDiagnostics;
    private ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>>? _lazyMergedSemanticDiagnostics;
    private ImmutableDictionary<AdditionalText, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>>? _lazyMergedAdditionalFileDiagnostics;
    private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>? _lazyMergedCompilationDiagnostics;
    private ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo>? _lazyMergedAnalyzerTelemetryInfo;

    public AnalysisResultPair(AnalysisResult? projectAnalysisResult, AnalysisResult? hostAnalysisResult)
    {
        if (projectAnalysisResult is not null && hostAnalysisResult is not null)
        {
        }
        else
        {
            Contract.ThrowIfTrue(projectAnalysisResult is null && hostAnalysisResult is null);
        }

        _projectAnalysisResult = projectAnalysisResult;
        _hostAnalysisResult = hostAnalysisResult;
    }

    public AnalysisResult? ProjectAnalysisResult => _projectAnalysisResult;

    public AnalysisResult? HostAnalysisResult => _hostAnalysisResult;

    public ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> MergedSyntaxDiagnostics
    {
        get
        {
            return InterlockedOperations.Initialize(
                ref _lazyMergedSyntaxDiagnostics,
                static arg =>
                {
                    if (arg.projectDiagnostics is null)
                    {
                        // project and host diagnostics cannot both be null
                        Contract.ThrowIfNull(arg.hostDiagnostics);
                        return arg.hostDiagnostics;
                    }
                    else if (arg.hostDiagnostics is null)
                    {
                        return arg.projectDiagnostics;
                    }

                    return MergeDiagnostics(arg.projectDiagnostics, arg.hostDiagnostics);
                },
                (projectDiagnostics: ProjectAnalysisResult?.SyntaxDiagnostics, hostDiagnostics: HostAnalysisResult?.SyntaxDiagnostics));
        }
    }

    public ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> MergedSemanticDiagnostics
    {
        get
        {
            return InterlockedOperations.Initialize(
                ref _lazyMergedSemanticDiagnostics,
                static arg =>
                {
                    if (arg.projectDiagnostics is null)
                    {
                        // project and host diagnostics cannot both be null
                        Contract.ThrowIfNull(arg.hostDiagnostics);
                        return arg.hostDiagnostics;
                    }
                    else if (arg.hostDiagnostics is null)
                    {
                        return arg.projectDiagnostics;
                    }

                    return MergeDiagnostics(arg.projectDiagnostics, arg.hostDiagnostics);
                },
                (projectDiagnostics: ProjectAnalysisResult?.SemanticDiagnostics, hostDiagnostics: HostAnalysisResult?.SemanticDiagnostics));
        }
    }

    public ImmutableDictionary<AdditionalText, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> MergedAdditionalFileDiagnostics
    {
        get
        {
            return InterlockedOperations.Initialize(
                ref _lazyMergedAdditionalFileDiagnostics,
                static arg =>
                {
                    if (arg.projectDiagnostics is null)
                    {
                        // project and host diagnostics cannot both be null
                        Contract.ThrowIfNull(arg.hostDiagnostics);
                        return arg.hostDiagnostics;
                    }
                    else if (arg.hostDiagnostics is null)
                    {
                        return arg.projectDiagnostics;
                    }

                    return MergeDiagnostics(arg.projectDiagnostics, arg.hostDiagnostics);
                },
                (projectDiagnostics: ProjectAnalysisResult?.AdditionalFileDiagnostics, hostDiagnostics: HostAnalysisResult?.AdditionalFileDiagnostics));
        }
    }

    public ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> MergedCompilationDiagnostics
    {
        get
        {
            return InterlockedOperations.Initialize(
                ref _lazyMergedCompilationDiagnostics,
                static arg =>
                {
                    if (arg.projectDiagnostics is null)
                    {
                        // project and host diagnostics cannot both be null
                        Contract.ThrowIfNull(arg.hostDiagnostics);
                        return arg.hostDiagnostics;
                    }
                    else if (arg.hostDiagnostics is null)
                    {
                        return arg.projectDiagnostics;
                    }

                    return MergeDiagnostics(arg.projectDiagnostics, arg.hostDiagnostics);
                },
                (projectDiagnostics: ProjectAnalysisResult?.CompilationDiagnostics, hostDiagnostics: HostAnalysisResult?.CompilationDiagnostics));
        }
    }

    public ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> MergedAnalyzerTelemetryInfo
    {
        get
        {
            return InterlockedOperations.Initialize(
                ref _lazyMergedAnalyzerTelemetryInfo,
                static arg =>
                {
                    if (arg.projectTelemetryInfo is null)
                    {
                        // project and host telemetry cannot both be null
                        Contract.ThrowIfNull(arg.hostTelemetryInfo);
                        return arg.hostTelemetryInfo;
                    }
                    else if (arg.hostTelemetryInfo is null)
                    {
                        return arg.projectTelemetryInfo;
                    }

                    return MergeTelemetry(arg.projectTelemetryInfo, arg.hostTelemetryInfo);
                },
                (projectTelemetryInfo: ProjectAnalysisResult?.AnalyzerTelemetryInfo, hostTelemetryInfo: HostAnalysisResult?.AnalyzerTelemetryInfo));
        }
    }

    public static AnalysisResultPair? FromResult(AnalysisResult? projectAnalysisResult, AnalysisResult? hostAnalysisResult)
    {
        if (projectAnalysisResult is null && hostAnalysisResult is null)
            return null;

        return new AnalysisResultPair(projectAnalysisResult, hostAnalysisResult);
    }

    private static ImmutableDictionary<TKey, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> MergeDiagnostics<TKey>(
        ImmutableDictionary<TKey, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> first,
        ImmutableDictionary<TKey, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> second)
        where TKey : class
    {
        var localSyntaxDiagnostics = first.ToBuilder();
        foreach (var (tree, treeDiagnostics) in second)
        {
            if (!localSyntaxDiagnostics.TryGetValue(tree, out var projectSyntaxDiagnostics))
            {
                localSyntaxDiagnostics.Add(tree, treeDiagnostics);
                continue;
            }

            localSyntaxDiagnostics[tree] = MergeDiagnostics(projectSyntaxDiagnostics, treeDiagnostics);
        }

        return localSyntaxDiagnostics.ToImmutable();
    }

    private static ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> MergeDiagnostics(
        ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> first,
        ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> second)
    {
        var analyzerToDiagnostics = first.ToBuilder();
        foreach (var (analyzer, diagnostics) in second)
        {
            if (!analyzerToDiagnostics.TryGetValue(analyzer, out var firstDiagnostics))
            {
                analyzerToDiagnostics.Add(analyzer, diagnostics);
                continue;
            }

            analyzerToDiagnostics[analyzer] = firstDiagnostics.AddRange(diagnostics);
        }

        return analyzerToDiagnostics.ToImmutable();
    }

    private static ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> MergeTelemetry(
        ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> first,
        ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> second)
    {
        var analyzerToDiagnostics = first.ToBuilder();
        foreach (var (analyzer, telemetry) in second)
        {
            if (!analyzerToDiagnostics.TryGetValue(analyzer, out var firstTelemetry))
            {
                analyzerToDiagnostics.Add(analyzer, telemetry);
                continue;
            }

            // For telemetry info, keep whichever instance had the longest time
            if (telemetry.ExecutionTime > firstTelemetry.ExecutionTime)
                analyzerToDiagnostics[analyzer] = telemetry;
        }

        return analyzerToDiagnostics.ToImmutable();
    }
}
