// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename;

/// <summary>
/// A helper class that contains some of the methods and filters that must be used when
/// processing the raw results from the FindReferences API.
/// </summary>
internal sealed partial class SymbolicRenameLocations
{
    internal static class ReferenceProcessing
    {
        private static async Task<bool> ShouldIncludeSymbolAsync(
            ISymbol referencedSymbol, ISymbol originalSymbol, Solution solution, bool considerSymbolReferences, CancellationToken cancellationToken)
        {
            if (referencedSymbol.IsPropertyAccessor())
            {
                return considerSymbolReferences;
            }

            if (referencedSymbol.Equals(originalSymbol))
            {
                return true;
            }

            // Parameters of properties and methods can cascade to each other in
            // indexer scenarios.
            if (originalSymbol.Kind == SymbolKind.Parameter && referencedSymbol.Kind == SymbolKind.Parameter)
            {
                return true;
            }

            // If the original symbol is a property, cascade to the backing field
            if (referencedSymbol.Kind == SymbolKind.Field && originalSymbol.Equals(((IFieldSymbol)referencedSymbol).AssociatedSymbol))
            {
                return true;
            }

            // If the symbol doesn't actually exist in source, we never want to rename it
            if (referencedSymbol.IsImplicitlyDeclared)
            {
                return considerSymbolReferences;
            }

            // We can cascade from members to other members only if the names match. The example
            // where the names might be different is explicit interface implementations in
            // Visual Basic and VB's identifiers are case insensitive. 
            // Do not cascade to symbols that are defined only in metadata.
            if (referencedSymbol.Kind == originalSymbol.Kind &&
                string.Compare(TrimNameToAfterLastDot(referencedSymbol.Name), TrimNameToAfterLastDot(originalSymbol.Name), StringComparison.OrdinalIgnoreCase) == 0 &&
                referencedSymbol.Locations.Any(static loc => loc.IsInSource))
            {
                return true;
            }

            // If the original symbol is an alias, then the referenced symbol will be where we
            // actually see references.
            if (originalSymbol.Kind == SymbolKind.Alias)
            {
                var target = ((IAliasSymbol)originalSymbol).Target;

                switch (target)
                {
                    case INamedTypeSymbol nt:
                        return nt.ConstructedFrom.Equals(referencedSymbol)
                            || IsConstructorForType(possibleConstructor: referencedSymbol, possibleType: nt);

                    case INamespaceOrTypeSymbol s:
                        return s.Equals(referencedSymbol);

                    default: return false;
                }
            }

            // cascade from property accessor to property (someone in C# renames base.get_X, or the accessor override)
            if (await IsPropertyAccessorOrAnOverrideAsync(referencedSymbol, solution, cancellationToken).ConfigureAwait(false) ||
                await IsPropertyAccessorOrAnOverrideAsync(originalSymbol, solution, cancellationToken).ConfigureAwait(false))
            {
                return true;
            }

            // cascade from constructor to named type
            if (IsConstructorForType(possibleConstructor: referencedSymbol, possibleType: originalSymbol))
            {
                return true;
            }

            if (referencedSymbol.ContainingSymbol != null &&
                referencedSymbol.ContainingSymbol.Kind == SymbolKind.NamedType &&
                ((INamedTypeSymbol)referencedSymbol.ContainingSymbol).TypeKind == TypeKind.Interface &&
                !originalSymbol.ExplicitInterfaceImplementations().Any(predicate: static (s, referencedSymbol) => s.Equals(referencedSymbol), arg: referencedSymbol))
            {
                return true;
            }

            // When a parameter in primary constructor get renamed, we also want to rename its generated property. And vice versa.
            if (IsPropertyGeneratedFromPrimaryConstructorParameter(originalSymbol, referencedSymbol, cancellationToken)
                || IsPropertyGeneratedFromPrimaryConstructorParameter(referencedSymbol, originalSymbol, cancellationToken))
            {
                return true;
            }

            return false;

            // Local functions
            static bool IsConstructorForType(ISymbol possibleConstructor, ISymbol possibleType)
            {
                return possibleConstructor.IsConstructor()
                    && possibleType is INamedTypeSymbol namedType
                    && Equals(possibleConstructor.ContainingType.ConstructedFrom, namedType.ConstructedFrom);
            }
        }

