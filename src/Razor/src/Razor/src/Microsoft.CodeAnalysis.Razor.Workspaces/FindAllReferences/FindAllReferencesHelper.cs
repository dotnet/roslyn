// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.FindAllReferences;

internal static class FindAllReferencesHelper
{
    public static async Task<string?> GetResultTextAsync(IDocumentMappingService documentMappingService, ISolutionQueryOperations solutionQueryOperations, int lineNumber, string filePath, CancellationToken cancellationToken)
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
        // To identify which situation we're in, we try to map the start and the end of the line to C#, as an indicator. If
        // either start or end fail to map, it means the entire line is not C#

        // TODO: Note the call to ISolutionQueryOperations.GetProjectsContainingDocument(...) will be removed with the introduction of solution snapshots.
        if (solutionQueryOperations.GetProjectsContainingDocument(filePath).FirstOrDefault() is { } project &&
            project.TryGetDocument(filePath, out var document))
        {
            var codeDoc = await document.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
            var line = codeDoc.Source.Text.Lines[lineNumber];
            var csharpDocument = codeDoc.GetRequiredCSharpDocument();
            if (!documentMappingService.TryMapToCSharpDocumentPosition(csharpDocument, line.Start, out _, out _) ||
                !documentMappingService.TryMapToCSharpDocumentPosition(csharpDocument, line.End, out _, out _))
            {
                var start = line.GetFirstNonWhitespacePosition() ?? line.Start;
                return codeDoc.Source.Text.ToString(TextSpan.FromBounds(start, line.End));
            }
        }

        return null;
    }
}
