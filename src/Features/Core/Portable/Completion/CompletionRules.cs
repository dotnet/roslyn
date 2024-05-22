// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Completion;

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
    /// The rule determining how snippets work.
    /// </summary>
    public SnippetsRule SnippetsRule { get; }

    private CompletionRules(
        bool dismissIfEmpty,
        bool dismissIfLastCharacterDeleted,
        ImmutableArray<char> defaultCommitCharacters,
        EnterKeyRule defaultEnterKeyRule,
        SnippetsRule snippetsRule)
    {
        DismissIfEmpty = dismissIfEmpty;
        DismissIfLastCharacterDeleted = dismissIfLastCharacterDeleted;
        DefaultCommitCharacters = defaultCommitCharacters.NullToEmpty();
        DefaultEnterKeyRule = defaultEnterKeyRule;
        SnippetsRule = snippetsRule;
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
        ImmutableArray<char> defaultCommitCharacters = default,
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
        Optional<bool> dismissIfEmpty = default,
        Optional<bool> dismissIfLastCharacterDeleted = default,
        Optional<ImmutableArray<char>> defaultCommitCharacters = default,
        Optional<EnterKeyRule> defaultEnterKeyRule = default,
        Optional<SnippetsRule> snippetsRule = default)
    {
        var newDismissIfEmpty = dismissIfEmpty.HasValue ? dismissIfEmpty.Value : DismissIfEmpty;
        var newDismissIfLastCharacterDeleted = dismissIfLastCharacterDeleted.HasValue ? dismissIfLastCharacterDeleted.Value : DismissIfLastCharacterDeleted;
        var newDefaultCommitCharacters = defaultCommitCharacters.HasValue ? defaultCommitCharacters.Value : DefaultCommitCharacters;
        var newDefaultEnterKeyRule = defaultEnterKeyRule.HasValue ? defaultEnterKeyRule.Value : DefaultEnterKeyRule;
        var newSnippetsRule = snippetsRule.HasValue ? snippetsRule.Value : SnippetsRule;

        if (newDismissIfEmpty == DismissIfEmpty &&
            newDismissIfLastCharacterDeleted == DismissIfLastCharacterDeleted &&
            newDefaultCommitCharacters == DefaultCommitCharacters &&
            newDefaultEnterKeyRule == DefaultEnterKeyRule &&
            newSnippetsRule == SnippetsRule)
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
        => With(dismissIfEmpty: dismissIfEmpty);

    /// <summary>
    /// Creates a copy of this <see cref="CompletionRules"/> with the <see cref="DismissIfLastCharacterDeleted"/> property changed.
    /// </summary>
    public CompletionRules WithDismissIfLastCharacterDeleted(bool dismissIfLastCharacterDeleted)
        => With(dismissIfLastCharacterDeleted: dismissIfLastCharacterDeleted);

    /// <summary>
    /// Creates a copy of this <see cref="CompletionRules"/> with the <see cref="DefaultCommitCharacters"/> property changed.
    /// </summary>
    public CompletionRules WithDefaultCommitCharacters(ImmutableArray<char> defaultCommitCharacters)
        => With(defaultCommitCharacters: defaultCommitCharacters);

    /// <summary>
    /// Creates a copy of this <see cref="CompletionRules"/> with the <see cref="DefaultEnterKeyRule"/> property changed.
    /// </summary>
    public CompletionRules WithDefaultEnterKeyRule(EnterKeyRule defaultEnterKeyRule)
        => With(defaultEnterKeyRule: defaultEnterKeyRule);

    /// <summary>
    /// Creates a copy of the this <see cref="CompletionRules"/> with the <see cref="SnippetsRule"/> property changed.
    /// </summary>
    public CompletionRules WithSnippetsRule(SnippetsRule snippetsRule)
        => With(snippetsRule: snippetsRule);

    private static readonly ImmutableArray<char> s_defaultCommitKeys = [' ', '{', '}', '[', ']', '(', ')', '.', ',', ':', ';', '+', '-', '*', '/', '%', '&', '|', '^', '!', '~', '=', '<', '>', '?', '@', '#', '\'', '\"', '\\'];

    /// <summary>
    /// The default <see cref="CompletionRules"/> if none is otherwise specified.
    /// </summary>
    public static readonly CompletionRules Default = new(
        dismissIfEmpty: false,
        dismissIfLastCharacterDeleted: false,
        defaultCommitCharacters: s_defaultCommitKeys,
        defaultEnterKeyRule: EnterKeyRule.Default,
        snippetsRule: SnippetsRule.Default);
}