        private static bool IsPropertyGeneratedFromPrimaryConstructorParameter(
            ISymbol propertySymbol, ISymbol parameterSymbol, CancellationToken cancellationToken)
            => parameterSymbol is IParameterSymbol parameter && propertySymbol.Equals(parameter.GetAssociatedSynthesizedRecordProperty(cancellationToken));

        private static async Task<bool> IsPropertyAccessorOrAnOverrideAsync(
            ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            var result = await RenameUtilities.TryGetPropertyFromAccessorOrAnOverrideAsync(
                symbol, solution, cancellationToken).ConfigureAwait(false);
            return result != null;
        }

        private static string TrimNameToAfterLastDot(string name)
        {
            var position = name.LastIndexOf('.');

            if (position == -1)
            {
                return name;
            }
            else
            {
                return name[(position + 1)..];
            }
        }

        /// <summary>
        /// Given a ISymbol, returns the renameable locations for a given symbol.
        /// </summary>
        public static async Task<ImmutableArray<RenameLocation>> GetRenamableDefinitionLocationsAsync(
            ISymbol referencedSymbol, ISymbol originalSymbol, Solution solution, CancellationToken cancellationToken)
        {
            var shouldIncludeSymbol = await ShouldIncludeSymbolAsync(referencedSymbol, originalSymbol, solution, false, cancellationToken).ConfigureAwait(false);
            if (!shouldIncludeSymbol)
            {
                return [];
            }

            // Namespaces are definitions and references all in one. Since every definition
            // location is also a reference, we'll ignore it's definitions.
            if (referencedSymbol.Kind == SymbolKind.Namespace)
            {
                return [];
            }

            var results = ArrayBuilder<RenameLocation>.GetInstance();

            // If the original symbol was an alias, then the definitions will just be the
            // location of the alias, always
            if (originalSymbol.Kind == SymbolKind.Alias)
            {
                var location = originalSymbol.Locations.Single();
                AddRenameLocationIfNotGenerated(location);
                return results.ToImmutableAndFree();
            }

            var isRenamableAccessor = await IsPropertyAccessorOrAnOverrideAsync(referencedSymbol, solution, cancellationToken).ConfigureAwait(false);
            foreach (var location in referencedSymbol.Locations)
            {
                if (location.IsInSource)
                {
                    AddRenameLocationIfNotGenerated(location, isRenamableAccessor);
                }
            }

            // If we're renaming a named type, we'll also have to find constructors and
            // destructors declarations that match the name
            if (referencedSymbol.Kind == SymbolKind.NamedType && referencedSymbol.Locations.All(l => l.IsInSource))
            {
                var firstLocation = referencedSymbol.Locations[0];
                var syntaxFacts = solution.GetRequiredDocument(firstLocation.SourceTree!)
                                          .GetRequiredLanguageService<ISyntaxFactsService>();

                var namedType = (INamedTypeSymbol)referencedSymbol;
                foreach (var method in namedType.GetMembers().OfType<IMethodSymbol>())
                {
                    if (method is
                        {
                            IsImplicitlyDeclared: false,
                            MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor or MethodKind.Destructor,
                        })
                    {
                        foreach (var location in method.Locations)
                        {
                            if (location.IsInSource)
                            {
                                var token = location.FindToken(cancellationToken);
                                if (!syntaxFacts.IsReservedOrContextualKeyword(token) &&
                                    token.ValueText == referencedSymbol.Name)
                                {
                                    AddRenameLocationIfNotGenerated(location);
                                }
                            }
                        }
                    }
                }
            }

            return results.ToImmutableAndFree();

            void AddRenameLocationIfNotGenerated(Location location, bool isRenamableAccessor = false)
            {
                RoslynDebug.Assert(location.IsInSource);
                var document = solution.GetRequiredDocument(location.SourceTree);

                // If the location is in a source generated file, we won't rename it. Our assumption in this case is we
                // have cascaded to this symbol from our original source symbol, and the generator will update this file
                // based on the renamed symbol.
                if (document is not SourceGeneratedDocument)
                    results.Add(new RenameLocation(location, document.Id, isRenamableAccessor: isRenamableAccessor));
            }
        }

