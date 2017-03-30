// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.Extensions.Options
{
    public static class SetOptionsExtensions
    {
        public static void SetUseSuggestionMode(this AbstractIntegrationTest test, bool value)
    => test.VisualStudio.Instance.VisualStudioWorkspace.SetUseSuggestionMode(value);

        public static void SetQuickInfo(this AbstractIntegrationTest test, bool value)
        {
            test.VisualStudio.Instance.VisualStudioWorkspace.SetQuickInfo(value);
            test.WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        public static void SetOptionInfer(this AbstractIntegrationTest test, ProjectUtils.Project project, bool value)
        {
            test.VisualStudio.Instance.VisualStudioWorkspace.SetOptionInfer(project.Name, value);
            test.WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        public static void SetPersistenceOption(this AbstractIntegrationTest test, bool value)
        {
            test.VisualStudio.Instance.VisualStudioWorkspace.SetPersistenceOption(value);
            test.WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        public static void SetFullSolutionAnalysis(this AbstractIntegrationTest test, bool value)
        {
            test.VisualStudio.Instance.VisualStudioWorkspace.SetPerLanguageOption(
                optionName: "Closed File Diagnostic",
                feature: "ServiceFeaturesOnOff",
                language: LanguageNames.CSharp,
                value: value ? "true" : "false");

            test.VisualStudio.Instance.VisualStudioWorkspace.SetPerLanguageOption(
                optionName: "Closed File Diagnostic",
                feature: "ServiceFeaturesOnOff",
                language: LanguageNames.VisualBasic,
                value: value ? "true" : "false");
        }
    }
}
