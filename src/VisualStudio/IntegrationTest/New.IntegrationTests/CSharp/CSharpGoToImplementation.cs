// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.GoToImplementation)]
public class CSharpGoToImplementation : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpGoToImplementation()
        : base(nameof(CSharpGoToImplementation))
    {
    }

    [IdeTheory]
    [CombinatorialData]
    public async Task SimpleGoToImplementation(bool asyncNavigation)
    {
        await TestServices.Editor.ConfigureAsyncNavigation(asyncNavigation ? AsyncNavigationKind.Asynchronous : AsyncNavigationKind.Synchronous, HangMitigatingCancellationToken);

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

        if (asyncNavigation)
        {
            // The navigation completed asynchronously, so navigate to the first item in the results list
            Assert.Equal($"'IGoo' implementations - Entire solution", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                $"<unknown>: class Implementation : IGoo",
                string.Join(Environment.NewLine, results.Select(result => $"{result.GetItemOrigin()?.ToString() ?? "<unknown>"}: {result.GetText()}")));
            results[0].NavigateTo(isPreview: false, shouldActivate: true);

            await TestServices.Workarounds.WaitForNavigationAsync(HangMitigatingCancellationToken);
        }

        Assert.Equal($"FileImplementation.cs", await TestServices.Shell.GetActiveDocumentFileNameAsync(HangMitigatingCancellationToken));
        await TestServices.EditorVerifier.TextContainsAsync("class $$Implementation", assertCaretPosition: true, HangMitigatingCancellationToken);
        Assert.False(await TestServices.Shell.IsActiveTabProvisionalAsync(HangMitigatingCancellationToken));
    }

    [IdeTheory]
    [CombinatorialData]
    public async Task GoToImplementationOpensProvisionalTabIfDocumentNotOpen(bool asyncNavigation)
    {
        await TestServices.Editor.ConfigureAsyncNavigation(asyncNavigation ? AsyncNavigationKind.Asynchronous : AsyncNavigationKind.Synchronous, HangMitigatingCancellationToken);

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

        if (asyncNavigation)
        {
            // The navigation completed asynchronously, so navigate to the first item in the results list
            Assert.Equal($"'IBar' implementations - Entire solution", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                $"<unknown>: class Implementation : IBar",
                string.Join(Environment.NewLine, results.Select(result => $"{result.GetItemOrigin()?.ToString() ?? "<unknown>"}: {result.GetText()}")));
            results[0].NavigateTo(isPreview: true, shouldActivate: true);

            await TestServices.Workarounds.WaitForNavigationAsync(HangMitigatingCancellationToken);
        }

        Assert.Equal("FileImplementation.cs", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
        await TestServices.EditorVerifier.TextContainsAsync("class $$Implementation", assertCaretPosition: true, HangMitigatingCancellationToken);
        Assert.True(await TestServices.Shell.IsActiveTabProvisionalAsync(HangMitigatingCancellationToken));
    }

    [IdeTheory]
    [CombinatorialData]
    public async Task GoToImplementationFromMetadataAsSource(bool asyncNavigation)
    {
        await TestServices.Editor.ConfigureAsyncNavigation(asyncNavigation ? AsyncNavigationKind.Asynchronous : AsyncNavigationKind.Synchronous, HangMitigatingCancellationToken);

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
        Assert.Equal("IDisposable [decompiled] [Read Only]", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
        await TestServices.Editor.GoToImplementationAsync(HangMitigatingCancellationToken);

        if (asyncNavigation)
        {
            // The navigation completed asynchronously, so navigate to the first item in the results list
            Assert.Equal($"'IDisposable' implementations - Entire solution", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);

            // This test includes results from metadata on this path, so filter those out
            results = results.WhereAsArray(result => result.GetItemOrigin() != ItemOrigin.ExactMetadata);

            AssertEx.EqualOrDiff(
                $"<unknown>: class Implementation : IDisposable",
                string.Join(Environment.NewLine, results.Select(result => $"{result.GetItemOrigin()?.ToString() ?? "<unknown>"}: {result.GetText()}")));
            results[0].NavigateTo(isPreview: false, shouldActivate: true);

            await TestServices.Workarounds.WaitForNavigationAsync(HangMitigatingCancellationToken);
        }

        Assert.Equal($"FileImplementation.cs", await TestServices.Shell.GetActiveDocumentFileNameAsync(HangMitigatingCancellationToken));
        await TestServices.EditorVerifier.TextContainsAsync("class $$Implementation : IDisposable", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory]
    [CombinatorialData]
    public async Task GoToImplementationFromSourceAndMetadata(bool asyncNavigation)
    {
        await TestServices.Editor.ConfigureAsyncNavigation(asyncNavigation ? AsyncNavigationKind.Asynchronous : AsyncNavigationKind.Synchronous, HangMitigatingCancellationToken);

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
        Assert.Equal($"FileUsage.cs", await TestServices.Shell.GetActiveDocumentFileNameAsync(HangMitigatingCancellationToken));
        await TestServices.Editor.GoToImplementationAsync(HangMitigatingCancellationToken);
        Assert.Equal("'Dispose' implementations - Entire solution", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));

        var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);

        // There are a lot of results, no point transcribing them all into a test

        // Doc:
        // StandardTableKeyNames.DocumentName is the path used to navigate to the entry.
        // StandardTableKeyNames.DisplayPath is only used for what is displayed to the end user.
        // If this is not set, then StandardTableKeyNames.DocumentName is displayed to the end user.
        //
        // Metadata definitions do not have DocumentName. THey implement custom navigation.

        AssertEx.Contains(results, r => r.GetText() == "public void Dispose()" && Path.GetFileName(r.GetDocumentName()) == "FileImplementation.cs", Inspect);
        AssertEx.Contains(results, r => r.GetText() == "void Stream.Dispose()" && Path.GetFileName(r.GetDisplayPath()) == "mscorlib.dll", Inspect);

        static string Inspect(ITableEntryHandle2 entry)
            => $"Text: '{entry.GetText()}' DocumentName: '{entry.GetDocumentName()}' DisplayPath: '{entry.GetDisplayPath()}'";
    }
}
