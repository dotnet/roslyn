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

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp.Providers
{
    [ExportSignatureHelpProvider("TupleSignatureHelpProvider", LanguageNames.CSharp), Shared]
    internal class TupleConstructionSignatureHelpProvider : AbstractCSharpSignatureHelpProvider
    {
        private static readonly Func<TupleExpressionSyntax, SyntaxToken> s_getOpenToken = e => e.OpenParenToken;
        private static readonly Func<TupleExpressionSyntax, SyntaxToken> s_getCloseToken = e => e.CloseParenToken;
        private static readonly Func<TupleExpressionSyntax, IEnumerable<SyntaxNodeOrToken>> s_getArgumentsWithSeparators = e => e.Arguments.GetWithSeparators();
        private static readonly Func<TupleExpressionSyntax, IEnumerable<string>> s_getArgumentNames = e => e.Arguments.Select(a => a.NameColon?.Name.Identifier.ValueText ?? string.Empty);

        protected override SignatureHelpState GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
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
            while (TryGetTupleExpression(SignatureHelpTriggerKind.Other,
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
            while (TryGetParenthesizedExpression(SignatureHelpTriggerKind.Other,
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

        public override bool IsRetriggerCharacter(Char ch)
        {
            return ch == ')';
        }

        public override bool IsTriggerCharacter(Char ch)
        {
            return ch == '(' || ch == ',';
        }

        protected override async Task ProvideSignaturesWorkerAsync(SignatureContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var trigger = context.Trigger;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var typeInferrer = document.Project.LanguageServices.GetService<ITypeInferenceService>();
            var symbolDisplayer = document.Project.LanguageServices.GetService<ISymbolDisplayService>();
            var anonymousTypeDisplayer = document.Project.LanguageServices.GetService<IAnonymousTypeDisplayService>();

            var inferredTypes = FindNearestTupleConstructionWithInferrableType(root, semanticModel, position, trigger,
                typeInferrer, syntaxFacts, cancellationToken, out var targetExpression);

            if (inferredTypes == null || !inferredTypes.Any())
            {
                return;
            }

            var prefixParts = SpecializedCollections.SingletonList(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, "("));
            var suffixParts = SpecializedCollections.SingletonList(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, ")"));
            var separatorParts = GetSeparatorParts();

            foreach (var type in inferredTypes)
            {
                var item = base.CreateItem(type, semanticModel, position, symbolDisplayer, anonymousTypeDisplayer,
                    isVariadic: false,
                    prefixParts: prefixParts,
                    separatorParts: separatorParts,
                    suffixParts: suffixParts,
                    parameters: CreateParameters(type, semanticModel, position),
                    descriptionParts: null);

                context.AddItem(item);
            }

            context.SetSpan(targetExpression.Span);
            context.SetState(GetCurrentArgumentState(root, position, syntaxFacts, targetExpression.FullSpan, cancellationToken));
        }

        private IEnumerable<INamedTypeSymbol> FindNearestTupleConstructionWithInferrableType(SyntaxNode root, SemanticModel semanticModel, int position, SignatureHelpTrigger trigger,
            ITypeInferenceService typeInferrer, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken, out ExpressionSyntax targetExpression)
        {
            // Walk upward through TupleExpressionSyntax/ParenthsizedExpressionSyntax looking for a 
            // place where we can infer a tuple type. 
            ParenthesizedExpressionSyntax parenthesizedExpression = null;
            while (TryGetTupleExpression(trigger.Kind, root, position, syntaxFacts, cancellationToken, out var tupleExpression) ||
                   TryGetParenthesizedExpression(trigger.Kind, root, position, syntaxFacts, cancellationToken, out parenthesizedExpression))
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

        private IList<CommonParameterData> CreateParameters(INamedTypeSymbol tupleType, SemanticModel semanticModel, int position)
        {
            var spacePart = Space();
            var result = new List<CommonParameterData>();
            foreach (var element in tupleType.TupleElements)
            {
                var type = element.Type;

                // The display name for each element. 
                // Empty strings for elements not explicitly declared
                var elementName = element.IsImplicitlyDeclared? string.Empty: element.Name;

                var typeParts = type.ToMinimalDisplayParts(semanticModel, position);
                if (!string.IsNullOrEmpty(elementName))
                {
                    typeParts = typeParts.AddRange(new[] { spacePart, new SymbolDisplayPart(SymbolDisplayPartKind.PropertyName, null, elementName) });
                }

                result.Add(new CommonParameterData(name: elementName, isOptional: false, symbol: tupleType, position: position, displayParts: typeParts));
            }

            return result;
        }

        private bool TryGetTupleExpression(SignatureHelpTriggerKind triggerReason, SyntaxNode root, int position, 
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

        private bool TryGetParenthesizedExpression(SignatureHelpTriggerKind triggerReason, SyntaxNode root, int position,
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
