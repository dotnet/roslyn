// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
        // At the end of the session we ask the diagnostic analyzer to reanalyze 
        // the documents to clean up the diagnostics.
        // An id may be present in this set even if the document doesn't have a rude edit anymore.
        private readonly object _documentsWithReportedRudeEditsGuard = new object();
        private readonly HashSet<DocumentId> _documentsWithReportedRudeEdits;

        private readonly ImmutableDictionary<ProjectId, ProjectReadOnlyReason> _projects;

        // EncEditSessionInfo is populated on a background thread and then read from the UI thread
        private readonly object _encEditSessionInfoGuard = new object();
        private EncEditSessionInfo _encEditSessionInfo = new EncEditSessionInfo();

        private readonly ImmutableDictionary<ActiveMethodId, ImmutableArray<NonRemappableRegion>> _nonRemappableRegions;

        internal EditSession(
            Solution baseSolution,
            DebuggingSession debuggingSession,
            IActiveStatementProvider activeStatementProvider,
            ImmutableDictionary<ProjectId, ProjectReadOnlyReason> projects,
            ImmutableDictionary<ActiveMethodId, ImmutableArray<NonRemappableRegion>> nonRemappableRegions,
            bool stoppedAtException)
        {
            Debug.Assert(baseSolution != null);
            Debug.Assert(debuggingSession != null);
            Debug.Assert(activeStatementProvider != null);
            Debug.Assert(nonRemappableRegions != null);

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

            _nonRemappableRegions = nonRemappableRegions;

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

            bool SupportsEditAndContinue(DocumentId documentId)
                => solution.GetProject(documentId.ProjectId).LanguageServices.GetService<IEditAndContinueAnalyzer>() != null;

            foreach (var debugInfo in debugInfos)
            {
                var documentName = debugInfo.DocumentNameOpt;
                if (documentName == null)
                {
                    // Ignore active statements that do not have a source location.
                    continue;
                }

                var documentIds = solution.GetDocumentIdsWithFilePath(documentName);
                var firstDocumentId = documentIds.FirstOrDefault(SupportsEditAndContinue);
                if (firstDocumentId == null)
                {
                    // Ignore active statements that don't belong to the solution or language that supports EnC service.
                    continue;
                }

                if (!byDocument.TryGetValue(firstDocumentId, out var primaryDocumentActiveStatements))
                {
                    byDocument.Add(firstDocumentId, primaryDocumentActiveStatements = ArrayBuilder<ActiveStatement>.GetInstance());
                }

                var activeStatement = new ActiveStatement(
                    ordinal: byInstruction.Count,
                    primaryDocumentOrdinal: primaryDocumentActiveStatements.Count,
                    documentIds: documentIds,
                    flags: debugInfo.Flags,
                    span: GetUpToDateSpan(debugInfo),
                    instructionId: debugInfo.InstructionId,
                    threadIds: debugInfo.ThreadIds);

                primaryDocumentActiveStatements.Add(activeStatement);

                // TODO: associate only those documents that are from a project with the right module id
                // https://github.com/dotnet/roslyn/issues/24320
                for (var i = 1; i < documentIds.Length; i++)
                {
                    var documentId = documentIds[i];
                    if (!SupportsEditAndContinue(documentId))
                    {
                        continue;
                    }

                    if (!byDocument.TryGetValue(documentId, out var linkedDocumentActiveStatements))
                    {
                        byDocument.Add(documentId, linkedDocumentActiveStatements = ArrayBuilder<ActiveStatement>.GetInstance());
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

        private LinePositionSpan GetUpToDateSpan(ActiveStatementDebugInfo activeStatementInfo)
        {
            if ((activeStatementInfo.Flags & ActiveStatementFlags.MethodUpToDate) != 0)
            {
                return activeStatementInfo.LinePositionSpan;
            }

            // Map active statement spans in non-remappable regions to the latest source locations.
            if (_nonRemappableRegions.TryGetValue(activeStatementInfo.InstructionId.MethodId, out var regionsInMethod))
            {
                foreach (var region in regionsInMethod)
                {
                    if (region.Span.Contains(activeStatementInfo.LinePositionSpan))
                    {
                        return activeStatementInfo.LinePositionSpan.AddLineDelta(region.LineDelta);
                    }
                }
            }

            // The active statement is in a method that's not up-to-date but the active span have not changed.
            // We only add changed spans to non-remappable regions map, so we won't find unchanged span there.
            // Return the original span.
            return activeStatementInfo.LinePositionSpan;
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
                    var exceptionRegions = analyzer.GetExceptionRegions(sourceText, syntaxRoot, activeStatement.Span, activeStatement.IsNonLeaf, out var isCovered);

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
            return Projects.TryGetValue(id, out _);
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

        private async Task<HashSet<ISymbol>> GetAllAddedSymbolsAsync(Project project, CancellationToken cancellationToken)
        {
            try
            {
                Analysis[] analyses;
                lock (_analysesGuard)
                {
                    analyses = _analyses.Values.ToArray();
                }

                HashSet<ISymbol> addedSymbols = null;
                foreach (var analysis in analyses)
                {
                    // Only consider analyses for documents that belong the currently analyzed project.
                    if (analysis.Document.Project == project)
                    {
                        var results = await analysis.Results.GetValueAsync(cancellationToken).ConfigureAwait(false);
                        if (!results.HasChangesAndErrors)
                        {
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
                    }
                }

                return addedSymbols;
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
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

            var analyzer = document.Project.LanguageServices.GetRequiredService<IEditAndContinueAnalyzer>();

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

                        var trackingService = _baseSolution.Workspace.Services.GetService<IActiveStatementTrackingService>();
                        var baseProject = _baseSolution.GetProject(document.Project.Id);
                        var result = await analyzer.AnalyzeDocumentAsync(baseProject, documentBaseActiveStatements, document, trackingService, cancellationToken).ConfigureAwait(false);

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

                var hasChanges = false;
                var hasSignificantChanges = false;

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

                    if (result.ActiveStatements.Length > 0)
                    {
                        allActiveStatements.Add((documentId, result.ActiveStatements, result.ExceptionRegions));
                    }
                }

                // Ideally we shouldn't be asking for deltas in absence of significant changes.
                // But in VS we have no way of telling the debugger that the changes made 
                // to the source are not significant. So we emit an empty delta.
                // Debug.Assert(allEdits.Count > 0 || allLineEdits.Count > 0);

                return new ProjectChanges(allEdits.ToImmutableAndFree(), allLineEdits.ToImmutableAndFree(), allActiveStatements.ToImmutableAndFree());
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(e))
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
                var allAddedSymbols = await GetAllAddedSymbolsAsync(project, cancellationToken).ConfigureAwait(false);
                var baseActiveStatements = await BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
                var baseActiveExceptionRegions = await BaseActiveExceptionRegions.GetValueAsync(cancellationToken).ConfigureAwait(false);

                var pdbStream = new MemoryStream();
                var updatedMethods = new List<MethodDefinitionHandle>();

                using (var metadataStream = SerializableBytes.CreateWritableStream())
                using (var ilStream = SerializableBytes.CreateWritableStream())
                {
                    var result = currentCompilation.EmitDifference(
                        baseline,
                        projectChanges.SemanticEdits,
                        s => allAddedSymbols?.Contains(s) ?? false,
                        metadataStream,
                        ilStream,
                        pdbStream,
                        updatedMethods,
                        cancellationToken);

                    var updatedMethodTokens = updatedMethods.Select(h => MetadataTokens.GetToken(h)).ToArray();

                    // Determine all active statements whose span changed and exception region span deltas.

                    GetActiveStatementAndExceptionRegionSpans(
                        baseline.OriginalMetadata.GetModuleVersionId(),
                        baseActiveStatements,
                        baseActiveExceptionRegions,
                        updatedMethodTokens,
                        _nonRemappableRegions,
                        projectChanges.NewActiveStatements,
                        out var activeStatementsInUpdatedMethods,
                        out var nonRemappableRegions);

                    return new Deltas(
                        ilStream.ToArray(),
                        metadataStream.ToArray(),
                        pdbStream,
                        updatedMethodTokens,
                        projectChanges.LineChanges,
                        nonRemappableRegions,
                        activeStatementsInUpdatedMethods,
                        result);
                }
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashAndPropagate(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static void GetActiveStatementAndExceptionRegionSpans(
            Guid moduleId,
            ActiveStatementsMap baseActiveStatements,
            ImmutableArray<ActiveStatementExceptionRegions> baseActiveExceptionRegions,
            int[] updatedMethodTokens,
            ImmutableDictionary<ActiveMethodId, ImmutableArray<NonRemappableRegion>> previousNonRemappableRegions,
            ImmutableArray<(DocumentId DocumentId, ImmutableArray<ActiveStatement> ActiveStatements, ImmutableArray<ImmutableArray<LinePositionSpan>> ExceptionRegions)> newActiveStatementsInChangedDocuments,
            out ImmutableArray<(Guid ThreadId, ActiveInstructionId OldInstructionId, LinePositionSpan NewSpan)> activeStatementsInUpdatedMethods,
            out ImmutableArray<(ActiveMethodId Method, NonRemappableRegion Region)> nonRemappableRegions)
        {
            var changedNonRemappableSpans = PooledDictionary<(int MethodToken, int MethodVersion, LinePositionSpan BaseSpan), LinePositionSpan>.GetInstance();
            var activeStatementsInUpdatedMethodsBuilder = ArrayBuilder<(Guid, ActiveInstructionId, LinePositionSpan)>.GetInstance();
            var nonRemappableRegionsBuilder = ArrayBuilder<(ActiveMethodId Method, NonRemappableRegion Region)>.GetInstance();

            // Process active statements and their exception regions in changed documents of this project/module:
            foreach (var (documentId, newActiveStatements, newExceptionRegions) in newActiveStatementsInChangedDocuments)
            {
                var oldActiveStatements = baseActiveStatements.DocumentMap[documentId];
                Debug.Assert(oldActiveStatements.Length == newActiveStatements.Length);
                Debug.Assert(newActiveStatements.Length == newExceptionRegions.Length);

                for (var i = 0; i < newActiveStatements.Length; i++)
                {
                    var oldActiveStatement = oldActiveStatements[i];
                    var newActiveStatement = newActiveStatements[i];
                    var oldInstructionId = oldActiveStatement.InstructionId;
                    var methodToken = oldInstructionId.MethodId.Token;
                    var methodVersion = oldInstructionId.MethodId.Version;

                    var isMethodUpdated = updatedMethodTokens.Contains(methodToken);
                    if (isMethodUpdated)
                    {
                        foreach (var threadId in oldActiveStatement.ThreadIds)
                        {
                            activeStatementsInUpdatedMethodsBuilder.Add((threadId, oldInstructionId, newActiveStatement.Span));
                        }
                    }

                    void AddNonRemappableRegion(LinePositionSpan oldSpan, LinePositionSpan newSpan, bool isExceptionRegion)
                    {
                        if (oldActiveStatement.IsMethodUpToDate)
                        {
                            // Start tracking non-remappable regions for active statements in methods that were up-to-date 
                            // when break state was entered and now being updated (regardless of whether the active span changed or not).
                            if (isMethodUpdated)
                            {
                                var lineDelta = oldSpan.GetLineDelta(newSpan: newSpan);
                                nonRemappableRegionsBuilder.Add((oldInstructionId.MethodId, new NonRemappableRegion(oldSpan, lineDelta, isExceptionRegion)));
                            }

                            // If the method has been up-to-date and it is not updated now then either the active statement span has not changed,
                            // or the entire method containing it moved. In neither case do we need to start tracking non-remapable region
                            // for the active statement since movement of whole method bodies (if any) is handled only on PDB level without 
                            // triggering any remapping on the IL level.
                        }
                        else if (oldSpan != newSpan)
                        {
                            // The method is not up-to-date hence we maintain non-remapable span map for it that needs to be updated.
                            changedNonRemappableSpans[(methodToken, methodVersion, oldSpan)] = newSpan;
                        }
                    }

                    AddNonRemappableRegion(oldActiveStatement.Span, newActiveStatement.Span, isExceptionRegion: false);

                    var j = 0;
                    foreach (var oldSpan in baseActiveExceptionRegions[oldActiveStatement.Ordinal].Spans)
                    {
                        AddNonRemappableRegion(oldSpan, newExceptionRegions[oldActiveStatement.PrimaryDocumentOrdinal][j++], isExceptionRegion: true);
                    }
                }
            }

            activeStatementsInUpdatedMethods = activeStatementsInUpdatedMethodsBuilder.ToImmutableAndFree();

            // Gather all active method instances contained in this project/module that are not up-to-date:
            var unremappedActiveMethods = PooledHashSet<ActiveMethodId>.GetInstance();
            foreach (var (instruction, baseActiveStatement) in baseActiveStatements.InstructionMap)
            {
                if (moduleId == instruction.MethodId.ModuleId && !baseActiveStatement.IsMethodUpToDate)
                {
                    unremappedActiveMethods.Add(instruction.MethodId);
                }
            }

            if (unremappedActiveMethods.Count > 0)
            {
                foreach (var (methodInstance, regionsInMethod) in previousNonRemappableRegions)
                {
                    // Skip non-remappable regions that belong to method instances that are from a different module 
                    // or no longer active (all active statements in these method instances have been remapped to newer versions).
                    if (!unremappedActiveMethods.Contains(methodInstance))
                    {
                        continue;
                    }

                    foreach (var region in regionsInMethod)
                    {
                        // We have calculated changes against a base snapshot (last break state):
                        var baseSpan = region.Span.AddLineDelta(region.LineDelta);

                        NonRemappableRegion newRegion;
                        if (changedNonRemappableSpans.TryGetValue((methodInstance.Token, methodInstance.Version, baseSpan), out var newSpan))
                        {
                            // all spans must be of the same size:
                            Debug.Assert(newSpan.End.Line - newSpan.Start.Line == baseSpan.End.Line - baseSpan.Start.Line);
                            Debug.Assert(region.Span.End.Line - region.Span.Start.Line == baseSpan.End.Line - baseSpan.Start.Line);

                            newRegion = region.WithLineDelta(region.Span.GetLineDelta(newSpan: newSpan));
                        }
                        else
                        {
                            newRegion = region;
                        }

                        nonRemappableRegionsBuilder.Add((methodInstance, newRegion));
                    }
                }
            }

            nonRemappableRegions = nonRemappableRegionsBuilder.ToImmutableAndFree();
            changedNonRemappableSpans.Free();
            unremappedActiveMethods.Free();
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

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly EditSession _editSession;

            public TestAccessor(EditSession editSession)
            {
                _editSession = editSession;
            }

            internal static void GetActiveStatementAndExceptionRegionSpans(
                Guid moduleId,
                ActiveStatementsMap baseActiveStatements,
                ImmutableArray<ActiveStatementExceptionRegions> baseActiveExceptionRegions,
                int[] updatedMethodTokens,
                ImmutableDictionary<ActiveMethodId, ImmutableArray<NonRemappableRegion>> previousNonRemappableRegions,
                ImmutableArray<(DocumentId DocumentId, ImmutableArray<ActiveStatement> ActiveStatements, ImmutableArray<ImmutableArray<LinePositionSpan>> ExceptionRegions)> newActiveStatementsInChangedDocuments,
                out ImmutableArray<(Guid ThreadId, ActiveInstructionId OldInstructionId, LinePositionSpan NewSpan)> activeStatementsInUpdatedMethods,
                out ImmutableArray<(ActiveMethodId Method, NonRemappableRegion Region)> nonRemappableRegions)
            {
                EditSession.GetActiveStatementAndExceptionRegionSpans(
                    moduleId,
                    baseActiveStatements,
                    baseActiveExceptionRegions,
                    updatedMethodTokens,
                    previousNonRemappableRegions,
                    newActiveStatementsInChangedDocuments,
                    out activeStatementsInUpdatedMethods,
                    out nonRemappableRegions);
            }
        }
    }
}
