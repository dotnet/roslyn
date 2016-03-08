// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    internal partial class DiagnosticIncrementalAnalyzer : BaseDiagnosticIncrementalAnalyzer
    {
        private class LatestDiagnosticsForSpanGetter
        {
            private readonly DiagnosticIncrementalAnalyzer _owner;

            private readonly Document _document;
            private readonly DiagnosticAnalyzer _compilerAnalyzer;

            private readonly TextSpan _range;
            private readonly bool _blockForData;
            private readonly bool _includeSuppressedDiagnostics;
            private readonly CancellationToken _cancellationToken;

            private readonly DiagnosticAnalyzerDriver _spanBasedDriver;
            private readonly DiagnosticAnalyzerDriver _documentBasedDriver;
            private readonly DiagnosticAnalyzerDriver _projectDriver;

            public LatestDiagnosticsForSpanGetter(
                DiagnosticIncrementalAnalyzer owner, Document document, SyntaxNode root, TextSpan range, bool blockForData, bool includeSuppressedDiagnostics, CancellationToken cancellationToken) :
                this(owner, document, root, range, blockForData, new List<DiagnosticData>(), includeSuppressedDiagnostics, cancellationToken)
            {
            }

            public LatestDiagnosticsForSpanGetter(
                DiagnosticIncrementalAnalyzer owner, Document document, SyntaxNode root, TextSpan range, bool blockForData, List<DiagnosticData> diagnostics, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
            {
                _owner = owner;

                _document = document;
                _compilerAnalyzer = _owner.HostAnalyzerManager.GetCompilerDiagnosticAnalyzer(_document.Project.Language);

                _range = range;
                _blockForData = blockForData;
                _includeSuppressedDiagnostics = includeSuppressedDiagnostics;
                _cancellationToken = cancellationToken;

                Diagnostics = diagnostics;

                // Share the diagnostic analyzer driver across all analyzers.
                var fullSpan = root?.FullSpan;

                // We are computing diagnostics for a single document/span, so we don't need to enable concurrent analysis.
                const bool concurrentAnalysis = false;
                const bool reportSuppressedDiagnostics = true;

                _spanBasedDriver = new DiagnosticAnalyzerDriver(_document, _range, root, _owner, concurrentAnalysis, reportSuppressedDiagnostics, _cancellationToken);
                _documentBasedDriver = new DiagnosticAnalyzerDriver(_document, fullSpan, root, _owner, concurrentAnalysis, reportSuppressedDiagnostics, _cancellationToken);
                _projectDriver = new DiagnosticAnalyzerDriver(_document.Project, _owner, concurrentAnalysis, reportSuppressedDiagnostics, _cancellationToken);
            }

            public List<DiagnosticData> Diagnostics { get; }

            public async Task<bool> TryGetAsync()
            {
                try
                {
                    var textVersion = await _document.GetTextVersionAsync(_cancellationToken).ConfigureAwait(false);
                    var syntaxVersion = await _document.GetSyntaxVersionAsync(_cancellationToken).ConfigureAwait(false);
                    var projectTextVersion = await _document.Project.GetLatestDocumentVersionAsync(_cancellationToken).ConfigureAwait(false);
                    var semanticVersion = await _document.Project.GetDependentSemanticVersionAsync(_cancellationToken).ConfigureAwait(false);

                    var containsFullResult = true;
                    foreach (var stateSet in _owner._stateManager.GetOrCreateStateSets(_document.Project))
                    {
                        containsFullResult &= await TryGetSyntaxAndSemanticDiagnosticsAsync(stateSet, textVersion, syntaxVersion, semanticVersion).ConfigureAwait(false);

                        // check whether compilation end code fix is enabled
                        if (!_document.Project.Solution.Workspace.Options.GetOption(InternalDiagnosticsOptions.CompilationEndCodeFix))
                        {
                            continue;
                        }

                        // check whether heuristic is enabled
                        if (_blockForData && _document.Project.Solution.Workspace.Options.GetOption(InternalDiagnosticsOptions.UseCompilationEndCodeFixHeuristic))
                        {
                            var analysisData = await stateSet.GetState(StateType.Project).TryGetExistingDataAsync(_document, _cancellationToken).ConfigureAwait(false);

                            // no previous compilation end diagnostics in this file.
                            if (analysisData == null || analysisData.Items.Length == 0 ||
                                !analysisData.TextVersion.Equals(projectTextVersion) ||
                                !analysisData.DataVersion.Equals(semanticVersion))
                            {
                                continue;
                            }
                        }

                        containsFullResult &= await TryGetDocumentDiagnosticsAsync(
                            stateSet, StateType.Project, (t, d) => t.Equals(projectTextVersion) && d.Equals(semanticVersion), GetProjectDiagnosticsAsync).ConfigureAwait(false);
                    }

                    // if we are blocked for data, then we should always have full result.
                    Contract.Requires(!_blockForData || containsFullResult);
                    return containsFullResult;
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private async Task<bool> TryGetSyntaxAndSemanticDiagnosticsAsync(StateSet stateSet, VersionStamp textVersion, VersionStamp syntaxVersion, VersionStamp semanticVersion)
            {
                // unfortunately, we need to special case compiler diagnostic analyzer so that 
                // we can do span based analysis even though we implemented it as semantic model analysis
                if (stateSet.Analyzer == _compilerAnalyzer)
                {
                    return await TryGetSyntaxAndSemanticCompilerDiagnostics(stateSet, textVersion, syntaxVersion, semanticVersion).ConfigureAwait(false);
                }

                var containsFullResult = await TryGetDocumentDiagnosticsAsync(
                                            stateSet, StateType.Syntax, (t, d) => t.Equals(textVersion) && d.Equals(syntaxVersion), GetSyntaxDiagnosticsAsync).ConfigureAwait(false);

                containsFullResult &= await TryGetDocumentDiagnosticsAsync(
                    stateSet, StateType.Document, (t, d) => t.Equals(textVersion) && d.Equals(semanticVersion), GetSemanticDiagnosticsAsync).ConfigureAwait(false);

                return containsFullResult;
            }

            private async Task<bool> TryGetSyntaxAndSemanticCompilerDiagnostics(StateSet stateSet, VersionStamp textVersion, VersionStamp syntaxVersion, VersionStamp semanticVersion)
            {
                // First, get syntax errors
                var containsFullResult = await TryGetDocumentDiagnosticsAsync(
                                            stateSet, StateType.Syntax, true,
                                            (t, d) => t.Equals(textVersion) && d.Equals(syntaxVersion),
                                            async (_1, _2) =>
                                            {
                                                var root = await _document.GetSyntaxRootAsync(_cancellationToken).ConfigureAwait(false);
                                                var diagnostics = root.GetDiagnostics();

                                                return GetDiagnosticData(_document, root.SyntaxTree, _range, diagnostics);
                                            }).ConfigureAwait(false);

                // second get semantic errors
                containsFullResult &= await TryGetDocumentDiagnosticsAsync(
                    stateSet, StateType.Document, true,
                    (t, d) => t.Equals(textVersion) && d.Equals(semanticVersion),
                    async (_1, _2) =>
                    {
                        var model = await _document.GetSemanticModelAsync(_cancellationToken).ConfigureAwait(false);
                        VerifyDiagnostics(model);

                        var root = await _document.GetSyntaxRootAsync(_cancellationToken).ConfigureAwait(false);
                        var adjustedSpan = AdjustSpan(_document, root, _range);
                        var diagnostics = model.GetDeclarationDiagnostics(adjustedSpan, _cancellationToken).Concat(model.GetMethodBodyDiagnostics(adjustedSpan, _cancellationToken));

                        return GetDiagnosticData(_document, model.SyntaxTree, _range, diagnostics);
                    }).ConfigureAwait(false);

                return containsFullResult;
            }

            [Conditional("DEBUG")]
            private void VerifyDiagnostics(SemanticModel model)
            {
#if DEBUG
                // Exclude unused import diagnostics since they are never reported when a span is passed.
                // (See CSharp/VisualBasicCompilation.GetDiagnosticsForMethodBodiesInTree.)
                Func<Diagnostic, bool> shouldInclude = d => _range.IntersectsWith(d.Location.SourceSpan) && !IsUnusedImportDiagnostic(d);

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
                StateSet stateSet, StateType stateType, Func<VersionStamp, VersionStamp, bool> versionCheck,
                Func<DiagnosticAnalyzerDriver, DiagnosticAnalyzer, Task<IEnumerable<DiagnosticData>>> getDiagnostics)
            {
                if (_owner.Owner.IsAnalyzerSuppressed(stateSet.Analyzer, _document.Project) ||
                    !ShouldRunAnalyzerForStateType(stateSet, stateType))
                {
                    return true;
                }

                bool supportsSemanticInSpan = stateSet.Analyzer.SupportsSpanBasedSemanticDiagnosticAnalysis();
                var analyzerDriver = GetAnalyzerDriverBasedOnStateType(stateType, supportsSemanticInSpan);

                return await TryGetDocumentDiagnosticsAsync(stateSet, stateType, supportsSemanticInSpan, versionCheck, getDiagnostics, analyzerDriver).ConfigureAwait(false);
            }

            private async Task<bool> TryGetDocumentDiagnosticsAsync(
                StateSet stateSet, StateType stateType, bool supportsSemanticInSpan,
                Func<VersionStamp, VersionStamp, bool> versionCheck,
                Func<DiagnosticAnalyzerDriver, DiagnosticAnalyzer, Task<IEnumerable<DiagnosticData>>> getDiagnostics,
                DiagnosticAnalyzerDriver analyzerDriverOpt = null)
            {
                Func<DiagnosticData, bool> shouldInclude = d =>
                    d.DocumentId == _document.Id &&
                    _range.IntersectsWith(d.TextSpan) &&
                    (_includeSuppressedDiagnostics || !d.IsSuppressed);

                // make sure we get state even when none of our analyzer has ran yet. 
                // but this shouldn't create analyzer that doesn't belong to this project (language)
                var state = stateSet.GetState(stateType);

                // see whether we can use existing info
                var existingData = await state.TryGetExistingDataAsync(_document, _cancellationToken).ConfigureAwait(false);
                if (existingData != null && versionCheck(existingData.TextVersion, existingData.DataVersion))
                {
                    if (existingData.Items == null || existingData.Items.Length == 0)
                    {
                        return true;
                    }

                    Diagnostics.AddRange(existingData.Items.Where(shouldInclude));
                    return true;
                }

                // check whether we want up-to-date document wide diagnostics
                if (!BlockForData(stateType, supportsSemanticInSpan))
                {
                    return false;
                }

                var dx = await getDiagnostics(analyzerDriverOpt, stateSet.Analyzer).ConfigureAwait(false);
                if (dx != null)
                {
                    // no state yet
                    Diagnostics.AddRange(dx.Where(shouldInclude));
                }

                return true;
            }

            private bool ShouldRunAnalyzerForStateType(StateSet stateSet, StateType stateType)
            {
                return DiagnosticIncrementalAnalyzer.ShouldRunAnalyzerForStateType(stateSet.Analyzer, stateType);
            }

            private bool BlockForData(StateType stateType, bool supportsSemanticInSpan)
            {
                if (stateType == StateType.Document && !supportsSemanticInSpan && !_blockForData)
                {
                    return false;
                }

                if (stateType == StateType.Project && !_blockForData)
                {
                    return false;
                }

                // TODO:
                // this probably need to change in v2 engine. but in v1 engine, we have assumption that all syntax related action
                // will return diagnostics that only belong to given span
                return true;
            }

            private DiagnosticAnalyzerDriver GetAnalyzerDriverBasedOnStateType(StateType stateType, bool supportsSemanticInSpan)
            {
                return stateType == StateType.Project ? _projectDriver : supportsSemanticInSpan ? _spanBasedDriver : _documentBasedDriver;
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
