// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
    {
        /// <summary>
        /// Weak cache from module or assembly symbol to the project it came from.  The project it came from will either
        /// have produced it from it's <see cref="Compilation"/>'s <see cref="Compilation.Assembly"/> or from the <see
        /// cref="IAssemblySymbol"/> or <see cref="IModuleSymbol"/> created by <see
        /// cref="Compilation.GetAssemblyOrModuleSymbol"/> given one of the project's <see
        /// cref="Project.MetadataReferences"/>.
        /// </summary>
        private readonly ConditionalWeakTable<ISymbol, ProjectId?> _moduleOrAssemblySymbolToProjectId =
            new ConditionalWeakTable<ISymbol, ProjectId?>();

        /// <summary>
        /// Weak cache from a <see cref="Compilation"/> to the ID of the <see cref="Project"/> it was created from. Only
        /// used for mapping a <see cref="Project"/>'s <see cref="Compilation.GlobalNamespace"/> back.  These <see
        /// cref="INamespaceSymbol"/>'s do not belong to a <see cref="IAssemblySymbol"/> or <see cref="IModuleSymbol"/>
        /// and thus cannot be tracked with <see cref="_moduleOrAssemblySymbolToProjectId"/>.
        /// </summary>
        private readonly ConditionalWeakTable<Compilation, ProjectId?> _compilationToProjectId =
            new ConditionalWeakTable<Compilation, ProjectId?>();

        /// <summary>
        /// Gets the <see cref="ProjectId"/> associated with an arbitrary symbol.
        /// </summary>
        public ProjectId? GetExactProjectId(ISymbol? symbol, CancellationToken cancellationToken)
        {
            // Walk up the symbol so we can get to the containing namespace/module/assembly that will be used to map
            // back to a project.

            while (symbol != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = GetProjectIdDirectly(symbol);
                if (result != null)
                    return result;

                symbol = symbol.ContainingSymbol;
            }

            return null;

            ProjectId? GetProjectIdDirectly(ISymbol symbol)
            {
                if (symbol.IsKind(SymbolKind.Namespace, out INamespaceSymbol? ns))
                {
                    if (ns.ContainingCompilation != null)
                    {
                        // A namespace that spans a compilation.  These don't belong to an assembly/module. However, we
                        // can map their compilation directly 
                        return _compilationToProjectId.TryGetValue(ns.ContainingCompilation, out var projectId)
                            ? projectId
                            : _compilationToProjectId.GetValue(ns.ContainingCompilation, c => ComputeProjectIdForCompilation(c));
                    }
                }
                else if (symbol.IsKind(SymbolKind.NetModule) ||
                         symbol.IsKind(SymbolKind.Assembly))
                {
                    var result = GetProjectIdForModuleOrAssembly(symbol);
                    if (result != null)
                        return result;
                }

                return null;
            }

            ProjectId? GetProjectIdForModuleOrAssembly(ISymbol symbol)
            {
                Debug.Assert(symbol.Kind == SymbolKind.NetModule || symbol.Kind == SymbolKind.Assembly);

                // Check our cache first (non-allocating).  If not there, go the more expensive route and compute the
                // corresponding project.
                return _moduleOrAssemblySymbolToProjectId.TryGetValue(symbol, out var projectId)
                    ? projectId
                    : _moduleOrAssemblySymbolToProjectId.GetValue(symbol, sym => ComputeProjectIdForModuleOrAssembly(sym));
            }

            ProjectId? ComputeProjectIdForCompilation(Compilation compilation)
            {
                // Look through each project to see if we can find the one that produced this compilation.
                foreach (var (id, _) in this.ProjectStates)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // No need to create the compilation for the project.  If the project didn't have a compilation, it
                    // couldn't have produced this compilation we're looking for.
                    if (this.TryGetCompilation(id, out var otherCompilation) &&
                        otherCompilation == compilation)
                    {
                        return id;
                    }
                }

                return null;
            }

            ProjectId? ComputeProjectIdForModuleOrAssembly(ISymbol symbol)
            {
                // First, check if this was the source assembly symbol for a specific project.
                if (symbol is IAssemblySymbol assemblySymbol)
                {
                    foreach (var (id, _) in this.ProjectStates)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // A symbol can only belong to a project if that project actually produced it from it's compilation.
                        // So, if we have no compilation, this definitely isn't a match.
                        if (!this.TryGetCompilation(id, out var compilation))
                            continue;

                        // See if this is a reference to the source assembly for this compilation itself.
                        if (compilation.Assembly.Equals(symbol))
                            return id;
                    }
                }

                // Now, see if this was an assembly symbol created for a metadata reference.
                foreach (var (id, _) in this.ProjectStates)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // A symbol can only belong to a project if that project actually produced it from it's compilation.
                    // So, if we have no compilation, this definitely isn't a match.
                    if (!this.TryGetCompilation(id, out var compilation))
                        continue;

                    // Now, see if it was a reference to to any of the metadata assembly symbols for this project.
                    foreach (var metadataReference in compilation.References)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var assemblyOrModule = compilation.GetAssemblyOrModuleSymbol(metadataReference);
                        if (symbol.Equals(assemblyOrModule))
                            return id;
                    }
                }

                return null;
            }
        }
    }
}
