// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp.Providers
{
    internal partial class InvocationExpressionSignatureHelpProvider : AbstractCSharpSignatureHelpProvider
    {
        public override bool IsTriggerCharacter(char ch)
        {
            return ch == '(' || ch == ',';
        }

        public override bool IsRetriggerCharacter(char ch)
        {
            return ch == ')';
        }

        private bool TryGetInvocationExpression(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerKind triggerReason, CancellationToken cancellationToken, out InvocationExpressionSyntax expression)
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

        protected override async Task ProvideSignaturesWorkerAsync(SignatureContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var trigger = context.Trigger;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            if (!TryGetInvocationExpression(root, position, document.GetLanguageService<ISyntaxFactsService>(), trigger.Kind, cancellationToken, out var invocationExpression))
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelForNodeAsync(invocationExpression, cancellationToken).ConfigureAwait(false);
            var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
            if (within == null)
            {
                return;
            }

            // get the regular signature help items
            var symbolDisplayService = document.Project.LanguageServices.GetService<ISymbolDisplayService>();
            var methodGroup = semanticModel.GetMemberGroup(invocationExpression.Expression, cancellationToken)
                                                       .OfType<IMethodSymbol>()
                                                       .ToImmutableArray()
                                                       .FilterToVisibleAndBrowsableSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation);
            // try to bind to the actual method
            var symbolInfo = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken);
            var matchedMethodSymbol = symbolInfo.Symbol as IMethodSymbol;

            // if the symbol could be bound, replace that item in the symbol list
            if (matchedMethodSymbol != null && matchedMethodSymbol.IsGenericMethod)
            {
                methodGroup = methodGroup.SelectAsArray(m => matchedMethodSymbol.OriginalDefinition == m ? matchedMethodSymbol : m);
            }

            methodGroup = methodGroup.Sort(
                symbolDisplayService, semanticModel, invocationExpression.SpanStart);

            var expressionType = semanticModel.GetTypeInfo(invocationExpression.Expression, cancellationToken).Type as INamedTypeSymbol;

            var anonymousTypeDisplayService = document.Project.LanguageServices.GetService<IAnonymousTypeDisplayService>();

            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(invocationExpression.ArgumentList);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            if (methodGroup.Any())
            {
                var items = GetMethodGroupItems(invocationExpression, semanticModel, symbolDisplayService, anonymousTypeDisplayService, within, methodGroup, cancellationToken);
                if (items != null)
                {
                    context.AddItems(items);
                    context.SetSpan(textSpan);
                    context.SetState(GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken));
                }
            }
            else if (expressionType?.TypeKind == TypeKind.Delegate)
            {
                var items = GetDelegateInvokeItems(invocationExpression, semanticModel, symbolDisplayService, anonymousTypeDisplayService, within, expressionType, cancellationToken);
                if (items != null)
                {
                    context.AddItems(items);
                    context.SetSpan(textSpan);
                    context.SetState(GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken));
                }
            }
        }

        protected override SignatureHelpState GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
        {
            if (TryGetInvocationExpression(
                    root,
                    position,
                    syntaxFacts,
                    SignatureHelpTriggerKind.Other,
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
