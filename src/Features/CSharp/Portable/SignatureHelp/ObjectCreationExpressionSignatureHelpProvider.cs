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

            // get the candidate methods
            var symbolDisplayService = document.GetLanguageService<ISymbolDisplayService>();
            var methods = ImmutableArray<IMethodSymbol>.Empty;
            if (type.TypeKind == TypeKind.Delegate)
            {
                var invokeMethod = type.DelegateInvokeMethod;
                if (invokeMethod is null)
                {
                    return null;
                }

                methods = ImmutableArray.Create(invokeMethod);
            }
            else
            {
                methods = type.InstanceConstructors
                    .WhereAsArray(c => c.IsAccessibleWithin(within))
                    .WhereAsArray(s => s.IsEditorBrowsable(options.HideAdvancedMembers, semanticModel.Compilation))
                    .Sort(semanticModel, objectCreationExpression.SpanStart);

                if (!methods.Any())
                {
                    return null;
                }
            }

            // guess the best candidate if needed and determine parameter index
            var currentSymbol = semanticModel.GetSymbolInfo(objectCreationExpression, cancellationToken).Symbol as IMethodSymbol;
            var semanticFactsService = document.GetRequiredLanguageService<ISemanticFactsService>();
            var arguments = objectCreationExpression.ArgumentList.Arguments;
            int parameterIndex;
            if (currentSymbol is null)
            {
                (currentSymbol, parameterIndex) = GuessCurrentSymbolAndParameter(arguments, methods, position, semanticModel, semanticFactsService);
            }
            else
            {
                // The compiler told us the correct overload, but we need to find out the parameter to highlight given cursor position
                _ = FindParameterIndexIfCompatibleMethod(arguments, currentSymbol, position, semanticModel, semanticFactsService, out parameterIndex);
            }

            // present items and select
            ImmutableArray<SignatureHelpItem> items;
            int? selectedItem;
            var structuralTypeDisplayService = document.Project.LanguageServices.GetRequiredService<IStructuralTypeDisplayService>();
            if (type.TypeKind == TypeKind.Delegate)
            {
                var invokeMethod = type.DelegateInvokeMethod;
                Debug.Assert(invokeMethod is not null);
                items = ConvertDelegateTypeConstructor(objectCreationExpression, invokeMethod, semanticModel, structuralTypeDisplayService, position);
                selectedItem = 0;
            }
            else
            {
                var documentationCommentFormattingService = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();

                items = methods.SelectAsArray(c =>
                    ConvertNormalTypeConstructor(c, objectCreationExpression, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService));

                selectedItem = TryGetSelectedIndex(methods, currentSymbol);
            }

            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(objectCreationExpression.ArgumentList);
            return MakeSignatureHelpItems(items, textSpan, currentSymbol, parameterIndex, selectedItem, arguments, position);
        }
    }
}
