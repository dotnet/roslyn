// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.SignatureHelp;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SignatureHelp
{
    [ExportSignatureHelpProvider("ObjectCreationExpressionSignatureHelpProvider", LanguageNames.CSharp)]
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

        private bool TryGetObjectCreationExpression(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out ObjectCreationExpressionSyntax expression)
        {
            if (!CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out expression))
            {
                return false;
            }

            return expression.ArgumentList != null;
        }

        private bool IsTriggerToken(SyntaxToken token)
        {
            return !token.IsKind(SyntaxKind.None) &&
                token.ValueText.Length == 1 &&
                IsTriggerCharacter(token.ValueText[0]) &&
                token.Parent is ArgumentListSyntax &&
                token.Parent.Parent is ObjectCreationExpressionSyntax;
        }

        private static bool IsArgumentListToken(ObjectCreationExpressionSyntax expression, SyntaxToken token)
        {
            return expression.ArgumentList != null &&
                expression.ArgumentList.Span.Contains(token.SpanStart) &&
                token != expression.ArgumentList.CloseParenToken;
        }

        protected override async Task<SignatureHelpItems> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            ObjectCreationExpressionSyntax objectCreationExpression;
            if (!TryGetObjectCreationExpression(root, position, document.GetLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out objectCreationExpression))
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelForNodeAsync(objectCreationExpression, cancellationToken).ConfigureAwait(false);
            var type = semanticModel.GetTypeInfo(objectCreationExpression, cancellationToken).Type as INamedTypeSymbol;
            if (type == null)
            {
                return null;
            }

            var within = semanticModel.GetEnclosingNamedType(position, cancellationToken);
            if (within == null)
            {
                return null;
            }

            var symbolDisplayService = document.Project.LanguageServices.GetService<ISymbolDisplayService>();
            var anonymousTypeDisplayService = document.Project.LanguageServices.GetService<IAnonymousTypeDisplayService>();
            var documentationCommentFormattingService = document.Project.LanguageServices.GetService<IDocumentationCommentFormattingService>();
            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(objectCreationExpression.ArgumentList);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            return CreateSignatureHelpItems(type.TypeKind == TypeKind.Delegate
                ? GetDelegateTypeConstructors(objectCreationExpression, semanticModel, symbolDisplayService, anonymousTypeDisplayService, type, within, cancellationToken)
                : GetNormalTypeConstructors(document, objectCreationExpression, semanticModel, symbolDisplayService, anonymousTypeDisplayService, documentationCommentFormattingService, type, within, cancellationToken),
                textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken));
        }

        public override SignatureHelpState GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
        {
            ObjectCreationExpressionSyntax expression;
            if (TryGetObjectCreationExpression(
                    root,
                    position,
                    syntaxFacts,
                    SignatureHelpTriggerReason.InvokeSignatureHelpCommand,
                    cancellationToken,
                    out expression) &&
                currentSpan.Start == SignatureHelpUtilities.GetSignatureHelpSpan(expression.ArgumentList).Start)
            {
                return SignatureHelpUtilities.GetSignatureHelpState(expression.ArgumentList, position);
            }

            return null;
        }
    }
}
