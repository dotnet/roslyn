// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Roslyn.VisualStudio.IntegrationTests.Extensions.SolutionExplorer;
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

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void NavigateTo()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.AddFile("test1.vb", project: project, open: false, contents: @"
Class FirstClass
    Sub FirstMethod()
    End Sub
End Class");


            this.AddFile("test2.vb", project: project, open: true, contents: @"
");

            this.InvokeNavigateToAndPressEnter("FirstMethod");
            Editor.WaitForActiveView("test1.vb");
            Assert.Equal("FirstMethod", Editor.GetSelectedText());

            // Verify C# files are found when navigating from VB
            VisualStudio.Instance.SolutionExplorer.AddProject("CSProject", WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp);
            VisualStudio.Instance.SolutionExplorer.AddFile("CSProject", "csfile.cs", open: true);

            this.InvokeNavigateToAndPressEnter("FirstClass");
            Editor.WaitForActiveView("test1.vb");
            Assert.Equal("FirstClass", Editor.GetSelectedText());
        }
    }
}