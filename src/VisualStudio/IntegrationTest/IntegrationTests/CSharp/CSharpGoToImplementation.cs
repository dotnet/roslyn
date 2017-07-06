﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
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

        [Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)]
        public void SimpleGoToImplementation()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "FileImplementation.cs");
            VisualStudio.SolutionExplorer.OpenFile(project, "FileImplementation.cs");
            VisualStudio.Editor.SetText(
@"class Implementation : IFoo
{
}");
            VisualStudio.SolutionExplorer.AddFile(project, "FileInterface.cs");
            VisualStudio.SolutionExplorer.OpenFile(project, "FileInterface.cs");
            VisualStudio.Editor.SetText(
@"interface IFoo 
{
}");
            VisualStudio.Editor.PlaceCaret("interface IFoo");
            VisualStudio.Editor.GoToImplementation();
            VisualStudio.Editor.Verify.TextContains(@"class Implementation$$", assertCaretPosition: true);
            Assert.False(VisualStudio.Shell.IsActiveTabProvisional());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)]
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
            VisualStudio.SolutionExplorer.CloseFile(project, "FileImplementation.cs", saveFile: true);
            VisualStudio.SolutionExplorer.AddFile(project, "FileInterface.cs");
            VisualStudio.SolutionExplorer.OpenFile(project, "FileInterface.cs");
            VisualStudio.Editor.SetText(
@"interface IBar
{
}");
            VisualStudio.Editor.PlaceCaret("interface IBar");
            VisualStudio.Editor.GoToImplementation();
            VisualStudio.Editor.Verify.TextContains(@"class Implementation$$", assertCaretPosition: true);
            Assert.True(VisualStudio.Shell.IsActiveTabProvisional());
        }


        // TODO: Enable this once the GoToDefinition tests are merged
        [Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)]
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
            VisualStudio.Editor.GoToDefinition();
            VisualStudio.Editor.GoToImplementation();
            VisualStudio.Editor.Verify.TextContains(@"class Implementation$$ : IDisposable", assertCaretPosition: true);
        }
    }
}
