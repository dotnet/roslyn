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
    public class BasicGoToDefinition : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicGoToDefinition(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicGoToDefinition))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)]
        public void GoToClassDeclaration()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.AddFile("FileDef.vb", project);
            this.OpenFile("FileDef.vb", project);
            Editor.SetText(
@"Class SomeClass
End Class");
            this.AddFile("FileConsumer.vb", project);
            this.OpenFile("FileConsumer.vb", project);
            Editor.SetText(
@"Class SomeOtherClass
    Dim gibberish As SomeClass
End Class");
            this.PlaceCaret("SomeClass");
            Editor.GoToDefinition();
            this.VerifyTextContains(@"Class SomeClass$$", assertCaretPosition: true);
            Assert.False(VisualStudio.Instance.Shell.IsActiveTabProvisional());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)]
        public void ObjectBrowserNavigation()
        {
            SetUpEditor(
@"Class C
    Dim i As Integer$$
End Class");
            VisualStudioWorkspaceOutOfProc.SetFeatureOption(feature: "VisualStudioNavigationOptions", optionName: "NavigateToObjectBrowser", language: LanguageName, valueString: "True");
            
            Editor.GoToDefinition();
            Assert.Equal("Object Browser", VisualStudio.Instance.Shell.GetActiveWindowCaption());

            VisualStudioWorkspaceOutOfProc.SetFeatureOption(feature: "VisualStudioNavigationOptions", optionName: "NavigateToObjectBrowser", language: LanguageName, valueString: "False");

            this.OpenFile("Class1.vb", new ProjectUtils.Project(ProjectName));
            Editor.GoToDefinition();
            this.VerifyTextContains("Public Structure Int32");
        }
    }
}