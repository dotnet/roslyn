// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols;

// Logic related to finding declarations with a completely custom predicate goes here.
// Completely custom predicates can not be optimized in any way as there is no way to
// tell what the predicate will return true for.
//
// Also, because we have no control over these predicates, we cannot remote these queries
// over to the OOP process.

public static partial class SymbolFinder
{
    /// <summary>
    /// Find the symbols for declarations made in source with a matching name.
    /// </summary>
    public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Solution solution, Func<string, bool> predicate, CancellationToken cancellationToken = default)
        => FindSourceDeclarationsAsync(solution, predicate, SymbolFilter.All, cancellationToken);

    /// <summary>
    /// Find the symbols for declarations made in source with a matching name.
    /// </summary>
    public static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Solution solution, Func<string, bool> predicate, SymbolFilter filter, CancellationToken cancellationToken = default)
    {
        using var query = SearchQuery.CreateCustom(predicate);
        var declarations = await FindSourceDeclarationsWithCustomQueryAsync(
            solution, query, filter, cancellationToken).ConfigureAwait(false);

        return declarations;
    }

    internal static async Task<ImmutableArray<ISymbol>> FindSourceDeclarationsWithCustomQueryAsync(
        Solution solution, SearchQuery query, SymbolFilter filter, CancellationToken cancellationToken)
    {
        if (solution == null)
        {
            throw new ArgumentNullException(nameof(solution));
        }

        if (query.Name != null && string.IsNullOrWhiteSpace(query.Name))
        {
            return [];
        }

        using (Logger.LogBlock(FunctionId.SymbolFinder_Solution_Predicate_FindSourceDeclarationsAsync, cancellationToken))
        {
            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var result);
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetRequiredProject(projectId);
                var symbols = await FindSourceDeclarationsWithCustomQueryAsync(project, query, filter, cancellationToken).ConfigureAwait(false);
                result.AddRange(symbols);
            }

            return result.ToImmutable();
        }
    }

    /// <summary>
    /// Find the symbols for declarations made in source with a matching name.
    /// </summary>
    public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Project project, Func<string, bool> predicate, CancellationToken cancellationToken = default)
        => FindSourceDeclarationsAsync(project, predicate, SymbolFilter.All, cancellationToken);

    /// <summary>
    /// Find the symbols for declarations made in source with a matching name.
    /// </summary>
    public static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Project project, Func<string, bool> predicate, SymbolFilter filter, CancellationToken cancellationToken = default)
    {
        using var query = SearchQuery.CreateCustom(predicate);
        var declarations = await FindSourceDeclarationsWithCustomQueryAsync(
            project, query, filter, cancellationToken).ConfigureAwait(false);

        return declarations;
    }

    internal static async Task<ImmutableArray<ISymbol>> FindSourceDeclarationsWithCustomQueryAsync(
        Project project, SearchQuery query, SymbolFilter filter, CancellationToken cancellationToken)
    {
        if (project == null)
        {
            throw new ArgumentNullException(nameof(project));
        }

        if (query.Name != null && string.IsNullOrWhiteSpace(query.Name))
        {
            return [];
        }

        using (Logger.LogBlock(FunctionId.SymbolFinder_Project_Predicate_FindSourceDeclarationsAsync, cancellationToken))
        {
            if (await project.ContainsSymbolsWithNameAsync(query.GetPredicate(), filter, cancellationToken).ConfigureAwait(false))
            {
                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

                var unfiltered = compilation.GetSymbolsWithName(query.GetPredicate(), filter, cancellationToken)
                                            .ToImmutableArray();

                return DeclarationFinder.FilterByCriteria(unfiltered, filter);
            }
        }

        return [];
    }
}
