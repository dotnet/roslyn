// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp.Providers
{
    internal partial class InvocationExpressionSignatureHelpProvider
    {
        private IList<SignatureHelpItem> GetDelegateInvokeItems(
            InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, ISymbolDisplayService symbolDisplayService, IAnonymousTypeDisplayService anonymousTypeDisplayService,
            ISymbol within, INamedTypeSymbol delegateType, CancellationToken cancellationToken)
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
                prefixParts: GetDelegateInvokePreambleParts(invokeMethod, semanticModel, position),
                separatorParts: GetSeparatorParts(),
                suffixParts: GetDelegateInvokePostambleParts(),
                parameters: GetDelegateInvokeParameters(invokeMethod, semanticModel, position, cancellationToken))
                .WithSymbol(null); // will cause documentation to be empty.

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

        private IList<CommonParameterData> GetDelegateInvokeParameters(
            IMethodSymbol invokeMethod, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            var result = new List<CommonParameterData>();

            foreach (var parameter in invokeMethod.Parameters)
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.Add(new CommonParameterData(
                    parameter.Name,
                    parameter.IsOptional,
                    parameter,
                    position,
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