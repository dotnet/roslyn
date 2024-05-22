// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp;

internal partial class InvocationExpressionSignatureHelpProviderBase
{
    private static IMethodSymbol? GetDelegateInvokeMethod(
        InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, ISymbol within,
        INamedTypeSymbol delegateType, CancellationToken cancellationToken)
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

        return invokeMethod;
    }

    private static IList<SignatureHelpItem> GetDelegateOrFunctionPointerInvokeItems(InvocationExpressionSyntax invocationExpression, IMethodSymbol invokeMethod, SemanticModel semanticModel, IStructuralTypeDisplayService structuralTypeDisplayService, IDocumentationCommentFormattingService documentationCommentFormattingService, out int? selectedItem, CancellationToken cancellationToken)
    {
        var position = invocationExpression.SpanStart;
        var item = CreateItem(
            invokeMethod, semanticModel, position,
            structuralTypeDisplayService,
            isVariadic: invokeMethod.IsParams(),
            documentationFactory: null,
            prefixParts: GetDelegateOrFunctionPointerInvokePreambleParts(invokeMethod, semanticModel, position),
            separatorParts: GetSeparatorParts(),
            suffixParts: GetDelegateOrFunctionPointerInvokePostambleParts(),
            parameters: GetDelegateOrFunctionPointerInvokeParameters(invokeMethod, semanticModel, position, documentationCommentFormattingService, cancellationToken));

        // Since we're returning a single item, we can selected it as the "best one".
        selectedItem = 0;

        return [item];
    }

    private static IList<SymbolDisplayPart> GetDelegateOrFunctionPointerInvokePreambleParts(IMethodSymbol invokeMethod, SemanticModel semanticModel, int position)
    {
        var displayParts = new List<SymbolDisplayPart>();
        displayParts.AddRange(invokeMethod.ReturnType.ToMinimalDisplayParts(semanticModel, position));
        displayParts.Add(Space());

        if (invokeMethod.MethodKind == MethodKind.FunctionPointerSignature)
        {
            displayParts.Add(Keyword(SyntaxKind.DelegateKeyword));
            displayParts.Add(Operator(SyntaxKind.AsteriskToken));
        }
        else
        {
            displayParts.AddRange(invokeMethod.ContainingType.ToMinimalDisplayParts(semanticModel, position));
        }

        displayParts.Add(Punctuation(SyntaxKind.OpenParenToken));

        return displayParts;
    }

    private static IList<SignatureHelpSymbolParameter> GetDelegateOrFunctionPointerInvokeParameters(
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

    private static IList<SymbolDisplayPart> GetDelegateOrFunctionPointerInvokePostambleParts()
        => [Punctuation(SyntaxKind.CloseParenToken)];
}
