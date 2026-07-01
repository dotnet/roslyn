// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;

internal interface IDocumentMappingService
{
    bool TryMapToRazorDocumentRange(RazorCSharpDocument csharpDocument, LinePositionSpan csharpRange, MappingBehavior mappingBehavior, out LinePositionSpan razorRange);

    bool TryMapToCSharpDocumentRange(RazorCSharpDocument csharpDocument, LinePositionSpan razorRange, out LinePositionSpan csharpRange);

    bool TryMapToRazorDocumentPosition(RazorCSharpDocument csharpDocument, int csharpIndex, out LinePosition razorPosition, out int razorIndex);

    bool TryMapToCSharpDocumentPosition(RazorCSharpDocument csharpDocument, int razorIndex, out LinePosition csharpPosition, out int csharpIndex);

    ImmutableArray<LinePositionSpan> GetCSharpSpansOverlappingRazorSpan(RazorCSharpDocument csharpDocument, LinePositionSpan razorSpan);

    /// <summary>
    /// Maps a range in the specified generated document uri to a range in the Razor document that owns the
    /// generated document. If the uri passed in is not for a generated document, or the range cannot be mapped
    /// for some other reason, the original passed in range is returned unchanged.
    /// </summary>
    Task<(Uri MappedDocumentUri, LinePositionSpan MappedRange)> MapToHostDocumentUriAndRangeAsync(RemoteDocumentSnapshot originSnapshot, Uri generatedDocumentUri, LinePositionSpan generatedDocumentRange, CancellationToken cancellationToken);
}
