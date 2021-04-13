// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    [ExportSignatureHelpProvider("ObjectCreationExpressionSignatureHelpProvider", LanguageNames.CSharp), Shared]
    internal partial class ObjectCreationExpressionSignatureHelpProvider : AbstractCSharpSignatureHelpProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ObjectCreationExpressionSignatureHelpProvider()
        {
        }

        public override bool IsTriggerCharacter(char ch)
            => ch == '(' || ch == ',';

        public override bool IsRetriggerCharacter(char ch)
            => ch == ')';

        private bool TryGetObjectCreationExpression(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out BaseObjectCreationExpressionSyntax expression)
        {
            if (!CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out expression))
            {
                return false;
            }

            return expression.ArgumentList != null;
        }

        private bool IsTriggerToken(SyntaxToken token)
            => SignatureHelpUtilities.IsTriggerParenOrComma<BaseObjectCreationExpressionSyntax>(token, IsTriggerCharacter);

        private static bool IsArgumentListToken(BaseObjectCreationExpressionSyntax expression, SyntaxToken token)
        {
            return expression.ArgumentList != null &&
                expression.ArgumentList.Span.Contains(token.SpanStart) &&
                token != expression.ArgumentList.CloseParenToken;
        }

        protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (!TryGetObjectCreationExpression(root, position, document.GetRequiredLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out var objectCreationExpression))
            {
                return null;
            }

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(objectCreationExpression, cancellationToken).ConfigureAwait(false);
            if (semanticModel.GetTypeInfo(objectCreationExpression, cancellationToken).Type is not INamedTypeSymbol type)
            {
                return null;
            }

            var within = semanticModel.GetEnclosingNamedType(position, cancellationToken);
            if (within == null)
            {
                return null;
            }

            var anonymousTypeDisplayService = document.GetRequiredLanguageService<IAnonymousTypeDisplayService>();
            var documentationCommentFormattingService = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();
            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(objectCreationExpression.ArgumentList!);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            var (items, selectedItem) = type.TypeKind == TypeKind.Delegate
                ? GetDelegateTypeConstructors(objectCreationExpression, semanticModel, anonymousTypeDisplayService, type)
                : GetNormalTypeConstructors(document, objectCreationExpression, semanticModel, anonymousTypeDisplayService, documentationCommentFormattingService, type, within, cancellationToken);

            return CreateSignatureHelpItems(items, textSpan,
                GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItem);
        }

        public override SignatureHelpState? GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
        {
            if (TryGetObjectCreationExpression(
                    root,
                    position,
                    syntaxFacts,
                    SignatureHelpTriggerReason.InvokeSignatureHelpCommand,
                    cancellationToken,
                    out var expression) &&
                currentSpan.Start == SignatureHelpUtilities.GetSignatureHelpSpan(expression.ArgumentList!).Start)
            {
                return SignatureHelpUtilities.GetSignatureHelpState(expression.ArgumentList!, position);
            }

            return null;
        }
    }
}
