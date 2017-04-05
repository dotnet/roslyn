// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    // All the logic for finding all declarations in a given solution/project with some name 
    // is in this file.  

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

        internal static async Task<ImmutableArray<ISymbol>> FindAllDeclarationsWithNormalQueryAsync(
            Project project, SearchQuery query, SymbolFilter criteria, CancellationToken cancellationToken)
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
                return ImmutableArray<ISymbol>.Empty;
            }

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            var list = ArrayBuilder<ISymbol>.GetInstance();

            // get declarations from the compilation's assembly
            await AddCompilationDeclarationsWithNormalQueryAsync(project, query, criteria, list, cancellationToken).ConfigureAwait(false);

            // get declarations from directly referenced projects and metadata
            foreach (var assembly in compilation.GetReferencedAssemblySymbols())
            {
                var assemblyProject = project.Solution.GetProject(assembly, cancellationToken);
                if (assemblyProject != null)
                {
                    await AddCompilationDeclarationsWithNormalQueryAsync(assemblyProject, query, criteria, list, compilation, assembly, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await AddMetadataDeclarationsWithNormalQueryAsync(
                        project.Solution, assembly, compilation.GetMetadataReference(assembly) as PortableExecutableReference,
                        query, criteria, list, cancellationToken).ConfigureAwait(false);
                }
            }

            // Make certain all namespace symbols returned by API are from the compilation.
            for (var i = 0; i < list.Count; i++)
            {
                var symbol = list[i];
                if (symbol is INamespaceSymbol ns)
                {
                    list[i] = compilation.GetCompilationNamespace(ns);
                }
            }

            return list.ToImmutableAndFree();
        }
    }
}