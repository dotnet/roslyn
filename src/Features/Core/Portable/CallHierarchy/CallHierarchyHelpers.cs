// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CallHierarchy;

/// <summary>
/// Shared helpers for call hierarchy operations, used by both LSP and VS implementations.
/// </summary>
internal static class CallHierarchyHelpers
{
    /// <summary>
    /// Gets the target symbol for call hierarchy, normalizing reduced/constructed method symbols.
    /// </summary>
    public static ISymbol GetTargetSymbol(ISymbol symbol)
    {
        if (symbol is IMethodSymbol methodSymbol)
        {
            methodSymbol = methodSymbol.ReducedFrom ?? methodSymbol;
            methodSymbol = methodSymbol.ConstructedFrom;
            return methodSymbol;
        }

        return symbol;
    }

    /// <summary>
    /// Finds all direct callers of the given symbol.
    /// </summary>
    public static async Task<ImmutableArray<SymbolCallerInfo>> FindDirectCallersAsync(
        ISymbol symbol,
        Solution solution,
        IImmutableSet<Document>? documents,
        CancellationToken cancellationToken)
    {
        var callers = await SymbolFinder.FindCallersAsync(symbol, solution, documents, cancellationToken).ConfigureAwait(false);
        return callers.ToImmutableArray();
    }

    /// <summary>
    /// Finds callers through overriding members.
    /// </summary>
    public static async Task<ImmutableArray<SymbolCallerInfo>> FindCallersToOverridesAsync(
        ISymbol symbol,
        Solution solution,
        IImmutableSet<Document>? documents,
        CancellationToken cancellationToken)
    {
        var overrides = await SymbolFinder.FindOverridesAsync(symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<SymbolCallerInfo>.GetInstance(out var results);
        foreach (var @override in overrides)
        {
            var callers = await SymbolFinder.FindCallersAsync(@override, solution, documents, cancellationToken).ConfigureAwait(false);
            results.AddRange(callers);
        }

        return results.ToImmutableArray();
    }

    /// <summary>
    /// Finds overriding members of the given symbol.
    /// </summary>
    public static async Task<ImmutableArray<ISymbol>> FindOverridingMembersAsync(
        ISymbol symbol,
        Solution solution,
        CancellationToken cancellationToken)
    {
        var overrides = await SymbolFinder.FindOverridesAsync(symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
        return overrides.ToImmutableArray();
    }

    /// <summary>
    /// Finds interface members implemented by the given symbol.
    /// </summary>
    public static async Task<ImmutableArray<ISymbol>> FindImplementedInterfaceMembersAsync(
        ISymbol symbol,
        Solution solution,
        CancellationToken cancellationToken)
    {
        var members = await SymbolFinder.FindImplementedInterfaceMembersAsync(symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
        return members.ToImmutableArray();
    }

    /// <summary>
    /// Finds implementers of the given interface member.
    /// </summary>
    public static async Task<ImmutableArray<ISymbol>> FindImplementersAsync(
        ISymbol interfaceMember,
        Solution solution,
        CancellationToken cancellationToken)
    {
        var implementers = await SymbolFinder.FindImplementationsAsync(interfaceMember, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
        return implementers.ToImmutableArray();
    }
}
