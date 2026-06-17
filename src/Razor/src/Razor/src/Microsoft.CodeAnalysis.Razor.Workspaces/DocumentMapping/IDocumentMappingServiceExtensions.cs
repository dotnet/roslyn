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

        var inDeclDocument = false;
        var languageKind = codeDocument.GetLanguageKind(razorIndex, rightAssociative: false);
        if (languageKind is RazorLanguageKind.CSharp)
        {
            if (service.TryMapToCSharpDocumentLinePosition(codeDocument, razorIndex, out var mappedPosition, out _, out inDeclDocument))
            {
                // For C# locations, we attempt to return the corresponding position
                // within the projected document
                position = mappedPosition.ToPosition();
            }
            else
            {
                // Some locations are classified as C# but do not correspond to a position in the
                // projected document. This currently happens for some Razor directive content,
                // like the assembly name in @addTagHelper, so fall back to Razor.
                languageKind = RazorLanguageKind.Razor;
            }
        }

        return new DocumentPositionInfo(languageKind, position, razorIndex, inDeclDocument);
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

    /// <summary>
    /// Convenience method to map from Razor to C#, which checks both impl and decl documents
    /// </summary>
    /// <remarks>
    /// A position in a Razor document could map to one of two different C# documents, but the only situation
    /// where it would map to both is when the resulting position in the C# document is semantically equivalent.
    /// i.e., a Razor using or namespace directive would map to both the decl and impl documents, but in either case
    /// it ends up at a C# using or namespace directive, so it doesn't matter which one we get back.
    ///
    /// For all other positions in the Razor document, only one document will be mappable.
    ///
    /// Note that the same is NOT true in reverse: A mappable position in a C# document might be unique to either
    /// the decl or impl document, but that would only be a coincidence. Part of the reason we emit inDeclDocument
    /// as an out parameter is because in order to map back to Razor later, we must know which document the C# position
    /// came from.
    /// </remarks>
    public static bool TryMapToCSharpDocumentLinePosition(this IDocumentMappingService service, RazorCodeDocument codeDocument, int razorIndex, out LinePosition csharpPosition, out int csharpIndex, out bool inDeclDocument)
    {
        inDeclDocument = false;
        if (service.TryMapToCSharpDocumentPosition(codeDocument.GetRequiredCSharpDocument(declarationDocument: false), razorIndex, out csharpPosition, out csharpIndex))
        {
            return true;
        }

        inDeclDocument = true;
        if (codeDocument.GetCSharpDocument(declarationDocument: true) is { } declDocument &&
            service.TryMapToCSharpDocumentPosition(declDocument, razorIndex, out csharpPosition, out csharpIndex))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Convenience method to map from Razor to C#, which checks both impl and decl documents
    /// </summary>
    public static bool TryMapToCSharpDocumentLinePositionSpan(this IDocumentMappingService service, RazorCodeDocument codeDocument, LinePositionSpan razorRange, out LinePositionSpan csharpRange, out bool inDeclDocument)
    {
        inDeclDocument = false;
        if (service.TryMapToCSharpDocumentRange(codeDocument.GetRequiredCSharpDocument(declarationDocument: false), razorRange, out csharpRange))
        {
            return true;
        }

        if (codeDocument.GetCSharpDocument(declarationDocument: true) is { } declDocument &&
            service.TryMapToCSharpDocumentRange(declDocument, razorRange, out csharpRange))
        {
            inDeclDocument = true;
            return true;
        }

        csharpRange = default;
        return false;
    }
}
