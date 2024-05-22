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
            symbols = symbols.WhereAsArray(s => s.Kind != SymbolKind.Alias);
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
            return renameLocations.Select(l => solution.GetRequiredDocument(l.DocumentId));
        }
        else
        {
            var documentsOfRenameSymbolDeclaration = symbol.Locations.Where(l => l.IsInSource).Select(l => solution.GetRequiredDocument(l.SourceTree!));
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
        {
            return ((IMethodSymbol)symbol).AssociatedSymbol;
        }

        if (symbol.IsOverride && symbol.GetOverriddenMember() != null)
        {
            var originalSourceSymbol = await SymbolFinder.FindSourceDefinitionAsync(
                symbol.GetOverriddenMember(), solution, cancellationToken).ConfigureAwait(false);

            if (originalSourceSymbol != null)
            {
                return await TryGetPropertyFromAccessorOrAnOverrideAsync(originalSourceSymbol, solution, cancellationToken).ConfigureAwait(false);
            }
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
                {
                    return propertyAccessorOrAnOverride;
                }
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
        var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (symbol == null)
            return null;

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
            if (methodSymbol.MethodKind is MethodKind.Constructor or
                MethodKind.StaticConstructor or
                MethodKind.Destructor)
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
        var property = await RenameUtilities.TryGetPropertyFromAccessorOrAnOverrideAsync(bestSymbol, solution, cancellationToken).ConfigureAwait(false);

        return property ?? bestSymbol;
    }
}
