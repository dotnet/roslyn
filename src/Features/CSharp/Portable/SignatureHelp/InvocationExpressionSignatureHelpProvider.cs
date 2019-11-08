// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal partial class InvocationExpressionSignatureHelpProvider : AbstractOrdinaryMethodSignatureHelpProvider
    {
        [ImportingConstructor]
        public InvocationExpressionSignatureHelpProvider()
        {
        }

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
            var symbolDisplayService = document.GetLanguageService<ISymbolDisplayService>();
            var methodGroup = semanticModel.GetMemberGroup(invocationExpression.Expression, cancellationToken)
                                           .OfType<IMethodSymbol>()
                                           .ToImmutableArray()
                                           .FilterToVisibleAndBrowsableSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation);

            // try to bind to the actual method
            var symbolInfo = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken);

            // if the symbol could be bound, replace that item in the symbol list
            if (symbolInfo is
            {
                Symbol: IMethodSymbol { IsGenericMethod: true } matchedMethodSymbol
            }
)
            {
                methodGroup = methodGroup.SelectAsArray(m => Equals(matchedMethodSymbol.OriginalDefinition, m) ? matchedMethodSymbol : m);
            }

            methodGroup = methodGroup.Sort(
                symbolDisplayService, semanticModel, invocationExpression.SpanStart);


            var anonymousTypeDisplayService = document.GetLanguageService<IAnonymousTypeDisplayService>();
            var documentationCommentFormattingService = document.GetLanguageService<IDocumentationCommentFormattingService>();

            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(invocationExpression.ArgumentList);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            if (methodGroup.Any())
            {
                var (items, selectedItem) = GetMethodGroupItemsAndSelection(
                    document, invocationExpression, semanticModel, within, methodGroup, symbolInfo, cancellationToken);

                return CreateSignatureHelpItems(
                    items,
                    textSpan,
                    GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken),
                    selectedItem);
            }
            else if (semanticModel.GetTypeInfo(invocationExpression.Expression, cancellationToken).Type is INamedTypeSymbol expressionType && expressionType.TypeKind == TypeKind.Delegate)
            {
                var items = GetDelegateInvokeItems(invocationExpression, semanticModel, symbolDisplayService, anonymousTypeDisplayService,
                    documentationCommentFormattingService, within, expressionType, out var selectedItem, cancellationToken);

                return CreateSignatureHelpItems(items, textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItem);
            }
            else
            {
                return null;
            }
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
