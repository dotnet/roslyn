﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SymbolMapping;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal static class FindUsagesHelpers
    {
        public static string GetDisplayName(ISymbol symbol)
            => symbol.IsConstructor() ? symbol.ContainingType.Name : symbol.Name;

        /// <summary>
        /// Common helper for both the synchronous and streaming versions of FAR. 
        /// It returns the symbol we want to search for and the solution we should
        /// be searching.
        /// 
        /// Note that the <see cref="Solution"/> returned may absolutely *not* be
        /// the same as <c>document.Project.Solution</c>.  This is because 
        /// there may be symbol mapping involved (for example in Metadata-As-Source
        /// scenarios).
        /// </summary>
        public static async Task<(ISymbol symbol, Solution solution)?> GetRelevantSymbolAndSolutionAtPositionAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (symbol == null)
                return null;

            // If this document is not in the primary workspace, we may want to search for results
            // in a solution different from the one we started in. Use the starting workspace's
            // ISymbolMappingService to get a context for searching in the proper solution.
            var mappingService = document.Project.Solution.Workspace.Services.GetService<ISymbolMappingService>();

            var mapping = await mappingService.MapSymbolAsync(document, symbol, cancellationToken).ConfigureAwait(false);
            if (mapping == null)
                return null;

            return (mapping.Symbol, mapping.Project.Solution);
        }

        public static async Task<(Solution solution, ISymbol symbol, ImmutableArray<ISymbol> implementations, string message)?> FindSourceImplementationsAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var symbolAndSolutionOpt = await GetRelevantSymbolAndSolutionAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            if (symbolAndSolutionOpt == null)
                return null;

            var (symbol, solution) = symbolAndSolutionOpt.Value;
            return await FindSourceImplementationsAsync(
                solution, symbol, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<(Solution solution, ISymbol symbol, ImmutableArray<ISymbol> implementations, string message)?> FindSourceImplementationsAsync(
            Solution solution, ISymbol symbol, CancellationToken cancellationToken)
        {
            var builder = new HashSet<ISymbol>(SymbolEquivalenceComparer.Instance);

            // If we're in a linked file, try to find all the symbols this links to, and find all the implementations of
            // each of those linked symbols. De-dupe the results so the user only gets unique results.
            var linkedSymbols = await SymbolFinder.FindLinkedSymbolsAsync(
                symbol, solution, cancellationToken).ConfigureAwait(false);

            foreach (var linkedSymbol in linkedSymbols)
            {
                builder.AddRange(await FindSourceImplementationsWorkerAsync(
                    solution, linkedSymbol, cancellationToken).ConfigureAwait((bool)false));
            }

            var result = builder.ToImmutableArray();
            var message = result.IsEmpty ? EditorFeaturesResources.The_symbol_has_no_implementations : null;

            return (solution, symbol, result, message);
        }

        private static async Task<ImmutableArray<ISymbol>> FindSourceImplementationsWorkerAsync(
            Solution solution, ISymbol symbol, CancellationToken cancellationToken)
        {
            var implementations = await FindSourceAndMetadataImplementationsAsync(solution, symbol, cancellationToken).ConfigureAwait(false);
            return implementations.WhereAsArray(s => s.Locations.Any(l => l.IsInSource));
        }

        private static async Task<ImmutableArray<ISymbol>> FindSourceAndMetadataImplementationsAsync(
            Solution solution, ISymbol symbol, CancellationToken cancellationToken)
        {
            if (symbol.IsInterfaceType() || symbol.IsImplementableMember())
            {
                var implementations = await SymbolFinder.FindImplementationsAsync(
                    symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

                // It's important we use a HashSet here -- we may have cases in an inheritance hierarchy where more than one method
                // in an overrides chain implements the same interface method, and we want to duplicate those. The easiest way to do it
                // is to just use a HashSet.
                var implementationsAndOverrides = new HashSet<ISymbol>();

                foreach (var implementation in implementations)
                {
                    implementationsAndOverrides.Add(implementation);

                    // FindImplementationsAsync will only return the base virtual/abstract method, not that method and the overrides
                    // of the method. We should also include those.
                    if (implementation.IsOverridable())
                    {
                        var overrides = await SymbolFinder.FindOverridesAsync(
                            implementation, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
                        implementationsAndOverrides.AddRange(overrides);
                    }
                }

                if (!symbol.IsInterfaceType() &&
                    !symbol.IsAbstract)
                {
                    implementationsAndOverrides.Add(symbol);
                }

                return implementationsAndOverrides.ToImmutableArray();
            }
            else if (symbol is INamedTypeSymbol { TypeKind: TypeKind.Class } namedType)
            {
                var derivedClasses = await SymbolFinder.FindDerivedClassesAsync(
                    namedType, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

                return derivedClasses.Concat(symbol).ToImmutableArray();
            }
            else if (symbol.IsOverridable())
            {
                var overrides = await SymbolFinder.FindOverridesAsync(
                    symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
                return overrides.Concat(symbol).ToImmutableArray();
            }
            else
            {
                // This is something boring like a regular method or type, so we'll just go there directly
                return ImmutableArray.Create(symbol);
            }
        }

        private static SymbolDisplayFormat GetFormat(ISymbol definition)
        {
            return definition.Kind == SymbolKind.Parameter
                ? s_parameterDefinitionFormat
                : s_definitionFormat;
        }

        private static readonly SymbolDisplayFormat s_namePartsFormat = new SymbolDisplayFormat(
            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);

        private static readonly SymbolDisplayFormat s_definitionFormat =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
                kindOptions: SymbolDisplayKindOptions.IncludeMemberKeyword | SymbolDisplayKindOptions.IncludeNamespaceKeyword | SymbolDisplayKindOptions.IncludeTypeKeyword,
                localOptions: SymbolDisplayLocalOptions.IncludeType,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static readonly SymbolDisplayFormat s_parameterDefinitionFormat = s_definitionFormat
            .AddParameterOptions(SymbolDisplayParameterOptions.IncludeName);

        public static ImmutableArray<TaggedText> GetDisplayParts(ISymbol definition)
            => definition.ToDisplayParts(GetFormat(definition)).ToTaggedText();

        public static ImmutableArray<TaggedText> GetNameDisplayParts(ISymbol definition)
            => definition.ToDisplayParts(s_namePartsFormat).ToTaggedText();
    }
}
