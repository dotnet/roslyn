// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Options;

internal sealed class PackageSettingsPersister : IOptionPersister
{
    private RoslynPackage? _lazyRoslynPackage;
    private readonly IGlobalOptionService _optionService;

    public PackageSettingsPersister(
        IThreadingContext threadingContext,
        IAsyncServiceProvider serviceProvider,
        IGlobalOptionService optionService)
    {
        _optionService = optionService;

        // Start the process of loading the Roslyn package, but don't wait for it to complete.
        // Roslyn package load will in turn invoke our Initialize method with the instance of the package.
        LoadRoslynPackageAsync(threadingContext, serviceProvider).ReportNonFatalErrorAsync().Forget();
    }

    private static async Task LoadRoslynPackageAsync(IThreadingContext threadingContext, IAsyncServiceProvider serviceProvider)
    {
        var cancellationToken = threadingContext.DisposalToken;
        await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        _ = await RoslynPackage.GetOrLoadAsync(threadingContext, serviceProvider, cancellationToken).ConfigureAwait(true);
    }

    public void Initialize(RoslynPackage package)
    {
        Assumes.Null(_lazyRoslynPackage);

        _lazyRoslynPackage = package;
        _optionService.RefreshOption(new OptionKey2(SolutionCrawlerOptionsStorage.SolutionBackgroundAnalysisScopeOption), _lazyRoslynPackage.AnalysisScope);
        _lazyRoslynPackage.AnalysisScopeChanged += OnAnalysisScopeChanged;
    }

    private void OnAnalysisScopeChanged(object? sender, EventArgs e)
    {
        Assumes.Present(_lazyRoslynPackage);
        _optionService.RefreshOption(new OptionKey2(SolutionCrawlerOptionsStorage.SolutionBackgroundAnalysisScopeOption), _lazyRoslynPackage.AnalysisScope);
    }

    public bool TryFetch(OptionKey2 optionKey, out object? value)
    {
        // This option is refreshed via the constructor to avoid UI dependencies when retrieving option values. If
        // we happen to reach this point before the value is available, try to obtain it without blocking, and
        // otherwise fall back to the default.
        if (optionKey.Option == SolutionCrawlerOptionsStorage.SolutionBackgroundAnalysisScopeOption)
        {
            if (_lazyRoslynPackage is not null)
            {
                value = _lazyRoslynPackage.AnalysisScope;
                return true;
            }
            else
            {
                value = SolutionCrawlerOptionsStorage.SolutionBackgroundAnalysisScopeOption.Definition.DefaultValue;
                return true;
            }
        }

        value = null;
        return false;
    }

    public bool TryPersist(OptionKey2 optionKey, object? value)
    {
        if (!Equals(optionKey.Option, SolutionCrawlerOptionsStorage.SolutionBackgroundAnalysisScopeOption))
            return false;

        if (_lazyRoslynPackage is not null)
        {
            _lazyRoslynPackage.AnalysisScope = (BackgroundAnalysisScope?)value;
        }

        return true;
    }
}
