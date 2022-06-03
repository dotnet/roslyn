// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.EditAndContinue.Contracts;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class EditSession
    {
        internal readonly DebuggingSession DebuggingSession;
        internal readonly EditSessionTelemetry Telemetry;

        // Maps active statement instructions reported by the debugger to their latest spans that might not yet have been applied
        // (remapping not triggered yet). Consumed by the next edit session and updated when changes are committed at the end of the edit session.
        //
        // Consider a function F containing a call to function G. While G is being executed, F is updated a couple of times (in two edit sessions)
        // before the thread returns from G and is remapped to the latest version of F. At the start of the second edit session,
        // the active instruction reported by the debugger is still at the original location since function F has not been remapped yet (G has not returned yet).
        //
        // '>' indicates an active statement instruction for non-leaf frame reported by the debugger.
        // v1 - before first edit, G executing
        // v2 - after first edit, G still executing
        // v3 - after second edit and G returned
        //
        // F v1:        F v2:       F v3:
        // 0: nop       0: nop      0: nop
        // 1> G()       1> nop      1: nop
        // 2: nop       2: G()      2: nop
        // 3: nop       3: nop      3> G()
        //
        // When entering a break state we query the debugger for current active statements.
        // The returned statements reflect the current state of the threads in the runtime.
        // When a change is successfully applied we remember changes in active statement spans.
        // These changes are passed to the next edit session.
        // We use them to map the spans for active statements returned by the debugger.
        //
        // In the above case the sequence of events is
        // 1st break: get active statements returns (F, v=1, il=1, span1) the active statement is up-to-date
        // 1st apply: detected span change for active statement (F, v=1, il=1): span1->span2
        // 2nd break: previously updated statements contains (F, v=1, il=1)->span2
        //            get active statements returns (F, v=1, il=1, span1) which is mapped to (F, v=1, il=1, span2) using previously updated statements
        // 2nd apply: detected span change for active statement (F, v=1, il=1): span2->span3
        // 3rd break: previously updated statements contains (F, v=1, il=1)->span3
        //            get active statements returns (F, v=3, il=3, span3) the active statement is up-to-date
        //
        internal readonly ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>> NonRemappableRegions;

        /// <summary>
        /// Gets the capabilities of the runtime with respect to applying code changes.
        /// Retrieved lazily from <see cref="DebuggingSession.DebuggerService"/> since they are only needed when changes are detected in the solution.
        /// </summary>
        internal readonly AsyncLazy<EditAndContinueCapabilities> Capabilities;

        /// <summary>
        /// Map of base active statements.
        /// Calculated lazily based on info retrieved from <see cref="DebuggingSession.DebuggerService"/> since it is only needed when changes are detected in the solution.
        /// </summary>
        internal readonly AsyncLazy<ActiveStatementsMap> BaseActiveStatements;

        /// <summary>
        /// Cache of document EnC analyses. 
        /// </summary>
        internal readonly EditAndContinueDocumentAnalysesCache Analyses;

        /// <summary>
        /// True for Edit and Continue edit sessions - when the application is in break state.
        /// False for Hot Reload edit sessions - when the application is running.
        /// </summary>
        internal readonly bool InBreakState;

        /// <summary>
        /// A <see cref="DocumentId"/> is added whenever EnC analyzer reports 
        /// rude edits or module diagnostics. At the end of the session we ask the diagnostic analyzer to reanalyze 
        /// the documents to clean up the diagnostics.
        /// </summary>
        private readonly HashSet<DocumentId> _documentsWithReportedDiagnostics = new();
        private readonly object _documentsWithReportedDiagnosticsGuard = new();

        internal EditSession(
            DebuggingSession debuggingSession,
            ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>> nonRemappableRegions,
            EditSessionTelemetry telemetry,
            AsyncLazy<ActiveStatementsMap>? lazyActiveStatementMap,
            bool inBreakState)
        {
            DebuggingSession = debuggingSession;
            NonRemappableRegions = nonRemappableRegions;
            Telemetry = telemetry;
            InBreakState = inBreakState;

            telemetry.SetBreakState(inBreakState);

            BaseActiveStatements = lazyActiveStatementMap ?? (inBreakState ?
                new AsyncLazy<ActiveStatementsMap>(GetBaseActiveStatementsAsync, cacheResult: true) :
                new AsyncLazy<ActiveStatementsMap>(ActiveStatementsMap.Empty));

            Capabilities = new AsyncLazy<EditAndContinueCapabilities>(GetCapabilitiesAsync, cacheResult: true);
            Analyses = new EditAndContinueDocumentAnalysesCache(BaseActiveStatements, Capabilities);
        }

        /// <summary>
        /// The compiler has various scenarios that will cause it to synthesize things that might not be covered
        /// by existing rude edits, but we still need to ensure the runtime supports them before we proceed.
        /// </summary>
        private async Task<Diagnostic?> GetUnsupportedChangesDiagnosticAsync(EmitDifferenceResult emitResult, CancellationToken cancellationToken)
        {
            Debug.Assert(emitResult.Success);
            Debug.Assert(emitResult.Baseline is not null);

            // if there were no changed types then there is nothing to check
            if (emitResult.ChangedTypes.Length == 0)
            {
                return null;
            }

            var capabilities = await Capabilities.GetValueAsync(cancellationToken).ConfigureAwait(false);
            if (!capabilities.HasFlag(EditAndContinueCapabilities.NewTypeDefinition))
            {
                // If the runtime doesn't support adding new types then we expect every row number for any type that is
                // emitted will be less than or equal to the number of rows in the original metadata.
                var highestEmittedTypeDefRow = emitResult.ChangedTypes.Max(t => MetadataTokens.GetRowNumber(t));
                var highestExistingTypeDefRow = emitResult.Baseline.OriginalMetadata.GetMetadataReader().GetTableRowCount(TableIndex.TypeDef);

                if (highestEmittedTypeDefRow > highestExistingTypeDefRow)
                {
                    var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.AddingTypeRuntimeCapabilityRequired);
                    return Diagnostic.Create(descriptor, Location.None);
                }
            }

            return null;
        }

        /// <summary>
        /// Errors to be reported when a project is updated but the corresponding module does not support EnC.
        /// </summary>
        /// <returns><see langword="default"/> if the module is not loaded.</returns>
        public async Task<ImmutableArray<Diagnostic>?> GetModuleDiagnosticsAsync(Guid mvid, Project oldProject, Project newProject, ImmutableArray<DocumentAnalysisResults> documentAnalyses, CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(documentAnalyses.IsEmpty);

            var availability = await DebuggingSession.DebuggerService.GetAvailabilityAsync(mvid, cancellationToken).ConfigureAwait(false);
            if (availability.Status == ManagedHotReloadAvailabilityStatus.ModuleNotLoaded)
            {
                return null;
            }

            if (availability.Status == ManagedHotReloadAvailabilityStatus.Available)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var descriptor = EditAndContinueDiagnosticDescriptors.GetModuleDiagnosticDescriptor(availability.Status);
            var messageArgs = new[] { newProject.Name, availability.LocalizedMessage };

            using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var diagnostics);

            await foreach (var location in CreateChangedLocationsAsync(oldProject, newProject, documentAnalyses, cancellationToken).ConfigureAwait(false))
            {
                diagnostics.Add(Diagnostic.Create(descriptor, location, messageArgs));
            }

            return diagnostics.ToImmutable();
        }

        private static async IAsyncEnumerable<Location> CreateChangedLocationsAsync(Project oldProject, Project newProject, ImmutableArray<DocumentAnalysisResults> documentAnalyses, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var hasRemovedOrAddedDocument = false;
            foreach (var documentAnalysis in documentAnalyses)
            {
                if (!documentAnalysis.HasChanges)
                {
                    continue;
                }

                var oldDocument = await oldProject.GetDocumentAsync(documentAnalysis.DocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                var newDocument = await newProject.GetDocumentAsync(documentAnalysis.DocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                if (oldDocument == null || newDocument == null)
                {
                    hasRemovedOrAddedDocument = true;
                    continue;
                }

                var oldText = await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var newText = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var newTree = await newDocument.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                // document location:
                yield return Location.Create(newTree, GetFirstLineDifferenceSpan(oldText, newText));
            }

            // project location:
            if (hasRemovedOrAddedDocument)
            {
                yield return Location.None;
            }
        }

        private static TextSpan GetFirstLineDifferenceSpan(SourceText oldText, SourceText newText)
        {
            var oldLineCount = oldText.Lines.Count;
            var newLineCount = newText.Lines.Count;

            for (var i = 0; i < Math.Min(oldLineCount, newLineCount); i++)
            {
                var oldLineSpan = oldText.Lines[i].Span;
                var newLineSpan = newText.Lines[i].Span;
                if (oldLineSpan != newLineSpan || !oldText.GetSubText(oldLineSpan).ContentEquals(newText.GetSubText(newLineSpan)))
                {
                    return newText.Lines[i].Span;
                }
            }

            return (oldLineCount == newLineCount) ? default :
                   (newLineCount > oldLineCount) ? newText.Lines[oldLineCount].Span :
                   TextSpan.FromBounds(newText.Lines[newLineCount - 1].End, newText.Lines[newLineCount - 1].EndIncludingLineBreak);
        }

        private async Task<EditAndContinueCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken)
        {
            try
            {
                var capabilities = await DebuggingSession.DebuggerService.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
                return EditAndContinueCapabilitiesParser.Parse(capabilities);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return EditAndContinueCapabilities.Baseline;
            }
        }

        private async Task<ActiveStatementsMap> GetBaseActiveStatementsAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Last committed solution reflects the state of the source that is in sync with the binaries that are loaded in the debuggee.
                var debugInfos = await DebuggingSession.DebuggerService.GetActiveStatementsAsync(cancellationToken).ConfigureAwait(false);
                return ActiveStatementsMap.Create(debugInfos, NonRemappableRegions);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return ActiveStatementsMap.Empty;
            }
        }

        private static async Task PopulateChangedAndAddedDocumentsAsync(CommittedSolution oldSolution, Project newProject, ArrayBuilder<Document> changedOrAddedDocuments, CancellationToken cancellationToken)
        {
            changedOrAddedDocuments.Clear();

            if (!newProject.SupportsEditAndContinue())
            {
                return;
            }

            var oldProject = oldSolution.GetProject(newProject.Id);

            if (oldProject == null)
            {
                EditAndContinueWorkspaceService.Log.Write("EnC state of '{0}' [0x{1:X8}] queried: project not loaded", newProject.Id.DebugName, newProject.Id);

                // TODO (https://github.com/dotnet/roslyn/issues/1204):
                //
                // When debugging session is started some projects might not have been loaded to the workspace yet (may be explicitly unloaded by the user).
                // We capture the base solution. Edits in files that are in projects that haven't been loaded won't be applied
                // and will result in source mismatch when the user steps into them.
                //
                // We can allow project to be added by including all its documents here.
                // When we analyze these documents later on we'll check if they match the PDB.
                // If so we can add them to the committed solution and detect further changes.
                // It might be more efficient though to track added projects separately.

                return;
            }

            if (oldProject.State == newProject.State)
            {
                return;
            }

            foreach (var documentId in newProject.State.DocumentStates.GetChangedStateIds(oldProject.State.DocumentStates, ignoreUnchangedContent: true))
            {
                var document = newProject.GetRequiredDocument(documentId);
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
                var baseSource = await oldProject.GetRequiredDocument(documentId).GetTextAsync(cancellationToken).ConfigureAwait(false);
                var source = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                if (baseSource.ContentEquals(source))
                {
                    continue;
                }

                changedOrAddedDocuments.Add(document);
            }

            foreach (var documentId in newProject.State.DocumentStates.GetAddedStateIds(oldProject.State.DocumentStates))
            {
                var document = newProject.GetRequiredDocument(documentId);
                if (document.State.Attributes.DesignTimeOnly)
                {
                    continue;
                }

                changedOrAddedDocuments.Add(document);
            }

            // TODO: support document removal/rename (see https://github.com/dotnet/roslyn/issues/41144, https://github.com/dotnet/roslyn/issues/49013).
            if (changedOrAddedDocuments.IsEmpty() && !HasChangesThatMayAffectSourceGenerators(oldProject.State, newProject.State))
            {
                // Based on the above assumption there are no changes in source generated files.
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var oldSourceGeneratedDocumentStates = await oldProject.Solution.State.GetSourceGeneratedDocumentStatesAsync(oldProject.State, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

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

        internal static async IAsyncEnumerable<DocumentId> GetChangedDocumentsAsync(Project oldProject, Project newProject, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Debug.Assert(oldProject.Id == newProject.Id);

            if (!newProject.SupportsEditAndContinue() || oldProject.State == newProject.State)
            {
                yield break;
            }

            foreach (var documentId in newProject.State.DocumentStates.GetChangedStateIds(oldProject.State.DocumentStates, ignoreUnchangedContent: true))
            {
                yield return documentId;
            }

            if (!HasChangesThatMayAffectSourceGenerators(oldProject.State, newProject.State))
            {
                // Based on the above assumption there are no changes in source generated files.
                yield break;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var oldSourceGeneratedDocumentStates = await oldProject.Solution.State.GetSourceGeneratedDocumentStatesAsync(oldProject.State, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var newSourceGeneratedDocumentStates = await newProject.Solution.State.GetSourceGeneratedDocumentStatesAsync(newProject.State, cancellationToken).ConfigureAwait(false);

            foreach (var documentId in newSourceGeneratedDocumentStates.GetChangedStateIds(oldSourceGeneratedDocumentStates, ignoreUnchangedContent: true))
            {
                yield return documentId;
            }
        }

        /// <summary>
        /// Given the following assumptions:
        /// - source generators are deterministic,
        /// - source documents, metadata references and compilation options have not changed,
        /// - additional documents have not changed,
        /// - analyzer config documents have not changed,
        /// the outputs of source generators will not change.
        /// 
        /// Currently it's not possible to change compilation options (Project System is readonly during debugging).
        /// </summary>
        private static bool HasChangesThatMayAffectSourceGenerators(ProjectState oldProject, ProjectState newProject)
            => newProject.DocumentStates.HasAnyStateChanges(oldProject.DocumentStates) ||
               newProject.AdditionalDocumentStates.HasAnyStateChanges(oldProject.AdditionalDocumentStates) ||
               newProject.AnalyzerConfigDocumentStates.HasAnyStateChanges(oldProject.AnalyzerConfigDocumentStates);

        private async Task<(ImmutableArray<DocumentAnalysisResults> results, ImmutableArray<Diagnostic> diagnostics)> AnalyzeDocumentsAsync(
            ArrayBuilder<Document> changedOrAddedDocuments,
            ActiveStatementSpanProvider newDocumentActiveStatementSpanProvider,
            CancellationToken cancellationToken)
        {
            using var _1 = ArrayBuilder<Diagnostic>.GetInstance(out var documentDiagnostics);
            using var _2 = ArrayBuilder<(Document? oldDocument, Document newDocument)>.GetInstance(out var documents);

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

                        documents.Add((oldDocument, newDocument));
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(oldDocumentState);
                }
            }

            var analyses = await Analyses.GetDocumentAnalysesAsync(DebuggingSession.LastCommittedSolution, documents, newDocumentActiveStatementSpanProvider, cancellationToken).ConfigureAwait(false);
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
        public async ValueTask<bool> HasChangesAsync(Solution solution, ActiveStatementSpanProvider solutionActiveStatementSpanProvider, string? sourceFilePath, CancellationToken cancellationToken)
        {
            try
            {
                var baseSolution = DebuggingSession.LastCommittedSolution;
                if (baseSolution.HasNoChanges(solution))
                {
                    return false;
                }

                // TODO: source generated files?
                var projects = (sourceFilePath == null) ? solution.Projects :
                    from documentId in solution.GetDocumentIdsWithFilePath(sourceFilePath)
                    select solution.GetProject(documentId.ProjectId)!;

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

                    var (changedDocumentAnalyses, documentDiagnostics) = await AnalyzeDocumentsAsync(changedOrAddedDocuments, solutionActiveStatementSpanProvider, cancellationToken).ConfigureAwait(false);
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
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
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

        internal static async ValueTask<ProjectChanges> GetProjectChangesAsync(
            ActiveStatementsMap baseActiveStatements,
            Compilation oldCompilation,
            Compilation newCompilation,
            Project oldProject,
            Project newProject,
            ImmutableArray<DocumentAnalysisResults> changedDocumentAnalyses,
            CancellationToken cancellationToken)
        {
            try
            {
                using var _1 = ArrayBuilder<SemanticEditInfo>.GetInstance(out var allEdits);
                using var _2 = ArrayBuilder<SequencePointUpdates>.GetInstance(out var allLineEdits);
                using var _3 = ArrayBuilder<DocumentActiveStatementChanges>.GetInstance(out var activeStatementsInChangedDocuments);

                var analyzer = newProject.LanguageServices.GetRequiredService<IEditAndContinueAnalyzer>();
                var requiredCapabilities = EditAndContinueCapabilities.None;

                foreach (var analysis in changedDocumentAnalyses)
                {
                    if (!analysis.HasSignificantValidChanges)
                    {
                        continue;
                    }

                    // we shouldn't be asking for deltas in presence of errors:
                    Contract.ThrowIfTrue(analysis.HasChangesAndErrors);

                    // Active statements are calculated if document changed and has no syntax errors:
                    Contract.ThrowIfTrue(analysis.ActiveStatements.IsDefault);

                    allEdits.AddRange(analysis.SemanticEdits);
                    allLineEdits.AddRange(analysis.LineEdits);
                    requiredCapabilities |= analysis.RequiredCapabilities;

                    if (analysis.ActiveStatements.Length > 0)
                    {
                        var oldDocument = await oldProject.GetDocumentAsync(analysis.DocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

                        var oldActiveStatements = (oldDocument == null) ? ImmutableArray<UnmappedActiveStatement>.Empty :
                            await baseActiveStatements.GetOldActiveStatementsAsync(analyzer, oldDocument, cancellationToken).ConfigureAwait(false);

                        activeStatementsInChangedDocuments.Add(new(oldActiveStatements, analysis.ActiveStatements, analysis.ExceptionRegions));
                    }
                }

                MergePartialEdits(oldCompilation, newCompilation, allEdits, out var mergedEdits, out var addedSymbols, cancellationToken);

                return new ProjectChanges(
                    mergedEdits,
                    allLineEdits.ToImmutable(),
                    addedSymbols,
                    activeStatementsInChangedDocuments.ToImmutable(),
                    requiredCapabilities);
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
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
            using var _2 = ArrayBuilder<(ISymbol? oldSymbol, ISymbol? newSymbol)>.GetInstance(edits.Count, out var resolvedSymbols);

            foreach (var edit in edits)
            {
                SymbolKeyResolution oldResolution;
                if (edit.Kind is SemanticEditKind.Update or SemanticEditKind.Delete)
                {
                    oldResolution = edit.Symbol.Resolve(oldCompilation, ignoreAssemblyKey: true, cancellationToken);
                    Contract.ThrowIfNull(oldResolution.Symbol);
                }
                else
                {
                    oldResolution = default;
                }

                SymbolKeyResolution newResolution;
                if (edit.Kind is SemanticEditKind.Update or SemanticEditKind.Insert or SemanticEditKind.Replace)
                {
                    newResolution = edit.Symbol.Resolve(newCompilation, ignoreAssemblyKey: true, cancellationToken);
                    Contract.ThrowIfNull(newResolution.Symbol);
                }
                else
                {
                    newResolution = default;
                }

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
                        Contract.ThrowIfNull(newSymbol);
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
            var mergedSyntaxMaps = new Dictionary<SymbolKey, Func<SyntaxNode, SyntaxNode?>?>(symbolKeyComparer);

            var editsByPartialType = edits
                .Where(edit => edit.PartialType != null)
                .GroupBy(edit => edit.PartialType!.Value, symbolKeyComparer);

            foreach (var partialTypeEdits in editsByPartialType)
            {
                // Either all edits have syntax map or none has.
                Debug.Assert(
                    partialTypeEdits.All(edit => edit.SyntaxMapTree != null && edit.SyntaxMap != null) ||
                    partialTypeEdits.All(edit => edit.SyntaxMapTree == null && edit.SyntaxMap == null));

                Func<SyntaxNode, SyntaxNode?>? mergedSyntaxMap;
                if (partialTypeEdits.First().SyntaxMap != null)
                {
                    var newTrees = partialTypeEdits.SelectAsArray(edit => edit.SyntaxMapTree!);
                    var syntaxMaps = partialTypeEdits.SelectAsArray(edit => edit.SyntaxMap!);
                    mergedSyntaxMap = node => syntaxMaps[newTrees.IndexOf(node.SyntaxTree)](node);
                }
                else
                {
                    mergedSyntaxMap = null;
                }

                mergedSyntaxMaps.Add(partialTypeEdits.Key, mergedSyntaxMap);
            }

            // Deduplicate edits based on their target symbol and use merged syntax map calculated above for a given partial type.

            using var _3 = PooledHashSet<ISymbol>.GetInstance(out var visitedSymbols);

            for (var i = 0; i < edits.Count; i++)
            {
                var edit = edits[i];

                if (edit.PartialType != null)
                {
                    var (oldSymbol, newSymbol) = resolvedSymbols[i];
                    if (visitedSymbols.Add(newSymbol ?? oldSymbol!))
                    {
                        var syntaxMap = mergedSyntaxMaps[edit.PartialType.Value];
                        mergedEditsBuilder.Add(new SemanticEdit(edit.Kind, oldSymbol, newSymbol, syntaxMap, preserveLocalVariables: syntaxMap != null));
                    }
                }
            }

            mergedEdits = mergedEditsBuilder.ToImmutable();
            addedSymbols = addedSymbolsBuilder.ToImmutableHashSet();
        }

        public async ValueTask<SolutionUpdate> EmitSolutionUpdateAsync(Solution solution, ActiveStatementSpanProvider solutionActiveStatementSpanProvider, CancellationToken cancellationToken)
        {
            try
            {
                using var _1 = ArrayBuilder<ManagedModuleUpdate>.GetInstance(out var deltas);
                using var _2 = ArrayBuilder<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)>)>.GetInstance(out var nonRemappableRegions);
                using var _3 = ArrayBuilder<(ProjectId, EmitBaseline)>.GetInstance(out var emitBaselines);
                using var _4 = ArrayBuilder<(ProjectId, ImmutableArray<Diagnostic>)>.GetInstance(out var diagnostics);
                using var _5 = ArrayBuilder<Document>.GetInstance(out var changedOrAddedDocuments);
                using var _6 = ArrayBuilder<(DocumentId, ImmutableArray<RudeEditDiagnostic>)>.GetInstance(out var documentsWithRudeEdits);
                Diagnostic? syntaxError = null;

                var oldSolution = DebuggingSession.LastCommittedSolution;

                var isBlocked = false;
                var hasEmitErrors = false;
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

                        Telemetry.LogProjectAnalysisSummary(ProjectAnalysisSummary.ValidChanges, newProject.State.ProjectInfo.Attributes.TelemetryId, ImmutableArray.Create(mvidReadError.Descriptor.Id));
                        isBlocked = true;
                        continue;
                    }

                    if (mvid == Guid.Empty)
                    {
                        EditAndContinueWorkspaceService.Log.Write("Emitting update of '{0}' [0x{1:X8}]: project not built", newProject.Id.DebugName, newProject.Id);
                        continue;
                    }

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

                    var (changedDocumentAnalyses, documentDiagnostics) = await AnalyzeDocumentsAsync(changedOrAddedDocuments, solutionActiveStatementSpanProvider, cancellationToken).ConfigureAwait(false);
                    if (documentDiagnostics.Any())
                    {
                        // The diagnostic hasn't been reported by GetDocumentDiagnosticsAsync since out-of-sync documents are likely to be synchronized
                        // before the changes are attempted to be applied. If we still have any out-of-sync documents we report warnings and ignore changes in them.
                        // If in future the file is updated so that its content matches the PDB checksum, the document transitions to a matching state, 
                        // and we consider any further changes to it for application.
                        diagnostics.Add((newProject.Id, documentDiagnostics));
                    }

                    var projectSummary = GetProjectAnalysisSymmary(changedDocumentAnalyses);
                    if (projectSummary == ProjectAnalysisSummary.NoChanges)
                    {
                        continue;
                    }

                    // PopulateChangedAndAddedDocumentsAsync returns no changes if base project does not exist
                    var oldProject = oldSolution.GetProject(newProject.Id);
                    Contract.ThrowIfNull(oldProject);

                    // The capability of a module to apply edits may change during edit session if the user attaches debugger to 
                    // an additional process that doesn't support EnC (or detaches from such process). Before we apply edits 
                    // we need to check with the debugger.
                    var (moduleDiagnostics, isModuleLoaded) = await GetModuleDiagnosticsAsync(mvid, oldProject, newProject, changedDocumentAnalyses, cancellationToken).ConfigureAwait(false);

                    var isModuleEncBlocked = isModuleLoaded && !moduleDiagnostics.IsEmpty;
                    if (isModuleEncBlocked)
                    {
                        diagnostics.Add((newProject.Id, moduleDiagnostics));
                        isBlocked = true;
                    }

                    if (projectSummary == ProjectAnalysisSummary.CompilationErrors)
                    {
                        // only remember the first syntax error we encounter:
                        syntaxError ??= changedDocumentAnalyses.FirstOrDefault(a => a.SyntaxError != null)?.SyntaxError;
                        isBlocked = true;
                    }
                    else if (projectSummary == ProjectAnalysisSummary.RudeEdits)
                    {
                        foreach (var analysis in changedDocumentAnalyses)
                        {
                            if (analysis.RudeEditErrors.Length > 0)
                            {
                                documentsWithRudeEdits.Add((analysis.DocumentId, analysis.RudeEditErrors));
                                Telemetry.LogRudeEditDiagnostics(analysis.RudeEditErrors);
                            }
                        }

                        isBlocked = true;
                    }

                    if (isModuleEncBlocked || projectSummary != ProjectAnalysisSummary.ValidChanges)
                    {
                        Telemetry.LogProjectAnalysisSummary(projectSummary, newProject.State.ProjectInfo.Attributes.TelemetryId, moduleDiagnostics.NullToEmpty().SelectAsArray(d => d.Descriptor.Id));
                        continue;
                    }

                    if (!DebuggingSession.TryGetOrCreateEmitBaseline(newProject, out var createBaselineDiagnostics, out var baseline, out var baselineAccessLock))
                    {
                        Debug.Assert(!createBaselineDiagnostics.IsEmpty);

                        // Report diagnosics even when the module is never going to be loaded (e.g. in multi-targeting scenario, where only one framework being debugged).
                        // This is consistent with reporting compilation errors - the IDE reports them for all TFMs regardless of what framework the app is running on.
                        diagnostics.Add((newProject.Id, createBaselineDiagnostics));
                        Telemetry.LogProjectAnalysisSummary(projectSummary, newProject.State.ProjectInfo.Attributes.TelemetryId, createBaselineDiagnostics);
                        isBlocked = true;
                        continue;
                    }

                    EditAndContinueWorkspaceService.Log.Write("Emitting update of '{0}' [0x{1:X8}]", newProject.Id.DebugName, newProject.Id);

                    var oldCompilation = await oldProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var newCompilation = await newProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    Contract.ThrowIfNull(oldCompilation);
                    Contract.ThrowIfNull(newCompilation);

                    var oldActiveStatementsMap = await BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
                    var projectChanges = await GetProjectChangesAsync(oldActiveStatementsMap, oldCompilation, newCompilation, oldProject, newProject, changedDocumentAnalyses, cancellationToken).ConfigureAwait(false);

                    using var pdbStream = SerializableBytes.CreateWritableStream();
                    using var metadataStream = SerializableBytes.CreateWritableStream();
                    using var ilStream = SerializableBytes.CreateWritableStream();

                    // project must support compilations since it supports EnC
                    Contract.ThrowIfNull(newCompilation);

                    EmitDifferenceResult emitResult;

                    // The lock protects underlying baseline readers from being disposed while emitting delta.
                    // If the lock is disposed at this point the session has been incorrectly disposed while operations on it are in progress.
                    using (baselineAccessLock.DisposableRead())
                    {
                        DebuggingSession.ThrowIfDisposed();

                        emitResult = newCompilation.EmitDifference(
                            baseline,
                            projectChanges.SemanticEdits,
                            projectChanges.AddedSymbols.Contains,
                            metadataStream,
                            ilStream,
                            pdbStream,
                            cancellationToken);
                    }

                    if (emitResult.Success)
                    {
                        Contract.ThrowIfNull(emitResult.Baseline);

                        var unsupportedChangesDiagnostic = await GetUnsupportedChangesDiagnosticAsync(emitResult, cancellationToken).ConfigureAwait(false);
                        if (unsupportedChangesDiagnostic is not null)
                        {
                            diagnostics.Add((newProject.Id, ImmutableArray.Create(unsupportedChangesDiagnostic)));
                            isBlocked = true;
                        }
                        else
                        {
                            var updatedMethodTokens = emitResult.UpdatedMethods.SelectAsArray(h => MetadataTokens.GetToken(h));
                            var changedTypeTokens = emitResult.ChangedTypes.SelectAsArray(h => MetadataTokens.GetToken(h));

                            // Determine all active statements whose span changed and exception region span deltas.
                            GetActiveStatementAndExceptionRegionSpans(
                                mvid,
                                oldActiveStatementsMap,
                                updatedMethodTokens,
                                NonRemappableRegions,
                                projectChanges.ActiveStatementChanges,
                                out var activeStatementsInUpdatedMethods,
                                out var moduleNonRemappableRegions,
                                out var exceptionRegionUpdates);

                            deltas.Add(new ManagedModuleUpdate(
                                mvid,
                                ilStream.ToImmutableArray(),
                                metadataStream.ToImmutableArray(),
                                pdbStream.ToImmutableArray(),
                                projectChanges.LineChanges,
                                updatedMethodTokens,
                                changedTypeTokens,
                                activeStatementsInUpdatedMethods,
                                exceptionRegionUpdates,
                                projectChanges.RequiredCapabilities));

                            nonRemappableRegions.Add((mvid, moduleNonRemappableRegions));
                            emitBaselines.Add((newProject.Id, emitResult.Baseline));
                        }
                    }
                    else
                    {
                        // error
                        isBlocked = hasEmitErrors = true;
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

                    Telemetry.LogProjectAnalysisSummary(projectSummary, newProject.State.ProjectInfo.Attributes.TelemetryId, emitResult.Diagnostics);
                }

                // log capabilities for edit sessions with changes or reported errors:
                if (isBlocked || deltas.Count > 0)
                {
                    Telemetry.LogRuntimeCapabilities(await Capabilities.GetValueAsync(cancellationToken).ConfigureAwait(false));
                }

                var update = isBlocked ?
                    SolutionUpdate.Blocked(diagnostics.ToImmutable(), documentsWithRudeEdits.ToImmutable(), syntaxError, hasEmitErrors) :
                    new SolutionUpdate(
                        new ManagedModuleUpdates(
                            (deltas.Count > 0) ? ManagedModuleUpdateStatus.Ready : ManagedModuleUpdateStatus.None,
                            deltas.ToImmutable()),
                        nonRemappableRegions.ToImmutable(),
                        emitBaselines.ToImmutable(),
                        diagnostics.ToImmutable(),
                        documentsWithRudeEdits.ToImmutable(),
                        syntaxError);

                return update;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        // internal for testing
        internal static void GetActiveStatementAndExceptionRegionSpans(
            Guid moduleId,
            ActiveStatementsMap oldActiveStatementMap,
            ImmutableArray<int> updatedMethodTokens,
            ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>> previousNonRemappableRegions,
            ImmutableArray<DocumentActiveStatementChanges> activeStatementsInChangedDocuments,
            out ImmutableArray<ManagedActiveStatementUpdate> activeStatementsInUpdatedMethods,
            out ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)> nonRemappableRegions,
            out ImmutableArray<ManagedExceptionRegionUpdate> exceptionRegionUpdates)
        {
            using var _1 = PooledDictionary<(ManagedModuleMethodId MethodId, SourceFileSpan BaseSpan), SourceFileSpan>.GetInstance(out var changedNonRemappableSpans);
            var activeStatementsInUpdatedMethodsBuilder = ArrayBuilder<ManagedActiveStatementUpdate>.GetInstance();
            var nonRemappableRegionsBuilder = ArrayBuilder<(ManagedModuleMethodId Method, NonRemappableRegion Region)>.GetInstance();

            // Process active statements and their exception regions in changed documents of this project/module:
            foreach (var (oldActiveStatements, newActiveStatements, newExceptionRegions) in activeStatementsInChangedDocuments)
            {
                Debug.Assert(oldActiveStatements.Length == newExceptionRegions.Length);
                Debug.Assert(newActiveStatements.Length == newExceptionRegions.Length);

                for (var i = 0; i < newActiveStatements.Length; i++)
                {
                    var (_, oldActiveStatement, oldActiveStatementExceptionRegions) = oldActiveStatements[i];
                    var newActiveStatement = newActiveStatements[i];
                    var newActiveStatementExceptionRegions = newExceptionRegions[i];

                    var instructionId = newActiveStatement.InstructionId;
                    var methodId = instructionId.Method.Method;

                    var isMethodUpdated = updatedMethodTokens.Contains(methodId.Token);
                    if (isMethodUpdated)
                    {
                        activeStatementsInUpdatedMethodsBuilder.Add(new ManagedActiveStatementUpdate(methodId, instructionId.ILOffset, newActiveStatement.Span.ToSourceSpan()));
                    }

                    Debug.Assert(!oldActiveStatement.IsStale);

                    // Adds a region with specified PDB spans.
                    void AddNonRemappableRegion(SourceFileSpan oldSpan, SourceFileSpan newSpan, bool isExceptionRegion)
                    {
                        // it is a rude edit to change the path of the region span:
                        Debug.Assert(oldSpan.Path == newSpan.Path);

                        // The up-to-date flag is copied when new active statement is created from the corresponding old one.
                        Debug.Assert(oldActiveStatement.IsMethodUpToDate == newActiveStatement.IsMethodUpToDate);

                        if (oldActiveStatement.IsMethodUpToDate)
                        {
                            // Start tracking non-remappable regions for active statements in methods that were up-to-date 
                            // when break state was entered and now being updated (regardless of whether the active span changed or not).
                            if (isMethodUpdated)
                            {
                                nonRemappableRegionsBuilder.Add((methodId, new NonRemappableRegion(oldSpan, newSpan, isExceptionRegion)));
                            }
                            else if (!isExceptionRegion)
                            {
                                // If the method has been up-to-date and it is not updated now then either the active statement span has not changed,
                                // or the entire method containing it moved. In neither case do we need to start tracking non-remapable region
                                // for the active statement since movement of whole method bodies (if any) is handled only on PDB level without 
                                // triggering any remapping on the IL level.
                                //
                                // That said, we still add a non-remappable region for this active statement, so that we know in future sessions
                                // that this active statement existed and its span has not changed. We don't report these regions to the debugger,
                                // but we use them to map active statement spans to the baseline snapshots of following edit sessions.
                                nonRemappableRegionsBuilder.Add((methodId, new NonRemappableRegion(oldSpan, oldSpan, isExceptionRegion: false)));
                            }
                        }
                        else if (oldSpan.Span != newSpan.Span)
                        {
                            // The method is not up-to-date hence we might have a previous non-remappable span mapping that needs to be brought forward to the new snapshot.
                            changedNonRemappableSpans[(methodId, oldSpan)] = newSpan;
                        }
                    }

                    AddNonRemappableRegion(oldActiveStatement.FileSpan, newActiveStatement.FileSpan, isExceptionRegion: false);

                    // The spans of the exception regions are known (non-default) for active statements in changed documents
                    // as we ensured earlier that all changed documents are in-sync.

                    for (var j = 0; j < oldActiveStatementExceptionRegions.Spans.Length; j++)
                    {
                        AddNonRemappableRegion(oldActiveStatementExceptionRegions.Spans[j], newActiveStatementExceptionRegions[j], isExceptionRegion: true);
                    }
                }
            }

            activeStatementsInUpdatedMethods = activeStatementsInUpdatedMethodsBuilder.ToImmutableAndFree();

            // Gather all active method instances contained in this project/module that are not up-to-date:
            using var _2 = PooledHashSet<ManagedModuleMethodId>.GetInstance(out var unremappedActiveMethods);
            foreach (var (instruction, baseActiveStatement) in oldActiveStatementMap.InstructionMap)
            {
                if (moduleId == instruction.Method.Module && !baseActiveStatement.IsMethodUpToDate)
                {
                    unremappedActiveMethods.Add(instruction.Method.Method);
                }
            }

            // Update previously calculated non-remappable region mappings.
            // These map to the old snapshot and we need them to map to the new snapshot, which will be the baseline for the next session.
            if (unremappedActiveMethods.Count > 0)
            {
                foreach (var (methodInstance, regionsInMethod) in previousNonRemappableRegions)
                {
                    // Skip non-remappable regions that belong to method instances that are from a different module.
                    if (methodInstance.Module != moduleId)
                    {
                        continue;
                    }

                    // Skip no longer active methods - all active statements in these method instances have been remapped to newer versions.
                    // New active statement can't appear in a stale method instance since such instance can't be invoked.
                    if (!unremappedActiveMethods.Contains(methodInstance.Method))
                    {
                        continue;
                    }

                    foreach (var region in regionsInMethod)
                    {
                        // We have calculated changes against a base snapshot (last break state):
                        var baseSpan = region.NewSpan;

                        NonRemappableRegion newRegion;
                        if (changedNonRemappableSpans.TryGetValue((methodInstance.Method, baseSpan), out var newSpan))
                        {
                            // all spans must be of the same size:
                            Debug.Assert(newSpan.Span.End.Line - newSpan.Span.Start.Line == baseSpan.Span.End.Line - baseSpan.Span.Start.Line);
                            Debug.Assert(region.OldSpan.Span.End.Line - region.OldSpan.Span.Start.Line == baseSpan.Span.End.Line - baseSpan.Span.Start.Line);
                            Debug.Assert(newSpan.Path == region.OldSpan.Path);

                            newRegion = region.WithNewSpan(newSpan);
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

            // Note: https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1319289
            //
            // The update should include the file name, otherwise it is not possible for the debugger to find 
            // the right IL span of the exception handler in case when multiple handlers in the same method
            // have the same mapped span but different mapped file name:
            //
            //   try { active statement }
            //   #line 20 "bar"
            //   catch (IOException) { }
            //   #line 20 "baz"
            //   catch (Exception) { }
            //
            // The range span in exception region updates is the new span. Deltas are inverse.
            //   old = new + delta
            //   new = old – delta
            exceptionRegionUpdates = nonRemappableRegions.SelectAsArray(
                r => r.Region.IsExceptionRegion,
                r => new ManagedExceptionRegionUpdate(
                    r.Method,
                    -r.Region.OldSpan.Span.GetLineDelta(r.Region.NewSpan.Span),
                    r.Region.NewSpan.Span.ToSourceSpan()));
        }
    }
}
