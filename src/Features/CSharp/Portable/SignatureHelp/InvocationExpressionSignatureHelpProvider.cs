﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
using Microsoft.CodeAnalysis.LanguageService;
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

        private async Task<InvocationExpressionSyntax?> TryGetInvocationExpressionAsync(Document document, int position, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            if (!CommonSignatureHelpUtilities.TryGetSyntax(
                    root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out InvocationExpressionSyntax? expression))
            {
                return null;
            }

            if (expression.ArgumentList is null)
                return null;

            return expression;
        }

        private bool IsTriggerToken(SyntaxToken token)
            => SignatureHelpUtilities.IsTriggerParenOrComma<InvocationExpressionSyntax>(token, IsTriggerCharacter);

        private static bool IsArgumentListToken(InvocationExpressionSyntax expression, SyntaxToken token)
        {
            return expression.ArgumentList.Span.Contains(token.SpanStart) &&
                token != expression.ArgumentList.CloseParenToken;
        }

        protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(
            Document document,
            int position,
            SignatureHelpTriggerInfo triggerInfo,
            SignatureHelpOptions options,
            CancellationToken cancellationToken)
        {
            var invocationExpression = await TryGetInvocationExpressionAsync(
                document, position, triggerInfo.TriggerReason, cancellationToken).ConfigureAwait(false);
            if (invocationExpression is null)
                return null;

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(invocationExpression, cancellationToken).ConfigureAwait(false);
            var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
            if (within == null)
                return null;

            var invokedType = semanticModel.GetTypeInfo(invocationExpression.Expression, cancellationToken).Type;
            if (invokedType is INamedTypeSymbol { TypeKind: TypeKind.Delegate } or IFunctionPointerTypeSymbol)
            {
                return await GetItemsWorkerForDelegateOrFunctionPointerAsync(document, position, invocationExpression, within, cancellationToken).ConfigureAwait(false);
            }

            // get the candidate methods
            var symbolDisplayService = document.GetLanguageService<ISymbolDisplayService>();
            var methods = semanticModel
                .GetMemberGroup(invocationExpression.Expression, cancellationToken)
                .OfType<IMethodSymbol>()
                .ToImmutableArray()
                .FilterToVisibleAndBrowsableSymbols(options.HideAdvancedMembers, semanticModel.Compilation);
            methods = GetAccessibleMethods(invocationExpression, semanticModel, within, methods, cancellationToken);
            methods = methods.Sort(semanticModel, invocationExpression.SpanStart);

            if (!methods.Any())
                return null;

            // guess the best candidate if needed and determine parameter index
            var arguments = invocationExpression.ArgumentList.Arguments;
            var symbolInfo = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken);
            var candidates = symbolInfo.Symbol is IMethodSymbol exactMatch
                ? ImmutableArray.Create(exactMatch)
                : methods;
            LightweightOverloadResolution.RefineOverloadAndPickParameter(
                document, position, semanticModel, candidates, arguments, out var currentSymbol, out var parameterIndexOverride);

            // if the symbol could be bound, replace that item in the symbol list
            if (currentSymbol?.IsGenericMethod == true)
            {
                methods = methods.SelectAsArray(m => Equals(currentSymbol.OriginalDefinition, m) ? currentSymbol : m);
            }

            // present items and select
            var (items, selectedItem) = await GetMethodGroupItemsAndSelectionAsync(
                methods, document, invocationExpression, semanticModel, symbolInfo, currentSymbol, cancellationToken).ConfigureAwait(false);

            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(invocationExpression.ArgumentList);
            var argumentState = await GetCurrentArgumentStateAsync(
                document, position, textSpan, cancellationToken).ConfigureAwait(false);
            return CreateSignatureHelpItems(items, textSpan, argumentState, selectedItem, parameterIndexOverride);
        }

        protected async Task<SignatureHelpItems?> GetItemsWorkerForDelegateOrFunctionPointerAsync(
            Document document,
            int position,
            InvocationExpressionSyntax invocationExpression,
            ISymbol within,
            CancellationToken cancellationToken)
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
                throw ExceptionUtilities.Unreachable();
            }

            if (currentSymbol is null)
            {
                return null;
            }

            // determine parameter index
            var arguments = invocationExpression.ArgumentList.Arguments;
            var semanticFactsService = document.GetRequiredLanguageService<ISemanticFactsService>();
            LightweightOverloadResolution.FindParameterIndexIfCompatibleMethod(
                arguments, currentSymbol, position, semanticModel, semanticFactsService, out var parameterIndexOverride);

            // present item and select
            var structuralTypeDisplayService = document.Project.Services.GetRequiredService<IStructuralTypeDisplayService>();
            var documentationCommentFormattingService = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();

            var items = GetDelegateOrFunctionPointerInvokeItems(invocationExpression, currentSymbol,
                semanticModel, structuralTypeDisplayService, documentationCommentFormattingService, out var selectedItem, cancellationToken);

            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(invocationExpression.ArgumentList);
            var argumentState = await GetCurrentArgumentStateAsync(
                document, position, textSpan, cancellationToken).ConfigureAwait(false);
            return CreateSignatureHelpItems(items, textSpan, argumentState, selectedItem, parameterIndexOverride);
        }

        private async Task<SignatureHelpState?> GetCurrentArgumentStateAsync(
            Document document,
            int position,
            TextSpan currentSpan,
            CancellationToken cancellationToken)
        {
            var expression = await TryGetInvocationExpressionAsync(
                document, position, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken).ConfigureAwait(false);
            if (expression is { ArgumentList: not null } &&
                currentSpan.Start == SignatureHelpUtilities.GetSignatureHelpSpan(expression.ArgumentList).Start)
            {
                return SignatureHelpUtilities.GetSignatureHelpState(expression.ArgumentList, position);
            }

            return null;
        }
    }
}
