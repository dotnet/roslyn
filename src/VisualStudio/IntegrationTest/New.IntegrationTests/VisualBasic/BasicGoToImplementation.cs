// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.GoToImplementation)]
public class BasicGoToImplementation : AbstractEditorTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicGoToImplementation(ITestOutputHelper testOutputHelper)
                : base(nameof(BasicGoToImplementation))
    {
        _testOutputHelper = testOutputHelper;
    }

    private void Log(string message)
        => _testOutputHelper.WriteLine(message);

    [IdeTheory(Skip = "https://github.com/dotnet/roslyn/issues/85737")]
    [CombinatorialData]
    public async Task SimpleGoToImplementation(bool asyncNavigation)
    {
        Log("Configuring async navigation mode");
        await TestServices.Editor.ConfigureAsyncNavigation(asyncNavigation ? AsyncNavigationKind.Asynchronous : AsyncNavigationKind.Synchronous, HangMitigatingCancellationToken);

        var project = ProjectName;
        Log("Adding FileImplementation.vb to the project");
        await TestServices.SolutionExplorer.AddFileAsync(project, "FileImplementation.vb", cancellationToken: HangMitigatingCancellationToken);
        Log("Opening FileImplementation.vb");
        await TestServices.SolutionExplorer.OpenFileAsync(project, "FileImplementation.vb", HangMitigatingCancellationToken);
        Log("Setting the text of FileImplementation.vb");
        await TestServices.Editor.SetTextAsync(
            """
            Class Implementation
              Implements IGoo
            End Class
            """, HangMitigatingCancellationToken);
        Log("Adding FileInterface.vb to the project");
        await TestServices.SolutionExplorer.AddFileAsync(project, "FileInterface.vb", cancellationToken: HangMitigatingCancellationToken);
        Log("Opening FileInterface.vb");
        await TestServices.SolutionExplorer.OpenFileAsync(project, "FileInterface.vb", HangMitigatingCancellationToken);
        Log("Setting the text of FileInterface.vb");
        await TestServices.Editor.SetTextAsync(
            """
            Interface IGoo 
            End Interface
            """, HangMitigatingCancellationToken);
        Log("Placing the caret on 'Interface IGoo'");
        await TestServices.Editor.PlaceCaretAsync("Interface IGoo", charsOffset: 0, HangMitigatingCancellationToken);
        Log("Invoking Go To Implementation");
        await TestServices.Editor.GoToImplementationAsync(HangMitigatingCancellationToken);

        if (asyncNavigation)
        {
            // The navigation completed asynchronously, so navigate to the first item in the results list
            Log("Verifying the Find References/Implementations window caption");
            Assert.Equal($"'IGoo' implementations - Entire solution", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            Log("Retrieving the contents of the Find References/Implementations window");
            var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                $"<unknown>: Class Implementation",
                string.Join(Environment.NewLine, results.Select(result => $"{result.GetItemOrigin()?.ToString() ?? "<unknown>"}: {result.GetText()}")));
            Log("Navigating to the first result");
            results[0].NavigateTo(isPreview: false, shouldActivate: true);

            Log("Waiting for navigation to complete");
            await TestServices.Workarounds.WaitForNavigationAsync(HangMitigatingCancellationToken);
        }

        Log("Verifying the active document file name");
        Assert.Equal($"FileImplementation.vb", await TestServices.Shell.GetActiveDocumentFileNameAsync(HangMitigatingCancellationToken));
        Log("Verifying the editor text contains the expected caret marker");
        await TestServices.EditorVerifier.TextContainsAsync("Class $$Implementation", assertCaretPosition: true);
        Log("Verifying the active tab is not provisional");
        Assert.False(await TestServices.Shell.IsActiveTabProvisionalAsync(HangMitigatingCancellationToken));
    }
}
