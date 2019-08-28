// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    [ExportSignatureHelpProvider("TupleSignatureHelpProvider", LanguageNames.CSharp), Shared]
    internal class TupleConstructionSignatureHelpProvider : AbstractCSharpSignatureHelpProvider
    {
        private static readonly Func<TupleExpressionSyntax, SyntaxToken> s_getOpenToken = e => e.OpenParenToken;
        private static readonly Func<TupleExpressionSyntax, SyntaxToken> s_getCloseToken = e => e.CloseParenToken;
        private static readonly Func<TupleExpressionSyntax, IEnumerable<SyntaxNodeOrToken>> s_getArgumentsWithSeparators = e => e.Arguments.GetWithSeparators();
        private static readonly Func<TupleExpressionSyntax, IEnumerable<string>> s_getArgumentNames = e => e.Arguments.Select(a => a.NameColon?.Name.Identifier.ValueText ?? string.Empty);

        [ImportingConstructor]
        public TupleConstructionSignatureHelpProvider()
        {
        }

        public override SignatureHelpState GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
        {
            if (GetOuterMostTupleExpressionInSpan(root, position, syntaxFacts, currentSpan, cancellationToken, out var expression))
            {
                return CommonSignatureHelpUtilities.GetSignatureHelpState(expression, position,
                   getOpenToken: s_getOpenToken,
                   getCloseToken: s_getCloseToken,
                   getArgumentsWithSeparators: s_getArgumentsWithSeparators,
                   getArgumentNames: s_getArgumentNames);
            }

            if (GetOuterMostParenthesizedExpressionInSpan(root, position, syntaxFacts, currentSpan, cancellationToken, out var parenthesizedExpression))
            {
                if (currentSpan.Start == parenthesizedExpression.SpanStart)
                {
                    return new SignatureHelpState(
                        argumentIndex: 0,
                        argumentCount: 0,
                        argumentName: string.Empty,
                        argumentNames: null);
                }
            }

            return null;
        }

        private bool GetOuterMostTupleExpressionInSpan(SyntaxNode root, int position,
            ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken, out TupleExpressionSyntax result)
        {
            result = null;
            while (TryGetTupleExpression(SignatureHelpTriggerReason.InvokeSignatureHelpCommand,
                root, position, syntaxFacts, cancellationToken, out var expression))
            {
                if (!currentSpan.Contains(expression.Span))
                {
                    break;
                }

                result = expression;
                position = expression.SpanStart;
            }

            return result != null;
        }

        private bool GetOuterMostParenthesizedExpressionInSpan(SyntaxNode root, int position,
         ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken, out ParenthesizedExpressionSyntax result)
        {
            result = null;
            while (TryGetParenthesizedExpression(SignatureHelpTriggerReason.InvokeSignatureHelpCommand,
                root, position, syntaxFacts, cancellationToken, out var expression))
            {
                if (!currentSpan.Contains(expression.Span))
                {
                    break;
                }

                result = expression;
                position = expression.SpanStart;
            }

            return result != null;
        }

        public override Boolean IsRetriggerCharacter(Char ch)
        {
            return ch == ')';
        }

        public override Boolean IsTriggerCharacter(Char ch)
        {
            return ch == '(' || ch == ',';
        }

        protected override async Task<SignatureHelpItems> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var typeInferrer = document.GetLanguageService<ITypeInferenceService>();
            var inferredTypes = FindNearestTupleConstructionWithInferrableType(root, semanticModel, position, triggerInfo,
                typeInferrer, syntaxFacts, cancellationToken, out var targetExpression);

            if (inferredTypes == null || !inferredTypes.Any())
            {
                return null;
            }

            return CreateItems(position, root, syntaxFacts, targetExpression, semanticModel, inferredTypes, cancellationToken);
        }

        IEnumerable<INamedTypeSymbol> FindNearestTupleConstructionWithInferrableType(SyntaxNode root, SemanticModel semanticModel, int position, SignatureHelpTriggerInfo triggerInfo,
            ITypeInferenceService typeInferrer, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken, out ExpressionSyntax targetExpression)
        {
            // Walk upward through TupleExpressionSyntax/ParenthsizedExpressionSyntax looking for a 
            // place where we can infer a tuple type. 
            ParenthesizedExpressionSyntax parenthesizedExpression = null;
            while (TryGetTupleExpression(triggerInfo.TriggerReason, root, position, syntaxFacts, cancellationToken, out var tupleExpression) ||
                   TryGetParenthesizedExpression(triggerInfo.TriggerReason, root, position, syntaxFacts, cancellationToken, out parenthesizedExpression))
            {
                targetExpression = (ExpressionSyntax)tupleExpression ?? parenthesizedExpression;
                var inferredTypes = typeInferrer.InferTypes(semanticModel, targetExpression.SpanStart, cancellationToken);

                var tupleTypes = inferredTypes.Where(t => t.IsTupleType).OfType<INamedTypeSymbol>().ToList();
                if (tupleTypes.Any())
                {
                    return tupleTypes;
                }

                position = targetExpression.GetFirstToken().SpanStart;
            }

            targetExpression = null;
            return null;
        }

        private SignatureHelpItems CreateItems(int position, SyntaxNode root, ISyntaxFactsService syntaxFacts,
            SyntaxNode targetExpression, SemanticModel semanticModel, IEnumerable<INamedTypeSymbol> tupleTypes, CancellationToken cancellationToken)
        {
            var prefixParts = SpecializedCollections.SingletonEnumerable(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, "(")).ToTaggedText();
            var suffixParts = SpecializedCollections.SingletonEnumerable(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, ")")).ToTaggedText();
            var separatorParts = GetSeparatorParts().ToTaggedText();

            var items = tupleTypes.Select(tupleType => Convert(
                tupleType, prefixParts, suffixParts, separatorParts, semanticModel, position))
                .ToList();

            var state = GetCurrentArgumentState(root, position, syntaxFacts, targetExpression.FullSpan, cancellationToken);
            return CreateSignatureHelpItems(items, targetExpression.Span, state, selectedItem: null);
        }

        SignatureHelpItem Convert(INamedTypeSymbol tupleType, ImmutableArray<TaggedText> prefixParts, ImmutableArray<TaggedText> suffixParts,
            ImmutableArray<TaggedText> separatorParts, SemanticModel semanticModel, int position)
        {
            return new SymbolKeySignatureHelpItem(
                    symbol: tupleType,
                    isVariadic: false,
                    documentationFactory: null,
                    prefixParts: prefixParts,
                    separatorParts: separatorParts,
                    suffixParts: suffixParts,
                    parameters: ConvertTupleMembers(tupleType, semanticModel, position),
                    descriptionParts: null);
        }

        private IEnumerable<SignatureHelpParameter> ConvertTupleMembers(INamedTypeSymbol tupleType, SemanticModel semanticModel, int position)
        {
            var spacePart = Space();
            var result = new List<SignatureHelpParameter>();
            foreach (var element in tupleType.TupleElements)
            {
                var type = element.GetTypeWithAnnotatedNullability();

                // The display name for each element. 
                // Empty strings for elements not explicitly declared
                var elementName = element.IsImplicitlyDeclared ? string.Empty : element.Name;

                var typeParts = type.ToMinimalDisplayParts(semanticModel, position).ToList();
                if (!string.IsNullOrEmpty(elementName))
                {
                    typeParts.Add(spacePart);
                    typeParts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.PropertyName, null, elementName));
                }

                result.Add(new SignatureHelpParameter(name: string.Empty, isOptional: false, documentationFactory: null, displayParts: typeParts));
            }

            return result;
        }

        private bool TryGetTupleExpression(SignatureHelpTriggerReason triggerReason, SyntaxNode root, int position,
            ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken, out TupleExpressionSyntax tupleExpression)
        {
            return CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTupleExpressionTriggerToken,
                IsTupleArgumentListToken, cancellationToken, out tupleExpression);
        }

        private bool IsTupleExpressionTriggerToken(SyntaxToken token)
        {
            return SignatureHelpUtilities.IsTriggerParenOrComma<TupleExpressionSyntax>(token, IsTriggerCharacter);
        }

        private static bool IsTupleArgumentListToken(TupleExpressionSyntax tupleExpression, SyntaxToken token)
        {
            return tupleExpression.Arguments.FullSpan.Contains(token.SpanStart) &&
                token != tupleExpression.CloseParenToken;
        }

        private bool TryGetParenthesizedExpression(SignatureHelpTriggerReason triggerReason, SyntaxNode root, int position,
            ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken, out ParenthesizedExpressionSyntax parenthesizedExpression)
        {
            return CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason,
                IsParenthesizedExpressionTriggerToken, IsParenthesizedExpressionToken, cancellationToken, out parenthesizedExpression);
        }

        private bool IsParenthesizedExpressionTriggerToken(SyntaxToken token)
        {
            return token.IsKind(SyntaxKind.OpenParenToken) && token.Parent is ParenthesizedExpressionSyntax;
        }

        private static bool IsParenthesizedExpressionToken(ParenthesizedExpressionSyntax expr, SyntaxToken token)
        {
            return expr.FullSpan.Contains(token.SpanStart) &&
                token != expr.CloseParenToken;
        }
    }
}
