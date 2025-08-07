// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UserFacingStrings;

/// <summary>
/// Main entry point for extracting and analyzing user-facing strings.
/// </summary>
internal static class UserFacingStringExtractor
{
    /// <summary>
    /// Extracts all strings from a document and returns them with AI-generated confidence scores.
    /// </summary>
    /// <param name="document">The document to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Array of strings with confidence scores, sorted by confidence (highest first)</returns>
    internal static async Task<ImmutableArray<(string stringValue, double confidenceScore, string suggestedKey, string reasoning)>> ExtractUserFacingStringsAsync(
        Document document,
        CancellationToken cancellationToken = default)
    {
        var extractorService = document.GetLanguageService<IUserFacingStringExtractorService>();
        if (extractorService == null)
            return ImmutableArray<(string, double, string, string)>.Empty;

        var results = await extractorService.ExtractAndAnalyzeAsync(document, cancellationToken).ConfigureAwait(false);
        
        // Return sorted by confidence score (highest first)
        return results
            .Select(r => (r.candidate.Value, r.analysis.ConfidenceScore, r.analysis.SuggestedResourceKey, r.analysis.Reasoning))
            .OrderByDescending(r => r.ConfidenceScore)
            .ToImmutableArray();
    }
}
