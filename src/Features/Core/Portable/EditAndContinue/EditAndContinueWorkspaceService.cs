// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Implements core of Edit and Continue orchestration: management of edit sessions and connecting EnC related services.
    /// </summary>
    internal sealed class EditAndContinueWorkspaceService : IEditAndContinueWorkspaceService
    {
        [ExportWorkspaceServiceFactory(typeof(IEditAndContinueWorkspaceService)), Shared]
        private sealed class Factory : IWorkspaceServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory()
            {
            }

            [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
            public IWorkspaceService? CreateService(HostWorkspaceServices workspaceServices)
                => new EditAndContinueWorkspaceService();
        }

        internal static readonly TraceLog Log = new(2048, "EnC");

        private readonly EditSessionTelemetry _editSessionTelemetry;
        private readonly DebuggingSessionTelemetry _debuggingSessionTelemetry;
        private Func<Project, CompilationOutputs> _compilationOutputsProvider;
        private Action<DebuggingSessionTelemetry.Data> _reportTelemetry;

        private DebuggingSession? _debuggingSession;

        internal EditAndContinueWorkspaceService()
        {
            _debuggingSessionTelemetry = new DebuggingSessionTelemetry();
            _editSessionTelemetry = new EditSessionTelemetry();
            _compilationOutputsProvider = GetCompilationOutputs;
            _reportTelemetry = ReportTelemetry;
        }

        private static CompilationOutputs GetCompilationOutputs(Project project)
        {
            // The Project System doesn't always indicate whether we emit PDB, what kind of PDB we emit nor the path of the PDB.
            // To work around we look for the PDB on the path specified in the PDB debug directory.
            // https://github.com/dotnet/roslyn/issues/35065
            return new CompilationOutputFilesWithImplicitPdbPath(project.CompilationOutputInfo.AssemblyPath);
        }

        public void OnSourceFileUpdated(Document document)
        {
            var debuggingSession = _debuggingSession;
            if (debuggingSession != null)
            {
                // fire and forget
                _ = Task.Run(() => debuggingSession.LastCommittedSolution.OnSourceFileUpdatedAsync(document, debuggingSession.CancellationToken));
            }
        }

        public async ValueTask StartDebuggingSessionAsync(Solution solution, IManagedEditAndContinueDebuggerService debuggerService, bool captureMatchingDocuments, CancellationToken cancellationToken)
        {
            var initialDocumentStates =
                captureMatchingDocuments ? await CommittedSolution.GetMatchingDocumentsAsync(solution, _compilationOutputsProvider, cancellationToken).ConfigureAwait(false) :
                SpecializedCollections.EmptyEnumerable<KeyValuePair<DocumentId, CommittedSolution.DocumentState>>();

            var runtimeCapabilities = await debuggerService.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);

            var capabilities = ParseCapabilities(runtimeCapabilities);

            // For now, runtimes aren't returning capabilities, we just fall back to a known set.
            if (capabilities == EditAndContinueCapabilities.None)
            {
                capabilities = EditAndContinueCapabilities.Baseline | EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddStaticFieldToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType | EditAndContinueCapabilities.NewTypeDefinition;
            }

            var newSession = new DebuggingSession(solution, debuggerService, capabilities, _compilationOutputsProvider, initialDocumentStates, _debuggingSessionTelemetry, _editSessionTelemetry);
            var previousSession = Interlocked.CompareExchange(ref _debuggingSession, newSession, null);
            Contract.ThrowIfFalse(previousSession == null, "New debugging session can't be started until the existing one has ended.");
        }

        // internal for testing
        internal static EditAndContinueCapabilities ParseCapabilities(ImmutableArray<string> capabilities)
        {
            var caps = EditAndContinueCapabilities.None;

            foreach (var capability in capabilities)
            {
                caps |= capability switch
                {
                    "Baseline" => EditAndContinueCapabilities.Baseline,
                    "AddMethodToExistingType" => EditAndContinueCapabilities.AddMethodToExistingType,
                    "AddStaticFieldToExistingType" => EditAndContinueCapabilities.AddStaticFieldToExistingType,
                    "AddInstanceFieldToExistingType" => EditAndContinueCapabilities.AddInstanceFieldToExistingType,
                    "NewTypeDefinition" => EditAndContinueCapabilities.NewTypeDefinition,

                    // To make it eaiser for  runtimes to specify more broad capabilities
                    "AddDefinitionToExistingType" => EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddStaticFieldToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType,

                    _ => EditAndContinueCapabilities.None
                };
            }

            return caps;
        }
        public void EndDebuggingSession(out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            var debuggingSession = Interlocked.Exchange(ref _debuggingSession, null);
            Contract.ThrowIfNull(debuggingSession, "Debugging session has not started.");

            debuggingSession.EndSession(out documentsToReanalyze, out var telemetryData);

            _reportTelemetry(telemetryData);
        }

        internal void RestartEditSession(bool inBreakState, out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            var debuggingSession = _debuggingSession;
            Contract.ThrowIfNull(debuggingSession);

            // Document analyses must be recalculated to account for active statements.
            debuggingSession.RestartEditSession(inBreakState, out documentsToReanalyze);
        }

        public void BreakStateEntered(out ImmutableArray<DocumentId> documentsToReanalyze)
            => RestartEditSession(inBreakState: true, out documentsToReanalyze);

        internal static bool SupportsEditAndContinue(Project project)
            => project.LanguageServices.GetService<IEditAndContinueAnalyzer>() != null;

        // Note: source generated files have relative paths: https://github.com/dotnet/roslyn/issues/51998
        internal static bool SupportsEditAndContinue(TextDocumentState documentState)
            => !documentState.Attributes.DesignTimeOnly &&
               documentState is not DocumentState or DocumentState { SupportsSyntaxTree: true } &&
               (PathUtilities.IsAbsolute(documentState.FilePath) || documentState is SourceGeneratedDocumentState);

        public async ValueTask<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        {
            try
            {
                var debuggingSession = _debuggingSession;
                if (debuggingSession == null)
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                // Not a C# or VB project.
                var project = document.Project;
                if (!SupportsEditAndContinue(project))
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                // Document does not compile to the assembly (e.g. cshtml files, .g.cs files generated for completion only)
                if (!SupportsEditAndContinue(document.DocumentState))
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                // Do not analyze documents (and report diagnostics) of projects that have not been built.
                // Allow user to make any changes in these documents, they won't be applied within the current debugging session.
                // Do not report the file read error - it might be an intermittent issue. The error will be reported when the
                // change is attempted to be applied.
                var (mvid, _) = await debuggingSession.GetProjectModuleIdAsync(project, cancellationToken).ConfigureAwait(false);
                if (mvid == Guid.Empty)
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                var (oldDocument, oldDocumentState) = await debuggingSession.LastCommittedSolution.GetDocumentAndStateAsync(document.Id, document, cancellationToken).ConfigureAwait(false);
                if (oldDocumentState is CommittedSolution.DocumentState.OutOfSync or
                    CommittedSolution.DocumentState.Indeterminate or
                    CommittedSolution.DocumentState.DesignTimeOnly)
                {
                    // Do not report diagnostics for existing out-of-sync documents or design-time-only documents.
                    return ImmutableArray<Diagnostic>.Empty;
                }

                var editSession = debuggingSession.EditSession;
                var analysis = await editSession.Analyses.GetDocumentAnalysisAsync(debuggingSession.LastCommittedSolution, oldDocument, document, activeStatementSpanProvider, debuggingSession.Capabilities, cancellationToken).ConfigureAwait(false);
                if (analysis.HasChanges)
                {
                    // Once we detected a change in a document let the debugger know that the corresponding loaded module
                    // is about to be updated, so that it can start initializing it for EnC update, reducing the amount of time applying
                    // the change blocks the UI when the user "continues".
                    if (debuggingSession.AddModulePreparedForUpdate(mvid))
                    {
                        // fire and forget:
                        _ = Task.Run(() => debuggingSession.DebuggerService.PrepareModuleForUpdateAsync(mvid, cancellationToken), cancellationToken);
                    }
                }

                if (analysis.RudeEditErrors.IsEmpty)
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                editSession.Telemetry.LogRudeEditDiagnostics(analysis.RudeEditErrors);

                // track the document, so that we can refresh or clean diagnostics at the end of edit session:
                editSession.TrackDocumentWithReportedDiagnostics(document.Id);

                var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                return analysis.RudeEditErrors.SelectAsArray((e, t) => e.ToDiagnostic(t), tree);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return ImmutableArray<Diagnostic>.Empty;
            }
        }

        /// <summary>
        /// Determine whether the updates made to projects containing the specified file (or all projects that are built,
        /// if <paramref name="sourceFilePath"/> is null) are ready to be applied and the debugger should attempt to apply
        /// them on "continue".
        /// </summary>
        /// <returns>
        /// Returns <see cref="ManagedModuleUpdateStatus.Blocked"/> if there are rude edits or other errors
        /// that block the application of the updates. Might return <see cref="ManagedModuleUpdateStatus.Ready"/> even if there are
        /// errors in the code that will block the application of the updates. E.g. emit diagnostics can't be determined until
        /// emit is actually performed. Therefore, this method only serves as an optimization to avoid unnecessary emit attempts,
        /// but does not provide a definitive answer. Only <see cref="EmitSolutionUpdateAsync"/> can definitively determine whether
        /// the update is valid or not.
        /// </returns>
        public ValueTask<bool> HasChangesAsync(Solution solution, ActiveStatementSpanProvider activeStatementSpanProvider, string? sourceFilePath, CancellationToken cancellationToken)
        {
            // GetStatusAsync is called outside of edit session when the debugger is determining
            // whether a source file checksum matches the one in PDB.
            // The debugger expects no changes in this case.
            var debuggingSession = _debuggingSession;
            if (debuggingSession == null)
            {
                return default;
            }

            return debuggingSession.EditSession.HasChangesAsync(solution, activeStatementSpanProvider, sourceFilePath, cancellationToken);
        }

        public async ValueTask<EmitSolutionUpdateResults> EmitSolutionUpdateAsync(
            Solution solution,
            ActiveStatementSpanProvider activeStatementSpanProvider,
            CancellationToken cancellationToken)
        {
            var debuggingSession = _debuggingSession;
            if (debuggingSession == null)
            {
                return EmitSolutionUpdateResults.Empty;
            }

            var solutionUpdate = await debuggingSession.EditSession.EmitSolutionUpdateAsync(solution, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
            if (solutionUpdate.ModuleUpdates.Status == ManagedModuleUpdateStatus.Ready)
            {
                debuggingSession.EditSession.StorePendingUpdate(solution, solutionUpdate);
            }

            // Note that we may return empty deltas if all updates have been deferred.
            // The debugger will still call commit or discard on the update batch.
            return new EmitSolutionUpdateResults(solutionUpdate.ModuleUpdates, solutionUpdate.Diagnostics, solutionUpdate.DocumentsWithRudeEdits);
        }

        public void CommitSolutionUpdate(out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            var debuggingSession = _debuggingSession;
            Contract.ThrowIfNull(debuggingSession);

            var pendingUpdate = debuggingSession.EditSession.RetrievePendingUpdate();
            debuggingSession.CommitSolutionUpdate(pendingUpdate);

            // restart edit session with no active statements (switching to run mode):
            RestartEditSession(inBreakState: false, out documentsToReanalyze);
        }

        public void DiscardSolutionUpdate()
        {
            var debuggingSession = _debuggingSession;
            Contract.ThrowIfNull(debuggingSession);

            _ = debuggingSession.EditSession.RetrievePendingUpdate();
        }

        public async ValueTask<ImmutableArray<ImmutableArray<ActiveStatementSpan>>> GetBaseActiveStatementSpansAsync(Solution solution, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            var debuggingSession = _debuggingSession;
            if (debuggingSession == null || !debuggingSession.EditSession.InBreakState)
            {
                return default;
            }

            var lastCommittedSolution = debuggingSession.LastCommittedSolution;
            var baseActiveStatements = await debuggingSession.EditSession.BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
            using var _1 = PooledDictionary<string, ArrayBuilder<(ProjectId, int)>>.GetInstance(out var documentIndicesByMappedPath);
            using var _2 = PooledHashSet<ProjectId>.GetInstance(out var projectIds);

            // Construct map of mapped file path to a text document in the current solution
            // and a set of projects these documents are contained in.
            for (var i = 0; i < documentIds.Length; i++)
            {
                var documentId = documentIds[i];

                var document = await solution.GetTextDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
                if (document?.FilePath == null)
                {
                    // document has been deleted or has no path (can't have an active statement anymore):
                    continue;
                }

                // Multiple documents may have the same path (linked file).
                // The documents represent the files that #line directives map to.
                // Documents that have the same path must have different project id.
                documentIndicesByMappedPath.MultiAdd(document.FilePath, (documentId.ProjectId, i));
                projectIds.Add(documentId.ProjectId);
            }

            using var _3 = PooledDictionary<ActiveStatement, ArrayBuilder<(DocumentId unmappedDocumentId, LinePositionSpan span)>>.GetInstance(
                out var activeStatementsInChangedDocuments);

            // Analyze changed documents in projects containing active statements:
            foreach (var projectId in projectIds)
            {
                var newProject = solution.GetRequiredProject(projectId);
                var analyzer = newProject.LanguageServices.GetRequiredService<IEditAndContinueAnalyzer>();

                await foreach (var documentId in EditSession.GetChangedDocumentsAsync(lastCommittedSolution, newProject, cancellationToken).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var newDocument = await solution.GetRequiredDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

                    var (oldDocument, _) = await lastCommittedSolution.GetDocumentAndStateAsync(newDocument.Id, newDocument, cancellationToken).ConfigureAwait(false);
                    if (oldDocument == null)
                    {
                        // Document is out-of-sync, can't reason about its content with respect to the binaries loaded in the debuggee.
                        continue;
                    }

                    var oldDocumentActiveStatements = await baseActiveStatements.GetOldActiveStatementsAsync(analyzer, oldDocument, cancellationToken).ConfigureAwait(false);

                    var analysis = await analyzer.AnalyzeDocumentAsync(
                        lastCommittedSolution.GetRequiredProject(documentId.ProjectId),
                        baseActiveStatements,
                        newDocument,
                        newActiveStatementSpans: ImmutableArray<LinePositionSpan>.Empty,
                        debuggingSession.Capabilities,
                        cancellationToken).ConfigureAwait(false);

                    // Document content did not change or unable to determine active statement spans in a document with syntax errors:
                    if (!analysis.ActiveStatements.IsDefault)
                    {
                        for (var i = 0; i < oldDocumentActiveStatements.Length; i++)
                        {
                            // Note: It is possible that one active statement appears in multiple documents if the documents represent a linked file.
                            // Example (old and new contents):
                            //   #if Condition       #if Condition
                            //     #line 1 a.txt       #line 1 a.txt
                            //     [|F(1);|]           [|F(1000);|]     
                            //   #else               #else
                            //     #line 1 a.txt       #line 1 a.txt
                            //     [|F(2);|]           [|F(2);|]
                            //   #endif              #endif
                            // 
                            // In the new solution the AS spans are different depending on which document view of the same file we are looking at.
                            // Different views correspond to different projects.
                            activeStatementsInChangedDocuments.MultiAdd(oldDocumentActiveStatements[i].Statement, (analysis.DocumentId, analysis.ActiveStatements[i].Span));
                        }
                    }
                }
            }

            using var _4 = ArrayBuilder<ImmutableArray<ActiveStatementSpan>>.GetInstance(out var spans);
            spans.AddMany(ImmutableArray<ActiveStatementSpan>.Empty, documentIds.Length);

            foreach (var (mappedPath, documentBaseActiveStatements) in baseActiveStatements.DocumentPathMap)
            {
                if (documentIndicesByMappedPath.TryGetValue(mappedPath, out var indices))
                {
                    // translate active statements from base solution to the new solution, if the documents they are contained in changed:
                    foreach (var (projectId, index) in indices)
                    {
                        spans[index] = documentBaseActiveStatements.SelectAsArray(
                            activeStatement =>
                            {
                                LinePositionSpan span;
                                DocumentId? unmappedDocumentId;

                                if (activeStatementsInChangedDocuments.TryGetValue(activeStatement, out var newSpans))
                                {
                                    (unmappedDocumentId, span) = newSpans.Single(ns => ns.unmappedDocumentId.ProjectId == projectId);
                                }
                                else
                                {
                                    span = activeStatement.Span;
                                    unmappedDocumentId = null;
                                }

                                return new ActiveStatementSpan(activeStatement.Ordinal, span, activeStatement.Flags, unmappedDocumentId);
                            });
                    }
                }
            }

            documentIndicesByMappedPath.FreeValues();
            activeStatementsInChangedDocuments.FreeValues();

            return spans.ToImmutable();
        }

        public async ValueTask<ImmutableArray<ActiveStatementSpan>> GetAdjustedActiveStatementSpansAsync(TextDocument mappedDocument, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        {
            var debuggingSession = _debuggingSession;
            if (debuggingSession == null || !debuggingSession.EditSession.InBreakState)
            {
                return ImmutableArray<ActiveStatementSpan>.Empty;
            }

            if (!SupportsEditAndContinue(mappedDocument.State))
            {
                return ImmutableArray<ActiveStatementSpan>.Empty;
            }

            Contract.ThrowIfNull(mappedDocument.FilePath);

            var newProject = mappedDocument.Project;
            var newSolution = newProject.Solution;
            var oldProject = debuggingSession.LastCommittedSolution.GetProject(newProject.Id);
            if (oldProject == null)
            {
                // project has been added, no changes in active statement spans:
                return ImmutableArray<ActiveStatementSpan>.Empty;
            }

            var baseActiveStatements = await debuggingSession.EditSession.BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
            if (!baseActiveStatements.DocumentPathMap.TryGetValue(mappedDocument.FilePath, out var oldMappedDocumentActiveStatements))
            {
                // no active statements in this document
                return ImmutableArray<ActiveStatementSpan>.Empty;
            }

            var newDocumentActiveStatementSpans = await activeStatementSpanProvider(mappedDocument.Id, mappedDocument.FilePath, cancellationToken).ConfigureAwait(false);
            if (newDocumentActiveStatementSpans.IsEmpty)
            {
                return ImmutableArray<ActiveStatementSpan>.Empty;
            }

            var analyzer = newProject.LanguageServices.GetRequiredService<IEditAndContinueAnalyzer>();

            using var _ = ArrayBuilder<ActiveStatementSpan>.GetInstance(out var adjustedMappedSpans);

            // Start with the current locations of the tracking spans.
            adjustedMappedSpans.AddRange(newDocumentActiveStatementSpans);

            // Update tracking spans to the latest known locations of the active statements contained in changed documents based on their analysis.
            await foreach (var unmappedDocumentId in EditSession.GetChangedDocumentsAsync(debuggingSession.LastCommittedSolution, newProject, cancellationToken).ConfigureAwait(false))
            {
                var newUnmappedDocument = await newSolution.GetRequiredDocumentAsync(unmappedDocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

                var (oldUnmappedDocument, _) = await debuggingSession.LastCommittedSolution.GetDocumentAndStateAsync(newUnmappedDocument.Id, newUnmappedDocument, cancellationToken).ConfigureAwait(false);
                if (oldUnmappedDocument == null)
                {
                    // document out-of-date
                    continue;
                }

                var analysis = await debuggingSession.EditSession.Analyses.GetDocumentAnalysisAsync(debuggingSession.LastCommittedSolution, oldUnmappedDocument, newUnmappedDocument, activeStatementSpanProvider, debuggingSession.Capabilities, cancellationToken).ConfigureAwait(false);

                // Document content did not change or unable to determine active statement spans in a document with syntax errors:
                if (!analysis.ActiveStatements.IsDefault)
                {
                    foreach (var activeStatement in analysis.ActiveStatements)
                    {
                        var i = adjustedMappedSpans.FindIndex((s, ordinal) => s.Ordinal == ordinal, activeStatement.Ordinal);
                        if (i >= 0)
                        {
                            adjustedMappedSpans[i] = new ActiveStatementSpan(activeStatement.Ordinal, activeStatement.Span, activeStatement.Flags, unmappedDocumentId);
                        }
                    }
                }
            }

            return adjustedMappedSpans.ToImmutable();
        }

        public async ValueTask<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(Solution solution, ActiveStatementSpanProvider activeStatementSpanProvider, ManagedInstructionId instructionId, CancellationToken cancellationToken)
        {
            try
            {
                // It is allowed to call this method before entering or after exiting break mode. In fact, the VS debugger does so.
                // We return null since there the concept of active statement only makes sense during break mode.
                var debuggingSession = _debuggingSession;
                if (debuggingSession == null || !debuggingSession.EditSession.InBreakState)
                {
                    return null;
                }

                var baseActiveStatements = await debuggingSession.EditSession.BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
                if (!baseActiveStatements.InstructionMap.TryGetValue(instructionId, out var baseActiveStatement))
                {
                    return null;
                }

                var documentId = await FindChangedDocumentContainingUnmappedActiveStatementAsync(baseActiveStatements, debuggingSession, instructionId.Method.Module, baseActiveStatement, solution, cancellationToken).ConfigureAwait(false);
                if (documentId == null)
                {
                    // Active statement not found in any changed documents, return its last position:
                    return baseActiveStatement.Span;
                }

                var newDocument = await solution.GetDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                if (newDocument == null)
                {
                    // The document has been deleted.
                    return null;
                }

                var (oldDocument, _) = await debuggingSession.LastCommittedSolution.GetDocumentAndStateAsync(newDocument.Id, newDocument, cancellationToken).ConfigureAwait(false);
                if (oldDocument == null)
                {
                    // document out-of-date
                    return null;
                }

                var analysis = await debuggingSession.EditSession.Analyses.GetDocumentAnalysisAsync(debuggingSession.LastCommittedSolution, oldDocument, newDocument, activeStatementSpanProvider, debuggingSession.Capabilities, cancellationToken).ConfigureAwait(false);
                if (!analysis.HasChanges)
                {
                    // Document content did not change:
                    return baseActiveStatement.Span;
                }

                if (analysis.HasSyntaxErrors)
                {
                    // Unable to determine active statement spans in a document with syntax errors:
                    return null;
                }

                Contract.ThrowIfTrue(analysis.ActiveStatements.IsDefault);
                return analysis.ActiveStatements.GetStatement(baseActiveStatement.Ordinal).Span;
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return null;
            }
        }

        /// <summary>
        /// Called by the debugger to determine whether a non-leaf active statement is in an exception region,
        /// so it can determine whether the active statement can be remapped. This only happens when the EnC is about to apply changes.
        /// If the debugger determines we can remap active statements, the application of changes proceeds.
        /// 
        /// TODO: remove (https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1310859)
        /// </summary>
        /// <returns>
        /// True if the instruction is located within an exception region, false if it is not, null if the instruction isn't an active statement in a changed method 
        /// or the exception regions can't be determined.
        /// </returns>
        public async ValueTask<bool?> IsActiveStatementInExceptionRegionAsync(Solution solution, ManagedInstructionId instructionId, CancellationToken cancellationToken)
        {
            try
            {
                var debuggingSession = _debuggingSession;
                if (debuggingSession == null || !debuggingSession.EditSession.InBreakState)
                {
                    return null;
                }

                // This method is only called when the EnC is about to apply changes, at which point all active statements and
                // their exception regions will be needed. Hence it's not necessary to scope this query down to just the instruction
                // the debugger is interested at this point while not calculating the others.

                var baseActiveStatements = await debuggingSession.EditSession.BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
                if (!baseActiveStatements.InstructionMap.TryGetValue(instructionId, out var baseActiveStatement))
                {
                    return null;
                }

                var documentId = await FindChangedDocumentContainingUnmappedActiveStatementAsync(baseActiveStatements, debuggingSession, instructionId.Method.Module, baseActiveStatement, solution, cancellationToken).ConfigureAwait(false);
                if (documentId == null)
                {
                    // the active statement is contained in an unchanged document, thus it doesn't matter whether it's in an exception region or not
                    return null;
                }

                var newDocument = solution.GetRequiredDocument(documentId);
                var (oldDocument, _) = await debuggingSession.LastCommittedSolution.GetDocumentAndStateAsync(newDocument.Id, newDocument, cancellationToken).ConfigureAwait(false);
                if (oldDocument == null)
                {
                    // Document is out-of-sync, can't reason about its content with respect to the binaries loaded in the debuggee.
                    return null;
                }

                var analyzer = newDocument.Project.LanguageServices.GetRequiredService<IEditAndContinueAnalyzer>();
                var oldDocumentActiveStatements = await baseActiveStatements.GetOldActiveStatementsAsync(analyzer, oldDocument, cancellationToken).ConfigureAwait(false);
                return oldDocumentActiveStatements.GetStatement(baseActiveStatement.Ordinal).ExceptionRegions.IsActiveStatementCovered;
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return null;
            }
        }

        private static async Task<DocumentId?> FindChangedDocumentContainingUnmappedActiveStatementAsync(
            ActiveStatementsMap activeStatementsMap,
            DebuggingSession debuggingSession,
            Guid moduleId,
            ActiveStatement baseActiveStatement,
            Solution newSolution,
            CancellationToken cancellationToken)
        {
            DocumentId? documentId = null;
            if (debuggingSession.TryGetProjectId(moduleId, out var projectId))
            {
                var oldProject = debuggingSession.LastCommittedSolution.GetProject(projectId);
                if (oldProject == null)
                {
                    // project has been added (should have no active statements under normal circumstances)
                    return null;
                }

                var newProject = newSolution.GetProject(projectId);
                if (newProject == null)
                {
                    // project has been deleted
                    return null;
                }

                documentId = await GetChangedDocumentContainingUnmappedActiveStatementAsync(activeStatementsMap, debuggingSession.LastCommittedSolution, newProject, baseActiveStatement, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Search for the document in all changed projects in the solution.

                using var documentFoundCancellationSource = new CancellationTokenSource();
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(documentFoundCancellationSource.Token, cancellationToken);

                async Task GetTaskAsync(ProjectId projectId)
                {
                    var newProject = newSolution.GetRequiredProject(projectId);
                    var id = await GetChangedDocumentContainingUnmappedActiveStatementAsync(activeStatementsMap, debuggingSession.LastCommittedSolution, newProject, baseActiveStatement, linkedTokenSource.Token).ConfigureAwait(false);
                    Interlocked.CompareExchange(ref documentId, id, null);
                    if (id != null)
                    {
                        documentFoundCancellationSource.Cancel();
                    }
                }

                var tasks = newSolution.ProjectIds.Select(GetTaskAsync);

                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (documentFoundCancellationSource.IsCancellationRequested)
                {
                    // nop: cancelled because we found the document
                }
            }

            return documentId;
        }

        // Enumerate all changed documents in the project whose module contains the active statement.
        // For each such document enumerate all #line directives to find which maps code to the span that contains the active statement.
        private static async ValueTask<DocumentId?> GetChangedDocumentContainingUnmappedActiveStatementAsync(ActiveStatementsMap baseActiveStatements, CommittedSolution oldSolution, Project newProject, ActiveStatement activeStatement, CancellationToken cancellationToken)
        {
            var analyzer = newProject.LanguageServices.GetRequiredService<IEditAndContinueAnalyzer>();

            await foreach (var documentId in EditSession.GetChangedDocumentsAsync(oldSolution, newProject, cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var newDocument = newProject.GetRequiredDocument(documentId);
                var (oldDocument, _) = await oldSolution.GetDocumentAndStateAsync(newDocument.Id, newDocument, cancellationToken).ConfigureAwait(false);
                if (oldDocument == null)
                {
                    // Document is out-of-sync, can't reason about its content with respect to the binaries loaded in the debuggee.
                    return null;
                }

                var oldActiveStatements = await baseActiveStatements.GetOldActiveStatementsAsync(analyzer, oldDocument, cancellationToken).ConfigureAwait(false);
                if (oldActiveStatements.Any(s => s.Statement == activeStatement))
                {
                    return documentId;
                }
            }

            return null;
        }

        private static void ReportTelemetry(DebuggingSessionTelemetry.Data data)
        {
            // report telemetry (fire and forget):
            _ = Task.Run(() => LogDebuggingSessionTelemetry(data, Logger.Log, LogAggregator.GetNextId));
        }

        // internal for testing
        internal static void LogDebuggingSessionTelemetry(DebuggingSessionTelemetry.Data debugSessionData, Action<FunctionId, LogMessage> log, Func<int> getNextId)
        {
            const string SessionId = nameof(SessionId);
            const string EditSessionId = nameof(EditSessionId);

            var debugSessionId = getNextId();

            log(FunctionId.Debugging_EncSession, KeyValueLogMessage.Create(map =>
            {
                map[SessionId] = debugSessionId;
                map["SessionCount"] = debugSessionData.EditSessionData.Length;
                map["EmptySessionCount"] = debugSessionData.EmptyEditSessionCount;
            }));

            foreach (var editSessionData in debugSessionData.EditSessionData)
            {
                var editSessionId = getNextId();

                log(FunctionId.Debugging_EncSession_EditSession, KeyValueLogMessage.Create(map =>
                {
                    map[SessionId] = debugSessionId;
                    map[EditSessionId] = editSessionId;

                    map["HadCompilationErrors"] = editSessionData.HadCompilationErrors;
                    map["HadRudeEdits"] = editSessionData.HadRudeEdits;
                    map["HadValidChanges"] = editSessionData.HadValidChanges;
                    map["HadValidInsignificantChanges"] = editSessionData.HadValidInsignificantChanges;

                    map["RudeEditsCount"] = editSessionData.RudeEdits.Length;
                    map["EmitDeltaErrorIdCount"] = editSessionData.EmitErrorIds.Length;
                }));

                foreach (var errorId in editSessionData.EmitErrorIds)
                {
                    log(FunctionId.Debugging_EncSession_EditSession_EmitDeltaErrorId, KeyValueLogMessage.Create(map =>
                    {
                        map[SessionId] = debugSessionId;
                        map[EditSessionId] = editSessionId;
                        map["ErrorId"] = errorId;
                    }));
                }

                foreach (var (editKind, syntaxKind) in editSessionData.RudeEdits)
                {
                    log(FunctionId.Debugging_EncSession_EditSession_RudeEdit, KeyValueLogMessage.Create(map =>
                    {
                        map[SessionId] = debugSessionId;
                        map[EditSessionId] = editSessionId;

                        map["RudeEditKind"] = editKind;
                        map["RudeEditSyntaxKind"] = syntaxKind;
                        map["RudeEditBlocking"] = editSessionData.HadRudeEdits;
                    }));
                }
            }
        }

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor
        {
            private readonly EditAndContinueWorkspaceService _service;

            public TestAccessor(EditAndContinueWorkspaceService service)
            {
                _service = service;
            }

            internal DebuggingSession? GetDebuggingSession() => _service._debuggingSession;
            internal EditSession? GetEditSession() => _service._debuggingSession?.EditSession;
            internal void SetOutputProvider(Func<Project, CompilationOutputs> value) => _service._compilationOutputsProvider = value;
            internal void SetReportTelemetry(Action<DebuggingSessionTelemetry.Data> value) => _service._reportTelemetry = value;
        }
    }
}
