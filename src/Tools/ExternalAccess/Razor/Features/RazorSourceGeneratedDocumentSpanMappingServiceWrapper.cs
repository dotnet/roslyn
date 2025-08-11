
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

[ExportWorkspaceService(typeof(ISourceGeneratedDocumentSpanMappingService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorSourceGeneratedDocumentSpanMappingServiceWrapper(
    [Import(AllowDefault = true)] IRazorSourceGeneratedDocumentSpanMappingService? implementation) : ISourceGeneratedDocumentSpanMappingService
{
    private readonly IRazorSourceGeneratedDocumentSpanMappingService? _implementation = implementation;

    public bool CanMapSpans(SourceGeneratedDocument document)
    {
        return _implementation is not null && document.IsRazorSourceGeneratedDocument();
    }

    public async Task<ImmutableArray<MappedTextChange>> GetMappedTextChangesAsync(SourceGeneratedDocument oldDocument, SourceGeneratedDocument newDocument, CancellationToken cancellationToken)
    {
        if (_implementation is null ||
            !oldDocument.IsRazorSourceGeneratedDocument() ||
            !newDocument.IsRazorSourceGeneratedDocument())
        {
            return [];
        }

        var mappedChanges = await _implementation.GetMappedTextChangesAsync(oldDocument, newDocument, cancellationToken).ConfigureAwait(false);
        if (mappedChanges.IsDefaultOrEmpty)
        {
            return [];
        }

        using var _ = ArrayBuilder<MappedTextChange>.GetInstance(out var changesBuilder);
        foreach (var change in mappedChanges)
        {
            if (change.IsDefault)
            {
                continue;
            }

            foreach (var textChange in change.TextChanges)
            {
                changesBuilder.Add(new MappedTextChange(change.FilePath, textChange));
            }
        }

        return changesBuilder.ToImmutableAndClear();
    }

    public async Task<ImmutableArray<MappedSpanResult>> MapSpansAsync(SourceGeneratedDocument document, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken)
    {
        if (_implementation is null ||
            !document.IsRazorSourceGeneratedDocument())
        {
            return [];
        }

        var mappedSpans = await _implementation.MapSpansAsync(document, spans, cancellationToken).ConfigureAwait(false);
        if (mappedSpans.Length != spans.Length)
        {
            return [];
        }

        using var _ = ArrayBuilder<MappedSpanResult>.GetInstance(out var spansBuilder);
        foreach (var span in mappedSpans)
        {
            spansBuilder.Add(span.IsDefault
                ? default
                : new MappedSpanResult(span.FilePath, span.LinePositionSpan, span.Span));
        }

        return spansBuilder.ToImmutableAndClear();
    }
}
