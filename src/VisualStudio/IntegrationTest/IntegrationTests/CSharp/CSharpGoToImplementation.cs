// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpGoToImplementation : AbstractIdeEditorTest
    {
        public CSharpGoToImplementation()
            : base(nameof(CSharpGoToImplementation))
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        [IdeFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)]
        public async Task SimpleGoToImplementationAsync()
        {
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "FileImplementation.cs");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "FileImplementation.cs");
            await VisualStudio.Editor.SetTextAsync(
@"class Implementation : IGoo
{
}");
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "FileInterface.cs");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "FileInterface.cs");
            await VisualStudio.Editor.SetTextAsync(
@"interface IGoo 
{
}");
            await VisualStudio.Editor.PlaceCaretAsync("interface IGoo");
            await VisualStudio.Editor.GoToImplementationAsync();
            await VisualStudio.Editor.Verify.TextContainsAsync(@"class Implementation$$", assertCaretPosition: true);
            Assert.False(await VisualStudio.Shell.IsActiveTabProvisionalAsync());
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)]
        public async Task GoToImplementationOpensProvisionalTabIfDocumentNotOpenAsync()
        {
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "FileImplementation.cs");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "FileImplementation.cs");
            await VisualStudio.Editor.SetTextAsync(
@"class Implementation : IBar
{
}
");
            await VisualStudio.SolutionExplorer.CloseFileAsync(ProjectName, "FileImplementation.cs", saveFile: true);
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "FileInterface.cs");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "FileInterface.cs");
            await VisualStudio.Editor.SetTextAsync(
@"interface IBar
{
}");
            await VisualStudio.Editor.PlaceCaretAsync("interface IBar");
            await VisualStudio.Editor.GoToImplementationAsync();
            await VisualStudio.Editor.Verify.TextContainsAsync(@"class Implementation$$", assertCaretPosition: true);
            Assert.True(await VisualStudio.Shell.IsActiveTabProvisionalAsync());
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)]
        public async Task GoToImplementationFromMetadataAsSourceAsync()
        {
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "FileImplementation.cs");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "FileImplementation.cs");
            await VisualStudio.Editor.SetTextAsync(
@"using System;

class Implementation : IDisposable
{
    public void SomeMethod()
    {
        IDisposable d;
    }
}");
            await VisualStudio.Editor.PlaceCaretAsync("IDisposable d", charsOffset: -1);
            await VisualStudio.Editor.GoToDefinitionAsync();
            await VisualStudio.Editor.GoToImplementationAsync();
            await VisualStudio.Editor.Verify.TextContainsAsync(@"class Implementation$$ : IDisposable", assertCaretPosition: true);
        }
    }
}
