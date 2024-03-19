// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddMissingImports;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTestGenerator.Api;

[Export]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class UnitTestGeneratorAddMissingImportsFeatureServiceAccessor(IGlobalOptionService globalOptions)
{
    private readonly IGlobalOptionService _globalOptions = globalOptions;

    internal async Task<Document> AddMissingImportsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
    {
        var options = await GetOptionsAsync(document, cancellationToken).ConfigureAwait(false);
        var service = document.Project.GetRequiredLanguageService<IAddMissingImportsFeatureService>();

        // Unfortunately, the unit testing system doesn't have a way to report progress.
        return await service.AddMissingImportsAsync(document, textSpan, options, CodeAnalysisProgress.None, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<WrappedMissingImportsAnalysisResult> AnalyzeAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
    {
        var options = await GetOptionsAsync(document, cancellationToken).ConfigureAwait(false);
        var service = document.Project.GetRequiredLanguageService<IAddMissingImportsFeatureService>();
        var result = await service.AnalyzeAsync(document, textSpan, options, cancellationToken).ConfigureAwait(false);
        return new WrappedMissingImportsAnalysisResult(result.AddImportFixData.SelectAsArray(data => new WrappedAddImportFixData(data)));
    }

    internal async Task<Document> AddMissingImportsAsync(Document document, WrappedMissingImportsAnalysisResult analysisResult, CancellationToken cancellationToken)
    {
        var options = await GetOptionsAsync(document, cancellationToken).ConfigureAwait(false);
        var service = document.Project.GetRequiredLanguageService<IAddMissingImportsFeatureService>();
        var unwrappedResult = new AddMissingImportsAnalysisResult(analysisResult.AddImportFixDatas.SelectAsArray(result => result.Underlying));

        // Unfortunately, the unit testing system doesn't have a way to report progress.
        return await service.AddMissingImportsAsync(document, unwrappedResult, options.CleanupOptions.FormattingOptions, CodeAnalysisProgress.None, cancellationToken).ConfigureAwait(false);
    }

    private async Task<AddMissingImportsOptions> GetOptionsAsync(Document document, CancellationToken cancellationToken)
    {
        var cleanupOptions = await document.GetCodeCleanupOptionsAsync(_globalOptions, cancellationToken).ConfigureAwait(false);

        var options = new AddMissingImportsOptions(
            CleanupOptions: cleanupOptions,
            HideAdvancedMembers: _globalOptions.GetOption(CompletionOptionsStorage.HideAdvancedMembers, document.Project.Language));

        return options;
    }
}
