// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    [ExportSignatureHelpProvider(nameof(InitializerExpressionSignatureHelpProvider), LanguageNames.CSharp), Shared]
    internal partial class InitializerExpressionSignatureHelpProvider : AbstractOrdinaryMethodSignatureHelpProvider
    {
        public override bool IsTriggerCharacter(char ch)
            => ch == '{' || ch == ',';

        public override bool IsRetriggerCharacter(char ch)
            => ch == '}';

        private bool TryGetInitializerExpression(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out InitializerExpressionSyntax expression)
            => CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsInitializerExpressionToken, cancellationToken, out expression) &&
               expression != null;

        private bool IsTriggerToken(SyntaxToken token)
            => !token.IsKind(SyntaxKind.None) &&
               token.ValueText.Length == 1 &&
               IsTriggerCharacter(token.ValueText[0]) &&
               token.Parent is InitializerExpressionSyntax;

        private static bool IsInitializerExpressionToken(InitializerExpressionSyntax expression, SyntaxToken token)
            => expression.Span.Contains(token.SpanStart) && token != expression.CloseBraceToken;

        protected override async Task<SignatureHelpItems> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (!TryGetInitializerExpression(root, position, document.GetLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out var initializerExpression))
            {
                return null;
            }

            var addMethods = await CommonSignatureHelpUtilities.GetCollectionInitializerAddMethodsAsync(
                document, initializerExpression, cancellationToken).ConfigureAwait(false);
            if (addMethods.IsDefaultOrEmpty)
            {
                return null;
            }

            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(initializerExpression);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return CreateCollectionInitializerSignatureHelpItems(addMethods.Select(s =>
                ConvertMethodGroupMethod(document, s, initializerExpression.OpenBraceToken.SpanStart, semanticModel, cancellationToken)).ToList(),
                textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken));
        }

        public override SignatureHelpState GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
        {
            if (TryGetInitializerExpression(
                    root,
                    position,
                    syntaxFacts,
                    SignatureHelpTriggerReason.InvokeSignatureHelpCommand,
                    cancellationToken,
                    out var expression) &&
                currentSpan.Start == SignatureHelpUtilities.GetSignatureHelpSpan(expression).Start)
            {
                return SignatureHelpUtilities.GetSignatureHelpState(expression, position);
            }

            return null;
        }
    }
}
