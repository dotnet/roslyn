// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.UserFacingStrings;

/// <summary>
/// Service for extracting all string literals from code without any filtering.
/// </summary>
internal interface IUserFacingStringExtractorService : ILanguageService
{
    /// <summary>
    /// Extracts all string literals from a document and analyzes them with AI.
    /// No manual filtering is applied - all strings are sent to the AI for analysis.
    /// </summary>
    Task<ImmutableArray<(UserFacingStringCandidate candidate, UserFacingStringAnalysis analysis)>> ExtractAndAnalyzeAsync(
        Document document,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets cached results without triggering new analysis. Fast method for retrieving existing analysis.
    /// </summary>
    Task<ImmutableArray<(UserFacingStringCandidate candidate, UserFacingStringAnalysis analysis)>> GetCachedResultsAsync(
        Document document,
        CancellationToken cancellationToken);
}
