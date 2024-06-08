// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InlineHints;

internal abstract class AbstractInlineTypeHintsService : IInlineTypeHintsService
{
    protected static readonly SymbolDisplayFormat s_minimalTypeStyle = new SymbolDisplayFormat(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    protected abstract TypeHint? TryGetTypeHint(
        SemanticModel semanticModel, SyntaxNode node,
        bool displayAllOverride,
        bool forImplicitVariableTypes,
        bool forLambdaParameterTypes,
        bool forImplicitObjectCreation,
        CancellationToken cancellationToken);

    public async Task<ImmutableArray<InlineHint>> GetInlineHintsAsync(
        Document document,
        TextSpan textSpan,
        InlineTypeHintsOptions options,
        SymbolDescriptionOptions displayOptions,
        bool displayAllOverride,
        CancellationToken cancellationToken)
    {
        var enabledForTypes = options.EnabledForTypes;
        if (!enabledForTypes && !displayAllOverride)
            return [];

        var forImplicitVariableTypes = enabledForTypes && options.ForImplicitVariableTypes;
        var forLambdaParameterTypes = enabledForTypes && options.ForLambdaParameterTypes;
        var forImplicitObjectCreation = enabledForTypes && options.ForImplicitObjectCreation;
        if (!forImplicitVariableTypes && !forLambdaParameterTypes && !forImplicitObjectCreation && !displayAllOverride)
            return [];

        var anonymousTypeService = document.GetRequiredLanguageService<IStructuralTypeDisplayService>();
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        using var _1 = ArrayBuilder<InlineHint>.GetInstance(out var result);

        foreach (var node in root.DescendantNodes(n => n.Span.IntersectsWith(textSpan)))
        {
            var hintOpt = TryGetTypeHint(
                semanticModel, node,
                displayAllOverride,
                forImplicitVariableTypes,
                forLambdaParameterTypes,
                forImplicitObjectCreation,
                cancellationToken);
            if (hintOpt == null)
                continue;

            var (type, span, textChange, prefix, suffix) = hintOpt.Value;

            using var _2 = ArrayBuilder<SymbolDisplayPart>.GetInstance(out var finalParts);
            finalParts.AddRange(prefix);

            var parts = type.ToDisplayParts(s_minimalTypeStyle);
            AddParts(anonymousTypeService, finalParts, parts, semanticModel, span.Start);

            // If we have nothing to show, then don't bother adding this hint.
            if (finalParts.All(p => string.IsNullOrWhiteSpace(p.ToString())))
                continue;

            finalParts.AddRange(suffix);
            var taggedText = finalParts.ToTaggedText();

            result.Add(new InlineHint(
                span, taggedText, textChange, ranking: InlineHintsConstants.TypeRanking,
                InlineHintHelpers.GetDescriptionFunction(span.Start, type.GetSymbolKey(cancellationToken: cancellationToken), displayOptions)));
        }

        return result.ToImmutableAndClear();
    }

    private static void AddParts(
        IStructuralTypeDisplayService anonymousTypeService,
        ArrayBuilder<SymbolDisplayPart> finalParts,
        ImmutableArray<SymbolDisplayPart> parts,
        SemanticModel semanticModel,
        int position,
        HashSet<INamedTypeSymbol>? seenSymbols = null)
    {
        seenSymbols ??= [];

        foreach (var part in parts)
        {
            if (part.Symbol is INamedTypeSymbol { IsAnonymousType: true } anonymousType)
            {
                if (seenSymbols.Add(anonymousType))
                {
                    var anonymousParts = anonymousTypeService.GetAnonymousTypeParts(anonymousType, semanticModel, position);
                    AddParts(anonymousTypeService, finalParts, anonymousParts, semanticModel, position, seenSymbols);
                    seenSymbols.Remove(anonymousType);
                }
                else
                {
                    finalParts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Text, symbol: null, "..."));
                }
            }
            else
            {
                finalParts.Add(part);
            }
        }
    }
}
