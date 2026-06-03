// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.CodeAnalysis.Remote.Razor;

using SyntaxToken = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxToken;

internal sealed class RemoteLinkedEditingRangeService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteLinkedEditingRangeService
{
    internal sealed class Factory : FactoryBase<IRemoteLinkedEditingRangeService>
    {
        protected override IRemoteLinkedEditingRangeService CreateService(in ServiceArgs args)
            => new RemoteLinkedEditingRangeService(in args);
    }

    public ValueTask<LinePositionSpan[]?> GetRangesAsync(
        RazorSolutionWrapper solutionInfo,
        DocumentId razorDocumentId,
        LinePosition linePosition,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => GetRangesAsync(context, linePosition, cancellationToken),
            cancellationToken);

    public async ValueTask<LinePositionSpan[]?> GetRangesAsync(
        RemoteDocumentContext context,
        LinePosition linePosition,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        if (!codeDocument.Source.Text.TryGetSourceLocation(linePosition, out var validLocation))
        {
            return null;
        }

        var syntaxTree = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree();

        // We only care if the user is within a TagHelper or HTML tag with a valid start and end tag.
        if (TryGetNearestMarkupNameTokens(syntaxTree, validLocation, out var startTagNameToken, out var endTagNameToken) &&
            (startTagNameToken.Span.Contains(validLocation.AbsoluteIndex) || endTagNameToken.Span.Contains(validLocation.AbsoluteIndex) ||
            startTagNameToken.Span.End == validLocation.AbsoluteIndex || endTagNameToken.Span.End == validLocation.AbsoluteIndex))
        {
            var startSpan = startTagNameToken.GetLinePositionSpan(codeDocument.Source);
            var endSpan = endTagNameToken.GetLinePositionSpan(codeDocument.Source);

            return [startSpan, endSpan];
        }

        return null;
    }

    private static bool TryGetNearestMarkupNameTokens(
        RazorSyntaxTree syntaxTree,
        SourceLocation location,
        out SyntaxToken startTagNameToken,
        out SyntaxToken endTagNameToken)
    {
        var owner = syntaxTree.Root.FindInnermostNode(location.AbsoluteIndex);
        var element = owner?.FirstAncestorOrSelf<MarkupSyntaxNode>(
            a => a.Kind is SyntaxKind.MarkupTagHelperElement || a.Kind is SyntaxKind.MarkupElement);

        if (element is null)
        {
            startTagNameToken = default;
            endTagNameToken = default;
            return false;
        }

        if (element is BaseMarkupElementSyntax { StartTag: var startTag, EndTag: var endTag })
        {
            startTagNameToken = startTag?.Name ?? default;
            endTagNameToken = endTag?.Name ?? default;

            return startTagNameToken.IsValid() && endTagNameToken.IsValid();
        }

        throw new InvalidOperationException("Element is expected to be a MarkupTagHelperElement or MarkupElement.");
    }
}
