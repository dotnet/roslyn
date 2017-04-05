// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        /// <summary>
        /// Find the declared symbols from either source, referenced projects or metadata assemblies with the specified name.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindDeclarationsAsync(
            Project project, string name, bool ignoreCase, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return ImmutableArray<ISymbol>.Empty;
            }

            return await FindAllDeclarationsWithNormalQueryAsync(
                project, SearchQuery.Create(name, ignoreCase), SymbolFilter.All, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Find the declared symbols from either source, referenced projects or metadata assemblies with the specified name.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindDeclarationsAsync(
            Project project, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return ImmutableArray<ISymbol>.Empty;
            }

            return await FindAllDeclarationsWithNormalQueryAsync(
                project, SearchQuery.Create(name, ignoreCase), filter, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        internal static Task<ImmutableArray<ISymbol>> FindAllDeclarationsWithNormalQueryAsync(
            Project project, SearchQuery query, SymbolFilter filter, CancellationToken cancellationToken)
        {
            // All entrypoints to this function are Find functions that are only searching
            // for specific strings (i.e. they never do a custom search).
            Debug.Assert(query.Kind != SearchKind.Custom);

            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (query.Name != null && string.IsNullOrWhiteSpace(query.Name))
            {
                return SpecializedTasks.EmptyImmutableArray<ISymbol>();
            }

            using (Logger.LogBlock(FunctionId.SymbolFinder_FindDeclarationsAsync, cancellationToken))
            {
                return FindAllDeclarationsWithNormalQueryAsyncImpl(project, query, filter, cancellationToken);
            }
        }

        private static async Task<ImmutableArray<ISymbol>> FindAllDeclarationsWithNormalQueryAsyncImpl(
            Project project, SearchQuery query, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            // All entrypoints to this function are Find functions that are only searching
            // for specific strings (i.e. they never do a custom search).
            Debug.Assert(query.Kind != SearchKind.Custom);

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            var list = ArrayBuilder<ISymbol>.GetInstance();

            // get declarations from the compilation's assembly
            await AddDeclarationsWithNormalQueryAsync(project, query, criteria, list, cancellationToken).ConfigureAwait(false);

            // get declarations from directly referenced projects and metadata
            foreach (var assembly in compilation.GetReferencedAssemblySymbols())
            {
                var assemblyProject = project.Solution.GetProject(assembly, cancellationToken);
                if (assemblyProject != null)
                {
                    await AddDeclarationsWithNormalQueryAsync(assemblyProject, query, criteria, list, compilation, assembly, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await AddDeclarationsWithNormalQueryAsync(
                        project.Solution, assembly, compilation.GetMetadataReference(assembly) as PortableExecutableReference,
                        query, criteria, list, cancellationToken).ConfigureAwait(false);
                }
            }

            return TranslateNamespaces(list.ToImmutableAndFree(), compilation);
        }

        /// <summary>
        /// Makes certain all namespace symbols returned by API are from the compilation.
        /// </summary>
        private static ImmutableArray<ISymbol> TranslateNamespaces(
            ImmutableArray<ISymbol> symbols, Compilation compilation)
        {
            var builder = ArrayBuilder<ISymbol>.GetInstance();
            foreach (var symbol in symbols)
            {
                var ns = symbol as INamespaceSymbol;
                if (ns != null)
                {
                    builder.Add(compilation.GetCompilationNamespace(ns));
                }
                else
                {
                    builder.Add(symbol);
                }
            }

            var result = builder.Count == symbols.Length
                ? symbols
                : builder.ToImmutable();

            builder.Free();

            return result;
        }

        private static Task AddDeclarationsWithNormalQueryAsync(
            Project project, SearchQuery query, SymbolFilter filter,
            ArrayBuilder<ISymbol> list, CancellationToken cancellationToken)
        {
            Debug.Assert(query.Kind != SearchKind.Custom);
            return AddDeclarationsWithNormalQueryAsync(
                project, query, filter, list,
                startingCompilation: null,
                startingAssembly: null,
                cancellationToken: cancellationToken);
        }

        private static async Task AddDeclarationsWithNormalQueryAsync(
            Project project,
            SearchQuery query,
            SymbolFilter filter,
            ArrayBuilder<ISymbol> list,
            Compilation startingCompilation,
            IAssemblySymbol startingAssembly,
            CancellationToken cancellationToken)
        {
            Debug.Assert(query.Kind != SearchKind.Custom);

            using (Logger.LogBlock(FunctionId.SymbolFinder_Project_AddDeclarationsAsync, cancellationToken))
            {
                if (!await project.ContainsSymbolsWithNameAsync(query.GetPredicate(), filter, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                var unfilteredSymbols = await GetUnfilteredSymbolsWithNormalQueryAsync(
                    project, query, filter, startingCompilation, startingAssembly, cancellationToken).ConfigureAwait(false);
                list.AddRange(FilterByCriteria(unfilteredSymbols, filter));
            }
        }

        private static async Task<ImmutableArray<ISymbol>> GetUnfilteredSymbolsWithNormalQueryAsync(
            Project project,
            SearchQuery query,
            SymbolFilter filter,
            Compilation startingCompilation,
            IAssemblySymbol startingAssembly,
            CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (startingCompilation != null && startingAssembly != null && compilation.Assembly != startingAssembly)
            {
                // Return symbols from skeleton assembly in this case so that symbols have the same language as startingCompilation.
                return compilation.GetSymbolsWithName(query.GetPredicate(), filter, cancellationToken)
                    .Select(s => s.GetSymbolKey().Resolve(startingCompilation, cancellationToken: cancellationToken).Symbol)
                    .WhereNotNull()
                    .ToImmutableArray();
            }
            else
            {
                return compilation.GetSymbolsWithName(query.GetPredicate(), filter, cancellationToken)
                                  .ToImmutableArray();
            }
        }

        private static async Task AddDeclarationsWithNormalQueryAsync(
            Solution solution, IAssemblySymbol assembly, PortableExecutableReference referenceOpt, 
            SearchQuery query, SymbolFilter filter, ArrayBuilder<ISymbol> list, CancellationToken cancellationToken)
        {
            // All entrypoints to this function are Find functions that are only searching
            // for specific strings (i.e. they never do a custom search).
            Debug.Assert(query.Kind != SearchKind.Custom);

            using (Logger.LogBlock(FunctionId.SymbolFinder_Assembly_AddDeclarationsAsync, cancellationToken))
            {
                if (referenceOpt != null)
                {
                    var info = await SymbolTreeInfo.TryGetInfoForMetadataReferenceAsync(
                        solution, referenceOpt, loadOnly: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (info != null)
                    {
                        var symbols = await info.FindAsync(query, assembly, filter, cancellationToken).ConfigureAwait(false);
                        list.AddRange(symbols);
                    }
                }
            }
        }

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Solution solution, string name, bool ignoreCase, CancellationToken cancellationToken = default(CancellationToken))
            => FindSourceDeclarationsAsync(solution, name, ignoreCase, SymbolFilter.All, cancellationToken);

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(
            Solution solution, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return ImmutableArray<ISymbol>.Empty;
            }

            using (Logger.LogBlock(FunctionId.SymbolFinder_Solution_Name_FindSourceDeclarationsAsync, cancellationToken))
            {
                return await FindSourceDeclarationsWithNormalQueryAsyncImpl(
                    solution, SearchQuery.Create(name, ignoreCase), filter, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<ImmutableArray<ISymbol>> FindSourceDeclarationsWithNormalQueryAsyncImpl(
            Solution solution, SearchQuery query, SymbolFilter filter, CancellationToken cancellationToken)
        {
            Debug.Assert(query.Kind != SearchKind.Custom);

            if (query.Name != null && string.IsNullOrWhiteSpace(query.Name))
            {
                return ImmutableArray<ISymbol>.Empty;
            }

            var result = ArrayBuilder<ISymbol>.GetInstance();
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                await AddDeclarationsWithNormalQueryAsync(project, query, filter, result, cancellationToken).ConfigureAwait(false);
            }

            return result.ToImmutableAndFree();
        }

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Project project, string name, bool ignoreCase, CancellationToken cancellationToken = default(CancellationToken))
            => FindSourceDeclarationsAsync(project, name, ignoreCase, SymbolFilter.All, cancellationToken);

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(
            Project project, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return SpecializedCollections.EmptyEnumerable<ISymbol>();
            }

            using (Logger.LogBlock(FunctionId.SymbolFinder_Project_Name_FindSourceDeclarationsAsync, cancellationToken))
            {
                var list = ArrayBuilder<ISymbol>.GetInstance();
                await AddDeclarationsWithNormalQueryAsync(
                    project, SearchQuery.Create(name, ignoreCase), filter, list, cancellationToken).ConfigureAwait(false);
                return list.ToImmutableAndFree();
            }
        }

        internal static ImmutableArray<ISymbol> FilterByCriteria(
            ImmutableArray<ISymbol> symbols, SymbolFilter criteria)
        {
            var builder = ArrayBuilder<ISymbol>.GetInstance();
            foreach (var symbol in symbols)
            {
                if (symbol.IsImplicitlyDeclared || symbol.IsAccessor())
                {
                    continue;
                }

                if (MeetCriteria(symbol, criteria))
                {
                    builder.Add(symbol);
                }
            }

            var result = builder.Count == symbols.Length
                ? symbols
                : builder.ToImmutable();

            builder.Free();
            return result;
        }

        private static bool MeetCriteria(ISymbol symbol, SymbolFilter filter)
        {
            if (IsOn(filter, SymbolFilter.Namespace) && symbol.Kind == SymbolKind.Namespace)
            {
                return true;
            }

            if (IsOn(filter, SymbolFilter.Type) && symbol is ITypeSymbol)
            {
                return true;
            }

            if (IsOn(filter, SymbolFilter.Member) && IsNonTypeMember(symbol))
            {
                return true;
            }

            return false;
        }

        private static bool IsNonTypeMember(ISymbol symbol)
        {
            return symbol.Kind == SymbolKind.Method ||
                   symbol.Kind == SymbolKind.Property ||
                   symbol.Kind == SymbolKind.Event ||
                   symbol.Kind == SymbolKind.Field;
        }

        private static bool IsOn(SymbolFilter filter, SymbolFilter flag)
        {
            return (filter & flag) == flag;
        }
    }
}