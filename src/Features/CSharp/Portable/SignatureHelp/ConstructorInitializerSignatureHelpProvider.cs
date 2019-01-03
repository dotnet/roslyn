// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    [ExportSignatureHelpProvider("ConstructorInitializerSignatureHelpProvider", LanguageNames.CSharp), Shared]
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
            return SignatureHelpUtilities.IsTriggerParenOrComma<ConstructorInitializerSyntax>(token, IsTriggerCharacter);
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
            if (!TryGetConstructorInitializer(root, position, document.GetLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out var constructorInitializer))
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

            // get the candidate methods
            var currentConstructor = semanticModel.GetDeclaredSymbol(constructorInitializer.Parent);
            var symbolDisplayService = document.GetLanguageService<ISymbolDisplayService>();
            var accessibleConstructors = type.InstanceConstructors
                .WhereAsArray(c => c.IsAccessibleWithin(within) && !c.Equals(currentConstructor))
                .WhereAsArray(c => c.IsEditorBrowsable(document.ShouldHideAdvancedMembers(), semanticModel.Compilation))
                .Sort(symbolDisplayService, semanticModel, constructorInitializer.SpanStart);
            accessibleConstructors = RemoveUnacceptable(accessibleConstructors, constructorInitializer, within, semanticModel, cancellationToken);

            if (!accessibleConstructors.Any())
            {
                return null;
            }

            // try to bind to the actual constructor
            var currentSymbol = semanticModel.GetSymbolInfo(constructorInitializer, cancellationToken).Symbol;

            var semanticFactsService = document.GetLanguageService<ISemanticFactsService>();
            var arguments = constructorInitializer.ArgumentList.Arguments;
            var parameterIndex = -1;
            if (currentSymbol is null)
            {
                (currentSymbol, parameterIndex) = GuessCurrentSymbolAndParameter(arguments, accessibleConstructors, position,
                    semanticModel, semanticFactsService, cancellationToken);
            }
            else
            {
                _ = IsAcceptable(arguments, (IMethodSymbol)currentSymbol, position, semanticModel, semanticFactsService, out parameterIndex);
            }

            // present items and select
            var anonymousTypeDisplayService = document.GetLanguageService<IAnonymousTypeDisplayService>();
            var documentationCommentFormattingService = document.GetLanguageService<IDocumentationCommentFormattingService>();
            var items = accessibleConstructors.SelectAsArray(c =>
                Convert(c, constructorInitializer.ArgumentList.OpenParenToken, semanticModel, symbolDisplayService, anonymousTypeDisplayService, documentationCommentFormattingService, cancellationToken));

            var selectedItem = TryGetSelectedIndex(accessibleConstructors, currentSymbol);

            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(constructorInitializer.ArgumentList);
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

        private static ImmutableArray<IMethodSymbol> RemoveUnacceptable(IEnumerable<IMethodSymbol> methodGroup, ConstructorInitializerSyntax constructorInitializer,
            ISymbol within, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return methodGroup.Where(m => !IsUnacceptable(constructorInitializer.ArgumentList.Arguments, m)).ToImmutableArray();
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
                constructor.Parameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService, cancellationToken)).ToList());
            return item;
        }

        private static IList<SymbolDisplayPart> GetPreambleParts(
            IMethodSymbol method,
            SemanticModel semanticModel,
            int position)
        {
            var result = new List<SymbolDisplayPart>();

            result.AddRange(method.ContainingType.ToMinimalDisplayParts(semanticModel, position));
            result.Add(Punctuation(SyntaxKind.OpenParenToken));

            return result;
        }

        private static IList<SymbolDisplayPart> GetPostambleParts(IMethodSymbol method)
        {
            return SpecializedCollections.SingletonList(
                Punctuation(SyntaxKind.CloseParenToken));
        }
    }
}
