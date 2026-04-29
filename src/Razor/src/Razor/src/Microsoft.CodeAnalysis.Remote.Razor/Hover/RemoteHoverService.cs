// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Hover;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using static Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Roslyn.LanguageServer.Protocol.Hover?>;
using ExternalHandlers = Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteHoverService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteHoverService
{
    internal sealed class Factory : FactoryBase<IRemoteHoverService>
    {
        protected override IRemoteHoverService CreateService(in ServiceArgs args)
            => new RemoteHoverService(in args);
    }

    private readonly IClientCapabilitiesService _clientCapabilitiesService = args.ExportProvider.GetExportedValue<IClientCapabilitiesService>();

    protected override IDocumentPositionInfoStrategy DocumentPositionInfoStrategy => PreferAttributeNameDocumentPositionInfoStrategy.Instance;

    public ValueTask<RemoteResponse<Hover?>> GetHoverAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        Position position,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => GetHoverAsync(context, position, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<Hover?>> GetHoverAsync(
        RemoteDocumentContext context,
        Position position,
        CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        var sourceText = codeDocument.Source.Text;
        if (!sourceText.TryGetAbsoluteIndex(position, out var hostDocumentIndex))
        {
            return NoFurtherHandling;
        }

        var originalHostDocumentIndex = hostDocumentIndex;

        // Adjust position if on a component end tag to use the start tag position
        hostDocumentIndex = codeDocument.AdjustPositionForComponentEndTag(hostDocumentIndex);

        var clientCapabilities = _clientCapabilitiesService.ClientCapabilities;
        var positionInfo = GetPositionInfo(codeDocument, hostDocumentIndex, preferCSharpOverHtml: true);

        if (positionInfo.LanguageKind == RazorLanguageKind.CSharp)
        {
            var generatedDocument = await context.Snapshot
                .GetGeneratedDocumentAsync(cancellationToken)
                .ConfigureAwait(false);

            var csharpHover = await ExternalHandlers.Hover
                .GetHoverAsync(
                    generatedDocument,
                    positionInfo.Position.ToLinePosition(),
                    clientCapabilities.SupportsVisualStudioExtensions(),
                    clientCapabilities.SupportsMarkdown(),
                    cancellationToken)
                .ConfigureAwait(false);

            // Roslyn couldn't provide a hover, so we're done.
            if (csharpHover is null)
            {
                return NoFurtherHandling;
            }

            // Map the hover range back to the host document
            if (csharpHover.Range is { } range &&
                DocumentMappingService.TryMapToRazorDocumentRange(codeDocument.GetRequiredCSharpDocument(), range.ToLinePositionSpan(), out var hostDocumentSpan))
            {
                csharpHover.Range = LspFactory.CreateRange(hostDocumentSpan);
            }

            // If we adjusted from an end tag to a start tag, we need to make sure the range covers the end tag,
            // not just the start tag, or VS won't show the hover info
            if (originalHostDocumentIndex > hostDocumentIndex &&
                csharpHover.Range is not null)
            {
                // We were originally on the end tag somewhere, then redirected to the start tag to get the hover.
                // We now need to translate the range we got back, and mapped, over to the end tag again. This is as
                // easy as just offsetting the range by the difference between our original and adjusted index.
                if (sourceText.TryGetAbsoluteIndex(csharpHover.Range.Start, out var adjustedStart) &&
                    sourceText.TryGetAbsoluteIndex(csharpHover.Range.End, out var adjustedEnd))
                {
                    var offset = originalHostDocumentIndex - hostDocumentIndex;

                    // Make sure we don't fall off the end of the document, though it should be impossible
                    adjustedStart = Math.Min(adjustedStart + offset, sourceText.Length - 1);
                    adjustedEnd = Math.Min(adjustedEnd + offset, sourceText.Length - 1);

                    csharpHover.Range = sourceText.GetRange(adjustedStart, adjustedEnd);
                }
            }

            // As there is a C# hover, stop further handling.
            return new RemoteResponse<Hover?>(StopHandling: true, Result: csharpHover);
        }

        if (positionInfo.LanguageKind is not (RazorLanguageKind.Html or RazorLanguageKind.Razor))
        {
            Debug.Fail($"Encountered an unexpected {nameof(RazorLanguageKind)}: {positionInfo.LanguageKind}");
            return NoFurtherHandling;
        }

        // If this is Html or Razor, try to retrieve a hover from Razor.
        var options = HoverDisplayOptions.From(clientCapabilities);

        // In co-hosting, there isn't a singleton IComponentAvailabilityService in the MEF composition.
        // So, we construct one using the current solution snapshot.
        // All of this will change when solution snapshots are available in the core Razor project model.

        // TODO: Remove this when solution snapshots are available in the core Razor project model.
        var componentAvailabilityService = new ComponentAvailabilityService(context.Snapshot.ProjectSnapshot.SolutionSnapshot);

        var razorHover = await HoverFactory
            .GetHoverAsync(codeDocument, hostDocumentIndex, options, componentAvailabilityService, cancellationToken)
            .ConfigureAwait(false);

        // Roslyn couldn't provide a hover, so we're done.
        if (razorHover is null)
        {
            return CallHtml;
        }

        // Ensure that we convert our Hover to a Roslyn Hover.
        var resultHover = ConvertHover(razorHover);

        return Results(resultHover);
    }

    /// <summary>
    ///  Converts a <see cref="Hover"/> to a <see cref="Hover"/>.
    /// </summary>
    /// <remarks>
    ///  Once Razor moves wholly over to Roslyn.LanguageServer.Protocol, this method can be removed.
    /// </remarks>
    private static Hover ConvertHover(Hover hover)
    {
        // Note: Razor only ever produces a Hover with MarkupContent or a VSInternalHover with RawContents.
        // Both variants return a Range.

        return hover switch
        {
            VSInternalHover { Range: var range, RawContent: { } rawContent } => new VSInternalHover()
            {
                Range = range,
                Contents = string.Empty,
                RawContent = rawContent
            },
            Hover { Range: var range, Contents.Fourth: MarkupContent contents } => new Hover()
            {
                Range = range,
                Contents = contents
            },
            _ => Assumed.Unreachable<Hover>(),
        };
    }
}
