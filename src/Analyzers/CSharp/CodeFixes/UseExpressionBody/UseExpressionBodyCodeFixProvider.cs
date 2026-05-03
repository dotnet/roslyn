// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseExpressionBody), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class UseExpressionBodyCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    private static readonly ImmutableArray<UseExpressionBodyHelper> s_helpers = UseExpressionBodyHelper.Helpers;

    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = s_helpers.SelectAsArray(h => h.DiagnosticId);

#if WORKSPACE
    protected override CodeActionCleanup Cleanup => CodeActionCleanup.SyntaxOnly;
#endif

    protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
        => !diagnostic.IsSuppressed ||
           diagnostic.Properties.ContainsKey(UseExpressionBodyDiagnosticAnalyzer.FixesError);

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();

        var priority = diagnostic.Severity == DiagnosticSeverity.Hidden
            ? CodeActionPriority.Low
            : CodeActionPriority.Default;

        var title = diagnostic.GetMessage();

        RegisterCodeFix(context, title, title, priority);
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var accessorLists = new HashSet<AccessorListSyntax>();
        foreach (var diagnostic in diagnostics)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddEdits(semanticModel, editor, diagnostic, accessorLists, cancellationToken);
        }

        // Ensure that if we changed any accessors that the accessor lists they're contained
        // in are formatted properly as well.  Do this as a last pass so that we see all
        // individual changes made to the child accessors if we're doing a fix-all.
        foreach (var accessorList in accessorLists)
        {
            editor.ReplaceNode(accessorList, (current, _) => current.WithAdditionalAnnotations(Formatter.Annotation));
        }
    }

    private static void AddEdits(
        SemanticModel semanticModel, SyntaxEditor editor, Diagnostic diagnostic,
        HashSet<AccessorListSyntax> accessorLists,
        CancellationToken cancellationToken)
    {
        var declarationLocation = diagnostic.AdditionalLocations[0];
        var helper = s_helpers.Single(h => h.DiagnosticId == diagnostic.Id);
        var declaration = declarationLocation.FindNode(getInnermostNodeForTie: true, cancellationToken);
        var useExpressionBody = diagnostic.Properties.ContainsKey(nameof(UseExpressionBody));

        var updatedDeclaration = helper.Update(semanticModel, declaration, useExpressionBody, cancellationToken)
                                       .WithAdditionalAnnotations(Formatter.Annotation);

        editor.ReplaceNode(declaration, updatedDeclaration);

        if (declaration.Parent is AccessorListSyntax accessorList)
        {
            accessorLists.Add(accessorList);
        }
    }
}
