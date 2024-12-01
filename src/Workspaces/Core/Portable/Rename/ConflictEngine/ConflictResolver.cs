// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine;

internal static partial class ConflictResolver
{
    private static readonly SymbolDisplayFormat s_metadataSymbolDisplayFormat = new(
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
    /// Performs the renaming of the symbol in the solution, identifies renaming conflicts and automatically
    /// resolves them where possible.
    /// </summary>
    /// <param name="replacementText">The new name of the identifier</param>
    /// <param name="nonConflictSymbolKeys">Used after renaming references. References that now bind to any of these
    /// symbols are not considered to be in conflict. Useful for features that want to rename existing references to
    /// point at some existing symbol. Normally this would be a conflict, but this can be used to override that
    /// behavior.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A conflict resolution containing the new solution.</returns>
    internal static async Task<ConflictResolution> ResolveLightweightConflictsAsync(
        ISymbol symbol,
        LightweightRenameLocations lightweightRenameLocations,
        string replacementText,
        ImmutableArray<SymbolKey> nonConflictSymbolKeys,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using (Logger.LogBlock(FunctionId.Renamer_ResolveConflictsAsync, cancellationToken))
        {
            var solution = lightweightRenameLocations.Solution;
            var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var serializableSymbol = SerializableSymbolAndProjectId.Dehydrate(lightweightRenameLocations.Solution, symbol, cancellationToken);
                var serializableLocationSet = lightweightRenameLocations.Dehydrate();

                var result = await client.TryInvokeAsync<IRemoteRenamerService, SerializableConflictResolution?>(
                    solution,
                    (service, solutionInfo, cancellationToken) => service.ResolveConflictsAsync(solutionInfo, serializableSymbol, serializableLocationSet, replacementText, nonConflictSymbolKeys, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                if (result.HasValue && result.Value != null)
                    return await result.Value.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false);

                // TODO: do not fall back to in-proc if client is available (https://github.com/dotnet/roslyn/issues/47557)
            }
        }

        var heavyweightLocations = await lightweightRenameLocations.ToSymbolicLocationsAsync(symbol, cancellationToken).ConfigureAwait(false);
        if (heavyweightLocations is null)
            return new ConflictResolution(WorkspacesResources.Failed_to_resolve_rename_conflicts);

        return await ResolveSymbolicLocationConflictsInCurrentProcessAsync(
            heavyweightLocations, replacementText, nonConflictSymbolKeys, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Finds any conflicts that would arise from using <paramref name="replacementText"/> as the new name for a
    /// symbol and returns how to resolve those conflicts.  Will not cross any process boundaries to do this.
    /// </summary>
    internal static async Task<ConflictResolution> ResolveSymbolicLocationConflictsInCurrentProcessAsync(
        SymbolicRenameLocations renameLocations,
        string replacementText,
        ImmutableArray<SymbolKey> nonConflictSymbolKeys,
        CancellationToken cancellationToken)
    {
        // when someone e.g. renames a symbol from metadata through the API (IDE blocks this), we need to return
        var renameSymbolDeclarationLocation = renameLocations.Symbol.Locations.Where(loc => loc.IsInSource).FirstOrDefault();
        if (renameSymbolDeclarationLocation == null)
        {
            // Symbol "{0}" is not from source.
            return new ConflictResolution(string.Format(WorkspacesResources.Symbol_0_is_not_from_source, renameLocations.Symbol.Name));
        }

        var resolution = await ResolveMutableConflictsAsync(
            renameLocations, renameSymbolDeclarationLocation, replacementText, nonConflictSymbolKeys, cancellationToken).ConfigureAwait(false);

        return resolution.ToConflictResolution();
    }

    private static Task<MutableConflictResolution> ResolveMutableConflictsAsync(
        SymbolicRenameLocations renameLocationSet,
        Location renameSymbolDeclarationLocation,
        string replacementText,
        ImmutableArray<SymbolKey> nonConflictSymbolKeys,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = new Session(
            renameLocationSet, renameSymbolDeclarationLocation, replacementText, nonConflictSymbolKeys, cancellationToken);
        return session.ResolveConflictsAsync();
    }

    /// <summary>
    /// Used to find the symbols associated with the Invocation Expression surrounding the Token
    /// </summary>
    private static ImmutableArray<ISymbol> SymbolsForEnclosingInvocationExpressionWorker(SyntaxNode invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken);
        return symbolInfo.Symbol == null
            ? default
            : [symbolInfo.Symbol];
    }

    private static SyntaxNode? GetExpansionTargetForLocationPerLanguage(SyntaxToken tokenOrNode, Document document)
    {
        var renameRewriterService = document.GetRequiredLanguageService<IRenameRewriterLanguageService>();
        var complexifiedTarget = renameRewriterService.GetExpansionTargetForLocation(tokenOrNode);
        return complexifiedTarget;
    }

    private static bool LocalVariableConflictPerLanguage(SyntaxToken tokenOrNode, Document document, ImmutableArray<ISymbol> newReferencedSymbols)
    {
        var renameRewriterService = document.GetRequiredLanguageService<IRenameRewriterLanguageService>();
        var isConflict = renameRewriterService.LocalVariableConflict(tokenOrNode, newReferencedSymbols);
        return isConflict;
    }

    private static bool IsIdentifierValid_Worker(Solution solution, string replacementText, IEnumerable<ProjectId> projectIds)
    {
        foreach (var language in projectIds.Select(p => solution.GetRequiredProject(p).Language).Distinct())
        {
            var languageServices = solution.Services.GetLanguageServices(language);
            var renameRewriterLanguageService = languageServices.GetRequiredService<IRenameRewriterLanguageService>();
            var syntaxFactsLanguageService = languageServices.GetRequiredService<ISyntaxFactsService>();
            if (!renameRewriterLanguageService.IsIdentifierValid(replacementText, syntaxFactsLanguageService))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsRenameValid(MutableConflictResolution conflictResolution, ISymbol renamedSymbol)
    {
        // if we rename an identifier and it now binds to a symbol from metadata this should be treated as
        // an invalid rename.
        return conflictResolution.ReplacementTextValid && renamedSymbol != null && renamedSymbol.Locations.Any(static loc => loc.IsInSource);
    }

    private static async Task AddImplicitConflictsAsync(
        ISymbol renamedSymbol,
        ISymbol originalSymbol,
        IEnumerable<ReferenceLocation> implicitReferenceLocations,
        SemanticModel semanticModel,
        Location originalDeclarationLocation,
        int newDeclarationLocationStartingPosition,
        MutableConflictResolution conflictResolution,
        CancellationToken cancellationToken)
    {
        {
            var renameRewriterService =
                conflictResolution.CurrentSolution.Services.GetRequiredLanguageService<IRenameRewriterLanguageService>(renamedSymbol.Language);
            var implicitUsageConflicts = renameRewriterService.ComputePossibleImplicitUsageConflicts(renamedSymbol, semanticModel, originalDeclarationLocation, newDeclarationLocationStartingPosition, cancellationToken);
            foreach (var implicitUsageConflict in implicitUsageConflicts)
            {
                Contract.ThrowIfNull(implicitUsageConflict.SourceTree);
                conflictResolution.AddOrReplaceRelatedLocation(new RelatedLocation(
                    implicitUsageConflict.SourceSpan, conflictResolution.OldSolution.GetRequiredDocument(implicitUsageConflict.SourceTree).Id, RelatedLocationType.UnresolvableConflict));
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
            var renameRewriterService = implicitReferenceLocationsPerLanguage.First().Document.Project.Services.GetRequiredService<IRenameRewriterLanguageService>();
            var implicitConflicts = await renameRewriterService.ComputeImplicitReferenceConflictsAsync(
                originalSymbol,
                renamedSymbol,
                implicitReferenceLocationsPerLanguage,
                cancellationToken).ConfigureAwait(false);

            foreach (var implicitConflict in implicitConflicts)
            {
                Contract.ThrowIfNull(implicitConflict.SourceTree);
                conflictResolution.AddRelatedLocation(new RelatedLocation(
                    implicitConflict.SourceSpan, conflictResolution.OldSolution.GetRequiredDocument(implicitConflict.SourceTree).Id, RelatedLocationType.UnresolvableConflict));
            }
        }
    }

    /// <summary>
    /// Computes an adds conflicts relating to declarations, which are independent of
    /// location-based checks. Examples of these types of conflicts include renaming a member to
    /// the same name as another member of a type: binding doesn't change (at least from the
    /// perspective of find all references), but we still need to track it.
    /// </summary>
    private static async Task AddDeclarationConflictsAsync(
        ISymbol renamedSymbol,
        ISymbol renameSymbol,
        IEnumerable<ISymbol> referencedSymbols,
        MutableConflictResolution conflictResolution,
        IDictionary<Location, Location> reverseMappedLocations,
        CancellationToken cancellationToken)
    {
        try
        {
            var projectOpt = conflictResolution.CurrentSolution.GetProject(renamedSymbol.ContainingAssembly, cancellationToken);
            if (renamedSymbol.ContainingSymbol.IsKind(SymbolKind.NamedType))
            {
                Contract.ThrowIfNull(projectOpt);
                var otherThingsNamedTheSame = renamedSymbol.ContainingType.GetMembers(renamedSymbol.Name)
                                                       .Where(s => !s.Equals(renamedSymbol) &&
                                                                   string.Equals(s.MetadataName, renamedSymbol.MetadataName, StringComparison.Ordinal));

                IEnumerable<ISymbol> otherThingsNamedTheSameExcludeMethodAndParameterizedProperty;

                // Possibly overloaded symbols are excluded here and handled elsewhere
                var semanticFactsService = projectOpt.Services.GetRequiredService<ISemanticFactsService>();
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
                Contract.ThrowIfNull(projectOpt);
                // There also might be language specific rules we need to include
                var languageRenameService = projectOpt.Services.GetRequiredService<IRenameRewriterLanguageService>();
                var languageConflicts = await languageRenameService.ComputeDeclarationConflictsAsync(
                    conflictResolution.ReplacementText,
                    renamedSymbol,
                    renameSymbol,
                    referencedSymbols,
                    conflictResolution.OldSolution,
                    conflictResolution.CurrentSolution,
                    reverseMappedLocations,
                    cancellationToken).ConfigureAwait(false);

                foreach (var languageConflict in languageConflicts)
                {
                    Contract.ThrowIfNull(languageConflict.SourceTree);
                    conflictResolution.AddOrReplaceRelatedLocation(new RelatedLocation(
                        languageConflict.SourceSpan, conflictResolution.OldSolution.GetRequiredDocument(languageConflict.SourceTree).Id, RelatedLocationType.UnresolvableConflict));
                }
            }
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
        {
            // A NullReferenceException is happening in this method, but the dumps do not
            // contain information about this stack frame because this method is async and
            // therefore the exception filter in IdentifyConflictsAsync is insufficient.
            // See https://devdiv.visualstudio.com/DevDiv/_workitems?_a=edit&id=378642

            throw ExceptionUtilities.Unreachable();
        }
    }

    private static void AddConflictingSymbolLocations(IEnumerable<ISymbol> conflictingSymbols, MutableConflictResolution conflictResolution, IDictionary<Location, Location> reverseMappedLocations)
    {
        foreach (var newSymbol in conflictingSymbols)
        {
            foreach (var newLocation in newSymbol.Locations)
            {
                if (newLocation.IsInSource)
                {
                    if (reverseMappedLocations.TryGetValue(newLocation, out var oldLocation))
                    {
                        Contract.ThrowIfNull(oldLocation.SourceTree);
                        conflictResolution.AddOrReplaceRelatedLocation(new RelatedLocation(
                            oldLocation.SourceSpan, conflictResolution.OldSolution.GetRequiredDocument(oldLocation.SourceTree).Id, RelatedLocationType.UnresolvableConflict));
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
                var overriddenSymbol = symbol.GetOverriddenMember();

                if (overriddenSymbol != null)
                {
                    overriddenSymbol = await SymbolFinder.FindSourceDefinitionAsync(overriddenSymbol, solution, cancellationToken).ConfigureAwait(false);
                    overriddenFromMetadata = overriddenSymbol == null || overriddenSymbol.Locations.All(loc => loc.IsInMetadata);
                }
            }

            var location = await GetSymbolLocationAsync(solution, symbol, cancellationToken).ConfigureAwait(false);
            if (location != null && location.IsInSource)
            {
                renameDeclarationLocations[symbolIndex] = new RenameDeclarationLocationReference(solution.GetDocumentId(location.SourceTree), location.SourceSpan, overriddenFromMetadata, locations.Length);
            }
            else
            {
                renameDeclarationLocations[symbolIndex] = new RenameDeclarationLocationReference(GetString(symbol), locations.Length);
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
                .WhereAsArray(p => p.Kind is not SymbolDisplayPartKind.PropertyName and not SymbolDisplayPartKind.FieldName)
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
    private static async Task<Location?> GetSymbolLocationAsync(Solution solution, ISymbol symbol, CancellationToken cancellationToken)
    {
        var locations = symbol.Locations;

        var originalsourcesymbol = await SymbolFinder.FindSourceDefinitionAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
        if (originalsourcesymbol != null)
        {
            locations = originalsourcesymbol.Locations;
        }

        var orderedLocations = locations
            .OrderBy(l => l.IsInSource ? solution.GetDocumentId(l.SourceTree)!.Id : Guid.Empty)
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

        var index = newMetadataName.IndexOf(replacementText, 0);
        var newMetadataNameBuilder = new StringBuilder();

        // Every loop updates the newMetadataName to resemble the oldMetadataName
        while (index != -1 && index < oldMetadataName.Length)
        {
            // This check is to see if the part of string before the string match, matches
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
        => s_metadataNameSeparators.IndexOf(element) != -1;
}
