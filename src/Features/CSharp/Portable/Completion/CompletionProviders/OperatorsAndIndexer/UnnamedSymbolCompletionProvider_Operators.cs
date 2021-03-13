// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class UnnamedSymbolCompletionProvider
    {
        [Flags]
        private enum OperatorPosition
        {
            None = 0,
            Prefix = 1,
            Infix = 2,
            Postfix = 4,
        }

        private readonly int OperatorSortingGroupIndex = 2;

        private readonly string OperatorName = nameof(OperatorName);
        private readonly ImmutableDictionary<string, string> OperatorProperties =
            ImmutableDictionary<string, string>.Empty.Add(KindName, OperatorKindName);

        private void AddOperator(CompletionContext context, IMethodSymbol op)
        {
            var item = SymbolCompletionItem.CreateWithSymbolId(
                displayText: GetOperatorDisplayText(op),
                displayTextSuffix: null,
                inlineDescription: GetOperatorInlineDescription(op),
                filterText: "",
                sortText: SortText(OperatorSortingGroupIndex, $"{GetOperatorSortIndex(op):000}"),
                symbols: ImmutableArray.Create(op),
                rules: CompletionItemRules.Default,
                contextPosition: context.Position,
                properties: OperatorProperties
                    .Add(OperatorName, op.Name));
            context.AddItem(item);
        }

        private async Task<CompletionChange> GetOperatorChangeAsync(
            Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var operatorName = item.Properties[OperatorName];

            var operatorPosition = GetOperatorPosition(operatorName);

            if (operatorPosition.HasFlag(OperatorPosition.Infix))
                return await ReplaceDotAndTokenAfterWithTextAsync(document, item, text: $" {item.DisplayText} ", removeConditionalAccess: true, positionOffset: 0, cancellationToken).ConfigureAwait(false);

            if (operatorPosition.HasFlag(OperatorPosition.Postfix))
                return await ReplaceDotAndTokenAfterWithTextAsync(document, item, text: $"{item.DisplayText} ", removeConditionalAccess: true, positionOffset: 0, cancellationToken).ConfigureAwait(false);

            if (operatorPosition.HasFlag(OperatorPosition.Prefix))
            {
                var position = SymbolCompletionItem.GetContextPosition(item);
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var (_, dotLikeToken, expression) = FindTokensAtPosition(root, position);

                expression = expression.GetRootConditionalAccessExpression() ?? expression;

                var textChanges = TemporaryArray<TextChange>.Empty;

                // Place the new operator before the expression, and delete the dot.
                textChanges.Add(new TextChange(new TextSpan(expression.SpanStart, 0), item.DisplayText));
                textChanges.Add(new TextChange(dotLikeToken.Span, ""));

                var replacement = item.DisplayText + text.ToString(TextSpan.FromBounds(expression.SpanStart, dotLikeToken.SpanStart));
                var fullTextChange = new TextChange(
                    TextSpan.FromBounds(
                        expression.SpanStart,
                        dotLikeToken.Kind() == SyntaxKind.DotDotToken ? dotLikeToken.Span.Start + 1 : dotLikeToken.Span.End),
                    replacement);

                var newPosition = expression.SpanStart + replacement.Length;
                return CompletionChange.Create(fullTextChange, textChanges.ToImmutableAndClear(), newPosition);
            }

            throw ExceptionUtilities.UnexpectedValue(operatorPosition);
        }

        private static Task<CompletionDescription> GetOperatorDescriptionAsync(
            Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            return SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);
        }

        private static string GetOperatorDisplayText(IMethodSymbol method)
        {
            return method.Name switch
            {
                // binary
                WellKnownMemberNames.AdditionOperatorName => "+",
                WellKnownMemberNames.BitwiseAndOperatorName => "&",
                WellKnownMemberNames.BitwiseOrOperatorName => "|",
                WellKnownMemberNames.DivisionOperatorName => "/",
                WellKnownMemberNames.EqualityOperatorName => "==",
                WellKnownMemberNames.ExclusiveOrOperatorName => "^",
                WellKnownMemberNames.GreaterThanOperatorName => ">",
                WellKnownMemberNames.GreaterThanOrEqualOperatorName => ">=",
                WellKnownMemberNames.InequalityOperatorName => "!=",
                WellKnownMemberNames.LeftShiftOperatorName => "<<",
                WellKnownMemberNames.LessThanOperatorName => "<",
                WellKnownMemberNames.LessThanOrEqualOperatorName => "<=",
                WellKnownMemberNames.ModulusOperatorName => "%",
                WellKnownMemberNames.MultiplyOperatorName => "*",
                WellKnownMemberNames.RightShiftOperatorName => ">>",
                WellKnownMemberNames.SubtractionOperatorName => "-",

                // Unary
                WellKnownMemberNames.DecrementOperatorName => "--",
                WellKnownMemberNames.FalseOperatorName => "false",
                WellKnownMemberNames.IncrementOperatorName => "++",
                WellKnownMemberNames.LogicalNotOperatorName => "!",
                WellKnownMemberNames.OnesComplementOperatorName => "~",
                WellKnownMemberNames.TrueOperatorName => "true",
                WellKnownMemberNames.UnaryNegationOperatorName => "-",
                WellKnownMemberNames.UnaryPlusOperatorName => "+",

                var name => throw ExceptionUtilities.UnexpectedValue(name),
            };
        }

        private static string GetOperatorInlineDescription(IMethodSymbol method)
        {
            return method.Name switch
            {
                // binary
                WellKnownMemberNames.AdditionOperatorName => "x + y",
                WellKnownMemberNames.BitwiseAndOperatorName => "x & y",
                WellKnownMemberNames.BitwiseOrOperatorName => "x | y",
                WellKnownMemberNames.DivisionOperatorName => "x / y",
                WellKnownMemberNames.EqualityOperatorName => "x == y",
                WellKnownMemberNames.ExclusiveOrOperatorName => "x ^ y",
                WellKnownMemberNames.GreaterThanOperatorName => "x > y",
                WellKnownMemberNames.GreaterThanOrEqualOperatorName => "x >= y",
                WellKnownMemberNames.InequalityOperatorName => "x != y",
                WellKnownMemberNames.LeftShiftOperatorName => "x << y",
                WellKnownMemberNames.LessThanOperatorName => "x < y",
                WellKnownMemberNames.LessThanOrEqualOperatorName => "x <= y",
                WellKnownMemberNames.ModulusOperatorName => "x % y",
                WellKnownMemberNames.MultiplyOperatorName => "x * y",
                WellKnownMemberNames.RightShiftOperatorName => "x >> y",
                WellKnownMemberNames.SubtractionOperatorName => "x - y",

                // Unary
                WellKnownMemberNames.DecrementOperatorName => "x--",
                WellKnownMemberNames.FalseOperatorName => "false",
                WellKnownMemberNames.IncrementOperatorName => "x++",
                WellKnownMemberNames.LogicalNotOperatorName => "!x",
                WellKnownMemberNames.OnesComplementOperatorName => "~x",
                WellKnownMemberNames.TrueOperatorName => "true",
                WellKnownMemberNames.UnaryNegationOperatorName => "-x",
                WellKnownMemberNames.UnaryPlusOperatorName => "+x",

                var name => throw ExceptionUtilities.UnexpectedValue(name),
            };
        }

        private static int GetOperatorSortIndex(IMethodSymbol method)
        {
            return method.Name switch
            {
                // comparison and negation
                WellKnownMemberNames.EqualityOperatorName => 0,             // ==
                WellKnownMemberNames.InequalityOperatorName => 1,           // !=
                WellKnownMemberNames.GreaterThanOperatorName => 2,          // >
                WellKnownMemberNames.GreaterThanOrEqualOperatorName => 3,   // >=
                WellKnownMemberNames.LessThanOperatorName => 4,             // <
                WellKnownMemberNames.LessThanOrEqualOperatorName => 5,      // <=
                WellKnownMemberNames.LogicalNotOperatorName => 6,           // !
                // mathematical
                WellKnownMemberNames.AdditionOperatorName => 7,             // +
                WellKnownMemberNames.SubtractionOperatorName => 8,          // -
                WellKnownMemberNames.MultiplyOperatorName => 9,             // *
                WellKnownMemberNames.DivisionOperatorName => 10,            // /
                WellKnownMemberNames.ModulusOperatorName => 11,             // %
                WellKnownMemberNames.IncrementOperatorName => 12,           // ++
                WellKnownMemberNames.DecrementOperatorName => 13,           // --
                WellKnownMemberNames.UnaryPlusOperatorName => 14,           // +
                WellKnownMemberNames.UnaryNegationOperatorName => 15,       // -
                // bit operations
                WellKnownMemberNames.BitwiseAndOperatorName => 16,          // &
                WellKnownMemberNames.BitwiseOrOperatorName => 17,           // |
                WellKnownMemberNames.ExclusiveOrOperatorName => 18,         // ^
                WellKnownMemberNames.LeftShiftOperatorName => 19,           // <<
                WellKnownMemberNames.RightShiftOperatorName => 20,          // >>
                WellKnownMemberNames.OnesComplementOperatorName => 21,      // ~
                // true false
                WellKnownMemberNames.FalseOperatorName => 22,               // false
                WellKnownMemberNames.TrueOperatorName => 23,                // true

                var name => throw ExceptionUtilities.UnexpectedValue(name),
            };
        }

        private static OperatorPosition GetOperatorPosition(string operatorName)
        {
            switch (operatorName)
            {
                // binary
                case WellKnownMemberNames.AdditionOperatorName:
                case WellKnownMemberNames.BitwiseAndOperatorName:
                case WellKnownMemberNames.BitwiseOrOperatorName:
                case WellKnownMemberNames.DivisionOperatorName:
                case WellKnownMemberNames.EqualityOperatorName:
                case WellKnownMemberNames.ExclusiveOrOperatorName:
                case WellKnownMemberNames.GreaterThanOperatorName:
                case WellKnownMemberNames.GreaterThanOrEqualOperatorName:
                case WellKnownMemberNames.InequalityOperatorName:
                case WellKnownMemberNames.LeftShiftOperatorName:
                case WellKnownMemberNames.LessThanOperatorName:
                case WellKnownMemberNames.LessThanOrEqualOperatorName:
                case WellKnownMemberNames.ModulusOperatorName:
                case WellKnownMemberNames.MultiplyOperatorName:
                case WellKnownMemberNames.RightShiftOperatorName:
                case WellKnownMemberNames.SubtractionOperatorName:
                    return OperatorPosition.Infix;

                // Unary
                case WellKnownMemberNames.DecrementOperatorName:
                case WellKnownMemberNames.IncrementOperatorName:
                    return OperatorPosition.Prefix | OperatorPosition.Postfix;

                case WellKnownMemberNames.FalseOperatorName:
                case WellKnownMemberNames.TrueOperatorName:
                    return OperatorPosition.None;

                case WellKnownMemberNames.LogicalNotOperatorName:
                case WellKnownMemberNames.OnesComplementOperatorName:
                case WellKnownMemberNames.UnaryNegationOperatorName:
                case WellKnownMemberNames.UnaryPlusOperatorName:
                    return OperatorPosition.Prefix;

                default:
                    throw ExceptionUtilities.UnexpectedValue(operatorName);
            }
        }
    }
}
