// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;

internal static class DocumentMappingHelper
{
    public static bool TryMapToCSharpDocumentPosition(RazorCSharpDocument csharpDocument, int razorIndex, out LinePosition csharpPosition, out int csharpIndex)
    {
        foreach (var mapping in csharpDocument.SourceMappingsSortedByOriginal)
        {
            var originalSpan = mapping.OriginalSpan;
            var originalAbsoluteIndex = originalSpan.AbsoluteIndex;
            if (originalAbsoluteIndex <= razorIndex)
            {
                // Treat the mapping as owning the edge at its end (hence <= originalSpan.Length),
                // otherwise we wouldn't handle the cursor being right after the final C# char
                var distanceIntoOriginalSpan = razorIndex - originalAbsoluteIndex;
                if (distanceIntoOriginalSpan <= originalSpan.Length)
                {
                    csharpIndex = mapping.GeneratedSpan.AbsoluteIndex + distanceIntoOriginalSpan;
                    csharpPosition = csharpDocument.Text.GetLinePosition(csharpIndex);
                    return true;
                }
            }
            else
            {
                // This span (and all following) are after the area we're interested in
                break;
            }
        }

        csharpPosition = default;
        csharpIndex = default;
        return false;
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
    public static bool TryMapToCSharpDocumentLinePosition(RazorCodeDocument codeDocument, int razorIndex, out LinePosition csharpPosition, out int csharpIndex, out bool inDeclDocument)
    {
        inDeclDocument = false;
        if (TryMapToCSharpDocumentPosition(codeDocument.GetRequiredCSharpDocument(declarationDocument: false), razorIndex, out csharpPosition, out csharpIndex))
        {
            return true;
        }

        inDeclDocument = true;
        if (codeDocument.GetCSharpDocument(declarationDocument: true) is { } declDocument &&
            TryMapToCSharpDocumentPosition(declDocument, razorIndex, out csharpPosition, out csharpIndex))
        {
            return true;
        }

        return false;
    }

    public static bool IsInStringLiteral(
        RazorCodeDocument codeDocument,
        SyntaxNode csharpSyntaxRoot,
        SyntaxNode? declSyntaxRoot,
        int razorIndex,
        bool multilineOnly)
    {
        if (!TryMapToCSharpDocumentLinePosition(codeDocument, razorIndex, out _, out var csharpIndex, out var inDeclDocument))
        {
            return false;
        }

        var syntaxRoot = inDeclDocument
            ? declSyntaxRoot
            : csharpSyntaxRoot;

        return syntaxRoot?.FindNode(new TextSpan(csharpIndex, 0), getInnermostNodeForTie: true) is { } csharpNode &&
            csharpNode.IsStringLiteral(multilineOnly);
    }
}
