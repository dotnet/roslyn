// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.GoToImplementation)]
    public class CSharpGoToImplementation : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpGoToImplementation()
            : base(nameof(CSharpGoToImplementation))
        {
        }

        [IdeFact, Trait(Traits.Editor, Traits.Editors.LanguageServerProtocol)]
        public async Task SimpleGoToImplementation()
        {
            var project = ProjectName;
            await TestServices.SolutionExplorer.AddFileAsync(project, "FileImplementation.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "FileImplementation.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"class Implementation : IGoo
{
}", HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddFileAsync(project, "FileInterface.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "FileInterface.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"interface IGoo 
{
}", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("interface IGoo", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.Editor.GoToImplementationAsync(HangMitigatingCancellationToken);
            var dirtyModifier = await TestServices.Editor.GetDirtyIndicatorAsync(HangMitigatingCancellationToken);
            Assert.Equal($"FileImplementation.cs{dirtyModifier}", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            await TestServices.EditorVerifier.TextContainsAsync(@"class Implementation$$", assertCaretPosition: true, HangMitigatingCancellationToken);
            Assert.False(await TestServices.Shell.IsActiveTabProvisionalAsync(HangMitigatingCancellationToken));
        }

        [IdeFact, Trait(Traits.Editor, Traits.Editors.LanguageServerProtocol)]
        public async Task GoToImplementationOpensProvisionalTabIfDocumentNotOpen()
        {
            var project = ProjectName;
            await TestServices.SolutionExplorer.AddFileAsync(project, "FileImplementation.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "FileImplementation.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"class Implementation : IBar
{
}
", HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.CloseCodeFileAsync(project, "FileImplementation.cs", saveFile: true, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddFileAsync(project, "FileInterface.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "FileInterface.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"interface IBar
{
}", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("interface IBar", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.Editor.GoToImplementationAsync(HangMitigatingCancellationToken);
            Assert.Equal("FileImplementation.cs", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            await TestServices.EditorVerifier.TextContainsAsync(@"class Implementation$$", assertCaretPosition: true, HangMitigatingCancellationToken);
            Assert.True(await TestServices.Shell.IsActiveTabProvisionalAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task GoToImplementationFromMetadataAsSource()
        {
            var project = ProjectName;
            await TestServices.SolutionExplorer.AddFileAsync(project, "FileImplementation.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "FileImplementation.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"using System;

class Implementation : IDisposable
{
    public void SomeMethod()
    {
        IDisposable d;
    }
}", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("IDisposable d", charsOffset: -1, HangMitigatingCancellationToken);
            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Equal("IDisposable [from metadata]", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            await TestServices.Editor.GoToImplementationAsync(HangMitigatingCancellationToken);
            var dirtyModifier = await TestServices.Editor.GetDirtyIndicatorAsync(HangMitigatingCancellationToken);
            Assert.Equal($"FileImplementation.cs{dirtyModifier}", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            await TestServices.EditorVerifier.TextContainsAsync(@"class Implementation$$ : IDisposable", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task GoToImplementationFromSourceAndMetadata()
        {
            var project = ProjectName;
            await TestServices.SolutionExplorer.AddFileAsync(project, "FileImplementation.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "FileImplementation.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"using System;

class Implementation : IDisposable
{
    public void Dispose()
    {
    }
}", HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.CloseCodeFileAsync(project, "FileImplementation.cs", saveFile: true, HangMitigatingCancellationToken);

            await TestServices.SolutionExplorer.AddFileAsync(project, "FileUsage.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "FileUsage.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
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
}", HangMitigatingCancellationToken);

            await TestServices.Editor.PlaceCaretAsync("Dispose", charsOffset: -1, HangMitigatingCancellationToken);

            // This one won't automatically navigate to the implementation
            var dirtyModifier = await TestServices.Editor.GetDirtyIndicatorAsync(HangMitigatingCancellationToken);
            Assert.Equal($"FileUsage.cs{dirtyModifier}", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            await TestServices.Editor.GoToImplementationAsync(HangMitigatingCancellationToken);
            Assert.Equal("'Dispose' implementations - Entire solution", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));

            var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);

            // There are a lot of results, no point transcribing them all into a test
            Assert.Contains(results, r => r.GetText() == "public void Dispose()" && Path.GetFileName(r.GetDocumentName()) == "FileImplementation.cs");
            Assert.Contains(results, r => r.GetText() == "void Stream.Dispose()" && r.GetDocumentName() == "Stream");
        }
    }
}
