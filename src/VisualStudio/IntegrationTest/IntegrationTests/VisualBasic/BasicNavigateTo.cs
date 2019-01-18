// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicNavigateTo : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicNavigateTo(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicNavigateTo))
        {
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/19530"), Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public void NavigateTo()
        {
            var project = new ProjectUtils.Project(ProjectName);
            var csProject = new ProjectUtils.Project("CSProject");
            VisualStudio.SolutionExplorer.AddFile(project, "test1.vb", open: false, contents: @"
Class FirstClass
    Sub FirstMethod()
    End Sub
End Class");


            VisualStudio.SolutionExplorer.AddFile(project, "test2.vb", open: true, contents: @"
");
            VisualStudio.Editor.InvokeNavigateTo("FirstMethod");
            VisualStudio.Editor.NavigateToSendKeys("{ENTER}");
            VisualStudio.Editor.WaitForActiveView("test1.vb");
            Assert.Equal("FirstMethod", VisualStudio.Editor.GetSelectedText());

            // Verify C# files are found when navigating from VB
            VisualStudio.SolutionExplorer.AddProject(csProject, WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp);
            VisualStudio.SolutionExplorer.AddFile(csProject, "csfile.cs", open: true);

            VisualStudio.Editor.InvokeNavigateTo("FirstClass");
            VisualStudio.Editor.NavigateToSendKeys("{ENTER}");
            VisualStudio.Editor.WaitForActiveView("test1.vb");
            Assert.Equal("FirstClass", VisualStudio.Editor.GetSelectedText());
        }
    }
}
