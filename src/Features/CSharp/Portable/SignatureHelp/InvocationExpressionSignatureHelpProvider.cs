﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    [ExportSignatureHelpProvider("InvocationExpressionSignatureHelpProvider", LanguageNames.CSharp), Shared]
    internal sealed class InvocationExpressionSignatureHelpProvider : InvocationExpressionSignatureHelpProviderBase
    {
        [ImportingConstructor]
        public InvocationExpressionSignatureHelpProvider()
        {
        }
    }

    internal partial class InvocationExpressionSignatureHelpProviderBase : AbstractOrdinaryMethodSignatureHelpProvider
    {
        public override bool IsTriggerCharacter(char ch)
        {
            return ch == '(' || ch == ',';
        }

        public override bool IsRetriggerCharacter(char ch)
        {
            return ch == ')';
        }

        private bool TryGetInvocationExpression(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out InvocationExpressionSyntax expression)
        {
            if (!CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out expression))
            {
                return false;
            }

            return expression.ArgumentList != null;
        }

        private bool IsTriggerToken(SyntaxToken token)
        {
            return SignatureHelpUtilities.IsTriggerParenOrComma<InvocationExpressionSyntax>(token, IsTriggerCharacter);
        }

        private static bool IsArgumentListToken(InvocationExpressionSyntax expression, SyntaxToken token)
        {
            return expression.ArgumentList.Span.Contains(token.SpanStart) &&
                token != expression.ArgumentList.CloseParenToken;
        }

        protected override async Task<SignatureHelpItems> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (!TryGetInvocationExpression(root, position, document.GetLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out var invocationExpression))
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelForNodeAsync(invocationExpression, cancellationToken).ConfigureAwait(false);
            var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
            if (within == null)
            {
                return null;
            }

            // get the regular signature help items
            var symbolDisplayService = document.Project.LanguageServices.GetRequiredService<ISymbolDisplayService>();
            var methodGroup = semanticModel.GetMemberGroup(invocationExpression.Expression, cancellationToken)
                                           .OfType<IMethodSymbol>()
                                           .ToImmutableArray()
                                           .FilterToVisibleAndBrowsableSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation);

            // try to bind to the actual method
            var symbolInfo = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken);

            // if the symbol could be bound, replace that item in the symbol list
            if (symbolInfo.Symbol is IMethodSymbol matchedMethodSymbol && matchedMethodSymbol.IsGenericMethod)
            {
                methodGroup = methodGroup.SelectAsArray(m => Equals(matchedMethodSymbol.OriginalDefinition, m) ? matchedMethodSymbol : m);
            }

            methodGroup = methodGroup.Sort(
                symbolDisplayService, semanticModel, invocationExpression.SpanStart);

            var anonymousTypeDisplayService = document.Project.LanguageServices.GetService<IAnonymousTypeDisplayService>();
            var documentationCommentFormattingService = document.Project.LanguageServices.GetService<IDocumentationCommentFormattingService>();

            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(invocationExpression.ArgumentList);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            if (methodGroup.Any())
            {
                var accessibleMethods = GetAccessibleMethods(invocationExpression, semanticModel, within, methodGroup, cancellationToken);
                var (items, selectedItem) = await GetMethodGroupItemsAndSelectionAsync(accessibleMethods, document, invocationExpression, semanticModel, symbolInfo, cancellationToken).ConfigureAwait(false);

                return CreateSignatureHelpItems(
                    items,
                    textSpan,
                    GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken),
                    selectedItem);
            }

            if (semanticModel.GetTypeInfo(invocationExpression.Expression, cancellationToken).Type is INamedTypeSymbol expressionType && expressionType.TypeKind == TypeKind.Delegate)
            {
                var items = GetDelegateInvokeItems(invocationExpression, semanticModel, symbolDisplayService, anonymousTypeDisplayService,
                    documentationCommentFormattingService, within, expressionType, out var selectedItem, cancellationToken);

                return CreateSignatureHelpItems(items, textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItem);
            }

            return null;
        }

        public override SignatureHelpState GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
        {
            if (TryGetInvocationExpression(
                    root,
                    position,
                    syntaxFacts,
                    SignatureHelpTriggerReason.InvokeSignatureHelpCommand,
                    cancellationToken,
                    out var expression) &&
                currentSpan.Start == SignatureHelpUtilities.GetSignatureHelpSpan(expression.ArgumentList).Start)
            {
                return SignatureHelpUtilities.GetSignatureHelpState(expression.ArgumentList, position);
            }

            return null;
        }
    }
}
