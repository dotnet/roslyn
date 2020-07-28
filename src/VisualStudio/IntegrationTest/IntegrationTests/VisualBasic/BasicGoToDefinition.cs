// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicGoToDefinition : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicGoToDefinition(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicGoToDefinition))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)]
        public void GoToClassDeclaration()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "FileDef.vb");
            VisualStudio.SolutionExplorer.OpenFile(project, "FileDef.vb");
            VisualStudio.Editor.SetText(
@"Class SomeClass
End Class");
            VisualStudio.SolutionExplorer.AddFile(project, "FileConsumer.vb");
            VisualStudio.SolutionExplorer.OpenFile(project, "FileConsumer.vb");
            VisualStudio.Editor.SetText(
@"Class SomeOtherClass
    Dim gibberish As SomeClass
End Class");
            VisualStudio.Editor.PlaceCaret("SomeClass");
            VisualStudio.Editor.GoToDefinition();
            VisualStudio.Editor.Verify.TextContains(@"Class SomeClass$$", assertCaretPosition: true);
            Assert.False(VisualStudio.Shell.IsActiveTabProvisional());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)]
        public void ObjectBrowserNavigation()
        {
            SetUpEditor(
@"Class C
    Dim i As Integer$$
End Class");
            VisualStudio.Workspace.SetFeatureOption(feature: "VisualStudioNavigationOptions", optionName: "NavigateToObjectBrowser", language: LanguageName, valueString: "True");

            VisualStudio.Editor.GoToDefinition();
            Assert.Equal("Object Browser", VisualStudio.Shell.GetActiveWindowCaption());

            VisualStudio.Workspace.SetFeatureOption(feature: "VisualStudioNavigationOptions", optionName: "NavigateToObjectBrowser", language: LanguageName, valueString: "False");

            VisualStudio.SolutionExplorer.OpenFile(new ProjectUtils.Project(ProjectName), "Class1.vb");
            VisualStudio.Editor.GoToDefinition();
            VisualStudio.Editor.Verify.TextContains("Public Structure Int32");
        }
    }
}
