// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.Razor;

#pragma warning disable RS0030 // Do not use banned APIs
[ExportWorkspaceService(typeof(ISourceGeneratedDocumentSpanMappingService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
internal sealed class RazorSourceGeneratedDocumentSpanMappingServiceWrapper(
    [Import(AllowDefault = true)] Lazy<IRazorSourceGeneratedDocumentSpanMappingService>? implementation) : ISourceGeneratedDocumentSpanMappingService
#pragma warning restore RS0030 // Do not use banned APIs
{
    private readonly Lazy<IRazorSourceGeneratedDocumentSpanMappingService>? _implementation = implementation;

    public bool CanMapSpans(SourceGeneratedDocument document)
        => _implementation is not null && document.IsRazorSourceGeneratedDocument();

    public Task<ImmutableArray<MappedTextChange>> GetMappedTextChangesAsync(SourceGeneratedDocument oldDocument, SourceGeneratedDocument newDocument, CancellationToken cancellationToken)
    {
        if (_implementation is null ||
            !oldDocument.IsRazorSourceGeneratedDocument() ||
            !newDocument.IsRazorSourceGeneratedDocument())
        {
            return SpecializedTasks.EmptyImmutableArray<MappedTextChange>();
        }

        return _implementation.Value.GetMappedTextChangesAsync(oldDocument, newDocument, cancellationToken);
    }

    public Task<ImmutableArray<MappedSpanResult>> MapSpansAsync(SourceGeneratedDocument document, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken)
    {
        if (_implementation is null || !document.IsRazorSourceGeneratedDocument())
        {
            return SpecializedTasks.EmptyImmutableArray<MappedSpanResult>();
        }

        return _implementation.Value.MapSpansAsync(document, spans, cancellationToken);
    }
}
