// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp.Providers
{
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

        private bool TryGetConstructorInitializer(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerKind triggerReason, CancellationToken cancellationToken, out ConstructorInitializerSyntax expression)
        {
            if (!CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out expression))
            {
                return false;
            }

            return expression.ArgumentList != null;
        }

        private bool IsTriggerToken(SyntaxToken token)
        {
            return SignatureHelpUtilities.IsTriggerParenOrComma<ConstructorInitializerSyntax>(token, IsTriggerCharacter);
        }

        private static bool IsArgumentListToken(ConstructorInitializerSyntax expression, SyntaxToken token)
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

            if (!TryGetConstructorInitializer(root, position, document.GetLanguageService<ISyntaxFactsService>(), trigger.Kind, cancellationToken, out var constructorInitializer))
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var within = semanticModel.GetEnclosingNamedType(position, cancellationToken);
            if (within == null)
            {
                return;
            }

            if (within.TypeKind != TypeKind.Struct && within.TypeKind != TypeKind.Class)
            {
                return;
            }

            var type = constructorInitializer.Kind() == SyntaxKind.BaseConstructorInitializer
                ? within.BaseType
                : within;

            if (type == null)
            {
                return;
            }

            var symbolDisplayService = document.Project.LanguageServices.GetService<ISymbolDisplayService>();
            var accessibleConstructors = type.InstanceConstructors
                                             .WhereAsArray(c => c.IsAccessibleWithin(within))
                                             .WhereAsArray(c => c.IsEditorBrowsable(document.ShouldHideAdvancedMembers(), semanticModel.Compilation))
                                             .Sort(symbolDisplayService, semanticModel, constructorInitializer.SpanStart);

            if (!accessibleConstructors.Any())
            {
                return;
            }

            var anonymousTypeDisplayService = document.Project.LanguageServices.GetService<IAnonymousTypeDisplayService>();
            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(constructorInitializer.ArgumentList);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            context.AddItems(accessibleConstructors.Select(c =>
                Convert(c, constructorInitializer.ArgumentList.OpenParenToken, semanticModel, symbolDisplayService, anonymousTypeDisplayService, cancellationToken)));

            context.SetSpan(textSpan);
            context.SetState(GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken));
        }

        protected override SignatureHelpState GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
        {
            if (TryGetConstructorInitializer(root, position, syntaxFacts, SignatureHelpTriggerKind.Other, cancellationToken, out var expression) &&
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
            CancellationToken cancellationToken)
        {
            var position = openToken.SpanStart;
            var item = CreateItem(
                constructor, semanticModel, position,
                symbolDisplayService, anonymousTypeDisplayService,
                constructor.IsParams(),
                GetPreambleParts(constructor, semanticModel, position),
                GetSeparatorParts(),
                GetPostambleParts(constructor),
                constructor.Parameters.Select(p => Convert(p, semanticModel, position, cancellationToken)).ToList());
            return item;
        }

        private IList<SymbolDisplayPart> GetPreambleParts(
            IMethodSymbol method,
            SemanticModel semanticModel,
            int position)
        {
            var result = new List<SymbolDisplayPart>();

            result.AddRange(method.ContainingType.ToMinimalDisplayParts(semanticModel, position));
            result.Add(Punctuation(SyntaxKind.OpenParenToken));

            return result;
        }

        private IList<SymbolDisplayPart> GetPostambleParts(IMethodSymbol method)
        {
            return SpecializedCollections.SingletonList(
                Punctuation(SyntaxKind.CloseParenToken));
        }
    }
}