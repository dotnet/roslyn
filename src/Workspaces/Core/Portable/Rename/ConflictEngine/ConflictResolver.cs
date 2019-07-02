// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    internal static partial class ConflictResolver
    {
        private static readonly SymbolDisplayFormat s_metadataSymbolDisplayFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeConstraints | SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeModifiers | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
            delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
            extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
            parameterOptions: SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType,
            propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

        private const string s_metadataNameSeparators = " .,:<`>()\r\n";

        /// <summary>
        /// Performs the renaming of the symbol in the solution, identifies renaming conflicts and automatically resolves them where possible.
        /// </summary>
        /// <param name="renameLocationSet">The locations to perform the renaming at.</param>
        /// <param name="originalText">The original name of the identifier.</param>
        /// <param name="replacementText">The new name of the identifier</param>
        /// <param name="optionSet">The option for rename</param>
        /// <param name="hasConflict">Called after renaming references.  Can be used by callers to
        /// indicate if the new symbols that the reference binds to should be considered to be ok or
        /// are in conflict.  'true' means they are conflicts.  'false' means they are not conflicts.
        /// 'null' means that the default conflict check should be used.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A conflict resolution containing the new solution.</returns>
        public static Task<ConflictResolution> ResolveConflictsAsync(
            RenameLocations renameLocationSet,
            string originalText,
            string replacementText,
            OptionSet optionSet,
            Func<IEnumerable<ISymbol>, bool?> hasConflict,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // when someone e.g. renames a symbol from metadata through the API (IDE blocks this), we need to return
            var renameSymbolDeclarationLocation = renameLocationSet.Symbol.Locations.Where(loc => loc.IsInSource).FirstOrDefault();
            if (renameSymbolDeclarationLocation == null)
            {
                // Symbol "{0}" is not from source.
                throw new ArgumentException(string.Format(WorkspacesResources.Symbol_0_is_not_from_source, renameLocationSet.Symbol.Name));
            }

            var session = new Session(renameLocationSet, renameSymbolDeclarationLocation, originalText, replacementText, optionSet, hasConflict, cancellationToken);
            return session.ResolveConflictsAsync();
        }

        /// <summary>
        /// Used to find the symbols associated with the Invocation Expression surrounding the Token
        /// </summary>
        private static IEnumerable<ISymbol> SymbolsForEnclosingInvocationExpressionWorker(SyntaxNode invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken);
            IEnumerable<ISymbol> symbols = null;
            if (symbolInfo.Symbol == null)
            {
                return null;
            }
            else
            {
                symbols = SpecializedCollections.SingletonEnumerable(symbolInfo.Symbol);
                return symbols;
            }
        }

        private static SyntaxNode GetExpansionTargetForLocationPerLanguage(SyntaxToken tokenOrNode, Document document)
        {
            var renameRewriterService = document.GetLanguageService<IRenameRewriterLanguageService>();
            var complexifiedTarget = renameRewriterService.GetExpansionTargetForLocation(tokenOrNode);
            return complexifiedTarget;
        }

        private static bool LocalVariableConflictPerLanguage(SyntaxToken tokenOrNode, Document document, IEnumerable<ISymbol> newReferencedSymbols)
        {
            var renameRewriterService = document.GetLanguageService<IRenameRewriterLanguageService>();
            var isConflict = renameRewriterService.LocalVariableConflict(tokenOrNode, newReferencedSymbols);
            return isConflict;
        }

        private static bool IsIdentifierValid_Worker(Solution solution, string replacementText, IEnumerable<ProjectId> projectIds, CancellationToken cancellationToken)
        {
            foreach (var language in projectIds.Select(p => solution.GetProject(p).Language).Distinct())
            {
                var languageServices = solution.Workspace.Services.GetLanguageServices(language);
                var renameRewriterLanguageService = languageServices.GetService<IRenameRewriterLanguageService>();
                var syntaxFactsLanguageService = languageServices.GetService<ISyntaxFactsService>();
                if (!renameRewriterLanguageService.IsIdentifierValid(replacementText, syntaxFactsLanguageService))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsRenameValid(ConflictResolution conflictResolution, ISymbol renamedSymbol)
        {
            // if we rename an identifier and it now binds to a symbol from metadata this should be treated as
            // an invalid rename.
            return conflictResolution.ReplacementTextValid && renamedSymbol != null && renamedSymbol.Locations.Any(loc => loc.IsInSource);
        }

        private static async Task AddImplicitConflictsAsync(
            ISymbol renamedSymbol,
            ISymbol originalSymbol,
            IEnumerable<ReferenceLocation> implicitReferenceLocations,
            SemanticModel semanticModel,
            Location originalDeclarationLocation,
            int newDeclarationLocationStartingPosition,
            ConflictResolution conflictResolution,
            CancellationToken cancellationToken)
        {
            {
                var renameRewriterService = conflictResolution.NewSolution.Workspace.Services.GetLanguageServices(renamedSymbol.Language).GetService<IRenameRewriterLanguageService>();
                var implicitUsageConflicts = renameRewriterService.ComputePossibleImplicitUsageConflicts(renamedSymbol, semanticModel, originalDeclarationLocation, newDeclarationLocationStartingPosition, cancellationToken);
                foreach (var implicitUsageConflict in implicitUsageConflicts)
                {
                    conflictResolution.AddOrReplaceRelatedLocation(new RelatedLocation(implicitUsageConflict.SourceSpan, conflictResolution.OldSolution.GetDocument(implicitUsageConflict.SourceTree).Id, RelatedLocationType.UnresolvableConflict));
                }
            }

            if (implicitReferenceLocations.IsEmpty())
            {
                return;
            }

            foreach (var implicitReferenceLocationsPerLanguage in implicitReferenceLocations.GroupBy(loc => loc.Document.Project.Language))
            {
                // the location of the implicit reference defines the language rules to check.
                // E.g. foreach in C# using a MoveNext in VB that is renamed to MOVENEXT (within VB)
                var renameRewriterService = implicitReferenceLocationsPerLanguage.First().Document.Project.LanguageServices.GetService<IRenameRewriterLanguageService>();
                var implicitConflicts = await renameRewriterService.ComputeImplicitReferenceConflictsAsync(
                    originalSymbol,
                    renamedSymbol,
                    implicitReferenceLocationsPerLanguage,
                    cancellationToken).ConfigureAwait(false);

                foreach (var implicitConflict in implicitConflicts)
                {
                    conflictResolution.AddRelatedLocation(new RelatedLocation(implicitConflict.SourceSpan, conflictResolution.OldSolution.GetDocument(implicitConflict.SourceTree).Id, RelatedLocationType.UnresolvableConflict));
                }
            }
        }

        /// <summary>
        /// Computes an adds conflicts relating to declarations, which are independent of
        /// location-based checks. Examples of these types of conflicts include renaming a member to
        /// the same name as another member of a type: binding doesn't change (at least from the
        /// perspective of find all references), but we still need to track it.
        /// </summary>
        internal static async Task AddDeclarationConflictsAsync(
            ISymbol renamedSymbol,
            ISymbol renameSymbol,
            IEnumerable<SymbolAndProjectId> referencedSymbols,
            ConflictResolution conflictResolution,
            IDictionary<Location, Location> reverseMappedLocations,
            CancellationToken cancellationToken)
        {
            try
            {
                var project = conflictResolution.NewSolution.GetProject(renamedSymbol.ContainingAssembly, cancellationToken);

                if (renamedSymbol.ContainingSymbol.IsKind(SymbolKind.NamedType))
                {
                    var otherThingsNamedTheSame = renamedSymbol.ContainingType.GetMembers(renamedSymbol.Name)
                                                           .Where(s => !s.Equals(renamedSymbol) &&
                                                                       string.Equals(s.MetadataName, renamedSymbol.MetadataName, StringComparison.Ordinal));

                    IEnumerable<ISymbol> otherThingsNamedTheSameExcludeMethodAndParameterizedProperty;

                    // Possibly overloaded symbols are excluded here and handled elsewhere
                    var semanticFactsService = project.LanguageServices.GetService<ISemanticFactsService>();
                    if (semanticFactsService.SupportsParameterizedProperties)
                    {
                        otherThingsNamedTheSameExcludeMethodAndParameterizedProperty = otherThingsNamedTheSame
                            .Where(s => !s.MatchesKind(SymbolKind.Method, SymbolKind.Property) ||
                                !renamedSymbol.MatchesKind(SymbolKind.Method, SymbolKind.Property));
                    }
                    else
                    {
                        otherThingsNamedTheSameExcludeMethodAndParameterizedProperty = otherThingsNamedTheSame
                            .Where(s => s.Kind != SymbolKind.Method || renamedSymbol.Kind != SymbolKind.Method);
                    }

                    AddConflictingSymbolLocations(otherThingsNamedTheSameExcludeMethodAndParameterizedProperty, conflictResolution, reverseMappedLocations);
                }


                if (renamedSymbol.IsKind(SymbolKind.Namespace) && renamedSymbol.ContainingSymbol.IsKind(SymbolKind.Namespace))
                {
                    var otherThingsNamedTheSame = ((INamespaceSymbol)renamedSymbol.ContainingSymbol).GetMembers(renamedSymbol.Name)
                                                            .Where(s => !s.Equals(renamedSymbol) &&
                                                                        !s.IsKind(SymbolKind.Namespace) &&
                                                                        string.Equals(s.MetadataName, renamedSymbol.MetadataName, StringComparison.Ordinal));

                    AddConflictingSymbolLocations(otherThingsNamedTheSame, conflictResolution, reverseMappedLocations);
                }

                if (renamedSymbol.IsKind(SymbolKind.NamedType) && renamedSymbol.ContainingSymbol is INamespaceOrTypeSymbol)
                {
                    var otherThingsNamedTheSame = ((INamespaceOrTypeSymbol)renamedSymbol.ContainingSymbol).GetMembers(renamedSymbol.Name)
                                                            .Where(s => !s.Equals(renamedSymbol) &&
                                                                        string.Equals(s.MetadataName, renamedSymbol.MetadataName, StringComparison.Ordinal));

                    var conflictingSymbolLocations = otherThingsNamedTheSame.Where(s => !s.IsKind(SymbolKind.Namespace));
                    if (otherThingsNamedTheSame.Any(s => s.IsKind(SymbolKind.Namespace)))
                    {
                        conflictingSymbolLocations = conflictingSymbolLocations.Concat(renamedSymbol);
                    }

                    AddConflictingSymbolLocations(conflictingSymbolLocations, conflictResolution, reverseMappedLocations);
                }

                // Some types of symbols (namespaces, cref stuff, etc) might not have ContainingAssemblies
                if (renamedSymbol.ContainingAssembly != null)
                {
                    // There also might be language specific rules we need to include
                    var languageRenameService = project.LanguageServices.GetService<IRenameRewriterLanguageService>();
                    var languageConflicts = await languageRenameService.ComputeDeclarationConflictsAsync(
                        conflictResolution.ReplacementText,
                        renamedSymbol,
                        renameSymbol,
                        referencedSymbols,
                        conflictResolution.OldSolution,
                        conflictResolution.NewSolution,
                        reverseMappedLocations,
                        cancellationToken).ConfigureAwait(false);

                    foreach (var languageConflict in languageConflicts)
                    {
                        conflictResolution.AddOrReplaceRelatedLocation(new RelatedLocation(languageConflict.SourceSpan, conflictResolution.OldSolution.GetDocument(languageConflict.SourceTree).Id, RelatedLocationType.UnresolvableConflict));
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                // A NullReferenceException is happening in this method, but the dumps do not
                // contain information about this stack frame because this method is async and
                // therefore the exception filter in IdentifyConflictsAsync is insufficient.
                // See https://devdiv.visualstudio.com/DevDiv/_workitems?_a=edit&id=378642

                throw ExceptionUtilities.Unreachable;
            }
        }

        internal static void AddConflictingParametersOfProperties(
            IEnumerable<ISymbol> properties, string newPropertyName, ArrayBuilder<Location> conflicts)
        {
            // check if the new property name conflicts with any parameter of the properties.
            // Note: referencedSymbols come from the original solution, so there is no need to reverse map the locations of the parameters
            foreach (var symbol in properties)
            {
                var prop = (IPropertySymbol)symbol;

                var conflictingParameter = prop.Parameters.FirstOrDefault(param => string.Compare(param.Name, newPropertyName, StringComparison.OrdinalIgnoreCase) == 0);

                if (conflictingParameter != null)
                {
                    conflicts.AddRange(conflictingParameter.Locations);
                }
            }
        }

        private static void AddConflictingSymbolLocations(IEnumerable<ISymbol> conflictingSymbols, ConflictResolution conflictResolution, IDictionary<Location, Location> reverseMappedLocations)
        {
            foreach (var newSymbol in conflictingSymbols)
            {
                foreach (var newLocation in newSymbol.Locations)
                {
                    if (newLocation.IsInSource)
                    {
                        if (reverseMappedLocations.TryGetValue(newLocation, out var oldLocation))
                        {
                            conflictResolution.AddOrReplaceRelatedLocation(new RelatedLocation(oldLocation.SourceSpan, conflictResolution.OldSolution.GetDocument(oldLocation.SourceTree).Id, RelatedLocationType.UnresolvableConflict));
                        }
                    }
                }
            }
        }

        public static async Task<RenameDeclarationLocationReference[]> CreateDeclarationLocationAnnotationsAsync(
            Solution solution,
            IEnumerable<ISymbol> symbols,
            CancellationToken cancellationToken)
        {
            var renameDeclarationLocations = new RenameDeclarationLocationReference[symbols.Count()];

            var symbolIndex = 0;
            foreach (var symbol in symbols)
            {
                var locations = symbol.Locations;
                var overriddenFromMetadata = false;

                if (symbol.IsOverride)
                {
                    var overriddenSymbol = symbol.OverriddenMember();

                    if (overriddenSymbol != null)
                    {
                        overriddenSymbol = await SymbolFinder.FindSourceDefinitionAsync(overriddenSymbol, solution, cancellationToken).ConfigureAwait(false);
                        overriddenFromMetadata = overriddenSymbol == null || overriddenSymbol.Locations.All(loc => loc.IsInMetadata);
                    }
                }

                var location = await GetSymbolLocationAsync(solution, symbol, cancellationToken).ConfigureAwait(false);
                if (location != null && location.IsInSource)
                {
                    renameDeclarationLocations[symbolIndex] = new RenameDeclarationLocationReference(solution.GetDocumentId(location.SourceTree), location.SourceSpan, overriddenFromMetadata, locations.Count());
                }
                else
                {
                    renameDeclarationLocations[symbolIndex] = new RenameDeclarationLocationReference(GetString(symbol), locations.Count());
                }

                symbolIndex++;
            }

            return renameDeclarationLocations;
        }

        private static string GetString(ISymbol symbol)
        {
            if (symbol.IsAnonymousType())
            {
                return symbol.ToDisplayParts(s_metadataSymbolDisplayFormat)
                    .WhereAsArray(p => p.Kind != SymbolDisplayPartKind.PropertyName && p.Kind != SymbolDisplayPartKind.FieldName)
                    .ToDisplayString();
            }
            else
            {
                return symbol.ToDisplayString(s_metadataSymbolDisplayFormat);
            }
        }

        /// <summary>
        /// Gives the First Location for a given Symbol by ordering the locations using DocumentId first and Location starting position second
        /// </summary>
        private static async Task<Location> GetSymbolLocationAsync(Solution solution, ISymbol symbol, CancellationToken cancellationToken)
        {
            var locations = symbol.Locations;

            var originalsourcesymbol = await SymbolFinder.FindSourceDefinitionAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
            if (originalsourcesymbol != null)
            {
                locations = originalsourcesymbol.Locations;
            }

            var orderedLocations = locations.OrderBy(l => l.IsInSource ? solution.GetDocumentId(l.SourceTree).Id : Guid.Empty)
                .ThenBy(l => l.IsInSource ? l.SourceSpan.Start : int.MaxValue);

            return orderedLocations.FirstOrDefault();
        }

        private static bool HeuristicMetadataNameEquivalenceCheck(
            string oldMetadataName,
            string newMetadataName,
            string originalText,
            string replacementText)
        {
            if (string.Equals(oldMetadataName, newMetadataName, StringComparison.Ordinal))
            {
                return true;
            }

            var index = 0;
            index = newMetadataName.IndexOf(replacementText, 0);
            var newMetadataNameBuilder = new StringBuilder();

            // Every loop updates the newMetadataName to resemble the oldMetadataName
            while (index != -1 && index < oldMetadataName.Length)
            {
                // This check is to ses if the part of string before the string match, matches
                if (!IsSubStringEqual(oldMetadataName, newMetadataName, index))
                {
                    return false;
                }

                // Ok to replace
                if (IsWholeIdentifier(newMetadataName, replacementText, index))
                {
                    newMetadataNameBuilder.Append(newMetadataName, 0, index);
                    newMetadataNameBuilder.Append(originalText);
                    newMetadataNameBuilder.Append(newMetadataName, index + replacementText.Length, newMetadataName.Length - (index + replacementText.Length));
                    newMetadataName = newMetadataNameBuilder.ToString();
                    newMetadataNameBuilder.Clear();
                }

                index = newMetadataName.IndexOf(replacementText, index + 1);
            }

            return string.Equals(newMetadataName, oldMetadataName, StringComparison.Ordinal);
        }

        private static bool IsSubStringEqual(
            string str1,
            string str2,
            int index)
        {
            Debug.Assert(index <= str1.Length && index <= str2.Length, "Index cannot be greater than the string");
            var currentIndex = 0;
            while (currentIndex < index)
            {
                if (str1[currentIndex] != str2[currentIndex])
                {
                    return false;
                }

                currentIndex++;
            }

            return true;
        }

        private static bool IsWholeIdentifier(
            string metadataName,
            string searchText,
            int index)
        {
            if (index == -1)
            {
                return false;
            }

            // Check for the previous char
            if (index != 0)
            {
                var previousChar = metadataName[index - 1];

                if (!IsIdentifierSeparator(previousChar))
                {
                    return false;
                }
            }

            // Check for the next char
            if (index + searchText.Length != metadataName.Length)
            {
                var nextChar = metadataName[index + searchText.Length];

                if (!IsIdentifierSeparator(nextChar))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsIdentifierSeparator(char element)
        {
            return s_metadataNameSeparators.IndexOf(element) != -1;
        }
    }
}
