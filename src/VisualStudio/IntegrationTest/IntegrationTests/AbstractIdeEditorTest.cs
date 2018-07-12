// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractIdeEditorTest : AbstractIdeIntegrationTest
    {
        private readonly string _solutionName;
        private readonly string _projectTemplate;

        protected AbstractIdeEditorTest()
        {
        }

        protected AbstractIdeEditorTest(string solutionName)
            : this(solutionName, WellKnownProjectTemplates.ClassLibrary)
        {
        }

        protected AbstractIdeEditorTest(
            string solutionName,
            string projectTemplate)
        {
            _solutionName = solutionName;
            _projectTemplate = projectTemplate;
        }

        protected abstract string LanguageName
        {
            get;
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (_solutionName != null)
            {
                await SolutionExplorer.CreateSolutionAsync(_solutionName);
                await SolutionExplorer.AddProjectAsync(ProjectName, _projectTemplate, LanguageName);
                await SolutionExplorer.RestoreNuGetPackagesAsync(ProjectName);

                // Winforms and XAML do not open text files on creation
                // so these editor tasks will not work if that is the project template being used.
                if (_projectTemplate != WellKnownProjectTemplates.WinFormsApplication &&
                    _projectTemplate != WellKnownProjectTemplates.WpfApplication &&
                    _projectTemplate != WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
                {
                    await Workspace.SetUseSuggestionModeAsync(false);
                    await ClearEditorAsync();
                }
            }
        }

        protected async Task ClearEditorAsync()
            => await SetUpEditorAsync("$$");

        protected async Task SetUpEditorAsync(string markupCode)
        {
            MarkupTestFile.GetPosition(markupCode, out string code, out int caretPosition);

            var originalValue = Workspace.IsPrettyListingOn(LanguageName);

            await Workspace.SetPrettyListingAsync(LanguageName, false);
            try
            {
                await Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
                await Editor.SetTextAsync(code);
                await Editor.MoveCaretAsync(caretPosition);
                await Editor.ActivateAsync();
            }
            finally
            {
                await Workspace.SetPrettyListingAsync(LanguageName, originalValue);
            }
        }

        protected async Task<ClassificationSpan[]> GetLightbulbPreviewClassificationAsync(string menuText)
        {
            return await Editor.GetLightbulbPreviewClassificationsAsync(menuText);
        }
    }
}
