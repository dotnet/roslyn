// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.DocumentMapping;

internal static class IDocumentMappingServiceExtensions
{
    public static bool TryMapToRazorDocumentRange(this IDocumentMappingService service, RazorCSharpDocument csharpDocument, LinePositionSpan csharpRange, out LinePositionSpan razorRange)
        => service.TryMapToRazorDocumentRange(csharpDocument, csharpRange, MappingBehavior.Strict, out razorRange);

    public static bool TryMapToRazorDocumentRange(this IDocumentMappingService service, RazorCSharpDocument csharpDocument, LspRange csharpRange, [NotNullWhen(true)] out LspRange? razorRange)
        => service.TryMapToRazorDocumentRange(csharpDocument, csharpRange, MappingBehavior.Strict, out razorRange);

    public static DocumentPositionInfo GetPositionInfo(
        this IDocumentMappingService service,
        RazorCodeDocument codeDocument,
        int razorIndex)
    {
        var sourceText = codeDocument.Source.Text;

        if (sourceText.Length == 0)
        {
            Debug.Assert(razorIndex == 0);

            // Special case for empty documents, to just force Html. When there is no content, then there are no source mappings,
            // so the map call below fails, and we would default to Razor. This is fine for most cases, but empty documents are a
            // special case where Html provides much better results when users first start typing.
            return new DocumentPositionInfo(RazorLanguageKind.Html, new Position(0, 0), razorIndex);
        }

        var position = sourceText.GetPosition(razorIndex);

        var languageKind = codeDocument.GetLanguageKind(razorIndex, rightAssociative: false);
        if (languageKind is RazorLanguageKind.CSharp)
        {
            if (service.TryMapToCSharpDocumentPosition(codeDocument.GetRequiredCSharpDocument(), razorIndex, out Position? mappedPosition, out _))
            {
                // For C# locations, we attempt to return the corresponding position
                // within the projected document
                position = mappedPosition;
            }
            else
            {
                // Some locations are classified as C# but do not correspond to a position in the
                // projected document. This currently happens for some Razor directive content,
                // like the assembly name in @addTagHelper, so fall back to Razor.
                languageKind = RazorLanguageKind.Razor;
            }
        }

        return new DocumentPositionInfo(languageKind, position, razorIndex);
    }

    public static bool TryMapToRazorDocumentRange(this IDocumentMappingService service, RazorCSharpDocument csharpDocument, LspRange csharpRange, MappingBehavior mappingBehavior, [NotNullWhen(true)] out LspRange? razorRange)
    {
        var result = service.TryMapToRazorDocumentRange(csharpDocument, csharpRange.ToLinePositionSpan(), mappingBehavior, out var razorLinePositionSpan);
        razorRange = result ? razorLinePositionSpan.ToRange() : null;
        return result;
    }

    public static bool TryMapToCSharpDocumentRange(this IDocumentMappingService service, RazorCSharpDocument csharpDocument, LspRange razorRange, [NotNullWhen(true)] out LspRange? csharpRange)
    {
        var result = service.TryMapToCSharpDocumentRange(csharpDocument, razorRange.ToLinePositionSpan(), out var csharpLinePositionSpan);
        csharpRange = result ? csharpLinePositionSpan.ToRange() : null;
        return result;
    }

    public static bool TryMapToRazorDocumentPosition(this IDocumentMappingService service, RazorCSharpDocument csharpDocument, int csharpIndex, [NotNullWhen(true)] out Position? razorPosition, out int razorIndex)
    {
        var result = service.TryMapToRazorDocumentPosition(csharpDocument, csharpIndex, out var razorLinePosition, out razorIndex);
        razorPosition = result ? razorLinePosition.ToPosition() : null;
        return result;
    }

    public static bool TryMapToCSharpDocumentPosition(this IDocumentMappingService service, RazorCSharpDocument csharpDocument, int razorIndex, [NotNullWhen(true)] out Position? csharpPosition, out int csharpIndex)
    {
        var result = service.TryMapToCSharpDocumentPosition(csharpDocument, razorIndex, out var csharpLinePosition, out csharpIndex);
        csharpPosition = result ? csharpLinePosition.ToPosition() : null;
        return result;
    }

    public static bool TryMapToCSharpPositionOrNext(this IDocumentMappingService service, RazorCSharpDocument csharpDocument, int razorIndex, [NotNullWhen(true)] out Position? csharpPosition, out int csharpIndex)
    {
        var result = service.TryMapToCSharpPositionOrNext(csharpDocument, razorIndex, out var csharpLinePosition, out csharpIndex);
        csharpPosition = result ? csharpLinePosition.ToPosition() : null;
        return result;
    }
}
