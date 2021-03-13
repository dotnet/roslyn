// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
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

        /// <summary>
        /// Ordered in the order we want to display operators in the completion list.
        /// </summary>
        private static readonly ImmutableArray<(string name, OperatorPosition position)> s_operatorInfo =
            ImmutableArray.Create(
                (WellKnownMemberNames.EqualityOperatorName, OperatorPosition.Infix),
                (WellKnownMemberNames.InequalityOperatorName, OperatorPosition.Infix),
                (WellKnownMemberNames.GreaterThanOperatorName, OperatorPosition.Infix),
                (WellKnownMemberNames.GreaterThanOrEqualOperatorName, OperatorPosition.Infix),
                (WellKnownMemberNames.LessThanOperatorName, OperatorPosition.Infix),
                (WellKnownMemberNames.LessThanOrEqualOperatorName, OperatorPosition.Infix),
                (WellKnownMemberNames.LogicalNotOperatorName, OperatorPosition.Prefix),
                (WellKnownMemberNames.AdditionOperatorName, OperatorPosition.Infix),
                (WellKnownMemberNames.SubtractionOperatorName, OperatorPosition.Infix),
                (WellKnownMemberNames.MultiplyOperatorName, OperatorPosition.Infix),
                (WellKnownMemberNames.DivisionOperatorName, OperatorPosition.Infix),
                (WellKnownMemberNames.ModulusOperatorName, OperatorPosition.Infix),
                (WellKnownMemberNames.IncrementOperatorName, OperatorPosition.Prefix | OperatorPosition.Postfix),
                (WellKnownMemberNames.DecrementOperatorName, OperatorPosition.Prefix | OperatorPosition.Postfix),
                (WellKnownMemberNames.UnaryPlusOperatorName, OperatorPosition.Prefix),
                (WellKnownMemberNames.UnaryNegationOperatorName, OperatorPosition.Prefix),
                (WellKnownMemberNames.BitwiseAndOperatorName, OperatorPosition.Infix),
                (WellKnownMemberNames.BitwiseOrOperatorName, OperatorPosition.Infix),
                (WellKnownMemberNames.ExclusiveOrOperatorName, OperatorPosition.Infix),
                (WellKnownMemberNames.LeftShiftOperatorName, OperatorPosition.Infix),
                (WellKnownMemberNames.RightShiftOperatorName, OperatorPosition.Infix),
                (WellKnownMemberNames.OnesComplementOperatorName, OperatorPosition.Prefix),
                (WellKnownMemberNames.FalseOperatorName, OperatorPosition.None),
                (WellKnownMemberNames.TrueOperatorName, OperatorPosition.None));

        private static readonly CompletionItemRules s_operatorRules;

        static UnnamedSymbolCompletionProvider()
        {
            using var _ = PooledHashSet<char>.GetInstance(out var filterCharacters);

            foreach (var (opName, _) in s_operatorInfo)
            {
                var opText = GetOperatorText(opName);
                foreach (var ch in opText)
                {
                    if (!char.IsLetterOrDigit(ch))
                        filterCharacters.Add(ch);
                }
            }

            var opCharacters = ImmutableArray.CreateRange(filterCharacters);
            s_operatorRules = CompletionItemRules.Default
                .WithFilterCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Add, opCharacters))
                .WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, opCharacters));
        }

        private void AddOperatorGroup(CompletionContext context, string opName, IEnumerable<ISymbol> operators)
        {
            var sortIndex = s_operatorInfo.IndexOf(i => i.name == opName);
            var displayText = GetOperatorText(opName);

            var item = SymbolCompletionItem.CreateWithSymbolId(
                displayText: displayText,
                displayTextSuffix: null,
                inlineDescription: GetOperatorInlineDescription(opName),
                filterText: displayText,
                sortText: SortText(OperatorSortingGroupIndex, $"{sortIndex:000}"),
                symbols: operators.ToImmutableArray(),
                rules: s_operatorRules,
                contextPosition: context.Position,
                properties: OperatorProperties
                    .Add(OperatorName, opName));
            context.AddItem(item);
        }

        private static string GetOperatorText(string opName)
            => SyntaxFacts.GetText(SyntaxFacts.GetOperatorKind(opName));

        private async Task<CompletionChange> GetOperatorChangeAsync(
            Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var operatorName = item.Properties[OperatorName];
            var operatorPosition = GetOperatorPosition(operatorName);

            if (operatorPosition.HasFlag(OperatorPosition.Infix))
                return await ReplaceTextAfterOperatorAsync(document, item, text: $" {item.DisplayText} ", cancellationToken).ConfigureAwait(false);

            if (operatorPosition.HasFlag(OperatorPosition.Postfix))
                return await ReplaceTextAfterOperatorAsync(document, item, text: $"{item.DisplayText} ", cancellationToken).ConfigureAwait(false);

            if (operatorPosition.HasFlag(OperatorPosition.Prefix))
            {
                var position = SymbolCompletionItem.GetContextPosition(item);
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var (dotLikeToken, expressionStart) = GetDotAndExpressionStart(root, position);

                // Place the new operator before the expression, and delete the dot.
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var replacement = item.DisplayText + text.ToString(TextSpan.FromBounds(expressionStart, dotLikeToken.SpanStart));
                var fullTextChange = new TextChange(
                    TextSpan.FromBounds(
                        expressionStart,
                        dotLikeToken.Kind() == SyntaxKind.DotDotToken ? dotLikeToken.Span.Start + 1 : dotLikeToken.Span.End),
                    replacement);

                var newPosition = expressionStart + replacement.Length;
                return CompletionChange.Create(fullTextChange, newPosition);
            }

            throw ExceptionUtilities.UnexpectedValue(operatorPosition);
        }

        private static OperatorPosition GetOperatorPosition(string operatorName)
            => s_operatorInfo.Single(t => t.name == operatorName).position;

        private static Task<CompletionDescription> GetOperatorDescriptionAsync(
            Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            return SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);
        }

        private static string GetOperatorInlineDescription(string opName)
        {
            var opText = GetOperatorText(opName);
            var position = GetOperatorPosition(opName);

            if (position.HasFlag(OperatorPosition.Postfix))
                return $"x{opText}";

            if (position.HasFlag(OperatorPosition.Infix))
                return $"x {opText} y";

            if (position.HasFlag(OperatorPosition.Prefix))
                return $"{opText}x";

            return opText;
        }
    }
}
