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
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.GoToImplementation)]
public class CSharpGoToImplementation : AbstractEditorTest
{
    private readonly ITestOutputHelper _output;

    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpGoToImplementation(ITestOutputHelper output)
        : base(nameof(CSharpGoToImplementation))
    {
        _output = output;
    }

    [IdeTheory]
    [CombinatorialData]
    public async Task SimpleGoToImplementation(bool asyncNavigation)
    {
        _output.WriteLine($"CSharpGoToImplementation.SimpleGoToImplementation(asyncNavigation: {asyncNavigation}): action 1");
        await TestServices.Editor.ConfigureAsyncNavigation(asyncNavigation ? AsyncNavigationKind.Asynchronous : AsyncNavigationKind.Synchronous, HangMitigatingCancellationToken);

        _output.WriteLine($"CSharpGoToImplementation.SimpleGoToImplementation(asyncNavigation: {asyncNavigation}): action 2");
        var project = ProjectName;
        _output.WriteLine($"CSharpGoToImplementation.SimpleGoToImplementation(asyncNavigation: {asyncNavigation}): action 3");
        await TestServices.SolutionExplorer.AddFileAsync(project, "FileImplementation.cs", cancellationToken: HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.SimpleGoToImplementation(asyncNavigation: {asyncNavigation}): action 4");
        await TestServices.SolutionExplorer.OpenFileAsync(project, "FileImplementation.cs", HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.SimpleGoToImplementation(asyncNavigation: {asyncNavigation}): action 5");
        await TestServices.Editor.SetTextAsync(
            """
            class Implementation : IGoo
            {
            }
            """, HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.SimpleGoToImplementation(asyncNavigation: {asyncNavigation}): action 6");
        await TestServices.SolutionExplorer.AddFileAsync(project, "FileInterface.cs", cancellationToken: HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.SimpleGoToImplementation(asyncNavigation: {asyncNavigation}): action 7");
        await TestServices.SolutionExplorer.OpenFileAsync(project, "FileInterface.cs", HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.SimpleGoToImplementation(asyncNavigation: {asyncNavigation}): action 8");
        await TestServices.Editor.SetTextAsync(
            """
            interface IGoo 
            {
            }
            """, HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.SimpleGoToImplementation(asyncNavigation: {asyncNavigation}): action 9");
        await TestServices.Editor.PlaceCaretAsync("interface IGoo", charsOffset: 0, HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.SimpleGoToImplementation(asyncNavigation: {asyncNavigation}): action 10");
        await TestServices.Editor.GoToImplementationAsync(HangMitigatingCancellationToken);

        _output.WriteLine($"CSharpGoToImplementation.SimpleGoToImplementation(asyncNavigation: {asyncNavigation}): action 11");
        if (asyncNavigation)
        {
            // The navigation completed asynchronously, so navigate to the first item in the results list
            _output.WriteLine($"CSharpGoToImplementation.SimpleGoToImplementation(asyncNavigation: {asyncNavigation}): action 12");
            Assert.Equal($"'IGoo' implementations - Entire solution", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            _output.WriteLine($"CSharpGoToImplementation.SimpleGoToImplementation(asyncNavigation: {asyncNavigation}): action 13");
            var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);
            _output.WriteLine($"CSharpGoToImplementation.SimpleGoToImplementation(asyncNavigation: {asyncNavigation}): action 14");
            AssertEx.EqualOrDiff(
                $"<unknown>: class Implementation : IGoo",
                string.Join(Environment.NewLine, results.Select(result => $"{result.GetItemOrigin()?.ToString() ?? "<unknown>"}: {result.GetText()}")));
            _output.WriteLine($"CSharpGoToImplementation.SimpleGoToImplementation(asyncNavigation: {asyncNavigation}): action 15");
            results[0].NavigateTo(isPreview: false, shouldActivate: true);

            _output.WriteLine($"CSharpGoToImplementation.SimpleGoToImplementation(asyncNavigation: {asyncNavigation}): action 16");
            await TestServices.Workarounds.WaitForNavigationAsync(HangMitigatingCancellationToken);
        }

        _output.WriteLine($"CSharpGoToImplementation.SimpleGoToImplementation(asyncNavigation: {asyncNavigation}): action 17");
        Assert.Equal($"FileImplementation.cs", await TestServices.Shell.GetActiveDocumentFileNameAsync(HangMitigatingCancellationToken));
        _output.WriteLine($"CSharpGoToImplementation.SimpleGoToImplementation(asyncNavigation: {asyncNavigation}): action 18");
        await TestServices.EditorVerifier.TextContainsAsync("class $$Implementation", assertCaretPosition: true, HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.SimpleGoToImplementation(asyncNavigation: {asyncNavigation}): action 19");
        Assert.False(await TestServices.Shell.IsActiveTabProvisionalAsync(HangMitigatingCancellationToken));
    }

    [IdeTheory]
    [CombinatorialData]
    public async Task GoToImplementationOpensProvisionalTabIfDocumentNotOpen(bool asyncNavigation)
    {
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 1");
        await TestServices.Editor.ConfigureAsyncNavigation(asyncNavigation ? AsyncNavigationKind.Asynchronous : AsyncNavigationKind.Synchronous, HangMitigatingCancellationToken);

        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 2");
        var project = ProjectName;
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 3");
        await TestServices.SolutionExplorer.AddFileAsync(project, "FileImplementation.cs", cancellationToken: HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 4");
        await TestServices.SolutionExplorer.OpenFileAsync(project, "FileImplementation.cs", HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 5");
        await TestServices.Editor.SetTextAsync(
            """
            class Implementation : IBar
            {
            }

            """, HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 6");
        await TestServices.SolutionExplorer.CloseCodeFileAsync(project, "FileImplementation.cs", saveFile: true, HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 7");
        await TestServices.SolutionExplorer.AddFileAsync(project, "FileInterface.cs", cancellationToken: HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 8");
        await TestServices.SolutionExplorer.OpenFileAsync(project, "FileInterface.cs", HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 9");
        await TestServices.Editor.SetTextAsync(
            """
            interface IBar
            {
            }
            """, HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 10");
        await TestServices.Editor.PlaceCaretAsync("interface IBar", charsOffset: 0, HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 11");
        await TestServices.Editor.GoToImplementationAsync(HangMitigatingCancellationToken);

        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 12");
        if (asyncNavigation)
        {
            // The navigation completed asynchronously, so navigate to the first item in the results list
            _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 13");
            Assert.Equal($"'IBar' implementations - Entire solution", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 14");
            var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);
            _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 15");
            AssertEx.EqualOrDiff(
                $"<unknown>: class Implementation : IBar",
                string.Join(Environment.NewLine, results.Select(result => $"{result.GetItemOrigin()?.ToString() ?? "<unknown>"}: {result.GetText()}")));
            _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 16");
            results[0].NavigateTo(isPreview: true, shouldActivate: true);

            _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 17");
            await TestServices.Workarounds.WaitForNavigationAsync(HangMitigatingCancellationToken);
        }

        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 18");
        Assert.Equal("FileImplementation.cs", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 19");
        await TestServices.EditorVerifier.TextContainsAsync("class $$Implementation", assertCaretPosition: true, HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationOpensProvisionalTabIfDocumentNotOpen(asyncNavigation: {asyncNavigation}): action 20");
        Assert.True(await TestServices.Shell.IsActiveTabProvisionalAsync(HangMitigatingCancellationToken));
    }

    [IdeTheory]
    [CombinatorialData]
    public async Task GoToImplementationFromMetadataAsSource(bool asyncNavigation)
    {
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromMetadataAsSource(asyncNavigation: {asyncNavigation}): action 1");
        await TestServices.Editor.ConfigureAsyncNavigation(asyncNavigation ? AsyncNavigationKind.Asynchronous : AsyncNavigationKind.Synchronous, HangMitigatingCancellationToken);

        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromMetadataAsSource(asyncNavigation: {asyncNavigation}): action 2");
        var project = ProjectName;
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromMetadataAsSource(asyncNavigation: {asyncNavigation}): action 3");
        await TestServices.SolutionExplorer.AddFileAsync(project, "FileImplementation.cs", cancellationToken: HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromMetadataAsSource(asyncNavigation: {asyncNavigation}): action 4");
        await TestServices.SolutionExplorer.OpenFileAsync(project, "FileImplementation.cs", HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromMetadataAsSource(asyncNavigation: {asyncNavigation}): action 5");
        await TestServices.Editor.SetTextAsync(
            """
            using System;

            class Implementation : IDisposable
            {
                public void SomeMethod()
                {
                    IDisposable d;
                }
            }
            """, HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromMetadataAsSource(asyncNavigation: {asyncNavigation}): action 6");
        await TestServices.Editor.PlaceCaretAsync("IDisposable d", charsOffset: -1, HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromMetadataAsSource(asyncNavigation: {asyncNavigation}): action 7");
        await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromMetadataAsSource(asyncNavigation: {asyncNavigation}): action 8");
        Assert.Equal("IDisposable [decompiled] [Read Only]", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromMetadataAsSource(asyncNavigation: {asyncNavigation}): action 9");
        await TestServices.Editor.GoToImplementationAsync(HangMitigatingCancellationToken);

        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromMetadataAsSource(asyncNavigation: {asyncNavigation}): action 10");
        if (asyncNavigation)
        {
            // The navigation completed asynchronously, so navigate to the first item in the results list
            _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromMetadataAsSource(asyncNavigation: {asyncNavigation}): action 11");
            Assert.Equal($"'IDisposable' implementations - Entire solution", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromMetadataAsSource(asyncNavigation: {asyncNavigation}): action 12");
            var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);

            // This test includes results from metadata on this path, so filter those out
            _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromMetadataAsSource(asyncNavigation: {asyncNavigation}): action 13");
            results = results.WhereAsArray(result => result.GetItemOrigin() != ItemOrigin.ExactMetadata);

            _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromMetadataAsSource(asyncNavigation: {asyncNavigation}): action 14");
            AssertEx.EqualOrDiff(
                $"<unknown>: class Implementation : IDisposable",
                string.Join(Environment.NewLine, results.Select(result => $"{result.GetItemOrigin()?.ToString() ?? "<unknown>"}: {result.GetText()}")));
            _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromMetadataAsSource(asyncNavigation: {asyncNavigation}): action 15");
            results[0].NavigateTo(isPreview: false, shouldActivate: true);

            _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromMetadataAsSource(asyncNavigation: {asyncNavigation}): action 16");
            await TestServices.Workarounds.WaitForNavigationAsync(HangMitigatingCancellationToken);
        }

        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromMetadataAsSource(asyncNavigation: {asyncNavigation}): action 17");
        Assert.Equal($"FileImplementation.cs", await TestServices.Shell.GetActiveDocumentFileNameAsync(HangMitigatingCancellationToken));
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromMetadataAsSource(asyncNavigation: {asyncNavigation}): action 18");
        await TestServices.EditorVerifier.TextContainsAsync("class $$Implementation : IDisposable", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory]
    [CombinatorialData]
    public async Task GoToImplementationFromSourceAndMetadata(bool asyncNavigation)
    {
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromSourceAndMetadata(asyncNavigation: {asyncNavigation}): action 1");
        await TestServices.Editor.ConfigureAsyncNavigation(asyncNavigation ? AsyncNavigationKind.Asynchronous : AsyncNavigationKind.Synchronous, HangMitigatingCancellationToken);

        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromSourceAndMetadata(asyncNavigation: {asyncNavigation}): action 2");
        var project = ProjectName;
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromSourceAndMetadata(asyncNavigation: {asyncNavigation}): action 3");
        await TestServices.SolutionExplorer.AddFileAsync(project, "FileImplementation.cs", cancellationToken: HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromSourceAndMetadata(asyncNavigation: {asyncNavigation}): action 4");
        await TestServices.SolutionExplorer.OpenFileAsync(project, "FileImplementation.cs", HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromSourceAndMetadata(asyncNavigation: {asyncNavigation}): action 5");
        await TestServices.Editor.SetTextAsync(
            """
            using System;

            class Implementation : IDisposable
            {
                public void Dispose()
                {
                }
            }
            """, HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromSourceAndMetadata(asyncNavigation: {asyncNavigation}): action 6");
        await TestServices.SolutionExplorer.CloseCodeFileAsync(project, "FileImplementation.cs", saveFile: true, HangMitigatingCancellationToken);

        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromSourceAndMetadata(asyncNavigation: {asyncNavigation}): action 7");
        await TestServices.SolutionExplorer.AddFileAsync(project, "FileUsage.cs", cancellationToken: HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromSourceAndMetadata(asyncNavigation: {asyncNavigation}): action 8");
        await TestServices.SolutionExplorer.OpenFileAsync(project, "FileUsage.cs", HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromSourceAndMetadata(asyncNavigation: {asyncNavigation}): action 9");
        await TestServices.Editor.SetTextAsync(
            """
            using System;

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
            }
            """, HangMitigatingCancellationToken);

        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromSourceAndMetadata(asyncNavigation: {asyncNavigation}): action 10");
        await TestServices.Editor.PlaceCaretAsync("Dispose", charsOffset: -1, HangMitigatingCancellationToken);

        // This one won't automatically navigate to the implementation
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromSourceAndMetadata(asyncNavigation: {asyncNavigation}): action 11");
        Assert.Equal($"FileUsage.cs", await TestServices.Shell.GetActiveDocumentFileNameAsync(HangMitigatingCancellationToken));
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromSourceAndMetadata(asyncNavigation: {asyncNavigation}): action 12");
        await TestServices.Editor.GoToImplementationAsync(HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromSourceAndMetadata(asyncNavigation: {asyncNavigation}): action 13");
        Assert.Equal("'Dispose' implementations - Entire solution", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));

        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromSourceAndMetadata(asyncNavigation: {asyncNavigation}): action 14");
        var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);

        // There are a lot of results, no point transcribing them all into a test

        // Doc:
        // StandardTableKeyNames.DocumentName is the path used to navigate to the entry.
        // StandardTableKeyNames.DisplayPath is only used for what is displayed to the end user.
        // If this is not set, then StandardTableKeyNames.DocumentName is displayed to the end user.
        //
        // Metadata definitions do not have DocumentName. THey implement custom navigation.

        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromSourceAndMetadata(asyncNavigation: {asyncNavigation}): action 15");
        AssertEx.Contains(results, r => r.GetText() == "public void Dispose()" && Path.GetFileName(r.GetDocumentName()) == "FileImplementation.cs", Inspect);
        _output.WriteLine($"CSharpGoToImplementation.GoToImplementationFromSourceAndMetadata(asyncNavigation: {asyncNavigation}): action 16");
        AssertEx.Contains(results, r => r.GetText() == "void Stream.Dispose()" && Path.GetFileName(r.GetDisplayPath()) == "mscorlib.dll", Inspect);

        static string Inspect(ITableEntryHandle2 entry)
            => $"Text: '{entry.GetText()}' DocumentName: '{entry.GetDocumentName()}' DisplayPath: '{entry.GetDisplayPath()}'";
    }
}
