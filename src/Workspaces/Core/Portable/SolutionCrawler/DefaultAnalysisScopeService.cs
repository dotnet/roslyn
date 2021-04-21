// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal sealed class DefaultAnalysisScopeService : IAnalysisScopeService
    {
        private readonly IOptionService _optionsService;

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public DefaultAnalysisScopeService(IOptionService optionsService)
        {
            _optionsService = optionsService;
            _optionsService.OptionChanged += OnOptionChanged;
        }

        public event EventHandler? AnalysisScopeChanged;

        public ValueTask<BackgroundAnalysisScope> GetAnalysisScopeAsync(Project project, CancellationToken cancellationToken)
            => GetAnalysisScopeAsync(project.Solution.Options, project.Language, cancellationToken);

        public ValueTask<BackgroundAnalysisScope> GetAnalysisScopeAsync(OptionSet options, string language, CancellationToken cancellationToken)
            => new(SolutionCrawlerOptions.GetBackgroundAnalysisScopeFromOptions(options, language));

        private void OnOptionChanged(object? sender, OptionChangedEventArgs e)
        {
            if (e.Option != SolutionCrawlerOptions.BackgroundAnalysisScopeOption)
                return;

            AnalysisScopeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
