// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    private partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// This is in charge of anything related to <see cref="StateSet"/>
        /// </summary>
        private partial class StateManager(DiagnosticAnalyzerInfoCache analyzerInfoCache)
        {
            private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache = analyzerInfoCache;

            /// <summary>
            /// Analyzers supplied by the host (IDE). These are built-in to the IDE, the compiler, or from an installed IDE extension (VSIX). 
            /// Maps language name to the analyzers and their state.
            /// </summary>
            private ImmutableDictionary<HostAnalyzerStateSetKey, HostAnalyzerStateSets> _hostAnalyzerStateMap = ImmutableDictionary<HostAnalyzerStateSetKey, HostAnalyzerStateSets>.Empty;

            /// <summary>
            /// Analyzers referenced by the project via a PackageReference. Updates are protected by _projectAnalyzerStateMapGuard.
            /// ImmutableDictionary used to present a safe, non-immutable view to users.
            /// </summary>
            private ImmutableDictionary<ProjectId, ProjectAnalyzerStateSets> _projectAnalyzerStateMap = ImmutableDictionary<ProjectId, ProjectAnalyzerStateSets>.Empty;

            /// <summary>
            /// Guard around updating _projectAnalyzerStateMap. This is used in UpdateProjectStateSets to avoid
            /// duplicated calculations for a project during contentious calls.
            /// </summary>
            private readonly SemaphoreSlim _projectAnalyzerStateMapGuard = new(initialCount: 1);

            /// <summary>
            /// Return <see cref="StateSet"/>s for the given <see cref="Project"/>.
            /// This will never create new <see cref="StateSet"/> but will return ones already created.
            /// </summary>
            public ImmutableArray<StateSet> GetStateSets(Project project)
            {
                using var _ = ArrayBuilder<StateSet>.GetInstance(out var result);

                var analyzerReferences = project.Solution.SolutionState.Analyzers.HostAnalyzerReferences;
                foreach (var (key, value) in _hostAnalyzerStateMap)
                {
                    if (key.AnalyzerReferences == analyzerReferences)
                        result.AddRange(value.OrderedStateSets);
                }

                // No need to use _projectAnalyzerStateMapGuard during reads of _projectAnalyzerStateMap
                if (_projectAnalyzerStateMap.TryGetValue(project.Id, out var entry))
                    result.AddRange(entry.StateSetMap.Values);

                return result.ToImmutableAndClear();
            }

            /// <summary>
            /// Return <see cref="StateSet"/>s for the given <see cref="Project"/>. 
            /// This will either return already created <see cref="StateSet"/>s for the specific snapshot of <see cref="Project"/> or
            /// it will create new <see cref="StateSet"/>s for the <see cref="Project"/> and update internal state.
            /// </summary>
            public async Task<ImmutableArray<StateSet>> GetOrCreateStateSetsAsync(Project project, CancellationToken cancellationToken)
            {
                var projectStateSets = await GetOrCreateProjectStateSetsAsync(project, cancellationToken).ConfigureAwait(false);
                return GetOrCreateHostStateSets(project, projectStateSets).OrderedStateSets.AddRange(projectStateSets.StateSetMap.Values);
            }

            /// <summary>
            /// Return <see cref="StateSet"/> for the given <see cref="DiagnosticAnalyzer"/> in the context of <see cref="Project"/>.
            /// This will either return already created <see cref="StateSet"/> for the specific snapshot of <see cref="Project"/> or
            /// it will create new <see cref="StateSet"/> for the <see cref="Project"/>. and update internal state.
            /// </summary>
            public async Task<StateSet?> GetOrCreateStateSetAsync(Project project, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
            {
                var projectStateSets = await GetOrCreateProjectStateSetsAsync(project, cancellationToken).ConfigureAwait(false);
                if (projectStateSets.StateSetMap.TryGetValue(analyzer, out var stateSet))
                {
                    return stateSet;
                }

                var hostStateSetMap = GetOrCreateHostStateSets(project, projectStateSets).StateSetMap;
                if (hostStateSetMap.TryGetValue(analyzer, out stateSet))
                {
                    return stateSet;
                }

                return null;
            }

            private static ImmutableDictionary<DiagnosticAnalyzer, StateSet> CreateStateSetMap(
                IEnumerable<ImmutableArray<DiagnosticAnalyzer>> projectAnalyzerCollection,
                IEnumerable<ImmutableArray<DiagnosticAnalyzer>> hostAnalyzerCollection,
                bool includeWorkspacePlaceholderAnalyzers)
            {
                var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, StateSet>();

                if (includeWorkspacePlaceholderAnalyzers)
                {
                    builder.Add(FileContentLoadAnalyzer.Instance, new StateSet(FileContentLoadAnalyzer.Instance, isHostAnalyzer: true));
                    builder.Add(GeneratorDiagnosticsPlaceholderAnalyzer.Instance, new StateSet(GeneratorDiagnosticsPlaceholderAnalyzer.Instance, isHostAnalyzer: true));
                }

                foreach (var analyzers in projectAnalyzerCollection)
                {
                    foreach (var analyzer in analyzers)
                    {
                        Debug.Assert(analyzer != FileContentLoadAnalyzer.Instance && analyzer != GeneratorDiagnosticsPlaceholderAnalyzer.Instance);

                        // TODO: 
                        // #1, all de-duplication should move to DiagnosticAnalyzerInfoCache
                        // #2, not sure whether de-duplication of analyzer itself makes sense. this can only happen
                        //     if user deliberately put same analyzer twice.
                        if (builder.ContainsKey(analyzer))
                        {
                            continue;
                        }

                        builder.Add(analyzer, new StateSet(analyzer, isHostAnalyzer: false));
                    }
                }

                foreach (var analyzers in hostAnalyzerCollection)
                {
                    foreach (var analyzer in analyzers)
                    {
                        Debug.Assert(analyzer != FileContentLoadAnalyzer.Instance && analyzer != GeneratorDiagnosticsPlaceholderAnalyzer.Instance);

                        // TODO: 
                        // #1, all de-duplication should move to DiagnosticAnalyzerInfoCache
                        // #2, not sure whether de-duplication of analyzer itself makes sense. this can only happen
                        //     if user deliberately put same analyzer twice.
                        if (builder.ContainsKey(analyzer))
                        {
                            continue;
                        }

                        builder.Add(analyzer, new StateSet(analyzer, isHostAnalyzer: true));
                    }
                }

                return builder.ToImmutable();
            }

            private readonly record struct HostAnalyzerStateSetKey(
                string Language, bool HasSdkCodeStyleAnalyzers, IReadOnlyList<AnalyzerReference> AnalyzerReferences);
        }
    }
}
