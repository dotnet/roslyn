// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions;

/// <summary>
/// Similar to SuggestedAction, but in a location that can be used by
/// both local Roslyn and LSP.
/// </summary>
internal sealed class UnifiedSuggestedAction
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
    public CodeActionPriority CodeActionPriority { get; }

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

    public UnifiedSuggestedActionFlavors? Flavors { get; }

    public ImmutableArray<UnifiedSuggestedActionSet> NestedActionSets { get; }
    public IRefactorOrFixAllState? RefactorOrFixAllState { get; }

    private UnifiedSuggestedAction(
        CodeAction codeAction,
        CodeActionPriority codeActionPriority,
        object provider,
        CodeRefactoringKind? codeRefactoringKind,
        ImmutableArray<Diagnostic> diagnostics,
        UnifiedSuggestedActionFlavors? flavors,
        ImmutableArray<UnifiedSuggestedActionSet> nestedActionSets,
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

    public static UnifiedSuggestedAction CreateWithFlavors(
        CodeAction codeAction,
        CodeActionPriority codeActionPriority,
        object provider,
        CodeRefactoringKind? codeRefactoringKind,
        ImmutableArray<Diagnostic> diagnostics,
        UnifiedSuggestedActionFlavors? flavors)
    {
        return new(codeAction, codeActionPriority, provider, codeRefactoringKind, diagnostics, flavors, nestedActionSets: default, refactorOrFixAllState: null);
    }

    public static UnifiedSuggestedAction CreateWithNestedActionSets(
        CodeAction codeAction,
        CodeActionPriority codeActionPriority,
        object provider,
        CodeRefactoringKind? codeRefactoringKind,
        ImmutableArray<Diagnostic> diagnostics,
        ImmutableArray<UnifiedSuggestedActionSet> nestedActionSets)
    {
        return new(codeAction, codeActionPriority, provider, codeRefactoringKind, diagnostics, flavors: null, nestedActionSets, refactorOrFixAllState: null);
    }

    public static UnifiedSuggestedAction CreateRefactorOrFixAll(
        CodeAction codeAction,
        CodeActionPriority codeActionPriority,
        CodeRefactoringKind? codeRefactoringKind,
        ImmutableArray<Diagnostic> diagnostics,
        IRefactorOrFixAllState refactorOrFixAllState)
    {
        return new(codeAction, codeActionPriority, refactorOrFixAllState.Provider, codeRefactoringKind, diagnostics, flavors: null, nestedActionSets: default, refactorOrFixAllState);
    }
}

internal readonly record struct UnifiedSuggestedActionFlavors(
    string Title,
    ImmutableArray<UnifiedSuggestedAction> Actions);
