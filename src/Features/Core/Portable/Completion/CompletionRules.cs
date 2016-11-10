﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// Presentation and behavior rules for completion.
    /// </summary>
    public sealed class CompletionRules
    {
        /// <summary>
        /// True if the completion list should be dismissed if the user's typing causes it to filter
        /// and display no items.
        /// </summary>
        public bool DismissIfEmpty { get; }

        /// <summary>
        /// True if the list should be dismissed when the user deletes the last character in the span.
        /// </summary>
        public bool DismissIfLastCharacterDeleted { get; }

        /// <summary>
        /// The default set of typed characters that cause the selected item to be committed.
        /// Individual <see cref="CompletionItem"/>s can override this.
        /// </summary>
        public ImmutableArray<char> DefaultCommitCharacters { get; }

        /// <summary>
        /// The default rule that determines if the enter key is passed through to the editor after the selected item has been committed.
        /// Individual <see cref="CompletionItem"/>s can override this.
        /// </summary>
        public EnterKeyRule DefaultEnterKeyRule { get; }

        /// <summary>
        /// The rule determing how snippets work.
        /// </summary>
        public SnippetsRule SnippetsRule { get; }

        private CompletionRules(
            bool dismissIfEmpty,
            bool dismissIfLastCharacterDeleted,
            ImmutableArray<char> defaultCommitCharacters,
            EnterKeyRule defaultEnterKeyRule,
            SnippetsRule snippetsRule)
        {
            this.DismissIfEmpty = dismissIfEmpty;
            this.DismissIfLastCharacterDeleted = dismissIfLastCharacterDeleted;
            this.DefaultCommitCharacters = defaultCommitCharacters.NullToEmpty();
            this.DefaultEnterKeyRule = defaultEnterKeyRule;
            this.SnippetsRule = snippetsRule;
        }

        /// <summary>
        /// Creates a new <see cref="CompletionRules"/> instance.
        /// </summary>
        /// <param name="dismissIfEmpty">True if the completion list should be dismissed if the user's typing causes it to filter and display no items.</param>
        /// <param name="dismissIfLastCharacterDeleted">True if the list should be dismissed when the user deletes the last character in the span.</param>
        /// <param name="defaultCommitCharacters">The default set of typed characters that cause the selected item to be committed.</param>
        /// <param name="defaultEnterKeyRule">The default rule that determines if the enter key is passed through to the editor after the selected item has been committed.</param>
        public static CompletionRules Create(
            bool dismissIfEmpty,
            bool dismissIfLastCharacterDeleted,
            ImmutableArray<char> defaultCommitCharacters,
            EnterKeyRule defaultEnterKeyRule)
        {
            return Create(dismissIfEmpty, dismissIfLastCharacterDeleted, defaultCommitCharacters,
                defaultEnterKeyRule, SnippetsRule.Default);
        }

        /// <summary>
        /// Creates a new <see cref="CompletionRules"/> instance.
        /// </summary>
        /// <param name="dismissIfEmpty">True if the completion list should be dismissed if the user's typing causes it to filter and display no items.</param>
        /// <param name="dismissIfLastCharacterDeleted">True if the list should be dismissed when the user deletes the last character in the span.</param>
        /// <param name="defaultCommitCharacters">The default set of typed characters that cause the selected item to be committed.</param>
        /// <param name="defaultEnterKeyRule">The default rule that determines if the enter key is passed through to the editor after the selected item has been committed.</param>
        /// <param name="snippetsRule">The rule that controls snippets behavior.</param>
        public static CompletionRules Create(
            bool dismissIfEmpty = false,
            bool dismissIfLastCharacterDeleted = false,
            ImmutableArray<char> defaultCommitCharacters = default(ImmutableArray<char>),
            EnterKeyRule defaultEnterKeyRule = EnterKeyRule.Default,
            SnippetsRule snippetsRule = SnippetsRule.Default)
        {
            return new CompletionRules(
                dismissIfEmpty: dismissIfEmpty,
                dismissIfLastCharacterDeleted: dismissIfLastCharacterDeleted,
                defaultCommitCharacters: defaultCommitCharacters,
                defaultEnterKeyRule: defaultEnterKeyRule,
                snippetsRule: snippetsRule);
        }

        private CompletionRules With(
            Optional<bool> dismissIfEmpty = default(Optional<bool>),
            Optional<bool> dismissIfLastCharacterDeleted = default(Optional<bool>),
            Optional<ImmutableArray<char>> defaultCommitCharacters = default(Optional<ImmutableArray<char>>),
            Optional<EnterKeyRule> defaultEnterKeyRule = default(Optional<EnterKeyRule>),
            Optional<SnippetsRule> snippetsRule = default(Optional<SnippetsRule>))
        {
            var newDismissIfEmpty = dismissIfEmpty.HasValue ? dismissIfEmpty.Value : this.DismissIfEmpty;
            var newDismissIfLastCharacterDeleted = dismissIfLastCharacterDeleted.HasValue ? dismissIfLastCharacterDeleted.Value : this.DismissIfLastCharacterDeleted;
            var newDefaultCommitCharacters = defaultCommitCharacters.HasValue ? defaultCommitCharacters.Value : this.DefaultCommitCharacters;
            var newDefaultEnterKeyRule = defaultEnterKeyRule.HasValue ? defaultEnterKeyRule.Value : this.DefaultEnterKeyRule;
            var newSnippetsRule = snippetsRule.HasValue ? snippetsRule.Value : this.SnippetsRule;

            if (newDismissIfEmpty == this.DismissIfEmpty &&
                newDismissIfLastCharacterDeleted == this.DismissIfLastCharacterDeleted &&
                newDefaultCommitCharacters == this.DefaultCommitCharacters &&
                newDefaultEnterKeyRule == this.DefaultEnterKeyRule &&
                newSnippetsRule == this.SnippetsRule)
            {
                return this;
            }
            else
            {
                return Create(
                    newDismissIfEmpty,
                    newDismissIfLastCharacterDeleted,
                    newDefaultCommitCharacters,
                    newDefaultEnterKeyRule,
                    newSnippetsRule);
            }
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionRules"/> with the <see cref="DismissIfEmpty"/> property changed.
        /// </summary>
        public CompletionRules WithDismissIfEmpty(bool dismissIfEmpty)
        {
            return With(dismissIfEmpty: dismissIfEmpty);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionRules"/> with the <see cref="DismissIfLastCharacterDeleted"/> property changed.
        /// </summary>
        public CompletionRules WithDismissIfLastCharacterDeleted(bool dismissIfLastCharacterDeleted)
        {
            return With(dismissIfLastCharacterDeleted: dismissIfLastCharacterDeleted);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionRules"/> with the <see cref="DefaultCommitCharacters"/> property changed.
        /// </summary>
        public CompletionRules WithDefaultCommitCharacters(ImmutableArray<char> defaultCommitCharacters)
        {
            return With(defaultCommitCharacters: defaultCommitCharacters);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionRules"/> with the <see cref="DefaultEnterKeyRule"/> property changed.
        /// </summary>
        public CompletionRules WithDefaultEnterKeyRule(EnterKeyRule defaultEnterKeyRule)
        {
            return With(defaultEnterKeyRule: defaultEnterKeyRule);
        }

        /// <summary>
        /// Creates a copy of the this <see cref="CompletionRules"/> with the <see cref="SnippetsRule"/> property changed.
        /// </summary>
        public CompletionRules WithSnippetsRule(SnippetsRule snippetsRule)
        {
            return With(snippetsRule: snippetsRule);
        }

        private static readonly ImmutableArray<char> s_defaultCommitKeys = ImmutableArray.Create(
                ' ', '{', '}', '[', ']', '(', ')', '.', ',', ':',
                ';', '+', '-', '*', '/', '%', '&', '|', '^', '!',
                '~', '=', '<', '>', '?', '@', '#', '\'', '\"', '\\');

        /// <summary>
        /// The default <see cref="CompletionRules"/> if none is otherwise specified.
        /// </summary>
        public static readonly CompletionRules Default = new CompletionRules(
            dismissIfEmpty: false,
            dismissIfLastCharacterDeleted: false,
            defaultCommitCharacters: s_defaultCommitKeys,
            defaultEnterKeyRule: EnterKeyRule.Default,
            snippetsRule: SnippetsRule.Default);
    }
}