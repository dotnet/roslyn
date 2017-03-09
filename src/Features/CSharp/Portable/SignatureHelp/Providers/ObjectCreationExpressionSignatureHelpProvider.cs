// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp.Providers
{
    internal partial class ObjectCreationExpressionSignatureHelpProvider : AbstractCSharpSignatureHelpProvider
    {
        public override bool IsTriggerCharacter(char ch)
        {
            return ch == '(' || ch == ',';
        }

        public override bool IsRetriggerCharacter(char ch)
        {
            return ch == ')';
        }

        private bool TryGetObjectCreationExpression(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerKind triggerReason, CancellationToken cancellationToken, out ObjectCreationExpressionSyntax expression)
        {
            if (!CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out expression))
            {
                return false;
            }

            return expression.ArgumentList != null;
        }

        private bool IsTriggerToken(SyntaxToken token)
        {
            return SignatureHelpUtilities.IsTriggerParenOrComma<ObjectCreationExpressionSyntax>(token, IsTriggerCharacter);
        }

        private static bool IsArgumentListToken(ObjectCreationExpressionSyntax expression, SyntaxToken token)
        {
            return expression.ArgumentList != null &&
                expression.ArgumentList.Span.Contains(token.SpanStart) &&
                token != expression.ArgumentList.CloseParenToken;
        }

        protected override async Task ProvideSignaturesWorkerAsync(SignatureContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var trigger = context.Trigger;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (!TryGetObjectCreationExpression(root, position, document.GetLanguageService<ISyntaxFactsService>(), trigger.Kind, cancellationToken, out var objectCreationExpression))
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelForNodeAsync(objectCreationExpression, cancellationToken).ConfigureAwait(false);
            var type = semanticModel.GetTypeInfo(objectCreationExpression, cancellationToken).Type as INamedTypeSymbol;
            if (type == null)
            {
                return;
            }

            var within = semanticModel.GetEnclosingNamedType(position, cancellationToken);
            if (within == null)
            {
                return;
            }

            var symbolDisplayService = document.Project.LanguageServices.GetService<ISymbolDisplayService>();
            var anonymousTypeDisplayService = document.Project.LanguageServices.GetService<IAnonymousTypeDisplayService>();
            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(objectCreationExpression.ArgumentList);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var items = type.TypeKind == TypeKind.Delegate
                ? GetDelegateTypeConstructors(objectCreationExpression, semanticModel, symbolDisplayService, anonymousTypeDisplayService, type, within, cancellationToken)
                : GetNormalTypeConstructors(document, objectCreationExpression, semanticModel, symbolDisplayService, anonymousTypeDisplayService, type, within, cancellationToken);

            if (items == null)
            {
                return;
            }

            context.AddItems(items);
            context.SetSpan(textSpan);
            context.SetState(GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken));
        }

        protected override SignatureHelpState GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
        {
            if (TryGetObjectCreationExpression(
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
