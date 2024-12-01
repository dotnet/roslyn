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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeFixes;

/// <summary>
/// Helper type for <see cref="CodeFixProvider"/>s that need to provide 'fix all' support in a document, by operate by
/// applying one fix at a time, then recomputing the work to do after that fix is applied.  While this is not generally
/// desirable from a performance perspective (due to the costs of forking a document after each fix), it is sometimes
/// necessary as individual fixes can impact the code so substantially that successive fixes may no longer apply, or may
/// have dramatically different data to work with before the fix.  For example, if one fix removes statements entirely
/// that another fix was contained in.
/// </summary>
internal abstract class ForkingSyntaxEditorBasedCodeFixProvider<TDiagnosticNode>
    : SyntaxEditorBasedCodeFixProvider
    where TDiagnosticNode : SyntaxNode
{
    protected abstract (string title, string equivalenceKey) GetTitleAndEquivalenceKey(CodeFixContext context);

    /// <summary>
    /// Subclasses must override this to actually provide the fix for a particular diagnostic.  The implementation will
    /// be passed the <em>current</em> <paramref name="document"/> (containing the changes from all prior fixes), the
    /// the <paramref name="diagnosticNode"/> in that document, for the current diagnostic being fixed.  And the <see
    /// cref="Diagnostic.Properties"/> for that diagnostic.  The diagnostic itself is not passed along as it was
    /// computed with respect to the original user document, and as such its <see cref="Diagnostic.Location"/> and <see
    /// cref="Diagnostic.AdditionalLocations"/> will not be correct.
    /// </summary>
    protected abstract Task FixAsync(
        Document document,
        SyntaxEditor editor,
        TDiagnosticNode diagnosticNode,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken);

    protected sealed override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
        // Never try to fix the secondary diagnostics that were produced just to fade out code.
        => !diagnostic.Descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.Unnecessary);

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var (title, equivalenceKey) = GetTitleAndEquivalenceKey(context);
        RegisterCodeFix(context, title, equivalenceKey);
        return Task.CompletedTask;
    }

    protected sealed override async Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CancellationToken cancellationToken)
    {
        var originalRoot = editor.OriginalRoot;

        using var _ = ArrayBuilder<(TDiagnosticNode diagnosticNode, Diagnostic diagnostic)>.GetInstance(out var originalNodes);
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

        while (originalNodes.TryPop(out var tuple))
        {
            var (originalDiagnosticNode, diagnostic) = tuple;
            var currentRoot = semanticDocument.Root;
            var diagnosticNode = currentRoot.GetCurrentNodes(originalDiagnosticNode).Single();

            var subEditor = new SyntaxEditor(currentRoot, solutionServices);

            await FixAsync(
                semanticDocument.Document,
                subEditor,
                diagnosticNode,
                diagnostic.Properties,
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
