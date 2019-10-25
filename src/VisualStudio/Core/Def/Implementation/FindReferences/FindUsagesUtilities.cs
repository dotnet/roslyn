// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal static class FindUsagesUtilities
    {
        internal static async Task<(Guid projectId, Guid documentId, string projectName, SourceText sourceText)> GetGuidAndProjectNameAndSourceTextAsync(Document document, CancellationToken token)
        {
            // The FAR system needs to know the guid for the project that a def/reference is 
            // from (to support features like filtering).  Normally that would mean we could
            // only support this from a VisualStudioWorkspace.  However, we want till work 
            // in cases like Any-Code (which does not use a VSWorkspace).  So we are tolerant
            // when we have another type of workspace.  This means we will show results, but
            // certain features (like filtering) may not work in that context.
            var vsWorkspace = document.Project.Solution.Workspace as VisualStudioWorkspace;

            var projectName = document.Project.Name;
            var projectGuid = vsWorkspace?.GetProjectGuid(document.Project.Id) ?? Guid.Empty;
            var documentGuid = vsWorkspace?.GetDocumentIdInCurrentContext(document.Id).Id ?? Guid.Empty;

            var sourceText = await document.GetTextAsync(token).ConfigureAwait(false);
            return (projectGuid, documentGuid, projectName, sourceText);
        }

        internal static async Task<(ExcerptResult, SourceText)> ExcerptAsync(SourceText sourceText, DocumentSpan documentSpan, CancellationToken token)
        {
            var excerptService = documentSpan.Document.Services.GetService<IDocumentExcerptService>();
            if (excerptService != null)
            {
                var result = await excerptService.TryExcerptAsync(documentSpan.Document, documentSpan.SourceSpan, ExcerptMode.SingleLine, token).ConfigureAwait(false);
                if (result != null)
                {
                    return (result.Value, GetLineContainingPosition(result.Value.Content, result.Value.MappedSpan.Start));
                }
            }

            var classificationResult = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(documentSpan, token).ConfigureAwait(false);

            // need to fix the span issue tracking here - https://github.com/dotnet/roslyn/issues/31001
            var excerptResult = new ExcerptResult(
                sourceText,
                classificationResult.HighlightSpan,
                classificationResult.ClassifiedSpans,
                documentSpan.Document,
                documentSpan.SourceSpan);

            return (excerptResult, GetLineContainingPosition(sourceText, documentSpan.SourceSpan.Start));
        }

        internal static async Task<MappedSpanResult?> TryMapAndGetFirstAsync(DocumentSpan documentSpan, SourceText sourceText, CancellationToken cancellationToken)
        {
            var service = documentSpan.Document.Services.GetService<ISpanMappingService>();
            if (service == null)
            {
                return new MappedSpanResult(documentSpan.Document.FilePath, sourceText.Lines.GetLinePositionSpan(documentSpan.SourceSpan), documentSpan.SourceSpan);
            }

            var results = await service.MapSpansAsync(
                documentSpan.Document, SpecializedCollections.SingletonEnumerable(documentSpan.SourceSpan), cancellationToken).ConfigureAwait(false);

            if (results.IsDefaultOrEmpty)
            {
                return new MappedSpanResult(documentSpan.Document.FilePath, sourceText.Lines.GetLinePositionSpan(documentSpan.SourceSpan), documentSpan.SourceSpan);
            }

            // if span mapping service filtered out the span, make sure
            // to return null so that we remove the span from the result
            return results.FirstOrNullable(r => !r.IsDefault);
        }

        internal static SourceText GetLineContainingPosition(SourceText text, int position)
        {
            var line = text.Lines.GetLineFromPosition(position);

            return text.GetSubText(line.Span);
        }
    }
}
