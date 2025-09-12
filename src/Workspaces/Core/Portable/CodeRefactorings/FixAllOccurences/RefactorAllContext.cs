// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

/// <summary>
/// Context for "Refactor all occurrences" for code refactorings provided by each <see cref="CodeRefactoringProvider"/>.
/// </summary>
/// <remarks>
/// TODO: Make public, tracked with https://github.com/dotnet/roslyn/issues/60703
/// </remarks>
public sealed class RefactorAllContext : IRefactorOrFixAllContext
{
    internal RefactorAllState State { get; }

    internal RefactorAllProvider RefactorAllProvider => State.FixAllProvider;

    /// <summary>
    /// Document within which refactor all occurrences was triggered.
    /// </summary>
    public Document Document => State.Document!;

    /// <summary>
    /// Underlying <see cref="CodeRefactoringProvider"/> which triggered this refactor all.
    /// </summary>
    public CodeRefactoringProvider CodeRefactoringProvider => State.Provider;

    /// <summary>
    /// <see cref="RefactorAllScope"/> to refactor all occurrences.
    /// </summary>
    public RefactorAllScope Scope => (RefactorAllScope)State.Scope;

    /// <summary>
    /// The <see cref="CodeAction.EquivalenceKey"/> value expected of a <see cref="CodeAction"/> participating in this
    /// refactor all.
    /// </summary>
    public string? CodeActionEquivalenceKey => State.CodeActionEquivalenceKey;

    /// <summary>
    /// CancellationToken for refactor all session.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    public IProgress<CodeAnalysisProgress> Progress { get; }

    /// <summary>
    /// Project to refactor all occurrences within.
    /// </summary>
    public Project Project => State.Project;

    public Solution Solution => Project.Solution;

    #region IFixAllContext implementation
    IRefactorOrFixAllState IRefactorOrFixAllContext.State => this.State;

    IRefactorOrFixProvider IRefactorOrFixAllContext.Provider => this.CodeRefactoringProvider;

    string IRefactorOrFixAllContext.GetDefaultTitle() => this.GetDefaultRefactorAllTitle();

    IRefactorOrFixAllContext IRefactorOrFixAllContext.With(
        Optional<(Document? document, Project project)> documentAndProject,
        Optional<FixAllScope> scope,
        Optional<string?> codeActionEquivalenceKey,
        Optional<CancellationToken> cancellationToken)
    {
        var newState = State.With(documentAndProject, scope, codeActionEquivalenceKey);
        var newCancellationToken = cancellationToken.HasValue ? cancellationToken.Value : this.CancellationToken;

        return State == newState && CancellationToken == newCancellationToken
            ? this
            : new RefactorAllContext(newState, this.Progress, newCancellationToken);
    }
    #endregion

    internal RefactorAllContext(
        RefactorAllState state,
        IProgress<CodeAnalysisProgress> progressTracker,
        CancellationToken cancellationToken)
    {
        State = state;
        this.Progress = progressTracker;
        this.CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the spans to refactor by document for the <see cref="Scope"/> for this refactor all occurrences fix. If no
    /// spans are specified, it indicates the entire document needs to be refactored.
    /// </summary>
    public Task<ImmutableDictionary<Document, Optional<ImmutableArray<TextSpan>>>> GetRefactorAllSpansAsync(CancellationToken cancellationToken)
        => State.GetRefactorAllSpansAsync(cancellationToken);

    internal string GetDefaultRefactorAllTitle()
        => FixAllHelper.GetDefaultFixAllTitle(this.Scope.ToFixAllScope(), this.State.CodeActionTitle, this.Document, this.Project);
}
