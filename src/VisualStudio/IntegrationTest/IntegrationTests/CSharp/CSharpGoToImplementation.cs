// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

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
            AddFile("FileImplementation.cs");
            OpenFile(ProjectName, "FileImplementation.cs");
            Editor.SetText(
@"class Implementation : IFoo
{
}");
            AddFile("FileInterface.cs");
            OpenFile(ProjectName, "FileInterface.cs");
            Editor.SetText(
@"interface IFoo 
{
}");
            PlaceCaret("interface IFoo");
            Editor.GoToImplementation();
            VerifyTextContains(@"class Implementation$$", assertCaretPosition: true);
            Assert.False(VisualStudio.Instance.Shell.IsActiveTabProvisional());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)]
        public void GoToImplementationOpensProvisionalTabIfDocumentNotOpen()
        {
            AddFile("FileImplementation.cs");
            OpenFile(ProjectName, "FileImplementation.cs");
            Editor.SetText(
@"class Implementation : IBar
{
}");
            CloseFile(ProjectName, "FileImplementation.cs");
            AddFile("FileInterface.cs");
            OpenFile(ProjectName, "FileInterface.cs");
            Editor.SetText(
@"interface IBar
{
}");
            PlaceCaret("interface IBar");
            Editor.GoToImplementation();
            VerifyTextContains(@"class Implementation$$", assertCaretPosition: true);
            Assert.True(VisualStudio.Instance.Shell.IsActiveTabProvisional());
        }


        // TODO: Enable this once the GoToDefinition tests are merged
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/15740"),
         Trait(Traits.Feature, Traits.Features.GoToImplementation)]
        public void GoToImplementationFromMetadataAsSource()
        {
            AddFile("FileImplementation.cs");
            OpenFile(ProjectName, "FileImplementation.cs");
            Editor.SetText(
@"using System;

class Implementation : IDisposable
{
  public void SomeMethod()
  {
    IDisposable d;
  }
}");
            PlaceCaret("IDisposable d");

            // Uncomment this line once the GoToDefinition tests are merged
            // Editor.GoToDefinition();
            Editor.GoToImplementation();
            VerifyTextContains(@"class Implementation : IDisposable", assertCaretPosition: true);
        }
    }
}
