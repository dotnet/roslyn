// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.Suggestions;

/// <summary>
/// Similar to SuggestedAction, but in a location that can be used by
/// both local Roslyn and LSP.
/// </summary>
internal sealed class SuggestedAction
{
    /// <summary>
    /// Original provider that created this suggested action. This is only used for extension exception management. If
    /// we encounter a (non-cancellation) exception thrown when using this provider, we wil disable it for the rest of
    /// the session.
    /// </summary>
    public object Provider { get; }

    /// <summary>
    /// Underlying <see cref="CodeAction"/> responsible for making the desired change to the user's code.
    /// </summary>
    public CodeAction CodeAction { get; }

    /// <summary>
    /// Priority that this action should be presented with.  Higher priority actions should be presented more
    /// prominently to the user.
    /// </summary>
    internal CodeActionPriority CodeActionPriority { get; }

    /// <summary>
    /// If this is a code refactoring, what sort of code refactoring it is.  Used to present different sorts of UI
    /// affordances in certain hosts.  Can be <see langword="null"/> if this was not created by a code refactoring but
    /// was instead created by a code fix.
    /// </summary>
    public CodeRefactoringKind? CodeRefactoringKind { get; }

    /// <summary>
    /// If this was created to fix specific diagnostics, these are those diagnostics. This may be empty if the action
    /// represents a code refactoring and not a code fix.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Optional flavors for this action.  Flavors are child actions that are presented as simple links, not as
    /// menu-items. For example the flavors to 'fix all in document/project/solution'.  It present, <see
    /// cref="NestedActionSets"/> will be empty and <see cref="RefactorOrFixAllCodeAction"/> will be <see
    /// langword="null"/>.
    /// </summary>
    public SuggestedActionFlavors? Flavors { get; }

    /// <summary>
    /// Nested actions that should ideally be shown in a sub-menu under this item.  This action will not itself be
    /// invocable, and serves only as a named container for these sub-actions.  If this is non-empty, then <see
    /// cref="Flavors"/> and <see cref="RefactorOrFixAllState"/> will be <see langword="null"/>.
    /// </summary>
    public ImmutableArray<SuggestedActionSet> NestedActionSets { get; }

    /// <summary>
    /// Non-null if this is a fix-all or refactor-all action.  If this is non-null, then <see cref="Flavors"/> will be
    /// <see langword="null"/> and <see cref="NestedActionSets"/> will be empty.
    /// </summary>
    public IRefactorOrFixAllState? RefactorOrFixAllState { get; }

    private SuggestedAction(
        CodeAction codeAction,
        CodeActionPriority codeActionPriority,
        object provider,
        CodeRefactoringKind? codeRefactoringKind,
        ImmutableArray<Diagnostic> diagnostics,
        SuggestedActionFlavors? flavors,
        ImmutableArray<SuggestedActionSet> nestedActionSets,
        IRefactorOrFixAllState? refactorOrFixAllState)
    {
        Provider = provider;
        CodeAction = codeAction;
        CodeActionPriority = codeActionPriority;
        CodeRefactoringKind = codeRefactoringKind;
        Diagnostics = diagnostics;
        Flavors = flavors;
        NestedActionSets = nestedActionSets;
        RefactorOrFixAllState = refactorOrFixAllState;
    }

    public static SuggestedAction CreateWithFlavors(
        CodeAction codeAction,
        CodeActionPriority codeActionPriority,
        object provider,
        CodeRefactoringKind? codeRefactoringKind,
        ImmutableArray<Diagnostic> diagnostics,
        SuggestedActionFlavors? flavors)
    {
        return new(codeAction, codeActionPriority, provider, codeRefactoringKind, diagnostics, flavors, nestedActionSets: [], refactorOrFixAllState: null);
    }

    public static SuggestedAction CreateWithNestedActionSets(
        CodeAction codeAction,
        CodeActionPriority codeActionPriority,
        object provider,
        CodeRefactoringKind? codeRefactoringKind,
        ImmutableArray<Diagnostic> diagnostics,
        ImmutableArray<SuggestedActionSet> nestedActionSets)
    {
        Contract.ThrowIfTrue(nestedActionSets.IsDefaultOrEmpty);
        return new(codeAction, codeActionPriority, provider, codeRefactoringKind, diagnostics, flavors: null, nestedActionSets, refactorOrFixAllState: null);
    }

    public static SuggestedAction CreateRefactorOrFixAll(
        CodeAction codeAction,
        CodeActionPriority codeActionPriority,
        CodeRefactoringKind? codeRefactoringKind,
        ImmutableArray<Diagnostic> diagnostics,
        IRefactorOrFixAllState refactorOrFixAllState)
    {
        Contract.ThrowIfNull(refactorOrFixAllState);
        return new(codeAction, codeActionPriority, refactorOrFixAllState.Provider, codeRefactoringKind, diagnostics, flavors: null, nestedActionSets: [], refactorOrFixAllState);
    }
}
