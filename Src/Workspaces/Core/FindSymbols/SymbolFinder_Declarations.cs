// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        /// <summary>
        /// Find the declared symbols from either source, referenced projects or metadata assemblies with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindDeclarationsAsync(
            Project project, string name, bool ignoreCase, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return SpecializedTasks.EmptyEnumerable<ISymbol>();
            }

            return FindDeclarationsAsyncImpl(project, name, ignoreCase, cancellationToken);
        }

        private static async Task<IEnumerable<ISymbol>> FindDeclarationsAsyncImpl(Project project, string name, bool ignoreCase, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            var list = new List<ISymbol>();

            // get declarations from the compilation's assembly
            await AddDeclarationsAsync(project, compilation.Assembly, name, ignoreCase, list, cancellationToken).ConfigureAwait(false);

            // get declarations from directly referenced projects and metadata
            foreach (var mr in compilation.References)
            {
                var assembly = compilation.GetAssemblyOrModuleSymbol(mr) as IAssemblySymbol;
                if (assembly != null)
                {
                    var assemblyProject = project.Solution.GetProject(assembly, cancellationToken);
                    if (assemblyProject != null)
                    {
                        await AddDeclarationsAsync(assemblyProject, assembly, name, ignoreCase, list, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await AddDeclarationsAsync(project.Solution, assembly, GetMetadataReferenceFilePath(mr), name, ignoreCase, list, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            // get declarations from metadata referenced in source directives
            foreach (var mr in compilation.DirectiveReferences)
            {
                var assembly = compilation.GetAssemblyOrModuleSymbol(mr) as IAssemblySymbol;
                if (assembly != null)
                {
                    await AddDeclarationsAsync(project.Solution, assembly, GetMetadataReferenceFilePath(mr), name, ignoreCase, list, cancellationToken).ConfigureAwait(false);
                }
            }

            return TranslateNamespaces(list, compilation);
        }

        private static string GetMetadataReferenceFilePath(MetadataReference metadataReference)
        {
            var executabeReference = metadataReference as PortableExecutableReference;
            if (executabeReference == null)
            {
                return null;
            }

            return executabeReference.FilePath;
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

        private static async Task AddDeclarationsAsync(Project project, IAssemblySymbol assembly, string name, bool ignoreCase, List<ISymbol> list, CancellationToken cancellationToken)
        {
            var info = await SymbolTreeInfo.GetInfoForProjectAsync(project, cancellationToken).ConfigureAwait(false);
            if (info != null && info.HasSymbols(name, ignoreCase))
            {
                list.AddRange(info.Find(assembly, name, ignoreCase, cancellationToken));
            }
        }

        private static async Task AddDeclarationsAsync(
            Solution solution, IAssemblySymbol assembly, string filePath, string name, bool ignoreCase, List<ISymbol> list, CancellationToken cancellationToken)
        {
            var info = await SymbolTreeInfo.GetInfoForAssemblyAsync(solution, assembly, filePath, cancellationToken).ConfigureAwait(false);
            if (info.HasSymbols(name, ignoreCase))
            {
                list.AddRange(info.Find(assembly, name, ignoreCase, cancellationToken));
            }
        }

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(
            Solution solution, string name, bool ignoreCase, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (solution == null)
            {
                throw new ArgumentNullException("solution");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return SpecializedTasks.EmptyEnumerable<ISymbol>();
            }

            return FindSourceDeclarationsAsyncImpl(solution, name, ignoreCase, cancellationToken);
        }

        private static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsyncImpl(
            Solution solution, string name, bool ignoreCase, CancellationToken cancellationToken)
        {
            var result = new List<ISymbol>();
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                var symbols = await FindSourceDeclarationsAsyncImpl(project, name, ignoreCase, cancellationToken).ConfigureAwait(false);
                result.AddRange(symbols);
            }

            return result;
        }

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(
            Project project, string name, bool ignoreCase, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return SpecializedTasks.EmptyEnumerable<ISymbol>();
            }

            return FindSourceDeclarationsAsyncImpl(project, name, ignoreCase, cancellationToken);
        }

        private static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsyncImpl(
            Project project, string name, bool ignoreCase, CancellationToken cancellationToken)
        {
            var info = await SymbolTreeInfo.GetInfoForProjectAsync(project, cancellationToken).ConfigureAwait(false);

            if (info != null && info.HasSymbols(name, ignoreCase))
            {
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                return info.Find(compilation.Assembly, name, ignoreCase, cancellationToken);
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<ISymbol>();
            }
        }

        /// <summary>
        /// Find the symbols for declarations made in source with a matching name.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(
            Solution solution, Func<string, bool> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (solution == null)
            {
                throw new ArgumentNullException("solution");
            }

            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }

            var result = new List<ISymbol>();
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                var symbols = await FindSourceDeclarationsAsync(project, predicate, cancellationToken).ConfigureAwait(false);
                result.AddRange(symbols);
            }

            return result;
        }

        /// <summary>
        /// Find the symbols for declarations made in source with a matching name.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(
            Project project, Func<string, bool> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var info = await SymbolTreeInfo.GetInfoForProjectAsync(project, cancellationToken).ConfigureAwait(false);
            if (info != null && info.HasSymbols(predicate, cancellationToken))
            {
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                return info.Search(compilation.Assembly, predicate, cancellationToken);
            }

            return SpecializedCollections.EmptyEnumerable<ISymbol>();
        }
    }
}