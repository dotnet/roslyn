// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis.Completion.Providers;
    using static WorkspacesResources;
    using RegexToken = EmbeddedSyntaxToken<RegexKind>;

    internal class RegexEmbeddedCompletionProvider : CompletionProvider
    {
        private const string StartKey = nameof(StartKey);
        private const string LengthKey = nameof(LengthKey);
        private const string NewTextKey = nameof(NewTextKey);
        private const string NewPositionKey = nameof(NewPositionKey);
        private const string DescriptionKey = nameof(DescriptionKey);

        // Always soft-select these completion items.  Also, never filter down.
        private static readonly CompletionItemRules s_rules =
            CompletionItemRules.Default.WithSelectionBehavior(CompletionItemSelectionBehavior.SoftSelection)
                                       .WithFilterCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, new char[] { }));

        private readonly RegexEmbeddedLanguageFeatures _language;

        public RegexEmbeddedCompletionProvider(RegexEmbeddedLanguageFeatures language)
        {
            _language = language;
        }

        public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
        {
            if (trigger.Kind == CompletionTriggerKind.Invoke ||
                trigger.Kind == CompletionTriggerKind.InvokeAndCommitIfUnique)
            {
                return true;
            }

            if (trigger.Kind == CompletionTriggerKind.Insertion)
            {
                return IsTriggerCharacter(trigger.Character);
            }

            return false;
        }

        private bool IsTriggerCharacter(char ch)
        {
            switch (ch)
            {
                case '\\': // any escape
                case '[':  // character class
                case '(':  // any group
                case '{':  // \p{
                    return true;
            }

            return false;
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var embeddedContext = new EmbeddedCompletionContext(context);
            await ProvideCompletionsAsync(embeddedContext).ConfigureAwait(false);

            if (embeddedContext.Items.Count == 0)
            {
                return;
            }

            var index = 0;
            foreach (var embeddedItem in embeddedContext.Items)
            {
                var change = embeddedItem.Change;
                var textChange = change.TextChange;

                var properties = ImmutableDictionary.CreateBuilder<string, string>();
                properties.Add(StartKey, textChange.Span.Start.ToString());
                properties.Add(LengthKey, textChange.Span.Length.ToString());
                properties.Add(NewTextKey, textChange.NewText);
                properties.Add(DescriptionKey, embeddedItem.Description);
                properties.Add(AbstractEmbeddedLanguageCompletionProvider.EmbeddedProviderName, this.Name);

                if (change.NewPosition != null)
                {
                    properties.Add(NewPositionKey, change.NewPosition.ToString());
                }

                // Keep everything sorted in the order the underlying embedded
                // language provided it.
                var sortText = index.ToString("0000");

                var item = CompletionItem.Create(
                    embeddedItem.DisplayText,
                    sortText: sortText,
                    properties: properties.ToImmutable(),
                    rules: s_rules);

                context.AddItem(item);
                index++;
            }

            context.IsExclusive = true;
        }

        private async Task ProvideCompletionsAsync(EmbeddedCompletionContext context)
        {
            if (!context.Options.GetOption(RegularExpressionsOptions.ProvideRegexCompletions, context.Document.Project.Language))
            {
                return;
            }

            var position = context.Position;
            var treeAndStringToken = await _language.TryGetTreeAndTokenAtPositionAsync(
                context.Document, position, context.CancellationToken).ConfigureAwait(false);
            if (treeAndStringToken == null)
            {
                return;
            }

            if (context.Trigger.Kind != CompletionTriggerKind.Invoke &&
                context.Trigger.Kind != CompletionTriggerKind.InvokeAndCommitIfUnique &&
                context.Trigger.Kind != CompletionTriggerKind.Insertion)
            {
                return;
            }

            var (tree, stringToken) = treeAndStringToken.Value;
            if (position <= stringToken.SpanStart || position >= stringToken.Span.End)
            {
                return;
            }

            // First, act as if the user just inserted the previous character.  This will cause us
            // to complete down to the set of relevant items based on that character. If we get
            // anything, we're done and can just show the user those items.  If we have no items to
            // add *and* the user was explicitly invoking completion, then just add the entire set
            // of suggestions to help the user out.
            var count = context.Items.Count;
            ProvideCompletionsAfterInsertion(context, tree, stringToken);

            if (count != context.Items.Count)
            {
                // We added items.  Nothing else to do here.
                return;
            }

            if (context.Trigger.Kind == CompletionTriggerKind.Insertion)
            {
                // The user was typing a character, and we had nothing to add for them.  Just bail
                // out immediately as we cannot help in this circumstance.
                return;
            }

            // We added no items, but the user explicitly asked for completion.  Add all the
            // items we can to help them out.
            var inCharacterClass = DetermineIfInCharacterClass(tree, context.Position);

            ProvideEscapeCompletions(context, stringToken, inCharacterClass, parentOpt: null);

            if (!inCharacterClass)
            {
                ProvideTopLevelCompletions(context, stringToken);
                ProvideCharacterClassCompletions(context, stringToken, parentOpt: null);
                ProvideGroupingCompletions(context, stringToken, parentOpt: null);
            }
        }

        private bool DetermineIfInCharacterClass(RegexTree tree, int pos)
        {
            var inCharacterClass = false;

            var virtualChar = tree.Text.FirstOrNullable(vc => vc.Span.Contains(pos));
            if (virtualChar != null)
            {
                inCharacterClass = IsInCharacterClass(tree.Root, virtualChar.Value, inCharacterClass: false);
            }

            return inCharacterClass;
        }

        private void ProvideTopLevelCompletions(
            EmbeddedCompletionContext context, SyntaxToken stringToken)
        {
            AddIfMissing(context, CreateItem(stringToken, "|", regex_alternation_short, regex_alternation_long, context, parentOpt: null));
            AddIfMissing(context, CreateItem(stringToken, "^", regex_start_of_string_or_line_short, regex_start_of_string_or_line_long, context, parentOpt: null));
            AddIfMissing(context, CreateItem(stringToken, "$", regex_end_of_string_or_line_short, regex_end_of_string_or_line_long, context, parentOpt: null));
            AddIfMissing(context, CreateItem(stringToken, ".", regex_any_character_group_short, regex_any_character_group_long, context, parentOpt: null));
            
            AddIfMissing(context, CreateItem(stringToken, "*", regex_match_zero_or_more_times_short, regex_match_zero_or_more_times_long, context, parentOpt: null));
            AddIfMissing(context, CreateItem(stringToken, "*?", regex_match_zero_or_more_times_lazy_short, regex_match_zero_or_more_times_lazy_long, context, parentOpt: null));

            AddIfMissing(context, CreateItem(stringToken, "+", regex_match_one_or_more_times_short, regex_match_one_or_more_times_long, context, parentOpt: null));
            AddIfMissing(context, CreateItem(stringToken, "+?", regex_match_one_or_more_times_lazy_short, regex_match_one_or_more_times_lazy_long, context, parentOpt: null));

            AddIfMissing(context, CreateItem(stringToken, "?", regex_match_zero_or_one_time_short, regex_match_zero_or_one_time_long, context, parentOpt: null));
            AddIfMissing(context, CreateItem(stringToken, "??", regex_match_zero_or_one_time_lazy_short, regex_match_zero_or_one_time_lazy_long, context, parentOpt: null));

            AddIfMissing(context, CreateItem(stringToken, "{n}", regex_match_exactly_n_times_short, regex_match_exactly_n_times_long, context, parentOpt: null, positionOffset: "{".Length, insertionText: "{}"));
            AddIfMissing(context, CreateItem(stringToken, "{n}?", regex_match_exactly_n_times_lazy_short, regex_match_exactly_n_times_lazy_long, context, parentOpt: null, positionOffset: "{".Length, insertionText: "{}?"));

            AddIfMissing(context, CreateItem(stringToken, "{n,}", regex_match_at_least_n_times_short, regex_match_at_least_n_times_long, context, parentOpt: null, positionOffset: "{".Length, insertionText: "{,}"));
            AddIfMissing(context, CreateItem(stringToken, "{n,}?", regex_match_at_least_n_times_lazy_short, regex_match_at_least_n_times_lazy_long, context, parentOpt: null, positionOffset: "{".Length, insertionText: "{,}?"));

            AddIfMissing(context, CreateItem(stringToken, "{m,n}", regex_match_between_m_and_n_times_short, regex_match_between_m_and_n_times_long, context, parentOpt: null, positionOffset: "{".Length, insertionText: "{,}"));
            AddIfMissing(context, CreateItem(stringToken, "{m,n}?", regex_match_between_m_and_n_times_lazy_short, regex_match_between_m_and_n_times_lazy_long, context, parentOpt: null, positionOffset: "{".Length, insertionText: "{,}?"));

            AddIfMissing(context, CreateItem(stringToken, "#", regex_end_of_line_comment_short, regex_end_of_line_comment_long, context, parentOpt: null));
        }

        private void ProvideCompletionsAfterInsertion(
            EmbeddedCompletionContext context, RegexTree tree, SyntaxToken stringToken)
        {
            var position = context.Position;
            var previousVirtualCharOpt = tree.Text.FirstOrNullable(vc => vc.Span.Contains(position - 1));
            if (previousVirtualCharOpt == null)
            {
                return;
            }

            var previousVirtualChar = previousVirtualCharOpt.Value;
            var result = FindToken(tree.Root, previousVirtualChar);
            if (result == null)
            {
                return;
            }

            var (parent, token) = result.Value;
            var inCharacterClass = IsInCharacterClass(tree.Root, previousVirtualChar, inCharacterClass: false);

            if (token.Kind == RegexKind.BackslashToken)
            {
                ProvideEscapeCompletions(context, stringToken, inCharacterClass, parent);
                return;
            }

            // see if we have ```\p{```.  If so, offer property categories
            if (previousVirtualChar.Char == '{')
            {
                ProvideCompletionsIfInUnicodeCategory(context, tree, previousVirtualChar);
                return;
            }

            if (inCharacterClass)
            {
                // Nothing more to offer if we're in a character class.
                return;
            }

            switch (token.Kind)
            {
                case RegexKind.OpenBracketToken:
                    ProvideCharacterClassCompletions(context, stringToken, parent);
                    return;
                case RegexKind.OpenParenToken:
                    ProvideGroupingCompletions(context, stringToken, parent);
                    return;
            }
        }

        private void ProvideCompletionsIfInUnicodeCategory(
            EmbeddedCompletionContext context, RegexTree tree, VirtualChar previousVirtualChar)
        {
            var index = tree.Text.IndexOf(previousVirtualChar);
            if (index >= 2 &&
                tree.Text[index - 2].Char == '\\' &&
                tree.Text[index - 1].Char == 'p')
            {
                var slashChar = tree.Text[index - 1];
                var result = FindToken(tree.Root, slashChar);
                if (result == null)
                {
                    return;
                }

                var (parent, token) = result.Value;
                if (parent is RegexEscapeNode)
                {
                    ProvideEscapeCategoryCompletions(context);
                }
            }

            // ProvideEscapeCategoryCompletions(context);
            return;
        }

        private void ProvideGroupingCompletions(
            EmbeddedCompletionContext context, SyntaxToken stringToken, RegexNode parentOpt)
        {
            if (parentOpt != null && !(parentOpt is RegexGroupingNode))
            {
                return;
            }

            AddIfMissing(context, CreateItem(stringToken, $"(  {regex_subexpression}  )", regex_matched_subexpression_short, regex_matched_subexpression_long, context, parentOpt, positionOffset: "(".Length, insertionText: "()"));
            AddIfMissing(context, CreateItem(stringToken, $"(?<  {regex_name}  >  {regex_subexpression}  )", regex_named_matched_subexpression_short, regex_named_matched_subexpression_long, context, parentOpt, positionOffset: "(?<".Length, insertionText: "(?<>)"));
            AddIfMissing(context, CreateItem(stringToken, $"(?<  {regex_name1}  -  {regex_name2}  >  {regex_subexpression}  )", regex_balancing_group_short, regex_balancing_group_long, context, parentOpt, positionOffset: "(?<".Length, insertionText: "(?<->)"));
            AddIfMissing(context, CreateItem(stringToken, $"(?:  {regex_subexpression}  )", regex_noncapturing_group_short, regex_noncapturing_group_long, context, parentOpt, positionOffset: "(?:".Length, insertionText: "(?:)"));
            AddIfMissing(context, CreateItem(stringToken, $"(?=  {regex_subexpression}  )", regex_zero_width_positive_lookahead_assertion_short, regex_zero_width_positive_lookahead_assertion_long, context, parentOpt, positionOffset: "(?=".Length, insertionText: "(?=)"));
            AddIfMissing(context, CreateItem(stringToken, $"(?!  {regex_subexpression}  )", regex_zero_width_negative_lookahead_assertion_short, regex_zero_width_negative_lookahead_assertion_long, context, parentOpt, positionOffset: "(?!".Length, insertionText: "(?!)"));
            AddIfMissing(context, CreateItem(stringToken, $"(?<=  {regex_subexpression}  )", regex_zero_width_positive_lookbehind_assertion_short, regex_zero_width_positive_lookbehind_assertion_long, context, parentOpt, positionOffset: "(?<=".Length, insertionText: "(?<=)"));
            AddIfMissing(context, CreateItem(stringToken, $"(?<!  {regex_subexpression}  )", regex_zero_width_negative_lookbehind_assertion_short, regex_zero_width_negative_lookbehind_assertion_long, context, parentOpt, positionOffset: "(?<!".Length, insertionText: "(?<!)"));
            AddIfMissing(context, CreateItem(stringToken, $"(?>  {regex_subexpression}  )", regex_nonbacktracking_subexpression_short, regex_nonbacktracking_subexpression_long, context, parentOpt, positionOffset: "(?>".Length, insertionText: "(?>)"));

            AddIfMissing(context, CreateItem(stringToken, $"(?(  {regex_expression}  )  {regex_yes}  |  {regex_no}  )", regex_conditional_expression_match_short, regex_conditional_expression_match_long, context, parentOpt, positionOffset: "(?(".Length, insertionText: "(?()|)"));
            AddIfMissing(context, CreateItem(stringToken, $"(?(  {regex_name_or_number}  )  {regex_yes}  |  {regex_no}  )", regex_conditional_group_match_short, regex_conditional_group_match_long, context, parentOpt, positionOffset: "(?(".Length, insertionText: "(?()|)"));

            AddIfMissing(context, CreateItem(stringToken, $"(?#  {regex_comment}  )", regex_inline_comment_short, regex_inline_comment_long, context, parentOpt, positionOffset: "(?#".Length, insertionText: "(?#)"));
            AddIfMissing(context, CreateItem(stringToken, $"(?imnsx-imnsx)", regex_inline_options_short, regex_inline_options_long, context, parentOpt, positionOffset: "(?".Length, insertionText: "(?)"));
            AddIfMissing(context, CreateItem(stringToken, $"(?imnsx-imnsx:  {regex_subexpression}  )", regex_group_options_short, regex_group_options_long, context, parentOpt, positionOffset: "(?".Length, insertionText: "(?:)"));
        }

        private void ProvideCharacterClassCompletions(
            EmbeddedCompletionContext context, SyntaxToken stringToken, RegexNode parentOpt)
        {
            AddIfMissing(context, CreateItem(stringToken, $"[  {regex_character_group}  ]", regex_positive_character_group_short, regex_positive_character_group_long, context, parentOpt, positionOffset: "[".Length, insertionText: "[]"));
            AddIfMissing(context, CreateItem(stringToken, $"[^  {regex_character_group}  ]", regex_negative_character_group_short, regex_negative_character_group_long, context, parentOpt, positionOffset: "[^".Length, insertionText: "[^]"));
            AddIfMissing(context, CreateItem(stringToken, $"[  {regex_base_group}  -[  {regex_excluded_group}  ]", regex_character_class_subtraction_short, regex_character_class_subtraction_long, context, parentOpt, positionOffset: "[".Length, insertionText: "[-[]]"));
        }

        private void ProvideEscapeCategoryCompletions(EmbeddedCompletionContext context)
        {
            foreach (var (name, (shortDesc, longDesc)) in RegexCharClass.EscapeCategories)
            {
                var displayText = name;
                if (displayText.StartsWith("_"))
                {
                    continue;
                }

                if (shortDesc != "")
                {
                    displayText += $"  -  {shortDesc}";
                }

                var description = longDesc.Length > 0
                    ? longDesc
                    : string.Format(regex_unicode_general_category_0, name);

                AddIfMissing(context, new RegexItem(
                    displayText,
                    description,
                    change: CompletionChange.Create(
                        new TextChange(new TextSpan(context.Position, 0), name), newPosition: null)));
            }
        }

        private void ProvideEscapeCompletions(
            EmbeddedCompletionContext context, SyntaxToken stringToken,
            bool inCharacterClass, RegexNode parentOpt)
        {
            if (parentOpt != null && !(parentOpt is RegexEscapeNode))
            {
                return;
            }

            if (!inCharacterClass)
            {
                AddIfMissing(context, CreateItem(stringToken, @"\A", regex_start_of_string_only_short, regex_start_of_string_only_long, context, parentOpt));
                AddIfMissing(context, CreateItem(stringToken, @"\b", regex_word_boundary_short, regex_word_boundary_long, context, parentOpt));
                AddIfMissing(context, CreateItem(stringToken, @"\B", regex_non_word_boundary_short, regex_non_word_boundary_long, context, parentOpt));
                AddIfMissing(context, CreateItem(stringToken, @"\G", regex_contiguous_matches_short, regex_contiguous_matches_long, context, parentOpt));
                AddIfMissing(context, CreateItem(stringToken, @"\z", regex_end_of_string_only_short, regex_end_of_string_only_long, context, parentOpt));
                AddIfMissing(context, CreateItem(stringToken, @"\Z", regex_end_of_string_or_before_ending_newline_short, regex_end_of_string_or_before_ending_newline_long, context, parentOpt));

                AddIfMissing(context, CreateItem(stringToken, $@"\k<  {regex_name_or_number}  >", regex_named_backreference_short, regex_named_backreference_long, context, parentOpt, @"\k<".Length, insertionText: @"\k<>"));
                // AddIfMissing(context, CreateItem(stringToken, @"\<>", "", "", context, parentOpt, @"\<".Length));
                AddIfMissing(context, CreateItem(stringToken, @"\1-9", regex_numbered_backreference_short, regex_numbered_backreference_long, context, parentOpt, @"\".Length, @"\"));
            }

            AddIfMissing(context, CreateItem(stringToken, @"\a", regex_bell_character_short, regex_bell_character_long, context, parentOpt));
            AddIfMissing(context, CreateItem(stringToken, @"\b", regex_backspace_character_short, regex_backspace_character_long, context, parentOpt));
            AddIfMissing(context, CreateItem(stringToken, @"\e", regex_escape_character_short, regex_escape_character_long, context, parentOpt));
            AddIfMissing(context, CreateItem(stringToken, @"\f", regex_form_feed_character_short, regex_form_feed_character_long, context, parentOpt));
            AddIfMissing(context, CreateItem(stringToken, @"\n", regex_new_line_character_short, regex_new_line_character_long, context, parentOpt));
            AddIfMissing(context, CreateItem(stringToken, @"\r", regex_carriage_return_character_short, regex_carriage_return_character_long, context, parentOpt));
            AddIfMissing(context, CreateItem(stringToken, @"\t", regex_tab_character_short, regex_tab_character_long, context, parentOpt));
            AddIfMissing(context, CreateItem(stringToken, @"\v", regex_vertical_tab_character_short, regex_vertical_tab_character_long, context, parentOpt));

            AddIfMissing(context, CreateItem(stringToken, @"\x##", regex_hexadecimal_escape_short, regex_hexadecimal_escape_long, context, parentOpt, @"\x".Length, @"\x"));
            AddIfMissing(context, CreateItem(stringToken, @"\u####", regex_unicode_escape_short, regex_unicode_escape_long, context, parentOpt, @"\u".Length, @"\u"));
            AddIfMissing(context, CreateItem(stringToken, @"\cX", regex_control_character_short, regex_control_character_long, context, parentOpt, @"\c".Length, @"\c"));

            AddIfMissing(context, CreateItem(stringToken, @"\d", regex_decimal_digit_character_short, regex_decimal_digit_character_long, context, parentOpt));
            AddIfMissing(context, CreateItem(stringToken, @"\D", regex_non_digit_character_short, regex_non_digit_character_long, context, parentOpt));
            AddIfMissing(context, CreateItem(stringToken, @"\p{...}", regex_unicode_category_short, regex_unicode_category_long, context, parentOpt, @"\p".Length, @"\p"));
            AddIfMissing(context, CreateItem(stringToken, @"\P{...}", regex_negative_unicode_category_short, regex_negative_unicode_category_long, context, parentOpt, @"\P".Length, @"\P"));
            AddIfMissing(context, CreateItem(stringToken, @"\s", regex_white_space_character_short, regex_white_space_character_long, context, parentOpt));
            AddIfMissing(context, CreateItem(stringToken, @"\S", regex_non_white_space_character_short, regex_non_white_space_character_long, context, parentOpt));
            AddIfMissing(context, CreateItem(stringToken, @"\w", regex_word_character_short, regex_word_character_long, context, parentOpt));
            AddIfMissing(context, CreateItem(stringToken, @"\W", regex_non_word_character_short, regex_non_word_character_long, context, parentOpt));
        }

        private void AddIfMissing(EmbeddedCompletionContext context, RegexItem item)
        {
            if (context.Names.Add(item.DisplayText))
            {
                context.Items.Add(item);
            }
        }

        private RegexItem CreateItem(
            SyntaxToken stringToken, string displayText, 
            string shortDescription, string longDescription,
            EmbeddedCompletionContext context, RegexNode parentOpt, 
            int? positionOffset = null, string insertionText = null)
        {
            var replacementStart = parentOpt != null
                ? parentOpt.GetSpan().Start
                : context.Position;

            var replacementSpan = TextSpan.FromBounds(replacementStart, context.Position);
            var newPosition = replacementStart + positionOffset;

            insertionText = insertionText ?? displayText;
            var escapedInsertionText = _language.EscapeText(insertionText, stringToken);
            if (shortDescription != "")
            {
                displayText += $"  -  {shortDescription}";
            }

            if (escapedInsertionText != insertionText)
            {
                newPosition += escapedInsertionText.Length - insertionText.Length;
            }

            return new RegexItem(
                displayText, longDescription, 
                CompletionChange.Create(
                    new TextChange(replacementSpan, escapedInsertionText),
                    newPosition));
        }

        private (RegexNode parent, RegexToken Token)? FindToken(
            RegexNode parent, VirtualChar ch)
        {
            foreach (var child in parent)
            {
                if (child.IsNode)
                {
                    var result = FindToken(child.Node, ch);
                    if (result != null)
                    {
                        return result;
                    }
                }
                else
                {
                    if (child.Token.VirtualChars.Contains(ch))
                    {
                        return (parent, child.Token);
                    }
                }
            }

            return null;
        }

        private bool IsInCharacterClass(RegexNode parent, VirtualChar ch, bool inCharacterClass)
        {
            foreach (var child in parent)
            {
                if (child.IsNode)
                {
                    var result = IsInCharacterClass(child.Node, ch, inCharacterClass || parent is RegexBaseCharacterClassNode);
                    if (result)
                    {
                        return result;
                    }
                }
                else
                {
                    if (child.Token.VirtualChars.Contains(ch))
                    {
                        return inCharacterClass;
                    }
                }
            }

            return false;
        }

        public override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
        {
            if (!item.Properties.TryGetValue(StartKey, out var startString) ||
                !item.Properties.TryGetValue(LengthKey, out var lengthString) ||
                !item.Properties.TryGetValue(NewTextKey, out var newText))
            {
                return SpecializedTasks.Default<CompletionChange>();
            }

            item.Properties.TryGetValue(NewPositionKey, out var newPositionString);

            return Task.FromResult(CompletionChange.Create(
                new TextChange(new TextSpan(int.Parse(startString), int.Parse(lengthString)), newText),
                newPositionString == null ? default(int?) : int.Parse(newPositionString)));
        }

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            if (!item.Properties.TryGetValue(DescriptionKey, out var description))
            {
                return SpecializedTasks.Default<CompletionDescription>();
            }

            return Task.FromResult(CompletionDescription.Create(
                ImmutableArray.Create(new TaggedText(TextTags.Text, description))));
        }
    }

    internal class EmbeddedCompletionContext
    {
        private readonly CompletionContext _context;

        public readonly List<RegexItem> Items = new List<RegexItem>();
        public readonly HashSet<string> Names = new HashSet<string>();

        public EmbeddedCompletionContext(CompletionContext context)
        {
            _context = context;
        }

        public int Position => _context.Position;
        public OptionSet Options => _context.Options;
        public Document Document => _context.Document;
        public CompletionTrigger Trigger => _context.Trigger;
        public CancellationToken CancellationToken => _context.CancellationToken;
    }

    internal struct RegexItem
    {
        public readonly string DisplayText;
        public readonly string Description;
        public readonly CompletionChange Change;

        public RegexItem(
            string displayText, string description, CompletionChange change)
        {
            DisplayText = displayText;
            Description = description;
            Change = change;
        }
    }
}
