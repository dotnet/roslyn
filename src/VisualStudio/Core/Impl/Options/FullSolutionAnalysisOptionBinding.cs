// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class FullSolutionAnalysisOptionBinding
    {
        private readonly IOptionService _optionService;
        private readonly string _languageName;

        private readonly Option<bool> _fullSolutionAnalysis;
        private readonly PerLanguageOption<bool> _closedFileDiagnostics;

        public FullSolutionAnalysisOptionBinding(IOptionService optionService, string languageName)
        {
            _optionService = optionService;
            _languageName = languageName;

            _fullSolutionAnalysis = RuntimeOptions.FullSolutionAnalysis;
            _closedFileDiagnostics = ServiceFeatureOnOffOptions.ClosedFileDiagnostic;
        }

        public bool Value
        {
            get
            {
                return _optionService.GetOption(_closedFileDiagnostics, _languageName) &&
                       _optionService.GetOption(_fullSolutionAnalysis);
            }

            set
            {
                var oldOptions = _optionService.GetOptions();

                // set normal option first
                var newOptions = oldOptions.WithChangedOption(_closedFileDiagnostics, _languageName, value);

                // we only enable this option if it is disabled. we never disable this option here.
                if (value)
                {
                    newOptions = newOptions.WithChangedOption(_fullSolutionAnalysis, value);
                }

                _optionService.SetOptions(newOptions);
                OptionLogger.Log(oldOptions, newOptions);
            }
        }
    }
}
