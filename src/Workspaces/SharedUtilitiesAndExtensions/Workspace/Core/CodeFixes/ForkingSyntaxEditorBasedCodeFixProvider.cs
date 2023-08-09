// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeFixes;

internal abstract class ForkingSyntaxEditorBasedCodeFixProvider<TDiagnosticNode>
    : SyntaxEditorBasedCodeFixProvider
    where TDiagnosticNode : SyntaxNode
{
    private readonly string _title;
    private readonly string _equivalenceKey;

    protected ForkingSyntaxEditorBasedCodeFixProvider(
        string title, string equivalenceKey)
    {
        _title = title;
        _equivalenceKey = equivalenceKey;
    }

    protected abstract Task FixAsync(
        Document document, Diagnostic diagnostic, SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions, TDiagnosticNode diagnosticNode,
        CancellationToken cancellationToken);

    protected sealed override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
        => !diagnostic.Descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.Unnecessary);

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, _title, _equivalenceKey);
        return Task.CompletedTask;
    }

    protected sealed override async Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var originalRoot = editor.OriginalRoot;

        var originalNodes = new Stack<(TDiagnosticNode diagnosticNode, Diagnostic diagnostic)>();
        foreach (var diagnostic in diagnostics)
        {
            var diagnosticNode = (TDiagnosticNode)originalRoot.FindNode(
                diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
            originalNodes.Push((diagnosticNode, diagnostic));
        }

        var solutionServices = document.Project.Solution.Services;

        // We're going to be continually editing this tree.  Track all the nodes we care about so we can find them
        // across each edit.
        var semanticDocument = await SemanticDocument.CreateAsync(
            document.WithSyntaxRoot(originalRoot.TrackNodes(originalNodes.Select(static t => t.diagnosticNode))),
            cancellationToken).ConfigureAwait(false);

        while (originalNodes.Count > 0)
        {
            var (originalDiagnosticNode, diagnostic) = originalNodes.Pop();
            var currentRoot = semanticDocument.Root;
            var diagnosticNode = currentRoot.GetCurrentNodes(originalDiagnosticNode).Single();

            var subEditor = new SyntaxEditor(currentRoot, solutionServices);

            await FixAsync(
                semanticDocument.Document,
                diagnostic,
                subEditor,
                fallbackOptions,
                diagnosticNode,
                cancellationToken).ConfigureAwait(false);

            var changedRoot = subEditor.GetChangedRoot();
            if (currentRoot == changedRoot)
                continue;

            semanticDocument = await semanticDocument.WithSyntaxRootAsync(
                changedRoot, cancellationToken).ConfigureAwait(false);
        }

        editor.ReplaceNode(originalRoot, semanticDocument.Root);
    }
}
