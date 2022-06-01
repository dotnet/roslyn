// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
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
            var dirtyModifier = await TestServices.Editor.GetDirtyIndicatorAsync(HangMitigatingCancellationToken);
            Assert.Equal($"FileImplementation.vb{dirtyModifier}", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            await TestServices.EditorVerifier.TextContainsAsync(@"Class Implementation$$", assertCaretPosition: true);
            Assert.False(await TestServices.Shell.IsActiveTabProvisionalAsync(HangMitigatingCancellationToken));
        }
    }
}
