// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Utilities;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractEditorTest : AbstractIntegrationTest
    {
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
            VisualStudio.SolutionExplorer.CreateSolution(solutionName);
            VisualStudio.SolutionExplorer.AddProject(new ProjectUtils.Project(ProjectName), projectTemplate, LanguageName);

            if (projectTemplate == WellKnownProjectTemplates.CSharpNetCoreClassLibrary ||
                projectTemplate == WellKnownProjectTemplates.CSharpNetCoreConsoleApplication ||
                projectTemplate == WellKnownProjectTemplates.CSharpNetCoreUnitTest ||
                projectTemplate == WellKnownProjectTemplates.CSharpNetCoreXUnitTest ||
                projectTemplate == WellKnownProjectTemplates.VisualBasicNetCoreClassLibrary ||
                projectTemplate == WellKnownProjectTemplates.VisualBasicNetCoreConsoleApplication)
            {
                // Run a build before proceeding to ensure the project system downloads and
                // resolves the required .NET Core dependencies, then activate the editor
                // window so we end up in the expected state.
                VisualStudio.SolutionExplorer.BuildSolution(waitForBuildToFinish: true);
                VisualStudio.Editor.Activate();

                // Then close and reopen the solution to ensure the Error List is purged of
                // stale warnings/errors.
                string solutionFile = VisualStudio.SolutionExplorer.SolutionFileFullPath;
                VisualStudio.SolutionExplorer.CloseSolution(true);
                VisualStudio.SolutionExplorer.OpenSolution(solutionFile);
            }

            // Winforms and XAML do not open text files on creation
            // so these editor tasks will not work if that is the project template being used.
            if (projectTemplate != WellKnownProjectTemplates.WinFormsApplication &&
                projectTemplate != WellKnownProjectTemplates.WpfApplication &&
                projectTemplate != WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
            {
                VisualStudio.Workspace.SetUseSuggestionMode(false);
                ClearEditor();
            }
        }

        protected abstract string LanguageName { get; }

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