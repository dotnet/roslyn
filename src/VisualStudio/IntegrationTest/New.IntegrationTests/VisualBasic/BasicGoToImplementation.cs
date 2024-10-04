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

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.GoToImplementation)]
public class BasicGoToImplementation : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicGoToImplementation()
                : base(nameof(BasicGoToImplementation))
    {
    }

    [IdeTheory]
    [CombinatorialData]
    public async Task SimpleGoToImplementation(bool asyncNavigation)
    {
        await TestServices.Editor.ConfigureAsyncNavigation(asyncNavigation ? AsyncNavigationKind.Asynchronous : AsyncNavigationKind.Synchronous, HangMitigatingCancellationToken);

        var project = ProjectName;
        await TestServices.SolutionExplorer.AddFileAsync(project, "FileImplementation.vb", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "FileImplementation.vb", HangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync(
@"Class Implementation
  Implements IGoo
End Class", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddFileAsync(project, "FileInterface.vb", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "FileInterface.vb", HangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync(
@"Interface IGoo 
End Interface", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("Interface IGoo", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.Editor.GoToImplementationAsync(HangMitigatingCancellationToken);

        if (asyncNavigation)
        {
            // The navigation completed asynchronously, so navigate to the first item in the results list
            Assert.Equal($"'IGoo' implementations - Entire solution", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                $"<unknown>: Class Implementation",
                string.Join(Environment.NewLine, results.Select(result => $"{result.GetItemOrigin()?.ToString() ?? "<unknown>"}: {result.GetText()}")));
            results[0].NavigateTo(isPreview: false, shouldActivate: true);

            await TestServices.Workarounds.WaitForNavigationAsync(HangMitigatingCancellationToken);
        }

        Assert.Equal($"FileImplementation.vb", await TestServices.Shell.GetActiveDocumentFileNameAsync(HangMitigatingCancellationToken));
        await TestServices.EditorVerifier.TextContainsAsync("Class $$Implementation", assertCaretPosition: true);
        Assert.False(await TestServices.Shell.IsActiveTabProvisionalAsync(HangMitigatingCancellationToken));
    }
}
