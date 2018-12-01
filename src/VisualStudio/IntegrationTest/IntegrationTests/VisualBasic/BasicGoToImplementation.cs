// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicGoToImplementation : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicGoToImplementation( )
                    : base( nameof(BasicGoToImplementation))
        {
        }

        [TestMethod, TestCategory(Traits.Features.GoToImplementation)]
        public void SimpleGoToImplementation()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddFile(project, "FileImplementation.vb");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "FileImplementation.vb");
            VisualStudioInstance.Editor.SetText(
@"Class Implementation
  Implements IGoo
End Class");
            VisualStudioInstance.SolutionExplorer.AddFile(project, "FileInterface.vb");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "FileInterface.vb");
            VisualStudioInstance.Editor.SetText(
@"Interface IGoo 
End Interface");
            VisualStudioInstance.Editor.PlaceCaret("Interface IGoo");
            VisualStudioInstance.Editor.GoToImplementation();
            VisualStudioInstance.Editor.Verify.TextContains(@"Class Implementation$$", assertCaretPosition: true);
            Assert.IsFalse(VisualStudioInstance.Shell.IsActiveTabProvisional());
        }
    }
}
