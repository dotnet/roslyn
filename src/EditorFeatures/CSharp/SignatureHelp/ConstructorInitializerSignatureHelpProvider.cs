// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.SignatureHelp;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SignatureHelp
{
    [ExportSignatureHelpProvider("ConstructorInitializerSignatureHelpProvider", LanguageNames.CSharp)]
    internal partial class ConstructorInitializerSignatureHelpProvider : AbstractCSharpSignatureHelpProvider
    {
        public override bool IsTriggerCharacter(char ch)
        {
            return ch == '(' || ch == ',';
        }

        public override bool IsRetriggerCharacter(char ch)
        {
            return ch == ')';
        }

        private bool TryGetConstructorInitializer(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out ConstructorInitializerSyntax expression)
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
                token.Parent.Parent is ConstructorInitializerSyntax;
        }

        private static bool IsArgumentListToken(ConstructorInitializerSyntax expression, SyntaxToken token)
        {
            return expression.ArgumentList != null &&
                expression.ArgumentList.Span.Contains(token.SpanStart) &&
                token != expression.ArgumentList.CloseParenToken;
        }

        protected override async Task<SignatureHelpItems> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            ConstructorInitializerSyntax constructorInitializer;
            if (!TryGetConstructorInitializer(root, position, document.GetLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out constructorInitializer))
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var within = semanticModel.GetEnclosingNamedType(position, cancellationToken);
            if (within == null)
            {
                return null;
            }

            if (within.TypeKind != TypeKind.Struct && within.TypeKind != TypeKind.Class)
            {
                return null;
            }

            var type = constructorInitializer.Kind() == SyntaxKind.BaseConstructorInitializer
                ? within.BaseType
                : within;

            if (type == null)
            {
                return null;
            }

            var symbolDisplayService = document.Project.LanguageServices.GetService<ISymbolDisplayService>();
            var accessibleConstructors = type.InstanceConstructors
                                             .Where(c => c.IsAccessibleWithin(within))
                                             .Where(c => c.IsEditorBrowsable(document.ShouldHideAdvancedMembers(), semanticModel.Compilation))
                                             .Sort(symbolDisplayService, semanticModel, constructorInitializer.SpanStart);

            if (!accessibleConstructors.Any())
            {
                return null;
            }

            var anonymousTypeDisplayService = document.Project.LanguageServices.GetService<IAnonymousTypeDisplayService>();
            var documentationCommentFormattingService = document.Project.LanguageServices.GetService<IDocumentationCommentFormattingService>();
            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(constructorInitializer.ArgumentList);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            return CreateSignatureHelpItems(accessibleConstructors.Select(c =>
                Convert(c, constructorInitializer.ArgumentList.OpenParenToken, semanticModel, symbolDisplayService, anonymousTypeDisplayService, documentationCommentFormattingService, cancellationToken)),
                textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken));
        }

        public override SignatureHelpState GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
        {
            ConstructorInitializerSyntax expression;
            if (TryGetConstructorInitializer(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, out expression) &&
                currentSpan.Start == SignatureHelpUtilities.GetSignatureHelpSpan(expression.ArgumentList).Start)
            {
                return SignatureHelpUtilities.GetSignatureHelpState(expression.ArgumentList, position);
            }

            return null;
        }

        private SignatureHelpItem Convert(
            IMethodSymbol constructor,
            SyntaxToken openToken,
            SemanticModel semanticModel,
            ISymbolDisplayService symbolDisplayService,
            IAnonymousTypeDisplayService anonymousTypeDisplayService,
            IDocumentationCommentFormattingService documentationCommentFormattingService,
            CancellationToken cancellationToken)
        {
            var position = openToken.SpanStart;
            var item = CreateItem(
                constructor, semanticModel, position,
                symbolDisplayService, anonymousTypeDisplayService,
                constructor.IsParams(),
                constructor.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                GetPreambleParts(constructor, semanticModel, position),
                GetSeparatorParts(),
                GetPostambleParts(constructor),
                constructor.Parameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService, cancellationToken)));
            return item;
        }

        private IEnumerable<SymbolDisplayPart> GetPreambleParts(
            IMethodSymbol method,
            SemanticModel semanticModel,
            int position)
        {
            var result = new List<SymbolDisplayPart>();

            result.AddRange(method.ContainingType.ToMinimalDisplayParts(semanticModel, position));
            result.Add(Punctuation(SyntaxKind.OpenParenToken));

            return result;
        }

        private IEnumerable<SymbolDisplayPart> GetPostambleParts(IMethodSymbol method)
        {
            yield return Punctuation(SyntaxKind.CloseParenToken);
        }
    }
}
