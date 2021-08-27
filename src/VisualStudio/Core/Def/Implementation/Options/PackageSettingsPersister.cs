// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal sealed class PackageSettingsPersister : IOptionPersister
    {
        private readonly RoslynUserOptionsPackage _roslynUserOptionsPackage;
        private readonly IGlobalOptionService _optionService;

        public PackageSettingsPersister(
            RoslynUserOptionsPackage roslynUserOptionsPackage,
            IGlobalOptionService optionService)
        {
            _roslynUserOptionsPackage = roslynUserOptionsPackage;
            _optionService = optionService;

            _roslynUserOptionsPackage.AnalysisScopeChanged += OnAnalysisScopeChanged;
            _optionService.RefreshOption(new OptionKey(SolutionCrawlerOptions.SolutionBackgroundAnalysisScopeOption), _roslynUserOptionsPackage.AnalysisScope);
        }

        private void OnAnalysisScopeChanged(object? sender, EventArgs e)
        {
            _optionService.RefreshOption(new OptionKey(SolutionCrawlerOptions.SolutionBackgroundAnalysisScopeOption), _roslynUserOptionsPackage.AnalysisScope);
        }

        public bool TryFetch(OptionKey optionKey, out object? value)
        {
            // This option is refreshed via the constructor to avoid UI dependencies when retrieving option values
            Contract.ThrowIfTrue(Equals(optionKey.Option, SolutionCrawlerOptions.SolutionBackgroundAnalysisScopeOption));

            value = null;
            return false;
        }

        public bool TryPersist(OptionKey optionKey, object? value)
        {
            if (!Equals(optionKey.Option, SolutionCrawlerOptions.SolutionBackgroundAnalysisScopeOption))
                return false;

            _roslynUserOptionsPackage.AnalysisScope = (BackgroundAnalysisScope?)value;
            return true;
        }
    }
}
