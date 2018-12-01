// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicGoToDefinition : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicGoToDefinition( )
            : base( nameof(BasicGoToDefinition))
        {
        }

        [TestMethod, TestCategory(Traits.Features.GoToDefinition)]
        public void GoToClassDeclaration()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddFile(project, "FileDef.vb");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "FileDef.vb");
            VisualStudioInstance.Editor.SetText(
@"Class SomeClass
End Class");
            VisualStudioInstance.SolutionExplorer.AddFile(project, "FileConsumer.vb");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "FileConsumer.vb");
            VisualStudioInstance.Editor.SetText(
@"Class SomeOtherClass
    Dim gibberish As SomeClass
End Class");
            VisualStudioInstance.Editor.PlaceCaret("SomeClass");
            VisualStudioInstance.Editor.GoToDefinition();
            VisualStudioInstance.Editor.Verify.TextContains(@"Class SomeClass$$", assertCaretPosition: true);
            Assert.IsFalse(VisualStudioInstance.Shell.IsActiveTabProvisional());
        }

        [TestMethod, TestCategory(Traits.Features.GoToDefinition)]
        public void ObjectBrowserNavigation()
        {
            SetUpEditor(
@"Class C
    Dim i As Integer$$
End Class");
            VisualStudioInstance.Workspace.SetFeatureOption(feature: "VisualStudioNavigationOptions", optionName: "NavigateToObjectBrowser", language: LanguageName, valueString: "True");

            VisualStudioInstance.Editor.GoToDefinition();
            Assert.AreEqual("Object Browser", VisualStudioInstance.Shell.GetActiveWindowCaption());

            VisualStudioInstance.Workspace.SetFeatureOption(feature: "VisualStudioNavigationOptions", optionName: "NavigateToObjectBrowser", language: LanguageName, valueString: "False");

            VisualStudioInstance.SolutionExplorer.OpenFile(new ProjectUtils.Project(ProjectName), "Class1.vb");
            VisualStudioInstance.Editor.GoToDefinition();
            VisualStudioInstance.Editor.Verify.TextContains("Public Structure Int32");
        }
    }
}
