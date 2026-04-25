// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Protocol.InlayHints;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteInlayHintService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteInlayHintService
{
    internal sealed class Factory : FactoryBase<IRemoteInlayHintService>
    {
        protected override IRemoteInlayHintService CreateService(in ServiceArgs args)
            => new RemoteInlayHintService(in args);
    }

    private readonly InlayHintCacheWrapperProvider _cacheWrapperProvider = args.ExportProvider.GetExportedValue<InlayHintCacheWrapperProvider>();
    private readonly IRazorEditService _razorEditService = args.ExportProvider.GetExportedValue<IRazorEditService>();

    public ValueTask<InlayHint[]?> GetInlayHintsAsync(JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo, JsonSerializableDocumentId razorDocumentId, InlayHintParams inlayHintParams, bool displayAllOverride, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => GetInlayHintsAsync(context, inlayHintParams, displayAllOverride, cancellationToken),
            cancellationToken);

    private async ValueTask<InlayHint[]?> GetInlayHintsAsync(RemoteDocumentContext context, InlayHintParams inlayHintParams, bool displayAllOverride, CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        var csharpDocument = codeDocument.GetRequiredCSharpDocument();

        var span = inlayHintParams.Range.ToLinePositionSpan();

        cancellationToken.ThrowIfCancellationRequested();

        var overlappingSpans = DocumentMappingService.GetCSharpSpansOverlappingRazorSpan(csharpDocument, span);

        if (overlappingSpans.IsEmpty)
        {
            // There's no C# in the range.
            return null;
        }

        var generatedDocument = await context.Snapshot
            .GetGeneratedDocumentAsync(cancellationToken)
            .ConfigureAwait(false);

        var textDocument = inlayHintParams.TextDocument.WithUri(generatedDocument.CreateUri());

        using var inlayHintsBuilder = new PooledArrayBuilder<InlayHint>();
        var razorSourceText = codeDocument.Source.Text;
        var csharpSourceText = codeDocument.GetCSharpSourceText();
        var root = codeDocument.GetRequiredSyntaxRoot();

        foreach (var csharpSpan in overlappingSpans)
        {
            var range = csharpSpan.ToRange();
            var hints = await InlayHints.GetInlayHintsAsync(generatedDocument, textDocument, range, displayAllOverride, _cacheWrapperProvider.GetCache(), cancellationToken).ConfigureAwait(false);
            if (hints is null)
            {
                continue;
            }

            foreach (var hint in hints)
            {
                if (csharpSourceText.TryGetAbsoluteIndex(hint.Position.ToLinePosition(), out var absoluteIndex) &&
                    DocumentMappingService.TryMapToRazorDocumentPosition(csharpDocument, absoluteIndex, out var hostDocumentPosition, out var hostDocumentIndex))
                {
                    // We know this C# maps to Razor, but does it map to Razor that we like?

                    // We don't want inlay hints in tag helper attributes
                    var node = root.FindInnermostNode(hostDocumentIndex);
                    if (node?.FirstAncestorOrSelf<MarkupTagHelperAttributeValueSyntax>() is not null)
                    {
                        continue;
                    }

                    // Inlay hints in directives are okay, eg '@attribute [Description(description: "Desc")]', but if the hint is going to be
                    // at the very start of the directive, we want to strip any TextEdit as it would make for an invalid document. eg: '// @page template: "/"'
                    if (node?.SpanStart == hostDocumentIndex &&
                        node.FirstAncestorOrSelf<RazorDirectiveSyntax>(static n => n.IsDirectiveKind(DirectiveKind.SingleLine)) is not null)
                    {
                        hint.TextEdits = null;
                    }

                    if (hint.TextEdits is not null)
                    {
                        var changes = hint.TextEdits.SelectAsArray(csharpSourceText.GetTextChange);
                        var textChanges = await _razorEditService.MapCSharpEditsAsync(changes, context.Snapshot, cancellationToken).ConfigureAwait(false);

                        var textEdits = textChanges.SelectAsArray(razorSourceText.GetTextEdit);

                        hint.TextEdits = ImmutableCollectionsMarshal.AsArray(textEdits);
                    }

                    hint.Data = new InlayHintDataWrapper(inlayHintParams.TextDocument, hint.Data, hint.Position);
                    hint.Position = hostDocumentPosition.ToPosition();

                    inlayHintsBuilder.Add(hint);
                }
            }
        }

        return inlayHintsBuilder.ToArray();
    }

    public ValueTask<InlayHint> ResolveHintAsync(JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo, JsonSerializableDocumentId razorDocumentId, InlayHint inlayHint, CancellationToken cancellationToken)
       => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => ResolveInlayHintAsync(context, inlayHint, cancellationToken),
            cancellationToken);

    private async ValueTask<InlayHint> ResolveInlayHintAsync(RemoteDocumentContext context, InlayHint inlayHint, CancellationToken cancellationToken)
    {
        var generatedDocument = await context.Snapshot
            .GetGeneratedDocumentAsync(cancellationToken)
            .ConfigureAwait(false);

        return await InlayHints.ResolveInlayHintAsync(generatedDocument, inlayHint, _cacheWrapperProvider.GetCache(), cancellationToken).ConfigureAwait(false);
    }
}
