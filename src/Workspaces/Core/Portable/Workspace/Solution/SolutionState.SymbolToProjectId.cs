// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
    {
        /// <inheritdoc cref="Solution.GetOriginatingProjectId"/>
        public ProjectId? GetOriginatingProjectId(ISymbol? symbol)
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
                    return GetOriginatingProjectId(ns.ConstituentNamespaces[0]);
                }
            }
            else if (symbol.IsKind(SymbolKind.Assembly) ||
                     symbol.IsKind(SymbolKind.NetModule))
            {
                if (!assemblyOrModuleSymbolToProjectId.TryGetValue(symbol, out var projectId))
                {
                    foreach (var (id, tracker) in _projectIdToTrackerMap)
                    {
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
