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
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHint;
using Microsoft.CodeAnalysis.Razor.Protocol.InlayHints;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;
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

    private readonly InlayHintCacheProvider _cacheProvider = args.ExportProvider.GetExportedValue<InlayHintCacheProvider>();
    private readonly IRazorEditService _razorEditService = args.ExportProvider.GetExportedValue<IRazorEditService>();

    public ValueTask<InlayHint[]?> GetInlayHintsAsync(JsonSerializableRazorSolutionWrapper solutionInfo, JsonSerializableDocumentId razorDocumentId, InlayHintParams inlayHintParams, bool displayAllOverride, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => GetInlayHintsAsync(context, inlayHintParams, displayAllOverride, cancellationToken),
            cancellationToken);

    private async ValueTask<InlayHint[]?> GetInlayHintsAsync(RemoteDocumentContext context, InlayHintParams inlayHintParams, bool displayAllOverride, CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        var span = inlayHintParams.Range.ToLinePositionSpan();

        cancellationToken.ThrowIfCancellationRequested();

        using var inlayHintsBuilder = new PooledArrayBuilder<InlayHint>();
        var sawCSharpSpan = false;
        var seenHints = new HashSet<(LinePosition Position, string? Label)>();

        await AddInlayHintsAsync(codeDocument.GetRequiredCSharpDocument(declarationDocument: false), cancellationToken).ConfigureAwait(false);

        if (codeDocument.GetCSharpDocument(declarationDocument: true) is { } declCSharpDocument)
        {
            await AddInlayHintsAsync(declCSharpDocument, cancellationToken).ConfigureAwait(false);
        }

        return !sawCSharpSpan
            ? null
            : inlayHintsBuilder.ToArray();

        async ValueTask AddInlayHintsAsync(RazorCSharpDocument csharpDocument, CancellationToken cancellationToken)
        {
            var overlappingSpans = DocumentMappingService.GetCSharpSpansOverlappingRazorSpan(csharpDocument, span);

            if (overlappingSpans.IsEmpty)
            {
                // There's no C# in the range for this generated document.
                return;
            }

            sawCSharpSpan = true;

            var inDeclDocument = csharpDocument.IsDeclarationDocument;
            var generatedDocument = await context.Snapshot
                .GetGeneratedDocumentAsync(inDeclDocument, cancellationToken)
                .ConfigureAwait(false);

            var textDocument = inlayHintParams.TextDocument.WithUri(generatedDocument.GetURI());

            var razorSourceText = codeDocument.Source.Text;
            var csharpSourceText = csharpDocument.Text;
            var root = codeDocument.GetRequiredSyntaxRoot();

            foreach (var csharpSpan in overlappingSpans)
            {
                var range = csharpSpan.ToRange();
                var hints = await GetInlayHintsAsync(generatedDocument, textDocument, range, displayAllOverride, _cacheProvider.GetCache(), cancellationToken).ConfigureAwait(false);
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
                            var textChanges = await _razorEditService.MapCSharpEditsAsync(changes, inDeclDocument, context.Snapshot, cancellationToken).ConfigureAwait(false);

                            var textEdits = textChanges.SelectAsArray(razorSourceText.GetTextEdit);

                            hint.TextEdits = ImmutableCollectionsMarshal.AsArray(textEdits);
                        }

                        if (!seenHints.Add((hostDocumentPosition, hint.Label.First)))
                        {
                            continue;
                        }

                        hint.Data = new InlayHintDataWrapper(inlayHintParams.TextDocument, hint.Data, hint.Position, inDeclDocument);
                        hint.Position = hostDocumentPosition.ToPosition();

                        inlayHintsBuilder.Add(hint);
                    }
                }
            }
        }
    }

    public ValueTask<InlayHint> ResolveHintAsync(JsonSerializableRazorSolutionWrapper solutionInfo, JsonSerializableDocumentId razorDocumentId, InlayHint inlayHint, bool inDeclDocument, CancellationToken cancellationToken)
       => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => ResolveInlayHintAsync(context, inlayHint, inDeclDocument, cancellationToken),
            cancellationToken);

    private async ValueTask<InlayHint> ResolveInlayHintAsync(RemoteDocumentContext context, InlayHint inlayHint, bool inDeclDocument, CancellationToken cancellationToken)
    {
        var generatedDocument = await context.Snapshot
            .GetGeneratedDocumentAsync(inDeclDocument, cancellationToken)
            .ConfigureAwait(false);

        return await ResolveInlayHintAsync(generatedDocument, inlayHint, _cacheProvider.GetCache(), cancellationToken).ConfigureAwait(false);
    }

    private static Task<InlayHint[]?> GetInlayHintsAsync(
        Document document,
        TextDocumentIdentifier textDocumentIdentifier,
        LspRange range,
        bool displayAllOverride,
        InlayHintCache cache,
        CancellationToken cancellationToken)
    {
        var options = GetInlineHintsOptions(displayAllOverride);

        return InlayHintHandler.GetInlayHintsAsync(
            document,
            textDocumentIdentifier,
            range,
            options,
            displayAllOverride,
            cache,
            cancellationToken);
    }

    private static Task<InlayHint> ResolveInlayHintAsync(
        Document document,
        InlayHint request,
        InlayHintCache cache,
        CancellationToken cancellationToken)
    {
        var data = InlayHintResolveHandler.GetInlayHintResolveData(request);
        var options = GetInlineHintsOptions(data.DisplayAllOverride);
        return InlayHintResolveHandler.ResolveInlayHintAsync(document, request, cache, data, options, cancellationToken);
    }

    private static InlineHintsOptions GetInlineHintsOptions(bool displayAllOverride)
    {
        var options = InlineHintsOptions.Default;
        if (!displayAllOverride)
        {
            options = options with
            {
                TypeOptions = options.TypeOptions with { EnabledForTypes = true },
                ParameterOptions = options.ParameterOptions with { EnabledForParameters = true },
            };
        }

        return options;
    }
}
