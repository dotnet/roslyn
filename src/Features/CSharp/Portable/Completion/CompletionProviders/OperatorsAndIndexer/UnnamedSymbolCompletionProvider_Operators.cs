// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
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

        // Place operators after conversions.
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
                (WellKnownMemberNames.UnsignedRightShiftOperatorName, OperatorPosition.Infix),
                (WellKnownMemberNames.OnesComplementOperatorName, OperatorPosition.Prefix));

        /// <summary>
        /// Mapping from operator name to info about it.
        /// </summary>
        private static readonly Dictionary<string, (int sortOrder, OperatorPosition position)> s_operatorNameToInfo = new();

        private static readonly CompletionItemRules s_operatorRules;

        static UnnamedSymbolCompletionProvider()
        {
            // Collect all the characters used in C# operators and make them filter characters and not commit
            // characters. We want people to be able to write `x.=` and have that filter down to operators like `==` and
            // `!=` so they can select and commit them.
            using var _ = PooledHashSet<char>.GetInstance(out var filterCharacters);

            for (var i = 0; i < s_operatorInfo.Length; i++)
            {
                var (opName, position) = s_operatorInfo[i];
                var opText = GetOperatorText(opName);
                s_operatorNameToInfo[opName] = (sortOrder: i, position);

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
            if (!s_operatorNameToInfo.TryGetValue(opName, out var sortOrderAndPosition))
                return;

            var displayText = GetOperatorText(opName);

            context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
                displayText: displayText,
                displayTextSuffix: null,
                inlineDescription: GetOperatorInlineDescription(opName),
                filterText: displayText,
                sortText: SortText(OperatorSortingGroupIndex, $"{sortOrderAndPosition.sortOrder:000}"),
                symbols: operators.ToImmutableArray(),
                rules: s_operatorRules,
                contextPosition: context.Position,
                properties: OperatorProperties.Add(OperatorName, opName),
                isComplexTextEdit: true));
        }

        private static string GetOperatorText(string opName)
            => SyntaxFacts.GetText(SyntaxFacts.GetOperatorKind(opName));

        private async Task<CompletionChange> GetOperatorChangeAsync(
            Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var opName = item.Properties[OperatorName];
            var opPosition = GetOperatorPosition(opName);

            if (opPosition.HasFlag(OperatorPosition.Infix))
                return await ReplaceTextAfterOperatorAsync(document, item, text: $" {item.DisplayText} ", cancellationToken).ConfigureAwait(false);

            if (opPosition.HasFlag(OperatorPosition.Postfix))
                return await ReplaceTextAfterOperatorAsync(document, item, text: $"{item.DisplayText} ", cancellationToken).ConfigureAwait(false);

            if (opPosition.HasFlag(OperatorPosition.Prefix))
            {
                var position = SymbolCompletionItem.GetContextPosition(item);
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var (dotLikeToken, expressionStart) = GetDotAndExpressionStart(root, position, cancellationToken);

                // Place the new operator before the expression, and delete the dot.
                var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                var replacement = item.DisplayText + text.ToString(TextSpan.FromBounds(expressionStart, dotLikeToken.SpanStart));
                var fullTextChange = new TextChange(
                    TextSpan.FromBounds(
                        expressionStart,
                        dotLikeToken.Kind() == SyntaxKind.DotDotToken ? dotLikeToken.Span.Start + 1 : dotLikeToken.Span.End),
                    replacement);

                var newPosition = expressionStart + replacement.Length;
                return CompletionChange.Create(fullTextChange, newPosition);
            }

            throw ExceptionUtilities.UnexpectedValue(opPosition);
        }

        private static OperatorPosition GetOperatorPosition(string operatorName)
            => s_operatorNameToInfo[operatorName].position;

        private static Task<CompletionDescription> GetOperatorDescriptionAsync(Document document, CompletionItem item, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, displayOptions, cancellationToken);

        private static string GetOperatorInlineDescription(string opName)
        {
            var opText = GetOperatorText(opName);
            var opPosition = GetOperatorPosition(opName);

            if (opPosition.HasFlag(OperatorPosition.Postfix))
                return $"x{opText}";

            if (opPosition.HasFlag(OperatorPosition.Infix))
                return $"x {opText} y";

            if (opPosition.HasFlag(OperatorPosition.Prefix))
                return $"{opText}x";

            return opText;
        }
    }
}
