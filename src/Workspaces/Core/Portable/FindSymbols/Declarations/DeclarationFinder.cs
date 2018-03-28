// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal static partial class DeclarationFinder
    {
        private static Task AddCompilationDeclarationsWithNormalQueryAsync(
            Project project, SearchQuery query, SymbolFilter filter,
            ArrayBuilder<SymbolAndProjectId> list, CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(query.Kind == SearchKind.Custom, "Custom queries are not supported in this API");
            return AddCompilationDeclarationsWithNormalQueryAsync(
                project, query, filter, list,
                startingCompilation: null,
                startingAssembly: null,
                cancellationToken: cancellationToken);
        }

        private static async Task AddCompilationDeclarationsWithNormalQueryAsync(
            Project project,
            SearchQuery query,
            SymbolFilter filter,
            ArrayBuilder<SymbolAndProjectId> list,
            Compilation startingCompilation,
            IAssemblySymbol startingAssembly,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(query.Kind == SearchKind.Custom, "Custom queries are not supported in this API");

            using (Logger.LogBlock(FunctionId.SymbolFinder_Project_AddDeclarationsAsync, cancellationToken))
            {
                if (!await project.ContainsSymbolsWithNameAsync(query.GetPredicate(), filter, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var symbolsWithName = compilation.GetSymbolsWithName(query.GetPredicate(), filter, cancellationToken)
                                                 .Select(s => new SymbolAndProjectId(s, project.Id))
                                                 .ToImmutableArray();

                if (startingCompilation != null && startingAssembly != null && compilation.Assembly != startingAssembly)
                {
                    // Return symbols from skeleton assembly in this case so that symbols have 
                    // the same language as startingCompilation.
                    symbolsWithName = symbolsWithName.Select(s => s.WithSymbol(s.Symbol.GetSymbolKey().Resolve(startingCompilation, cancellationToken: cancellationToken).Symbol))
                                                     .Where(s => s.Symbol != null)
                                                     .ToImmutableArray();
                }

                list.AddRange(FilterByCriteria(symbolsWithName, filter));
            }
        }

        private static async Task AddMetadataDeclarationsWithNormalQueryAsync(
            Project project, IAssemblySymbol assembly, PortableExecutableReference referenceOpt, 
            SearchQuery query, SymbolFilter filter, ArrayBuilder<SymbolAndProjectId> list, 
            CancellationToken cancellationToken)
        {
            // All entrypoints to this function are Find functions that are only searching
            // for specific strings (i.e. they never do a custom search).
            Contract.ThrowIfTrue(query.Kind == SearchKind.Custom, "Custom queries are not supported in this API");

            using (Logger.LogBlock(FunctionId.SymbolFinder_Assembly_AddDeclarationsAsync, cancellationToken))
            {
                if (referenceOpt != null)
                {
                    var info = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
                        project.Solution, referenceOpt, loadOnly: false, cancellationToken: cancellationToken).ConfigureAwait(false);

                    var symbols = await info.FindAsync(
                            query, assembly, project.Id, filter, cancellationToken).ConfigureAwait(false);
                    list.AddRange(symbols);
                }
            }
        }

        internal static ImmutableArray<SymbolAndProjectId> FilterByCriteria(ImmutableArray<SymbolAndProjectId> symbols, SymbolFilter criteria)
            => symbols.WhereAsArray(s => MeetCriteria(s.Symbol, criteria));

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
