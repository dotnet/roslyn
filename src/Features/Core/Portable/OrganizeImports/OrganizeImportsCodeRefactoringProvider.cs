// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.OrganizeImports;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeRefactoringProviderNames.OrganizeImports), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class OrganizeImportsCodeRefactoringProvider() : SyntaxEditorBasedCodeRefactoringProvider
{
    protected override ImmutableArray<FixAllScope> SupportedFixAllScopes => [FixAllScope.Project, FixAllScope.Solution];

    private static async Task<(SyntaxNode oldRoot, SyntaxNode newRoot)> RemoveImportsAsync(
        Document document, IOrganizeImportsService organizeImportsService, CancellationToken cancellationToken)
    {
        var oldRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var options = await document.GetOrganizeImportsOptionsAsync(cancellationToken).ConfigureAwait(false);
        var newDocument = await organizeImportsService.OrganizeImportsAsync(document, options, cancellationToken).ConfigureAwait(false);
        var newRoot = await newDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        return (oldRoot, newRoot);
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<TextSpan> fixAllSpans, SyntaxEditor editor, string? equivalenceKey, CancellationToken cancellationToken)
    {
        var organizeImportsService = document.GetRequiredLanguageService<IOrganizeImportsService>();
        var (oldRoot, newRoot) = await RemoveImportsAsync(document, organizeImportsService, cancellationToken).ConfigureAwait(false);

        // If no changes were made, then we don't need to do anything.
        if (oldRoot == newRoot)
            return;

        editor.ReplaceNode(oldRoot, newRoot);
    }

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, span, cancellationToken) = context;

        var organizeImportsService = document.GetRequiredLanguageService<IOrganizeImportsService>();
        var (oldRoot, newRoot) = await RemoveImportsAsync(document, organizeImportsService, cancellationToken).ConfigureAwait(false);

        // If no changes were made, then we don't need to do anything.
        if (oldRoot == newRoot)
            return;

        context.RegisterRefactoring(CodeAction.Create(
            organizeImportsService.SortImportsDisplayStringWithAccelerator,
            cancellationToken => Task.FromResult(document.WithSyntaxRoot(newRoot))));
    }
}
