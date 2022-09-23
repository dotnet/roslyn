// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal sealed class PackageSettingsPersister : IOptionPersister
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IAsyncServiceProvider _serviceProvider;
        private readonly IGlobalOptionService _optionService;
        private RoslynPackage? _lazyRoslynPackage;

        public PackageSettingsPersister(
            IThreadingContext threadingContext,
            IAsyncServiceProvider serviceProvider,
            IGlobalOptionService optionService)
        {
            _threadingContext = threadingContext;
            _serviceProvider = serviceProvider;
            _optionService = optionService;

            // Start the process of loading the Roslyn package and getting options, but don't wait for it to complete.
            // The setting will be refreshed once available.
            InitializeAsync(_threadingContext.DisposalToken).ReportNonFatalErrorAsync().Forget();
        }

        private async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _lazyRoslynPackage = await RoslynPackage.GetOrLoadAsync(_threadingContext, _serviceProvider, cancellationToken).ConfigureAwait(true);
            Assumes.Present(_lazyRoslynPackage);

            _optionService.RefreshOption(new OptionKey(SolutionCrawlerOptionsStorage.SolutionBackgroundAnalysisScopeOption), _lazyRoslynPackage.AnalysisScope);
            _lazyRoslynPackage.AnalysisScopeChanged += OnAnalysisScopeChanged;
        }

        private void OnAnalysisScopeChanged(object? sender, EventArgs e)
        {
            Assumes.Present(_lazyRoslynPackage);
            _optionService.RefreshOption(new OptionKey(SolutionCrawlerOptionsStorage.SolutionBackgroundAnalysisScopeOption), _lazyRoslynPackage.AnalysisScope);
        }

        public bool TryFetch(OptionKey optionKey, out object? value)
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
                    value = SolutionCrawlerOptionsStorage.SolutionBackgroundAnalysisScopeOption.DefaultValue;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public bool TryPersist(OptionKey optionKey, object? value)
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
}
