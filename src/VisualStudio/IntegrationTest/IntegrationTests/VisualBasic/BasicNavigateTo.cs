// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicNavigateTo : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicNavigateTo()
            : base(nameof(BasicNavigateTo))
        {
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/19530"), TestProperty(Traits.Feature, Traits.Features.NavigateTo)]
        public void NavigateTo()
        {
            var project = new ProjectUtils.Project(ProjectName);
            var csProject = new ProjectUtils.Project("CSProject");
            VisualStudioInstance.SolutionExplorer.AddFile(project, "test1.vb", open: false, contents: @"
Class FirstClass
    Sub FirstMethod()
    End Sub
End Class");


            VisualStudioInstance.SolutionExplorer.AddFile(project, "test2.vb", open: true, contents: @"
");
            VisualStudioInstance.Editor.InvokeNavigateTo("FirstMethod");
            VisualStudioInstance.Editor.NavigateToSendKeys("{ENTER}");
            VisualStudioInstance.Editor.WaitForActiveView("test1.vb");
            Assert.AreEqual("FirstMethod", VisualStudioInstance.Editor.GetSelectedText());

            // Verify C# files are found when navigating from VB
            VisualStudioInstance.SolutionExplorer.AddProject(csProject, WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp);
            VisualStudioInstance.SolutionExplorer.AddFile(csProject, "csfile.cs", open: true);

            VisualStudioInstance.Editor.InvokeNavigateTo("FirstClass");
            VisualStudioInstance.Editor.NavigateToSendKeys("{ENTER}");
            VisualStudioInstance.Editor.WaitForActiveView("test1.vb");
            Assert.AreEqual("FirstClass", VisualStudioInstance.Editor.GetSelectedText());
        }
    }
}
