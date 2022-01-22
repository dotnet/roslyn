// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Completion
{
    public enum CompletionItemSelectionBehavior
    {
        Default,

        /// <summary>
        /// If no text has been typed, the item should be soft selected. This is appropriate for 
        /// completion providers that want to provide suggestions that shouldn't interfere with 
        /// typing.  For example a provider that comes up on space might offer items that are soft
        /// selected so that an additional space (or other puntuation character) will not then 
        /// commit that item.
        /// </summary>
        SoftSelection,

        /// <summary>
        /// If no text has been typed, the item should be hard selected.  This is appropriate for
        /// completion providers that are providing suggestions the user is nearly certain to 
        /// select.  Because the item is hard selected, any commit characters typed after it will
        /// cause it to be committed.
        /// </summary>
        HardSelection,
    }

    /// <summary>
    /// Rules for how the individual items are handled.
    /// </summary>
    public sealed class CompletionItemRules
    {
        /// <summary>
        /// The rule used when no rule is specified when constructing a <see cref="CompletionItem"/>.
        /// </summary>
        public static CompletionItemRules Default =
            new(
                filterCharacterRules: default,
                commitCharacterRules: default,
                enterKeyRule: EnterKeyRule.Default,
                formatOnCommit: false,
                matchPriority: Completion.MatchPriority.Default,
                selectionBehavior: CompletionItemSelectionBehavior.Default);

        /// <summary>
        /// Rules that modify the set of characters that can be typed to filter the list of completion items.
        /// </summary>
        public ImmutableArray<CharacterSetModificationRule> FilterCharacterRules { get; }

        /// <summary>
        /// Rules that modify the set of characters that can be typed to cause the selected item to be committed.
        /// </summary>
        public ImmutableArray<CharacterSetModificationRule> CommitCharacterRules { get; }

        /// <summary>
        /// A rule about whether the enter key is passed through to the editor after the selected item has been committed.
        /// </summary>
        public EnterKeyRule EnterKeyRule { get; }

        /// <summary>
        /// True if the modified text should be formatted automatically.
        /// </summary>
        public bool FormatOnCommit { get; }

        /// <summary>
        /// True if the related completion item should be initially selected.
        /// </summary>
        public int MatchPriority { get; }

        /// <summary>
        /// How this item should be selected when the completion list first appears and
        /// before the user has typed any characters.
        /// </summary>
        public CompletionItemSelectionBehavior SelectionBehavior { get; }

        private CompletionItemRules(
            ImmutableArray<CharacterSetModificationRule> filterCharacterRules,
            ImmutableArray<CharacterSetModificationRule> commitCharacterRules,
            EnterKeyRule enterKeyRule,
            bool formatOnCommit,
            int matchPriority,
            CompletionItemSelectionBehavior selectionBehavior)
        {
            FilterCharacterRules = filterCharacterRules.NullToEmpty();
            CommitCharacterRules = commitCharacterRules.NullToEmpty();
            EnterKeyRule = enterKeyRule;
            FormatOnCommit = formatOnCommit;
            MatchPriority = matchPriority;
            SelectionBehavior = selectionBehavior;
        }

        /// <summary>
        /// Creates a new <see cref="CompletionItemRules"/> instance.
        /// </summary>
        /// <param name="filterCharacterRules">Rules about which keys typed are used to filter the list of completion items.</param>
        /// <param name="commitCharacterRules">Rules about which keys typed caused the completion item to be committed.</param>
        /// <param name="enterKeyRule">Rule about whether the enter key is passed through to the editor after the selected item has been committed.</param>
        /// <param name="formatOnCommit">True if the modified text should be formatted automatically.</param>
        /// <param name="matchPriority">True if the related completion item should be initially selected.</param>
        /// <returns></returns>
        public static CompletionItemRules Create(
            ImmutableArray<CharacterSetModificationRule> filterCharacterRules,
            ImmutableArray<CharacterSetModificationRule> commitCharacterRules,
            EnterKeyRule enterKeyRule,
            bool formatOnCommit,
            int? matchPriority)
        {
            return Create(
                filterCharacterRules, commitCharacterRules,
                enterKeyRule, formatOnCommit, matchPriority,
                selectionBehavior: CompletionItemSelectionBehavior.Default);
        }

        /// <summary>
        /// Creates a new <see cref="CompletionItemRules"/> instance.
        /// </summary>
        /// <param name="filterCharacterRules">Rules about which keys typed are used to filter the list of completion items.</param>
        /// <param name="commitCharacterRules">Rules about which keys typed caused the completion item to be committed.</param>
        /// <param name="enterKeyRule">Rule about whether the enter key is passed through to the editor after the selected item has been committed.</param>
        /// <param name="formatOnCommit">True if the modified text should be formatted automatically.</param>
        /// <param name="matchPriority">True if the related completion item should be initially selected.</param>
        /// <param name="selectionBehavior">How this item should be selected if no text has been typed after the completion list is brought up.</param>
        /// <returns></returns>
        public static CompletionItemRules Create(
            ImmutableArray<CharacterSetModificationRule> filterCharacterRules = default,
            ImmutableArray<CharacterSetModificationRule> commitCharacterRules = default,
            EnterKeyRule enterKeyRule = EnterKeyRule.Default,
            bool formatOnCommit = false,
            int? matchPriority = null,
            CompletionItemSelectionBehavior selectionBehavior = CompletionItemSelectionBehavior.Default)
        {
            if (filterCharacterRules.IsDefaultOrEmpty &&
                commitCharacterRules.IsDefaultOrEmpty &&
                enterKeyRule == Default.EnterKeyRule &&
                formatOnCommit == Default.FormatOnCommit &&
                matchPriority.GetValueOrDefault() == Default.MatchPriority &&
                selectionBehavior == Default.SelectionBehavior)
            {
                return Default;
            }
            else
            {
                return new CompletionItemRules(
                    filterCharacterRules, commitCharacterRules, enterKeyRule, formatOnCommit,
                    matchPriority.GetValueOrDefault(), selectionBehavior);
            }
        }

        /// <summary>
        /// Creates a new <see cref="CompletionItemRules"/> instance--internal for TypeScript.
        /// </summary>
        /// <param name="filterCharacterRules">Rules about which keys typed are used to filter the list of completion items.</param>
        /// <param name="commitCharacterRules">Rules about which keys typed caused the completion item to be committed.</param>
        /// <param name="enterKeyRule">Rule about whether the enter key is passed through to the editor after the selected item has been committed.</param>
        /// <param name="formatOnCommit">True if the modified text should be formatted automatically.</param>
        /// <param name="preselect">True if the related completion item should be initially selected.</param>
        /// <returns></returns>
        internal static CompletionItemRules Create(
            ImmutableArray<CharacterSetModificationRule> filterCharacterRules,
            ImmutableArray<CharacterSetModificationRule> commitCharacterRules,
            EnterKeyRule enterKeyRule,
            bool formatOnCommit,
            bool preselect)
        {
            var matchPriority = preselect ? Completion.MatchPriority.Preselect : Completion.MatchPriority.Default;
            return CompletionItemRules.Create(filterCharacterRules, commitCharacterRules, enterKeyRule, formatOnCommit, matchPriority);
        }

        private CompletionItemRules With(
            Optional<ImmutableArray<CharacterSetModificationRule>> filterRules = default,
            Optional<ImmutableArray<CharacterSetModificationRule>> commitRules = default,
            Optional<EnterKeyRule> enterKeyRule = default,
            Optional<bool> formatOnCommit = default,
            Optional<int> matchPriority = default,
            Optional<CompletionItemSelectionBehavior> selectionBehavior = default)
        {
            var newFilterRules = filterRules.HasValue ? filterRules.Value : FilterCharacterRules;
            var newCommitRules = commitRules.HasValue ? commitRules.Value : CommitCharacterRules;
            var newEnterKeyRule = enterKeyRule.HasValue ? enterKeyRule.Value : EnterKeyRule;
            var newFormatOnCommit = formatOnCommit.HasValue ? formatOnCommit.Value : FormatOnCommit;
            var newMatchPriority = matchPriority.HasValue ? matchPriority.Value : MatchPriority;
            var newSelectionBehavior = selectionBehavior.HasValue ? selectionBehavior.Value : SelectionBehavior;

            if (newFilterRules == FilterCharacterRules &&
                newCommitRules == CommitCharacterRules &&
                newEnterKeyRule == EnterKeyRule &&
                newFormatOnCommit == FormatOnCommit &&
                newMatchPriority == MatchPriority &&
                newSelectionBehavior == SelectionBehavior)
            {
                return this;
            }
            else
            {
                return Create(
                    newFilterRules, newCommitRules,
                    newEnterKeyRule, newFormatOnCommit,
                    newMatchPriority, newSelectionBehavior);
            }
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItemRules"/> with the <see cref="FilterCharacterRules"/> property changed.
        /// </summary>
        public CompletionItemRules WithFilterCharacterRules(ImmutableArray<CharacterSetModificationRule> filterCharacterRules)
            => With(filterRules: filterCharacterRules);

        internal CompletionItemRules WithFilterCharacterRule(CharacterSetModificationRule rule)
            => With(filterRules: ImmutableArray.Create(rule));

        internal CompletionItemRules WithCommitCharacterRule(CharacterSetModificationRule rule)
            => With(commitRules: ImmutableArray.Create(rule));

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItemRules"/> with the <see cref="CommitCharacterRules"/> property changed.
        /// </summary>
        public CompletionItemRules WithCommitCharacterRules(ImmutableArray<CharacterSetModificationRule> commitCharacterRules)
            => With(commitRules: commitCharacterRules);

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItemRules"/> with the <see cref="EnterKeyRule"/> property changed.
        /// </summary>
        public CompletionItemRules WithEnterKeyRule(EnterKeyRule enterKeyRule)
            => With(enterKeyRule: enterKeyRule);

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItemRules"/> with the <see cref="FormatOnCommit"/> property changed.
        /// </summary>
        public CompletionItemRules WithFormatOnCommit(bool formatOnCommit)
            => With(formatOnCommit: formatOnCommit);

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItemRules"/> with the <see cref="MatchPriority"/> property changed.
        /// </summary>
        public CompletionItemRules WithMatchPriority(int matchPriority)
            => With(matchPriority: matchPriority);

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItemRules"/> with the <see cref="SelectionBehavior"/> property changed.
        /// </summary>
        public CompletionItemRules WithSelectionBehavior(CompletionItemSelectionBehavior selectionBehavior)
            => With(selectionBehavior: selectionBehavior);
    }
}
