// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class FullSolutionAnalysisOptionBinding
    {
        private readonly OptionStore _optionStore;
        private readonly string _languageName;

        private readonly Option<bool> _fullSolutionAnalysis;
        private readonly PerLanguageOption<bool?> _closedFileDiagnostics;

        public FullSolutionAnalysisOptionBinding(OptionStore optionStore, string languageName)
        {
            _optionStore = optionStore;
            _languageName = languageName;

            _fullSolutionAnalysis = RuntimeOptions.FullSolutionAnalysis;
            _closedFileDiagnostics = ServiceFeatureOnOffOptions.ClosedFileDiagnostic;
        }

        public bool Value
        {
            get
            {
                return ServiceFeatureOnOffOptions.IsClosedFileDiagnosticsEnabled(_optionStore.GetOptions(), _languageName) &&
                       _optionStore.GetOption(_fullSolutionAnalysis);
            }

            set
            {
                // set normal option first
                _optionStore.SetOption(_closedFileDiagnostics, _languageName, value);

                // we only enable this option if it is disabled. we never disable this option here.
                if (value)
                {
                    _optionStore.SetOption(_fullSolutionAnalysis, value);
                }
            }
        }
    }
}
