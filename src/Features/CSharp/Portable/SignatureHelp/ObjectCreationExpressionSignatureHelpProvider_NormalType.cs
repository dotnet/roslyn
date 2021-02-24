// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    internal partial class ObjectCreationExpressionSignatureHelpProvider
    {
        private static (IList<SignatureHelpItem> items, int? selectedItem) GetNormalTypeConstructors(
            Document document,
            BaseObjectCreationExpressionSyntax objectCreationExpression,
            SemanticModel semanticModel,
            IAnonymousTypeDisplayService anonymousTypeDisplayService,
            IDocumentationCommentFormattingService documentationCommentFormattingService,
            INamedTypeSymbol normalType,
            ISymbol within,
            CancellationToken cancellationToken)
        {
            var accessibleConstructors = normalType.InstanceConstructors
                                                   .WhereAsArray(c => c.IsAccessibleWithin(within))
                                                   .WhereAsArray(s => s.IsEditorBrowsable(document.ShouldHideAdvancedMembers(), semanticModel.Compilation))
                                                   .Sort(semanticModel, objectCreationExpression.SpanStart);

            var symbolInfo = semanticModel.GetSymbolInfo(objectCreationExpression, cancellationToken);
            var selectedItem = TryGetSelectedIndex(accessibleConstructors, symbolInfo.Symbol);

            var items = accessibleConstructors.SelectAsArray(c =>
                ConvertNormalTypeConstructor(c, objectCreationExpression, semanticModel, anonymousTypeDisplayService, documentationCommentFormattingService));

            return (items, selectedItem);
        }

        private static SignatureHelpItem ConvertNormalTypeConstructor(
            IMethodSymbol constructor,
            BaseObjectCreationExpressionSyntax objectCreationExpression,
            SemanticModel semanticModel,
            IAnonymousTypeDisplayService anonymousTypeDisplayService,
            IDocumentationCommentFormattingService documentationCommentFormattingService)
        {
            var position = objectCreationExpression.SpanStart;
            var item = CreateItem(
                constructor, semanticModel, position,
                anonymousTypeDisplayService,
                constructor.IsParams(),
                constructor.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                GetNormalTypePreambleParts(constructor, semanticModel, position),
                GetSeparatorParts(),
                GetNormalTypePostambleParts(),
                constructor.Parameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService)).ToList());

            return item;
        }

        private static IList<SymbolDisplayPart> GetNormalTypePreambleParts(
            IMethodSymbol method,
            SemanticModel semanticModel,
            int position)
        {
            var result = new List<SymbolDisplayPart>();

            result.AddRange(method.ContainingType.ToMinimalDisplayParts(semanticModel, position));
            result.Add(Punctuation(SyntaxKind.OpenParenToken));

            return result;
        }

        private static IList<SymbolDisplayPart> GetNormalTypePostambleParts()
        {
            return SpecializedCollections.SingletonList(
                Punctuation(SyntaxKind.CloseParenToken));
        }
    }
}
