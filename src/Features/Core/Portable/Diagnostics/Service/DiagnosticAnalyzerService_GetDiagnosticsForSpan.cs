// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    private static async Task<ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>>> ComputeDocumentDiagnosticsCoreInProcessAsync(
        DocumentAnalysisExecutor executor,
        CancellationToken cancellationToken)
    {
        using var _ = PooledDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>>.GetInstance(out var builder);
        foreach (var analyzer in executor.AnalysisScope.Analyzers)
        {
            var diagnostics = await executor.ComputeDiagnosticsInProcessAsync(analyzer, cancellationToken).ConfigureAwait(false);
            builder.Add(analyzer, diagnostics);
        }

        return builder.ToImmutableDictionary();
    }

    public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanInProcessAsync(
        TextDocument document,
        TextSpan? range,
        DiagnosticIdFilter diagnosticIdFilter,
        CodeActionRequestPriority? priority,
        DiagnosticKind diagnosticKind,
        CancellationToken cancellationToken)
    {
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        var project = document.Project;
        var unfilteredAnalyzers = GetProjectAnalyzers_OnlyCallInProcess(project);
        var analyzers = unfilteredAnalyzers
            .WhereAsArray(a => DocumentAnalysisExecutor.IsAnalyzerEnabledForProject(a, project, _globalOptions));

        // Note that some callers, such as diagnostic tagger, might pass in a range equal to the entire document span.
        // We clear out range for such cases as we are computing full document diagnostics.
        if (range == new TextSpan(0, text.Length))
            range = null;

        // We log performance info when we are computing diagnostics for a span
        var logPerformanceInfo = range.HasValue;

        // If we are computing full document diagnostics, we will attempt to perform incremental
        // member edit analysis. This analysis is currently only enabled with LSP pull diagnostics.
        var incrementalAnalysis = range is null && document is Document { SupportsSyntaxTree: true };

        var (syntaxAnalyzers, semanticSpanAnalyzers, semanticDocumentAnalyzers) = await GetAllAnalyzersAsync().ConfigureAwait(false);
        syntaxAnalyzers = await FilterAnalyzersAsync(syntaxAnalyzers, AnalysisKind.Syntax, range).ConfigureAwait(false);
        semanticSpanAnalyzers = await FilterAnalyzersAsync(semanticSpanAnalyzers, AnalysisKind.Semantic, range).ConfigureAwait(false);
        semanticDocumentAnalyzers = await FilterAnalyzersAsync(semanticDocumentAnalyzers, AnalysisKind.Semantic, span: null).ConfigureAwait(false);

        var allDiagnostics = await this.ComputeDiagnosticsInProcessAsync(
            document, range, analyzers, syntaxAnalyzers, semanticSpanAnalyzers, semanticDocumentAnalyzers,
            incrementalAnalysis, logPerformanceInfo,
            cancellationToken).ConfigureAwait(false);
        return allDiagnostics.WhereAsArray(ShouldInclude);

        async ValueTask<(
            ImmutableArray<DiagnosticAnalyzer> syntaxAnalyzers,
            ImmutableArray<DiagnosticAnalyzer> semanticSpanAnalyzers,
            ImmutableArray<DiagnosticAnalyzer> semanticDocumentAnalyzers)> GetAllAnalyzersAsync()
        {
            try
            {
                using var _1 = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(out var syntaxAnalyzers);

                // If we are performing incremental member edit analysis to compute diagnostics incrementally,
                // we divide the analyzers into those that support span-based incremental analysis and
                // those that do not support incremental analysis and must be executed for the entire document.
                // Otherwise, if we are not performing incremental analysis, all semantic analyzers are added
                // to the span-based analyzer set as we want to compute diagnostics only for the given span.
                using var _2 = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(out var semanticSpanBasedAnalyzers);
                using var _3 = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(out var semanticDocumentBasedAnalyzers);

                using var _4 = TelemetryLogging.LogBlockTimeAggregatedHistogram(FunctionId.RequestDiagnostics_Summary, $"Pri{priority.GetPriorityInt()}");

                foreach (var analyzer in analyzers)
                {
                    if (!await ShouldIncludeAnalyzerAsync(analyzer).ConfigureAwait(false))
                        continue;

                    bool includeSyntax = true, includeSemantic = true;
                    if (diagnosticKind != DiagnosticKind.All)
                    {
                        var isCompilerAnalyzer = analyzer.IsCompilerAnalyzer();
                        includeSyntax = isCompilerAnalyzer
                            ? diagnosticKind == DiagnosticKind.CompilerSyntax
                            : diagnosticKind == DiagnosticKind.AnalyzerSyntax;
                        includeSemantic = isCompilerAnalyzer
                            ? diagnosticKind == DiagnosticKind.CompilerSemantic
                            : diagnosticKind == DiagnosticKind.AnalyzerSemantic;
                    }

                    includeSyntax = includeSyntax && analyzer.SupportAnalysisKind(AnalysisKind.Syntax);
                    includeSemantic = includeSemantic && analyzer.SupportAnalysisKind(AnalysisKind.Semantic) && document is Document;

                    if (includeSyntax || includeSemantic)
                    {
                        if (includeSyntax)
                        {
                            syntaxAnalyzers.Add(analyzer);
                        }

                        if (includeSemantic)
                        {
                            if (!incrementalAnalysis)
                            {
                                // For non-incremental analysis, we always attempt to compute all
                                // analyzer diagnostics for the requested span.
                                semanticSpanBasedAnalyzers.Add(analyzer);
                            }
                            else if (analyzer.SupportsSpanBasedSemanticDiagnosticAnalysis())
                            {
                                // We can perform incremental analysis only for analyzers that support
                                // span-based semantic diagnostic analysis.
                                semanticSpanBasedAnalyzers.Add(analyzer);
                            }
                            else
                            {
                                semanticDocumentBasedAnalyzers.Add(analyzer);
                            }
                        }
                    }
                }

                return (
                    syntaxAnalyzers.ToImmutableAndClear(),
                    semanticSpanBasedAnalyzers.ToImmutableAndClear(),
                    semanticDocumentBasedAnalyzers.ToImmutableAndClear());
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        async ValueTask<bool> ShouldIncludeAnalyzerAsync(DiagnosticAnalyzer analyzer)
        {
            // Skip executing analyzer if its priority does not match the request priority.
            if (!await MatchesPriorityAsync(analyzer).ConfigureAwait(false))
                return false;

            // Special case DocumentDiagnosticAnalyzer to never skip these document analyzers based on
            // 'shouldIncludeDiagnostic' predicate. More specifically, TS has special document analyzer which report
            // 0 supported diagnostics, but we always want to execute it.  This also applies to our special built in
            // analyzers 'FileContentLoadAnalyzer' and 'GeneratorDiagnosticsPlaceholderAnalyzer'.
            if (analyzer is DocumentDiagnosticAnalyzer)
                return true;

            // Skip analyzer if none of its reported diagnostics should be included.
            if (diagnosticIdFilter != DiagnosticIdFilter.All)
            {
                var descriptors = _analyzerInfoCache.GetDiagnosticDescriptors(analyzer);
                return diagnosticIdFilter.Allow(descriptors.Select(d => d.Id));
            }

            return true;
        }

        // <summary>
        // Returns true if the given <paramref name="analyzer"/> can report diagnostics that can have fixes from a code
        // fix provider with <see cref="CodeFixProvider.RequestPriority"/> matching <see
        // cref="ICodeActionRequestPriorityProvider.Priority"/>. This method is useful for performing a performance
        // optimization for lightbulb diagnostic computation, wherein we can reduce the set of analyzers to be executed
        // when computing fixes for a specific <see cref="ICodeActionRequestPriorityProvider.Priority"/>.
        // </summary>
        async Task<bool> MatchesPriorityAsync(DiagnosticAnalyzer analyzer)
        {
            // If caller isn't asking for prioritized result, then run all analyzers.
            if (priority is null)
                return true;

            // 'CodeActionRequestPriority.Lowest' is used for suppression/configuration fixes,
            // which requires all analyzer diagnostics.
            if (priority == CodeActionRequestPriority.Lowest)
                return true;

            // The compiler analyzer always counts for any priority.  It's diagnostics may be fixed
            // by high pri or normal pri fixers.
            if (analyzer.IsCompilerAnalyzer())
                return true;

            // Check if we are computing diagnostics for 'CodeActionRequestPriority.Low' and
            // this analyzer was de-prioritized to low priority bucket.
            if (priority == CodeActionRequestPriority.Low &&
                await this.IsDeprioritizedAnalyzerAsync(project, analyzer, cancellationToken).ConfigureAwait(false))
            {
                return true;
            }

            // Now compute this analyzer's priority and compare it with the provider's request 'Priority'.
            // Our internal 'IBuiltInAnalyzer' can specify custom request priority, while all
            // the third-party analyzers are assigned 'Medium' priority.
            var analyzerPriority = analyzer is IBuiltInAnalyzer { IsHighPriority: true }
                ? CodeActionRequestPriority.High
                : CodeActionRequestPriority.Default;

            return priority == analyzerPriority;
        }

        async Task<ImmutableArray<DiagnosticAnalyzer>> FilterAnalyzersAsync(
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AnalysisKind kind,
            TextSpan? span)
        {
            using var _1 = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(analyzers.Length, out var filteredAnalyzers);

            foreach (var analyzer in analyzers)
            {
                // Check if this is an expensive analyzer that needs to be de-prioritized to a lower priority bucket.
                // If so, we skip this analyzer from execution in the current priority bucket.
                // We will subsequently execute this analyzer in the lower priority bucket.
                if (await ShouldDeprioritizeAnalyzerAsync(analyzer, kind, span).ConfigureAwait(false))
                    continue;

                filteredAnalyzers.Add(analyzer);
            }

            return filteredAnalyzers.ToImmutableAndClear();
        }

        async ValueTask<bool> ShouldDeprioritizeAnalyzerAsync(
            DiagnosticAnalyzer analyzer, AnalysisKind kind, TextSpan? span)
        {
            // PERF: In order to improve lightbulb performance, we perform de-prioritization optimization for certain analyzers
            // that moves the analyzer to a lower priority bucket. However, to ensure that de-prioritization happens for very rare cases,
            // we only perform this optimizations when following conditions are met:
            //  1. We are performing semantic span-based analysis.
            //  2. We are processing 'CodeActionRequestPriority.Normal' priority request.
            //  3. Analyzer registers certain actions that are known to lead to high performance impact due to its broad analysis scope,
            //     such as SymbolStart/End actions and SemanticModel actions.

            // Conditions 1. and 2.
            if (kind != AnalysisKind.Semantic ||
                !span.HasValue ||
                priority != CodeActionRequestPriority.Default)
            {
                return false;
            }

            // Condition 3.
            // Check if this is a candidate analyzer that can be de-prioritized into a lower priority bucket based on registered actions.
            return await this.IsDeprioritizedAnalyzerAsync(project, analyzer, cancellationToken).ConfigureAwait(false);
        }

        bool ShouldInclude(DiagnosticData diagnostic)
        {
            if (diagnostic.DocumentId != document.Id)
                return false;

            if (range != null && !range.Value.IntersectsWith(diagnostic.DataLocation.UnmappedFileSpan.GetClampedTextSpan(text)))
                return false;

            return diagnosticIdFilter.Allow(diagnostic.Id);
        }
    }

    private async Task<ImmutableArray<DiagnosticData>> ComputeDiagnosticsInProcessAsync(
        TextDocument document,
        TextSpan? range,
        ImmutableArray<DiagnosticAnalyzer> allAnalyzers,
        ImmutableArray<DiagnosticAnalyzer> syntaxAnalyzers,
        ImmutableArray<DiagnosticAnalyzer> semanticSpanAnalyzers,
        ImmutableArray<DiagnosticAnalyzer> semanticDocumentAnalyzers,
        bool incrementalAnalysis,
        bool logPerformanceInfo,
        CancellationToken cancellationToken)
    {
        // We log performance info when we are computing diagnostics for a span
        var project = document.Project;

        var hostAnalyzerInfo = GetOrCreateHostAnalyzerInfo_OnlyCallInProcess(project);
        var compilationWithAnalyzers = await GetOrCreateCompilationWithAnalyzers_OnlyCallInProcessAsync(
            document.Project, allAnalyzers, hostAnalyzerInfo, this.CrashOnAnalyzerException, cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var list);

        await ComputeDocumentDiagnosticsAsync(syntaxAnalyzers, AnalysisKind.Syntax, range, incrementalAnalysis: false).ConfigureAwait(false);
        await ComputeDocumentDiagnosticsAsync(semanticSpanAnalyzers, AnalysisKind.Semantic, range, incrementalAnalysis).ConfigureAwait(false);
        await ComputeDocumentDiagnosticsAsync(semanticDocumentAnalyzers, AnalysisKind.Semantic, span: null, incrementalAnalysis: false).ConfigureAwait(false);

        return list.ToImmutableAndClear();

        async Task ComputeDocumentDiagnosticsAsync(
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AnalysisKind kind,
            TextSpan? span,
            bool incrementalAnalysis)
        {
            if (analyzers.Length == 0)
                return;

            Debug.Assert(!incrementalAnalysis || kind == AnalysisKind.Semantic);
            Debug.Assert(!incrementalAnalysis || analyzers.All(analyzer => analyzer.SupportsSpanBasedSemanticDiagnosticAnalysis()));

            var analysisScope = new DocumentAnalysisScope(document, span, analyzers, kind);
            var executor = new DocumentAnalysisExecutor(this, analysisScope, compilationWithAnalyzers, logPerformanceInfo);
            var version = await GetDiagnosticVersionAsync(document.Project, cancellationToken).ConfigureAwait(false);

            var computeTask = incrementalAnalysis
                ? _incrementalMemberEditAnalyzer.ComputeDiagnosticsInProcessAsync(executor, analyzers, version, cancellationToken)
                : ComputeDocumentDiagnosticsCoreInProcessAsync(executor, cancellationToken);
            var diagnosticsMap = await computeTask.ConfigureAwait(false);

            if (incrementalAnalysis)
                _incrementalMemberEditAnalyzer.UpdateDocumentWithCachedDiagnostics((Document)document);

            list.AddRange(diagnosticsMap.SelectMany(kvp => kvp.Value));
        }
    }
}
