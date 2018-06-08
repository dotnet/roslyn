// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    // All the logic for finding source declarations in a given solution/project with some name 
    // is in this file.  

    public static partial class SymbolFinder
    {
        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Solution solution, string name, bool ignoreCase, CancellationToken cancellationToken = default)
            => FindSourceDeclarationsAsync(solution, name, ignoreCase, SymbolFilter.All, cancellationToken);

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(
            Solution solution, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken = default)
        {
            using (Logger.LogBlock(FunctionId.SymbolFinder_Solution_Name_FindSourceDeclarationsAsync, cancellationToken))
            {
                var declarations = await DeclarationFinder.FindSourceDeclarationsWithNormalQueryAsync(
                    solution, name, ignoreCase, filter, cancellationToken).ConfigureAwait(false);
                return declarations.SelectAsArray(t => t.Symbol);
            }
        }

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Project project, string name, bool ignoreCase, CancellationToken cancellationToken = default)
            => FindSourceDeclarationsAsync(project, name, ignoreCase, SymbolFilter.All, cancellationToken);

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(
            Project project, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken = default)
        {
            using (Logger.LogBlock(FunctionId.SymbolFinder_Project_Name_FindSourceDeclarationsAsync, cancellationToken))
            {
                var declarations = await DeclarationFinder.FindSourceDeclarationsWithNormalQueryAsync(
                    project, name, ignoreCase, filter, cancellationToken).ConfigureAwait(false);

                return declarations.SelectAsArray(t => t.Symbol);
            }
        }

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters

        /// <summary>
        /// Find the symbols for declarations made in source with the specified pattern. This pattern is matched
        /// using heuristics that may change from release to release. So, the set of symbols matched by a given
        /// pattern may change between releases. For example, new symbols may be matched by a pattern and/or
        /// symbols previously matched by a pattern no longer are. However, the set of symbols matched by a
        /// specific release will be consistent for a specific pattern.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsWithPatternAsync(Solution solution, string pattern, CancellationToken cancellationToken = default)
            => FindSourceDeclarationsWithPatternAsync(solution, pattern, SymbolFilter.All, cancellationToken);

        /// <summary>
        /// Find the symbols for declarations made in source with the specified pattern. This pattern is matched
        /// using heuristics that may change from release to release. So, the set of symbols matched by a given
        /// pattern may change between releases. For example, new symbols may be matched by a pattern and/or
        /// symbols previously matched by a pattern no longer are. However, the set of symbols matched by a
        /// specific release will be consistent for a specific pattern.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsWithPatternAsync(
            Solution solution, string pattern, SymbolFilter filter, CancellationToken cancellationToken = default)
        {
            using (Logger.LogBlock(FunctionId.SymbolFinder_Solution_Pattern_FindSourceDeclarationsAsync, cancellationToken))
            {
                var declarations = await DeclarationFinder.FindSourceDeclarationsWithPatternAsync(
                    solution, pattern, filter, cancellationToken).ConfigureAwait(false);
                return declarations.SelectAsArray(t => t.Symbol);
            }
        }

        /// <summary>
        /// Find the symbols for declarations made in source with the specified pattern. This pattern is matched
        /// using heuristics that may change from release to release. So, the set of symbols matched by a given
        /// pattern may change between releases. For example, new symbols may be matched by a pattern and/or
        /// symbols previously matched by a pattern no longer are. However, the set of symbols matched by a
        /// specific release will be consistent for a specific pattern.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsWithPatternAsync(Project project, string pattern, CancellationToken cancellationToken = default)
            => FindSourceDeclarationsWithPatternAsync(project, pattern, SymbolFilter.All, cancellationToken);

        /// <summary>
        /// Find the symbols for declarations made in source with the specified pattern. This pattern is matched
        /// using heuristics that may change from release to release. So, the set of symbols matched by a given
        /// pattern may change between releases. For example, new symbols may be matched by a pattern and/or
        /// symbols previously matched by a pattern no longer are. However, the set of symbols matched by a
        /// specific release will be consistent for a specific pattern.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsWithPatternAsync(
            Project project, string pattern, SymbolFilter filter, CancellationToken cancellationToken = default)
        {
            using (Logger.LogBlock(FunctionId.SymbolFinder_Project_Pattern_FindSourceDeclarationsAsync, cancellationToken))
            {
                var declarations = await DeclarationFinder.FindSourceDeclarationsWithPatternAsync(
                    project, pattern, filter, cancellationToken).ConfigureAwait(false);

                return declarations.SelectAsArray(t => t.Symbol);
            }
        }

#pragma warning restore RS0026
    }
}
