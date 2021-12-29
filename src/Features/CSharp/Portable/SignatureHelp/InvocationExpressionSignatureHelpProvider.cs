// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host.Mef;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InvocationExpressionSignatureHelpProvider()
        {
        }
    }

    internal partial class InvocationExpressionSignatureHelpProviderBase : AbstractOrdinaryMethodSignatureHelpProvider
    {
        public override bool IsTriggerCharacter(char ch)
            => ch is '(' or ',';

        public override bool IsRetriggerCharacter(char ch)
            => ch == ')';

        private bool TryGetInvocationExpression(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out InvocationExpressionSyntax expression)
        {
            if (!CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out expression))
            {
                return false;
            }

            return expression.ArgumentList != null;
        }

        private bool IsTriggerToken(SyntaxToken token)
            => SignatureHelpUtilities.IsTriggerParenOrComma<InvocationExpressionSyntax>(token, IsTriggerCharacter);

        private static bool IsArgumentListToken(InvocationExpressionSyntax expression, SyntaxToken token)
        {
            return expression.ArgumentList.Span.Contains(token.SpanStart) &&
                token != expression.ArgumentList.CloseParenToken;
        }

        protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, SignatureHelpOptions options, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (!TryGetInvocationExpression(root, position, document.GetRequiredLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out var invocationExpression))
            {
                return null;
            }

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(invocationExpression, cancellationToken).ConfigureAwait(false);
            var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
            if (within == null)
            {
                return null;
            }

            var structuralTypeDisplayService = document.Project.LanguageServices.GetRequiredService<IStructuralTypeDisplayService>();
            var documentationCommentFormattingService = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(invocationExpression.ArgumentList);
            var invokedType = semanticModel.GetTypeInfo(invocationExpression.Expression, cancellationToken).Type;

            if (invokedType is INamedTypeSymbol expressionType && expressionType.TypeKind == TypeKind.Delegate)
            {
                var items = GetDelegateInvokeItems(invocationExpression, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService,
                    within, expressionType, out var selectedItem, cancellationToken);

                return CreateSignatureHelpItems(items, textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItem);
            }
            else if (invokedType is IFunctionPointerTypeSymbol functionPointerType)
            {
                var items = GetFunctionPointerInvokeItems(invocationExpression, semanticModel, structuralTypeDisplayService,
                    documentationCommentFormattingService, functionPointerType, out var selectedItem, cancellationToken);

                return CreateSignatureHelpItems(items, textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItem);
            }
            else
            {

                // get the candidate methods
                var symbolDisplayService = document.GetLanguageService<ISymbolDisplayService>();
                IList<SignatureHelpItem> items;
                int? selectedItem;
                var methods = semanticModel.GetMemberGroup(invocationExpression.Expression, cancellationToken)
                                               .OfType<IMethodSymbol>()
                                               .ToImmutableArray()
                                               .FilterToVisibleAndBrowsableSymbols(options.HideAdvancedMembers, semanticModel.Compilation);

                methods = methods.Sort(semanticModel, invocationExpression.SpanStart);

                if (!methods.Any())
                {
                    return null;
                }

                // figure out the best candidate (if any)
                var currentSymbol = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken).Symbol;
                var semanticFactsService = document.GetRequiredLanguageService<ISemanticFactsService>();
                var arguments = invocationExpression.ArgumentList.Arguments;
                var parameterIndex = -1;
                if (currentSymbol is null)
                {
                    (currentSymbol, parameterIndex) = GuessCurrentSymbolAndParameter(arguments, methods, position,
                        semanticModel, semanticFactsService);
                }
                else
                {
                    // The compiler told us the correct overload, but we need to find out the parameter to highlight given cursor position
                    _ = FindParameterIndexIfCompatibleMethod(arguments, (IMethodSymbol)currentSymbol, position, semanticModel, semanticFactsService, out parameterIndex);
                }

                // if the symbol could be bound, replace that item in the symbol list
                if (currentSymbol is IMethodSymbol matchedMethodSymbol && matchedMethodSymbol.IsGenericMethod)
                {
                    methods = methods.SelectAsArray(m => Equals(matchedMethodSymbol.OriginalDefinition, m) ? matchedMethodSymbol : m);
                }

                // present items and select
                var accessibleMethods = GetAccessibleMethods(invocationExpression, semanticModel, within, methods, cancellationToken);
                if (!accessibleMethods.Any())
                {
                    return null;
                }

                (items, selectedItem) = await GetMethodGroupItemsAndSelectionAsync(
                    accessibleMethods, document, invocationExpression, semanticModel, currentSymbol is null ? default : new SymbolInfo(currentSymbol), cancellationToken).ConfigureAwait(false);
                return MakeSignatureHelpItems(items, textSpan, (IMethodSymbol?)currentSymbol, parameterIndex, selectedItem, arguments, position);
            }
        }

        private SignatureHelpState? GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
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
