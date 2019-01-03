// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    [ExportSignatureHelpProvider("InvocationExpressionSignatureHelpProvider", LanguageNames.CSharp), Shared]
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

            // get the candidate methods
            var methods = ImmutableArray<IMethodSymbol>.Empty; ;
            var expressionType = semanticModel.GetTypeInfo(invocationExpression.Expression, cancellationToken).Type as INamedTypeSymbol;
            var symbolDisplayService = document.GetLanguageService<ISymbolDisplayService>();
            var isDelegateType = expressionType != null && expressionType.TypeKind == TypeKind.Delegate;
            if (isDelegateType)
            {
                var delegateMethod = GetDelegateInvokeMethod(invocationExpression, semanticModel, within, expressionType, cancellationToken);
                if (delegateMethod != null)
                {
                    methods = ImmutableArray.Create(delegateMethod);
                }
            }
            else
            {
                methods = semanticModel.GetMemberGroup(invocationExpression.Expression, cancellationToken)
                               .OfType<IMethodSymbol>()
                               .ToImmutableArray()
                               .FilterToVisibleAndBrowsableSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation);

                methods = methods.Sort(symbolDisplayService, semanticModel, invocationExpression.SpanStart);
                methods = RemoveUnacceptable(methods, invocationExpression, within, semanticModel, cancellationToken);
            }

            if (!methods.Any())
            {
                return null;
            }

            // try to bind to the actual method
            var currentSymbol = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken).Symbol;

            var semanticFactsService = document.GetLanguageService<ISemanticFactsService>();
            var arguments = invocationExpression.ArgumentList.Arguments;
            var parameterIndex = -1;
            if (currentSymbol is null)
            {
                (currentSymbol, parameterIndex) = GuessCurrentSymbolAndParameter(arguments, methods, position,
                    semanticModel, semanticFactsService, cancellationToken);
            }
            else
            {
                // The compiler told us the correct overload, but we need to find out the parameter to highlight given cursor position
                _ = FindParameterIndexIfCompatibleMethod(arguments, (IMethodSymbol)currentSymbol, position, semanticModel, semanticFactsService, out parameterIndex);
            }

            // if the symbol could be bound, replace that item in the symbol list
            if (currentSymbol is IMethodSymbol matchedMethodSymbol && matchedMethodSymbol.IsGenericMethod)
            {
                methods = methods.SelectAsArray(m => matchedMethodSymbol.OriginalDefinition == m ? matchedMethodSymbol : m);
            }

            if (!methods.Any())
            {
                return null;
            }

            // present items and select
            var anonymousTypeDisplayService = document.GetLanguageService<IAnonymousTypeDisplayService>();
            var documentationCommentFormattingService = document.GetLanguageService<IDocumentationCommentFormattingService>();
            IList<SignatureHelpItem> items;
            int? selectedItem;
            if (isDelegateType)
            {
                items = methods.SelectAsArray(m =>
                    CreateItem(
                        m, semanticModel, position,
                        symbolDisplayService, anonymousTypeDisplayService,
                        isVariadic: false,
                        documentationFactory: null,
                        prefixParts: GetDelegateInvokePreambleParts(m, semanticModel, position),
                        separatorParts: GetSeparatorParts(),
                        suffixParts: GetDelegateInvokePostambleParts(),
                        parameters: GetDelegateInvokeParameters(m, semanticModel, position, documentationCommentFormattingService, cancellationToken)));
                selectedItem = 0;
            }
            else
            {
                (items, selectedItem) = GetMethodGroupItemsAndSelection(methods, currentSymbol,
                    invocationExpression, semanticModel, symbolDisplayService,
                    anonymousTypeDisplayService, documentationCommentFormattingService, cancellationToken);
            }

            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(invocationExpression.ArgumentList);
            return MakeSignatureHelpItems(items, textSpan, (IMethodSymbol)currentSymbol, parameterIndex, selectedItem, arguments, position);
        }
    }
}
