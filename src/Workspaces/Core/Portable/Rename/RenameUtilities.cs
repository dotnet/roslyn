// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename;

internal static class RenameUtilities
{
    internal static SyntaxToken UpdateAliasAnnotation(SyntaxToken token, ISymbol aliasSymbol, string replacementText)
    {
        // If the below Single() assert fails then it means the token has gone through a rename session where
        // it obtained an AliasSyntaxAnnotation and it is going through another rename session. Make sure the token
        // has only one annotation pertaining to the current session or try to extract only the current session annotation
        var originalAliasAnnotation = token.GetAnnotations(AliasAnnotation.Kind).Single();
        var originalAliasName = AliasAnnotation.GetAliasName(originalAliasAnnotation);

        if (originalAliasName == aliasSymbol.Name)
        {
            token = token.WithoutAnnotations(originalAliasAnnotation);
            var replacementAliasAnnotation = AliasAnnotation.Create(replacementText);
            token = token.WithAdditionalAnnotations(replacementAliasAnnotation);
        }

        return token;
    }

    /// <summary>
    /// Determines if a type symbol is an unrenamable target for an alias.
    /// Such types include arrays, tuples, pointers, function pointers, and dynamic, which cannot be renamed
    /// themselves but can be aliased with the "using alias = type" feature.
    /// </summary>
    internal static bool IsUnrenamableAliasTarget(ITypeSymbol typeSymbol)
    {
        return typeSymbol.IsTupleType ||
            typeSymbol.TypeKind is TypeKind.Array or TypeKind.Pointer or TypeKind.FunctionPointer or TypeKind.Dynamic;
    }

    /// <summary>
    /// Filters symbols to determine which should be used for renaming when both an alias and its target are present.
    /// For aliases to unrenamable types (like tuples, arrays, pointers, etc.), keeps the alias symbol.
    /// Otherwise, keeps the non-alias symbols (original behavior).
    /// </summary>
    internal static ImmutableArray<ISymbol> FilterAliasSymbols(ImmutableArray<ISymbol> symbols)
    {
        if (symbols.Length <= 1)
            return symbols;

        var aliasSymbol = symbols.FirstOrDefault(s => s.Kind == SymbolKind.Alias);
        var nonAliasSymbols = symbols.WhereAsArray(s => s.Kind != SymbolKind.Alias);

        // For aliases to types that cannot be renamed (like tuples, arrays, pointers, function pointers),
        // we should rename the alias itself, not the target type.
        if (aliasSymbol != null &&
            nonAliasSymbols is [ITypeSymbol targetType] &&
            IsUnrenamableAliasTarget(targetType))
        {
            // Keep the alias symbol for renaming
            return [aliasSymbol];
        }

        // Original behavior: use the non-alias symbols
        return nonAliasSymbols;
    }

    internal static ImmutableArray<ISymbol> GetSymbolsTouchingPosition(
        int position, SemanticModel semanticModel, SolutionServices services, CancellationToken cancellationToken)
    {
        var bindableToken = semanticModel.SyntaxTree.GetRoot(cancellationToken).FindToken(position, findInsideTrivia: true);
        var semanticInfo = semanticModel.GetSemanticInfo(bindableToken, services, cancellationToken);
        var symbols = semanticInfo.DeclaredSymbol != null
            ? [semanticInfo.DeclaredSymbol]
            : semanticInfo.GetSymbols(includeType: false);

        // if there are more than one symbol, then remove the alias symbols.
        // When using (not declaring) an alias, the alias symbol and the target symbol are returned
        // by GetSymbols
        if (symbols.Length > 1)
        {
            symbols = FilterAliasSymbols(symbols);
        }

        if (symbols.Length == 0)
        {
            var info = semanticModel.GetSymbolInfo(bindableToken, cancellationToken);
            if (info.CandidateReason == CandidateReason.MemberGroup)
            {
                return info.CandidateSymbols;
            }
        }

        return symbols;
    }

