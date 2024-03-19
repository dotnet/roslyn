// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using FixAllScope = Microsoft.CodeAnalysis.CodeFixes.FixAllScope;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

internal abstract partial class CommonFixAllState<TProvider, TFixAllProvider, TFixAllState> : IFixAllState
    where TFixAllProvider : IFixAllProvider
    where TFixAllState : CommonFixAllState<TProvider, TFixAllProvider, TFixAllState>
{
    public int CorrelationId { get; } = CorrelationIdFactory.GetNextId();
    public TFixAllProvider FixAllProvider { get; }
    public string? CodeActionEquivalenceKey { get; }
    public TProvider Provider { get; }
    public Document? Document { get; }
    public Project Project { get; }
    public Solution Solution => Project.Solution;
    public FixAllScope Scope { get; }
    public abstract FixAllKind FixAllKind { get; }
    public CodeActionOptionsProvider CodeActionOptionsProvider { get; }

    protected CommonFixAllState(
        TFixAllProvider fixAllProvider,
        Document? document,
        Project project,
        TProvider provider,
        CodeActionOptionsProvider optionsProvider,
        FixAllScope scope,
        string? codeActionEquivalenceKey)
    {
        Debug.Assert(document == null || document.Project == project);

        FixAllProvider = fixAllProvider;
        Document = document;
        Project = project;
        Provider = provider;
        CodeActionOptionsProvider = optionsProvider;
        Scope = scope;
        CodeActionEquivalenceKey = codeActionEquivalenceKey;
    }

    protected abstract TFixAllState With(Document? document, Project project, FixAllScope scope, string? codeActionEquivalenceKey);

    public TFixAllState With(
        Optional<(Document? document, Project project)> documentAndProject = default,
        Optional<FixAllScope> scope = default,
        Optional<string?> codeActionEquivalenceKey = default)
    {
        var (newDocument, newProject) = documentAndProject.HasValue ? documentAndProject.Value : (Document, Project);
        var newScope = scope.HasValue ? scope.Value : Scope;
        var newCodeActionEquivalenceKey = codeActionEquivalenceKey.HasValue ? codeActionEquivalenceKey.Value : CodeActionEquivalenceKey;

        if (newDocument == Document &&
            newProject == Project &&
            newScope == Scope &&
            newCodeActionEquivalenceKey == CodeActionEquivalenceKey)
        {
            return (TFixAllState)this;
        }

        return With(newDocument, newProject, newScope, newCodeActionEquivalenceKey);
    }

    #region IFixAllState implementation
    IFixAllProvider IFixAllState.FixAllProvider => this.FixAllProvider!;

    object IFixAllState.Provider => this.Provider!;

    IFixAllState IFixAllState.With(
        Optional<(Document? document, Project project)> documentAndProject,
        Optional<FixAllScope> scope,
        Optional<string?> codeActionEquivalenceKey)
        => this.With(documentAndProject, scope, codeActionEquivalenceKey);
    #endregion
}
