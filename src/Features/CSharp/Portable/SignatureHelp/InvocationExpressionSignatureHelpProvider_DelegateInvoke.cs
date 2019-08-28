// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    internal partial class InvocationExpressionSignatureHelpProvider
    {
        private IList<SignatureHelpItem> GetDelegateInvokeItems(
            InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, ISymbolDisplayService symbolDisplayService, IAnonymousTypeDisplayService anonymousTypeDisplayService,
            IDocumentationCommentFormattingService documentationCommentFormattingService, ISymbol within, INamedTypeSymbol delegateType, out int? selectedItem, CancellationToken cancellationToken)
        {
            selectedItem = null;
            var invokeMethod = delegateType.DelegateInvokeMethod;
            if (invokeMethod == null)
            {
                return null;
            }

            // Events can only be invoked directly from the class they were declared in.
            var expressionSymbol = semanticModel.GetSymbolInfo(invocationExpression.Expression, cancellationToken).GetAnySymbol();
            if (expressionSymbol.IsKind(SymbolKind.Event) &&
                !expressionSymbol.ContainingType.OriginalDefinition.Equals(within.OriginalDefinition))
            {
                return null;
            }

            var position = invocationExpression.SpanStart;
            var item = CreateItem(
                invokeMethod, semanticModel, position,
                symbolDisplayService, anonymousTypeDisplayService,
                isVariadic: invokeMethod.IsParams(),
                documentationFactory: null,
                prefixParts: GetDelegateInvokePreambleParts(invokeMethod, semanticModel, position),
                separatorParts: GetSeparatorParts(),
                suffixParts: GetDelegateInvokePostambleParts(),
                parameters: GetDelegateInvokeParameters(invokeMethod, semanticModel, position, documentationCommentFormattingService, cancellationToken));

            // Since we're returning a single item, we can selected it as the "best one".
            selectedItem = 0;

            return SpecializedCollections.SingletonList(item);
        }

        private IList<SymbolDisplayPart> GetDelegateInvokePreambleParts(IMethodSymbol invokeMethod, SemanticModel semanticModel, int position)
        {
            var displayParts = new List<SymbolDisplayPart>();
            displayParts.AddRange(invokeMethod.ReturnType.ToMinimalDisplayParts(semanticModel, position));
            displayParts.Add(Space());
            displayParts.AddRange(invokeMethod.ContainingType.ToMinimalDisplayParts(semanticModel, position));
            displayParts.Add(Punctuation(SyntaxKind.OpenParenToken));

            return displayParts;
        }

        private IList<SignatureHelpSymbolParameter> GetDelegateInvokeParameters(
            IMethodSymbol invokeMethod, SemanticModel semanticModel, int position, IDocumentationCommentFormattingService formattingService, CancellationToken cancellationToken)
        {
            var result = new List<SignatureHelpSymbolParameter>();

            foreach (var parameter in invokeMethod.Parameters)
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.Add(new SignatureHelpSymbolParameter(
                    parameter.Name,
                    parameter.IsOptional,
                    parameter.GetDocumentationPartsFactory(semanticModel, position, formattingService),
                    parameter.ToMinimalDisplayParts(semanticModel, position)));
            }

            return result;
        }

        private IList<SymbolDisplayPart> GetDelegateInvokePostambleParts()
        {
            return SpecializedCollections.SingletonList(
                Punctuation(SyntaxKind.CloseParenToken));
        }
    }
}
