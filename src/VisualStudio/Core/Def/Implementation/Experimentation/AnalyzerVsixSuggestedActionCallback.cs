// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Experimentation
{
    [Export(typeof(ISuggestedActionCallback))]
    class AnalyzerVsixSuggestedActionCallback : ISuggestedActionCallback
    {
        private const string AnalyzerEnabledFlight = @"LiveCA/LiveCAcf";
        private const string AnalyzerVsixHyperlink = @"https://aka.ms/livecodeanalysis";

        private readonly VisualStudioWorkspace _workspace;
        private readonly IInfoBarService _infoBarService;

        [ImportingConstructor]
        public AnalyzerVsixSuggestedActionCallback(IInfoBarService infoBarService, VisualStudioWorkspace workspace)
        {
            _workspace = workspace;
            _infoBarService = infoBarService;
        }

        public void OnSuggestedActionExecuted(SuggestedAction action)
        {
            var experimentationService = _workspace.Services.GetRequiredService<IExperimentationService>();
            if (experimentationService.IsExperimentEnabled(AnalyzerEnabledFlight))
            {
                _infoBarService.ShowInfoBarInGlobalView(
                    ServicesVSResources.Analyzer_vsix_try_description,
                    new InfoBarUI(title: ServicesVSResources.Analyzer_vsix_hyperlink,
                                  kind: InfoBarUI.UIKind.HyperLink,
                                  action: new Action(OpenInstallHyperlink)));
            }
        }

        private void OpenInstallHyperlink()
        {
            System.Diagnostics.Process.Start(AnalyzerVsixHyperlink);
        }
    }
}
