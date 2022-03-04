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
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic
{
    [Trait(Traits.Feature, Traits.Features.GoToImplementation)]
    public class BasicGoToImplementation : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicGoToImplementation()
                    : base(nameof(BasicGoToImplementation))
        {
        }

        [IdeFact]
        public async Task SimpleGoToImplementation()
        {
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

            string identifierWithCaret;
            var activeCaption = await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken);
            var dirtyModifier = await TestServices.Editor.GetDirtyIndicatorAsync(HangMitigatingCancellationToken);
            if (activeCaption == $"FileImplementation.vb{dirtyModifier}")
            {
                // The navigation completed synchronously; no further action necessary
                identifierWithCaret = "Implementation$$";
            }
            else
            {
                // The navigation completed asynchronously, so navigate to the first item in the results list
                Assert.Equal($"'IGoo' implementations - Entire solution", activeCaption);
                var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);
                AssertEx.EqualOrDiff(
                    $"<unknown>: Class Implementation",
                    string.Join(Environment.NewLine, results.Select(result => $"{result.GetItemOrigin()?.ToString() ?? "<unknown>"}: {result.GetText()}")));
                results[0].NavigateTo(isPreview: false, shouldActivate: true);

                // It's not clear why this delay is necessary. Navigation operations are expected to fully complete as part
                // of one of the above waiters, but GetActiveWindowCaptionAsync appears to return "Program.cs" (the previous
                // window caption) for a short delay after the above complete.
                await Task.Delay(2000);

                identifierWithCaret = "$$Implementation";
            }

            Assert.Equal($"FileImplementation.vb{dirtyModifier}", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            await TestServices.EditorVerifier.TextContainsAsync($@"Class {identifierWithCaret}", assertCaretPosition: true);
            Assert.False(await TestServices.Shell.IsActiveTabProvisionalAsync(HangMitigatingCancellationToken));
        }
    }
}