        internal static async Task<IEnumerable<RenameLocation>> GetRenamableReferenceLocationsAsync(ISymbol referencedSymbol, ISymbol originalSymbol, ReferenceLocation location, Solution solution, CancellationToken cancellationToken)
        {
            // We won't try to update references in source generated files; we'll assume the generator will rerun
            // and produce an updated document with the new name.
            if (location.Document is SourceGeneratedDocument)
                return [];

            var shouldIncludeSymbol = await ShouldIncludeSymbolAsync(referencedSymbol, originalSymbol, solution, true, cancellationToken).ConfigureAwait(false);
            if (!shouldIncludeSymbol)
                return [];

            // Implicit references are things like a foreach referencing GetEnumerator. We don't
            // want to consider those as part of the set
            if (location.IsImplicit)
                return [];

            var results = new List<RenameLocation>();

            // If we were originally naming an alias, then we'll only use the location if was
            // also bound through the alias
            if (originalSymbol.Kind == SymbolKind.Alias)
            {
                if (originalSymbol.Equals(location.Alias))
                {
                    results.Add(new RenameLocation(location, location.Document.Id));

                    // We also need to add the location of the alias
                    // itself
                    var aliasLocation = location.Alias.Locations.Single();
                    Contract.ThrowIfNull(aliasLocation.SourceTree);
                    results.Add(new RenameLocation(aliasLocation, solution.GetRequiredDocument(aliasLocation.SourceTree).Id));
                }
            }
            else
            {
                // If we bound through an alias, we'll only rename if the alias's name matches
                // the name of symbol it points to. We do this because it's common to see things
                // like "using Goo = System.Goo" where people want to import a single type
                // rather than a whole namespace of stuff.
                if (location.Alias != null)
                {
                    if (location.Alias.Name == referencedSymbol.Name)
                    {
                        results.Add(new RenameLocation(location.Location, location.Document.Id,
                            candidateReason: location.CandidateReason, isRenamableAliasUsage: true, isWrittenTo: location.IsWrittenTo));

                        // We also need to add the location of the alias itself
                        var aliasLocation = location.Alias.Locations.Single();
                        Contract.ThrowIfNull(aliasLocation.SourceTree);
                        results.Add(new RenameLocation(aliasLocation, solution.GetRequiredDocument(aliasLocation.SourceTree).Id));
                    }
                }
                else if (location.ContainingStringLocation != Location.None)
                {
                    // Location within a string
                    results.Add(new RenameLocation(
                        location.Location,
                        location.Document.Id,
                        containingLocationForStringOrComment: location.ContainingStringLocation.SourceSpan));
                }
                else
                {
                    // The simple case, so just the single location and we're done
                    results.Add(new RenameLocation(
                        location.Location,
                        location.Document.Id,
                        isWrittenTo: location.IsWrittenTo,
                        candidateReason: location.CandidateReason,
                        isRenamableAccessor: await IsPropertyAccessorOrAnOverrideAsync(referencedSymbol, solution, cancellationToken).ConfigureAwait(false)));
                }
            }

            return results;
        }

