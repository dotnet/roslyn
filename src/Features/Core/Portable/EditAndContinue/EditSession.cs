﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class EditSession : IDisposable
    {
        private readonly CancellationTokenSource _cancellationSource = new();

        internal readonly DebuggingSession DebuggingSession;
        internal readonly EditSessionTelemetry Telemetry;
        internal readonly IManagedEditAndContinueDebuggerService DebuggerService;

        private readonly ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>> _nonRemappableRegions;

        /// <summary>
        /// Lazily calculated map of base active statements.
        /// </summary>
        internal readonly AsyncLazy<ActiveStatementsMap> BaseActiveStatements;

        /// <summary>
        /// For each base active statement the exception regions around that statement. 
        /// </summary>
        internal ImmutableArray<ActiveStatementExceptionRegions> _lazyBaseActiveExceptionRegions;

        /// <summary>
        /// Cache of document EnC analyses. 
        /// </summary>
        internal readonly EditAndContinueDocumentAnalysesCache Analyses;

        /// <summary>
        /// A <see cref="DocumentId"/> is added whenever EnC analyzer reports 
        /// rude edits or module diagnostics. At the end of the session we ask the diagnostic analyzer to reanalyze 
        /// the documents to clean up the diagnostics.
        /// </summary>
        private readonly HashSet<DocumentId> _documentsWithReportedDiagnostics = new();
        private readonly object _documentsWithReportedDiagnosticsGuard = new();

        private PendingSolutionUpdate? _pendingUpdate;
        private bool _changesApplied;

        internal EditSession(
            DebuggingSession debuggingSession,
            EditSessionTelemetry telemetry,
            IManagedEditAndContinueDebuggerService debuggerService)
        {
            DebuggingSession = debuggingSession;
            Telemetry = telemetry;
            DebuggerService = debuggerService;

            _nonRemappableRegions = debuggingSession.NonRemappableRegions;

            BaseActiveStatements = new AsyncLazy<ActiveStatementsMap>(cancellationToken => GetBaseActiveStatementsAsync(cancellationToken), cacheResult: true);
            Analyses = new EditAndContinueDocumentAnalysesCache(BaseActiveStatements);
        }

        internal PendingSolutionUpdate? Test_GetPendingSolutionUpdate() => _pendingUpdate;

        internal CancellationToken CancellationToken => _cancellationSource.Token;
        internal void Cancel() => _cancellationSource.Cancel();

        public void Dispose()
            => _cancellationSource.Dispose();

        /// <summary>
        /// Errors to be reported when a project is updated but the corresponding module does not support EnC.
        /// </summary>
        /// <returns><see langword="default"/> if the module is not loaded.</returns>
        public async Task<ImmutableArray<Diagnostic>?> GetModuleDiagnosticsAsync(Guid mvid, string projectDisplayName, CancellationToken cancellationToken)
        {
            var availability = await DebuggerService.GetAvailabilityAsync(mvid, cancellationToken).ConfigureAwait(false);
            if (availability.Status == ManagedEditAndContinueAvailabilityStatus.ModuleNotLoaded)
            {
                return null;
            }

            if (availability.Status == ManagedEditAndContinueAvailabilityStatus.Available)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var descriptor = EditAndContinueDiagnosticDescriptors.GetModuleDiagnosticDescriptor(availability.Status);
            return ImmutableArray.Create(Diagnostic.Create(descriptor, Location.None, new[] { projectDisplayName, availability.LocalizedMessage }));
        }

        private async Task<ActiveStatementsMap> GetBaseActiveStatementsAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Last committed solution reflects the state of the source that is in sync with the binaries that are loaded in the debuggee.
                return CreateActiveStatementsMap(await DebuggerService.GetActiveStatementsAsync(cancellationToken).ConfigureAwait(false));
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
            {
                return new ActiveStatementsMap(
                    SpecializedCollections.EmptyReadOnlyDictionary<DocumentId, ImmutableArray<ActiveStatement>>(),
                    SpecializedCollections.EmptyReadOnlyDictionary<ManagedInstructionId, ActiveStatement>());
            }
        }

        private ActiveStatementsMap CreateActiveStatementsMap(ImmutableArray<ManagedActiveStatementDebugInfo> debugInfos)
        {
            var byDocument = PooledDictionary<DocumentId, ArrayBuilder<ActiveStatement>>.GetInstance();
            var byInstruction = PooledDictionary<ManagedInstructionId, ActiveStatement>.GetInstance();

            bool supportsEditAndContinue(DocumentId documentId)
                => EditAndContinueWorkspaceService.SupportsEditAndContinue(DebuggingSession.LastCommittedSolution.GetProject(documentId.ProjectId)!);

            foreach (var debugInfo in debugInfos)
            {
                var documentName = debugInfo.DocumentName;
                if (documentName == null)
                {
                    // Ignore active statements that do not have a source location.
                    continue;
                }

                // TODO: https://github.com/dotnet/roslyn/issues/49938
                // The committed solution may not contain all documents present in the PDB.
                var documentIds = DebuggingSession.LastCommittedSolution.GetDocumentIdsWithFilePath(documentName);
                var firstDocumentId = documentIds.FirstOrDefault(supportsEditAndContinue);
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
                    instructionId: debugInfo.ActiveInstruction);

                primaryDocumentActiveStatements.Add(activeStatement);

                // TODO: associate only those documents that are from a project with the right module id
                // https://github.com/dotnet/roslyn/issues/24320
                for (var i = 1; i < documentIds.Length; i++)
                {
                    var documentId = documentIds[i];
                    if (!supportsEditAndContinue(documentId))
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
                    byInstruction.Add(debugInfo.ActiveInstruction, activeStatement);
                }
                catch (ArgumentException)
                {
                    throw new InvalidOperationException($"Multiple active statements with the same instruction id returned by Active Statement Provider");
                }
            }

            return new ActiveStatementsMap(byDocument.ToMultiDictionaryAndFree(), byInstruction.ToDictionaryAndFree());
        }

        private LinePositionSpan GetUpToDateSpan(ManagedActiveStatementDebugInfo activeStatementInfo)
        {
            var activeSpan = activeStatementInfo.SourceSpan.ToLinePositionSpan();

            if ((activeStatementInfo.Flags & ActiveStatementFlags.MethodUpToDate) != 0)
            {
                return activeSpan;
            }

            var instructionId = activeStatementInfo.ActiveInstruction;

            // Map active statement spans in non-remappable regions to the latest source locations.
            if (_nonRemappableRegions.TryGetValue(instructionId.Method, out var regionsInMethod))
            {
                foreach (var region in regionsInMethod)
                {
                    if (region.Span.Contains(activeSpan))
                    {
                        return activeSpan.AddLineDelta(region.LineDelta);
                    }
                }
            }

            // The active statement is in a method that's not up-to-date but the active span have not changed.
            // We only add changed spans to non-remappable regions map, so we won't find unchanged span there.
            // Return the original span.
            return activeSpan;
        }

        /// <summary>
        /// Calculates exception regions for all active statements.
        /// If an active statement is in a document that's out-of-sync returns default(<see cref="ActiveStatementExceptionRegions"/>) for that statement.
        /// </summary>
        internal async Task<ImmutableArray<ActiveStatementExceptionRegions>> GetBaseActiveExceptionRegionsAsync(Solution solution, CancellationToken cancellationToken)
        {
            try
            {
                if (!_lazyBaseActiveExceptionRegions.IsDefault)
                {
                    return _lazyBaseActiveExceptionRegions;
                }

                var baseActiveStatements = await BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
                var instructionMap = baseActiveStatements.InstructionMap;
                using var builderDisposer = ArrayBuilder<ActiveStatementExceptionRegions>.GetInstance(instructionMap.Count, out var builder);
                builder.Count = instructionMap.Count;

                var hasOutOfSyncDocuments = false;

                foreach (var activeStatement in instructionMap.Values)
                {
                    bool isCovered;
                    ImmutableArray<LinePositionSpan> exceptionRegions;

                    // Can't calculate exception regions for active statements in out-of-sync documents.
                    var primaryDocument = await solution.GetDocumentAsync(activeStatement.PrimaryDocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                    var (baseDocument, _) = await DebuggingSession.LastCommittedSolution.GetDocumentAndStateAsync(activeStatement.PrimaryDocumentId, primaryDocument, cancellationToken).ConfigureAwait(false);
                    if (baseDocument != null)
                    {
                        Debug.Assert(baseDocument.SupportsSyntaxTree);

                        var sourceText = await baseDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        var syntaxRoot = await baseDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                        Contract.ThrowIfNull(syntaxRoot);

                        // The analyzer service have to be available as we only track active statements in projects that support EnC.
                        var analyzer = baseDocument.Project.LanguageServices.GetRequiredService<IEditAndContinueAnalyzer>();
                        exceptionRegions = analyzer.GetExceptionRegions(sourceText, syntaxRoot, activeStatement.Span, activeStatement.IsNonLeaf, out isCovered);
                    }
                    else
                    {
                        // Document is either out-of-sync, design-time-only or missing from the baseline.
                        // If it's missing or design-time-only it can't have active statements.
                        hasOutOfSyncDocuments = true;
                        isCovered = false;
                        exceptionRegions = default;
                    }

                    builder[activeStatement.Ordinal] = new ActiveStatementExceptionRegions(exceptionRegions, isCovered);
                }

                var result = builder.ToImmutable();

                // Only cache results if no active statements are in out-of-sync documents.
                if (!hasOutOfSyncDocuments)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyBaseActiveExceptionRegions, result);
                }

                return result;
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
            {
                return ImmutableArray<ActiveStatementExceptionRegions>.Empty;
            }
        }

        private static async Task PopulateChangedAndAddedDocumentsAsync(CommittedSolution baseSolution, Project newProject, ArrayBuilder<Document> changedOrAddedDocuments, CancellationToken cancellationToken)
        {
            changedOrAddedDocuments.Clear();

            if (!EditAndContinueWorkspaceService.SupportsEditAndContinue(newProject))
            {
                return;
            }

            var oldProject = baseSolution.GetProject(newProject.Id);
            if (oldProject == newProject)
            {
                return;
            }

            // When debugging session is started some projects might not have been loaded to the workspace yet. 
            // We capture the base solution. Edits in files that are in projects that haven't been loaded won't be applied
            // and will result in source mismatch when the user steps into them.
            //
            // TODO (https://github.com/dotnet/roslyn/issues/1204):
            // hook up the debugger reported error, check that the project has not been loaded and report a better error.
            // Here, we assume these projects are not modified.
            if (oldProject == null)
            {
                EditAndContinueWorkspaceService.Log.Write("EnC state of '{0}' [0x{1:X8}] queried: project not loaded", newProject.Id.DebugName, newProject.Id);
                return;
            }

            foreach (var documentId in newProject.State.DocumentStates.GetChangedStateIds(oldProject.State.DocumentStates, ignoreUnchangedContent: true))
            {
                var document = newProject.GetDocument(documentId)!;
                if (document.State.Attributes.DesignTimeOnly)
                {
                    continue;
                }

                // Check if the currently observed document content has changed compared to the base document content.
                // This is an important optimization that aims to avoid IO while stepping in sources that have not changed.
                //
                // We may be comparing out-of-date committed document content but we only make a decision based on that content
                // if it matches the current content. If the current content is equal to baseline content that does not match
                // the debuggee then the workspace has not observed the change made to the file on disk since baseline was captured
                // (there had to be one as the content doesn't match). When we are about to apply changes it is ok to ignore this
                // document because the user does not see the change yet in the buffer (if the doc is open) and won't be confused
                // if it is not applied yet. The change will be applied later after it's observed by the workspace.
                var baseSource = await oldProject.GetDocument(documentId)!.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var source = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                if (baseSource.ContentEquals(source))
                {
                    continue;
                }

                changedOrAddedDocuments.Add(document);
            }

            foreach (var documentId in newProject.State.DocumentStates.GetAddedStateIds(oldProject.State.DocumentStates))
            {
                var document = newProject.GetDocument(documentId)!;
                if (document.State.Attributes.DesignTimeOnly)
                {
                    continue;
                }

                changedOrAddedDocuments.Add(document);
            }

            // TODO: support document removal/rename (see https://github.com/dotnet/roslyn/issues/41144, https://github.com/dotnet/roslyn/issues/49013).

            // Given the following assumptions:
            // - source generators are deterministic,
            // - source documents, metadata references and compilation options have not changed,
            // - additional documents have not changed,
            // - analyzer config documents have not changed,
            // the outputs of source generators will not change.
            // 
            // Currently it's not possible to change compilation options (Project System is readonly during debugging).
            // Source documents are checked above.

            if (changedOrAddedDocuments.IsEmpty() &&
                newProject.State.DocumentStates.GetRemovedStateIds(oldProject.State.DocumentStates).IsEmpty() &&
                !newProject.State.AdditionalDocumentStates.HasAnyStateChanges(oldProject.State.AdditionalDocumentStates) &&
                !newProject.State.AnalyzerConfigDocumentStates.HasAnyStateChanges(oldProject.State.AnalyzerConfigDocumentStates))
            {
                // Based on the above assumption there are no changes in source generated files.
                return;
            }

            var oldSourceGeneratedDocumentStates = await oldProject.Solution.State.GetSourceGeneratedDocumentStatesAsync(oldProject.State, cancellationToken).ConfigureAwait(false);
            var newSourceGeneratedDocumentStates = await newProject.Solution.State.GetSourceGeneratedDocumentStatesAsync(newProject.State, cancellationToken).ConfigureAwait(false);

            foreach (var documentId in newSourceGeneratedDocumentStates.GetChangedStateIds(oldSourceGeneratedDocumentStates, ignoreUnchangedContent: true))
            {
                var newState = newSourceGeneratedDocumentStates.GetRequiredState(documentId);
                if (newState.Attributes.DesignTimeOnly)
                {
                    continue;
                }

                changedOrAddedDocuments.Add(newProject.GetOrCreateSourceGeneratedDocument(newState));
            }

            foreach (var documentId in newSourceGeneratedDocumentStates.GetAddedStateIds(oldSourceGeneratedDocumentStates))
            {
                var newState = newSourceGeneratedDocumentStates.GetRequiredState(documentId);
                if (newState.Attributes.DesignTimeOnly)
                {
                    continue;
                }

                changedOrAddedDocuments.Add(newProject.GetOrCreateSourceGeneratedDocument(newState));
            }
        }

        private async Task<(ImmutableArray<DocumentAnalysisResults> results, ImmutableArray<Diagnostic> diagnostics)> AnalyzeDocumentsAsync(
            Project newProject,
            ArrayBuilder<Document> changedOrAddedDocuments,
            SolutionActiveStatementSpanProvider newDocumentActiveStatementSpanProvider,
            CancellationToken cancellationToken)
        {
            using var _1 = ArrayBuilder<Diagnostic>.GetInstance(out var documentDiagnostics);
            using var _2 = ArrayBuilder<(Document newDocument, ImmutableArray<TextSpan> newActiveStatementSpans)>.GetInstance(out var builder);

            foreach (var newDocument in changedOrAddedDocuments)
            {
                var (oldDocument, oldDocumentState) = await DebuggingSession.LastCommittedSolution.GetDocumentAndStateAsync(newDocument.Id, newDocument, cancellationToken, reloadOutOfSyncDocument: true).ConfigureAwait(false);
                switch (oldDocumentState)
                {
                    case CommittedSolution.DocumentState.DesignTimeOnly:
                        break;

                    case CommittedSolution.DocumentState.Indeterminate:
                    case CommittedSolution.DocumentState.OutOfSync:
                        var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor((oldDocumentState == CommittedSolution.DocumentState.Indeterminate) ?
                            EditAndContinueErrorCode.UnableToReadSourceFileOrPdb : EditAndContinueErrorCode.DocumentIsOutOfSyncWithDebuggee);
                        documentDiagnostics.Add(Diagnostic.Create(descriptor, Location.Create(newDocument.FilePath!, textSpan: default, lineSpan: default), new[] { newDocument.FilePath }));
                        break;

                    case CommittedSolution.DocumentState.MatchesBuildOutput:
                        // Include the document regardless of whether the module it was built into has been loaded or not.
                        // If the module has been built it might get loaded later during the debugging session,
                        // at which point we apply all changes that have been made to the project so far.

                        // Fetch the active statement spans for the new document snapshot.
                        // These are the locations of the spans tracked by the editor from the base document to the current snapshot.
                        var activeStatementSpans = await newDocumentActiveStatementSpanProvider(newDocument.Id, cancellationToken).ConfigureAwait(false);

                        builder.Add((newDocument, activeStatementSpans));
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(oldDocumentState);
                }
            }

            // The base project may have been updated as documents were brought up-to-date in the committed solution.
            // Get the latest available snapshot of the base project from the committed solution and use it for analyses of all documents,
            // so that we use a single compilation for the base project (for efficiency).
            // Note that some other request might be updating documents in the committed solution that were not changed (not in changedOrAddedDocuments)
            // but are not up-to-date. These documents do not have impact on the analysis unless we read semantic information
            // from the project compilation. When reading such information we need to be aware of its potential incompleteness
            // and consult the compiler output binary (see https://github.com/dotnet/roslyn/issues/51261).
            var oldProject = DebuggingSession.LastCommittedSolution.GetProject(newProject.Id);
            Contract.ThrowIfNull(oldProject);

            var analyses = await Analyses.GetDocumentAnalysesAsync(oldProject, builder, cancellationToken).ConfigureAwait(false);
            return (analyses, documentDiagnostics.ToImmutable());
        }

        internal ImmutableArray<DocumentId> GetDocumentsWithReportedDiagnostics()
        {
            lock (_documentsWithReportedDiagnosticsGuard)
            {
                return ImmutableArray.CreateRange(_documentsWithReportedDiagnostics);
            }
        }

        internal void TrackDocumentWithReportedDiagnostics(DocumentId documentId)
        {
            lock (_documentsWithReportedDiagnosticsGuard)
            {
                _documentsWithReportedDiagnostics.Add(documentId);
            }
        }

        /// <summary>
        /// Determines whether projects contain any changes that might need to be applied.
        /// Checks only projects containing a given <paramref name="sourceFilePath"/> or all projects of the solution if <paramref name="sourceFilePath"/> is null.
        /// Invoked by the debugger on every step. It is critical for stepping performance that this method returns as fast as possible in absence of changes.
        /// </summary>
        public async ValueTask<bool> HasChangesAsync(Solution solution, SolutionActiveStatementSpanProvider solutionActiveStatementSpanProvider, string? sourceFilePath, CancellationToken cancellationToken)
        {
            try
            {
                if (_changesApplied)
                {
                    return false;
                }

                var baseSolution = DebuggingSession.LastCommittedSolution;
                if (baseSolution.HasNoChanges(solution))
                {
                    return false;
                }

                // TODO: source generated files?
                var projects = (sourceFilePath == null) ? solution.Projects :
                    from documentId in solution.GetDocumentIdsWithFilePath(sourceFilePath)
                    select solution.GetDocument(documentId)!.Project;

                using var _ = ArrayBuilder<Document>.GetInstance(out var changedOrAddedDocuments);

                foreach (var project in projects)
                {
                    await PopulateChangedAndAddedDocumentsAsync(baseSolution, project, changedOrAddedDocuments, cancellationToken).ConfigureAwait(false);
                    if (changedOrAddedDocuments.IsEmpty())
                    {
                        continue;
                    }

                    // Check MVID before analyzing documents as the analysis needs to read the PDB which will likely fail if we can't even read the MVID.
                    var (mvid, mvidReadError) = await DebuggingSession.GetProjectModuleIdAsync(project, cancellationToken).ConfigureAwait(false);
                    if (mvidReadError != null)
                    {
                        // Can't read MVID. This might be an intermittent failure, so don't report it here.
                        // Report the project as containing changes, so that we proceed to EmitSolutionUpdateAsync where we report the error if it still persists.
                        EditAndContinueWorkspaceService.Log.Write("EnC state of '{0}' [0x{1:X8}] queried: project not built", project.Id.DebugName, project.Id);
                        return true;
                    }

                    if (mvid == Guid.Empty)
                    {
                        // Project not built. We ignore any changes made in its sources.
                        EditAndContinueWorkspaceService.Log.Write("EnC state of '{0}' [0x{1:X8}] queried: project not built", project.Id.DebugName, project.Id);
                        continue;
                    }

                    var (changedDocumentAnalyses, documentDiagnostics) = await AnalyzeDocumentsAsync(project, changedOrAddedDocuments, solutionActiveStatementSpanProvider, cancellationToken).ConfigureAwait(false);
                    if (documentDiagnostics.Any())
                    {
                        EditAndContinueWorkspaceService.Log.Write("EnC state of '{0}' [0x{1:X8}] queried: out-of-sync documents present (diagnostic: '{2}')",
                            project.Id.DebugName, project.Id, documentDiagnostics[0]);

                        // Although we do not apply changes in out-of-sync/indeterminate documents we report that changes are present,
                        // so that the debugger triggers emit of updates. There we check if these documents are still in a bad state and report warnings
                        // that any changes in such documents are not applied.
                        return true;
                    }

                    var projectSummary = GetProjectAnalysisSymmary(changedDocumentAnalyses);
                    if (projectSummary != ProjectAnalysisSummary.NoChanges)
                    {
                        EditAndContinueWorkspaceService.Log.Write("EnC state of '{0}' [0x{1:X8}] queried: {2}", project.Id.DebugName, project.Id, projectSummary);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static ProjectAnalysisSummary GetProjectAnalysisSymmary(ImmutableArray<DocumentAnalysisResults> documentAnalyses)
        {
            var hasChanges = false;
            var hasSignificantValidChanges = false;

            foreach (var analysis in documentAnalyses)
            {
                // skip documents that actually were not changed:
                if (!analysis.HasChanges)
                {
                    continue;
                }

                // rude edit detection wasn't completed due to errors that prevent us from analyzing the document:
                if (analysis.HasChangesAndSyntaxErrors)
                {
                    return ProjectAnalysisSummary.CompilationErrors;
                }

                // rude edits detected:
                if (!analysis.RudeEditErrors.IsEmpty)
                {
                    return ProjectAnalysisSummary.RudeEdits;
                }

                hasChanges = true;
                hasSignificantValidChanges |= analysis.HasSignificantValidChanges;
            }

            if (!hasChanges)
            {
                // we get here if a document is closed and reopen without any actual change:
                return ProjectAnalysisSummary.NoChanges;
            }

            if (!hasSignificantValidChanges)
            {
                return ProjectAnalysisSummary.ValidInsignificantChanges;
            }

            return ProjectAnalysisSummary.ValidChanges;
        }

        internal static ProjectChanges GetProjectChanges(
            Compilation oldCompilation,
            Compilation newCompilation,
            ImmutableArray<DocumentAnalysisResults> changedDocumentAnalyses,
            CancellationToken cancellationToken)
        {
            try
            {
                using var _1 = ArrayBuilder<SemanticEditInfo>.GetInstance(out var allEdits);
                using var _2 = ArrayBuilder<(DocumentId, ImmutableArray<SourceLineUpdate>)>.GetInstance(out var allLineEdits);
                using var _3 = ArrayBuilder<(DocumentId, ImmutableArray<ActiveStatement>, ImmutableArray<ImmutableArray<LinePositionSpan>>)>.GetInstance(out var activeStatementsInChangedDocuments);

                foreach (var analysis in changedDocumentAnalyses)
                {
                    if (!analysis.HasSignificantValidChanges)
                    {
                        continue;
                    }

                    // we shouldn't be asking for deltas in presence of errors:
                    Contract.ThrowIfTrue(analysis.HasChangesAndErrors);

                    allEdits.AddRange(analysis.SemanticEdits);

                    var documentId = analysis.DocumentId;

                    if (analysis.LineEdits.Length > 0)
                    {
                        allLineEdits.Add((documentId, analysis.LineEdits));
                    }

                    if (analysis.ActiveStatements.Length > 0)
                    {
                        activeStatementsInChangedDocuments.Add((documentId, analysis.ActiveStatements, analysis.ExceptionRegions));
                    }
                }

                MergePartialEdits(oldCompilation, newCompilation, allEdits, out var mergedEdits, out var addedSymbols, cancellationToken);

                return new ProjectChanges(
                    mergedEdits,
                    allLineEdits.ToImmutable(),
                    addedSymbols,
                    activeStatementsInChangedDocuments.ToImmutable());
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        internal static void MergePartialEdits(
            Compilation oldCompilation,
            Compilation newCompilation,
            IReadOnlyList<SemanticEditInfo> edits,
            out ImmutableArray<SemanticEdit> mergedEdits,
            out ImmutableHashSet<ISymbol> addedSymbols,
            CancellationToken cancellationToken)
        {
            using var _0 = ArrayBuilder<SemanticEdit>.GetInstance(edits.Count, out var mergedEditsBuilder);
            using var _1 = PooledHashSet<ISymbol>.GetInstance(out var addedSymbolsBuilder);
            using var _2 = ArrayBuilder<(ISymbol? oldSymbol, ISymbol newSymbol)>.GetInstance(edits.Count, out var resolvedSymbols);

            foreach (var edit in edits)
            {
                SymbolKeyResolution oldResolution;
                if (edit.Kind == SemanticEditKind.Update)
                {
                    oldResolution = edit.Symbol.Resolve(oldCompilation, ignoreAssemblyKey: true, cancellationToken);
                    Contract.ThrowIfNull(oldResolution.Symbol);
                }
                else
                {
                    oldResolution = default;
                }

                var newResolution = edit.Symbol.Resolve(newCompilation, ignoreAssemblyKey: true, cancellationToken);
                Contract.ThrowIfNull(newResolution.Symbol);

                resolvedSymbols.Add((oldResolution.Symbol, newResolution.Symbol));
            }

            for (var i = 0; i < edits.Count; i++)
            {
                var edit = edits[i];

                if (edit.PartialType == null)
                {
                    var (oldSymbol, newSymbol) = resolvedSymbols[i];

                    if (edit.Kind == SemanticEditKind.Insert)
                    {
                        // Inserts do not need partial type merging.
                        addedSymbolsBuilder.Add(newSymbol);
                    }

                    mergedEditsBuilder.Add(new SemanticEdit(
                        edit.Kind,
                        oldSymbol: oldSymbol,
                        newSymbol: newSymbol,
                        syntaxMap: edit.SyntaxMap,
                        preserveLocalVariables: edit.SyntaxMap != null));
                }
            }

            // no partial type merging needed:
            if (edits.Count == mergedEditsBuilder.Count)
            {
                mergedEdits = mergedEditsBuilder.ToImmutable();
                addedSymbols = addedSymbolsBuilder.ToImmutableHashSet();
                return;
            }

            // Calculate merged syntax map for each partial type symbol:

            var symbolKeyComparer = SymbolKey.GetComparer(ignoreCase: false, ignoreAssemblyKeys: true);
            var mergedSyntaxMaps = new Dictionary<SymbolKey, Func<SyntaxNode, SyntaxNode?>>(symbolKeyComparer);

            var editsByPartialType = edits
                .Where(edit => edit.PartialType != null)
                .GroupBy(edit => edit.PartialType!.Value, symbolKeyComparer);

            foreach (var partialTypeEdits in editsByPartialType)
            {
                Debug.Assert(partialTypeEdits.All(edit => edit.SyntaxMapTree != null && edit.SyntaxMap != null));

                var newTrees = partialTypeEdits.SelectAsArray(edit => edit.SyntaxMapTree!);
                var syntaxMaps = partialTypeEdits.SelectAsArray(edit => edit.SyntaxMap!);

                mergedSyntaxMaps.Add(partialTypeEdits.Key, node => syntaxMaps[newTrees.IndexOf(node.SyntaxTree)](node));
            }

            // Deduplicate updates based on new symbol and use merged syntax map calculated above for a given partial type.

            using var _3 = PooledHashSet<ISymbol>.GetInstance(out var visitedSymbols);

            for (var i = 0; i < edits.Count; i++)
            {
                var edit = edits[i];

                if (edit.PartialType != null)
                {
                    Contract.ThrowIfFalse(edit.Kind == SemanticEditKind.Update);

                    var (oldSymbol, newSymbol) = resolvedSymbols[i];
                    if (visitedSymbols.Add(newSymbol))
                    {
                        mergedEditsBuilder.Add(new SemanticEdit(SemanticEditKind.Update, oldSymbol, newSymbol, mergedSyntaxMaps[edit.PartialType.Value], preserveLocalVariables: true));
                    }
                }
            }

            mergedEdits = mergedEditsBuilder.ToImmutable();
            addedSymbols = addedSymbolsBuilder.ToImmutableHashSet();
        }

        public async Task<SolutionUpdate> EmitSolutionUpdateAsync(Solution solution, SolutionActiveStatementSpanProvider solutionActiveStatementSpanProvider, CancellationToken cancellationToken)
        {
            try
            {
                using var _1 = ArrayBuilder<ManagedModuleUpdate>.GetInstance(out var deltas);
                using var _2 = ArrayBuilder<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)>)>.GetInstance(out var nonRemappableRegions);
                using var _3 = ArrayBuilder<(ProjectId, EmitBaseline)>.GetInstance(out var emitBaselines);
                using var _4 = ArrayBuilder<IDisposable>.GetInstance(out var readers);
                using var _5 = ArrayBuilder<(ProjectId, ImmutableArray<Diagnostic>)>.GetInstance(out var diagnostics);
                using var _6 = ArrayBuilder<Document>.GetInstance(out var changedOrAddedDocuments);

                var oldSolution = DebuggingSession.LastCommittedSolution;

                var isBlocked = false;
                foreach (var newProject in solution.Projects)
                {
                    await PopulateChangedAndAddedDocumentsAsync(oldSolution, newProject, changedOrAddedDocuments, cancellationToken).ConfigureAwait(false);
                    if (changedOrAddedDocuments.IsEmpty())
                    {
                        continue;
                    }

                    var (mvid, mvidReadError) = await DebuggingSession.GetProjectModuleIdAsync(newProject, cancellationToken).ConfigureAwait(false);
                    if (mvidReadError != null)
                    {
                        // The error hasn't been reported by GetDocumentDiagnosticsAsync since it might have been intermittent.
                        // The MVID is required for emit so we consider the error permanent and report it here.
                        // Bail before analyzing documents as the analysis needs to read the PDB which will likely fail if we can't even read the MVID.
                        diagnostics.Add((newProject.Id, ImmutableArray.Create(mvidReadError)));

                        Telemetry.LogProjectAnalysisSummary(ProjectAnalysisSummary.ValidChanges, ImmutableArray.Create(mvidReadError.Descriptor.Id));
                        isBlocked = true;
                        continue;
                    }

                    if (mvid == Guid.Empty)
                    {
                        EditAndContinueWorkspaceService.Log.Write("Emitting update of '{0}' [0x{1:X8}]: project not built", newProject.Id.DebugName, newProject.Id);
                        continue;
                    }

                    // PopulateChangedAndAddedDocumentsAsync returns no changes if base project does not exist
                    var oldProject = oldSolution.GetProject(newProject.Id);
                    Contract.ThrowIfNull(oldProject);

                    // Ensure that all changed documents are in-sync. Once a document is in-sync it can't get out-of-sync.
                    // Therefore, results of further computations based on base snapshots of changed documents can't be invalidated by 
                    // incoming events updating the content of out-of-sync documents.
                    // 
                    // If in past we concluded that a document is out-of-sync, attempt to check one more time before we block apply.
                    // The source file content might have been updated since the last time we checked.
                    //
                    // TODO (investigate): https://github.com/dotnet/roslyn/issues/38866
                    // It is possible that the result of Rude Edit semantic analysis of an unchanged document will change if there
                    // another document is updated. If we encounter a significant case of this we should consider caching such a result per project,
                    // rather then per document. Also, we might be observing an older semantics if the document that is causing the change is out-of-sync --
                    // e.g. the binary was built with an overload C.M(object), but a generator updated class C to also contain C.M(string),
                    // which change we have not observed yet. Then call-sites of C.M in a changed document observed by the analysis will be seen as C.M(object) 
                    // instead of the true C.M(string).

                    var (changedDocumentAnalyses, documentDiagnostics) = await AnalyzeDocumentsAsync(newProject, changedOrAddedDocuments, solutionActiveStatementSpanProvider, cancellationToken).ConfigureAwait(false);
                    if (documentDiagnostics.Any())
                    {
                        // The diagnostic hasn't been reported by GetDocumentDiagnosticsAsync since out-of-sync documents are likely to be synchronized
                        // before the changes are attempted to be applied. If we still have any out-of-sync documents we report warnings and ignore changes in them.
                        // If in future the file is updated so that its content matches the PDB checksum, the document transitions to a matching state, 
                        // and we consider any further changes to it for application.
                        diagnostics.Add((newProject.Id, documentDiagnostics));
                    }

                    // The capability of a module to apply edits may change during edit session if the user attaches debugger to 
                    // an additional process that doesn't support EnC (or detaches from such process). Before we apply edits 
                    // we need to check with the debugger.
                    var (moduleDiagnostics, isModuleLoaded) = await GetModuleDiagnosticsAsync(mvid, newProject.Name, cancellationToken).ConfigureAwait(false);

                    var isModuleEncBlocked = isModuleLoaded && !moduleDiagnostics.IsEmpty;
                    if (isModuleEncBlocked)
                    {
                        diagnostics.Add((newProject.Id, moduleDiagnostics));
                        isBlocked = true;
                    }

                    var projectSummary = GetProjectAnalysisSymmary(changedDocumentAnalyses);
                    if (projectSummary == ProjectAnalysisSummary.CompilationErrors || projectSummary == ProjectAnalysisSummary.RudeEdits)
                    {
                        isBlocked = true;
                    }

                    if (isModuleEncBlocked || projectSummary != ProjectAnalysisSummary.ValidChanges)
                    {
                        Telemetry.LogProjectAnalysisSummary(projectSummary, moduleDiagnostics.NullToEmpty().SelectAsArray(d => d.Descriptor.Id));
                        continue;
                    }

                    if (!DebuggingSession.TryGetOrCreateEmitBaseline(newProject, readers, out var createBaselineDiagnostics, out var baseline))
                    {
                        Debug.Assert(!createBaselineDiagnostics.IsEmpty);

                        // Report diagnosics even when the module is never going to be loaded (e.g. in multi-targeting scenario, where only one framework being debugged).
                        // This is consistent with reporting compilation errors - the IDE reports them for all TFMs regardless of what framework the app is running on.
                        diagnostics.Add((newProject.Id, createBaselineDiagnostics));
                        Telemetry.LogProjectAnalysisSummary(projectSummary, createBaselineDiagnostics);
                        isBlocked = true;
                        continue;
                    }

                    EditAndContinueWorkspaceService.Log.Write("Emitting update of '{0}' [0x{1:X8}]", newProject.Id.DebugName, newProject.Id);

                    var oldCompilation = await oldProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var newCompilation = await newProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    Contract.ThrowIfNull(oldCompilation);
                    Contract.ThrowIfNull(newCompilation);

                    var projectChanges = GetProjectChanges(oldCompilation, newCompilation, changedDocumentAnalyses, cancellationToken);
                    var oldActiveStatements = await BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);

                    // Exception regions of active statements in changed documents are calculated (non-default),
                    // since we already checked that no changed document is out-of-sync above.
                    var oldActiveExceptionRegions = await GetBaseActiveExceptionRegionsAsync(solution, cancellationToken).ConfigureAwait(false);

                    var lineEdits = projectChanges.LineChanges.SelectAsArray((lineChange, project) =>
                    {
                        var filePath = project.GetDocument(lineChange.DocumentId)!.FilePath;
                        Contract.ThrowIfNull(filePath);
                        return new SequencePointUpdates(filePath, lineChange.Changes);
                    }, newProject);

                    using var pdbStream = SerializableBytes.CreateWritableStream();
                    using var metadataStream = SerializableBytes.CreateWritableStream();
                    using var ilStream = SerializableBytes.CreateWritableStream();

                    var updatedMethods = ImmutableArray.CreateBuilder<MethodDefinitionHandle>();

                    // project must support compilations since it supports EnC
                    Contract.ThrowIfNull(newCompilation);

                    var emitResult = newCompilation.EmitDifference(
                        baseline,
                        projectChanges.SemanticEdits,
                        projectChanges.AddedSymbols.Contains,
                        metadataStream,
                        ilStream,
                        pdbStream,
                        updatedMethods,
                        cancellationToken);

                    if (emitResult.Success)
                    {
                        var updatedMethodTokens = updatedMethods.SelectAsArray(h => MetadataTokens.GetToken(h));

                        // Determine all active statements whose span changed and exception region span deltas.
                        GetActiveStatementAndExceptionRegionSpans(
                            mvid,
                            oldActiveStatements,
                            oldActiveExceptionRegions,
                            updatedMethodTokens,
                            _nonRemappableRegions,
                            projectChanges.NewActiveStatements,
                            out var activeStatementsInUpdatedMethods,
                            out var moduleNonRemappableRegions,
                            out var exceptionRegionUpdates);

                        deltas.Add(new ManagedModuleUpdate(
                            mvid,
                            ilStream.ToImmutableArray(),
                            metadataStream.ToImmutableArray(),
                            pdbStream.ToImmutableArray(),
                            lineEdits,
                            updatedMethodTokens,
                            activeStatementsInUpdatedMethods,
                            exceptionRegionUpdates));

                        nonRemappableRegions.Add((mvid, moduleNonRemappableRegions));
                        emitBaselines.Add((newProject.Id, emitResult.Baseline));
                    }
                    else
                    {
                        // error
                        isBlocked = true;
                    }

                    // TODO: https://github.com/dotnet/roslyn/issues/36061
                    // We should only report diagnostics from emit phase.
                    // Syntax and semantic diagnostics are already reported by the diagnostic analyzer.
                    // Currently we do not have means to distinguish between diagnostics reported from compilation and emit phases.
                    // Querying diagnostics of the entire compilation or just the updated files migth be slow.
                    // In fact, it is desirable to allow emitting deltas for symbols affected by the change while allowing untouched
                    // method bodies to have errors.
                    if (!emitResult.Diagnostics.IsEmpty)
                    {
                        diagnostics.Add((newProject.Id, emitResult.Diagnostics));
                    }

                    Telemetry.LogProjectAnalysisSummary(projectSummary, emitResult.Diagnostics);
                }

                if (isBlocked)
                {
                    foreach (var reader in readers)
                    {
                        reader.Dispose();
                    }

                    return SolutionUpdate.Blocked(diagnostics.ToImmutable());
                }

                return new SolutionUpdate(
                    new ManagedModuleUpdates(
                        (deltas.Count > 0) ? ManagedModuleUpdateStatus.Ready : ManagedModuleUpdateStatus.None,
                        deltas.ToImmutable()),
                    nonRemappableRegions.ToImmutable(),
                    readers.ToImmutable(),
                    emitBaselines.ToImmutable(),
                    diagnostics.ToImmutable());
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        // internal for testing
        internal static void GetActiveStatementAndExceptionRegionSpans(
            Guid moduleId,
            ActiveStatementsMap baseActiveStatements,
            ImmutableArray<ActiveStatementExceptionRegions> baseActiveExceptionRegions,
            ImmutableArray<int> updatedMethodTokens,
            ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>> previousNonRemappableRegions,
            ImmutableArray<(DocumentId DocumentId, ImmutableArray<ActiveStatement> ActiveStatements, ImmutableArray<ImmutableArray<LinePositionSpan>> ExceptionRegions)> newActiveStatementsInChangedDocuments,
            out ImmutableArray<ManagedActiveStatementUpdate> activeStatementsInUpdatedMethods,
            out ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)> nonRemappableRegions,
            out ImmutableArray<ManagedExceptionRegionUpdate> exceptionRegionUpdates)
        {
            using var _1 = PooledDictionary<(ManagedModuleMethodId MethodId, LinePositionSpan BaseSpan), LinePositionSpan>.GetInstance(out var changedNonRemappableSpans);
            var activeStatementsInUpdatedMethodsBuilder = ArrayBuilder<ManagedActiveStatementUpdate>.GetInstance();
            var nonRemappableRegionsBuilder = ArrayBuilder<(ManagedModuleMethodId Method, NonRemappableRegion Region)>.GetInstance();

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
                    var oldMethodId = oldInstructionId.Method.Method;

                    var isMethodUpdated = updatedMethodTokens.Contains(oldMethodId.Token);
                    if (isMethodUpdated)
                    {
                        activeStatementsInUpdatedMethodsBuilder.Add(new ManagedActiveStatementUpdate(oldMethodId, oldInstructionId.ILOffset, newActiveStatement.Span.ToSourceSpan()));
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
                                nonRemappableRegionsBuilder.Add((oldMethodId, new NonRemappableRegion(oldSpan, lineDelta, isExceptionRegion)));
                            }

                            // If the method has been up-to-date and it is not updated now then either the active statement span has not changed,
                            // or the entire method containing it moved. In neither case do we need to start tracking non-remapable region
                            // for the active statement since movement of whole method bodies (if any) is handled only on PDB level without 
                            // triggering any remapping on the IL level.
                        }
                        else if (oldSpan != newSpan)
                        {
                            // The method is not up-to-date hence we maintain non-remapable span map for it that needs to be updated.
                            changedNonRemappableSpans[(oldMethodId, oldSpan)] = newSpan;
                        }
                    }

                    AddNonRemappableRegion(oldActiveStatement.Span, newActiveStatement.Span, isExceptionRegion: false);

                    // The spans of the exception regions are known (non-default) for active statements in changed documents
                    // as we ensured earlier that all changed documents are in-sync. The outer loop only enumerates active 
                    // statements of changed documents, so the corresponding exception regions are initialized.

                    var j = 0;
                    foreach (var oldSpan in baseActiveExceptionRegions[oldActiveStatement.Ordinal].Spans)
                    {
                        AddNonRemappableRegion(oldSpan, newExceptionRegions[oldActiveStatement.PrimaryDocumentOrdinal][j++], isExceptionRegion: true);
                    }
                }
            }

            activeStatementsInUpdatedMethods = activeStatementsInUpdatedMethodsBuilder.ToImmutableAndFree();

            // Gather all active method instances contained in this project/module that are not up-to-date:
            using var _2 = PooledHashSet<ManagedModuleMethodId>.GetInstance(out var unremappedActiveMethods);
            foreach (var (instruction, baseActiveStatement) in baseActiveStatements.InstructionMap)
            {
                if (moduleId == instruction.Method.Module && !baseActiveStatement.IsMethodUpToDate)
                {
                    unremappedActiveMethods.Add(instruction.Method.Method);
                }
            }

            if (unremappedActiveMethods.Count > 0)
            {
                foreach (var (methodInstance, regionsInMethod) in previousNonRemappableRegions)
                {
                    if (methodInstance.Module != moduleId)
                    {
                        continue;
                    }

                    // Skip non-remappable regions that belong to method instances that are from a different module 
                    // or no longer active (all active statements in these method instances have been remapped to newer versions).
                    if (!unremappedActiveMethods.Contains(methodInstance.Method))
                    {
                        continue;
                    }

                    foreach (var region in regionsInMethod)
                    {
                        // We have calculated changes against a base snapshot (last break state):
                        var baseSpan = region.Span.AddLineDelta(region.LineDelta);

                        NonRemappableRegion newRegion;
                        if (changedNonRemappableSpans.TryGetValue((methodInstance.Method, baseSpan), out var newSpan))
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

                        nonRemappableRegionsBuilder.Add((methodInstance.Method, newRegion));
                    }
                }
            }

            nonRemappableRegions = nonRemappableRegionsBuilder.ToImmutableAndFree();

            // The range span in exception region updates is the new span. Deltas are inverse.
            //   old = new + delta
            //   new = old – delta
            exceptionRegionUpdates = nonRemappableRegions.SelectAsArray(
                r => r.Region.IsExceptionRegion,
                r => new ManagedExceptionRegionUpdate(
                    r.Method,
                    -r.Region.LineDelta,
                    r.Region.Span.AddLineDelta(r.Region.LineDelta).ToSourceSpan()));
        }

        internal void StorePendingUpdate(Solution solution, SolutionUpdate update)
        {
            var previousPendingUpdate = Interlocked.Exchange(ref _pendingUpdate, new PendingSolutionUpdate(
                solution,
                update.EmitBaselines,
                update.ModuleUpdates.Updates,
                update.NonRemappableRegions,
                update.ModuleReaders));

            // commit/discard was not called:
            Contract.ThrowIfFalse(previousPendingUpdate == null);
        }

        internal PendingSolutionUpdate RetrievePendingUpdate()
        {
            var pendingUpdate = Interlocked.Exchange(ref _pendingUpdate, null);
            Contract.ThrowIfNull(pendingUpdate);
            return pendingUpdate;
        }

        internal void ChangesApplied()
        {
            Debug.Assert(!_changesApplied);
            _changesApplied = true;
        }
    }
}
