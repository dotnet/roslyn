// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpGoToImplementation : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpGoToImplementation(VisualStudioInstanceFactory instanceFactory)
                    : base(instanceFactory, nameof(CSharpGoToImplementation))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation), Trait(Traits.Editor, Traits.Editors.LanguageServerProtocol)]
        public void SimpleGoToImplementation()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "FileImplementation.cs");
            VisualStudio.SolutionExplorer.OpenFile(project, "FileImplementation.cs");
            VisualStudio.Editor.SetText(
@"class Implementation : IGoo
{
}");
            VisualStudio.SolutionExplorer.AddFile(project, "FileInterface.cs");
            VisualStudio.SolutionExplorer.OpenFile(project, "FileInterface.cs");
            VisualStudio.Editor.SetText(
@"interface IGoo 
{
}");
            VisualStudio.Editor.PlaceCaret("interface IGoo");
            VisualStudio.Editor.GoToImplementation("FileImplementation.cs");
            VisualStudio.Editor.Verify.TextContains(@"class Implementation$$", assertCaretPosition: true);
            Assert.False(VisualStudio.Shell.IsActiveTabProvisional());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation), Trait(Traits.Editor, Traits.Editors.LanguageServerProtocol)]
        public void GoToImplementationOpensProvisionalTabIfDocumentNotOpen()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "FileImplementation.cs");
            VisualStudio.SolutionExplorer.OpenFile(project, "FileImplementation.cs");
            VisualStudio.Editor.SetText(
@"class Implementation : IBar
{
}
");
            VisualStudio.SolutionExplorer.CloseCodeFile(project, "FileImplementation.cs", saveFile: true);
            VisualStudio.SolutionExplorer.AddFile(project, "FileInterface.cs");
            VisualStudio.SolutionExplorer.OpenFile(project, "FileInterface.cs");
            VisualStudio.Editor.SetText(
@"interface IBar
{
}");
            VisualStudio.Editor.PlaceCaret("interface IBar");
            VisualStudio.Editor.GoToImplementation("FileImplementation.cs");
            VisualStudio.Editor.Verify.TextContains(@"class Implementation$$", assertCaretPosition: true);
            Assert.True(VisualStudio.Shell.IsActiveTabProvisional());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)]
        public void GoToImplementationFromMetadataAsSource()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "FileImplementation.cs");
            VisualStudio.SolutionExplorer.OpenFile(project, "FileImplementation.cs");
            VisualStudio.Editor.SetText(
@"using System;

class Implementation : IDisposable
{
    public void SomeMethod()
    {
        IDisposable d;
    }
}");
            VisualStudio.Editor.PlaceCaret("IDisposable d", charsOffset: -1);
            VisualStudio.Editor.GoToDefinition("IDisposable [from metadata]");
            VisualStudio.Editor.GoToImplementation("FileImplementation.cs");
            VisualStudio.Editor.Verify.TextContains(@"class Implementation$$ : IDisposable", assertCaretPosition: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)]
        public void GoToImplementationFromSourceAndMetadata()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "FileImplementation.cs");
            VisualStudio.SolutionExplorer.OpenFile(project, "FileImplementation.cs");
            VisualStudio.Editor.SetText(
@"using System;

class Implementation : IDisposable
{
    public void Dispose()
    {
    }
}");
            VisualStudio.SolutionExplorer.CloseCodeFile(project, "FileImplementation.cs", saveFile: true);

            VisualStudio.SolutionExplorer.AddFile(project, "FileUsage.cs");
            VisualStudio.SolutionExplorer.OpenFile(project, "FileUsage.cs");
            VisualStudio.Editor.SetText(
@"using System;

class C
{
    void M()
    {
        IDisposable c;
        try
        {
            c = new Implementation();
        }
        finally
        {
            c.Dispose();
        }
    }
}");

            VisualStudio.Editor.PlaceCaret("Dispose", charsOffset: -1);

            VisualStudio.Editor.GoToImplementation(expectedNavigateWindowName: null);

            var results = VisualStudio.FindReferencesWindow.GetContents();

            // There are a lot of results, no point transcribing them all into a test
            Assert.Contains(results, r => r.Code == "public void Dispose()" && Path.GetFileName(r.FilePath) == "FileImplementation.cs");
            Assert.Contains(results, r => r.Code == "void Stream.Dispose()" && r.FilePath == "Stream");
        }
    }
}
