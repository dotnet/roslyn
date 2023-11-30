// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServices.Setup;

namespace Microsoft.VisualStudio.LanguageServices.Options
{
    internal sealed class PackageSettingsPersister(IGlobalOptionService optionService) : IOptionPersister
    {
        private RoslynPackage? _lazyRoslynPackage;

        public void Initialize(RoslynPackage package)
        {
            _lazyRoslynPackage = package;
            optionService.RefreshOption(new OptionKey2(SolutionCrawlerOptionsStorage.SolutionBackgroundAnalysisScopeOption), _lazyRoslynPackage.AnalysisScope);
            _lazyRoslynPackage.AnalysisScopeChanged += OnAnalysisScopeChanged;
        }

        private void OnAnalysisScopeChanged(object? sender, EventArgs e)
        {
            Assumes.Present(_lazyRoslynPackage);
            optionService.RefreshOption(new OptionKey2(SolutionCrawlerOptionsStorage.SolutionBackgroundAnalysisScopeOption), _lazyRoslynPackage.AnalysisScope);
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
}
