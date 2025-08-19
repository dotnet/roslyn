// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UserFacingStrings;

/// <summary>
/// Language service that watches documents for string changes and maintains a cache of AI analysis results.
/// Minimizes AI calls by only analyzing new or changed strings.
/// </summary>
internal interface IUserFacingStringDocumentWatcher : ILanguageService
{
    /// <summary>
    /// Notifies the watcher that a document has changed and may need analysis.
    /// </summary>
    void OnDocumentChanged(DocumentId documentId, VersionStamp version);

    /// <summary>
    /// Attempts to get cached analysis for a string without triggering new analysis.
    /// </summary>
    bool TryGetCachedAnalysis(string stringValue, string context, out UserFacingStringAnalysis analysis);


    /// <summary>
    /// Analyzes a specific list of strings with their basic contexts in the background.
    /// This method processes only the provided strings rather than the entire document.
    /// </summary>
    Task AnalyzeSpecificStringsAsync(
        Document document,
        IReadOnlyList<(string stringValue, string basicContext, TextSpan location)> stringsToAnalyze,
        CancellationToken cancellationToken);
}
