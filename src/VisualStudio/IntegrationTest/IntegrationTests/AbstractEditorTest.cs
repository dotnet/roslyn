// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        protected AbstractEditorTest(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper)
        {
        }

        protected AbstractEditorTest(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper, string solutionName)
            : this(instanceFactory, testOutputHelper, solutionName, WellKnownProjectTemplates.ClassLibrary)
        {
        }

        protected AbstractEditorTest(
            VisualStudioInstanceFactory instanceFactory,
            ITestOutputHelper testOutputHelper,
            string solutionName,
            string projectTemplate)
           : base(instanceFactory, testOutputHelper)
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
            MarkupTestFile.GetPosition(markupCode, out string code, out int caretPosition);

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
