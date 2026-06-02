// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.CohostingShared;

#pragma warning disable RS0030 // Do not use banned APIs
[Export(typeof(IRazorSourceGeneratedDocumentSpanMappingService))]
[Shared]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class RazorSourceGeneratedDocumentSpanMappingService(IRemoteServiceInvoker remoteServiceInvoker) : IRazorSourceGeneratedDocumentSpanMappingService
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;

    public async Task<ImmutableArray<MappedTextChange>> GetMappedTextChangesAsync(SourceGeneratedDocument oldDocument, SourceGeneratedDocument newDocument, CancellationToken cancellationToken)
    {
        if (!oldDocument.IsRazorSourceGeneratedDocument() || !newDocument.IsRazorSourceGeneratedDocument())
        {
            // If either document is not a Razor source generated document, we cannot map text changes.
            return [];
        }

        // We have to get the text changes on this side, because we're dealing with changed source generated documents, and we can't
        // expect to transfer the Ids over to OOP and see the same changes
        var changes = await newDocument.GetTextChangesAsync(oldDocument, cancellationToken).ConfigureAwait(false);
        var changesArray = changes.ToImmutableArray();
        if (changesArray.IsDefaultOrEmpty)
        {
            return [];
        }

        var mappedChanges = await _remoteServiceInvoker.TryInvokeAsync<IRemoteSpanMappingService, ImmutableArray<RemoteMappedEditResult>>(
            oldDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.MapTextChangesAsync(solutionInfo, newDocument.Id, changesArray, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (mappedChanges.IsDefaultOrEmpty)
        {
            return [];
        }

        using var results = new PooledArrayBuilder<MappedTextChange>();

        foreach (var change in mappedChanges)
        {
            if (change.IsDefault)
            {
                continue;
            }

            foreach (var textChange in change.TextChanges)
            {
                results.Add(new MappedTextChange(change.FilePath, textChange));
            }
        }

        return results.ToImmutableAndClear();
    }

    public async Task<ImmutableArray<MappedSpanResult>> MapSpansAsync(SourceGeneratedDocument document, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken)
    {
        if (!document.IsRazorSourceGeneratedDocument())
        {
            // If the document is not a Razor source generated document, we cannot map spans.
            return [];
        }

        var mappedSpans = await _remoteServiceInvoker.TryInvokeAsync<IRemoteSpanMappingService, ImmutableArray<RemoteMappedSpanResult>>(
            document.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.MapSpansAsync(solutionInfo, document.Id, spans, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (mappedSpans.IsDefault ||
            mappedSpans.Length != spans.Length)
        {
            return [];
        }

        using var results = new PooledArrayBuilder<MappedSpanResult>();

        foreach (var span in mappedSpans)
        {
            results.Add(span.IsDefault
                ? default
                : new MappedSpanResult(span.FilePath, span.LinePositionSpan, span.Span));
        }

        return results.ToImmutableAndClear();
    }
}
