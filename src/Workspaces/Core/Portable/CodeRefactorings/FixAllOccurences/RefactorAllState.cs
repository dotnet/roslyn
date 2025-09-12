// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

internal sealed class RefactorAllState : CommonFixAllState<CodeRefactoringProvider, RefactorAllProvider, RefactorAllState>
{
    /// <summary>
    /// Original selection span from which refactor-all was invoked. This is used in <see
    /// cref="GetRefactorAllSpansAsync(CancellationToken)"/> to compute refactor all spans for <see
    /// cref="RefactorAllScope.ContainingMember"/> and <see cref="RefactorAllScope.ContainingType"/> scopes.
    /// </summary>
    private readonly TextSpan _selectionSpan;

    public override FixAllKind FixAllKind => FixAllKind.Refactoring;

    public string CodeActionTitle { get; }

    public RefactorAllState(
        RefactorAllProvider refactorAllProvider,
        Document document,
        TextSpan selectionSpan,
        CodeRefactoringProvider codeRefactoringProvider,
        RefactorAllScope refactorAllScope,
        CodeAction codeAction)
        : this(refactorAllProvider, document ?? throw new ArgumentNullException(nameof(document)), document.Project, selectionSpan, codeRefactoringProvider,
               refactorAllScope, codeAction.Title, codeAction.EquivalenceKey)
    {
    }

    public RefactorAllState(
        RefactorAllProvider refactorAllProvider,
        Project project,
        TextSpan selectionSpan,
        CodeRefactoringProvider codeRefactoringProvider,
        RefactorAllScope refactorAllScope,
        CodeAction codeAction)
        : this(refactorAllProvider, document: null, project ?? throw new ArgumentNullException(nameof(project)), selectionSpan, codeRefactoringProvider,
               refactorAllScope, codeAction.Title, codeAction.EquivalenceKey)
    {
    }

    private RefactorAllState(
        RefactorAllProvider refactorAllProvider,
        Document? document,
        Project project,
        TextSpan selectionSpan,
        CodeRefactoringProvider codeRefactoringProvider,
        RefactorAllScope refactorAllScope,
        string codeActionTitle,
        string? codeActionEquivalenceKey)
        : base(refactorAllProvider, document, project, codeRefactoringProvider, refactorAllScope.ToFixAllScope(), codeActionEquivalenceKey)
    {
        _selectionSpan = selectionSpan;
        this.CodeActionTitle = codeActionTitle;
    }

    protected override RefactorAllState With(Document? document, Project project, FixAllScope scope, string? codeActionEquivalenceKey)
    {
        return new RefactorAllState(
            this.FixAllProvider,
            document,
            project,
            _selectionSpan,
            this.Provider,
            scope.ToRefactorAllScope(),
            this.CodeActionTitle,
            codeActionEquivalenceKey);
    }

    /// <summary>
    /// Gets the spans to refactor by document for the <see cref="RefactorAllScope"/> for this refactor all occurrences
    /// action. If no spans are specified, it indicates the entire document needs to be refactored.
    /// </summary>
    internal async Task<ImmutableDictionary<Document, Optional<ImmutableArray<TextSpan>>>> GetRefactorAllSpansAsync(CancellationToken cancellationToken)
    {
        IEnumerable<Document>? documentsToRefactor = null;
        switch (this.Scope.ToRefactorAllScope())
        {
            case RefactorAllScope.ContainingType or RefactorAllScope.ContainingMember:
                Contract.ThrowIfNull(Document);
                var spanMappingService = Document.GetLanguageService<IFixAllSpanMappingService>();
                if (spanMappingService is null)
                    return ImmutableDictionary<Document, Optional<ImmutableArray<TextSpan>>>.Empty;

                var spansByDocument = await spanMappingService.GetFixAllSpansAsync(
                    Document, _selectionSpan, Scope, cancellationToken).ConfigureAwait(false);
                return spansByDocument.Select(kvp => KeyValuePair.Create(kvp.Key, new Optional<ImmutableArray<TextSpan>>(kvp.Value)))
                    .ToImmutableDictionaryOrEmpty();

            case RefactorAllScope.Document:
                Contract.ThrowIfNull(Document);
                documentsToRefactor = [Document];
                break;

            case RefactorAllScope.Project:
                documentsToRefactor = Project.Documents;
                break;

            case RefactorAllScope.Solution:
                documentsToRefactor = Project.Solution.Projects.SelectMany(p => p.Documents);
                break;

            default:
                return ImmutableDictionary<Document, Optional<ImmutableArray<TextSpan>>>.Empty;
        }

        return documentsToRefactor.ToImmutableDictionary(d => d, _ => default(Optional<ImmutableArray<TextSpan>>));
    }
}
