// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal static partial class DeclarationFinder
{
    private static async Task AddCompilationSourceDeclarationsWithNormalQueryAsync(
        Project project,
        SearchQuery query,
        SymbolFilter filter,
        ArrayBuilder<ISymbol> list,
        CancellationToken cancellationToken)
    {
        if (!project.SupportsCompilation)
            return;

        Contract.ThrowIfTrue(query.Kind == SearchKind.Custom, "Custom queries are not supported in this API");

        using (Logger.LogBlock(FunctionId.SymbolFinder_Project_AddDeclarationsAsync, cancellationToken))
        {
            var syntaxFacts = project.GetRequiredLanguageService<ISyntaxFactsService>();

            // If this is an exact query, we can speed things up by just calling into the
            // compilation entrypoints that take a string directly.
            //
            // the search is 'exact' if it's either an exact-case-sensitive search,
            // or it's an exact-case-insensitive search and we're in a case-insensitive
            // language.
            var isExactNameSearch = query.Kind == SearchKind.Exact ||
                (query.Kind == SearchKind.ExactIgnoreCase && !syntaxFacts.IsCaseSensitive);

            // Do a quick syntactic check first using our cheaply built indices.  That will help us avoid creating
            // a compilation here if it's not necessary.  In the case of an exact name search we can call a special 
            // overload that quickly uses the direct bloom-filter identifier maps in the index.  If it's nto an 
            // exact name search, then we will run the query's predicate over every DeclaredSymbolInfo stored in
            // the doc.
            var containsSymbol = isExactNameSearch
                ? await project.ContainsSymbolsWithNameAsync(query.Name!, cancellationToken).ConfigureAwait(false)
                : await project.ContainsSymbolsWithNameAsync(query.GetPredicate(), filter, cancellationToken).ConfigureAwait(false);

            if (!containsSymbol)
                return;

            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            var symbols = isExactNameSearch
                ? compilation.GetSymbolsWithName(query.Name!, filter, cancellationToken)
                : compilation.GetSymbolsWithName(query.GetPredicate(), filter, cancellationToken);

            var symbolsWithName = symbols.ToImmutableArray();

            list.AddRange(FilterByCriteria(symbolsWithName, filter));
        }
    }

    private static async Task AddMetadataDeclarationsWithNormalQueryAsync(
        Project project,
        AsyncLazy<IAssemblySymbol?> lazyAssembly,
        PortableExecutableReference reference,
        SearchQuery query,
        SymbolFilter filter,
        ArrayBuilder<ISymbol> list,
        CancellationToken cancellationToken)
    {
        // All entrypoints to this function are Find functions that are only searching
        // for specific strings (i.e. they never do a custom search).
        Contract.ThrowIfTrue(query.Kind == SearchKind.Custom, "Custom queries are not supported in this API");

        using (Logger.LogBlock(FunctionId.SymbolFinder_Assembly_AddDeclarationsAsync, cancellationToken))
        {
            var info = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
                project.Solution, reference, checksum: null, cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfNull(info);

            var symbols = await info.FindAsync(query, lazyAssembly, filter, cancellationToken).ConfigureAwait(false);
            list.AddRange(symbols);
        }
    }

    internal static ImmutableArray<ISymbol> FilterByCriteria(ImmutableArray<ISymbol> symbols, SymbolFilter criteria)
        => symbols.WhereAsArray(s => MeetCriteria(s, criteria));

    private static bool MeetCriteria(ISymbol symbol, SymbolFilter filter)
    {
        if (!symbol.IsImplicitlyDeclared && !symbol.IsAccessor())
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
        }

        return false;
    }

    private static bool IsNonTypeMember(ISymbol symbol)
    {
        return symbol.Kind is SymbolKind.Method or
               SymbolKind.Property or
               SymbolKind.Event or
               SymbolKind.Field;
    }

    private static bool IsOn(SymbolFilter filter, SymbolFilter flag)
        => (filter & flag) == flag;
}
