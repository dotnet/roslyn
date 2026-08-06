// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.FindAllReferences;

internal static class FindAllReferencesHelper
{
    public static async Task<string?> GetResultTextAsync(ISolutionQueryOperations solutionQueryOperations, int lineNumber, string filePath, CancellationToken cancellationToken)
    {
        // Roslyn will have sent us back text that comes from the .g.cs file, but that is often not helpful. For example give:
        //
        // <SurveyPrompt Title="Blah" />
        //
        // A FAR for the Title property will return just the word "Title" in the Text of the reference item, which does not
        // help the user reason about the result. For such cases, its better to return the text from the Razor file, even
        // though it will be unclassified, as it will help the user.
        //
        // However, for cases where the result comes from a C# block, for example:
        //
        // @code {
        //    public string Title { get; set; }
        // }
        //
        // A FAR for the Title property here will return the full line of code, classified by Roslyn, so we don't want to
        // do anything for those.
        //
        // To identify which situation we're in, we check whether both ends of the line fall within the same source
        // mapping. If they do, the entire line is within a single C# block and the C# text is appropriate. If either
        // end is outside any mapping, or the two ends belong to different mappings (which happens for lines that mix
        // Razor transitions with C# — e.g. "@if (condition)" where "@" is outside any mapping and "if (condition)"
        // is in its own mapping), we return the Razor source text instead.

        // TODO: Note the call to ISolutionQueryOperations.GetProjectsContainingDocument(...) will be removed with the introduction of solution snapshots.
        if (solutionQueryOperations.GetProjectsContainingDocument(filePath).FirstOrDefault() is { } project &&
            project.TryGetDocument(filePath, out var document))
        {
            var codeDoc = await document.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
            var line = codeDoc.Source.Text.Lines[lineNumber];
            var csharpDocument = codeDoc.GetRequiredCSharpDocument();

            var startMapping = FindContainingSourceMapping(csharpDocument, line.Start);
            // Use line.End - 1 (last content character) rather than the exclusive line.End boundary
            // to avoid false positives when line.End lands exactly on a mapping edge.
            var endMapping = line.End > line.Start
                ? FindContainingSourceMapping(csharpDocument, line.End - 1)
                : startMapping;

            // Only skip overriding the text (i.e. only use C# text) if both ends of the line are
            // within the same source mapping, meaning the entire line is pure C# code.
            if (startMapping is not null && endMapping is not null && ReferenceEquals(startMapping, endMapping))
            {
                return null;
            }

            var start = line.GetFirstNonWhitespacePosition() ?? line.Start;
            return codeDoc.Source.Text.ToString(TextSpan.FromBounds(start, line.End));
        }

        return null;
    }

    /// <summary>
    /// Finds the <see cref="SourceMapping"/> in <paramref name="csharpDocument"/> that contains
    /// <paramref name="razorIndex"/>, or <see langword="null"/> if no mapping covers that position.
    /// Mirrors the containment logic used by <c>TryMapToCSharpDocumentPositionInternal</c>.
    /// </summary>
    private static SourceMapping? FindContainingSourceMapping(RazorCSharpDocument csharpDocument, int razorIndex)
    {
        foreach (var mapping in csharpDocument.SourceMappingsSortedByOriginal)
        {
            var originalSpan = mapping.OriginalSpan;
            var originalAbsoluteIndex = originalSpan.AbsoluteIndex;
            if (originalAbsoluteIndex <= razorIndex)
            {
                // Treat the mapping as owning the edge at its end (hence <=), mirroring
                // TryMapToCSharpDocumentPositionInternal's inclusive-end semantics.
                var distanceIntoOriginalSpan = razorIndex - originalAbsoluteIndex;
                if (distanceIntoOriginalSpan <= originalSpan.Length)
                {
                    return mapping;
                }
            }
            else
            {
                // Mappings are sorted by original position; once we've passed razorIndex we can stop.
                break;
            }
        }

        return null;
    }
}
