// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InlineHints
{
    internal abstract class AbstractInlineTypeHintsService : IInlineTypeHintsService
    {
        private static readonly SymbolDisplayFormat s_minimalTypeStyle = new SymbolDisplayFormat(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        protected abstract (ITypeSymbol type, int position)? TryGetTypeHint(
            SemanticModel semanticModel, SyntaxNode node,
            bool forImplicitVariableTypes,
            bool forLambdaParameterTypes,
            CancellationToken cancellationToken);

        public async Task<ImmutableArray<InlineTypeHint>> GetInlineTypeHintsAsync(
            Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var displayAllOverride = options.GetOption(InlineHintsOptions.DisplayAllOverride);
            var enabledForTypes = displayAllOverride || options.GetOption(InlineHintsOptions.EnabledForTypes);
            if (!enabledForTypes)
                return ImmutableArray<InlineTypeHint>.Empty;

            var forImplicitVariableTypes = displayAllOverride || options.GetOption(InlineHintsOptions.ForImplicitVariableTypes);
            var forLambdaParameterTypes = displayAllOverride || options.GetOption(InlineHintsOptions.ForLambdaParameterTypes);
            if (!forImplicitVariableTypes && !forLambdaParameterTypes)
                return ImmutableArray<InlineTypeHint>.Empty;

            var anonymousTypeService = document.GetRequiredLanguageService<IAnonymousTypeDisplayService>();
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            using var _1 = ArrayBuilder<InlineTypeHint>.GetInstance(out var result);

            foreach (var node in root.DescendantNodes(n => n.Span.IntersectsWith(textSpan)))
            {
                var hintOpt = TryGetTypeHint(
                    semanticModel, node,
                    forImplicitVariableTypes,
                    forLambdaParameterTypes, cancellationToken);
                if (hintOpt == null)
                    continue;

                var (type, position) = hintOpt.Value;

                using var _2 = ArrayBuilder<SymbolDisplayPart>.GetInstance(out var finalParts);
                var parts = type.ToDisplayParts(s_minimalTypeStyle);

                AddParts(anonymousTypeService, finalParts, parts, semanticModel, position);
                result.Add(new InlineTypeHint(position, finalParts.ToImmutable(), type.GetSymbolKey(cancellationToken)));
            }

            return result.ToImmutable();
        }

        private void AddParts(
            IAnonymousTypeDisplayService anonymousTypeService,
            ArrayBuilder<SymbolDisplayPart> finalParts,
            ImmutableArray<SymbolDisplayPart> parts,
            SemanticModel semanticModel,
            int position,
            HashSet<INamedTypeSymbol>? seenSymbols = null)
        {
            seenSymbols ??= new();

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
}
