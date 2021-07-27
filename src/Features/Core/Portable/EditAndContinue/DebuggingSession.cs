﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Represents a debugging session.
    /// </summary>
    internal sealed class DebuggingSession : IDisposable
    {
        private readonly Func<Project, CompilationOutputs> _compilationOutputsProvider;
        private readonly CancellationTokenSource _cancellationSource = new();

        /// <summary>
        /// MVIDs read from the assembly built for given project id.
        /// </summary>
        private readonly Dictionary<ProjectId, (Guid Mvid, Diagnostic Error)> _projectModuleIds = new();
        private readonly Dictionary<Guid, ProjectId> _moduleIds = new();
        private readonly object _projectModuleIdsGuard = new();

        /// <summary>
        /// The current baseline for given project id.
        /// The baseline is updated when changes are committed at the end of edit session.
        /// The backing module readers of initial baselines need to be kept alive -- store them in
        /// <see cref="_initialBaselineModuleReaders"/> and dispose them at the end of the debugging session.
        /// </summary>
        /// <remarks>
        /// The baseline of each updated project is linked to its initial baseline that reads from the on-disk metadata and PDB.
        /// Therefore once an initial baseline is created it needs to be kept alive till the end of the debugging session,
        /// even whne it's replaced in <see cref="_projectEmitBaselines"/> by a newer baseline.
        /// </remarks>
        private readonly Dictionary<ProjectId, EmitBaseline> _projectEmitBaselines = new();
        private readonly List<IDisposable> _initialBaselineModuleReaders = new();
        private readonly object _projectEmitBaselinesGuard = new();

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
        internal ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>> NonRemappableRegions { get; private set; }

        internal EditSession EditSession { get; private set; }

        private readonly HashSet<Guid> _modulesPreparedForUpdate = new();
        private readonly object _modulesPreparedForUpdateGuard = new();

        internal readonly DebuggingSessionId Id;

        /// <summary>
        /// The solution captured when the debugging session entered run mode (application debugging started),
        /// or the solution which the last changes committed to the debuggee at the end of edit session were calculated from.
        /// The solution reflecting the current state of the modules loaded in the debugee.
        /// </summary>
        internal readonly CommittedSolution LastCommittedSolution;

        internal readonly IManagedEditAndContinueDebuggerService DebuggerService;

        /// <summary>
        /// Gets the capabilities of the runtime with respect to applying code changes.
        /// </summary>
        internal readonly EditAndContinueCapabilities Capabilities;

        /// <summary>
        /// True if the diagnostics produced by the session should be reported to the diagnotic analyzer.
        /// </summary>
        internal readonly bool ReportDiagnostics;

        private readonly DebuggingSessionTelemetry _telemetry;
        private readonly EditSessionTelemetry _editSessionTelemetry;

        private PendingSolutionUpdate? _pendingUpdate;
        private Action<DebuggingSessionTelemetry.Data> _reportTelemetry;

        internal DebuggingSession(
            DebuggingSessionId id,
            Solution solution,
            IManagedEditAndContinueDebuggerService debuggerService,
            EditAndContinueCapabilities capabilities,
            Func<Project, CompilationOutputs> compilationOutputsProvider,
            IEnumerable<KeyValuePair<DocumentId, CommittedSolution.DocumentState>> initialDocumentStates,
            bool reportDiagnostics)
        {
            _compilationOutputsProvider = compilationOutputsProvider;
            _telemetry = new DebuggingSessionTelemetry();
            _editSessionTelemetry = new EditSessionTelemetry();
            _reportTelemetry = ReportTelemetry;

            Id = id;
            Capabilities = capabilities;
            DebuggerService = debuggerService;
            LastCommittedSolution = new CommittedSolution(this, solution, initialDocumentStates);
            NonRemappableRegions = ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>>.Empty;
            EditSession = new EditSession(this, _editSessionTelemetry, inBreakState: false);
            ReportDiagnostics = reportDiagnostics;
        }

        public void Dispose()
        {
            _cancellationSource.Cancel();

            // Consider: Some APIs on DebuggingSession (GetDocumentDiagnostics, OnSourceFileUpdated, GetXxxSpansAsync) can be called at any point in time.
            // These APIs should not be using the readers being disposed here, but there is no guarantee. Consider refactoring that would guarantee correctness.
            foreach (var reader in GetBaselineModuleReaders())
            {
                reader.Dispose();
            }

            _cancellationSource.Dispose();
        }

        internal Task OnSourceFileUpdatedAsync(Document document)
            => LastCommittedSolution.OnSourceFileUpdatedAsync(document, _cancellationSource.Token);

        internal void StorePendingUpdate(Solution solution, SolutionUpdate update)
        {
            var previousPendingUpdate = Interlocked.Exchange(ref _pendingUpdate, new PendingSolutionUpdate(
                solution,
                update.EmitBaselines,
                update.ModuleUpdates.Updates,
                update.NonRemappableRegions));

            // commit/discard was not called:
            Contract.ThrowIfFalse(previousPendingUpdate == null);
        }

        internal PendingSolutionUpdate RetrievePendingUpdate()
        {
            var pendingUpdate = Interlocked.Exchange(ref _pendingUpdate, null);
            Contract.ThrowIfNull(pendingUpdate);
            return pendingUpdate;
        }

        private void EndEditSession(out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            documentsToReanalyze = EditSession.GetDocumentsWithReportedDiagnostics();

            var editSessionTelemetryData = EditSession.Telemetry.GetDataAndClear();

            // TODO: report a separate telemetry data for hot reload sessions to preserve the semantics of the current telemetry data
            // https://github.com/dotnet/roslyn/issues/52128
            if (EditSession.InBreakState)
            {
                _telemetry.LogEditSession(editSessionTelemetryData);
            }
        }

        public void EndSession(out ImmutableArray<DocumentId> documentsToReanalyze, out DebuggingSessionTelemetry.Data telemetryData)
        {
            EndEditSession(out documentsToReanalyze);
            telemetryData = _telemetry.GetDataAndClear();
            _reportTelemetry(telemetryData);

            Dispose();
        }

        public void BreakStateEntered(out ImmutableArray<DocumentId> documentsToReanalyze)
            => RestartEditSession(inBreakState: true, out documentsToReanalyze);

        internal void RestartEditSession(bool inBreakState, out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            EndEditSession(out documentsToReanalyze);
            EditSession = new EditSession(this, EditSession.Telemetry, inBreakState);
        }

        private ImmutableArray<IDisposable> GetBaselineModuleReaders()
        {
            lock (_projectEmitBaselinesGuard)
            {
                return _initialBaselineModuleReaders.ToImmutableArrayOrEmpty();
            }
        }

        internal CompilationOutputs GetCompilationOutputs(Project project)
            => _compilationOutputsProvider(project);

        internal bool AddModulePreparedForUpdate(Guid mvid)
        {
            lock (_modulesPreparedForUpdateGuard)
            {
                return _modulesPreparedForUpdate.Add(mvid);
            }
        }

        public void CommitSolutionUpdate(PendingSolutionUpdate update)
        {
            // Save new non-remappable regions for the next edit session.
            // If no edits were made the pending list will be empty and we need to keep the previous regions.

            var nonRemappableRegions = GroupToImmutableDictionary(
                from moduleRegions in update.NonRemappableRegions
                from region in moduleRegions.Regions
                group region.Region by new ManagedMethodId(moduleRegions.ModuleId, region.Method));

            if (nonRemappableRegions.Count > 0)
            {
                NonRemappableRegions = nonRemappableRegions;
            }

            // update baselines:
            lock (_projectEmitBaselinesGuard)
            {
                foreach (var (projectId, baseline) in update.EmitBaselines)
                {
                    _projectEmitBaselines[projectId] = baseline;
                }
            }

            LastCommittedSolution.CommitSolution(update.Solution);
        }

        /// <summary>
        /// Reads the MVID of a compiled project.
        /// </summary>
        /// <returns>
        /// An MVID and an error message to report, in case an IO exception occurred while reading the binary.
        /// The MVID is default if either project not built, or an it can't be read from the module binary.
        /// </returns>
        public async Task<(Guid Mvid, Diagnostic? Error)> GetProjectModuleIdAsync(Project project, CancellationToken cancellationToken)
        {
            lock (_projectModuleIdsGuard)
            {
                if (_projectModuleIds.TryGetValue(project.Id, out var id))
                {
                    return id;
                }
            }

            (Guid Mvid, Diagnostic? Error) ReadMvid()
            {
                var outputs = GetCompilationOutputs(project);

                try
                {
                    return (outputs.ReadAssemblyModuleVersionId(), Error: null);
                }
                catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
                {
                    return (Mvid: Guid.Empty, Error: null);
                }
                catch (Exception e)
                {
                    var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.ErrorReadingFile);
                    return (Mvid: Guid.Empty, Error: Diagnostic.Create(descriptor, Location.None, new[] { outputs.AssemblyDisplayPath, e.Message }));
                }
            }

            var newId = await Task.Run(ReadMvid, cancellationToken).ConfigureAwait(false);

            lock (_projectModuleIdsGuard)
            {
                if (_projectModuleIds.TryGetValue(project.Id, out var id))
                {
                    return id;
                }

                _moduleIds[newId.Mvid] = project.Id;
                return _projectModuleIds[project.Id] = newId;
            }
        }

        public bool TryGetProjectId(Guid moduleId, [NotNullWhen(true)] out ProjectId? projectId)
        {
            lock (_projectModuleIdsGuard)
            {
                return _moduleIds.TryGetValue(moduleId, out projectId);
            }
        }

        /// <summary>
        /// Get <see cref="EmitBaseline"/> for given project.
        /// </summary>
        /// <returns>True unless the project outputs can't be read.</returns>
        public bool TryGetOrCreateEmitBaseline(Project project, out ImmutableArray<Diagnostic> diagnostics, [NotNullWhen(true)] out EmitBaseline? baseline)
        {
            lock (_projectEmitBaselinesGuard)
            {
                if (_projectEmitBaselines.TryGetValue(project.Id, out baseline))
                {
                    diagnostics = ImmutableArray<Diagnostic>.Empty;
                    return true;
                }
            }

            var outputs = GetCompilationOutputs(project);
            if (!TryCreateInitialBaseline(outputs, out diagnostics, out var newBaseline, out var debugInfoReaderProvider, out var metadataReaderProvider))
            {
                // Unable to read the DLL/PDB at this point (it might be open by another process).
                // Don't cache the failure so that the user can attempt to apply changes again.
                return false;
            }

            lock (_projectEmitBaselinesGuard)
            {
                if (_projectEmitBaselines.TryGetValue(project.Id, out baseline))
                {
                    metadataReaderProvider.Dispose();
                    debugInfoReaderProvider.Dispose();
                    return true;
                }

                _projectEmitBaselines[project.Id] = newBaseline;

                _initialBaselineModuleReaders.Add(metadataReaderProvider);
                _initialBaselineModuleReaders.Add(debugInfoReaderProvider);
            }

            baseline = newBaseline;
            return true;
        }

        private static unsafe bool TryCreateInitialBaseline(
            CompilationOutputs compilationOutputs,
            out ImmutableArray<Diagnostic> diagnostics,
            [NotNullWhen(true)] out EmitBaseline? baseline,
            [NotNullWhen(true)] out DebugInformationReaderProvider? debugInfoReaderProvider,
            [NotNullWhen(true)] out MetadataReaderProvider? metadataReaderProvider)
        {
            // Read the metadata and symbols from the disk. Close the files as soon as we are done emitting the delta to minimize 
            // the time when they are being locked. Since we need to use the baseline that is produced by delta emit for the subsequent
            // delta emit we need to keep the module metadata and symbol info backing the symbols of the baseline alive in memory. 
            // Alternatively, we could drop the data once we are done with emitting the delta and re-emit the baseline again 
            // when we need it next time and the module is loaded.

            diagnostics = default;
            baseline = null;
            debugInfoReaderProvider = null;
            metadataReaderProvider = null;

            var success = false;
            var fileBeingRead = compilationOutputs.PdbDisplayPath;
            try
            {
                debugInfoReaderProvider = compilationOutputs.OpenPdb();
                if (debugInfoReaderProvider == null)
                {
                    throw new FileNotFoundException();
                }

                var debugInfoReader = debugInfoReaderProvider.CreateEditAndContinueMethodDebugInfoReader();

                fileBeingRead = compilationOutputs.AssemblyDisplayPath;

                metadataReaderProvider = compilationOutputs.OpenAssemblyMetadata(prefetch: true);
                if (metadataReaderProvider == null)
                {
                    throw new FileNotFoundException();
                }

                var metadataReader = metadataReaderProvider.GetMetadataReader();
                var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr)metadataReader.MetadataPointer, metadataReader.MetadataLength);

                baseline = EmitBaseline.CreateInitialBaseline(
                    moduleMetadata,
                    debugInfoReader.GetDebugInfo,
                    debugInfoReader.GetLocalSignature,
                    debugInfoReader.IsPortable);

                success = true;
                return true;
            }
            catch (Exception e)
            {
                var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.ErrorReadingFile);
                diagnostics = ImmutableArray.Create(Diagnostic.Create(descriptor, Location.None, new[] { fileBeingRead, e.Message }));
            }
            finally
            {
                if (!success)
                {
                    debugInfoReaderProvider?.Dispose();
                    metadataReaderProvider?.Dispose();
                }
            }

            return false;
        }

        private static ImmutableDictionary<K, ImmutableArray<V>> GroupToImmutableDictionary<K, V>(IEnumerable<IGrouping<K, V>> items)
            where K : notnull
        {
            var builder = ImmutableDictionary.CreateBuilder<K, ImmutableArray<V>>();

            foreach (var item in items)
            {
                builder.Add(item.Key, item.ToImmutableArray());
            }

            return builder.ToImmutable();
        }

        public async ValueTask<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        {
            try
            {
                // Not a C# or VB project.
                var project = document.Project;
                if (!project.SupportsEditAndContinue())
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                // Document does not compile to the assembly (e.g. cshtml files, .g.cs files generated for completion only)
                if (!document.DocumentState.SupportsEditAndContinue())
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                // Do not analyze documents (and report diagnostics) of projects that have not been built.
                // Allow user to make any changes in these documents, they won't be applied within the current debugging session.
                // Do not report the file read error - it might be an intermittent issue. The error will be reported when the
                // change is attempted to be applied.
                var (mvid, _) = await GetProjectModuleIdAsync(project, cancellationToken).ConfigureAwait(false);
                if (mvid == Guid.Empty)
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                var (oldDocument, oldDocumentState) = await LastCommittedSolution.GetDocumentAndStateAsync(document.Id, document, cancellationToken).ConfigureAwait(false);
                if (oldDocumentState is CommittedSolution.DocumentState.OutOfSync or
                    CommittedSolution.DocumentState.Indeterminate or
                    CommittedSolution.DocumentState.DesignTimeOnly)
                {
                    // Do not report diagnostics for existing out-of-sync documents or design-time-only documents.
                    return ImmutableArray<Diagnostic>.Empty;
                }

                var analysis = await EditSession.Analyses.GetDocumentAnalysisAsync(LastCommittedSolution, oldDocument, document, activeStatementSpanProvider, Capabilities, cancellationToken).ConfigureAwait(false);
                if (analysis.HasChanges)
                {
                    // Once we detected a change in a document let the debugger know that the corresponding loaded module
                    // is about to be updated, so that it can start initializing it for EnC update, reducing the amount of time applying
                    // the change blocks the UI when the user "continues".
                    if (AddModulePreparedForUpdate(mvid))
                    {
                        // fire and forget:
                        _ = Task.Run(() => DebuggerService.PrepareModuleForUpdateAsync(mvid, cancellationToken), cancellationToken);
                    }
                }

                if (analysis.RudeEditErrors.IsEmpty)
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                EditSession.Telemetry.LogRudeEditDiagnostics(analysis.RudeEditErrors);

                // track the document, so that we can refresh or clean diagnostics at the end of edit session:
                EditSession.TrackDocumentWithReportedDiagnostics(document.Id);

                var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                return analysis.RudeEditErrors.SelectAsArray((e, t) => e.ToDiagnostic(t), tree);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return ImmutableArray<Diagnostic>.Empty;
            }
        }

        public async ValueTask<EmitSolutionUpdateResults> EmitSolutionUpdateAsync(
            Solution solution,
            ActiveStatementSpanProvider activeStatementSpanProvider,
            CancellationToken cancellationToken)
        {
            var solutionUpdate = await EditSession.EmitSolutionUpdateAsync(solution, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
            if (solutionUpdate.ModuleUpdates.Status == ManagedModuleUpdateStatus.Ready)
            {
                StorePendingUpdate(solution, solutionUpdate);
            }

            // Note that we may return empty deltas if all updates have been deferred.
            // The debugger will still call commit or discard on the update batch.
            return new EmitSolutionUpdateResults(solutionUpdate.ModuleUpdates, solutionUpdate.Diagnostics, solutionUpdate.DocumentsWithRudeEdits);
        }

        public void CommitSolutionUpdate(out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            var pendingUpdate = RetrievePendingUpdate();
            CommitSolutionUpdate(pendingUpdate);

            // restart edit session with no active statements (switching to run mode):
            RestartEditSession(inBreakState: false, out documentsToReanalyze);
        }

        public void DiscardSolutionUpdate()
            => _ = RetrievePendingUpdate();

        public async ValueTask<ImmutableArray<ImmutableArray<ActiveStatementSpan>>> GetBaseActiveStatementSpansAsync(Solution solution, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            if (!EditSession.InBreakState)
            {
                return default;
            }

            var baseActiveStatements = await EditSession.BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
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

                await foreach (var documentId in EditSession.GetChangedDocumentsAsync(LastCommittedSolution, newProject, cancellationToken).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var newDocument = await solution.GetRequiredDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

                    var (oldDocument, _) = await LastCommittedSolution.GetDocumentAndStateAsync(newDocument.Id, newDocument, cancellationToken).ConfigureAwait(false);
                    if (oldDocument == null)
                    {
                        // Document is out-of-sync, can't reason about its content with respect to the binaries loaded in the debuggee.
                        continue;
                    }

                    var oldDocumentActiveStatements = await baseActiveStatements.GetOldActiveStatementsAsync(analyzer, oldDocument, cancellationToken).ConfigureAwait(false);

                    var analysis = await analyzer.AnalyzeDocumentAsync(
                        LastCommittedSolution.GetRequiredProject(documentId.ProjectId),
                        baseActiveStatements,
                        newDocument,
                        newActiveStatementSpans: ImmutableArray<LinePositionSpan>.Empty,
                        Capabilities,
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
            if (!EditSession.InBreakState)
            {
                return ImmutableArray<ActiveStatementSpan>.Empty;
            }

            if (!mappedDocument.State.SupportsEditAndContinue())
            {
                return ImmutableArray<ActiveStatementSpan>.Empty;
            }

            Contract.ThrowIfNull(mappedDocument.FilePath);

            var newProject = mappedDocument.Project;
            var newSolution = newProject.Solution;
            var oldProject = LastCommittedSolution.GetProject(newProject.Id);
            if (oldProject == null)
            {
                // project has been added, no changes in active statement spans:
                return ImmutableArray<ActiveStatementSpan>.Empty;
            }

            var baseActiveStatements = await EditSession.BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
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
            await foreach (var unmappedDocumentId in EditSession.GetChangedDocumentsAsync(LastCommittedSolution, newProject, cancellationToken).ConfigureAwait(false))
            {
                var newUnmappedDocument = await newSolution.GetRequiredDocumentAsync(unmappedDocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

                var (oldUnmappedDocument, _) = await LastCommittedSolution.GetDocumentAndStateAsync(newUnmappedDocument.Id, newUnmappedDocument, cancellationToken).ConfigureAwait(false);
                if (oldUnmappedDocument == null)
                {
                    // document out-of-date
                    continue;
                }

                var analysis = await EditSession.Analyses.GetDocumentAnalysisAsync(LastCommittedSolution, oldUnmappedDocument, newUnmappedDocument, activeStatementSpanProvider, Capabilities, cancellationToken).ConfigureAwait(false);

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
                if (!EditSession.InBreakState)
                {
                    return null;
                }

                var baseActiveStatements = await EditSession.BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
                if (!baseActiveStatements.InstructionMap.TryGetValue(instructionId, out var baseActiveStatement))
                {
                    return null;
                }

                var documentId = await FindChangedDocumentContainingUnmappedActiveStatementAsync(baseActiveStatements, instructionId.Method.Module, baseActiveStatement, solution, cancellationToken).ConfigureAwait(false);
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

                var (oldDocument, _) = await LastCommittedSolution.GetDocumentAndStateAsync(newDocument.Id, newDocument, cancellationToken).ConfigureAwait(false);
                if (oldDocument == null)
                {
                    // document out-of-date
                    return null;
                }

                var analysis = await EditSession.Analyses.GetDocumentAnalysisAsync(LastCommittedSolution, oldDocument, newDocument, activeStatementSpanProvider, Capabilities, cancellationToken).ConfigureAwait(false);
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
                if (!EditSession.InBreakState)
                {
                    return null;
                }

                // This method is only called when the EnC is about to apply changes, at which point all active statements and
                // their exception regions will be needed. Hence it's not necessary to scope this query down to just the instruction
                // the debugger is interested at this point while not calculating the others.

                var baseActiveStatements = await EditSession.BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
                if (!baseActiveStatements.InstructionMap.TryGetValue(instructionId, out var baseActiveStatement))
                {
                    return null;
                }

                var documentId = await FindChangedDocumentContainingUnmappedActiveStatementAsync(baseActiveStatements, instructionId.Method.Module, baseActiveStatement, solution, cancellationToken).ConfigureAwait(false);
                if (documentId == null)
                {
                    // the active statement is contained in an unchanged document, thus it doesn't matter whether it's in an exception region or not
                    return null;
                }

                var newDocument = solution.GetRequiredDocument(documentId);
                var (oldDocument, _) = await LastCommittedSolution.GetDocumentAndStateAsync(newDocument.Id, newDocument, cancellationToken).ConfigureAwait(false);
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

        private async Task<DocumentId?> FindChangedDocumentContainingUnmappedActiveStatementAsync(
            ActiveStatementsMap activeStatementsMap,
            Guid moduleId,
            ActiveStatement baseActiveStatement,
            Solution newSolution,
            CancellationToken cancellationToken)
        {
            DocumentId? documentId = null;
            if (TryGetProjectId(moduleId, out var projectId))
            {
                var oldProject = LastCommittedSolution.GetProject(projectId);
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

                documentId = await GetChangedDocumentContainingUnmappedActiveStatementAsync(activeStatementsMap, LastCommittedSolution, newProject, baseActiveStatement, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Search for the document in all changed projects in the solution.

                using var documentFoundCancellationSource = new CancellationTokenSource();
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(documentFoundCancellationSource.Token, cancellationToken);

                async Task GetTaskAsync(ProjectId projectId)
                {
                    var newProject = newSolution.GetRequiredProject(projectId);
                    var id = await GetChangedDocumentContainingUnmappedActiveStatementAsync(activeStatementsMap, LastCommittedSolution, newProject, baseActiveStatement, linkedTokenSource.Token).ConfigureAwait(false);
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
            _ = Task.Run(() => LogTelemetry(data, Logger.Log, LogAggregator.GetNextId));
        }

        private static void LogTelemetry(DebuggingSessionTelemetry.Data debugSessionData, Action<FunctionId, LogMessage> log, Func<int> getNextId)
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
            private readonly DebuggingSession _instance;

            public TestAccessor(DebuggingSession instance)
                => _instance = instance;

            public void SetNonRemappableRegions(ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>> nonRemappableRegions)
                => _instance.NonRemappableRegions = nonRemappableRegions;

            public ImmutableHashSet<Guid> GetModulesPreparedForUpdate()
            {
                lock (_instance._modulesPreparedForUpdateGuard)
                {
                    return _instance._modulesPreparedForUpdate.ToImmutableHashSet();
                }
            }

            public EmitBaseline GetProjectEmitBaseline(ProjectId id)
            {
                lock (_instance._projectEmitBaselinesGuard)
                {
                    return _instance._projectEmitBaselines[id];
                }
            }

            public ImmutableArray<IDisposable> GetBaselineModuleReaders()
                => _instance.GetBaselineModuleReaders();

            public PendingSolutionUpdate? GetPendingSolutionUpdate()
                => _instance._pendingUpdate;

            public void SetTelemetryLogger(Action<FunctionId, LogMessage> logger, Func<int> getNextId)
                => _instance._reportTelemetry = data => LogTelemetry(data, logger, getNextId);
        }
    }
}
