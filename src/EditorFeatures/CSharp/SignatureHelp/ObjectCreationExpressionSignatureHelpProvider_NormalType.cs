// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SignatureHelp
{
    internal partial class ObjectCreationExpressionSignatureHelpProvider
    {
        private IEnumerable<SignatureHelpItem> GetNormalTypeConstructors(
            Document document,
            ObjectCreationExpressionSyntax objectCreationExpression,
            SemanticModel semanticModel,
            ISymbolDisplayService symbolDisplayService,
            IAnonymousTypeDisplayService anonymousTypeDisplayService,
            IDocumentationCommentFormattingService documentationCommentFormattingService,
            INamedTypeSymbol normalType,
            ISymbol within,
            CancellationToken cancellationToken)
        {
            var accessibleConstructors = normalType.InstanceConstructors
                                                   .Where(c => c.IsAccessibleWithin(within))
                                                   .Where(s => s.IsEditorBrowsable(document.ShouldHideAdvancedMembers(), semanticModel.Compilation))
                                                   .Sort(symbolDisplayService, semanticModel, objectCreationExpression.SpanStart);

            if (!accessibleConstructors.Any())
            {
                return null;
            }

            return accessibleConstructors.Select(c =>
                ConvertNormalTypeConstructor(c, objectCreationExpression, semanticModel, symbolDisplayService, anonymousTypeDisplayService, documentationCommentFormattingService, cancellationToken));
        }

        private SignatureHelpItem ConvertNormalTypeConstructor(
            IMethodSymbol constructor,
            ObjectCreationExpressionSyntax objectCreationExpression,
            SemanticModel semanticModel,
            ISymbolDisplayService symbolDisplayService,
            IAnonymousTypeDisplayService anonymousTypeDisplayService,
            IDocumentationCommentFormattingService documentationCommentFormattingService,
            CancellationToken cancellationToken)
        {
            var position = objectCreationExpression.SpanStart;
            var item = CreateItem(
                constructor, semanticModel, position,
                symbolDisplayService, anonymousTypeDisplayService,
                constructor.IsParams(),
                constructor.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                GetNormalTypePreambleParts(constructor, semanticModel, position),
                GetSeparatorParts(),
                GetNormalTypePostambleParts(constructor),
                constructor.Parameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService, cancellationToken)));

            return item;
        }

        private IEnumerable<SymbolDisplayPart> GetNormalTypePreambleParts(
            IMethodSymbol method,
            SemanticModel semanticModel,
            int position)
        {
            var result = new List<SymbolDisplayPart>();

            result.AddRange(method.ContainingType.ToMinimalDisplayParts(semanticModel, position));
            result.Add(Punctuation(SyntaxKind.OpenParenToken));

            return result;
        }

        private IEnumerable<SymbolDisplayPart> GetNormalTypePostambleParts(IMethodSymbol method)
        {
            yield return Punctuation(SyntaxKind.CloseParenToken);
        }
    }
}
