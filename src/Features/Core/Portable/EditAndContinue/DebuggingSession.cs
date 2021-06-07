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
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.PooledObjects;
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

        private readonly HashSet<Guid> _modulesPreparedForUpdate = new();
        private readonly object _modulesPreparedForUpdateGuard = new();

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

        private readonly DebuggingSessionTelemetry _telemetry;
        internal EditSession EditSession;

        internal DebuggingSession(
            Solution solution,
            IManagedEditAndContinueDebuggerService debuggerService,
            EditAndContinueCapabilities capabilities,
            Func<Project, CompilationOutputs> compilationOutputsProvider,
            IEnumerable<KeyValuePair<DocumentId, CommittedSolution.DocumentState>> initialDocumentStates,
            DebuggingSessionTelemetry debuggingSessionTelemetry,
            EditSessionTelemetry editSessionTelemetry)
        {
            _compilationOutputsProvider = compilationOutputsProvider;
            _telemetry = debuggingSessionTelemetry;

            Capabilities = capabilities;
            DebuggerService = debuggerService;
            LastCommittedSolution = new CommittedSolution(this, solution, initialDocumentStates);
            NonRemappableRegions = ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>>.Empty;
            EditSession = new EditSession(this, editSessionTelemetry, inBreakState: false);
        }

        internal CancellationToken CancellationToken => _cancellationSource.Token;

        public void Dispose()
        {
            _cancellationSource.Cancel();

            foreach (var reader in GetBaselineModuleReaders())
            {
                reader.Dispose();
            }

            _cancellationSource.Dispose();
        }

        private void EndEditSession(out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            documentsToReanalyze = EditSession.GetDocumentsWithReportedDiagnostics();

            var editSessionTelemetryData = EditSession.Telemetry.GetDataAndClear();

            // TODO: report a separate telemetry data for hot reload sessions to preserve the semantics of the current telemetry data
            if (EditSession.InBreakState)
            {
                _telemetry.LogEditSession(editSessionTelemetryData);
            }
        }

        public void EndSession(out ImmutableArray<DocumentId> documentsToReanalyze, out DebuggingSessionTelemetry.Data telemetryData)
        {
            EndEditSession(out documentsToReanalyze);
            telemetryData = _telemetry.GetDataAndClear();
            Dispose();
        }

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
        }
    }
}
