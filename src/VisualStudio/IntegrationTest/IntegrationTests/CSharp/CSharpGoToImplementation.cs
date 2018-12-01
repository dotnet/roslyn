// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpGoToImplementation : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpGoToImplementation( )
                    : base( nameof(CSharpGoToImplementation))
        {
        }

        [TestMethod, TestCategory(Traits.Features.GoToImplementation)]
        public void SimpleGoToImplementation()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddFile(project, "FileImplementation.cs");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "FileImplementation.cs");
            VisualStudioInstance.Editor.SetText(
@"class Implementation : IGoo
{
}");
            VisualStudioInstance.SolutionExplorer.AddFile(project, "FileInterface.cs");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "FileInterface.cs");
            VisualStudioInstance.Editor.SetText(
@"interface IGoo 
{
}");
            VisualStudioInstance.Editor.PlaceCaret("interface IGoo");
            VisualStudioInstance.Editor.GoToImplementation();
            VisualStudioInstance.Editor.Verify.TextContains(@"class Implementation$$", assertCaretPosition: true);
            Assert.IsFalse(VisualStudioInstance.Shell.IsActiveTabProvisional());
        }

        [TestMethod, TestCategory(Traits.Features.GoToImplementation)]
        public void GoToImplementationOpensProvisionalTabIfDocumentNotOpen()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddFile(project, "FileImplementation.cs");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "FileImplementation.cs");
            VisualStudioInstance.Editor.SetText(
@"class Implementation : IBar
{
}
");
            VisualStudioInstance.SolutionExplorer.CloseFile(project, "FileImplementation.cs", saveFile: true);
            VisualStudioInstance.SolutionExplorer.AddFile(project, "FileInterface.cs");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "FileInterface.cs");
            VisualStudioInstance.Editor.SetText(
@"interface IBar
{
}");
            VisualStudioInstance.Editor.PlaceCaret("interface IBar");
            VisualStudioInstance.Editor.GoToImplementation();
            VisualStudioInstance.Editor.Verify.TextContains(@"class Implementation$$", assertCaretPosition: true);
            Assert.IsTrue(VisualStudioInstance.Shell.IsActiveTabProvisional());
        }


        // TODO: Enable this once the GoToDefinition tests are merged
        [TestMethod, TestCategory(Traits.Features.GoToImplementation)]
        public void GoToImplementationFromMetadataAsSource()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddFile(project, "FileImplementation.cs");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "FileImplementation.cs");
            VisualStudioInstance.Editor.SetText(
@"using System;

class Implementation : IDisposable
{
    public void SomeMethod()
    {
        IDisposable d;
    }
}");
            VisualStudioInstance.Editor.PlaceCaret("IDisposable d", charsOffset: -1);
            VisualStudioInstance.Editor.GoToDefinition();
            VisualStudioInstance.Editor.GoToImplementation();
            VisualStudioInstance.Editor.Verify.TextContains(@"class Implementation$$ : IDisposable", assertCaretPosition: true);
        }
    }
}
