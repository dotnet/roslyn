// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Represents a debugging session.
    /// </summary>
    internal sealed class DebuggingSession : IDisposable
    {
        public readonly Workspace Workspace;
        public readonly IActiveStatementProvider ActiveStatementProvider;
        public readonly IDebuggeeModuleMetadataProvider DebugeeModuleMetadataProvider;
        public readonly ICompilationOutputsProviderService CompilationOutputsProvider;

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
        private List<IDisposable> _lazyBaselineModuleReaders;
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
        /// or the solution which the last changes commited to the debuggee at the end of edit session were calculated from.
        /// The solution reflecting the current state of the modules loaded in the debugee.
        /// </summary>
        internal readonly CommittedSolution LastCommittedSolution;

        internal DebuggingSession(
            Workspace workspace,
            IDebuggeeModuleMetadataProvider debugeeModuleMetadataProvider,
            IActiveStatementProvider activeStatementProvider,
            ICompilationOutputsProviderService compilationOutputsProvider)
        {
            Debug.Assert(workspace != null);
            Debug.Assert(debugeeModuleMetadataProvider != null);

            Workspace = workspace;
            DebugeeModuleMetadataProvider = debugeeModuleMetadataProvider;
            CompilationOutputsProvider = compilationOutputsProvider;
            _projectModuleIds = new Dictionary<ProjectId, (Guid, Diagnostic)>();
            _projectEmitBaselines = new Dictionary<ProjectId, EmitBaseline>();
            _modulesPreparedForUpdate = new HashSet<Guid>();

            ActiveStatementProvider = activeStatementProvider;

            LastCommittedSolution = new CommittedSolution(this, workspace.CurrentSolution);
            NonRemappableRegions = ImmutableDictionary<ActiveMethodId, ImmutableArray<NonRemappableRegion>>.Empty;
        }

        // test only
        internal void Test_SetNonRemappableRegions(ImmutableDictionary<ActiveMethodId, ImmutableArray<NonRemappableRegion>> nonRemappableRegions)
        {
            NonRemappableRegions = nonRemappableRegions;
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

        internal void PrepareModuleForUpdate(Guid mvid)
        {
            lock (_modulesPreparedForUpdateGuard)
            {
                if (!_modulesPreparedForUpdate.Add(mvid))
                {
                    return;
                }
            }

            DebugeeModuleMetadataProvider.PrepareModuleForUpdate(mvid);
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
                    if (_lazyBaselineModuleReaders == null)
                    {
                        _lazyBaselineModuleReaders = new List<IDisposable>();
                    }

                    _lazyBaselineModuleReaders.AddRange(update.ModuleReaders);
                }
            }

            LastCommittedSolution.CommitSolution(update.Solution, update.ChangedDocuments);
        }

        /// <summary>
        /// Reads the MVID of a compiled project.
        /// </summary>
        /// <returns>
        /// An MVID and an error message to report, in case an IO exception occurred while reading the binary.
        /// The MVID is default if either project not built, or an it can't be read from the module binary.
        /// </returns>
        public async Task<(Guid Mvid, Diagnostic Error)> GetProjectModuleIdAsync(ProjectId projectId, CancellationToken cancellationToken)
        {
            lock (_projectModuleIdsGuard)
            {
                if (_projectModuleIds.TryGetValue(projectId, out var id))
                {
                    return id;
                }
            }

            (Guid Mvid, Diagnostic Error) ReadMvid()
            {
                var outputs = CompilationOutputsProvider.GetCompilationOutputs(projectId);

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
                if (_projectModuleIds.TryGetValue(projectId, out var id))
                {
                    return id;
                }

                return _projectModuleIds[projectId] = newId;
            }
        }

        /// <summary>
        /// Get <see cref="EmitBaseline"/> for given project.
        /// Must be called on MTA thread.
        /// </summary>
        /// <returns>Null if the module corresponding to he project hasn't been loaded yet</returns>
        /// <exception cref="IOException">Error reading project's binary.</exception>
        public EmitBaseline GetOrCreateEmitBaseline(ProjectId projectId, Guid mvid)
        {
            Debug.Assert(Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA, "SymReader requires MTA");

            EmitBaseline baseline;
            lock (_projectEmitBaselinesGuard)
            {
                if (_projectEmitBaselines.TryGetValue(projectId, out baseline))
                {
                    return baseline;
                }
            }

            var moduleInfo = DebugeeModuleMetadataProvider.TryGetBaselineModuleInfo(mvid);
            if (moduleInfo == null)
            {
                // Module not loaded.
                // Do not cache this result as the module may be loaded in the next edit session.
                return null;
            }

            var infoReader = EditAndContinueMethodDebugInfoReader.Create(moduleInfo.SymReader, version: 1);

            var newBaseline = EmitBaseline.CreateInitialBaseline(
                moduleInfo.Metadata,
                infoReader.GetDebugInfo,
                infoReader.GetLocalSignature,
                infoReader.IsPortable);

            lock (_projectEmitBaselinesGuard)
            {
                if (_projectEmitBaselines.TryGetValue(projectId, out baseline))
                {
                    return baseline;
                }

                return _projectEmitBaselines[projectId] = newBaseline;
            }
        }

        private static ImmutableDictionary<K, ImmutableArray<V>> GroupToImmutableDictionary<K, V>(IEnumerable<IGrouping<K, V>> items)
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
