// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    [ExportSignatureHelpProvider("ObjectCreationExpressionSignatureHelpProvider", LanguageNames.CSharp), Shared]
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
            return SignatureHelpUtilities.IsTriggerParenOrComma<ObjectCreationExpressionSyntax>(token, IsTriggerCharacter);
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
            if (!TryGetObjectCreationExpression(root, position, document.GetLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out var objectCreationExpression))
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

            // get the candidate methods
            var symbolDisplayService = document.GetLanguageService<ISymbolDisplayService>();
            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(objectCreationExpression.ArgumentList);
            var methods = ImmutableArray<IMethodSymbol>.Empty;
            if (type.TypeKind == TypeKind.Delegate)
            {
                var invokeMethod = type.DelegateInvokeMethod;
                if (invokeMethod != null)
                {
                    methods = ImmutableArray.Create(invokeMethod);
                }
            }
            else
            {
                methods = type.InstanceConstructors
                   .WhereAsArray(c => c.IsAccessibleWithin(within))
                   .WhereAsArray(s => s.IsEditorBrowsable(document.ShouldHideAdvancedMembers(), semanticModel.Compilation))
                   .Sort(symbolDisplayService, semanticModel, objectCreationExpression.SpanStart);
                methods = methods.Sort(symbolDisplayService, semanticModel, objectCreationExpression.SpanStart);
                methods = RemoveUnacceptable(methods, objectCreationExpression, within, semanticModel, cancellationToken);
            }

            if (!methods.Any())
            {
                return default;
            }

            // try to bind to the actual constructor
            var currentSymbol = semanticModel.GetSymbolInfo(objectCreationExpression, cancellationToken).Symbol;

            var semanticFactsService = document.GetLanguageService<ISemanticFactsService>();
            var arguments = objectCreationExpression.ArgumentList.Arguments;
            var parameterIndex = -1;
            if (currentSymbol is null)
            {
                (currentSymbol, parameterIndex) = GuessCurrentSymbolAndParameter(arguments, methods, position,
                    semanticModel, semanticFactsService, cancellationToken);
            }
            else
            {
                _ = IsAcceptable(arguments, (IMethodSymbol)currentSymbol, position, semanticModel, semanticFactsService, out parameterIndex);
            }

            // present items and select
            ImmutableArray<SignatureHelpItem> items;
            int? selectedItem;
            var anonymousTypeDisplayService = document.GetLanguageService<IAnonymousTypeDisplayService>();
            if (type.TypeKind == TypeKind.Delegate)
            {
                items = methods.SelectAsArray(m =>
                    CreateItem(
                        m, semanticModel, position,
                        symbolDisplayService, anonymousTypeDisplayService,
                        isVariadic: false,
                        documentationFactory: null,
                        prefixParts: GetDelegateTypePreambleParts(m, semanticModel, position),
                        separatorParts: GetSeparatorParts(),
                        suffixParts: GetDelegateTypePostambleParts(m),
                        parameters: GetDelegateTypeParameters(m, semanticModel, position, cancellationToken)));
                selectedItem = 0;
            }
            else
            {
                var documentationCommentFormattingService = document.GetLanguageService<IDocumentationCommentFormattingService>();
                items = methods.SelectAsArray(c =>
                    ConvertNormalTypeConstructor(c, objectCreationExpression, semanticModel, symbolDisplayService, anonymousTypeDisplayService, documentationCommentFormattingService, cancellationToken));
                selectedItem = TryGetSelectedIndex(methods, currentSymbol);
            }

            if (currentSymbol is null || parameterIndex < 0)
            {
                var argumentIndex = GetArgumentIndex(arguments, position);
                return new SignatureHelpItems(items, textSpan, argumentIndex < 0 ? 0 : argumentIndex, arguments.Count, argumentName: null, selectedItem);
            }

            var methodSymbol = (IMethodSymbol)currentSymbol;
            var parameters = methodSymbol.Parameters;
            var name = parameters.Length == 0 ? null : parameters[parameterIndex].Name;
            return new SignatureHelpItems(items, textSpan, parameterIndex, methodSymbol.Parameters.Length, name, selectedItem);
        }

        private static ImmutableArray<IMethodSymbol> RemoveUnacceptable(IEnumerable<IMethodSymbol> methodGroup, ObjectCreationExpressionSyntax objectCreation,
            ISymbol within, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return methodGroup.Where(m => !IsInacceptable(objectCreation.ArgumentList.Arguments, m)).ToImmutableArray();
        }
    }
}
