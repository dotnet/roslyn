// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Roslyn.VisualStudio.IntegrationTests.Extensions.SolutionExplorer;
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
            this.AddFile("FileImplementation.cs", project);
            this.OpenFile("FileImplementation.cs", project);
            Editor.SetText(
@"class Implementation : IFoo
{
}");
            this.AddFile("FileInterface.cs", project);
            this.OpenFile("FileInterface.cs", project);
            Editor.SetText(
@"interface IFoo 
{
}");
            this.PlaceCaret("interface IFoo");
            Editor.GoToImplementation();
            this.VerifyTextContains(@"class Implementation$$", assertCaretPosition: true);
            Assert.False(VisualStudio.Instance.Shell.IsActiveTabProvisional());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)]
        public void GoToImplementationOpensProvisionalTabIfDocumentNotOpen()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.AddFile("FileImplementation.cs", project);
            this.OpenFile("FileImplementation.cs", project);
            Editor.SetText(
@"class Implementation : IBar
{
}");
            this.CloseFile("FileImplementation.cs", project);
            this.AddFile("FileInterface.cs", project);
            this.OpenFile("FileInterface.cs", project);
            Editor.SetText(
@"interface IBar
{
}");
            this.PlaceCaret("interface IBar");
            Editor.GoToImplementation();
            this.VerifyTextContains(@"class Implementation$$", assertCaretPosition: true);
            Assert.True(VisualStudio.Instance.Shell.IsActiveTabProvisional());
        }


        // TODO: Enable this once the GoToDefinition tests are merged
        [Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)]
        public void GoToImplementationFromMetadataAsSource()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.AddFile("FileImplementation.cs", project);
            this.OpenFile("FileImplementation.cs", project);
            Editor.SetText(
@"using System;

class Implementation : IDisposable
{
    public void SomeMethod()
    {
        IDisposable d;
    }
}");
            this.PlaceCaret("IDisposable d", charsOffset: -1);
            Editor.GoToDefinition();
            Editor.GoToImplementation();
            this.VerifyTextContains(@"class Implementation$$ : IDisposable", assertCaretPosition: true);
        }
    }
}
