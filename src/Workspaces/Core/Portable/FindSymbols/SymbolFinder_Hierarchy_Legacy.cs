// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    // This file contains the legacy SymbolFinder APIs.  The APIs are legacy because they
    // do not contain enough information for us to effectively remote them over to the OOP
    // process to do the work.  Specifically, they lack the "current project context" necessary
    // to be able to effectively serialize symbols to/from the remote process.

    public static partial class SymbolFinder
    {
        /// <summary>
        /// Find symbols for members that override the specified member symbol.
        /// </summary>
        [Obsolete("Use the overload of FindOverridesAsync that takes a Project", error: false)]
        public static async Task<IEnumerable<ISymbol>> FindOverridesAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            var result = await FindOverridesAsync(
                SymbolAndProjectId.Create(symbol, projectId: null),
                solution, projects, cancellationToken).ConfigureAwait(false);

            return result.SelectAsArray(s => s.Symbol);
        }

        /// <summary>
        /// Find symbols for members that override the specified member symbol. <paramref name="symbol"/> must either be
        /// a source symbol from <paramref name="project"/> or a metadata symbol from one of <paramref
        /// name="project"/>'s <see cref="Project.MetadataReferences"/>.
        /// </summary>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static async Task<ImmutableArray<SymbolDefinition>> FindOverridesAsync(
            ISymbol symbol, Project project, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        {
            var result = await FindOverridesAsync(
                SymbolAndProjectId.Create(symbol, project.Id),
                project.Solution, projects, cancellationToken).ConfigureAwait(false);

            return SymbolDefinition.Create(project.Solution, result);
        }

        /// <summary>
        /// Find symbols for declarations that implement members of the specified interface symbol
        /// </summary>
        [Obsolete("Use the overload of FindImplementedInterfaceMembersAsync that takes a Project", error: false)]
        public static async Task<IEnumerable<ISymbol>> FindImplementedInterfaceMembersAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            var result = await FindImplementedInterfaceMembersAsync(
                SymbolAndProjectId.Create(symbol, projectId: null),
                solution, projects, cancellationToken).ConfigureAwait(false);
            return result.SelectAsArray(s => s.Symbol);
        }

        /// <summary>
        /// Find symbols for declarations that implement members of the specified interface symbol. <paramref
        /// name="symbol"/> must either be a source symbol from <paramref name="project"/> or a metadata symbol from one
        /// of <paramref name="project"/>'s <see cref="Project.MetadataReferences"/>.
        /// </summary>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static async Task<ImmutableArray<SymbolDefinition>> FindImplementedInterfaceMembersAsync(
            ISymbol symbol, Project project, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        {
            var result = await FindImplementedInterfaceMembersAsync(
                SymbolAndProjectId.Create(symbol, project.Id),
                project.Solution, projects, cancellationToken).ConfigureAwait(false);
            return SymbolDefinition.Create(project.Solution, result);
        }

        /// <summary>
        /// Finds the derived classes of the given type. Implementations of an interface are not considered "derived", but can be found
        /// with <see cref="FindImplementationsAsync(ISymbol, Solution, IImmutableSet{Project}, CancellationToken)"/>.
        /// </summary>
        /// <param name="type">The symbol to find derived types of.</param>
        /// <param name="solution">The solution to search in.</param>
        /// <param name="projects">The projects to search. Can be null to search the entire solution.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The derived types of the symbol. The symbol passed in is not included in this list.</returns>
        [Obsolete("Use the overload of FindDerivedClassesAsync that takes a Project", error: false)]
        public static async Task<IEnumerable<INamedTypeSymbol>> FindDerivedClassesAsync(
            INamedTypeSymbol type, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            var result = await FindDerivedClassesAsync(
                SymbolAndProjectId.Create(type, projectId: null),
                solution, projects, cancellationToken).ConfigureAwait(false);
            return result.SelectAsArray(s => s.Symbol);
        }

        /// <summary>
        /// Finds the derived classes of the given type. Implementations of an interface are not considered "derived",
        /// but can be found with <see cref="FindImplementationsAsync(ISymbol, Solution, IImmutableSet{Project},
        /// CancellationToken)"/>.  <paramref name="type"/> must either be a source symbol from <paramref
        /// name="project"/> or a metadata symbol from one of <paramref name="project"/>'s <see
        /// cref="Project.MetadataReferences"/>.
        /// </summary>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static async Task<ImmutableArray<SymbolDefinition>> FindDerivedClassesAsync(
            INamedTypeSymbol type, Project project, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        {
            var result = await FindDerivedClassesAsync(
                SymbolAndProjectId.Create(type, project.Id),
                project.Solution, projects, cancellationToken).ConfigureAwait(false);
            return SymbolDefinition.Create(project.Solution, result);
        }

        /// <summary>
        /// Finds the symbols that implement an interface or interface member.
        /// </summary>
        [Obsolete("Use the overload of FindImplementationsAsync that takes a Project", error: false)]
        public static async Task<IEnumerable<ISymbol>> FindImplementationsAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            var result = await FindImplementationsAsync(
                SymbolAndProjectId.Create(symbol, projectId: null),
                solution, projects, cancellationToken).ConfigureAwait(false);
            return result.SelectAsArray(s => s.Symbol);
        }

        /// <summary>
        /// Finds the symbols that implement an interface or interface member. <paramref name="symbol"/> must either be a
        /// source symbol from <paramref name="project"/> or a metadata symbol from one of <paramref name="project"/>'s
        /// <see cref="Project.MetadataReferences"/>.
        /// </summary>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static async Task<ImmutableArray<SymbolDefinition>> FindImplementationsAsync(
            ISymbol symbol, Project project, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        {
            var result = await FindImplementationsAsync(
                SymbolAndProjectId.Create(symbol, project.Id),
                project.Solution, projects, cancellationToken).ConfigureAwait(false);
            return SymbolDefinition.Create(project.Solution, result);
        }

        /// <summary>
        /// Finds all the callers of a specified symbol.
        /// </summary>
        [Obsolete("Use the overload of FindCallersAsync that takes a Project", error: false)]
        public static async Task<IEnumerable<SymbolCallerInfo>> FindCallersAsync(
            ISymbol symbol, Solution solution, CancellationToken cancellationToken = default)
        {
            return await FindCallersAsync(
                SymbolAndProjectId.Create(symbol, projectId: null),
                solution, documents: null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Finds all the callers of a specified symbol. <paramref name="symbol"/> must either be a
        /// source symbol from <paramref name="project"/> or a metadata symbol from one of <paramref name="project"/>'s
        /// <see cref="Project.MetadataReferences"/>.
        /// </summary>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static Task<ImmutableArray<SymbolCallerInfo>> FindCallersAsync(
            ISymbol symbol, Project project, CancellationToken cancellationToken = default)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        {
            return FindCallersAsync(
                SymbolAndProjectId.Create(symbol, project.Id),
                project.Solution, documents: null, cancellationToken);
        }

        /// <summary>
        /// Finds all the callers of a specified symbol.
        /// </summary>
        [Obsolete("Use the overload of FindCallersAsync that takes a Project", error: false)]
        public static async Task<IEnumerable<SymbolCallerInfo>> FindCallersAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Document> documents, CancellationToken cancellationToken = default)
        {
            return await FindCallersAsync(
                SymbolAndProjectId.Create(symbol, projectId: null),
                solution, documents, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Finds all the callers of a specified symbol. <paramref name="symbol"/> must either be a
        /// source symbol from <paramref name="project"/> or a metadata symbol from one of <paramref name="project"/>'s
        /// <see cref="Project.MetadataReferences"/>.
        /// </summary>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static Task<ImmutableArray<SymbolCallerInfo>> FindCallersAsync(
            ISymbol symbol, Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken = default)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        {
            return FindCallersAsync(
                SymbolAndProjectId.Create(symbol, project.Id),
                project.Solution, documents, cancellationToken);
        }
    }
}
