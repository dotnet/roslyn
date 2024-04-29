// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageService;

internal abstract partial class AbstractStructuralTypeDisplayService : IStructuralTypeDisplayService
{
    protected static readonly SymbolDisplayFormat s_minimalWithoutExpandedTuples = SymbolDisplayFormat.MinimallyQualifiedFormat.AddMiscellaneousOptions(
        SymbolDisplayMiscellaneousOptions.CollapseTupleTypes);

    private static readonly SymbolDisplayFormat s_delegateDisplay =
        s_minimalWithoutExpandedTuples.WithMemberOptions(s_minimalWithoutExpandedTuples.MemberOptions & ~SymbolDisplayMemberOptions.IncludeContainingType);

    protected abstract ISyntaxFacts SyntaxFactsService { get; }
    protected abstract ImmutableArray<SymbolDisplayPart> GetNormalAnonymousTypeParts(INamedTypeSymbol anonymousType, SemanticModel semanticModel, int position);

    public ImmutableArray<SymbolDisplayPart> GetAnonymousTypeParts(INamedTypeSymbol anonymousType, SemanticModel semanticModel, int position)
        => anonymousType.IsAnonymousDelegateType()
            ? GetDelegateAnonymousTypeParts(anonymousType, semanticModel, position)
            : GetNormalAnonymousTypeParts(anonymousType, semanticModel, position);

