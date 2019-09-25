// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
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

            var semanticModel = await document.GetSemanticModelForNodeAsync(initializerExpression, cancellationToken).ConfigureAwait(false);
            var compilation = semanticModel.Compilation;
            var ienumerableType = compilation.GetTypeByMetadataName(typeof(IEnumerable).FullName);
            if (ienumerableType == null)
            {
                return null;
            }

            // get the regular signature help items
            var symbolDisplayService = document.GetLanguageService<ISymbolDisplayService>();
            var parentOperation = semanticModel.GetOperation(initializerExpression.Parent, cancellationToken) as IObjectOrCollectionInitializerOperation;
            if (parentOperation == null)
            {
                return null;
            }

            var parentType = parentOperation.Type;
            if (parentType == null)
            {
                return null;
            }

            if (!parentType.AllInterfaces.Contains(ienumerableType))
            {
                return null;
            }

            var addSymbols = semanticModel.LookupSymbols(
                position, parentType, WellKnownMemberNames.CollectionInitializerAddMethodName, includeReducedExtensionMethods: true);

            // We want all the accessible '.Add' methods that take at least two arguments. For
            // example, say there is:
            //
            //      new JObject { { $$ } }
            //
            // Technically, the user could be calling the `.Add(object)` overload in this case.
            // However, normally in that case, they would just supply the value directly like so:
            //
            //      new JObject { new JProperty(...), new JProperty(...) }
            //
            // So, it's a strong signal when they're inside another `{ $$ }` that they want to
            // call the .Add methods that take multiple args, like so:
            //
            //      new JObject { { propName, propValue }, { propName, propValue } }

            var addMethods = addSymbols.OfType<IMethodSymbol>()
                                       .Where(m => m.Parameters.Length >= 2)
                                       .ToImmutableArray()
                                       .FilterToVisibleAndBrowsableSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation)
                                       .Sort(symbolDisplayService, semanticModel, initializerExpression.SpanStart);

            if (addMethods.IsEmpty)
            {
                return null;
            }

            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(initializerExpression);
            var anonymousTypeDisplayService = document.GetLanguageService<IAnonymousTypeDisplayService>();
            var documentationCommentFormattingService = document.GetLanguageService<IDocumentationCommentFormattingService>();
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            return CreateSignatureHelpItems(addMethods.Select(s =>
                ConvertMethodGroupMethod(s, initializerExpression.OpenBraceToken.SpanStart, semanticModel, symbolDisplayService, anonymousTypeDisplayService, documentationCommentFormattingService, cancellationToken)).ToList(),
                textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItem: null);
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
