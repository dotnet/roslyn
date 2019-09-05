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
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    [ExportSignatureHelpProvider("ElementAccessExpressionSignatureHelpProvider", LanguageNames.CSharp), Shared]
    internal sealed class ElementAccessExpressionSignatureHelpProvider : AbstractCSharpSignatureHelpProvider
    {
        [ImportingConstructor]
        public ElementAccessExpressionSignatureHelpProvider()
        {
        }

        public override bool IsTriggerCharacter(char ch)
        {
            return IsTriggerCharacterInternal(ch);
        }

        private static bool IsTriggerCharacterInternal(char ch)
        {
            return ch == '[' || ch == ',';
        }

        public override bool IsRetriggerCharacter(char ch)
        {
            return ch == ']';
        }

        private static bool TryGetElementAccessExpression(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out ExpressionSyntax identifier, out SyntaxToken openBrace)
        {
            return CompleteElementAccessExpression.TryGetSyntax(root, position, syntaxFacts, triggerReason, cancellationToken, out identifier, out openBrace) ||
                   IncompleteElementAccessExpression.TryGetSyntax(root, position, syntaxFacts, triggerReason, cancellationToken, out identifier, out openBrace) ||
                   ConditionalAccessExpression.TryGetSyntax(root, position, syntaxFacts, triggerReason, cancellationToken, out identifier, out openBrace);
        }

        protected override async Task<SignatureHelpItems> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (!TryGetElementAccessExpression(root, position, document.GetLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out var expression, out var openBrace))
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var expressionSymbol = semanticModel.GetSymbolInfo(expression, cancellationToken).GetAnySymbol();
            // goo?[$$]
            if (expressionSymbol is INamedTypeSymbol namedType)
            {
                if (namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T &&
                    expression.IsKind(SyntaxKind.NullableType) &&
                    expression.IsChildNode<ArrayTypeSyntax>(a => a.ElementType))
                {
                    // Speculatively bind the type part of the nullable as an expression
                    var nullableTypeSyntax = (NullableTypeSyntax)expression;
                    var speculativeBinding = semanticModel.GetSpeculativeSymbolInfo(position, nullableTypeSyntax.ElementType, SpeculativeBindingOption.BindAsExpression);
                    expressionSymbol = speculativeBinding.GetAnySymbol();
                    expression = nullableTypeSyntax.ElementType;
                }
            }

            if (expressionSymbol != null && expressionSymbol is INamedTypeSymbol)
            {
                return null;
            }

            if (!TryGetIndexers(position, semanticModel, expression, cancellationToken, out var indexers, out var expressionType) &&
                !TryGetComIndexers(semanticModel, expression, cancellationToken, out indexers, out expressionType))
            {
                return null;
            }

            var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
            if (within == null)
            {
                return null;
            }

            var accessibleIndexers = indexers.WhereAsArray(
                m => m.IsAccessibleWithin(within, throughType: expressionType));
            if (!accessibleIndexers.Any())
            {
                return null;
            }

            var symbolDisplayService = document.GetLanguageService<ISymbolDisplayService>();
            accessibleIndexers = accessibleIndexers.FilterToVisibleAndBrowsableSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation)
                                                   .Sort(symbolDisplayService, semanticModel, expression.SpanStart);

            var anonymousTypeDisplayService = document.GetLanguageService<IAnonymousTypeDisplayService>();
            var documentationCommentFormattingService = document.GetLanguageService<IDocumentationCommentFormattingService>();
            var textSpan = GetTextSpan(expression, openBrace);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            return CreateSignatureHelpItems(accessibleIndexers.Select(p =>
                Convert(p, openBrace, semanticModel, symbolDisplayService, anonymousTypeDisplayService, documentationCommentFormattingService, cancellationToken)).ToList(),
                textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItem: null);
        }

        private TextSpan GetTextSpan(ExpressionSyntax expression, SyntaxToken openBracket)
        {
            if (openBracket.Parent is BracketedArgumentListSyntax)
            {
                if (expression.Parent is ConditionalAccessExpressionSyntax conditional)
                {
                    return TextSpan.FromBounds(conditional.Span.Start, openBracket.FullSpan.End);
                }
                else
                {
                    return CompleteElementAccessExpression.GetTextSpan(expression, openBracket);
                }
            }
            else if (openBracket.Parent is ArrayRankSpecifierSyntax)
            {
                return IncompleteElementAccessExpression.GetTextSpan(expression, openBracket);
            }

            throw ExceptionUtilities.Unreachable;
        }

        public override SignatureHelpState GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
        {
            if (!TryGetElementAccessExpression(
                    root,
                    position,
                    syntaxFacts,
                    SignatureHelpTriggerReason.InvokeSignatureHelpCommand,
                    cancellationToken,
                    out var expression,
                    out var openBracket) ||
                currentSpan.Start != expression.SpanStart)
            {
                return null;
            }

            // If the user is actively typing, it's likely that we're in a broken state and the
            // syntax tree will be incorrect.  Because of this we need to synthesize a new
            // bracketed argument list so we can correctly map the cursor to the current argument
            // and then we need to account for this and offset the position check accordingly.
            int offset;
            BracketedArgumentListSyntax argumentList;
            var newBracketedArgumentList = SyntaxFactory.ParseBracketedArgumentList(openBracket.Parent.ToString());
            if (expression.Parent is ConditionalAccessExpressionSyntax)
            {
                // The typed code looks like: <expression>?[
                var conditional = (ConditionalAccessExpressionSyntax)expression.Parent;
                var elementBinding = SyntaxFactory.ElementBindingExpression(newBracketedArgumentList);
                var conditionalAccessExpression = SyntaxFactory.ConditionalAccessExpression(expression, elementBinding);
                offset = expression.SpanStart - conditionalAccessExpression.SpanStart;
                argumentList = ((ElementBindingExpressionSyntax)conditionalAccessExpression.WhenNotNull).ArgumentList;
            }
            else
            {
                // The typed code looks like:
                //   <expression>[
                // or
                //   <identifier>?[
                var elementAccessExpression = SyntaxFactory.ElementAccessExpression(expression, newBracketedArgumentList);
                offset = expression.SpanStart - elementAccessExpression.SpanStart;
                argumentList = elementAccessExpression.ArgumentList;
            }

            position -= offset;
            return SignatureHelpUtilities.GetSignatureHelpState(argumentList, position);
        }

        private bool TryGetComIndexers(
            SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken,
            out ImmutableArray<IPropertySymbol> indexers, out ITypeSymbol expressionType)
        {
            indexers = semanticModel.GetMemberGroup(expression, cancellationToken)
                .OfType<IPropertySymbol>()
                .ToImmutableArray();

            if (indexers.Any() && expression is MemberAccessExpressionSyntax)
            {
                expressionType = semanticModel.GetTypeInfo(((MemberAccessExpressionSyntax)expression).Expression, cancellationToken).Type;
                return true;
            }

            expressionType = null;
            return false;
        }

        private bool TryGetIndexers(
            int position, SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken,
            out ImmutableArray<IPropertySymbol> indexers, out ITypeSymbol expressionType)
        {
            expressionType = semanticModel.GetTypeInfo(expression, cancellationToken).Type;

            if (expressionType == null)
            {
                indexers = ImmutableArray<IPropertySymbol>.Empty;
                return false;
            }

            if (expressionType is IErrorTypeSymbol errorType)
            {
                // If `expression` is a QualifiedNameSyntax then GetTypeInfo().Type won't have any CandidateSymbols, so
                // we should then fall back to getting the actual symbol for the expression.
                expressionType = errorType.CandidateSymbols.FirstOrDefault().GetSymbolType()
                    ?? semanticModel.GetSymbolInfo(expression).GetAnySymbol().GetSymbolType();
            }

            indexers = semanticModel.LookupSymbols(position, expressionType.WithoutNullability(), WellKnownMemberNames.Indexer)
                .OfType<IPropertySymbol>()
                .ToImmutableArray();
            return true;
        }

        private SignatureHelpItem Convert(
            IPropertySymbol indexer,
            SyntaxToken openToken,
            SemanticModel semanticModel,
            ISymbolDisplayService symbolDisplayService,
            IAnonymousTypeDisplayService anonymousTypeDisplayService,
            IDocumentationCommentFormattingService documentationCommentFormattingService,
            CancellationToken cancellationToken)
        {
            var position = openToken.SpanStart;
            var item = CreateItem(indexer, semanticModel, position,
                symbolDisplayService, anonymousTypeDisplayService,
                indexer.IsParams(),
                indexer.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                GetPreambleParts(indexer, position, semanticModel),
                GetSeparatorParts(),
                GetPostambleParts(indexer),
                indexer.Parameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService, cancellationToken)).ToList());
            return item;
        }

        private IList<SymbolDisplayPart> GetPreambleParts(
            IPropertySymbol indexer,
            int position,
            SemanticModel semanticModel)
        {
            var result = new List<SymbolDisplayPart>();

            if (indexer.ReturnsByRef)
            {
                result.Add(Keyword(SyntaxKind.RefKeyword));
                result.Add(Space());
            }
            else if (indexer.ReturnsByRefReadonly)
            {
                result.Add(Keyword(SyntaxKind.RefKeyword));
                result.Add(Space());
                result.Add(Keyword(SyntaxKind.ReadOnlyKeyword));
                result.Add(Space());
            }

            result.AddRange(indexer.Type.ToMinimalDisplayParts(semanticModel, position));
            result.Add(Space());
            result.AddRange(indexer.ContainingType.ToMinimalDisplayParts(semanticModel, position));

            if (indexer.Name != WellKnownMemberNames.Indexer)
            {
                result.Add(Punctuation(SyntaxKind.DotToken));
                result.Add(new SymbolDisplayPart(SymbolDisplayPartKind.PropertyName, indexer, indexer.Name));
            }

            result.Add(Punctuation(SyntaxKind.OpenBracketToken));

            return result;
        }

        private IList<SymbolDisplayPart> GetPostambleParts(IPropertySymbol indexer)
        {
            return SpecializedCollections.SingletonList(
                Punctuation(SyntaxKind.CloseBracketToken));
        }

        private static class CompleteElementAccessExpression
        {
            internal static bool IsTriggerToken(SyntaxToken token)
            {
                return !token.IsKind(SyntaxKind.None) &&
                    token.ValueText.Length == 1 &&
                    IsTriggerCharacterInternal(token.ValueText[0]) &&
                    token.Parent is BracketedArgumentListSyntax &&
                    token.Parent.Parent is ElementAccessExpressionSyntax;
            }

            internal static bool IsArgumentListToken(ElementAccessExpressionSyntax expression, SyntaxToken token)
            {
                return expression.ArgumentList.Span.Contains(token.SpanStart) &&
                    token != expression.ArgumentList.CloseBracketToken;
            }

            internal static TextSpan GetTextSpan(SyntaxNode expression, SyntaxToken openBracket)
            {
                Contract.ThrowIfFalse(openBracket.Parent is BracketedArgumentListSyntax &&
                    (openBracket.Parent.Parent is ElementAccessExpressionSyntax || openBracket.Parent.Parent is ElementBindingExpressionSyntax));
                return SignatureHelpUtilities.GetSignatureHelpSpan((BracketedArgumentListSyntax)openBracket.Parent);
            }

            internal static bool TryGetSyntax(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out ExpressionSyntax identifier, out SyntaxToken openBrace)
            {
                if (CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out ElementAccessExpressionSyntax elementAccessExpression))
                {
                    identifier = elementAccessExpression.Expression;
                    openBrace = elementAccessExpression.ArgumentList.OpenBracketToken;
                    return true;
                }

                identifier = null;
                openBrace = default;
                return false;
            }
        }

        /// Error tolerance case for
        ///     "goo[$$]" or "goo?[$$]"
        /// which is parsed as an ArrayTypeSyntax variable declaration instead of an ElementAccessExpression  
        private static class IncompleteElementAccessExpression
        {
            internal static bool IsArgumentListToken(ArrayTypeSyntax node, SyntaxToken token)
            {
                return node.RankSpecifiers.Span.Contains(token.SpanStart) &&
                    token != node.RankSpecifiers.First().CloseBracketToken;
            }

            internal static bool IsTriggerToken(SyntaxToken token)
            {
                return !token.IsKind(SyntaxKind.None) &&
                    token.ValueText.Length == 1 &&
                    IsTriggerCharacterInternal(token.ValueText[0]) &&
                    token.Parent is ArrayRankSpecifierSyntax;
            }

            internal static TextSpan GetTextSpan(SyntaxNode expression, SyntaxToken openBracket)
            {
                Contract.ThrowIfFalse(openBracket.Parent is ArrayRankSpecifierSyntax && openBracket.Parent.Parent is ArrayTypeSyntax);
                return TextSpan.FromBounds(expression.SpanStart, openBracket.Parent.Span.End);
            }

            internal static bool TryGetSyntax(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out ExpressionSyntax identifier, out SyntaxToken openBrace)
            {
                if (CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out ArrayTypeSyntax arrayTypeSyntax))
                {
                    identifier = arrayTypeSyntax.ElementType;
                    openBrace = arrayTypeSyntax.RankSpecifiers.First().OpenBracketToken;
                    return true;
                }

                identifier = null;
                openBrace = default;
                return false;
            }
        }

        /// Error tolerance case for
        ///     "new String()?[$$]"
        /// which is parsed as a BracketedArgumentListSyntax parented by an ElementBindingExpressionSyntax parented by a ConditionalAccessExpressionSyntax
        private static class ConditionalAccessExpression
        {
            internal static bool IsTriggerToken(SyntaxToken token)
            {
                return !token.IsKind(SyntaxKind.None) &&
                    token.ValueText.Length == 1 &&
                    IsTriggerCharacterInternal(token.ValueText[0]) &&
                    token.Parent is BracketedArgumentListSyntax &&
                    token.Parent.Parent is ElementBindingExpressionSyntax &&
                    token.Parent.Parent.Parent is ConditionalAccessExpressionSyntax;
            }

            internal static bool IsArgumentListToken(ElementBindingExpressionSyntax expression, SyntaxToken token)
            {
                return expression.ArgumentList.Span.Contains(token.SpanStart) &&
                    token != expression.ArgumentList.CloseBracketToken;
            }

            internal static bool TryGetSyntax(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out ExpressionSyntax identifier, out SyntaxToken openBrace)
            {
                if (CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out ElementBindingExpressionSyntax elementBindingExpression))
                {
                    // Find the first conditional access expression that starts left of our open bracket
                    var conditionalAccess = elementBindingExpression.FirstAncestorOrSelf<ConditionalAccessExpressionSyntax>(
                        c => c.SpanStart < elementBindingExpression.SpanStart);

                    identifier = conditionalAccess.Expression;
                    openBrace = elementBindingExpression.ArgumentList.OpenBracketToken;

                    return true;
                }

                identifier = null;
                openBrace = default;
                return false;
            }
        }
    }
}
