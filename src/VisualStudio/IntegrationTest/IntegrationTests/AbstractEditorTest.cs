// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
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
        private readonly string _targetFrameworkMoniker;

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
            string projectTemplate,
            string targetFrameworkMoniker = null)
           : base(instanceFactory, testOutputHelper)
        {
            _solutionName = solutionName;
            _projectTemplate = projectTemplate;
            _targetFrameworkMoniker = targetFrameworkMoniker;
        }

        protected abstract string LanguageName { get; }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);

            if (_solutionName != null)
            {
                VisualStudio.SolutionExplorer.CreateSolution(_solutionName);
                var project = new ProjectUtils.Project(ProjectName);
                VisualStudio.SolutionExplorer.AddProject(project, _projectTemplate, LanguageName);

                if (!string.IsNullOrEmpty(_targetFrameworkMoniker))
                {
                    UpdateProjectTargetFramework(project, _targetFrameworkMoniker);
                }

                VisualStudio.SolutionExplorer.RestoreNuGetPackages(project);

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

        protected void UpdateProjectTargetFramework(ProjectUtils.Project project, string targetFrameworkMoniker)
        {
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);

            VisualStudio.SolutionExplorer.EditProjectFile(project);
            VisualStudio.Editor.SetText($@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{targetFrameworkMoniker}</TargetFramework>
  </PropertyGroup>
</Project>");
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
