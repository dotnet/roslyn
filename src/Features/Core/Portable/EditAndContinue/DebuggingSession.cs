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
using Roslyn.Utilities;

#nullable enable

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Represents a debugging session.
    /// </summary>
    internal sealed class DebuggingSession : IDisposable
    {
        private readonly Func<Project, CompilationOutputs> _compilationOutputsProvider;
        private readonly CancellationTokenSource _cancellationSource = new CancellationTokenSource();

        /// <summary>
        /// MVIDs read from the assembly built for given project id.
        /// </summary>
        private readonly Dictionary<ProjectId, (Guid Mvid, Diagnostic Error)> _projectModuleIds;
        private readonly object _projectModuleIdsGuard = new object();

        /// <summary>
        /// The current baseline for given project id.
        /// The baseline is updated when changes are committed at the end of edit session.
        /// The backing module readers of some baselines need to be kept alive -- store them in
        /// <see cref="_lazyBaselineModuleReaders"/> and dispose them at the end of the debugging session
        /// </summary>
        private readonly Dictionary<ProjectId, EmitBaseline> _projectEmitBaselines;
        private List<IDisposable>? _lazyBaselineModuleReaders;
        private readonly object _projectEmitBaselinesGuard = new object();

        // Maps active statement instructions to their latest spans.
        // Consumed by the next edit session and updated when changes are committed at the end of the edit session.
        //
        // Consider a function F containing a call to function G that is updated a couple of times
        // before the thread returns from G and is remapped to the latest version of F.
        // '>' indicates an active statement instruction.
        //
        // F v1:        F v2:       F v3:
        // 0: nop       0: nop      0: nop
        // 1> G()       1: nop      1: nop
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
        internal ImmutableDictionary<ActiveMethodId, ImmutableArray<NonRemappableRegion>> NonRemappableRegions { get; private set; }

        private readonly HashSet<Guid> _modulesPreparedForUpdate;
        private readonly object _modulesPreparedForUpdateGuard = new object();

        /// <summary>
        /// The solution captured when the debugging session entered run mode (application debugging started),
        /// or the solution which the last changes committed to the debuggee at the end of edit session were calculated from.
        /// The solution reflecting the current state of the modules loaded in the debugee.
        /// </summary>
        internal readonly CommittedSolution LastCommittedSolution;

        internal DebuggingSession(
            Solution solution,
            Func<Project, CompilationOutputs> compilationOutputsProvider)
        {
            _compilationOutputsProvider = compilationOutputsProvider;
            _projectModuleIds = new Dictionary<ProjectId, (Guid, Diagnostic)>();
            _projectEmitBaselines = new Dictionary<ProjectId, EmitBaseline>();
            _modulesPreparedForUpdate = new HashSet<Guid>();

            LastCommittedSolution = new CommittedSolution(this, solution);
            NonRemappableRegions = ImmutableDictionary<ActiveMethodId, ImmutableArray<NonRemappableRegion>>.Empty;
        }

        // test only
        internal void Test_SetNonRemappableRegions(ImmutableDictionary<ActiveMethodId, ImmutableArray<NonRemappableRegion>> nonRemappableRegions)
            => NonRemappableRegions = nonRemappableRegions;

        // test only
        internal ImmutableHashSet<Guid> Test_GetModulesPreparedForUpdate()
        {
            lock (_modulesPreparedForUpdateGuard)
            {
                return _modulesPreparedForUpdate.ToImmutableHashSet();
            }
        }

        // test only
        internal EmitBaseline Test_GetProjectEmitBaseline(ProjectId id)
        {
            lock (_projectEmitBaselinesGuard)
            {
                return _projectEmitBaselines[id];
            }
        }

        // internal for testing
        internal ImmutableArray<IDisposable> GetBaselineModuleReaders()
        {
            lock (_projectEmitBaselinesGuard)
            {
                return _lazyBaselineModuleReaders.ToImmutableArrayOrEmpty();
            }
        }

        internal CancellationToken CancellationToken => _cancellationSource.Token;
        internal void Cancel() => _cancellationSource.Cancel();

        public void Dispose()
        {
            foreach (var reader in GetBaselineModuleReaders())
            {
                reader.Dispose();
            }

            _cancellationSource.Dispose();
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
                from delta in update.Deltas
                from region in delta.NonRemappableRegions
                group region.Region by region.Method);

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

                if (!update.ModuleReaders.IsEmpty)
                {
                    _lazyBaselineModuleReaders ??= new List<IDisposable>();
                    _lazyBaselineModuleReaders.AddRange(update.ModuleReaders);
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
                catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
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

                return _projectModuleIds[project.Id] = newId;
            }
        }

        /// <summary>
        /// Get <see cref="EmitBaseline"/> for given project.
        /// </summary>
        /// <returns>True unless the project outputs can't be read.</returns>
        public bool TryGetOrCreateEmitBaseline(
            Project project,
            ArrayBuilder<IDisposable> readers,
            out ImmutableArray<Diagnostic> diagnostics,
            [NotNullWhen(true)] out EmitBaseline? baseline)
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
            }

            readers.Add(metadataReaderProvider);
            readers.Add(debugInfoReaderProvider);
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
    }
}
