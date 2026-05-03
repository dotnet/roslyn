// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddMissingImports;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTestGenerator.Api;

#pragma warning disable CA1822 // Mark members as static. Existing binary api with UnitTestGenerator.

[Export, Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class UnitTestGeneratorAddMissingImportsFeatureServiceAccessor()
{
    internal async Task<Document> AddMissingImportsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
    {
        var service = document.Project.GetRequiredLanguageService<IAddMissingImportsFeatureService>();

        // Unfortunately, the unit testing system doesn't have a way to report progress.
        return await service.AddMissingImportsAsync(document, textSpan, CodeAnalysisProgress.None, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<WrappedMissingImportsAnalysisResult> AnalyzeAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
    {
        var service = document.Project.GetRequiredLanguageService<IAddMissingImportsFeatureService>();
        var result = await service.AnalyzeAsync(document, textSpan, cleanupDocument: true, cancellationToken).ConfigureAwait(false);
        return new WrappedMissingImportsAnalysisResult(result.SelectAsArray(data => new WrappedAddImportFixData(data)));
    }

    internal async Task<Document> AddMissingImportsAsync(Document document, WrappedMissingImportsAnalysisResult analysisResult, CancellationToken cancellationToken)
    {
        var service = document.Project.GetRequiredLanguageService<IAddMissingImportsFeatureService>();
        var unwrappedResult = analysisResult.AddImportFixDatas.SelectAsArray(result => result.Underlying);

        // Unfortunately, the unit testing system doesn't have a way to report progress.
        return await service.AddMissingImportsAsync(document, unwrappedResult, CodeAnalysisProgress.None, cancellationToken).ConfigureAwait(false);
    }
}
