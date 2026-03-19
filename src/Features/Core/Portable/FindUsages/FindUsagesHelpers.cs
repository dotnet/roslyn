// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolMapping;

namespace Microsoft.CodeAnalysis.FindUsages;

internal static class FindUsagesHelpers
{
    public static string GetDisplayName(ISymbol symbol)
        => symbol.IsConstructor() ? symbol.ContainingType.Name : symbol.Name;

    public static Task<(ISymbol symbol, Project project)?> GetRelevantSymbolAndProjectAtPositionAsync(
        Document document, int position, CancellationToken cancellationToken)
        => GetRelevantSymbolAndProjectAtPositionAsync(document, position, preferPrimaryConstructor: false, cancellationToken);

    /// <summary>
    /// Common helper for both the synchronous and streaming versions of FAR. It returns the symbol we want to search
    /// for and the solution we should be searching.
    /// <para/> Note that the <see cref="Solution"/> returned may absolutely *not* be the same as
    /// <c>document.Project.Solution</c>.  This is because there may be symbol mapping involved (for example in
    /// Metadata-As-Source scenarios).
    /// </summary>
    /// <param name="preferPrimaryConstructor">Whether the named type or primary constructor should be preferred if the
    /// position is on a type-header fof a type declaration that has primary constructor parameters.</param>
    public static async Task<(ISymbol symbol, Project project)?> GetRelevantSymbolAndProjectAtPositionAsync(
        Document document, int position, bool preferPrimaryConstructor, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken).ConfigureAwait(false);
        if (symbol == null)
            return null;

        if (preferPrimaryConstructor && symbol is INamedTypeSymbol namedType)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var headerFacts = document.GetRequiredLanguageService<IHeaderFactsService>();
            var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
            if (headerFacts.IsOnTypeHeader(root, position, out _) &&
                semanticFacts.TryGetPrimaryConstructor(namedType, out var primaryConstructor))
            {
                symbol = primaryConstructor;
            }
        }

        // If this document is not in the primary workspace, we may want to search for results
        // in a solution different from the one we started in. Use the starting workspace's
        // ISymbolMappingService to get a context for searching in the proper solution.
        var mappingService = document.Project.Solution.Services.GetService<ISymbolMappingService>();

        var mapping = await mappingService.MapSymbolAsync(document, symbol, cancellationToken).ConfigureAwait(false);
        if (mapping == null)
            return null;

        return (mapping.Symbol, mapping.Project);
    }

    private static SymbolDisplayFormat GetFormat(ISymbol definition)
    {
        return definition.Kind == SymbolKind.Parameter
            ? s_parameterDefinitionFormat
            : s_definitionFormat;
    }

    private static readonly SymbolDisplayFormat s_definitionFormat =
        new(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
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
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private static readonly SymbolDisplayFormat s_parameterDefinitionFormat = s_definitionFormat
        .AddParameterOptions(SymbolDisplayParameterOptions.IncludeName);

    public static ImmutableArray<TaggedText> GetDisplayParts(ISymbol definition)
        // Range variables have to be specially handled as only ToMinimalDisplayParts can pass enough information 
        // to the compiler to properly display the pseudo-return-type they have.
        => (definition is IRangeVariableSymbol rangeVariable
            ? GetDisplayPartsForRangeVariable(rangeVariable)
            : definition.ToDisplayParts(GetFormat(definition))).ToTaggedText();

    private static ImmutableArray<SymbolDisplayPart> GetDisplayPartsForRangeVariable(IRangeVariableSymbol rangeVariable)
    {
        if (rangeVariable is
            {
                ContainingAssembly: ISourceAssemblySymbol sourceAssembly,
                Locations: [{ SourceTree: { } syntaxTree, SourceSpan: var sourceSpan }, ..],
            })
        {
            var compilation = sourceAssembly.Compilation;
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            return rangeVariable.ToMinimalDisplayParts(semanticModel, sourceSpan.Start);
        }

        return rangeVariable.ToDisplayParts(GetFormat(rangeVariable));
    }
}
