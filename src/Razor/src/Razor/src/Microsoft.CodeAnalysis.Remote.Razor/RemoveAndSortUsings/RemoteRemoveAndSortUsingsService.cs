// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Diagnostics;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteRemoveAndSortUsingsService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteRemoveAndSortUsingsService
{
    internal sealed class Factory : FactoryBase<IRemoteRemoveAndSortUsingsService>
    {
        protected override IRemoteRemoveAndSortUsingsService CreateService(in ServiceArgs args)
            => new RemoteRemoveAndSortUsingsService(in args);
    }

    public ValueTask<ImmutableArray<TextChange>> GetRemoveAndSortUsingsEditsAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId documentId,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => GetRemoveAndSortUsingsEditsAsync(context, cancellationToken),
            cancellationToken);

    private static async ValueTask<ImmutableArray<TextChange>> GetRemoveAndSortUsingsEditsAsync(
        RemoteDocumentContext context,
        CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        var sourceText = codeDocument.Source.Text;
        var syntaxTree = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree();
        var allUsingDirectives = syntaxTree.GetUsingDirectives();

        if (allUsingDirectives.Length == 0)
        {
            return [];
        }

        // Determine unused directive lines from cache. We assume diagnostics will have run before this command
        if (!UnusedDirectiveCache.TryGet(codeDocument, out var unusedDirectiveSpans))
        {
            return [];
        }

        // The cache stores the line numbers of the directives that are unused. We need to go through and find
        // the actual nodes that we want to keep, ie the used directives.
        using var usedDirectives = new PooledArrayBuilder<RazorUsingDirectiveSyntax>();
        foreach (var directive in allUsingDirectives)
        {
            if (!unusedDirectiveSpans.Contains(directive.Span))
            {
                usedDirectives.Add(directive);
            }
        }

        var textEdits = UsingDirectiveHelper.GetSortAndConsolidateEdits(codeDocument, usedDirectives.ToImmutableAndClear());
        return textEdits.SelectAsArray(sourceText.GetTextChange);
    }

    public ValueTask<ImmutableArray<TextChange>> GetSortUsingsEditsAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId documentId,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => GetSortUsingsEditsAsync(context, cancellationToken),
            cancellationToken);

    private static async ValueTask<ImmutableArray<TextChange>> GetSortUsingsEditsAsync(
        RemoteDocumentContext context,
        CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        var textEdits = UsingDirectiveHelper.GetSortAndConsolidateEdits(codeDocument);
        return textEdits.SelectAsArray(codeDocument.Source.Text.GetTextChange);
    }
}
