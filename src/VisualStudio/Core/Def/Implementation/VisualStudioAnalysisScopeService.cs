// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal sealed class VisualStudioAnalysisScopeService : IAnalysisScopeService
    {
        private readonly Workspace _workspace;
        private readonly IAsyncServiceProvider _serviceProvider;
        private readonly IThreadingContext _threadingContext;
        private readonly IOptionService _optionService;

        private RoslynPackage? _roslynPackage;

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public VisualStudioAnalysisScopeService(Workspace workspace, IOptionService optionService, IAsyncServiceProvider serviceProvider, IThreadingContext threadingContext)
        {
            _workspace = workspace;
            _serviceProvider = serviceProvider;
            _threadingContext = threadingContext;

            _optionService = optionService;
            _optionService.OptionChanged += OnOptionChanged;
        }

        public event EventHandler? AnalysisScopeChanged;

        public ValueTask<BackgroundAnalysisScope> GetAnalysisScopeAsync(Project project, CancellationToken cancellationToken)
            => GetAnalysisScopeAsync(project.Solution.Options, project.Language, cancellationToken);

        public async ValueTask<BackgroundAnalysisScope> GetAnalysisScopeAsync(OptionSet options, string language, CancellationToken cancellationToken)
        {
            if (!SolutionCrawlerOptions.LowMemoryForcedMinimalBackgroundAnalysis)
            {
                var roslynPackage = await TryGetRoslynPackageAsync(cancellationToken).ConfigureAwait(false);
                if (roslynPackage is { AnalysisScope: { } analysisScope })
                    return analysisScope;
            }

            return SolutionCrawlerOptions.GetBackgroundAnalysisScopeFromOptions(options, language);
        }

        private async ValueTask<RoslynPackage?> TryGetRoslynPackageAsync(CancellationToken cancellationToken)
        {
            if (_roslynPackage is null)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                var shell = (IVsShell7?)await _serviceProvider.GetServiceAsync(typeof(SVsShell)).ConfigureAwait(true);
                Assumes.Present(shell);
                await shell.LoadPackageAsync(typeof(RoslynPackage).GUID);

                if (ErrorHandler.Succeeded(((IVsShell)shell).IsPackageLoaded(typeof(RoslynPackage).GUID, out var package)))
                {
                    _roslynPackage = (RoslynPackage)package;
                    _roslynPackage.AnalysisScopeChanged += (_, _) => AnalysisScopeChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            return _roslynPackage;
        }

        private void OnOptionChanged(object sender, OptionChangedEventArgs e)
        {
            if (e.Option != SolutionCrawlerOptions.BackgroundAnalysisScopeOption)
                return;

            if (_roslynPackage is { AnalysisScope: not null })
            {
                // The background analysis scope is overridden by the package, so ignore the change
                return;
            }

            AnalysisScopeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
