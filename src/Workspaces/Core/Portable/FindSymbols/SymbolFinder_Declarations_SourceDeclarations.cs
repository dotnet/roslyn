// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    // All the logic for finding source declarations in a given solution/project with some name 
    // is in this file.  

    public static partial class SymbolFinder
    {
        #region Legacy API

        // This region contains the legacy FindDeclarations APIs.  The APIs are legacy because they
        // do not contain enough information for us to effectively remote them over to the OOP
        // process to do the work.  Specifically, they lack the "current project context" necessary
        // to be able to effectively serialize symbols to/from the remote process.

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
            using (Logger.LogBlock(FunctionId.SymbolFinder_Solution_Name_FindSourceDeclarationsAsync, cancellationToken))
            {
                var declarations = await FindSourceDeclarationsWithNormalQueryInLocalProcessAsync(
                    solution, name, ignoreCase, filter, cancellationToken).ConfigureAwait(false);
                return declarations.SelectAsArray(t => t.Symbol);
            }
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
            using (Logger.LogBlock(FunctionId.SymbolFinder_Project_Name_FindSourceDeclarationsAsync, cancellationToken))
            {
                var declarations = await FindSourceDeclarationsithNormalQueryInLocalProcessAsync(
                    project, name, ignoreCase, filter, cancellationToken).ConfigureAwait(false);

                return declarations.SelectAsArray(t => t.Symbol);
            }
        }

        #endregion

        #region Current API

        // This region contains the current FindDeclaratins APIs.  The current APIs allow for OOP 
        // implementation and will defer to the oop server if it is available.  If not, it will
        // compute the results in process.

        internal static async Task<ImmutableArray<SymbolAndProjectId>> FindSourceDeclarationsWithNormalQueryInLocalProcessAsync(
            Solution solution, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken)
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
                return ImmutableArray<SymbolAndProjectId>.Empty;
            }

            var query = SearchQuery.Create(name, ignoreCase);
            var result = ArrayBuilder<SymbolAndProjectId>.GetInstance();
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                await AddCompilationDeclarationsWithNormalQueryAsync(
                    project, query, filter, result, cancellationToken).ConfigureAwait(false);
            }

            return result.ToImmutableAndFree();
        }

        private static async Task<ImmutableArray<SymbolAndProjectId>> FindSourceDeclarationsithNormalQueryInLocalProcessAsync(
            Project project, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken)
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
                return ImmutableArray<SymbolAndProjectId>.Empty;
            }

            var list = ArrayBuilder<SymbolAndProjectId>.GetInstance();
            await AddCompilationDeclarationsWithNormalQueryAsync(
                project, SearchQuery.Create(name, ignoreCase),
                filter, list, cancellationToken).ConfigureAwait(false);
            return list.ToImmutableAndFree();
        }

        #endregion
    }
}