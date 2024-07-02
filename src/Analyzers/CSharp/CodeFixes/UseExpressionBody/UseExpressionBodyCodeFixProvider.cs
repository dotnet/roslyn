// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseExpressionBody), Shared]
internal partial class UseExpressionBodyCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }

    private static readonly ImmutableArray<UseExpressionBodyHelper> _helpers = UseExpressionBodyHelper.Helpers;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public UseExpressionBodyCodeFixProvider()
        => FixableDiagnosticIds = _helpers.SelectAsArray(h => h.DiagnosticId);

    protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
        => !diagnostic.IsSuppressed ||
           diagnostic.Properties.ContainsKey(UseExpressionBodyDiagnosticAnalyzer.FixesError);

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();

        var priority = diagnostic.Severity == DiagnosticSeverity.Hidden
            ? CodeActionPriority.Low
            : CodeActionPriority.Default;

        var title = diagnostic.GetMessage();

        RegisterCodeFix(context, title, title, priority);
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

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
        var helper = _helpers.Single(h => h.DiagnosticId == diagnostic.Id);
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
