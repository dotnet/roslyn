// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        public async Task<bool> TryAppendDiagnosticsForSpanAsync(Document document, TextSpan range, List<DiagnosticData> result, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            var blockForData = false;
            var getter = await LatestDiagnosticsForSpanGetter.CreateAsync(this, document, range, blockForData, includeSuppressedDiagnostics, diagnosticIdOpt: null, cancellationToken).ConfigureAwait(false);
            return await getter.TryGetAsync(result, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<DiagnosticData>> GetDiagnosticsForSpanAsync(Document document, TextSpan range, bool includeSuppressedDiagnostics = false, string diagnosticIdOpt = null, CancellationToken cancellationToken = default)
        {
            var blockForData = true;
            var getter = await LatestDiagnosticsForSpanGetter.CreateAsync(this, document, range, blockForData, includeSuppressedDiagnostics, diagnosticIdOpt, cancellationToken).ConfigureAwait(false);

            var list = new List<DiagnosticData>();
            var result = await getter.TryGetAsync(list, cancellationToken).ConfigureAwait(false);
            Debug.Assert(result);

            return list;
        }

        /// <summary>
        /// Get diagnostics for given span either by using cache or calculating it on the spot.
        /// </summary>
        private class LatestDiagnosticsForSpanGetter
        {
            private readonly DiagnosticIncrementalAnalyzer _owner;
            private readonly Project _project;
            private readonly Document _document;

            private readonly IEnumerable<StateSet> _stateSets;
            private readonly CompilationWithAnalyzers _analyzerDriverOpt;
            private readonly DiagnosticAnalyzer _compilerAnalyzer;

            private readonly TextSpan _range;
            private readonly bool _blockForData;
            private readonly bool _includeSuppressedDiagnostics;
            private readonly string _diagnosticId;

            // cache of project result
            private ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> _projectResultCache;

            private delegate Task<IEnumerable<DiagnosticData>> DiagnosticsGetterAsync(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken);

            public static async Task<LatestDiagnosticsForSpanGetter> CreateAsync(
                 DiagnosticIncrementalAnalyzer owner, Document document, TextSpan range, bool blockForData, bool includeSuppressedDiagnostics = false,
                 string diagnosticIdOpt = null, CancellationToken cancellationToken = default)
            {
                // REVIEW: IsAnalyzerSuppressed can be quite expensive in some cases. try to find a way to make it cheaper
                //         Here we don't filter out hidden diagnostic only analyzer since such analyzer can produce hidden diagnostic
                //         on active file (non local diagnostic)
                var stateSets = owner._stateManager
                                     .GetOrCreateStateSets(document.Project).Where(s => !owner.Owner.IsAnalyzerSuppressed(s.Analyzer, document.Project));

                // filter to specific diagnostic it is looking for
                if (diagnosticIdOpt != null)
                {
                    stateSets = stateSets.Where(s => owner.Owner.GetDiagnosticDescriptors(s.Analyzer).Any(d => d.Id == diagnosticIdOpt)).ToList();
                }

                var analyzerDriverOpt = await owner._compilationManager.CreateAnalyzerDriverAsync(document.Project, stateSets, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

                return new LatestDiagnosticsForSpanGetter(owner, analyzerDriverOpt, document, stateSets, diagnosticIdOpt, range, blockForData, includeSuppressedDiagnostics);
            }

            private LatestDiagnosticsForSpanGetter(
                DiagnosticIncrementalAnalyzer owner,
                CompilationWithAnalyzers analyzerDriverOpt,
                Document document,
                IEnumerable<StateSet> stateSets,
                string diagnosticId,
                TextSpan range, bool blockForData, bool includeSuppressedDiagnostics)
            {
                _owner = owner;

                _project = document.Project;
                _document = document;

                _stateSets = stateSets;
                _diagnosticId = diagnosticId;
                _analyzerDriverOpt = analyzerDriverOpt;
                _compilerAnalyzer = _owner.HostAnalyzerManager.GetCompilerDiagnosticAnalyzer(_document.Project.Language);

                _range = range;
                _blockForData = blockForData;
                _includeSuppressedDiagnostics = includeSuppressedDiagnostics;
            }

            public async Task<bool> TryGetAsync(List<DiagnosticData> list, CancellationToken cancellationToken)
            {
                try
                {
                    var containsFullResult = true;
                    foreach (var stateSet in _stateSets)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        containsFullResult &= await TryGetSyntaxAndSemanticDiagnosticsAsync(stateSet, list, cancellationToken).ConfigureAwait(false);

                        // check whether compilation end code fix is enabled
                        if (!_document.Project.Solution.Workspace.Options.GetOption(InternalDiagnosticsOptions.CompilationEndCodeFix))
                        {
                            continue;
                        }

                        // check whether heuristic is enabled
                        if (_blockForData && _document.Project.Solution.Workspace.Options.GetOption(InternalDiagnosticsOptions.UseCompilationEndCodeFixHeuristic))
                        {
                            var avoidLoadingData = true;
                            var state = stateSet.GetProjectState(_project.Id);
                            var result = await state.GetAnalysisDataAsync(_document, avoidLoadingData, cancellationToken).ConfigureAwait(false);

                            // no previous compilation end diagnostics in this file.
                            var version = await GetDiagnosticVersionAsync(_project, cancellationToken).ConfigureAwait(false);
                            if (state.IsEmpty(_document.Id) || result.Version != version)
                            {
                                continue;
                            }
                        }

                        containsFullResult &= await TryGetProjectDiagnosticsAsync(stateSet, GetProjectDiagnosticsAsync, list, cancellationToken).ConfigureAwait(false);
                    }

                    // if we are blocked for data, then we should always have full result.
                    Debug.Assert(!_blockForData || containsFullResult);
                    return containsFullResult;
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private async Task<bool> TryGetSyntaxAndSemanticDiagnosticsAsync(StateSet stateSet, List<DiagnosticData> list, CancellationToken cancellationToken)
            {
                // unfortunately, we need to special case compiler diagnostic analyzer so that
                // we can do span based analysis even though we implemented it as semantic model analysis
                if (stateSet.Analyzer == _compilerAnalyzer)
                {
                    return await TryGetSyntaxAndSemanticCompilerDiagnostics(stateSet, list, cancellationToken).ConfigureAwait(false);
                }

                var fullResult = true;
                fullResult &= await TryGetDocumentDiagnosticsAsync(stateSet, AnalysisKind.Syntax, GetSyntaxDiagnosticsAsync, list, cancellationToken).ConfigureAwait(false);
                fullResult &= await TryGetDocumentDiagnosticsAsync(stateSet, AnalysisKind.Semantic, GetSemanticDiagnosticsAsync, list, cancellationToken).ConfigureAwait(false);

                return fullResult;
            }

            private async Task<bool> TryGetSyntaxAndSemanticCompilerDiagnostics(StateSet stateSet, List<DiagnosticData> list, CancellationToken cancellationToken)
            {
                // First, get syntax errors and semantic errors
                var fullResult = true;
                fullResult &= await TryGetDocumentDiagnosticsAsync(stateSet, AnalysisKind.Syntax, GetCompilerSyntaxDiagnosticsAsync, list, cancellationToken).ConfigureAwait(false);
                fullResult &= await TryGetDocumentDiagnosticsAsync(stateSet, AnalysisKind.Semantic, GetCompilerSemanticDiagnosticsAsync, list, cancellationToken).ConfigureAwait(false);

                return fullResult;
            }

            private async Task<IEnumerable<DiagnosticData>> GetCompilerSyntaxDiagnosticsAsync(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
            {
                var root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var diagnostics = root.GetDiagnostics();

                return diagnostics.ConvertToLocalDiagnostics(_document, _range);
            }

            private async Task<IEnumerable<DiagnosticData>> GetCompilerSemanticDiagnosticsAsync(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
            {
                var model = await _document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                VerifyDiagnostics(model);

                var root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var adjustedSpan = AdjustSpan(_document, root, _range);
                var diagnostics = model.GetDeclarationDiagnostics(adjustedSpan, cancellationToken).Concat(model.GetMethodBodyDiagnostics(adjustedSpan, cancellationToken));

                return diagnostics.ConvertToLocalDiagnostics(_document, _range);
            }

            private Task<IEnumerable<DiagnosticData>> GetSyntaxDiagnosticsAsync(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
            {
                return _owner._executor.ComputeDiagnosticsAsync(_analyzerDriverOpt, _document, analyzer, AnalysisKind.Syntax, _range, cancellationToken);
            }

            private Task<IEnumerable<DiagnosticData>> GetSemanticDiagnosticsAsync(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
            {
                var supportsSemanticInSpan = analyzer.SupportsSpanBasedSemanticDiagnosticAnalysis();

                var analysisSpan = supportsSemanticInSpan ? (TextSpan?)_range : null;
                return _owner._executor.ComputeDiagnosticsAsync(_analyzerDriverOpt, _document, analyzer, AnalysisKind.Semantic, analysisSpan, cancellationToken);
            }

            private async Task<IEnumerable<DiagnosticData>> GetProjectDiagnosticsAsync(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
            {
                if (_projectResultCache == null)
                {
                    // execute whole project as one shot and cache the result.
                    var forceAnalyzerRun = true;
                    var analysisResult = await _owner._executor.GetProjectAnalysisDataAsync(_analyzerDriverOpt, _project, _stateSets, forceAnalyzerRun, cancellationToken).ConfigureAwait(false);

                    _projectResultCache = analysisResult.Result;
                }

                if (!_projectResultCache.TryGetValue(analyzer, out var result))
                {
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                return GetResult(result, AnalysisKind.NonLocal, _document.Id);
            }

            [Conditional("DEBUG")]
            private void VerifyDiagnostics(SemanticModel model)
            {
#if DEBUG
                // Exclude unused import diagnostics since they are never reported when a span is passed.
                // (See CSharp/VisualBasicCompilation.GetDiagnosticsForMethodBodiesInTree.)
                bool shouldInclude(Diagnostic d) => _range.IntersectsWith(d.Location.SourceSpan) && !IsUnusedImportDiagnostic(d);

                // make sure what we got from range is same as what we got from whole diagnostics
                var rangeDeclaractionDiagnostics = model.GetDeclarationDiagnostics(_range).ToArray();
                var rangeMethodBodyDiagnostics = model.GetMethodBodyDiagnostics(_range).ToArray();
                var rangeDiagnostics = rangeDeclaractionDiagnostics.Concat(rangeMethodBodyDiagnostics).Where(shouldInclude).ToArray();

                var wholeDeclarationDiagnostics = model.GetDeclarationDiagnostics().ToArray();
                var wholeMethodBodyDiagnostics = model.GetMethodBodyDiagnostics().ToArray();
                var wholeDiagnostics = wholeDeclarationDiagnostics.Concat(wholeMethodBodyDiagnostics).Where(shouldInclude).ToArray();

                if (!AreEquivalent(rangeDiagnostics, wholeDiagnostics))
                {
                    // otherwise, report non-fatal watson so that we can fix those cases
                    FatalError.ReportWithoutCrash(new Exception("Bug in GetDiagnostics"));

                    // make sure we hold onto these for debugging.
                    GC.KeepAlive(rangeDeclaractionDiagnostics);
                    GC.KeepAlive(rangeMethodBodyDiagnostics);
                    GC.KeepAlive(rangeDiagnostics);
                    GC.KeepAlive(wholeDeclarationDiagnostics);
                    GC.KeepAlive(wholeMethodBodyDiagnostics);
                    GC.KeepAlive(wholeDiagnostics);
                }
#endif
            }

            private static bool IsUnusedImportDiagnostic(Diagnostic d)
            {
                switch (d.Id)
                {
                    case "CS8019":
                    case "BC50000":
                    case "BC50001":
                        return true;
                    default:
                        return false;
                }
            }

            private static TextSpan AdjustSpan(Document document, SyntaxNode root, TextSpan span)
            {
                // this is to workaround a bug (https://github.com/dotnet/roslyn/issues/1557)
                // once that bug is fixed, we should be able to use given span as it is.

                var service = document.GetLanguageService<ISyntaxFactsService>();
                var startNode = service.GetContainingMemberDeclaration(root, span.Start);
                var endNode = service.GetContainingMemberDeclaration(root, span.End);

                if (startNode == endNode)
                {
                    // use full member span
                    if (service.IsMethodLevelMember(startNode))
                    {
                        return startNode.FullSpan;
                    }

                    // use span as it is
                    return span;
                }

                var startSpan = service.IsMethodLevelMember(startNode) ? startNode.FullSpan : span;
                var endSpan = service.IsMethodLevelMember(endNode) ? endNode.FullSpan : span;

                return TextSpan.FromBounds(Math.Min(startSpan.Start, endSpan.Start), Math.Max(startSpan.End, endSpan.End));
            }

            private async Task<bool> TryGetDocumentDiagnosticsAsync(
                StateSet stateSet,
                AnalysisKind kind,
                DiagnosticsGetterAsync diagnosticGetterAsync,
                List<DiagnosticData> list,
                CancellationToken cancellationToken)
            {
                if (!_owner.Owner.SupportAnalysisKind(stateSet.Analyzer, stateSet.Language, kind))
                {
                    return true;
                }

                // make sure we get state even when none of our analyzer has ran yet.
                // but this shouldn't create analyzer that doesn't belong to this project (language)
                var state = stateSet.GetActiveFileState(_document.Id);

                // see whether we can use existing info
                var existingData = state.GetAnalysisData(kind);
                var version = await GetDiagnosticVersionAsync(_document.Project, cancellationToken).ConfigureAwait(false);
                if (existingData.Version == version)
                {
                    if (existingData.Items.IsEmpty)
                    {
                        return true;
                    }

                    list.AddRange(existingData.Items.Where(ShouldInclude));
                    return true;
                }

                cancellationToken.ThrowIfCancellationRequested();

                // check whether we want up-to-date document wide diagnostics
                var supportsSemanticInSpan = stateSet.Analyzer.SupportsSpanBasedSemanticDiagnosticAnalysis();
                if (!BlockForData(kind, supportsSemanticInSpan))
                {
                    return false;
                }

                var dx = await diagnosticGetterAsync(stateSet.Analyzer, cancellationToken).ConfigureAwait(false);
                if (dx != null)
                {
                    // no state yet
                    list.AddRange(dx.Where(ShouldInclude));
                }

                return true;
            }

            private async Task<bool> TryGetProjectDiagnosticsAsync(
                StateSet stateSet,
                DiagnosticsGetterAsync diagnosticGetterAsync,
                List<DiagnosticData> list,
                CancellationToken cancellationToken)
            {
                if (!stateSet.Analyzer.SupportsProjectDiagnosticAnalysis())
                {
                    return true;
                }

                // make sure we get state even when none of our analyzer has ran yet.
                // but this shouldn't create analyzer that doesn't belong to this project (language)
                var state = stateSet.GetProjectState(_document.Project.Id);

                // see whether we can use existing info
                var result = await state.GetAnalysisDataAsync(_document, avoidLoadingData: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                var version = await GetDiagnosticVersionAsync(_document.Project, cancellationToken).ConfigureAwait(false);
                if (result.Version == version)
                {
                    var existingData = GetResult(result, AnalysisKind.NonLocal, _document.Id);
                    if (existingData.IsEmpty)
                    {
                        return true;
                    }

                    list.AddRange(existingData.Where(ShouldInclude));
                    return true;
                }

                cancellationToken.ThrowIfCancellationRequested();

                // check whether we want up-to-date document wide diagnostics
                var supportsSemanticInSpan = stateSet.Analyzer.SupportsSpanBasedSemanticDiagnosticAnalysis();
                if (!BlockForData(AnalysisKind.NonLocal, supportsSemanticInSpan))
                {
                    return false;
                }

                var dx = await diagnosticGetterAsync(stateSet.Analyzer, cancellationToken).ConfigureAwait(false);
                if (dx != null)
                {
                    // no state yet
                    list.AddRange(dx.Where(ShouldInclude));
                }

                return true;
            }

            private bool ShouldInclude(DiagnosticData diagnostic)
            {
                return diagnostic.DocumentId == _document.Id && _range.IntersectsWith(diagnostic.TextSpan)
                    && (_includeSuppressedDiagnostics || !diagnostic.IsSuppressed)
                    && (_diagnosticId == null || _diagnosticId == diagnostic.Id);
            }

            private bool BlockForData(AnalysisKind kind, bool supportsSemanticInSpan)
            {
                if (kind == AnalysisKind.Semantic && !supportsSemanticInSpan && !_blockForData)
                {
                    return false;
                }

                if (kind == AnalysisKind.NonLocal && !_blockForData)
                {
                    return false;
                }

                return true;
            }
        }

#if DEBUG
        internal static bool AreEquivalent(Diagnostic[] diagnosticsA, Diagnostic[] diagnosticsB)
        {
            var set = new HashSet<Diagnostic>(diagnosticsA, DiagnosticComparer.Instance);
            return set.SetEquals(diagnosticsB);
        }

        private sealed class DiagnosticComparer : IEqualityComparer<Diagnostic>
        {
            internal static readonly DiagnosticComparer Instance = new DiagnosticComparer();

            public bool Equals(Diagnostic x, Diagnostic y)
            {
                return x.Id == y.Id && x.Location == y.Location;
            }

            public int GetHashCode(Diagnostic obj)
            {
                return Hash.Combine(obj.Id.GetHashCode(), obj.Location.GetHashCode());
            }
        }
#endif
    }
}
