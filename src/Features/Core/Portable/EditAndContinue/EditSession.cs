// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed partial class EditSession
    {
        private readonly struct Analysis
        {
            public readonly Document Document;
            public readonly AsyncLazy<DocumentAnalysisResults> Results;

            public Analysis(Document document, AsyncLazy<DocumentAnalysisResults> results)
            {
                Document = document;
                Results = results;
            }
        }

        private readonly Solution _baseSolution;

        // signaled when the session is terminated:
        private readonly CancellationTokenSource _cancellation;

        internal readonly AsyncLazy<ActiveStatementsMap> BaseActiveStatements;

        /// <summary>
        /// For each base active statement the exception regions around that statement. 
        /// </summary>
        internal readonly AsyncLazy<ImmutableArray<ActiveStatementExceptionRegions>> BaseActiveExceptionRegions;

        private readonly DebuggingSession _debuggingSession;
        private readonly IActiveStatementProvider _activeStatementProvider;

        /// <summary>
        /// Stopped at exception, an unwind is required before EnC is allowed. All edits are rude.
        /// </summary>
        private readonly bool _stoppedAtException;

        // Results of changed documents analysis. 
        // The work is triggered by an incremental analyzer on idle or explicitly when "continue" operation is executed.
        // Contains analyses of the latest observed document versions.
        private readonly object _analysesGuard = new object();
        private readonly Dictionary<DocumentId, Analysis> _analyses;

        // A document id is added whenever any analysis reports rude edits.
        // We collect a set of document ids that contained a rude edit
        // at some point in time during the lifespan of an edit session.
        // At the end of the session we aks the diagnostic analyzer to reanalyze 
        // the documents to clean up the diagnostics.
        // An id may be present in this set even if the document doesn't have a rude edit anymore.
        private readonly object _documentsWithReportedRudeEditsGuard = new object();
        private readonly HashSet<DocumentId> _documentsWithReportedRudeEdits;

        private readonly ImmutableDictionary<ProjectId, ProjectReadOnlyReason> _projects;

        // EncEditSessionInfo is populated on a background thread and then read from the UI thread
        private readonly object _encEditSessionInfoGuard = new object();
        private EncEditSessionInfo _encEditSessionInfo = new EncEditSessionInfo();

        // Active statement spans that were updated in previous edit sessions.
        private readonly IReadOnlyDictionary<ActiveInstructionId, LinePositionSpan> _previouslyUpdatedActiveStatementSpans;

        internal EditSession(
            Solution baseSolution,
            DebuggingSession debuggingSession,
            IActiveStatementProvider activeStatementProvider,
            ImmutableDictionary<ProjectId, ProjectReadOnlyReason> projects,
            IReadOnlyDictionary<ActiveInstructionId, LinePositionSpan> previouslyUpdatedActiveStatementSpans,
            bool stoppedAtException)
        {
            Debug.Assert(baseSolution != null);
            Debug.Assert(debuggingSession != null);
            Debug.Assert(activeStatementProvider != null);
            Debug.Assert(previouslyUpdatedActiveStatementSpans != null);

            _baseSolution = baseSolution;
            _debuggingSession = debuggingSession;
            _activeStatementProvider = activeStatementProvider;
            _stoppedAtException = stoppedAtException;
            _projects = projects;
            _cancellation = new CancellationTokenSource();

            // TODO: small dict, pool?
            _analyses = new Dictionary<DocumentId, Analysis>();

            // TODO: small dict, pool?
            _documentsWithReportedRudeEdits = new HashSet<DocumentId>();

            _previouslyUpdatedActiveStatementSpans = previouslyUpdatedActiveStatementSpans;

            BaseActiveStatements = new AsyncLazy<ActiveStatementsMap>(GetBaseActiveStatementsAsync, cacheResult: true);
            BaseActiveExceptionRegions = new AsyncLazy<ImmutableArray<ActiveStatementExceptionRegions>>(GetBaseActiveExceptionRegionsAsync, cacheResult: true);
        }

        private async Task<ActiveStatementsMap> GetBaseActiveStatementsAsync(CancellationToken cancellationToken)
        {
            try
            {
                return CreateActiveStatementsMap(_baseSolution, await _activeStatementProvider.GetActiveStatementsAsync(cancellationToken).ConfigureAwait(false));
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return new ActiveStatementsMap(
                    SpecializedCollections.EmptyReadOnlyDictionary<DocumentId, ImmutableArray<ActiveStatement>>(), 
                    SpecializedCollections.EmptyReadOnlyDictionary<ActiveInstructionId, ActiveStatement>());
            }
        }

        private ActiveStatementsMap CreateActiveStatementsMap(Solution solution, ImmutableArray<ActiveStatementDebugInfo> debugInfos)
        {
            var byDocument = PooledDictionary<DocumentId, ArrayBuilder<ActiveStatement>>.GetInstance();
            var byInstruction = PooledDictionary<ActiveInstructionId, ActiveStatement>.GetInstance();

            foreach (var debugInfo in debugInfos)
            {
                // TODO (tomat): We need to track these for remapping purposes.
                //
                // Active statement is in user hidden code. The only information that we have from the debugger
                // is the method token. We don't need to track the statement (it's not in user code anyways),
                // but we should probably track the list of such methods in order to preserve their local variables.
                // Not sure what's exactly the scenario here, perhaps modifying async method/iterator? 
                // Dev12 just ignores these.
                //if ((debugInfo.Flags & ActiveStatementFlags.NonUserCode) != 0)
                //{
                //    continue;
                //}

                var linePositionSpan = debugInfo.LinePositionSpan;
                
                // Map outdated active statement spans - the span reported by the debugger might correspond to an IP that has not been remapped yet.
                // In that case we use the span where the active statement was located at the end of the previous edit session.
                if ((debugInfo.Flags & ActiveStatementFlags.MethodUpToDate) == 0 &&
                    _previouslyUpdatedActiveStatementSpans.TryGetValue(debugInfo.InstructionId, out var updatedLinePositionSpan))
                {
                    linePositionSpan = updatedLinePositionSpan;
                }

                var documentIds = solution.GetDocumentIdsWithFilePath(debugInfo.DocumentName);
                if (documentIds.IsEmpty)
                {
                    // TODO: continue without doc, warning if no document found for an active statement?
                    continue;
                }

                var firstDocumentId = documentIds.First();
                if (!byDocument.TryGetValue(firstDocumentId, out var primaryDocumentActiveStatements))
                {
                    byDocument.Add(firstDocumentId, primaryDocumentActiveStatements = ArrayBuilder<ActiveStatement>.GetInstance());
                }

                var activeStatement = new ActiveStatement(
                    ordinal: byInstruction.Count,
                    primaryDocumentOrdinal: primaryDocumentActiveStatements.Count,
                    documentIds: documentIds,
                    flags: debugInfo.Flags,
                    span: linePositionSpan,
                    instructionId: debugInfo.InstructionId,
                    threadIds: debugInfo.ThreadIds);

                primaryDocumentActiveStatements.Add(activeStatement);

                for (int i = 1; i < documentIds.Length; i++)
                {
                    if (!byDocument.TryGetValue(documentIds[i], out var linkedDocumentActiveStatements))
                    {
                        byDocument.Add(documentIds[i], linkedDocumentActiveStatements = ArrayBuilder<ActiveStatement>.GetInstance());
                    }

                    linkedDocumentActiveStatements.Add(activeStatement);
                }

                try
                {
                    byInstruction.Add(debugInfo.InstructionId, activeStatement);
                }
                catch (ArgumentException)
                {
                    throw new InvalidOperationException($"Multiple active statements with the same instruction id returned by " +
                        $"{_activeStatementProvider.GetType()}.{nameof(IActiveStatementProvider.GetActiveStatementsAsync)}");
                }
            }

            return new ActiveStatementsMap(byDocument.ToDictionaryAndFree(), byInstruction.ToDictionaryAndFree());
        }

        private async Task<ImmutableArray<ActiveStatementExceptionRegions>> GetBaseActiveExceptionRegionsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var baseActiveStatements = await BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
                var instructionMap = baseActiveStatements.InstructionMap;
                var builder = ArrayBuilder<ActiveStatementExceptionRegions>.GetInstance(instructionMap.Count);
                builder.Count = instructionMap.Count;

                foreach (var activeStatement in instructionMap.Values)
                {
                    var document = _baseSolution.GetDocument(activeStatement.PrimaryDocumentId);
                    var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                    var analyzer = document.Project.LanguageServices.GetService<IEditAndContinueAnalyzer>();
                    var exceptionRegions = analyzer.GetExceptionRegions(sourceText, syntaxRoot, activeStatement.Span, activeStatement.IsNonLeaf, out bool isCovered);

                    builder[activeStatement.Ordinal] = new ActiveStatementExceptionRegions(exceptionRegions, isCovered);
                }

                return builder.ToImmutableAndFree();
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return ImmutableArray<ActiveStatementExceptionRegions>.Empty;
            }
        }

        internal CancellationTokenSource Cancellation => _cancellation;

        internal Solution BaseSolution => _baseSolution;

        private Solution CurrentSolution => _baseSolution.Workspace.CurrentSolution;

        public bool StoppedAtException => _stoppedAtException;

        public IReadOnlyDictionary<ProjectId, ProjectReadOnlyReason> Projects => _projects;

        internal bool HasProject(ProjectId id)
        {
            return Projects.TryGetValue(id, out var reason);
        }

        private List<(DocumentId, AsyncLazy<DocumentAnalysisResults>)> GetChangedDocumentsAnalyses(Project baseProject, Project project)
        {
            var changes = project.GetChanges(baseProject);
            var changedDocuments = changes.GetChangedDocuments().Concat(changes.GetAddedDocuments());
            var result = new List<(DocumentId, AsyncLazy<DocumentAnalysisResults>)>();

            lock (_analysesGuard)
            {
                foreach (var changedDocumentId in changedDocuments)
                {
                    result.Add((changedDocumentId, GetDocumentAnalysisNoLock(project.GetDocument(changedDocumentId))));
                }
            }

            return result;
        }

        private async Task<HashSet<ISymbol>> GetAllAddedSymbols(CancellationToken cancellationToken)
        {
            Analysis[] analyses;
            lock (_analysesGuard)
            {
                analyses = _analyses.Values.ToArray();
            }

            HashSet<ISymbol> addedSymbols = null;
            foreach (var analysis in analyses)
            {
                var results = await analysis.Results.GetValueAsync(cancellationToken).ConfigureAwait(false);
                foreach (var edit in results.SemanticEdits)
                {
                    if (edit.Kind == SemanticEditKind.Insert)
                    {
                        if (addedSymbols == null)
                        {
                            addedSymbols = new HashSet<ISymbol>();
                        }

                        addedSymbols.Add(edit.NewSymbol);
                    }
                }
            }

            return addedSymbols;
        }

        public AsyncLazy<DocumentAnalysisResults> GetDocumentAnalysis(Document document)
        {
            lock (_analysesGuard)
            {
                return GetDocumentAnalysisNoLock(document);
            }
        }

        private AsyncLazy<DocumentAnalysisResults> GetDocumentAnalysisNoLock(Document document)
        {
            if (_analyses.TryGetValue(document.Id, out var analysis) && analysis.Document == document)
            {
                return analysis.Results;
            }

            var analyzer = document.Project.LanguageServices.GetService<IEditAndContinueAnalyzer>();

            var lazyResults = new AsyncLazy<DocumentAnalysisResults>(
                asynchronousComputeFunction: async cancellationToken =>
                {
                    try
                    {
                        var baseActiveStatements = await BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
                        if (!baseActiveStatements.DocumentMap.TryGetValue(document.Id, out var documentBaseActiveStatements))
                        {
                            documentBaseActiveStatements = ImmutableArray<ActiveStatement>.Empty;
                        }

                        var result = await analyzer.AnalyzeDocumentAsync(_baseSolution, documentBaseActiveStatements, document, cancellationToken).ConfigureAwait(false);

                        if (!result.RudeEditErrors.IsDefault)
                        {
                            lock (_documentsWithReportedRudeEditsGuard)
                            {
                                _documentsWithReportedRudeEdits.Add(document.Id);
                            }
                        }

                        return result;
                    }
                    catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                },
                cacheResult: true);

            _analyses[document.Id] = new Analysis(document, lazyResults);
            return lazyResults;
        }

        internal ImmutableArray<DocumentId> GetDocumentsWithReportedRudeEdits()
        {
            lock (_documentsWithReportedRudeEditsGuard)
            {
                return ImmutableArray.CreateRange(_documentsWithReportedRudeEdits);
            }
        }

        public async Task<ProjectAnalysisSummary> GetProjectAnalysisSummaryAsync(Project project, CancellationToken cancellationToken)
        {
            try
            {
                var baseProject = _baseSolution.GetProject(project.Id);

                // TODO (https://github.com/dotnet/roslyn/issues/1204):
                if (baseProject == null)
                {
                    return ProjectAnalysisSummary.NoChanges;
                }

                var documentAnalyses = GetChangedDocumentsAnalyses(baseProject, project);
                if (documentAnalyses.Count == 0)
                {
                    return ProjectAnalysisSummary.NoChanges;
                }

                bool hasChanges = false;
                bool hasSignificantChanges = false;

                foreach (var analysis in documentAnalyses)
                {
                    var result = await analysis.Item2.GetValueAsync(cancellationToken).ConfigureAwait(false);

                    // skip documents that actually were not changed:
                    if (!result.HasChanges)
                    {
                        continue;
                    }

                    // rude edit detection wasn't completed due to errors in compilation:
                    if (result.HasChangesAndCompilationErrors)
                    {
                        return ProjectAnalysisSummary.CompilationErrors;
                    }

                    // rude edits detected:
                    if (result.RudeEditErrors.Length != 0)
                    {
                        return ProjectAnalysisSummary.RudeEdits;
                    }

                    hasChanges = true;
                    hasSignificantChanges |= result.HasSignificantChanges;
                }

                if (!hasChanges)
                {
                    // we get here if a document is closed and reopen without any actual change:
                    return ProjectAnalysisSummary.NoChanges;
                }

                if (_stoppedAtException)
                {
                    // all edits are disallowed when stopped at exception:
                    return ProjectAnalysisSummary.RudeEdits;
                }

                return hasSignificantChanges ?
                    ProjectAnalysisSummary.ValidChanges :
                    ProjectAnalysisSummary.ValidInsignificantChanges;
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task<ProjectChanges> GetProjectChangesAsync(Project project, CancellationToken cancellationToken)
        {
            try
            {
                var baseProject = _baseSolution.GetProject(project.Id);
                var allEdits = ArrayBuilder<SemanticEdit>.GetInstance();
                var allLineEdits = ArrayBuilder<(DocumentId, ImmutableArray<LineChange>)>.GetInstance();
                var allActiveStatements = ArrayBuilder<(DocumentId, ImmutableArray<ActiveStatement>, ImmutableArray<ImmutableArray<LinePositionSpan>>)>.GetInstance();

                foreach (var (documentId, asyncResult) in GetChangedDocumentsAnalyses(baseProject, project))
                {
                    var result = await asyncResult.GetValueAsync(cancellationToken).ConfigureAwait(false);

                    // we shouldn't be asking for deltas in presence of errors:
                    Debug.Assert(!result.HasChangesAndErrors);

                    allEdits.AddRange(result.SemanticEdits);
                    if (result.LineEdits.Length > 0)
                    {
                        allLineEdits.Add((documentId, result.LineEdits));
                    }

                    allActiveStatements.Add((documentId, result.ActiveStatements, result.ExceptionRegions));
                }

                // Ideally we shouldn't be asking for deltas in absence of significant changes.
                // But in VS we have no way of telling the debugger that the changes made 
                // to the source are not significant. So we emit an empty delta.
                // Debug.Assert(allEdits.Count > 0 || allLineEdits.Count > 0);

                return new ProjectChanges(allEdits.ToImmutableAndFree(), allLineEdits.ToImmutableAndFree(), allActiveStatements.ToImmutableAndFree());
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public async Task<Deltas> EmitProjectDeltaAsync(Project project, EmitBaseline baseline, CancellationToken cancellationToken)
        {
            try
            {
                Debug.Assert(!_stoppedAtException);

                var projectChanges = await GetProjectChangesAsync(project, cancellationToken).ConfigureAwait(false);
                var currentCompilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var allAddedSymbols = await GetAllAddedSymbols(cancellationToken).ConfigureAwait(false);
                var baseActiveStatements = await BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
                var baseActiveExceptionRegions = await BaseActiveExceptionRegions.GetValueAsync(cancellationToken).ConfigureAwait(false);

                var pdbStream = new MemoryStream();
                var updatedMethods = new List<MethodDefinitionHandle>();

                using (var metadataStream = SerializableBytes.CreateWritableStream())
                using (var ilStream = SerializableBytes.CreateWritableStream())
                {
                    EmitDifferenceResult result = currentCompilation.EmitDifference(
                        baseline,
                        projectChanges.SemanticEdits,
                        s => allAddedSymbols?.Contains(s) ?? false,
                        metadataStream,
                        ilStream,
                        pdbStream,
                        updatedMethods,
                        cancellationToken);
                    
                    int[] updatedMethodTokens = updatedMethods.Select(h => MetadataTokens.GetToken(h)).ToArray();
                  
                    // Determine all active statements whose span changed and exception region span deltas.
                    GetActiveStatementAndExceptionRegionSpans(
                        baseline.OriginalMetadata.GetModuleVersionId(),
                        baseActiveStatements, 
                        baseActiveExceptionRegions,
                        updatedMethodTokens,
                        projectChanges.ActiveStatements, 
                        out var exceptionRegionSpanDeltas,
                        out var updatedActiveStatementSpans,
                        out var activeStatementsInUpdatedMethods);

                    return new Deltas(
                        ilStream.ToArray(),
                        metadataStream.ToArray(),
                        pdbStream,
                        updatedMethodTokens,
                        projectChanges.LineChanges,
                        updatedActiveStatementSpans,
                        exceptionRegionSpanDeltas,
                        activeStatementsInUpdatedMethods,
                        result);
                }
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
                // recover (cancel EnC)
                return null;
            }
        }

        private static void GetActiveStatementAndExceptionRegionSpans(
            Guid moduleId,
            ActiveStatementsMap baseActiveStatements, 
            ImmutableArray<ActiveStatementExceptionRegions> baseActiveExceptionRegions,
            int[] updatedMethodTokens,
            ImmutableArray<(DocumentId DocumentId, ImmutableArray<ActiveStatement> ActiveStatements, ImmutableArray<ImmutableArray<LinePositionSpan>> ExceptionRegions)> activeStatementsInChangedDocuments,
            out ImmutableArray<(int MethodToken, int OldMethodVersion, LinePositionSpan OldSpan, LinePositionSpan NewSpan)> exceptionRegionSpanDeltas,
            out ImmutableArray<(ActiveInstructionId, LinePositionSpan)> updatedActiveStatementSpans,
            out ImmutableArray<(Guid ThreadId, ActiveInstructionId OldInstructionId, LinePositionSpan NewSpan)> activeStatementsInUpdatedMethods)
        {
            int exceptionRegionCount = baseActiveExceptionRegions.Sum(regions => regions.Spans.Length);
            var exceptionRegionBuilder = ArrayBuilder<(int MethodToken, int OldMethodVersion, LinePositionSpan OldSpan, LinePositionSpan NewSpan)>.GetInstance(exceptionRegionCount);
            var changedActiveStatementSet = BitVector.Create(baseActiveExceptionRegions.Length);
            var updatedActiveStatementsBuilder = ArrayBuilder<(ActiveInstructionId, LinePositionSpan)>.GetInstance();
            var activeStatementsInUpdatedMethodsBuilder = ArrayBuilder<(Guid, ActiveInstructionId, LinePositionSpan)>.GetInstance();

            // Process active statements and their exception regions in changed documents of this project/module:
            foreach (var (documentId, newActiveStatements, newExceptionRegions) in activeStatementsInChangedDocuments)
            {
                var oldActiveStatements = baseActiveStatements.DocumentMap[documentId];
                Debug.Assert(oldActiveStatements.Length == newActiveStatements.Length);

                for (int i = 0; i < newActiveStatements.Length; i++)
                {
                    var oldActiveStatement = oldActiveStatements[i];
                    var newActiveStatement = newActiveStatements[i];
                    var oldInstructionId = oldActiveStatement.InstructionId;
                    var ordinal = oldActiveStatement.Ordinal;
                    var methodToken = oldActiveStatement.InstructionId.MethodToken;

                    if (oldActiveStatement.Span != newActiveStatement.Span)
                    {
                        updatedActiveStatementsBuilder.Add((oldInstructionId, newActiveStatement.Span));
                    }

                    if (updatedMethodTokens.Contains(methodToken))
                    {
                        foreach (var threadId in oldActiveStatement.ThreadIds)
                        {
                            activeStatementsInUpdatedMethodsBuilder.Add((threadId, oldInstructionId, newActiveStatement.Span));
                        }
                    }

                    Debug.Assert(!changedActiveStatementSet[ordinal]);

                    int j = 0;
                    foreach (var oldSpan in baseActiveExceptionRegions[ordinal].Spans)
                    {
                        var newSpan = newExceptionRegions[ordinal][j++];
                        exceptionRegionBuilder.Add((oldInstructionId.MethodToken, oldInstructionId.MethodVersion, oldSpan, newSpan));
                    }

                    changedActiveStatementSet[ordinal] = true;
                }
            }

            // Fill in unchanged exception regions.
            foreach (var (instructionId, activeStatement) in baseActiveStatements.InstructionMap)
            {
                int ordinal = activeStatement.Ordinal;

                if (moduleId == instructionId.ModuleId && !changedActiveStatementSet[ordinal])
                {
                    foreach (var span in baseActiveExceptionRegions[ordinal].Spans)
                    {
                        exceptionRegionBuilder.Add((instructionId.MethodToken, instructionId.MethodVersion, span, span));
                    }
                }
            }

            exceptionRegionSpanDeltas = exceptionRegionBuilder.ToImmutableAndFree();
            updatedActiveStatementSpans = updatedActiveStatementsBuilder.ToImmutableAndFree();
            activeStatementsInUpdatedMethods = activeStatementsInUpdatedMethodsBuilder.ToImmutableAndFree();
        }

        internal void LogRudeEditErrors(ImmutableArray<RudeEditDiagnostic> rudeEditErrors)
        {
            lock (_encEditSessionInfoGuard)
            {
                if (_encEditSessionInfo != null)
                {
                    foreach (var item in rudeEditErrors)
                    {
                        _encEditSessionInfo.LogRudeEdit((ushort)item.Kind, item.SyntaxKind);
                    }
                }
            }
        }

        internal void LogEmitProjectDeltaErrors(IEnumerable<string> errorIds)
        {
            lock (_encEditSessionInfoGuard)
            {
                Debug.Assert(_encEditSessionInfo != null);
                _encEditSessionInfo.EmitDeltaErrorIds = errorIds;
            }
        }

        internal void LogBuildState(ProjectAnalysisSummary lastEditSessionSummary)
        {
            lock (_encEditSessionInfoGuard)
            {
                Debug.Assert(_encEditSessionInfo != null);
                _encEditSessionInfo.HadCompilationErrors |= lastEditSessionSummary == ProjectAnalysisSummary.CompilationErrors;
                _encEditSessionInfo.HadRudeEdits |= lastEditSessionSummary == ProjectAnalysisSummary.RudeEdits;
                _encEditSessionInfo.HadValidChanges |= lastEditSessionSummary == ProjectAnalysisSummary.ValidChanges;
                _encEditSessionInfo.HadValidInsignificantChanges |= lastEditSessionSummary == ProjectAnalysisSummary.ValidInsignificantChanges;
            }
        }

        internal void LogEditSession(EncDebuggingSessionInfo encDebuggingSessionInfo)
        {
            lock (_encEditSessionInfoGuard)
            {
                Debug.Assert(_encEditSessionInfo != null);
                encDebuggingSessionInfo.EndEditSession(_encEditSessionInfo);
                _encEditSessionInfo = null;
            }
        }
    }
}
