// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractEditorTest : AbstractIntegrationTest
    {
        private readonly string _solutionName;
        private readonly string _projectTemplate;

        protected AbstractEditorTest(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        protected AbstractEditorTest(VisualStudioInstanceFactory instanceFactory, string solutionName)
            : this(instanceFactory, solutionName, WellKnownProjectTemplates.ClassLibrary)
        {
        }

        protected AbstractEditorTest(
            VisualStudioInstanceFactory instanceFactory,
            string solutionName,
            string projectTemplate)
           : base(instanceFactory)
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
                VisualStudio.SolutionExplorer.CreateSolution(_solutionName);
                VisualStudio.SolutionExplorer.AddProject(new ProjectUtils.Project(ProjectName), _projectTemplate, LanguageName);
                VisualStudio.SolutionExplorer.RestoreNuGetPackages(new ProjectUtils.Project(ProjectName));

                // Winforms and XAML do not open text files on creation
                // so these editor tasks will not work if that is the project template being used.
                if (_projectTemplate != WellKnownProjectTemplates.WinFormsApplication &&
                    _projectTemplate != WellKnownProjectTemplates.WpfApplication &&
                    _projectTemplate != WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
                {
                    VisualStudio.Editor.SetUseSuggestionMode(false);
                    ClearEditor();
                }
            }
        }

        protected void ClearEditor()
            => SetUpEditor("$$");

        protected void SetUpEditor(string markupCode)
        {
            MarkupTestFile.GetPosition(markupCode, out var code, out int caretPosition);

            VisualStudio.Editor.DismissCompletionSessions();
            VisualStudio.Editor.DismissLightBulbSession();

            var originalValue = VisualStudio.Workspace.IsPrettyListingOn(LanguageName);

            VisualStudio.Workspace.SetPrettyListing(LanguageName, false);
            try
            {
                VisualStudio.Editor.SetText(code);
                VisualStudio.Editor.MoveCaret(caretPosition);
                VisualStudio.Editor.Activate();
            }
            finally
            {
                VisualStudio.Workspace.SetPrettyListing(LanguageName, originalValue);
            }
        }

        protected ClassifiedToken[] GetLightbulbPreviewClassification(string menuText)
        {
            return VisualStudio.Editor.GetLightbulbPreviewClassification(menuText);
        }
    }
}
