// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Represents a debugging session.
/// </summary>
internal sealed class DebuggingSession : IDisposable
{
    private readonly Func<Project, CompilationOutputs> _compilationOutputsProvider;
    private readonly CancellationTokenSource _cancellationSource = new();

    internal readonly IPdbMatchingSourceTextProvider SourceTextProvider;

    /// <summary>
    /// Logs debugging session events.
    /// </summary>
    internal readonly TraceLog SessionLog;

    /// <summary>
    /// Logs EnC analysis events. 
    /// </summary>
    internal readonly TraceLog AnalysisLog;

    /// <summary>
    /// Current baselines for given project id.
    /// The baselines are updated when changes are committed at the end of edit session.
    /// </summary>
    /// <remarks>
    /// The backing module readers of initial baselines need to be kept alive -- store them in
    /// <see cref="_initialBaselineModuleReaders"/> and dispose them at the end of the debugging session.
    /// 
    /// The baseline of each updated project is linked to its initial baseline that reads from the on-disk metadata and PDB.
    /// Therefore once an initial baseline is created it needs to be kept alive till the end of the debugging session,
    /// even when it's replaced in <see cref="_projectBaselines"/> by a newer baseline.
    /// 
    /// One project may have multiple baselines. Deltas emitted for the project when source changes are applied are based 
    /// on the same source changes for all the baselines, however they differ in the baseline they are chained to (MVID and relative tokens).
    /// 
    /// For example, in the following scenario:
    /// 
    ///   A shared library Lib is referenced by two executable projects A and B and Lib.dll is copied to their respective output directories and the following events occur:
    ///   1) A is launched, modules A.exe and Lib.dll [1] are loaded.
    ///   2) Change is made to Lib.cs and applied.
    ///   3) B is launched, which builds new version of Lib.dll [2], and modules B.exe and Lib.dll [2] are loaded.
    ///   4) Another change is made to Lib.cs and applied.
    ///     
    ///   At this point we have two baselines for Lib: Lib.dll [1] and Lib.dll [2], each have different MVID.
    ///   We need to emit 2 deltas for the change in step 4:
    ///   - one that chains to the first delta applied to Lib.dll, which itself chains to the baseline of Lib.dll [1].
    ///   - one that chains to the baseline Lib.dll [2]
    /// </remarks>
    private readonly Dictionary<ProjectId, ImmutableList<ProjectBaseline>> _projectBaselines = [];
    private readonly Dictionary<Guid, (IDisposable metadata, IDisposable pdb)> _initialBaselineModuleReaders = [];
    private readonly object _projectEmitBaselinesGuard = new();

    /// <summary>
    /// To avoid accessing metadata/symbol readers that have been disposed,
    /// read lock is acquired before every operation that may access a baseline module/symbol reader 
    /// and write lock when the baseline readers are being disposed.
    /// </summary>
    private readonly ReaderWriterLockSlim _baselineContentAccessLock = new();
    private bool _isDisposed;

    internal EditSession EditSession { get; private set; }

    private readonly HashSet<Guid> _modulesPreparedForUpdate = [];
    private readonly object _modulesPreparedForUpdateGuard = new();

    internal readonly DebuggingSessionId Id;

    /// <summary>
    /// Incremented on every emit update invocation. Used by logging.
    /// </summary>
    private int _updateOrdinal;

    /// <summary>
    /// The solution captured when the debugging session entered run mode (application debugging started),
    /// or the solution which the last changes committed to the debuggee at the end of edit session were calculated from.
    /// The solution reflecting the current state of the modules loaded in the debugee.
    /// </summary>
    internal readonly CommittedSolution LastCommittedSolution;

    internal readonly IManagedHotReloadService DebuggerService;

    /// <summary>
    /// True if the diagnostics produced by the session should be reported to the diagnotic analyzer.
    /// </summary>
    internal readonly bool ReportDiagnostics;

    private readonly DebuggingSessionTelemetry _telemetry;
    private readonly EditSessionTelemetry _editSessionTelemetry = new();

    private PendingUpdate? _pendingUpdate;
    private Action<DebuggingSessionTelemetry.Data> _reportTelemetry;

    /// <summary>
    /// Last array of module updates generated during the debugging session.
    /// Useful for crash dump diagnostics.
    /// </summary>
#pragma warning disable IDE0052 // Remove unread private members
    private ImmutableArray<ManagedHotReloadUpdate> _lastModuleUpdatesLog;
#pragma warning restore IDE0052

