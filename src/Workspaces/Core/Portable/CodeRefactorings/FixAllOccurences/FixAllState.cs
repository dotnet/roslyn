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
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using FixAllScope = Microsoft.CodeAnalysis.CodeFixes.FixAllScope;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

internal sealed class FixAllState : CommonFixAllState<CodeRefactoringProvider, FixAllProvider, FixAllState>
{
    /// <summary>
    /// Original selection span from which FixAll was invoked.
    /// This is used in <see cref="GetFixAllSpansAsync(CancellationToken)"/>
    /// to compute fix all spans for <see cref="FixAllScope.ContainingMember"/>
    /// and <see cref="FixAllScope.ContainingType"/> scopes.
    /// </summary>
    private readonly TextSpan _selectionSpan;

    public override FixAllKind FixAllKind => FixAllKind.Refactoring;

    public string CodeActionTitle { get; }

    public FixAllState(
        FixAllProvider fixAllProvider,
        Document document,
        TextSpan selectionSpan,
        CodeRefactoringProvider codeRefactoringProvider,
        CodeActionOptionsProvider optionsProvider,
        FixAllScope fixAllScope,
        CodeAction codeAction)
        : this(fixAllProvider, document ?? throw new ArgumentNullException(nameof(document)), document.Project, selectionSpan, codeRefactoringProvider,
               optionsProvider, fixAllScope, codeAction.Title, codeAction.EquivalenceKey)
    {
    }

    public FixAllState(
        FixAllProvider fixAllProvider,
        Project project,
        TextSpan selectionSpan,
        CodeRefactoringProvider codeRefactoringProvider,
        CodeActionOptionsProvider optionsProvider,
        FixAllScope fixAllScope,
        CodeAction codeAction)
        : this(fixAllProvider, document: null, project ?? throw new ArgumentNullException(nameof(project)), selectionSpan, codeRefactoringProvider,
               optionsProvider, fixAllScope, codeAction.Title, codeAction.EquivalenceKey)
    {
    }

    private FixAllState(
        FixAllProvider fixAllProvider,
        Document? document,
        Project project,
        TextSpan selectionSpan,
        CodeRefactoringProvider codeRefactoringProvider,
        CodeActionOptionsProvider optionsProvider,
        FixAllScope fixAllScope,
        string codeActionTitle,
        string? codeActionEquivalenceKey)
        : base(fixAllProvider, document, project, codeRefactoringProvider, optionsProvider, fixAllScope, codeActionEquivalenceKey)
    {
        _selectionSpan = selectionSpan;
        this.CodeActionTitle = codeActionTitle;
    }

    protected override FixAllState With(Document? document, Project project, FixAllScope scope, string? codeActionEquivalenceKey)
    {
        return new FixAllState(
            this.FixAllProvider,
            document,
            project,
            _selectionSpan,
            this.Provider,
            this.CodeActionOptionsProvider,
            scope,
            this.CodeActionTitle,
            codeActionEquivalenceKey);
    }

    /// <summary>
    /// Gets the spans to fix by document for the <see cref="FixAllScope"/> for this fix all occurences fix.
    /// If no spans are specified, it indicates the entire document needs to be fixed.
    /// </summary>
    internal async Task<ImmutableDictionary<Document, Optional<ImmutableArray<TextSpan>>>> GetFixAllSpansAsync(CancellationToken cancellationToken)
    {
        IEnumerable<Document>? documentsToFix = null;
        switch (this.Scope)
        {
            case FixAllScope.ContainingType or FixAllScope.ContainingMember:
                Contract.ThrowIfNull(Document);
                var spanMappingService = Document.GetLanguageService<IFixAllSpanMappingService>();
                if (spanMappingService is null)
                    return ImmutableDictionary<Document, Optional<ImmutableArray<TextSpan>>>.Empty;

                var spansByDocument = await spanMappingService.GetFixAllSpansAsync(
                    Document, _selectionSpan, Scope, cancellationToken).ConfigureAwait(false);
                return spansByDocument.Select(kvp => KeyValuePairUtil.Create(kvp.Key, new Optional<ImmutableArray<TextSpan>>(kvp.Value)))
                    .ToImmutableDictionaryOrEmpty();

            case FixAllScope.Document:
                Contract.ThrowIfNull(Document);
                documentsToFix = [Document];
                break;

            case FixAllScope.Project:
                documentsToFix = Project.Documents;
                break;

            case FixAllScope.Solution:
                documentsToFix = Project.Solution.Projects.SelectMany(p => p.Documents);
                break;

            default:
                return ImmutableDictionary<Document, Optional<ImmutableArray<TextSpan>>>.Empty;
        }

        return documentsToFix.ToImmutableDictionary(d => d, _ => default(Optional<ImmutableArray<TextSpan>>));
    }
}
