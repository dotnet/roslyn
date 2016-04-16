// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    /// <summary>
    /// A helper class that contains some of the methods and filters that must be used when
    /// processing the raw results from the FindReferences API.
    /// </summary>
    internal sealed partial class RenameLocations
    {
        internal static class ReferenceProcessing
        {
            /// <summary>
            /// Given a symbol in a document, returns the "right" symbol that should be renamed in
            /// the case the name binds to things like aliases _and_ the underlying type at once.
            /// </summary>
            public static async Task<ISymbol> GetRenamableSymbolAsync(Document document, int position, CancellationToken cancellationToken)
            {
                var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (symbol == null)
                {
                    return null;
                }

                var definitionSymbol = await FindDefinitionSymbolAsync(symbol, document.Project.Solution, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfNull(definitionSymbol);

                return definitionSymbol;
            }

            /// <summary>
            /// Given a symbol, finds the symbol that actually defines the name that we're using.
            /// </summary>
            public static async Task<ISymbol> FindDefinitionSymbolAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(symbol);
                Contract.ThrowIfNull(solution);

                // Make sure we're on the original source definition if we can be
                var foundSymbol = await SymbolFinder.FindSourceDefinitionAsync(symbol, solution, cancellationToken).ConfigureAwait(false);

                symbol = foundSymbol ?? symbol;

                // If we're renaming a property, it might be a synthesized property for a method
                // backing field.
                if (symbol.Kind == SymbolKind.Parameter)
                {
                    if (symbol.ContainingSymbol.Kind == SymbolKind.Method)
                    {
                        var containingMethod = (IMethodSymbol)symbol.ContainingSymbol;
                        if (containingMethod.AssociatedSymbol is IPropertySymbol)
                        {
                            var associatedPropertyOrEvent = (IPropertySymbol)containingMethod.AssociatedSymbol;
                            var ordinal = containingMethod.Parameters.IndexOf((IParameterSymbol)symbol);
                            if (ordinal < associatedPropertyOrEvent.Parameters.Length)
                            {
                                return associatedPropertyOrEvent.Parameters[ordinal];
                            }
                        }
                    }
                }

                // if we are renaming a compiler generated delegate for an event, cascade to the event
                if (symbol.Kind == SymbolKind.NamedType)
                {
                    var typeSymbol = (INamedTypeSymbol)symbol;
                    if (typeSymbol.IsImplicitlyDeclared && typeSymbol.IsDelegateType() && typeSymbol.AssociatedSymbol != null)
                    {
                        return typeSymbol.AssociatedSymbol;
                    }
                }

                // If we are renaming a constructor or destructor, we wish to rename the whole type
                if (symbol.Kind == SymbolKind.Method)
                {
                    var methodSymbol = (IMethodSymbol)symbol;
                    if (methodSymbol.MethodKind == MethodKind.Constructor ||
                        methodSymbol.MethodKind == MethodKind.StaticConstructor ||
                        methodSymbol.MethodKind == MethodKind.Destructor)
                    {
                        return methodSymbol.ContainingType;
                    }
                }

                // If we are renaming a backing field for a property, cascade to the property
                if (symbol.Kind == SymbolKind.Field)
                {
                    var fieldSymbol = (IFieldSymbol)symbol;
                    if (fieldSymbol.IsImplicitlyDeclared &&
                        fieldSymbol.AssociatedSymbol.IsKind(SymbolKind.Property))
                    {
                        return fieldSymbol.AssociatedSymbol;
                    }
                }

                // in case this is e.g. an overridden property accessor, we'll treat the property itself as the definition symbol
                var property = await GetPropertyFromAccessorOrAnOverride(symbol, solution, cancellationToken).ConfigureAwait(false);

                return property ?? symbol;
            }

            private static async Task<bool> ShouldIncludeSymbolAsync(ISymbol referencedSymbol, ISymbol originalSymbol, Solution solution, bool considerSymbolReferences, CancellationToken cancellationToken)
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
                    referencedSymbol.Locations.Any(loc => loc.IsInSource))
                {
                    return true;
                }

                // If the original symbol is an alias, then the referenced symbol will be where we
                // actually see references.
                if (originalSymbol.Kind == SymbolKind.Alias)
                {
                    var target = ((IAliasSymbol)originalSymbol).Target;

                    return target.TypeSwitch(
                        (INamedTypeSymbol nt) => nt.ConstructedFrom.Equals(referencedSymbol),
                        (INamespaceOrTypeSymbol s) => s.Equals(referencedSymbol));
                }

                // cascade from property accessor to property (someone in C# renames base.get_X, or the accessor override)
                if (await IsPropertyAccessorOrAnOverride(referencedSymbol, solution, cancellationToken).ConfigureAwait(false) ||
                    await IsPropertyAccessorOrAnOverride(originalSymbol, solution, cancellationToken).ConfigureAwait(false))
                {
                    return true;
                }

                if (referencedSymbol.ContainingSymbol != null &&
                    referencedSymbol.ContainingSymbol.Kind == SymbolKind.NamedType &&
                    ((INamedTypeSymbol)referencedSymbol.ContainingSymbol).TypeKind == TypeKind.Interface &&
                    !originalSymbol.ExplicitInterfaceImplementations().Any(s => s.Equals(referencedSymbol)))
                {
                    return true;
                }

                return false;
            }

            internal static async Task<ISymbol> GetPropertyFromAccessorOrAnOverride(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
            {
                if (symbol.IsPropertyAccessor())
                {
                    return ((IMethodSymbol)symbol).AssociatedSymbol;
                }

                if (symbol.IsOverride && symbol.OverriddenMember() != null)
                {
                    var originalSourceSymbol = await SymbolFinder.FindSourceDefinitionAsync(symbol.OverriddenMember(), solution, cancellationToken).ConfigureAwait(false);

                    if (originalSourceSymbol != null)
                    {
                        return await GetPropertyFromAccessorOrAnOverride(originalSourceSymbol, solution, cancellationToken).ConfigureAwait(false);
                    }
                }

                if (symbol.Kind == SymbolKind.Method &&
                    symbol.ContainingType.TypeKind == TypeKind.Interface)
                {
                    var methodImplementors = await SymbolFinder.FindImplementationsAsync(symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

                    foreach (var methodImplementor in methodImplementors)
                    {
                        var propertyAccessorOrAnOverride = await GetPropertyFromAccessorOrAnOverride(methodImplementor, solution, cancellationToken).ConfigureAwait(false);
                        if (propertyAccessorOrAnOverride != null)
                        {
                            return propertyAccessorOrAnOverride;
                        }
                    }
                }

                return null;
            }

            private static async Task<bool> IsPropertyAccessorOrAnOverride(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
            {
                return await GetPropertyFromAccessorOrAnOverride(symbol, solution, cancellationToken).ConfigureAwait(false) != null;
            }

            private static string TrimNameToAfterLastDot(string name)
            {
                int position = name.LastIndexOf('.');

                if (position == -1)
                {
                    return name;
                }
                else
                {
                    return name.Substring(position + 1);
                }
            }

            /// <summary>
            /// Given a ISymbol, returns the renameable locations for a given symbol.
            /// </summary>
            public static async Task<IEnumerable<RenameLocation>> GetRenamableDefinitionLocationsAsync(ISymbol referencedSymbol, ISymbol originalSymbol, Solution solution, CancellationToken cancellationToken)
            {
                var shouldIncludeSymbol = await ShouldIncludeSymbolAsync(referencedSymbol, originalSymbol, solution, false, cancellationToken).ConfigureAwait(false);
                if (!shouldIncludeSymbol)
                {
                    return SpecializedCollections.EmptyEnumerable<RenameLocation>();
                }

                // Namespaces are definitions and references all in one. Since every definition
                // location is also a reference, we'll ignore it's definitions.
                if (referencedSymbol.Kind == SymbolKind.Namespace)
                {
                    return SpecializedCollections.EmptyEnumerable<RenameLocation>();
                }

                var results = new List<RenameLocation>();

                // If the original symbol was an alias, then the definitions will just be the
                // location of the alias, always
                if (originalSymbol.Kind == SymbolKind.Alias)
                {
                    var location = originalSymbol.Locations.Single();
                    results.Add(new RenameLocation(location, solution.GetDocument(location.SourceTree).Id));
                    return results;
                }

                var isRenamableAccessor = await IsPropertyAccessorOrAnOverride(referencedSymbol, solution, cancellationToken).ConfigureAwait(false);
                foreach (var location in referencedSymbol.Locations)
                {
                    if (location.IsInSource)
                    {
                        results.Add(new RenameLocation(location, solution.GetDocument(location.SourceTree).Id, isRenamableAccessor: isRenamableAccessor));
                    }
                }

                // If we're renaming a named type, we'll also have to find constructors and
                // destructors declarations that match the name
                if (referencedSymbol.Kind == SymbolKind.NamedType)
                {
                    var namedType = (INamedTypeSymbol)referencedSymbol;

                    foreach (var method in namedType.GetMembers().OfType<IMethodSymbol>())
                    {
                        if (!method.IsImplicitlyDeclared && (method.MethodKind == MethodKind.Constructor ||
                                                      method.MethodKind == MethodKind.StaticConstructor ||
                                                      method.MethodKind == MethodKind.Destructor))
                        {
                            foreach (var location in method.Locations)
                            {
                                if (location.IsInSource)
                                {
                                    var token = location.FindToken(cancellationToken);
                                    if (token.ValueText == referencedSymbol.Name)
                                    {
                                        results.Add(new RenameLocation(location, solution.GetDocument(location.SourceTree).Id));
                                    }
                                }
                            }
                        }
                    }
                }

                return results;
            }

            internal static async Task<IEnumerable<RenameLocation>> GetRenamableReferenceLocationsAsync(ISymbol referencedSymbol, ISymbol originalSymbol, ReferenceLocation location, Solution solution, CancellationToken cancellationToken)
            {
                var shouldIncludeSymbol = await ShouldIncludeSymbolAsync(referencedSymbol, originalSymbol, solution, true, cancellationToken).ConfigureAwait(false);
                if (!shouldIncludeSymbol)
                {
                    return SpecializedCollections.EmptyEnumerable<RenameLocation>();
                }

                // Implicit references are things like a foreach referencing GetEnumerator. We don't
                // want to consider those as part of the set
                if (location.IsImplicit)
                {
                    return SpecializedCollections.EmptyEnumerable<RenameLocation>();
                }

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
                        results.Add(new RenameLocation(aliasLocation, solution.GetDocument(aliasLocation.SourceTree).Id));
                    }
                }
                else
                {
                    // If we bound through an alias, we'll only rename if the alias's name matches
                    // the name of symbol it points to. We do this because it's common to see things
                    // like "using Foo = System.Foo" where people want to import a single type
                    // rather than a whole namespace of stuff.
                    if (location.Alias != null)
                    {
                        if (location.Alias.Name == referencedSymbol.Name)
                        {
                            results.Add(new RenameLocation(location.Location, location.Document.Id,
                                isCandidateLocation: location.IsCandidateLocation, isRenamableAliasUsage: true, isWrittenTo: location.IsWrittenTo));

                            // We also need to add the location of the alias itself
                            var aliasLocation = location.Alias.Locations.Single();
                            results.Add(new RenameLocation(aliasLocation, solution.GetDocument(aliasLocation.SourceTree).Id));
                        }
                    }
                    else
                    {
                        // The simple case, so just the single location and we're done
                        results.Add(new RenameLocation(
                            location.Location,
                            location.Document.Id,
                            isWrittenTo: location.IsWrittenTo,
                            isCandidateLocation: location.IsCandidateLocation,
                            isMethodGroupReference: location.IsCandidateLocation && location.CandidateReason == CandidateReason.MemberGroup,
                            isRenamableAccessor: await IsPropertyAccessorOrAnOverride(referencedSymbol, solution, cancellationToken).ConfigureAwait(false)));
                    }
                }

                return results;
            }

            internal static async Task<Tuple<IEnumerable<RenameLocation>, IEnumerable<RenameLocation>>> GetRenamableLocationsInStringsAndCommentsAsync(
                ISymbol originalSymbol,
                Solution solution,
                ISet<RenameLocation> renameLocations,
                bool renameInStrings,
                bool renameInComments,
                CancellationToken cancellationToken)
            {
                if (!renameInStrings && !renameInComments)
                {
                    return new Tuple<IEnumerable<RenameLocation>, IEnumerable<RenameLocation>>(null, null);
                }

                var renameText = originalSymbol.Name;
                List<RenameLocation> stringLocations = renameInStrings ? new List<RenameLocation>() : null;
                List<RenameLocation> commentLocations = renameInComments ? new List<RenameLocation>() : null;

                foreach (var documentsGroupedByLanguage in RenameUtilities.GetDocumentsAffectedByRename(originalSymbol, solution, renameLocations).GroupBy(d => d.Project.Language))
                {
                    var syntaxFactsLanguageService = solution.Workspace.Services.GetLanguageServices(documentsGroupedByLanguage.Key).GetService<ISyntaxFactsService>();
                    foreach (var document in documentsGroupedByLanguage)
                    {
                        if (renameInStrings)
                        {
                            await AddLocationsToRenameInStringsAsync(document, renameText, syntaxFactsLanguageService,
                                stringLocations, cancellationToken).ConfigureAwait(false);
                        }

                        if (renameInComments)
                        {
                            await AddLocationsToRenameInCommentsAsync(document, renameText, commentLocations, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }

                return new Tuple<IEnumerable<RenameLocation>, IEnumerable<RenameLocation>>(stringLocations, commentLocations);
            }

            private static async Task AddLocationsToRenameInStringsAsync(Document document, string renameText, ISyntaxFactsService syntaxFactsService, List<RenameLocation> renameLocations, CancellationToken cancellationToken)
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var renameTextLength = renameText.Length;

                var renameStringsAndPositions = root
                    .DescendantTokens()
                    .Where(t => syntaxFactsService.IsStringLiteral(t) && t.Span.Length >= renameTextLength)
                    .Select(t => Tuple.Create(t.ToString(), t.Span.Start, t.Span));

                if (renameStringsAndPositions.Any())
                {
                    AddLocationsToRenameInStringsAndComments(document, root.SyntaxTree, renameText,
                        renameStringsAndPositions, renameLocations, isRenameInStrings: true, isRenameInComments: false);
                }
            }

            private static async Task AddLocationsToRenameInCommentsAsync(Document document, string renameText, List<RenameLocation> renameLocations, CancellationToken cancellationToken)
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var renameTextLength = renameText.Length;

                var renameStringsAndPositions = root
                    .DescendantTrivia(descendIntoTrivia: true)
                    .Where(t => t.Span.Length >= renameTextLength)
                    .Select(t => Tuple.Create(t.ToString(), t.Span.Start, t.Token.Span));

                if (renameStringsAndPositions.Any())
                {
                    AddLocationsToRenameInStringsAndComments(document, root.SyntaxTree, renameText,
                        renameStringsAndPositions, renameLocations, isRenameInStrings: false, isRenameInComments: true);
                }
            }

            private static void AddLocationsToRenameInStringsAndComments(
                Document document,
                SyntaxTree tree,
                string renameText,
                IEnumerable<Tuple<string, int, TextSpan>> renameStringsAndPositions,
                List<RenameLocation> renameLocations,
                bool isRenameInStrings,
                bool isRenameInComments)
            {
                var regex = GetRegexForMatch(renameText);
                foreach (var renameStringAndPosition in renameStringsAndPositions)
                {
                    string renameString = renameStringAndPosition.Item1;
                    int renameStringPosition = renameStringAndPosition.Item2;
                    var containingSpan = renameStringAndPosition.Item3;

                    MatchCollection matches = regex.Matches(renameString);

                    foreach (Match match in matches)
                    {
                        int start = renameStringPosition + match.Index;
                        Debug.Assert(renameText.Length == match.Length);
                        var matchTextSpan = new TextSpan(start, renameText.Length);
                        var matchLocation = tree.GetLocation(matchTextSpan);
                        var renameLocation = new RenameLocation(matchLocation, document.Id, containingLocationForStringOrComment: containingSpan);
                        renameLocations.Add(renameLocation);
                    }
                }
            }

            private static Regex GetRegexForMatch(string matchText)
            {
                var matchString = string.Format(@"\b{0}\b", matchText);
                return new Regex(matchString, RegexOptions.CultureInvariant);
            }

            internal static string ReplaceMatchingSubStrings(string replaceInsideString, string matchText, string replacementText)
            {
                var regex = GetRegexForMatch(matchText);
                return regex.Replace(replaceInsideString, replacementText);
            }
        }
    }
}
