// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.OrganizeImports;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeRefactoringProviderNames.OrganizeImports), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class OrganizeImportsCodeRefactoringProvider() : SyntaxEditorBasedCodeRefactoringProvider
{
    protected override ImmutableArray<RefactorAllScope> SupportedRefactorAllScopes => [RefactorAllScope.Project, RefactorAllScope.Solution];

    /// <summary>
    /// Matches 'remove unnecessary imports' code fix.  This way we don't show 'sort imports' above it.
    /// </summary>
    protected override CodeActionRequestPriority ComputeRequestPriority()
        => CodeActionRequestPriority.Low;

    /// <summary>
    /// This refactoring provider touches syntax only.  So we can speed up fix all by having it only clean syntax
    /// and not semantics.
    /// </summary>
    protected override CodeActionCleanup Cleanup => CodeActionCleanup.SyntaxOnly;

    private static async Task<(SyntaxNode oldRoot, SyntaxNode newRoot)> RemoveImportsAsync(
        Document document, CancellationToken cancellationToken)
    {
        var organizeImportsService = document.GetRequiredLanguageService<IOrganizeImportsService>();
        var oldRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var options = await document.GetOrganizeImportsOptionsAsync(cancellationToken).ConfigureAwait(false);
        var newDocument = await organizeImportsService.OrganizeImportsAsync(document, options, cancellationToken).ConfigureAwait(false);
        var newRoot = await newDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        return (oldRoot, newRoot);
    }

    protected override async Task RefactorAllAsync(
        Document document, ImmutableArray<TextSpan> fixAllSpans, SyntaxEditor editor, string? equivalenceKey, CancellationToken cancellationToken)
    {
        var (oldRoot, newRoot) = await RemoveImportsAsync(document, cancellationToken).ConfigureAwait(false);

        // If no changes were made, then we don't need to do anything.
        if (oldRoot == newRoot)
            return;

        editor.ReplaceNode(oldRoot, newRoot);
    }

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, span, cancellationToken) = context;

        var oldRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = oldRoot.FindToken(span.Start);

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var importNode = token.GetAncestors<SyntaxNode>().FirstOrDefault(syntaxFacts.IsUsingOrExternOrImport);
        if (importNode is null)
            return;

        var (_, newRoot) = await RemoveImportsAsync(document, cancellationToken).ConfigureAwait(false);

        // If no changes were made, then we don't need to do anything.
        if (oldRoot == newRoot)
            return;

        // Find the containing namespace/compilation unit of this import.  We'll use that to determine the span
        // this refactoring applies to.
        var ancestor = importNode.Ancestors().FirstOrDefault(syntaxFacts.IsBaseNamespaceDeclaration) ?? oldRoot;
        var imports = ancestor.ChildNodes().Where(syntaxFacts.IsUsingOrExternOrImport);

        context.RegisterRefactoring(CodeAction.Create(
            document.GetRequiredLanguageService<IOrganizeImportsService>().SortImportsDisplayStringWithoutAccelerator,
            async cancellationToken => document.WithSyntaxRoot(newRoot)),
            applicableToSpan: imports.GetContainedSpan());
    }
}
