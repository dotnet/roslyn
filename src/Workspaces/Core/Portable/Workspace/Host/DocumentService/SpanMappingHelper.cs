// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host;

internal static class SpanMappingHelper
{
    public static bool CanMapSpans(Document document)
    {
        if (document is SourceGeneratedDocument sourceGeneratedDocument &&
            document.Project.Solution.Services.GetService<ISourceGeneratedDocumentSpanMappingService>() is { } sourceGeneratedSpanMappingService)
        {
            return sourceGeneratedSpanMappingService.CanMapSpans(sourceGeneratedDocument);
        }

        return document.DocumentServiceProvider.GetService<ISpanMappingService>() is not null;
    }

    public static async Task<ImmutableArray<MappedSpanResult>?> TryGetMappedSpanResultAsync(Document document, ImmutableArray<TextSpan> textSpans, CancellationToken cancellationToken)
    {
        if (document is SourceGeneratedDocument sourceGeneratedDocument &&
            document.Project.Solution.Services.GetService<ISourceGeneratedDocumentSpanMappingService>() is { } sourceGeneratedSpanMappingService)
        {
            var result = await sourceGeneratedSpanMappingService.MapSpansAsync(sourceGeneratedDocument, textSpans, cancellationToken).ConfigureAwait(false);
            if (result.IsDefaultOrEmpty)
            {
                return null;
            }

            Contract.ThrowIfFalse(textSpans.Length == result.Length,
                $"The number of input spans {textSpans.Length} should match the number of mapped spans returned {result.Length}");
            return result;
        }

        var spanMappingService = document.DocumentServiceProvider.GetService<ISpanMappingService>();
        if (spanMappingService == null)
        {
            return null;
        }

        var mappedSpanResult = await spanMappingService.MapSpansAsync(document, textSpans, cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfFalse(textSpans.Length == mappedSpanResult.Length,
            $"The number of input spans {textSpans.Length} should match the number of mapped spans returned {mappedSpanResult.Length}");
        return mappedSpanResult;
    }
}
