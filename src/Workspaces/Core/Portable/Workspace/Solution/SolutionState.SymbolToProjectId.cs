// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
    {
        /// <summary>
        /// Contains caches to help us quickly and accurately map from <see cref="ISymbol"/>s to <see
        /// cref="ProjectId"/>.
        /// </summary>
        /// <remarks>
        /// Note: this is not <see langword="readonly"/> as this is a struct and will mutate as operations are called on
        /// it.  However, in the common case where this data is not needed, it will have not have any impact.
        /// </remarks>
        private SymbolToProjectId _symbolToProjectId;

        /// <inheritdoc cref="Solution.GetExactProjectId"/>
        public ProjectId? GetExactProjectId(ISymbol symbol)
            => _symbolToProjectId.GetExactProjectId(symbol);

        /// <summary>
        /// A helper type that can be used to map from symbols to a project they could have come from.
        /// </summary>
        private struct SymbolToProjectId
        {
            private readonly SolutionState _solutionState;

            /// <summary>
            /// Cache we use to map between assembly and module symbols and the project they came from.  That way if we
            /// are asked about many symbols from the same assembly/module we can answer the question quickly after
            /// computing for the first one.
            /// </summary>
            private ConditionalWeakTable<ISymbol, ProjectId?>? _assemblyOrModuleSymbolToProjectId;
            private static readonly Func<ConditionalWeakTable<ISymbol, ProjectId?>> s_createTable = () => new ConditionalWeakTable<ISymbol, ProjectId?>();

            public SymbolToProjectId(SolutionState solutionState)
            {
                _solutionState = solutionState;
                _assemblyOrModuleSymbolToProjectId = null;
            }

            /// <inheritdoc cref="Solution.GetExactProjectId(ISymbol)"/>.
            public ProjectId? GetExactProjectId(ISymbol? symbol)
            {
                LazyInitialization.EnsureInitialized(ref _assemblyOrModuleSymbolToProjectId, s_createTable);

                // Walk up the symbol so we can get to the containing namespace/assembly that will be used to map
                // back to a project.

                while (symbol != null)
                {
                    var result = GetProjectIdDirectly(symbol, _assemblyOrModuleSymbolToProjectId);
                    if (result != null)
                        return result;

                    symbol = symbol.ContainingSymbol;
                }

                return null;
            }

            private ProjectId? GetProjectIdDirectly(
                ISymbol symbol, ConditionalWeakTable<ISymbol, ProjectId?> assemblyOrModuleSymbolToProjectId)
            {
                if (symbol.IsKind(SymbolKind.Namespace, out INamespaceSymbol? ns))
                {
                    if (ns.ContainingCompilation != null)
                    {
                        // A namespace that spans a compilation.  These don't belong to an assembly/module directly.
                        // However, as we're looking for the project this corresponds to, we can look for the
                        // source-module component (the first in the constituent namespaces) and then search using that.
                        return GetExactProjectId(ns.ConstituentNamespaces[0]);
                    }
                }
                else if (symbol.IsKind(SymbolKind.Assembly) ||
                         symbol.IsKind(SymbolKind.NetModule))
                {
                    if (!assemblyOrModuleSymbolToProjectId.TryGetValue(symbol, out var projectId))
                    {
                        foreach (var (id, state) in _solutionState.ProjectStates)
                        {
                            var tracker = _solutionState.GetCompilationTracker(id);
                            if (tracker.ContainsAssemblyOrModule(symbol))
                            {
                                projectId = id;
                                break;
                            }
                        }

                        // Have to lock as there's no atomic AddOrUpdate in netstandard2.0 and we could throw if two
                        // threads tried to add the same item.
#if NETSTANDARD
                        lock (assemblyOrModuleSymbolToProjectId)
                        {
                            assemblyOrModuleSymbolToProjectId.Remove(symbol);
                            assemblyOrModuleSymbolToProjectId.Add(symbol, projectId);
                        }
#else
                        assemblyOrModuleSymbolToProjectId.AddOrUpdate(symbol, projectId);
#endif
                    }

                    return projectId;
                }

                return null;
            }
        }
    }
}
