// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
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
using Roslyn.Utilities;

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

            var invokedType = semanticModel.GetTypeInfo(invocationExpression.Expression, cancellationToken).Type;
            if (invokedType is INamedTypeSymbol { TypeKind: TypeKind.Delegate }
                || invokedType is IFunctionPointerTypeSymbol)
            {
                return await GetItemsWorkerForDelegateOrFunctionPointerAsync(document, position, invocationExpression, within, cancellationToken).ConfigureAwait(false);
            }

            // get the candidate methods
            var symbolDisplayService = document.GetLanguageService<ISymbolDisplayService>();
            var methods = semanticModel.GetMemberGroup(invocationExpression.Expression, cancellationToken)
                                                       .OfType<IMethodSymbol>()
                                                       .ToImmutableArray()
                                                       .FilterToVisibleAndBrowsableSymbols(options.HideAdvancedMembers, semanticModel.Compilation);
            methods = GetAccessibleMethods(invocationExpression, semanticModel, within, methods, cancellationToken);
            methods = methods.Sort(semanticModel, invocationExpression.SpanStart);

            if (!methods.Any())
            {
                return null;
            }

            // guess the best candidate if needed and determine parameter index
            var arguments = invocationExpression.ArgumentList.Arguments;
            var symbolInfo = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken);
            var candidates = symbolInfo.Symbol is IMethodSymbol exactMatch
                ? ImmutableArray.Create(exactMatch)
                : methods;
            LightweightOverloadResolution.RefineOverloadAndPickParameter(document, position, semanticModel, candidates, arguments, out var currentSymbol, out var parameterIndex);

            // if the symbol could be bound, replace that item in the symbol list
            if (currentSymbol?.IsGenericMethod == true)
            {
                methods = methods.SelectAsArray(m => Equals(currentSymbol.OriginalDefinition, m) ? currentSymbol : m);
            }

            // present items and select
            var (items, selectedItem) = await GetMethodGroupItemsAndSelectionAsync(
                methods, document, invocationExpression, semanticModel, symbolInfo, currentSymbol, cancellationToken).ConfigureAwait(false);

            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(invocationExpression.ArgumentList);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            return CreateSignatureHelpItems(items, textSpan, GetCurrentArgumentState(root, position, parameterIndex, syntaxFacts, textSpan, cancellationToken), selectedItem);
        }

        protected async Task<SignatureHelpItems?> GetItemsWorkerForDelegateOrFunctionPointerAsync(Document document, int position,
            InvocationExpressionSyntax invocationExpression, ISymbol within, CancellationToken cancellationToken)
        {
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(invocationExpression, cancellationToken).ConfigureAwait(false);

            var invokedType = semanticModel.GetTypeInfo(invocationExpression.Expression, cancellationToken).Type;
            IMethodSymbol? currentSymbol;
            if (invokedType is INamedTypeSymbol { TypeKind: TypeKind.Delegate } expressionType)
            {
                currentSymbol = GetDelegateInvokeMethod(invocationExpression, semanticModel, within, expressionType, cancellationToken);
            }
            else if (invokedType is IFunctionPointerTypeSymbol functionPointerType)
            {
                currentSymbol = functionPointerType.Signature;
            }
            else
            {
                throw ExceptionUtilities.Unreachable;
            }

            if (currentSymbol is null)
            {
                return null;
            }

            // determine parameter index
            var arguments = invocationExpression.ArgumentList.Arguments;
            var semanticFactsService = document.GetRequiredLanguageService<ISemanticFactsService>();
            LightweightOverloadResolution.FindParameterIndexIfCompatibleMethod(arguments, currentSymbol, position, semanticModel, semanticFactsService, out var parameterIndex);

            // present item and select
            var structuralTypeDisplayService = document.Project.LanguageServices.GetRequiredService<IStructuralTypeDisplayService>();
            var documentationCommentFormattingService = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();

            var items = GetDelegateOrFunctionPointerInvokeItems(invocationExpression, currentSymbol,
                semanticModel, structuralTypeDisplayService, documentationCommentFormattingService, out var selectedItem, cancellationToken);

            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(invocationExpression.ArgumentList);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            return CreateSignatureHelpItems(items, textSpan, GetCurrentArgumentState(root, position, parameterIndex, syntaxFacts, textSpan, cancellationToken), selectedItem);
        }

        private SignatureHelpState? GetCurrentArgumentState(SyntaxNode root, int position, int parameterIndex, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
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
                return SignatureHelpUtilities.GetSignatureHelpState(expression.ArgumentList, position, parameterIndex);
            }

            return null;
        }
    }
}
