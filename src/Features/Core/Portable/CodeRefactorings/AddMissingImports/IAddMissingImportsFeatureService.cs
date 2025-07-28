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
    Task<ImmutableArray<AddImportFixData>> AnalyzeAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the same action as <see cref="IAddMissingImportsFeatureServiceExtensions.AddMissingImportsAsync"/> but
    /// with a predetermined analysis of the input instead of recalculating it.
    /// </summary>
    Task<Document> AddMissingImportsAsync(Document document, ImmutableArray<AddImportFixData> analysisResult, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken);
}

internal static class IAddMissingImportsFeatureServiceExtensions
{
    extension(IAddMissingImportsFeatureService service)
    {
        /// <summary>
        /// Attempts to add missing imports to the document within the provided <paramref name="textSpan"/>. The imports
        /// added will not add references to the project. 
        /// </summary>
        public async Task<Document> AddMissingImportsAsync(
    Document document, TextSpan textSpan, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
        {
            var analysisResult = await service.AnalyzeAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            return await service.AddMissingImportsAsync(
                document, analysisResult, progressTracker, cancellationToken).ConfigureAwait(false);
        }
    }
}
