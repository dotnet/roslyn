// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
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
            private readonly TextSpan _range;
            private readonly bool _blockForData;
            private readonly CancellationToken _cancellationToken;

            private readonly DiagnosticAnalyzerDriver _spanBasedDriver;
            private readonly DiagnosticAnalyzerDriver _documentBasedDriver;
            private readonly DiagnosticAnalyzerDriver _projectDriver;

            private readonly List<DiagnosticData> _diagnostics;

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

                _diagnostics = diagnostics;

                // Share the diagnostic analyzer driver across all analyzers.
                var fullSpan = root == null ? null : (TextSpan?)root.FullSpan;

                _spanBasedDriver = new DiagnosticAnalyzerDriver(_document, _range, root, _owner._diagnosticLogAggregator, _owner.HostDiagnosticUpdateSource, _cancellationToken);
                _documentBasedDriver = new DiagnosticAnalyzerDriver(_document, fullSpan, root, _owner._diagnosticLogAggregator, _owner.HostDiagnosticUpdateSource, _cancellationToken);
                _projectDriver = new DiagnosticAnalyzerDriver(_document.Project, _owner._diagnosticLogAggregator, _owner.HostDiagnosticUpdateSource, _cancellationToken);
            }

            public List<DiagnosticData> Diagnostics => _diagnostics;

            public async Task<bool> TryGetAsync()
            {
                try
                {
                    var textVersion = await _document.GetTextVersionAsync(_cancellationToken).ConfigureAwait(false);
                    var syntaxVersion = await _document.GetSyntaxVersionAsync(_cancellationToken).ConfigureAwait(false);
                    var projectTextVersion = await _document.Project.GetLatestDocumentVersionAsync(_cancellationToken).ConfigureAwait(false);
                    var semanticVersion = await _document.Project.GetDependentSemanticVersionAsync(_cancellationToken).ConfigureAwait(false);

                    var result = true;
                    foreach (var stateSet in _owner._stateManger.GetOrCreateStateSets(_document.Project))
                    {
                        result &= await TryGetDocumentDiagnosticsAsync(
                            stateSet, StateType.Syntax, (t, d) => t.Equals(textVersion) && d.Equals(syntaxVersion), GetSyntaxDiagnosticsAsync).ConfigureAwait(false);

                        result &= await TryGetDocumentDiagnosticsAsync(
                            stateSet, StateType.Document, (t, d) => t.Equals(textVersion) && d.Equals(semanticVersion), GetSemanticDiagnosticsAsync).ConfigureAwait(false);

                        // check whether compilation end code fix is enabled
                        if (!_document.Project.Solution.Workspace.Options.GetOption(InternalDiagnosticsOptions.CompilationEndCodeFix))
                        {
                            continue;
                        }

                        // check whether hueristic is enabled
                        if (_blockForData && _document.Project.Solution.Workspace.Options.GetOption(InternalDiagnosticsOptions.UseCompilationEndCodeFixHueristic))
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

                        result &= await TryGetDocumentDiagnosticsAsync(
                            stateSet, StateType.Project, (t, d) => t.Equals(projectTextVersion) && d.Equals(semanticVersion), GetProjectDiagnosticsWorkerAsync).ConfigureAwait(false);
                    }

                    return result;
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
                bool supportsSemanticInSpan;
                if (_spanBasedDriver.IsAnalyzerSuppressed(stateSet.Analyzer) ||
                    !ShouldRunAnalyzerForStateType(stateSet, stateType, out supportsSemanticInSpan))
                {
                    return true;
                }

                var analyzerDriver = GetAnalyzerDriverBasedOnStateType(stateType, supportsSemanticInSpan);

                var shouldInclude = (Func<DiagnosticData, bool>)(d => d.DocumentId == _document.Id && _range.IntersectsWith(d.TextSpan));

                // make sure we get state even when none of our analyzer has ran yet. 
                // but this shouldn't create analyzer that doesnt belong to this project (language)
                var state = stateSet.GetState(stateType);

                // see whether we can use existing info
                var existingData = await state.TryGetExistingDataAsync(_document, _cancellationToken).ConfigureAwait(false);
                if (existingData != null && versionCheck(existingData.TextVersion, existingData.DataVersion))
                {
                    if (existingData.Items == null || existingData.Items.Length == 0)
                    {
                        return true;
                    }

                    _diagnostics.AddRange(existingData.Items.Where(shouldInclude));
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
                    _diagnostics.AddRange(dx.Where(shouldInclude));
                }

                return true;
            }

            private bool ShouldRunAnalyzerForStateType(StateSet stateSet, StateType stateType, out bool supportsSemanticInSpan)
            {
                if (stateType == StateType.Project)
                {
                    return DiagnosticIncrementalAnalyzer.ShouldRunAnalyzerForStateType(_projectDriver, stateSet.Analyzer, stateType, out supportsSemanticInSpan);
                }

                return DiagnosticIncrementalAnalyzer.ShouldRunAnalyzerForStateType(_spanBasedDriver, stateSet.Analyzer, stateType, out supportsSemanticInSpan);
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