    internal DebuggingSession(
        DebuggingSessionId id,
        Solution solution,
        IManagedHotReloadService debuggerService,
        Func<Project, CompilationOutputs> compilationOutputsProvider,
        IPdbMatchingSourceTextProvider sourceTextProvider,
        TraceLog sessionLog,
        TraceLog analysisLog,
        bool reportDiagnostics)
    {
        sessionLog.Write($"Debugging session started: #{id}");

        _compilationOutputsProvider = compilationOutputsProvider;
        SourceTextProvider = sourceTextProvider;
        SessionLog = sessionLog;
        AnalysisLog = analysisLog;
        _reportTelemetry = ReportTelemetry;
        _telemetry = new DebuggingSessionTelemetry(solution.SolutionState.SolutionAttributes.TelemetryId);

        Id = id;
        DebuggerService = debuggerService;
        LastCommittedSolution = new CommittedSolution(this, solution);

        EditSession = new EditSession(
            this,
            nonRemappableRegions: ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>>.Empty,
            _editSessionTelemetry,
            lazyActiveStatementMap: null,
            inBreakState: false);

        ReportDiagnostics = reportDiagnostics;
    }

    public void Dispose()
    {
        Debug.Assert(!_isDisposed);

        _isDisposed = true;
        _cancellationSource.Cancel();
        _cancellationSource.Dispose();

        // Wait for all operations on baseline to finish before we dispose the readers.
        _baselineContentAccessLock.EnterWriteLock();

        lock (_projectEmitBaselinesGuard)
        {
            foreach (var (_, readers) in _initialBaselineModuleReaders)
            {
                readers.metadata.Dispose();
                readers.pdb.Dispose();
            }
        }

        _baselineContentAccessLock.ExitWriteLock();
        _baselineContentAccessLock.Dispose();

        if (Interlocked.Exchange(ref _pendingUpdate, null) != null)
        {
            throw new InvalidOperationException($"Pending update has not been committed or discarded.");
        }
    }

