// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp.Providers
{
    internal partial class ObjectCreationExpressionSignatureHelpProvider
    {
        private IList<SignatureHelpItem> GetNormalTypeConstructors(
            Document document,
            ObjectCreationExpressionSyntax objectCreationExpression,
            SemanticModel semanticModel,
            ISymbolDisplayService symbolDisplayService,
            IAnonymousTypeDisplayService anonymousTypeDisplayService,
            INamedTypeSymbol normalType,
            ISymbol within,
            CancellationToken cancellationToken)
        {
            var accessibleConstructors = normalType
                .InstanceConstructors
                .Where(c => c.IsAccessibleWithin(within))
                .Where(s => s.IsEditorBrowsable(document.ShouldHideAdvancedMembers(), semanticModel.Compilation))
                .ToImmutableArrayOrEmpty()
                .Sort(symbolDisplayService, semanticModel, objectCreationExpression.SpanStart);

            return accessibleConstructors.Select(c =>
                ConvertNormalTypeConstructor(c, objectCreationExpression, semanticModel, symbolDisplayService, anonymousTypeDisplayService, cancellationToken)).ToList();
        }

        private SignatureHelpItem ConvertNormalTypeConstructor(
            IMethodSymbol constructor,
            ObjectCreationExpressionSyntax objectCreationExpression,
            SemanticModel semanticModel,
            ISymbolDisplayService symbolDisplayService,
            IAnonymousTypeDisplayService anonymousTypeDisplayService,
            CancellationToken cancellationToken)
        {
            var position = objectCreationExpression.SpanStart;
            var item = CreateItem(
                constructor, semanticModel, position,
                symbolDisplayService, anonymousTypeDisplayService,
                constructor.IsParams(),
                GetNormalTypePreambleParts(constructor, semanticModel, position),
                GetSeparatorParts(),
                GetNormalTypePostambleParts(constructor),
                constructor.Parameters.Select(p => Convert(p, semanticModel, position, cancellationToken)).ToList());

            return item;
        }

        private IList<SymbolDisplayPart> GetNormalTypePreambleParts(
            IMethodSymbol method,
            SemanticModel semanticModel,
            int position)
        {
            var result = new List<SymbolDisplayPart>();

            result.AddRange(method.ContainingType.ToMinimalDisplayParts(semanticModel, position));
            result.Add(Punctuation(SyntaxKind.OpenParenToken));

            return result;
        }

        private IList<SymbolDisplayPart> GetNormalTypePostambleParts(IMethodSymbol method)
        {
            return SpecializedCollections.SingletonList(
                Punctuation(SyntaxKind.CloseParenToken));
        }
    }
}