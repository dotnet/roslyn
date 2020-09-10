// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
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
            VisualStudio.Editor.InvokeNavigateTo("FirstMethod", VirtualKey.Enter);
            VisualStudio.Editor.WaitForActiveView("test1.vb");
            Assert.Equal("FirstMethod", VisualStudio.Editor.GetSelectedText());

            // Verify C# files are found when navigating from VB
            VisualStudio.SolutionExplorer.AddProject(csProject, WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp);
            VisualStudio.SolutionExplorer.AddFile(csProject, "csfile.cs", open: true);

            VisualStudio.Editor.InvokeNavigateTo("FirstClass", VirtualKey.Enter);
            VisualStudio.Editor.WaitForActiveView("test1.vb");
            Assert.Equal("FirstClass", VisualStudio.Editor.GetSelectedText());
        }
    }
}
