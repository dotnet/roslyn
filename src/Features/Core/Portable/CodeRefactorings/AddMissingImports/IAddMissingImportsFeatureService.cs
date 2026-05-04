// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddMissingImports;

internal interface IAddMissingImportsFeatureService : ILanguageService
{
    /// <summary>
    /// Analyzes the document inside the <paramref name="textSpan"/> to determine if imports can be added.
    /// </summary>
    /// <param name="cleanupDocument">Whether the document should be cleaned up after an import is added.
    /// For example, in VB this may then case correct previous unbound references based on the new names
    /// brought into scope.</param>
    Task<ImmutableArray<AddImportFixData>> AnalyzeAsync(Document document, TextSpan textSpan, bool cleanupDocument, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the same action as <see cref="IAddMissingImportsFeatureServiceExtensions.AddMissingImportsAsync(
    /// IAddMissingImportsFeatureService, Document, TextSpan, IProgress{CodeAnalysisProgress}, CancellationToken)"/> but
    /// with a predetermined analysis of the input instead of recalculating it.
    /// </summary>
    Task<Document> AddMissingImportsAsync(Document document, ImmutableArray<AddImportFixData> analysisResult, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken);
}

internal static class IAddMissingImportsFeatureServiceExtensions
{
    /// <summary>
    /// Attempts to add missing imports to the document within the provided <paramref name="textSpan"/>. The imports
    /// added will not add references to the project. 
    /// </summary>
    public static Task<Document> AddMissingImportsAsync(
        this IAddMissingImportsFeatureService service, Document document, TextSpan textSpan, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        return AddMissingImportsAsync(service, document, textSpan, cleanupDocument: false, progressTracker, cancellationToken);
    }

    public static async Task<Document> AddMissingImportsAsync(
        this IAddMissingImportsFeatureService service, Document document, TextSpan textSpan, bool cleanupDocument, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        var analysisResult = await service.AnalyzeAsync(document, textSpan, cleanupDocument, cancellationToken).ConfigureAwait(false);
        return await service.AddMissingImportsAsync(
            document, analysisResult, progressTracker, cancellationToken).ConfigureAwait(false);
    }
}