    private static bool IsSymbolDefinedInsideMethod(ISymbol symbol)
    {
        return
            symbol.Kind is SymbolKind.Local or
            SymbolKind.Label or
            SymbolKind.RangeVariable or
            SymbolKind.Parameter;
    }

    internal static IEnumerable<Document> GetDocumentsAffectedByRename(ISymbol symbol, Solution solution, IEnumerable<RenameLocation> renameLocations)
    {
        if (IsSymbolDefinedInsideMethod(symbol))
        {
            // if the symbol was declared inside of a method, don't check for conflicts in non-renamed documents.
            return renameLocations.Select(l => solution.GetRequiredDocument(l.Location.SourceTree!));
        }
        else
        {
            var documentsOfRenameSymbolDeclaration = symbol.Locations.SelectAsArray(l => l.IsInSource, l => solution.GetRequiredDocument(l.SourceTree!));
            var projectIdsOfRenameSymbolDeclaration =
                documentsOfRenameSymbolDeclaration.SelectMany(d => d.GetLinkedDocumentIds())
                .Concat(documentsOfRenameSymbolDeclaration.First().Id)
                .Select(d => d.ProjectId).Distinct();

            // perf optimization: only look in declaring project when possible
            if (ShouldRenameOnlyAffectDeclaringProject(symbol))
            {
                var isSubset = renameLocations.Select(l => l.DocumentId.ProjectId).Distinct().Except(projectIdsOfRenameSymbolDeclaration).IsEmpty();
                Contract.ThrowIfFalse(isSubset);
                return projectIdsOfRenameSymbolDeclaration.SelectMany(p => solution.GetRequiredProject(p).Documents);
            }
            else
            {
                // We are trying to figure out the projects that directly depend on the project that contains the declaration for
                // the rename symbol.  Other projects should not be affected by the rename.
                var relevantProjects = projectIdsOfRenameSymbolDeclaration.Concat(projectIdsOfRenameSymbolDeclaration.SelectMany(p =>
                   solution.GetProjectDependencyGraph().GetProjectsThatDirectlyDependOnThisProject(p))).Distinct();
                return relevantProjects.SelectMany(p => solution.GetRequiredProject(p).Documents);
            }
        }
    }

    /// <summary>
    /// Renaming a private symbol typically confines the set of references and potential
    /// conflicts to that symbols declaring project. However, rename may cascade to
    /// non-public symbols which may then require other projects be considered.
    /// </summary>
    private static bool ShouldRenameOnlyAffectDeclaringProject(ISymbol symbol)
    {
        if (symbol.DeclaredAccessibility != Accessibility.Private)
        {
            // non-private members can influence other projects.
            return false;
        }

        if (symbol.ExplicitInterfaceImplementations().Any())
        {
            // Explicit interface implementations can cascade to other projects
            return false;
        }

        if (symbol.IsOverride)
        {
            // private-overrides aren't actually legal.  But if we see one, we tolerate it and search other projects in case
            // they override it.
            // https://github.com/dotnet/roslyn/issues/25682
            return false;
        }

        return true;
    }

    internal static TokenRenameInfo GetTokenRenameInfo(
        ISemanticFactsService semanticFacts,
        SemanticModel semanticModel,
        SyntaxToken token,
        CancellationToken cancellationToken)
    {
        var symbol = semanticFacts.GetDeclaredSymbol(semanticModel, token, cancellationToken);
        if (symbol != null)
        {
            return TokenRenameInfo.CreateSingleSymbolTokenInfo(symbol);
        }

        var symbolInfo = semanticModel.GetSymbolInfo(token, cancellationToken);
        if (symbolInfo.Symbol != null)
        {
            if (symbolInfo.Symbol.IsTupleType())
            {
                return TokenRenameInfo.NoSymbolsTokenInfo;
            }

            return TokenRenameInfo.CreateSingleSymbolTokenInfo(symbolInfo.Symbol);
        }

        if (symbolInfo.CandidateReason == CandidateReason.MemberGroup && symbolInfo.CandidateSymbols.Any())
        {
            // This is a reference from a nameof expression. Allow the rename but set the RenameOverloads option
            return TokenRenameInfo.CreateMemberGroupTokenInfo(symbolInfo.CandidateSymbols);
        }

        // If we have overload resolution issues at the callsite, we generally don't want to rename (as it's unclear
        // which overload the user is actually calling).  However, if there is just a single overload, there's no real
        // issue since it's clear which one the user wants to rename in that case.
        if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure && symbolInfo.CandidateSymbols.Length == 1)
            return TokenRenameInfo.CreateMemberGroupTokenInfo(symbolInfo.CandidateSymbols);

