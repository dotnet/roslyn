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
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Options
{
    internal sealed class PackageSettingsPersister : IOptionPersister, IVsPackageLoadEvents, IDisposable
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IAsyncServiceProvider _serviceProvider;
        private readonly IGlobalOptionService _optionService;

        private RoslynPackage? _lazyRoslynPackage;
        private IVsShell6? _shell;
        private uint _packageLoadEventsCookie = VSConstants.VSCOOKIE_NIL;

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

            // We attempt to get or load the RoslynPackage to proceed with the core initialization.
            // However, this package load might fail under certain circumstances.
            // If so, we start listening to the package load events from the shell and
            // invoke InitializeCore when the RoslynPackage gets loaded in the future.
            var package = await RoslynPackage.GetOrLoadAsync(_threadingContext, _serviceProvider, cancellationToken).ConfigureAwait(true);
            if (package != null)
            {
                InitializeCore(package);
            }
            else
            {
                _shell = (IVsShell6?)(await _serviceProvider.GetServiceAsync(typeof(SVsShell)).ConfigureAwait(false));
                Assumes.Present(_shell);
                _packageLoadEventsCookie = _shell.AdvisePackageLoadEvents(this);
            }
        }

        private void InitializeCore(RoslynPackage package)
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

        public void Dispose()
        {
            if (_packageLoadEventsCookie != VSConstants.VSCOOKIE_NIL)
            {
                Assumes.Present(_shell);
                _shell.UnadvisePackageLoadEvents(_packageLoadEventsCookie);
                _packageLoadEventsCookie = VSConstants.VSCOOKIE_NIL;
            }
        }

        #region IVsPackageLoadEvents

        void IVsPackageLoadEvents.OnPackageLoaded(ref Guid packageGuid, IVsPackage package)
        {
            if (packageGuid == typeof(RoslynPackage).GUID)
                InitializeCore((RoslynPackage)package);
        }

        #endregion IVsPackageLoadEvents
    }
}
