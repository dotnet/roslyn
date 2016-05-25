// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        private CompletionRules(
            bool dismissIfEmpty,
            bool dismissIfLastCharacterDeleted,
            ImmutableArray<char> defaultCommitCharacters,
            EnterKeyRule defaultEnterKeyRule)
        {
            this.DismissIfEmpty = dismissIfEmpty;
            this.DismissIfLastCharacterDeleted = dismissIfLastCharacterDeleted;
            this.DefaultCommitCharacters = defaultCommitCharacters.IsDefault ? ImmutableArray<char>.Empty : defaultCommitCharacters;
            this.DefaultEnterKeyRule = defaultEnterKeyRule;
        }

        /// <summary>
        /// Creates a new <see cref="CompletionRules"/> instance.
        /// </summary>
        /// <param name="dismissIfEmpty">True if the completion list should be dismissed if the user's typing causes it to filter and display no items.</param>
        /// <param name="dismissIfLastCharacterDeleted">True if the list should be dismissed when the user deletes the last character in the span.</param>
        /// <param name="defaultCommitCharacters">The default set of typed characters that cause the selected item to be committed.</param>
        /// <param name="defaultEnterKeyRule">The default rule that determines if the enter key is passed through to the editor after the selected item has been committed.</param>
        /// <returns></returns>
        public static CompletionRules Create(
            bool dismissIfEmpty = false,
            bool dismissIfLastCharacterDeleted = false,
            ImmutableArray<char> defaultCommitCharacters = default(ImmutableArray<char>),
            EnterKeyRule defaultEnterKeyRule = EnterKeyRule.Default)
        {
            return new CompletionRules(
                dismissIfEmpty: dismissIfEmpty,
                dismissIfLastCharacterDeleted: dismissIfLastCharacterDeleted,
                defaultCommitCharacters: defaultCommitCharacters,
                defaultEnterKeyRule: defaultEnterKeyRule);
        }

        private CompletionRules With(
            Optional<bool> dismissIfEmpty = default(Optional<bool>),
            Optional<bool> dismissIfLastCharacterDeleted = default(Optional<bool>),
            Optional<ImmutableArray<char>> defaultCommitCharacters = default(Optional<ImmutableArray<char>>),
            Optional<EnterKeyRule> defaultEnterKeyRule = default(Optional<EnterKeyRule>))
        {
            var newDismissIfEmpty = dismissIfEmpty.HasValue ? dismissIfEmpty.Value : this.DismissIfEmpty;
            var newDismissIfLastCharacterDeleted = dismissIfLastCharacterDeleted.HasValue ? dismissIfLastCharacterDeleted.Value : this.DismissIfLastCharacterDeleted;
            var newDefaultCommitCharacters = defaultCommitCharacters.HasValue ? defaultCommitCharacters.Value : this.DefaultCommitCharacters;
            var newDefaultEnterKeyRule = defaultEnterKeyRule.HasValue ? defaultEnterKeyRule.Value : this.DefaultEnterKeyRule;

            if (newDismissIfEmpty == this.DismissIfEmpty
                && newDismissIfLastCharacterDeleted == this.DismissIfLastCharacterDeleted
                && newDefaultCommitCharacters == this.DefaultCommitCharacters
                && newDefaultEnterKeyRule == this.DefaultEnterKeyRule)
            {
                return this;
            }
            else
            {
                return Create(
                    newDismissIfEmpty,
                    newDismissIfLastCharacterDeleted,
                    newDefaultCommitCharacters,
                    newDefaultEnterKeyRule);
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

        private static readonly ImmutableArray<char> s_defaultCommitKeys = new[]
            {
                ' ', '{', '}', '[', ']', '(', ')', '.', ',', ':',
                ';', '+', '-', '*', '/', '%', '&', '|', '^', '!',
                '~', '=', '<', '>', '?', '@', '#', '\'', '\"', '\\'
            }.ToImmutableArray();

        /// <summary>
        /// The default <see cref="CompletionRules"/> if none is otherwise specified.
        /// </summary>
        public static readonly CompletionRules Default
            = new CompletionRules(
                dismissIfEmpty: false,
                dismissIfLastCharacterDeleted: false,
                defaultCommitCharacters: s_defaultCommitKeys,
                defaultEnterKeyRule: EnterKeyRule.Never);
    }
}
