// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    internal partial class DiagnosticIncrementalAnalyzer : BaseDiagnosticIncrementalAnalyzer
    {
        private class LatestDiagnosticsForSpanGetter
        {
            private readonly DiagnosticIncrementalAnalyzer _owner;

            private readonly Document _document;
            private readonly TextSpan _range;
            private readonly bool _blockForData;
            private readonly CancellationToken _cancellationToken;

            private readonly DiagnosticAnalyzerDriver _spanBasedDriver;
            private readonly DiagnosticAnalyzerDriver _documentBasedDriver;
            private readonly DiagnosticAnalyzerDriver _projectDriver;

            public LatestDiagnosticsForSpanGetter(
                DiagnosticIncrementalAnalyzer owner, Document document, SyntaxNode root, TextSpan range, bool blockForData, CancellationToken cancellationToken) :
                this(owner, document, root, range, blockForData, new List<DiagnosticData>(), cancellationToken)
            {
            }

            public LatestDiagnosticsForSpanGetter(
                DiagnosticIncrementalAnalyzer owner, Document document, SyntaxNode root, TextSpan range, bool blockForData, List<DiagnosticData> diagnostics, CancellationToken cancellationToken)
            {
                _owner = owner;

                _document = document;
                _range = range;
                _blockForData = blockForData;
                _cancellationToken = cancellationToken;

                Diagnostics = diagnostics;

                // Share the diagnostic analyzer driver across all analyzers.
                var fullSpan = root?.FullSpan;

                _spanBasedDriver = new DiagnosticAnalyzerDriver(_document, _range, root, _owner, _cancellationToken);
                _documentBasedDriver = new DiagnosticAnalyzerDriver(_document, fullSpan, root, _owner, _cancellationToken);
                _projectDriver = new DiagnosticAnalyzerDriver(_document.Project, _owner, _cancellationToken);
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
                    foreach (var stateSet in _owner._stateManger.GetOrCreateStateSets(_document.Project))
                    {
                        containsFullResult &= await TryGetDocumentDiagnosticsAsync(
                            stateSet, StateType.Syntax, (t, d) => t.Equals(textVersion) && d.Equals(syntaxVersion), GetSyntaxDiagnosticsAsync).ConfigureAwait(false);

                        containsFullResult &= await TryGetDocumentDiagnosticsAsync(
                            stateSet, StateType.Document, (t, d) => t.Equals(textVersion) && d.Equals(semanticVersion), GetSemanticDiagnosticsAsync).ConfigureAwait(false);

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
                            stateSet, StateType.Project, (t, d) => t.Equals(projectTextVersion) && d.Equals(semanticVersion), GetProjectDiagnosticsWorkerAsync).ConfigureAwait(false);
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

            private async Task<bool> TryGetDocumentDiagnosticsAsync(
                StateSet stateSet, StateType stateType, Func<VersionStamp, VersionStamp, bool> versionCheck,
                Func<DiagnosticAnalyzerDriver, DiagnosticAnalyzer, Task<IEnumerable<DiagnosticData>>> getDiagnostics)
            {
                if (_spanBasedDriver.IsAnalyzerSuppressed(stateSet.Analyzer) ||
                    !(await ShouldRunAnalyzerForStateTypeAsync(stateSet, stateType).ConfigureAwait(false)))
                {
                    return true;
                }

                bool supportsSemanticInSpan = await stateSet.Analyzer.SupportsSpanBasedSemanticDiagnosticAnalysisAsync(_spanBasedDriver).ConfigureAwait(false);
                var analyzerDriver = GetAnalyzerDriverBasedOnStateType(stateType, supportsSemanticInSpan);
                Func<DiagnosticData, bool> shouldInclude = d => d.DocumentId == _document.Id && _range.IntersectsWith(d.TextSpan);

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

                var dx = await getDiagnostics(analyzerDriver, stateSet.Analyzer).ConfigureAwait(false);
                if (dx != null)
                {
                    // no state yet
                    Diagnostics.AddRange(dx.Where(shouldInclude));
                }

                return true;
            }

            private async Task<bool> ShouldRunAnalyzerForStateTypeAsync(StateSet stateSet, StateType stateType)
            {
                if (stateType == StateType.Project)
                {
                    return await DiagnosticIncrementalAnalyzer.ShouldRunAnalyzerForStateTypeAsync(_projectDriver, stateSet.Analyzer, stateType).ConfigureAwait(false);
                }

                return await DiagnosticIncrementalAnalyzer.ShouldRunAnalyzerForStateTypeAsync(_spanBasedDriver, stateSet.Analyzer, stateType).ConfigureAwait(false);
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

            private Task<IEnumerable<DiagnosticData>> GetProjectDiagnosticsWorkerAsync(DiagnosticAnalyzerDriver driver, DiagnosticAnalyzer analyzer)
            {
                return GetProjectDiagnosticsAsync(driver, analyzer, _owner.ForceAnalyzeAllDocuments);
            }
        }
    }
}
