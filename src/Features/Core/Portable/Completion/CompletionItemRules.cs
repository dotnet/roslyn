// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// Rules for how the individual items are handled.
    /// </summary>
    public sealed class CompletionItemRules
    {
        /// <summary>
        /// The rule used when no rule is specified when constructing a <see cref="CompletionItem"/>.
        /// </summary>
        public static CompletionItemRules Default = 
            new CompletionItemRules(
                filterCharacterRules: default(ImmutableArray<CharacterSetModificationRule>), 
                commitCharacterRules: default(ImmutableArray<CharacterSetModificationRule>),
                enterKeyRule: EnterKeyRule.Default, 
                formatOnCommit: false, 
                preselect: false);

        /// <summary>
        /// Rules that modify the set of characters that can be typed to filter the list of completion items.
        /// </summary>
        public ImmutableArray<CharacterSetModificationRule> FilterCharacterRules { get; }

        /// <summary>
        /// Rules that modify the set of characters that can be typed to cause the selected item to be commited.
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
        public bool Preselect { get; }

        private CompletionItemRules(
            ImmutableArray<CharacterSetModificationRule> filterCharacterRules,
            ImmutableArray<CharacterSetModificationRule> commitCharacterRules,
            EnterKeyRule enterKeyRule,
            bool formatOnCommit,
            bool preselect)
        {
            this.FilterCharacterRules = filterCharacterRules.IsDefault ? ImmutableArray<CharacterSetModificationRule>.Empty : filterCharacterRules;
            this.CommitCharacterRules = commitCharacterRules.IsDefault ? ImmutableArray<CharacterSetModificationRule>.Empty : commitCharacterRules;
            this.EnterKeyRule = enterKeyRule;
            this.FormatOnCommit = formatOnCommit;
            this.Preselect = preselect;
        }

        /// <summary>
        /// Creates a new <see cref="CompletionItemRules"/> instance.
        /// </summary>
        /// <param name="filterCharacterRules">Rules about which keys typed are used to filter the list of completion items.</param>
        /// <param name="commitCharacterRules">Rules about which keys typed caused the completion item to be committed.</param>
        /// <param name="enterKeyRule">Rule about whether the enter key is passed through to the editor after the selected item has been committed.</param>
        /// <param name="formatOnCommit">True if the modified text should be formatted automatically.</param>
        /// <param name="preselect">True if the related completion item should be initially selected.</param>
        /// <returns></returns>
        public static CompletionItemRules Create(
            ImmutableArray<CharacterSetModificationRule> filterCharacterRules = default(ImmutableArray<CharacterSetModificationRule>),
            ImmutableArray<CharacterSetModificationRule> commitCharacterRules = default(ImmutableArray<CharacterSetModificationRule>),
            EnterKeyRule enterKeyRule = EnterKeyRule.Default,
            bool formatOnCommit = false,
            bool preselect = false)
        {
            if (filterCharacterRules.IsDefaultOrEmpty
                && commitCharacterRules.IsDefaultOrEmpty
                && enterKeyRule == Default.EnterKeyRule
                && formatOnCommit == Default.FormatOnCommit
                && preselect == Default.Preselect)
            {
                return Default;
            }
            else
            {
                return new CompletionItemRules(filterCharacterRules, commitCharacterRules, enterKeyRule, formatOnCommit, preselect);
            }
        }

        private CompletionItemRules With(
            Optional<ImmutableArray<CharacterSetModificationRule>> filterRules = default(Optional<ImmutableArray<CharacterSetModificationRule>>),
            Optional<ImmutableArray<CharacterSetModificationRule>> commitRules = default(Optional<ImmutableArray<CharacterSetModificationRule>>),
            Optional<EnterKeyRule> enterKeyRule = default(Optional<EnterKeyRule>),
            Optional<bool> formatOnCommit = default(Optional<bool>),
            Optional<bool> preselect = default(Optional<bool>))
        {
            var newFilterRules = filterRules.HasValue ? filterRules.Value : this.FilterCharacterRules;
            var newCommitRules = commitRules.HasValue ? commitRules.Value : this.CommitCharacterRules;
            var newEnterKeyRule = enterKeyRule.HasValue ? enterKeyRule.Value : this.EnterKeyRule;
            var newFormatOnCommit = formatOnCommit.HasValue ? formatOnCommit.Value : this.FormatOnCommit;
            var newPreselect = preselect.HasValue ? preselect.Value : this.Preselect;

            if (newFilterRules == this.FilterCharacterRules &&
                newCommitRules == this.CommitCharacterRules &&
                newEnterKeyRule == this.EnterKeyRule &&
                newFormatOnCommit == this.FormatOnCommit &&
                newPreselect == this.Preselect)
            {
                return this;
            }
            else
            { 
                return Create(newFilterRules, newCommitRules, newEnterKeyRule, newFormatOnCommit, newPreselect);
            }
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItemRules"/> with the <see cref="FilterCharacterRules"/> property changed.
        /// </summary>
        public CompletionItemRules WithFilterCharacterRules(ImmutableArray<CharacterSetModificationRule> filterCharacterRules)
        {
            return this.With(filterRules: filterCharacterRules);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItemRules"/> with the <see cref="CommitCharacterRules"/> property changed.
        /// </summary>
        public CompletionItemRules WithCommitCharacterRules(ImmutableArray<CharacterSetModificationRule> commitCharacterRules)
        {
            return this.With(commitRules: commitCharacterRules);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItemRules"/> with the <see cref="EnterKeyRule"/> property changed.
        /// </summary>
        public CompletionItemRules WithEnterKeyRule(EnterKeyRule enterKeyRule)
        {
            return this.With(enterKeyRule: enterKeyRule);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItemRules"/> with the <see cref="FormatOnCommit"/> property changed.
        /// </summary>
        public CompletionItemRules WithFormatOnCommit(bool formatOnCommit)
        {
            return this.With(formatOnCommit: formatOnCommit);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItemRules"/> with the <see cref="Preselect"/> property changed.
        /// </summary>
        public CompletionItemRules WithPreselect(bool preselect)
        {
            return this.With(preselect: preselect);
        }
    }
}