    internal void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(DebuggingSession));
    }

    private void StorePendingUpdate(PendingUpdate update)
    {
        var previousPendingUpdate = Interlocked.Exchange(ref _pendingUpdate, update);

        // commit/discard was not called:
        if (previousPendingUpdate != null)
        {
            throw new InvalidOperationException($"Previous update has not been committed or discarded.");
        }
    }

    private PendingUpdate RetrievePendingUpdate()
    {
        var pendingUpdate = Interlocked.Exchange(ref _pendingUpdate, null);
        if (pendingUpdate == null)
        {
            throw new InvalidOperationException($"No pending update.");
        }

        return pendingUpdate;
    }

    private void EndEditSession()
    {
        var editSessionTelemetryData = EditSession.Telemetry.GetDataAndClear();
        _telemetry.LogEditSession(editSessionTelemetryData);
    }

    public void EndSession(out DebuggingSessionTelemetry.Data telemetryData)
    {
        ThrowIfDisposed();

        EndEditSession();
        telemetryData = _telemetry.GetDataAndClear();
        _reportTelemetry(telemetryData);

        Dispose();

        SessionLog.Write($"Debugging session ended: #{Id}");
    }

    public void BreakStateOrCapabilitiesChanged(bool? inBreakState)
        => RestartEditSession(nonRemappableRegions: null, inBreakState);

    internal void RestartEditSession(ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>>? nonRemappableRegions, bool? inBreakState)
    {
        SessionLog.Write($"Edit session restarted (break state: {inBreakState?.ToString() ?? "null"})");

        ThrowIfDisposed();

        EndEditSession();

        EditSession = new EditSession(
            this,
            nonRemappableRegions ?? EditSession.NonRemappableRegions,
            EditSession.Telemetry,
            (inBreakState == null) ? EditSession.BaseActiveStatements : null,
            inBreakState ?? EditSession.InBreakState);
    }

    internal CompilationOutputs GetCompilationOutputs(Project project)
        => _compilationOutputsProvider(project);

    private bool AddModulePreparedForUpdate(Guid mvid)
    {
        lock (_modulesPreparedForUpdateGuard)
        {
            return _modulesPreparedForUpdate.Add(mvid);
        }
    }

    /// <summary>
    /// Reads the latest MVID of the assembly compiled from given project.
    /// </summary>
    /// <returns>
    /// An MVID and an error message to report, in case an IO exception occurred while reading the binary.
    /// The MVID is <see cref="Guid.Empty"/> if either the project is not built, or the MVID can't be read from the module binary.
    /// </returns>
    internal Task<(Guid Mvid, Diagnostic? Error)> GetProjectModuleIdAsync(Project project, CancellationToken cancellationToken)
    {
        Debug.Assert(project.SupportsEditAndContinue());
        // Note: Does not cache the result as the project may be rebuilt at any point in time.
        return Task.Run(ReadMvid, cancellationToken);

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
                return (Mvid: Guid.Empty, Error: Diagnostic.Create(descriptor, Location.None, [outputs.AssemblyDisplayPath, e.Message]));
            }
        }
    }

    /// <summary>
    /// Get <see cref="EmitBaseline"/> for given project.
    /// </summary>
    /// <param name="moduleId">The current MVID of the project compilation output.</param>
    /// <param name="baselineProject">Project used to create the initial baseline, if the baseline does not exist yet.</param>
    /// <param name="baselineCompilation">Compilation used to create the initial baseline, if the baseline does not exist yet.</param>
    /// <returns>True unless the project outputs can't be read.</returns>
    internal ImmutableList<ProjectBaseline> GetOrCreateEmitBaselines(
        Guid moduleId,
        Project baselineProject,
        Compilation baselineCompilation,
        ArrayBuilder<Diagnostic> diagnostics,
        out ReaderWriterLockSlim baselineAccessLock)
    {
        baselineAccessLock = _baselineContentAccessLock;

        ImmutableList<ProjectBaseline>? existingBaselines;
        lock (_projectEmitBaselinesGuard)
        {
            if (TryGetBaselinesContainingModuleVersion(moduleId, out existingBaselines))
            {
                return existingBaselines;
            }
        }

        var outputs = GetCompilationOutputs(baselineProject);
        if (!TryCreateInitialBaseline(baselineCompilation, outputs, baselineProject.Id, diagnostics, out var initialBaseline, out var debugInfoReaderProvider, out var metadataReaderProvider))
        {
            // Unable to read the DLL/PDB at this point (it might be open by another process).
            // Don't cache the failure so that the user can attempt to apply changes again.
            return [];
        }

        // It is possible to compile a project with assembly references that have
        // the same name but different versions, cultures, or public key tokens,
        // although the SDK targets prevent such references in practice.
        var initiallyReferencedAssemblies = ImmutableDictionary.CreateBuilder<string, OneOrMany<AssemblyIdentity>>();

        foreach (var identity in baselineCompilation.ReferencedAssemblyNames)
        {
            initiallyReferencedAssemblies[identity.Name] = initiallyReferencedAssemblies.TryGetValue(identity.Name, out var value)
                ? value.Add(identity)
                : OneOrMany.Create(identity);
        }

        lock (_projectEmitBaselinesGuard)
        {
            if (TryGetBaselinesContainingModuleVersion(moduleId, out existingBaselines))
            {
                metadataReaderProvider.Dispose();
                debugInfoReaderProvider.Dispose();
                return existingBaselines;
            }

            var newBaseline = new ProjectBaseline(moduleId, baselineProject.Id, initialBaseline, initiallyReferencedAssemblies.ToImmutableDictionary(), generation: 0);
            var baselines = (existingBaselines ?? []).Add(newBaseline);

            _projectBaselines[baselineProject.Id] = baselines;
            _initialBaselineModuleReaders.Add(moduleId, (metadataReaderProvider, debugInfoReaderProvider));

            return baselines;
        }

        bool TryGetBaselinesContainingModuleVersion(Guid moduleId, [NotNullWhen(true)] out ImmutableList<ProjectBaseline>? baselines)
            => _projectBaselines.TryGetValue(baselineProject.Id, out baselines) &&
               baselines.Any(static (b, moduleId) => b.ModuleId == moduleId, moduleId);
    }

    private unsafe bool TryCreateInitialBaseline(
        Compilation compilation,
        CompilationOutputs compilationOutputs,
        ProjectId projectId,
        ArrayBuilder<Diagnostic> diagnostics,
        [NotNullWhen(true)] out EmitBaseline? baseline,
        [NotNullWhen(true)] out DebugInformationReaderProvider? debugInfoReaderProvider,
        [NotNullWhen(true)] out MetadataReaderProvider? metadataReaderProvider)
    {
        // Read the metadata and symbols from the disk. Close the files as soon as we are done emitting the delta to minimize 
        // the time when they are being locked. Since we need to use the baseline that is produced by delta emit for the subsequent
        // delta emit we need to keep the module metadata and symbol info backing the symbols of the baseline alive in memory. 
        // Alternatively, we could drop the data once we are done with emitting the delta and re-emit the baseline again 
        // when we need it next time and the module is loaded.

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
                compilation,
                moduleMetadata,
                debugInfoReader.GetDebugInfo,
                debugInfoReader.GetLocalSignature,
                debugInfoReader.IsPortable);

            success = true;
            return true;
        }
        catch (Exception e)
        {
            SessionLog.Write($"Failed to create baseline for '{projectId.DebugName}': {e.Message}", LogMessageSeverity.Error);

            var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.ErrorReadingFile);
            diagnostics.Add(Diagnostic.Create(descriptor, Location.None, [fileBeingRead, e.Message]));
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
            builder.Add(item.Key, [.. item]);
        }

        return builder.ToImmutable();
    }

    public async ValueTask<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
    {
        try
        {
            if (_isDisposed)
            {
                return [];
            }

            // Not a C# or VB project.
            var project = document.Project;
            if (!project.SupportsEditAndContinue())
            {
                return [];
            }

            // Document does not compile to the assembly (e.g. cshtml files, .g.cs files generated for completion only)
            if (!document.DocumentState.SupportsEditAndContinue())
            {
                return [];
            }

            // Do not analyze documents (and report diagnostics) of projects that have not been built.
            // Allow user to make any changes in these documents, they won't be applied within the current debugging session.
            // Do not report the file read error - it might be an intermittent issue. The error will be reported when the
            // change is attempted to be applied.
            var (mvid, _) = await GetProjectModuleIdAsync(project, cancellationToken).ConfigureAwait(false);
            if (mvid == Guid.Empty)
            {
                return [];
            }

            var (oldDocument, oldDocumentState) = await LastCommittedSolution.GetDocumentAndStateAsync(document, cancellationToken).ConfigureAwait(false);
            if (oldDocumentState is CommittedSolution.DocumentState.OutOfSync or
                CommittedSolution.DocumentState.Indeterminate or
                CommittedSolution.DocumentState.DesignTimeOnly)
            {
                // Do not report diagnostics for existing out-of-sync documents or design-time-only documents.
                return [];
            }

            var analysis = await EditSession.Analyses.GetDocumentAnalysisAsync(LastCommittedSolution, document.Project.Solution, oldDocument, document, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
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

            if (analysis.RudeEdits.IsEmpty)
            {
                return [];
            }

            EditSession.Telemetry.LogRudeEditDiagnostics(analysis.RudeEdits, project.State.Attributes.TelemetryId);

            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return analysis.RudeEdits.SelectAsArray((e, t) => e.ToDiagnostic(t), tree);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            return [];
        }
    }

    public async ValueTask<EmitSolutionUpdateResults> EmitSolutionUpdateAsync(
        Solution solution,
        ImmutableDictionary<ProjectId, RunningProjectOptions> runningProjects,
        ActiveStatementSpanProvider activeStatementSpanProvider,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        var updateId = new UpdateId(Id, Interlocked.Increment(ref _updateOrdinal));

        // Make sure the solution snapshot has all source-generated documents up-to-date.
        solution = solution.WithUpToDateSourceGeneratorDocuments(solution.ProjectIds);

        var solutionUpdate = await EditSession.EmitSolutionUpdateAsync(solution, activeStatementSpanProvider, updateId, runningProjects, cancellationToken).ConfigureAwait(false);

        solutionUpdate.Log(SessionLog, updateId);
        _lastModuleUpdatesLog = solutionUpdate.ModuleUpdates.Updates;

        switch (solutionUpdate.ModuleUpdates.Status)
        {
            case ModuleUpdateStatus.Ready:
                Contract.ThrowIfTrue(solutionUpdate.ModuleUpdates.Updates.IsEmpty && solutionUpdate.ProjectsToRebuild.IsEmpty);

                // We have updates to be applied or processes to restart. The debugger will call Commit/Discard on the solution
                // based on whether the updates will be applied successfully or not.

                StorePendingUpdate(new PendingSolutionUpdate(
                    solution,
                    solutionUpdate.StaleProjects,
                    solutionUpdate.ProjectsToRebuild,
                    solutionUpdate.ProjectBaselines,
                    solutionUpdate.ModuleUpdates.Updates,
                    solutionUpdate.NonRemappableRegions));

                break;

            case ModuleUpdateStatus.None:
                Contract.ThrowIfFalse(solutionUpdate.ModuleUpdates.Updates.IsEmpty);
                Contract.ThrowIfFalse(solutionUpdate.NonRemappableRegions.IsEmpty);

                // Insignificant changes should not cause rebuilds/restarts:
                Contract.ThrowIfFalse(solutionUpdate.ProjectsToRestart.IsEmpty);
                Contract.ThrowIfFalse(solutionUpdate.ProjectsToRebuild.IsEmpty);

                // No significant changes have been made.
                // Commit the solution to apply any insignificant changes that do not generate updates.
                LastCommittedSolution.CommitChanges(solution, solutionUpdate.StaleProjects);
                break;
        }

        // Note that we may return empty deltas if all updates have been deferred.
        // The debugger will still call commit or discard on the update batch.
        return new EmitSolutionUpdateResults()
        {
            Solution = solution,
            ModuleUpdates = solutionUpdate.ModuleUpdates,
            Diagnostics = solutionUpdate.Diagnostics,
            SyntaxError = solutionUpdate.SyntaxError,
            ProjectsToRestart = solutionUpdate.ProjectsToRestart,
            ProjectsToRebuild = solutionUpdate.ProjectsToRebuild,
            ProjectsToRedeploy = solutionUpdate.ProjectsToRedeploy,
        };
    }

    public void CommitSolutionUpdate()
    {
        ThrowIfDisposed();

        ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>>? newNonRemappableRegions = null;
        using var _ = PooledHashSet<ProjectId>.GetInstance(out var projectsToRebuildTransitive);
        IEnumerable<ProjectId> baselinesToDiscard = [];
        Solution? solution = null;

        var pendingUpdate = RetrievePendingUpdate();
        if (pendingUpdate is PendingSolutionUpdate pendingSolutionUpdate)
        {
            // Save new non-remappable regions for the next edit session.
            // If no edits were made the pending list will be empty and we need to keep the previous regions.

            newNonRemappableRegions = GroupToImmutableDictionary(
                from moduleRegions in pendingSolutionUpdate.NonRemappableRegions
                from region in moduleRegions.Regions
                group region.Region by new ManagedMethodId(moduleRegions.ModuleId, region.Method));

            if (newNonRemappableRegions.IsEmpty)
                newNonRemappableRegions = null;

            solution = pendingSolutionUpdate.Solution;

            // Once the project is rebuilt all its dependencies are going to be up-to-date.
            var dependencyGraph = solution.GetProjectDependencyGraph();
            foreach (var projectId in pendingSolutionUpdate.ProjectsToRebuild)
            {
                projectsToRebuildTransitive.Add(projectId);
                projectsToRebuildTransitive.AddRange(dependencyGraph.GetProjectsThatThisProjectTransitivelyDependsOn(projectId));
            }

            // Unstale all projects that will be up-to-date after rebuild.
            LastCommittedSolution.CommitChanges(solution, staleProjects: pendingSolutionUpdate.StaleProjects.RemoveRange(projectsToRebuildTransitive));

            foreach (var projectId in projectsToRebuildTransitive)
            {
                _editSessionTelemetry.LogUpdatedBaseline(solution.GetRequiredProject(projectId).State.ProjectInfo.Attributes.TelemetryId);
            }
        }

        // update baselines:

        // Wait for all operations on baseline content to finish before we dispose the readers.
        _baselineContentAccessLock.EnterWriteLock();

        lock (_projectEmitBaselinesGuard)
        {
            foreach (var updatedBaseline in pendingUpdate.ProjectBaselines)
            {
                _projectBaselines[updatedBaseline.ProjectId] = [.. _projectBaselines[updatedBaseline.ProjectId]
                    .Select(existingBaseline => existingBaseline.ModuleId == updatedBaseline.ModuleId ? updatedBaseline : existingBaseline)];
            }

            // Discard any open baseline readers for projects that need to be rebuilt,
            // so that the build can overwrite the underlying files.
            Contract.ThrowIfNull(solution);
            DiscardProjectBaselinesNoLock(solution, projectsToRebuildTransitive.Concat(baselinesToDiscard));
        }

        _baselineContentAccessLock.ExitWriteLock();

        _editSessionTelemetry.LogCommitted();

        // Restart edit session with no active statements (switching to run mode).
        RestartEditSession(newNonRemappableRegions, inBreakState: false);
    }

    public void DiscardSolutionUpdate()
    {
        ThrowIfDisposed();
        _ = RetrievePendingUpdate();
    }

    private void DiscardProjectBaselinesNoLock(Solution solution, IEnumerable<ProjectId> projects)
    {
        foreach (var projectId in projects)
        {
            if (_projectBaselines.TryGetValue(projectId, out var projectBaselines))
            {
                // remove all versions of modules associated with the project:
                _projectBaselines.Remove(projectId);

                foreach (var projectBaseline in projectBaselines)
                {
                    var (metadata, pdb) = _initialBaselineModuleReaders[projectBaseline.ModuleId];
                    metadata.Dispose();
                    pdb.Dispose();

                    _initialBaselineModuleReaders.Remove(projectBaseline.ModuleId);
                }

                SessionLog.Write($"Baselines discarded: {solution.GetRequiredProject(projectId).GetLogDisplay()}.");
            }
        }
    }

    /// <summary>
    /// Returns <see cref="ActiveStatementSpan"/>s for each document of <paramref name="documentIds"/>,
    /// or default if not in a break state.
    /// </summary>
    public async ValueTask<ImmutableArray<ImmutableArray<ActiveStatementSpan>>> GetBaseActiveStatementSpansAsync(Solution solution, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
    {
        try
        {
            if (_isDisposed || !EditSession.InBreakState)
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
                if (document?.State.SupportsEditAndContinue() != true)
                {
                    // document has been deleted or doesn't support EnC (can't have an active statement anymore):
                    continue;
                }

                if (!document.Project.SupportsEditAndContinue())
                {
                    // document is in a project that does not support EnC
                    continue;
                }

                Contract.ThrowIfNull(document.FilePath);

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
                var oldProject = LastCommittedSolution.GetProject(projectId);
                if (oldProject == null)
                {
                    // Document is in a project that's been added to the solution
                    // No need to map the breakpoint from its original (base) location supplied by the debugger to a new one.
                    continue;
                }

                var newProject = solution.GetRequiredProject(projectId);

                Debug.Assert(oldProject.SupportsEditAndContinue());
                Debug.Assert(newProject.SupportsEditAndContinue());

                var analyzer = newProject.Services.GetRequiredService<IEditAndContinueAnalyzer>();

                await foreach (var documentId in EditSession.GetChangedDocumentsAsync(SessionLog, oldProject, newProject, cancellationToken).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var newDocument = await solution.GetRequiredDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

                    var (oldDocument, _) = await LastCommittedSolution.GetDocumentAndStateAsync(newDocument, cancellationToken).ConfigureAwait(false);
                    if (oldDocument == null)
                    {
                        // Document is either
                        // 1) added -- no need to map the breakpoint from original location to a new one
                        // 2) out-of-sync, in which case we can't reason about its content with respect to the binaries loaded in the debuggee.
                        continue;
                    }

                    var oldDocumentActiveStatements = await baseActiveStatements.GetOldActiveStatementsAsync(analyzer, oldDocument, cancellationToken).ConfigureAwait(false);

                    var analysis = await analyzer.AnalyzeDocumentAsync(
                        newDocument.Id,
                        oldProject,
                        newProject,
                        EditSession.BaseActiveStatements,
                        newActiveStatementSpans: [],
                        EditSession.Capabilities,
                        AnalysisLog,
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
            spans.AddMany([], documentIds.Length);

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

                                return new ActiveStatementSpan(activeStatement.Id, span, activeStatement.Flags, unmappedDocumentId);
                            });
                    }
                }
            }

            documentIndicesByMappedPath.FreeValues();
            activeStatementsInChangedDocuments.FreeValues();

            Debug.Assert(spans.Count == documentIds.Length);
            return spans.ToImmutableAndClear();
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    public async ValueTask<ImmutableArray<ActiveStatementSpan>> GetAdjustedActiveStatementSpansAsync(TextDocument mappedDocument, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
    {
        try
        {
            if (_isDisposed || !EditSession.InBreakState || !mappedDocument.State.SupportsEditAndContinue() || !mappedDocument.Project.SupportsEditAndContinue())
            {
                return [];
            }

            Contract.ThrowIfNull(mappedDocument.FilePath);

            var newProject = mappedDocument.Project;
            var newSolution = newProject.Solution;
            var oldProject = LastCommittedSolution.GetProject(newProject.Id);
            if (oldProject == null)
            {
                // TODO: https://github.com/dotnet/roslyn/issues/79423
                // Enumerate all documents of the new project.
                return [];
            }

            var baseActiveStatements = await EditSession.BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
            if (!baseActiveStatements.DocumentPathMap.TryGetValue(mappedDocument.FilePath, out var oldMappedDocumentActiveStatements))
            {
                // no active statements in this document
                return [];
            }

            var newDocumentActiveStatementSpans = await activeStatementSpanProvider(mappedDocument.Id, mappedDocument.FilePath, cancellationToken).ConfigureAwait(false);
            if (newDocumentActiveStatementSpans.IsEmpty)
            {
                return [];
            }

            var analyzer = newProject.Services.GetRequiredService<IEditAndContinueAnalyzer>();

            using var _ = ArrayBuilder<ActiveStatementSpan>.GetInstance(out var adjustedMappedSpans);

            // Start with the current locations of the tracking spans.
            adjustedMappedSpans.AddRange(newDocumentActiveStatementSpans);

            // Update tracking spans to the latest known locations of the active statements contained in changed documents based on their analysis.
            await foreach (var unmappedDocumentId in EditSession.GetChangedDocumentsAsync(SessionLog, oldProject, newProject, cancellationToken).ConfigureAwait(false))
            {
                var newUnmappedDocument = await newSolution.GetRequiredDocumentAsync(unmappedDocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

                var (oldUnmappedDocument, _) = await LastCommittedSolution.GetDocumentAndStateAsync(newUnmappedDocument, cancellationToken).ConfigureAwait(false);
                if (oldUnmappedDocument == null)
                {
                    // document added or out-of-date 
                    continue;
                }

                var analysis = await EditSession.Analyses.GetDocumentAnalysisAsync(LastCommittedSolution, newUnmappedDocument.Project.Solution, oldUnmappedDocument, newUnmappedDocument, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);

                // Document content did not change or unable to determine active statement spans in a document with syntax errors:
                if (!analysis.ActiveStatements.IsDefault)
                {
                    foreach (var activeStatement in analysis.ActiveStatements)
                    {
                        var i = adjustedMappedSpans.FindIndex(static (s, id) => s.Id == id, activeStatement.Id);
                        if (i >= 0)
                        {
                            adjustedMappedSpans[i] = new ActiveStatementSpan(activeStatement.Id, activeStatement.Span, activeStatement.Flags, unmappedDocumentId);
                        }
                    }
                }
            }

            return adjustedMappedSpans.ToImmutableAndClear();
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    private static void ReportTelemetry(DebuggingSessionTelemetry.Data data)
    {
        // report telemetry (fire and forget):
        _ = Task.Run(() => DebuggingSessionTelemetry.Log(data, Logger.Log, CorrelationIdFactory.GetNextId));
    }

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor(DebuggingSession instance)
    {
        public ImmutableHashSet<Guid> GetModulesPreparedForUpdate()
        {
            lock (instance._modulesPreparedForUpdateGuard)
            {
                return [.. instance._modulesPreparedForUpdate];
            }
        }

        public ImmutableList<ProjectBaseline> GetProjectBaselines(ProjectId projectId)
        {
            lock (instance._projectEmitBaselinesGuard)
            {
                return instance._projectBaselines[projectId];
            }
        }

        public bool HasProjectEmitBaseline(ProjectId projectId)
        {
            lock (instance._projectEmitBaselinesGuard)
            {
                return instance._projectBaselines.ContainsKey(projectId);
            }
        }

        public ImmutableArray<IDisposable> GetBaselineModuleReaders()
        {
            lock (instance._projectEmitBaselinesGuard)
            {
                return [.. instance._initialBaselineModuleReaders.Values.SelectMany(entry => new IDisposable[] { entry.metadata, entry.pdb })];
            }
        }

        public PendingUpdate? GetPendingSolutionUpdate()
            => instance._pendingUpdate;

        public void SetTelemetryLogger(Action<FunctionId, LogMessage> logger, Func<int> getNextId)
            => instance._reportTelemetry = data => DebuggingSessionTelemetry.Log(data, logger, getNextId);
    }
}