        internal static async Task<(ImmutableArray<RenameLocation> strings, ImmutableArray<RenameLocation> comments)> GetRenamableLocationsInStringsAndCommentsAsync(
            ISymbol originalSymbol,
            Solution solution,
            ISet<RenameLocation> renameLocations,
            bool renameInStrings,
            bool renameInComments,
            CancellationToken cancellationToken)
        {
            if (!renameInStrings && !renameInComments)
                return default;

            var renameText = originalSymbol.Name;

            using var _1 = ArrayBuilder<RenameLocation>.GetInstance(out var stringLocations);
            using var _2 = ArrayBuilder<RenameLocation>.GetInstance(out var commentLocations);

            foreach (var documentsGroupedByLanguage in RenameUtilities.GetDocumentsAffectedByRename(originalSymbol, solution, renameLocations).GroupBy(d => d.Project.Language))
            {
                var syntaxFactsLanguageService = solution.Services.GetLanguageServices(documentsGroupedByLanguage.Key).GetService<ISyntaxFactsService>();

                if (syntaxFactsLanguageService != null)
                {
                    foreach (var document in documentsGroupedByLanguage)
                    {
                        if (renameInStrings)
                        {
                            await AddLocationsToRenameInStringsAsync(
                                document, renameText, syntaxFactsLanguageService,
                                stringLocations, cancellationToken).ConfigureAwait(false);
                        }

                        if (renameInComments)
                        {
                            await AddLocationsToRenameInCommentsAsync(document, renameText, commentLocations, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
            }

            return (renameInStrings ? stringLocations.ToImmutable() : default,
                    renameInComments ? commentLocations.ToImmutable() : default);
        }

        private static async Task AddLocationsToRenameInStringsAsync(
            Document document, string renameText, ISyntaxFactsService syntaxFactsService,
            ArrayBuilder<RenameLocation> renameLocations, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var renameTextLength = renameText.Length;

            var renameStringsAndPositions = root
                .DescendantTokens()
                .Where(t => syntaxFactsService.IsStringLiteralOrInterpolatedStringLiteral(t) && t.Span.Length >= renameTextLength)
                .Select(t => Tuple.Create(t.ToString(), t.Span.Start, t.Span));

            if (renameStringsAndPositions.Any())
            {
                AddLocationsToRenameInStringsAndComments(document, root.SyntaxTree, renameText,
                    renameStringsAndPositions, renameLocations);
            }
        }

        private static async Task AddLocationsToRenameInCommentsAsync(
            Document document, string renameText, ArrayBuilder<RenameLocation> renameLocations, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var renameTextLength = renameText.Length;

            var renameStringsAndPositions = root
                .DescendantTrivia(descendIntoTrivia: true)
                .Where(t => t.Span.Length >= renameTextLength)
                .Select(t => Tuple.Create(t.ToString(), t.Span.Start, t.Token.Span));

            if (renameStringsAndPositions.Any())
            {
                AddLocationsToRenameInStringsAndComments(document, root.SyntaxTree, renameText,
                    renameStringsAndPositions, renameLocations);
            }
        }

        private static void AddLocationsToRenameInStringsAndComments(
            Document document,
            SyntaxTree tree,
            string renameText,
            IEnumerable<Tuple<string, int, TextSpan>> renameStringsAndPositions,
            ArrayBuilder<RenameLocation> renameLocations)
        {
            var regex = RenameUtilities.GetRegexForMatch(renameText);
            foreach (var renameStringAndPosition in renameStringsAndPositions)
            {
                var renameString = renameStringAndPosition.Item1;
                var renameStringPosition = renameStringAndPosition.Item2;
                var containingSpan = renameStringAndPosition.Item3;

                var matches = regex.Matches(renameString);

                foreach (Match? match in matches)
                {
                    if (match == null)
                        continue;

                    var start = renameStringPosition + match.Index;
                    Debug.Assert(renameText.Length == match.Length);
                    var matchTextSpan = new TextSpan(start, renameText.Length);
                    var matchLocation = tree.GetLocation(matchTextSpan);
                    var renameLocation = new RenameLocation(matchLocation, document.Id, containingLocationForStringOrComment: containingSpan);
                    renameLocations.Add(renameLocation);
                }
            }
        }
    }
}
