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
    protected static readonly SymbolDisplayFormat s_minimalTypeStyle = new(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    protected abstract TypeHint? TryGetTypeHint(
        SemanticModel semanticModel, SyntaxNode node,
        bool displayAllOverride,
        bool forImplicitVariableTypes,
        bool forLambdaParameterTypes,
        bool forImplicitObjectCreation,
        bool forCollectionExpressions,
        CancellationToken cancellationToken);

    public async Task AddInlineHintsAsync(
        Document document,
        TextSpan textSpan,
        InlineTypeHintsOptions options,
        SymbolDescriptionOptions displayOptions,
        bool displayAllOverride,
        ArrayBuilder<InlineHint> result,
        CancellationToken cancellationToken)
    {
        var enabledForTypes = options.EnabledForTypes;
        if (!enabledForTypes && !displayAllOverride)
            return;

        var forImplicitVariableTypes = enabledForTypes && options.ForImplicitVariableTypes;
        var forLambdaParameterTypes = enabledForTypes && options.ForLambdaParameterTypes;
        var forImplicitObjectCreation = enabledForTypes && options.ForImplicitObjectCreation;
        var forCollectionExpressions = enabledForTypes && options.ForCollectionExpressions;
        if (!forImplicitVariableTypes && !forLambdaParameterTypes && !forImplicitObjectCreation && !forCollectionExpressions && !displayAllOverride)
            return;

        var anonymousTypeService = document.GetRequiredLanguageService<IStructuralTypeDisplayService>();
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        foreach (var node in root.DescendantNodes(n => n.Span.IntersectsWith(textSpan)))
        {
            var hint = TryGetTypeHint(
                semanticModel, node,
                displayAllOverride,
                forImplicitVariableTypes,
                forLambdaParameterTypes,
                forImplicitObjectCreation,
                forCollectionExpressions,
                cancellationToken);
            if (hint is not var (type, span, textChange, prefix, suffix))
                continue;

            var spanStart = span.Start;

            // We get hints on *nodes* that intersect the passed in text span.  However, while the full node may
            // intersect the span, the positions of the all the sub-nodes in it that we make hints for (like the
            // positions of the arguments in an invocation) may not.  So, filter out any hints that aren't actually
            // in the span we care about here.
            if (!textSpan.IntersectsWith(spanStart))
                continue;

            using var _2 = ArrayBuilder<SymbolDisplayPart>.GetInstance(out var finalParts);
            finalParts.AddRange(prefix);

            // Try to get the minimal display string for the type.  Try to use it if it's actually shorter (it may not
            // be as we've setup ToDisplayParts to only show the type name, while ToMinimalDisplayParts may show the
            // full name of the type if the short name doesn't bind.  This will also help us use aliases if present.
            var minimalDisplayParts = type.ToMinimalDisplayParts(semanticModel, spanStart, s_minimalTypeStyle);
            var displayParts = type.ToDisplayParts(s_minimalTypeStyle);
            var preferredParts = minimalDisplayParts.Length <= displayParts.Length ? minimalDisplayParts : displayParts;
            AddParts(anonymousTypeService, finalParts, preferredParts, semanticModel, spanStart);

            // If we have nothing to show, then don't bother adding this hint.
            if (finalParts.All(p => string.IsNullOrWhiteSpace(p.ToString())))
                continue;

            finalParts.AddRange(suffix);
            var taggedText = finalParts.ToTaggedText();

            result.Add(new InlineHint(
                span, taggedText, textChange, ranking: InlineHintsConstants.TypeRanking,
                InlineHintHelpers.GetDescriptionFunction(spanStart, type, displayOptions)));
        }
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