        if (RenameLocation.ShouldRename(symbolInfo.CandidateReason) &&
            symbolInfo.CandidateSymbols.Length == 1)
        {
            // TODO(cyrusn): We're allowing rename here, but we likely should let the user
            // know that there is an error in the code and that rename results might be
            // inaccurate.
            return TokenRenameInfo.CreateSingleSymbolTokenInfo(symbolInfo.CandidateSymbols[0]);
        }

        return TokenRenameInfo.NoSymbolsTokenInfo;
    }

    public static IEnumerable<ISymbol> GetOverloadedSymbols(ISymbol symbol)
    {
        if (symbol is IMethodSymbol)
        {
            var containingType = symbol.ContainingType;
            if (containingType.Kind == SymbolKind.NamedType)
            {
                foreach (var member in containingType.GetMembers())
                {
                    if (string.Equals(member.MetadataName, symbol.MetadataName, StringComparison.Ordinal) && member is IMethodSymbol && !member.Equals(symbol))
                    {
                        yield return member;
                    }
                }
            }
        }
    }

    public static async Task<ISymbol?> TryGetPropertyFromAccessorOrAnOverrideAsync(
        ISymbol symbol, Solution solution, CancellationToken cancellationToken)
    {
        if (symbol.IsPropertyAccessor())
            return ((IMethodSymbol)symbol).AssociatedSymbol;

        if (symbol.IsOverride && symbol.GetOverriddenMember() != null)
        {
            var originalSourceSymbol = await SymbolFinder.FindSourceDefinitionAsync(
                symbol.GetOverriddenMember(), solution, cancellationToken).ConfigureAwait(false);

            if (originalSourceSymbol != null)
                return await TryGetPropertyFromAccessorOrAnOverrideAsync(originalSourceSymbol, solution, cancellationToken).ConfigureAwait(false);
        }

        if (symbol.Kind == SymbolKind.Method &&
            symbol.ContainingType.TypeKind == TypeKind.Interface)
        {
            var methodImplementors = await SymbolFinder.FindImplementationsAsync(
                symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (var methodImplementor in methodImplementors)
            {
                var propertyAccessorOrAnOverride = await TryGetPropertyFromAccessorOrAnOverrideAsync(methodImplementor, solution, cancellationToken).ConfigureAwait(false);
                if (propertyAccessorOrAnOverride != null)
                    return propertyAccessorOrAnOverride;
            }
        }

        return null;
    }

    public static string ReplaceMatchingSubStrings(
        string replaceInsideString,
        string matchText,
        string replacementText,
        ImmutableSortedSet<TextSpan>? subSpansToReplace = null)
    {
        if (subSpansToReplace == null)
        {
            // We do not have already computed sub-spans to replace inside the string.
            // Get regex for matches within the string and replace all matches with replacementText.
            var regex = GetRegexForMatch(matchText);
            return regex.Replace(replaceInsideString, replacementText);
        }
        else
        {
            // We are provided specific matches to replace inside the string.
            // Process the input string from start to end, replacing matchText with replacementText
            // at the provided sub-spans within the string for these matches.
            var stringBuilder = new StringBuilder();
            var startOffset = 0;
            foreach (var subSpan in subSpansToReplace)
            {
                Debug.Assert(subSpan.Start <= replaceInsideString.Length);
                Debug.Assert(subSpan.End <= replaceInsideString.Length);

                // Verify that provided sub-span has a match with matchText.
                if (replaceInsideString.Substring(subSpan.Start, subSpan.Length) != matchText)
                    continue;

                // Append the sub-string from last match till the next match
                var offset = subSpan.Start - startOffset;
                stringBuilder.Append(replaceInsideString.Substring(startOffset, offset));

                // Append the replacementText
                stringBuilder.Append(replacementText);

                // Update startOffset to process the next match.
                startOffset += offset + subSpan.Length;
            }

            // Append the remaining of the sub-string within replaceInsideString after the last match. 
            stringBuilder.Append(replaceInsideString[startOffset..]);

            return stringBuilder.ToString();
        }
    }

    public static Regex GetRegexForMatch(string matchText)
    {
        var matchString = string.Format(@"\b{0}\b", matchText);
        return new Regex(matchString, RegexOptions.CultureInvariant);
    }

    /// <summary>
    /// Given a symbol in a document, returns the "right" symbol that should be renamed in
    /// the case the name binds to things like aliases _and_ the underlying type at once.
    /// </summary>
    public static async Task<ISymbol?> TryGetRenamableSymbolAsync(
        Document document, int position, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var semanticInfo = await SymbolFinder.GetSemanticInfoAtPositionAsync(
            semanticModel, position, document.Project.Solution.Services, cancellationToken).ConfigureAwait(false);
        
        var symbol = semanticInfo.GetAnySymbol(includeType: false);

        if (symbol == null)
            return null;

        // For conversion operators, GetSemanticInfo returns the conversion operator as DeclaredSymbol when the
        // position is on the return type (since the operator's location is set to the return type location).
        // But for rename, we want to rename the type, not the operator. Check if we have a conversion operator
        // as the declared symbol AND we have a type in the referenced symbols - if so, prefer the type.
        if (symbol is IMethodSymbol { MethodKind: MethodKind.Conversion } &&
            semanticInfo.ReferencedSymbols.FirstOrDefault() is INamedTypeSymbol referencedType)
        {
            symbol = referencedType;
        }

        var definitionSymbol = await FindDefinitionSymbolAsync(symbol, document.Project.Solution, cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfNull(definitionSymbol);

        return definitionSymbol;
    }

    /// <summary>
    /// Given a symbol, finds the symbol that actually defines the name that we're using.
    /// </summary>
    public static async Task<ISymbol> FindDefinitionSymbolAsync(
        ISymbol symbol, Solution solution, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(symbol);
        Contract.ThrowIfNull(solution);

        // Make sure we're on the original source definition if we can be
        var foundSymbol = await SymbolFinder.FindSourceDefinitionAsync(
            symbol, solution, cancellationToken).ConfigureAwait(false);

        var bestSymbol = foundSymbol ?? symbol;
        symbol = bestSymbol;

        // If we're renaming a property, it might be a synthesized property for a method
        // backing field.
        if (symbol is IParameterSymbol { ContainingSymbol: IMethodSymbol { AssociatedSymbol: IPropertySymbol associatedParameterProperty } containingMethod })
        {
            var ordinal = containingMethod.Parameters.IndexOf((IParameterSymbol)symbol);
            if (ordinal < associatedParameterProperty.Parameters.Length)
                return associatedParameterProperty.Parameters[ordinal];
        }

        // if we are renaming a compiler generated delegate for an event, cascade to the event
        if (symbol is INamedTypeSymbol { IsImplicitlyDeclared: true, TypeKind: TypeKind.Delegate, AssociatedSymbol: not null } typeSymbol)
            return typeSymbol.AssociatedSymbol;

        // If we are renaming a constructor or destructor, we wish to rename the whole type
        if (symbol is IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor or MethodKind.Destructor })
            return symbol.ContainingType;

        // If we are renaming a backing field for a property, cascade to the property
        if (symbol is IFieldSymbol { IsImplicitlyDeclared: true, AssociatedSymbol: IPropertySymbol associatedProperty })
            return associatedProperty;

        // in case this is e.g. an overridden property accessor, we'll treat the property itself as the definition symbol
        var property = await TryGetPropertyFromAccessorOrAnOverrideAsync(bestSymbol, solution, cancellationToken).ConfigureAwait(false);

        return property ?? bestSymbol;
    }
}
