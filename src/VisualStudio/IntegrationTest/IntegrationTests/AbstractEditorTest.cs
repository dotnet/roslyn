// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Roslyn.Test.Utilities;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractEditorTest : AbstractIntegrationTest
    {
        private readonly string _solutionName;
        private readonly string _projectTemplate;

        protected AbstractEditorTest(string solutionName)
            : this(solutionName, WellKnownProjectTemplates.ClassLibrary)
        {
        }

        protected AbstractEditorTest(
            string solutionName,
            string projectTemplate)
        {
            _solutionName = solutionName;
            _projectTemplate = projectTemplate;
        }

        protected abstract string LanguageName { get; }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);

            if (_solutionName != null)
            {
                VisualStudioInstance.SolutionExplorer.CreateSolution(_solutionName);
                VisualStudioInstance.SolutionExplorer.AddProject(new ProjectUtils.Project(ProjectName), _projectTemplate, LanguageName);
                VisualStudioInstance.SolutionExplorer.RestoreNuGetPackages(new ProjectUtils.Project(ProjectName));

                // Winforms and XAML do not open text files on creation
                // so these editor tasks will not work if that is the project template being used.
                if (_projectTemplate != WellKnownProjectTemplates.WinFormsApplication &&
                    _projectTemplate != WellKnownProjectTemplates.WpfApplication &&
                    _projectTemplate != WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
                {
                    VisualStudioInstance.Workspace.SetUseSuggestionMode(false);
                    ClearEditor();
                }
            }
        }

        protected void ClearEditor()
            => SetUpEditor("$$");

        protected void SetUpEditor(string markupCode)
        {
            MarkupTestFile.GetPosition(markupCode, out string code, out int caretPosition);

            var originalValue = VisualStudioInstance.Workspace.IsPrettyListingOn(LanguageName);

            VisualStudioInstance.Workspace.SetPrettyListing(LanguageName, false);
            try
            {
                VisualStudioInstance.Editor.SetText(code);
                VisualStudioInstance.Editor.MoveCaret(caretPosition);
                VisualStudioInstance.Editor.Activate();
            }
            finally
            {
                VisualStudioInstance.Workspace.SetPrettyListing(LanguageName, originalValue);
            }
        }

        protected ClassifiedToken[] GetLightbulbPreviewClassification(string menuText)
        {
            return VisualStudioInstance.Editor.GetLightbulbPreviewClassification(menuText);
        }
    }
}
