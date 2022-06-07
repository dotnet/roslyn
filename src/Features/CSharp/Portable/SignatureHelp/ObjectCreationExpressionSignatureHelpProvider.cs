// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    [ExportSignatureHelpProvider("ObjectCreationExpressionSignatureHelpProvider", LanguageNames.CSharp), Shared]
    internal partial class ObjectCreationExpressionSignatureHelpProvider : AbstractCSharpSignatureHelpProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ObjectCreationExpressionSignatureHelpProvider()
        {
        }

        public override bool IsTriggerCharacter(char ch)
            => ch is '(' or ',';

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

        protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, SignatureHelpOptions options, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (!TryGetObjectCreationExpression(root, position, document.GetRequiredLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out var objectCreationExpression)
                || objectCreationExpression.ArgumentList is null)
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

            var symbolDisplayService = document.GetLanguageService<ISymbolDisplayService>();
            if (type.TypeKind == TypeKind.Delegate)
            {
                return await GetItemsWorkerForDelegateAsync(document, position, objectCreationExpression, type, cancellationToken).ConfigureAwait(false);
            }

            // get the candidate methods
            var methods = type.InstanceConstructors
                             .WhereAsArray(c => c.IsAccessibleWithin(within))
                             .WhereAsArray(s => s.IsEditorBrowsable(options.HideAdvancedMembers, semanticModel.Compilation))
                             .Sort(semanticModel, objectCreationExpression.SpanStart);

            if (!methods.Any())
            {
                return null;
            }

            // guess the best candidate if needed and determine parameter index
            var arguments = objectCreationExpression.ArgumentList.Arguments;
            var candidates = semanticModel.GetSymbolInfo(objectCreationExpression, cancellationToken).Symbol is IMethodSymbol exactMatch
                ? ImmutableArray.Create(exactMatch)
                : methods;
            LightweightOverloadResolution.RefineOverloadAndPickParameter(document, position, semanticModel, methods, arguments, out var currentSymbol, out var parameterIndex);

            // present items and select
            var structuralTypeDisplayService = document.Project.LanguageServices.GetRequiredService<IStructuralTypeDisplayService>();
            var documentationCommentFormattingService = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();

            var items = methods.SelectAsArray(c =>
                ConvertNormalTypeConstructor(c, objectCreationExpression, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService));

            var selectedItem = TryGetSelectedIndex(methods, currentSymbol);

            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(objectCreationExpression.ArgumentList);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            return CreateSignatureHelpItems(items, textSpan, GetCurrentArgumentState(root, position, parameterIndex, syntaxFacts, textSpan, cancellationToken), selectedItem);
        }

        private async Task<SignatureHelpItems?> GetItemsWorkerForDelegateAsync(Document document, int position, BaseObjectCreationExpressionSyntax objectCreationExpression,
            INamedTypeSymbol type, CancellationToken cancellationToken)
        {
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(objectCreationExpression, cancellationToken).ConfigureAwait(false);
            Debug.Assert(type.TypeKind == TypeKind.Delegate);
            Debug.Assert(objectCreationExpression.ArgumentList is not null);

            var invokeMethod = type.DelegateInvokeMethod;
            if (invokeMethod is null)
            {
                return null;
            }

            // determine parameter index
            var arguments = objectCreationExpression.ArgumentList.Arguments;
            var semanticFactsService = document.GetRequiredLanguageService<ISemanticFactsService>();
            LightweightOverloadResolution.FindParameterIndexIfCompatibleMethod(arguments, invokeMethod, position, semanticModel, semanticFactsService, out var parameterIndex);

            // present item and select
            var structuralTypeDisplayService = document.Project.LanguageServices.GetRequiredService<IStructuralTypeDisplayService>();
            var items = ConvertDelegateTypeConstructor(objectCreationExpression, invokeMethod, semanticModel, structuralTypeDisplayService, position);
            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(objectCreationExpression.ArgumentList);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return CreateSignatureHelpItems(items, textSpan, GetCurrentArgumentState(root, position, parameterIndex, syntaxFacts, textSpan, cancellationToken), selectedItem: 0);
        }

        private SignatureHelpState? GetCurrentArgumentState(SyntaxNode root, int position, int parameterIndex, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
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
                return SignatureHelpUtilities.GetSignatureHelpState(expression.ArgumentList!, position, parameterIndex);
            }

            return null;
        }
    }
}
