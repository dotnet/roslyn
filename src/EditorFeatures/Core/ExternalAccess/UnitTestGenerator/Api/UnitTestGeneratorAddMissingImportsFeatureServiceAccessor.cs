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

[Export]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class UnitTestGeneratorAddMissingImportsFeatureServiceAccessor()
{
#pragma warning disable CA1822 // Mark members as static
    internal async Task<Document> AddMissingImportsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
#pragma warning restore CA1822
    {
        var service = document.Project.GetRequiredLanguageService<IAddMissingImportsFeatureService>();

        // Unfortunately, the unit testing system doesn't have a way to report progress.
        return await service.AddMissingImportsAsync(document, textSpan, CodeAnalysisProgress.None, cancellationToken).ConfigureAwait(false);
    }

#pragma warning disable CA1822 // Mark members as static
    internal async Task<WrappedMissingImportsAnalysisResult> AnalyzeAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
#pragma warning restore CA1822
    {
        var service = document.Project.GetRequiredLanguageService<IAddMissingImportsFeatureService>();
        var result = await service.AnalyzeAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
        return new WrappedMissingImportsAnalysisResult(result.AddImportFixData.SelectAsArray(data => new WrappedAddImportFixData(data)));
    }

#pragma warning disable CA1822 // Mark members as static
    internal async Task<Document> AddMissingImportsAsync(Document document, WrappedMissingImportsAnalysisResult analysisResult, CancellationToken cancellationToken)
#pragma warning restore CA1822
    {
        var service = document.Project.GetRequiredLanguageService<IAddMissingImportsFeatureService>();
        var unwrappedResult = new AddMissingImportsAnalysisResult(analysisResult.AddImportFixDatas.SelectAsArray(result => result.Underlying));

        // Unfortunately, the unit testing system doesn't have a way to report progress.
        return await service.AddMissingImportsAsync(document, unwrappedResult, CodeAnalysisProgress.None, cancellationToken).ConfigureAwait(false);
    }
}
