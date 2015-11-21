// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        /// <summary>
        /// Find the declared symbols from either source, referenced projects or metadata assemblies with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindDeclarationsAsync(Project project, string name, bool ignoreCase, CancellationToken cancellationToken = default(CancellationToken))
        {
            return FindDeclarationsAsync(project, name, ignoreCase, SymbolFilter.All, cancellationToken);
        }

        /// <summary>
        /// Find the declared symbols from either source, referenced projects or metadata assemblies with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindDeclarationsAsync(Project project, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken = default(CancellationToken))
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
                return SpecializedTasks.EmptyEnumerable<ISymbol>();
            }

            using (Logger.LogBlock(FunctionId.SymbolFinder_FindDeclarationsAsync, cancellationToken))
            {
                return FindDeclarationsAsyncImpl(project, name, ignoreCase, filter, cancellationToken);
            }
        }

        private static async Task<IEnumerable<ISymbol>> FindDeclarationsAsyncImpl(
            Project project, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            var list = new List<ISymbol>();

            // get declarations from the compilation's assembly
            await AddDeclarationsAsync(project, name, ignoreCase, criteria, list, cancellationToken).ConfigureAwait(false);

            // get declarations from directly referenced projects and metadata
            foreach (var assembly in compilation.GetReferencedAssemblySymbols())
            {
                var assemblyProject = project.Solution.GetProject(assembly, cancellationToken);
                if (assemblyProject != null)
                {
                    await AddDeclarationsAsync(assemblyProject, compilation, assembly, name, ignoreCase, criteria, list, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await AddDeclarationsAsync(project.Solution, assembly, GetMetadataReferenceFilePath(compilation.GetMetadataReference(assembly)), name, ignoreCase, criteria, list, cancellationToken).ConfigureAwait(false);
                }
            }

            return TranslateNamespaces(list, compilation);
        }

        private static string GetMetadataReferenceFilePath(MetadataReference metadataReference)
        {
            return (metadataReference as PortableExecutableReference)?.FilePath;
        }

        /// <summary>
        /// Makes certain all namespace symbols returned by API are from the compilation.
        /// </summary>
        private static IEnumerable<ISymbol> TranslateNamespaces(List<ISymbol> symbols, Compilation compilation)
        {
            foreach (var symbol in symbols)
            {
                var ns = symbol as INamespaceSymbol;
                if (ns != null)
                {
                    yield return compilation.GetCompilationNamespace(ns);
                }
                else
                {
                    yield return symbol;
                }
            }
        }

        private static async Task AddDeclarationsAsync(Project project, string name, bool ignoreCase, SymbolFilter filter, List<ISymbol> list, CancellationToken cancellationToken)
        {
            await AddDeclarationsAsync(project, null, null, name, ignoreCase, filter, list, cancellationToken).ConfigureAwait(false);
        }

        private static async Task AddDeclarationsAsync(Project project, Compilation startingCompilation, IAssemblySymbol startingAssembly, string name, bool ignoreCase, SymbolFilter filter, List<ISymbol> list, CancellationToken cancellationToken)
        {
            Func<string, bool> predicate = n => ignoreCase ? CaseInsensitiveComparison.Comparer.Equals(name, n) : StringComparer.Ordinal.Equals(name, n);

            using (Logger.LogBlock(FunctionId.SymbolFinder_Project_AddDeclarationsAsync, cancellationToken))
            using (var set = SharedPools.Default<HashSet<ISymbol>>().GetPooledObject())
            {
                if (!await project.ContainsSymbolsWithNameAsync(predicate, filter, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                if ((startingCompilation != null) && (startingAssembly != null) && (compilation.Assembly != startingAssembly))
                {
                    // Return symbols from skeleton assembly in this case so that symbols have the same language as startingCompilation.
                    list.AddRange(
                        FilterByCriteria(compilation.GetSymbolsWithName(predicate, filter, cancellationToken), filter)
                            .Select(s => s.GetSymbolKey().Resolve(startingCompilation, cancellationToken: cancellationToken).Symbol).WhereNotNull());
                }
                else
                {
                    list.AddRange(FilterByCriteria(compilation.GetSymbolsWithName(predicate, filter, cancellationToken), filter));
                }
            }
        }

        private static async Task AddDeclarationsAsync(
            Solution solution, IAssemblySymbol assembly, string filePath, string name, bool ignoreCase, SymbolFilter filter, List<ISymbol> list, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.SymbolFinder_Assembly_AddDeclarationsAsync, cancellationToken))
            {
                var info = await SymbolTreeInfo.GetInfoForAssemblyAsync(solution, assembly, filePath, cancellationToken).ConfigureAwait(false);
                if (info.HasSymbols(name, ignoreCase))
                {
                    list.AddRange(FilterByCriteria(info.Find(assembly, name, ignoreCase, cancellationToken), filter));
                }
            }
        }

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Solution solution, string name, bool ignoreCase, CancellationToken cancellationToken = default(CancellationToken))
        {
            return FindSourceDeclarationsAsync(solution, name, ignoreCase, SymbolFilter.All, cancellationToken);
        }

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Solution solution, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken = default(CancellationToken))
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
                return SpecializedTasks.EmptyEnumerable<ISymbol>();
            }

            using (Logger.LogBlock(FunctionId.SymbolFinder_Solution_Name_FindSourceDeclarationsAsync, cancellationToken))
            {
                return FindSourceDeclarationsAsyncImpl(solution, name, ignoreCase, filter, cancellationToken);
            }
        }

        private static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsyncImpl(
            Solution solution, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken)
        {
            var result = new List<ISymbol>();
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                var symbols = await FindSourceDeclarationsAsyncImpl(project, name, ignoreCase, filter, cancellationToken).ConfigureAwait(false);
                result.AddRange(symbols);
            }

            return result;
        }

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Project project, string name, bool ignoreCase, CancellationToken cancellationToken = default(CancellationToken))
        {
            return FindSourceDeclarationsAsync(project, name, ignoreCase, SymbolFilter.All, cancellationToken);
        }

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Project project, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken = default(CancellationToken))
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
                return SpecializedTasks.EmptyEnumerable<ISymbol>();
            }

            using (Logger.LogBlock(FunctionId.SymbolFinder_Project_Name_FindSourceDeclarationsAsync, cancellationToken))
            {
                return FindSourceDeclarationsAsyncImpl(project, name, ignoreCase, filter, cancellationToken);
            }
        }

        private static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsyncImpl(
            Project project, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken)
        {
            var list = new List<ISymbol>();

            await AddDeclarationsAsync(project, name, ignoreCase, filter, list, cancellationToken).ConfigureAwait(false);
            return list;
        }

        /// <summary>
        /// Find the symbols for declarations made in source with a matching name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Solution solution, Func<string, bool> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            return FindSourceDeclarationsAsync(solution, predicate, SymbolFilter.All, cancellationToken);
        }

        /// <summary>
        /// Find the symbols for declarations made in source with a matching name.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Solution solution, Func<string, bool> predicate, SymbolFilter filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            using (Logger.LogBlock(FunctionId.SymbolFinder_Solution_Predicate_FindSourceDeclarationsAsync, cancellationToken))
            {
                var result = new List<ISymbol>();
                foreach (var projectId in solution.ProjectIds)
                {
                    var project = solution.GetProject(projectId);
                    var symbols = await FindSourceDeclarationsAsync(project, predicate, filter, cancellationToken).ConfigureAwait(false);
                    result.AddRange(symbols);
                }

                return result;
            }
        }

        /// <summary>
        /// Find the symbols for declarations made in source with a matching name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Project project, Func<string, bool> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            return FindSourceDeclarationsAsync(project, predicate, SymbolFilter.All, cancellationToken);
        }

        /// <summary>
        /// Find the symbols for declarations made in source with a matching name.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Project project, Func<string, bool> predicate, SymbolFilter filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            using (Logger.LogBlock(FunctionId.SymbolFinder_Project_Predicate_FindSourceDeclarationsAsync, cancellationToken))
            {
                var result = new List<ISymbol>();
                if (!await project.ContainsSymbolsWithNameAsync(predicate, filter, cancellationToken).ConfigureAwait(false))
                {
                    return result;
                }

                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                result.AddRange(FilterByCriteria(compilation.GetSymbolsWithName(predicate, filter, cancellationToken), filter));
                return result;
            }
        }

        private static IEnumerable<ISymbol> FilterByCriteria(IEnumerable<ISymbol> symbols, SymbolFilter criteria)
        {
            foreach (var symbol in symbols)
            {
                if (symbol.IsImplicitlyDeclared || symbol.IsAccessor())
                {
                    continue;
                }

                if (MeetCriteria(symbol, criteria))
                {
                    yield return symbol;
                }
            }
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
