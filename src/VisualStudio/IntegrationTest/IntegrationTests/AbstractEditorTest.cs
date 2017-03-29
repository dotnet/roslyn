// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Automation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions;
using Roslyn.VisualStudio.IntegrationTests.Extensions.SolutionExplorer;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractEditorTest : AbstractIntegrationTest
    {
        protected readonly Editor_OutOfProc Editor;

        protected readonly string ProjectName = "TestProj";

        protected AbstractEditorTest(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, visualStudio => visualStudio.Instance.Editor)
        {
            Editor = (Editor_OutOfProc)TextViewWindow;
        }

        protected AbstractEditorTest(VisualStudioInstanceFactory instanceFactory, string solutionName)
            : this(instanceFactory, solutionName, WellKnownProjectTemplates.ClassLibrary)
        {
        }

        protected AbstractEditorTest(
            VisualStudioInstanceFactory instanceFactory,
            string solutionName,
            string projectTemplate)
           : base(instanceFactory, visualStudio => visualStudio.Instance.Editor)
        {
            this.CreateSolution(solutionName);
            this.AddProject(projectTemplate, new ProjectUtils.Project(ProjectName), LanguageName);

            Editor = (Editor_OutOfProc)TextViewWindow;

            // Winforms and XAML do not open text files on creation
            // so these editor tasks will not work if that is the project template being used.
            if (projectTemplate != WellKnownProjectTemplates.WinFormsApplication &&
                projectTemplate != WellKnownProjectTemplates.WpfApplication)
            {
                VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(false);
                ClearEditor();
            }
        }

        protected abstract string LanguageName { get; }

        protected void ClearEditor()
            => SetUpEditor("$$");

        protected void SetUpEditor(string markupCode)
        {
            MarkupTestFile.GetPosition(markupCode, out string code, out int caretPosition);

            var originalValue = VisualStudioWorkspaceOutOfProc.IsPrettyListingOn(LanguageName);

            VisualStudioWorkspaceOutOfProc.SetPrettyListing(LanguageName, false);
            try
            {
                Editor.SetText(code);
                Editor.MoveCaret(caretPosition);
            }
            finally
            {
                VisualStudioWorkspaceOutOfProc.SetPrettyListing(LanguageName, originalValue);
            }
        }
    }
}