    private ImmutableArray<SymbolDisplayPart> GetDelegateAnonymousTypeParts(
        INamedTypeSymbol anonymousType,
        SemanticModel semanticModel,
        int position)
    {
        var invokeMethod = anonymousType.DelegateInvokeMethod ?? throw ExceptionUtilities.Unreachable();

        return
        [
            new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, symbol: null, SyntaxFactsService.GetText(SyntaxFactsService.SyntaxKinds.DelegateKeyword)),
            .. Space(),
            .. MassageDelegateParts(invokeMethod, invokeMethod.ToMinimalDisplayParts(semanticModel, position, s_delegateDisplay))
        ];
    }

    private static ImmutableArray<SymbolDisplayPart> MassageDelegateParts(
        IMethodSymbol invokeMethod,
        ImmutableArray<SymbolDisplayPart> parts)
    {
        using var _ = ArrayBuilder<SymbolDisplayPart>.GetInstance(out var result);

        // Ugly hack.  Remove the "Invoke" name the compiler layer adds to the parts.
        foreach (var part in parts)
        {
            if (!Equals(invokeMethod, part.Symbol))
                result.Add(part);
        }

        return result.ToImmutableAndClear();
    }

    public StructuralTypeDisplayInfo GetTypeDisplayInfo(
        ISymbol orderSymbol,
        ImmutableArray<INamedTypeSymbol> directStructuralTypeReferences,
        SemanticModel semanticModel,
        int position)
    {
        if (directStructuralTypeReferences.Length == 0)
        {
            return new StructuralTypeDisplayInfo(
                SpecializedCollections.EmptyDictionary<INamedTypeSymbol, string>(),
                SpecializedCollections.EmptyList<SymbolDisplayPart>());
        }

        var transitiveStructuralTypeReferences = GetTransitiveStructuralTypeReferences(directStructuralTypeReferences);
        transitiveStructuralTypeReferences = OrderStructuralTypes(transitiveStructuralTypeReferences, orderSymbol);

        IList<SymbolDisplayPart> typeParts = [];

        if (transitiveStructuralTypeReferences.Length > 0)
        {
            typeParts.Add(PlainText(FeaturesResources.Types_colon));
            typeParts.AddRange(LineBreak());
        }

        for (var i = 0; i < transitiveStructuralTypeReferences.Length; i++)
        {
            if (i != 0)
            {
                typeParts.AddRange(LineBreak());
            }

            var structuralType = transitiveStructuralTypeReferences[i];
            typeParts.AddRange(Space(count: 4));

            var kind = structuralType.GetSymbolDisplayPartKind();

            typeParts.Add(Part(kind, structuralType, structuralType.Name));
            typeParts.AddRange(Space());
            typeParts.Add(PlainText(FeaturesResources.is_));
            typeParts.AddRange(Space());

            if (structuralType.IsValueType)
            {
                typeParts.AddRange(structuralType.ToMinimalDisplayParts(semanticModel, position));
            }
            else
            {
                typeParts.AddRange(GetAnonymousTypeParts(structuralType, semanticModel, position));
            }
        }

        // Finally, assign a name to all the anonymous types.
        var structuralTypeToName = GenerateStructuralTypeNames(transitiveStructuralTypeReferences);
        typeParts = StructuralTypeDisplayInfo.ReplaceStructuralTypes(
            typeParts, structuralTypeToName, semanticModel, position);

        return new StructuralTypeDisplayInfo(structuralTypeToName, typeParts);
    }

    private static Dictionary<INamedTypeSymbol, string> GenerateStructuralTypeNames(
        IList<INamedTypeSymbol> anonymousTypes)
    {
        var current = 0;
        var anonymousTypeToName = new Dictionary<INamedTypeSymbol, string>();
        foreach (var type in anonymousTypes)
        {
            anonymousTypeToName[type] = GenerateStructuralTypeName(current);
            current++;
        }

        return anonymousTypeToName;
    }

    private static string GenerateStructuralTypeName(int current)
    {
        var c = (char)('a' + current);
        if (c is >= 'a' and <= 'z')
        {
            return "'" + c.ToString();
        }

        return "'" + current.ToString();
    }

    private static ImmutableArray<INamedTypeSymbol> OrderStructuralTypes(
        ImmutableArray<INamedTypeSymbol> structuralTypes,
        ISymbol symbol)
    {
        if (symbol is IMethodSymbol method)
        {
            return [.. structuralTypes.OrderBy(
                (n1, n2) =>
                {
                    var index1 = method.TypeArguments.IndexOf(n1);
                    var index2 = method.TypeArguments.IndexOf(n2);
                    index1 = index1 < 0 ? int.MaxValue : index1;
                    index2 = index2 < 0 ? int.MaxValue : index2;

                    return index1 - index2;
                })];
        }
        else if (symbol is IPropertySymbol property)
        {
            return [.. structuralTypes.OrderBy(
                (n1, n2) =>
                {
                    if (n1.Equals(property.ContainingType) && !n2.Equals(property.ContainingType))
                    {
                        return -1;
                    }
                    else if (!n1.Equals(property.ContainingType) && n2.Equals(property.ContainingType))
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                })];
        }

        return structuralTypes;
    }

    private static ImmutableArray<INamedTypeSymbol> GetTransitiveStructuralTypeReferences(
        ImmutableArray<INamedTypeSymbol> structuralTypes)
    {
        var transitiveReferences = new Dictionary<INamedTypeSymbol, (int order, int count)>();
        var visitor = new StructuralTypeCollectorVisitor(transitiveReferences);

        foreach (var type in structuralTypes)
            type.Accept(visitor);

        // If we have at least one tuple that showed up multiple times, then move *all* tuples to the 'Types:'
        // section to clean up the display.
        var hasAtLeastOneTupleWhichAppearsMultipleTimes = transitiveReferences.Any(kvp => kvp.Key.IsTupleType && kvp.Value.count >= 2);

        using var _ = ArrayBuilder<INamedTypeSymbol>.GetInstance(out var result);

        foreach (var (namedType, _) in transitiveReferences.OrderBy(kvp => kvp.Value.order))
        {
            if (namedType.IsTupleType && !hasAtLeastOneTupleWhichAppearsMultipleTimes)
                continue;

            result.Add(namedType);
        }

        return result.ToImmutableAndClear();
    }

    protected static IEnumerable<SymbolDisplayPart> LineBreak(int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            yield return new SymbolDisplayPart(SymbolDisplayPartKind.LineBreak, null, "\r\n");
        }
    }

    protected static SymbolDisplayPart PlainText(string text)
        => Part(SymbolDisplayPartKind.Text, text);

    private static SymbolDisplayPart Part(SymbolDisplayPartKind kind, string text)
        => Part(kind, null, text);

    private static SymbolDisplayPart Part(SymbolDisplayPartKind kind, ISymbol? symbol, string text)
        => new(kind, symbol, text);

    protected static IEnumerable<SymbolDisplayPart> Space(int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            yield return new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, " ");
        }
    }

    protected static SymbolDisplayPart Punctuation(string text)
        => Part(SymbolDisplayPartKind.Punctuation, text);

    protected static SymbolDisplayPart Keyword(string text)
        => Part(SymbolDisplayPartKind.Keyword, text);
}
