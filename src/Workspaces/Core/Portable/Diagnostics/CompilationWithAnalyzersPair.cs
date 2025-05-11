// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed class CompilationWithAnalyzersPair
{
    private readonly CompilationWithAnalyzers? _projectCompilationWithAnalyzers;
    private readonly CompilationWithAnalyzers? _hostCompilationWithAnalyzers;

    public CompilationWithAnalyzersPair(CompilationWithAnalyzers? projectCompilationWithAnalyzers, CompilationWithAnalyzers? hostCompilationWithAnalyzers)
    {
        if (projectCompilationWithAnalyzers is not null && hostCompilationWithAnalyzers is not null)
        {
            Contract.ThrowIfFalse(projectCompilationWithAnalyzers.AnalysisOptions.ReportSuppressedDiagnostics == hostCompilationWithAnalyzers.AnalysisOptions.ReportSuppressedDiagnostics);
            Contract.ThrowIfFalse(projectCompilationWithAnalyzers.AnalysisOptions.ConcurrentAnalysis == hostCompilationWithAnalyzers.AnalysisOptions.ConcurrentAnalysis);
        }
        else
        {
            Contract.ThrowIfTrue(projectCompilationWithAnalyzers is null && hostCompilationWithAnalyzers is null);
        }

        _projectCompilationWithAnalyzers = projectCompilationWithAnalyzers;
        _hostCompilationWithAnalyzers = hostCompilationWithAnalyzers;
    }

    public Compilation? ProjectCompilation => _projectCompilationWithAnalyzers?.Compilation;

    public Compilation? HostCompilation => _hostCompilationWithAnalyzers?.Compilation;

    public CompilationWithAnalyzers? ProjectCompilationWithAnalyzers => _projectCompilationWithAnalyzers;

    public CompilationWithAnalyzers? HostCompilationWithAnalyzers => _hostCompilationWithAnalyzers;

    public bool ConcurrentAnalysis => _projectCompilationWithAnalyzers?.AnalysisOptions.ConcurrentAnalysis ?? _hostCompilationWithAnalyzers!.AnalysisOptions.ConcurrentAnalysis;

    public bool HasAnalyzers => ProjectAnalyzers.Any() || HostAnalyzers.Any();

    public ImmutableArray<DiagnosticAnalyzer> ProjectAnalyzers => _projectCompilationWithAnalyzers?.Analyzers ?? [];

    public ImmutableArray<DiagnosticAnalyzer> HostAnalyzers => _hostCompilationWithAnalyzers?.Analyzers ?? [];

    public Task<AnalyzerTelemetryInfo> GetAnalyzerTelemetryInfoAsync(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
    {
        if (ProjectAnalyzers.Contains(analyzer))
        {
            return ProjectCompilationWithAnalyzers!.GetAnalyzerTelemetryInfoAsync(analyzer, cancellationToken);
        }
        else
        {
            Debug.Assert(HostAnalyzers.Contains(analyzer));
            return HostCompilationWithAnalyzers!.GetAnalyzerTelemetryInfoAsync(analyzer, cancellationToken);
        }
    }

    public async Task<AnalysisResultPair?> GetAnalysisResultAsync(CancellationToken cancellationToken)
    {
        var projectAnalysisResult = ProjectCompilationWithAnalyzers is not null
            ? await ProjectCompilationWithAnalyzers.GetAnalysisResultAsync(cancellationToken).ConfigureAwait(false)
            : null;
        var hostAnalysisResult = HostCompilationWithAnalyzers is not null
            ? await HostCompilationWithAnalyzers.GetAnalysisResultAsync(cancellationToken).ConfigureAwait(false)
            : null;

        return AnalysisResultPair.FromResult(projectAnalysisResult, hostAnalysisResult);
    }

    public async Task<AnalysisResultPair?> GetAnalysisResultAsync(SyntaxTree tree, TextSpan? filterSpan, ImmutableArray<DiagnosticAnalyzer> projectAnalyzers, ImmutableArray<DiagnosticAnalyzer> hostAnalyzers, CancellationToken cancellationToken)
    {
        var projectAnalysisResult = projectAnalyzers.Any()
            ? await ProjectCompilationWithAnalyzers!.GetAnalysisResultAsync(tree, filterSpan, projectAnalyzers, cancellationToken).ConfigureAwait(false)
            : null;
        var hostAnalysisResult = hostAnalyzers.Any()
            ? await HostCompilationWithAnalyzers!.GetAnalysisResultAsync(tree, filterSpan, hostAnalyzers, cancellationToken).ConfigureAwait(false)
            : null;

        return AnalysisResultPair.FromResult(projectAnalysisResult, hostAnalysisResult);
    }

    public async Task<AnalysisResultPair?> GetAnalysisResultAsync(AdditionalText file, TextSpan? filterSpan, ImmutableArray<DiagnosticAnalyzer> projectAnalyzers, ImmutableArray<DiagnosticAnalyzer> hostAnalyzers, CancellationToken cancellationToken)
    {
        var projectAnalysisResult = projectAnalyzers.Any()
            ? await ProjectCompilationWithAnalyzers!.GetAnalysisResultAsync(file, filterSpan, projectAnalyzers, cancellationToken).ConfigureAwait(false)
            : null;
        var hostAnalysisResult = hostAnalyzers.Any()
            ? await HostCompilationWithAnalyzers!.GetAnalysisResultAsync(file, filterSpan, hostAnalyzers, cancellationToken).ConfigureAwait(false)
            : null;

        return AnalysisResultPair.FromResult(projectAnalysisResult, hostAnalysisResult);
    }

    public async Task<AnalysisResultPair?> GetAnalysisResultAsync(SemanticModel model, TextSpan? filterSpan, ImmutableArray<DiagnosticAnalyzer> projectAnalyzers, ImmutableArray<DiagnosticAnalyzer> hostAnalyzers, CancellationToken cancellationToken)
    {
        var projectAnalysisResult = projectAnalyzers.Any()
            ? await ProjectCompilationWithAnalyzers!.GetAnalysisResultAsync(model, filterSpan, projectAnalyzers, cancellationToken).ConfigureAwait(false)
            : null;
        var hostAnalysisResult = hostAnalyzers.Any()
            ? await HostCompilationWithAnalyzers!.GetAnalysisResultAsync(model, filterSpan, hostAnalyzers, cancellationToken).ConfigureAwait(false)
            : null;

        return AnalysisResultPair.FromResult(projectAnalysisResult, hostAnalysisResult);
    }
}
