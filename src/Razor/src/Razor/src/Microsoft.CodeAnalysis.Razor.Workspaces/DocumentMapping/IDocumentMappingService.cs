// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.DocumentMapping;

internal interface IDocumentMappingService
{
    bool TryMapToRazorDocumentRange(RazorCSharpDocument csharpDocument, LinePositionSpan csharpRange, MappingBehavior mappingBehavior, out LinePositionSpan razorRange);

    bool TryMapToCSharpDocumentRange(RazorCSharpDocument csharpDocument, LinePositionSpan razorRange, out LinePositionSpan csharpRange);

    bool TryMapToRazorDocumentPosition(RazorCSharpDocument csharpDocument, int csharpIndex, out LinePosition razorPosition, out int razorIndex);

    bool TryMapToCSharpDocumentPosition(RazorCSharpDocument csharpDocument, int razorIndex, out LinePosition csharpPosition, out int csharpIndex);

    bool TryMapToCSharpPositionOrNext(RazorCSharpDocument csharpDocument, int razorIndex, out LinePosition csharpPosition, out int csharpIndex);

    ImmutableArray<LinePositionSpan> GetCSharpSpansOverlappingRazorSpan(RazorCSharpDocument csharpDocument, LinePositionSpan razorSpan);
}
