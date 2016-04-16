// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SignatureHelp
{
    internal partial class InvocationExpressionSignatureHelpProvider
    {
        private IEnumerable<SignatureHelpItem> GetDelegateInvokeItems(
            InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, ISymbolDisplayService symbolDisplayService, IAnonymousTypeDisplayService anonymousTypeDisplayService,
            IDocumentationCommentFormattingService documentationCommentFormattingService, ISymbol within, INamedTypeSymbol delegateType, CancellationToken cancellationToken)
        {
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

            return SpecializedCollections.SingletonEnumerable(item);
        }

        private IEnumerable<SymbolDisplayPart> GetDelegateInvokePreambleParts(IMethodSymbol invokeMethod, SemanticModel semanticModel, int position)
        {
            var displayParts = new List<SymbolDisplayPart>();
            displayParts.AddRange(invokeMethod.ReturnType.ToMinimalDisplayParts(semanticModel, position));
            displayParts.Add(Space());
            displayParts.AddRange(invokeMethod.ContainingType.ToMinimalDisplayParts(semanticModel, position));
            displayParts.Add(Punctuation(SyntaxKind.OpenParenToken));

            return displayParts;
        }

        private IEnumerable<SignatureHelpParameter> GetDelegateInvokeParameters(
            IMethodSymbol invokeMethod, SemanticModel semanticModel, int position, IDocumentationCommentFormattingService formattingService, CancellationToken cancellationToken)
        {
            foreach (var parameter in invokeMethod.Parameters)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new SignatureHelpParameter(
                    parameter.Name,
                    parameter.IsOptional,
                    parameter.GetDocumentationPartsFactory(semanticModel, position, formattingService),
                    parameter.ToMinimalDisplayParts(semanticModel, position));
            }
        }

        private IEnumerable<SymbolDisplayPart> GetDelegateInvokePostambleParts()
        {
            yield return Punctuation(SyntaxKind.CloseParenToken);
        }
    }
}
