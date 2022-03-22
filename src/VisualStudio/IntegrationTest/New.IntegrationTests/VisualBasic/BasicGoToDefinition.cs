// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic
{
    [Trait(Traits.Feature, Traits.Features.GoToDefinition)]
    public class BasicGoToDefinition : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicGoToDefinition()
            : base(nameof(BasicGoToDefinition), WellKnownProjectTemplates.VisualBasicNetCoreClassLibrary)
        {
        }

        [IdeFact]
        public async Task GoToClassDeclaration()
        {
            var project = ProjectName;
            await TestServices.SolutionExplorer.AddFileAsync(project, "FileDef.vb", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "FileDef.vb", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"Class SomeClass
End Class", HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddFileAsync(project, "FileConsumer.vb", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "FileConsumer.vb", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"Class SomeOtherClass
    Dim gibberish As SomeClass
End Class", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("SomeClass", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            var dirtyModifier = await TestServices.Editor.GetDirtyIndicatorAsync(HangMitigatingCancellationToken);
            Assert.Equal($"FileDef.vb{dirtyModifier}", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            await TestServices.EditorVerifier.TextContainsAsync(@"Class SomeClass$$", assertCaretPosition: true, HangMitigatingCancellationToken);
            Assert.False(await TestServices.Shell.IsActiveTabProvisionalAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task ObjectBrowserNavigation()
        {
            var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);

            await SetUpEditorAsync(
@"Class C
    Dim i As Integer$$
End Class", HangMitigatingCancellationToken);
            globalOptions.SetGlobalOption(new OptionKey(VisualStudioNavigationOptions.NavigateToObjectBrowser, LanguageNames.VisualBasic), true);

            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Equal("Object Browser", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));

            globalOptions.SetGlobalOption(new OptionKey(VisualStudioNavigationOptions.NavigateToObjectBrowser, LanguageNames.VisualBasic), false);
            globalOptions.SetGlobalOption(new OptionKey(MetadataAsSourceOptionsStorage.NavigateToDecompiledSources, language: null), false);

            await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.vb", HangMitigatingCancellationToken);
            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Equal("Int32 [from metadata]", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            await TestServices.EditorVerifier.TextContainsAsync("Public Structure Int32", cancellationToken: HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task GoToBaseFromMetadataAsSource()
        {
            var project = ProjectName;
            await TestServices.SolutionExplorer.AddFileAsync(project, "SomeClass.vb", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "SomeClass.vb", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"Class SomeClass
    Public Overrides Function ToString() As String
        Return MyBase.ToString()
    End Function
End Class", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("Overrides", charsOffset: -1, HangMitigatingCancellationToken);
            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Equal("Object [from metadata]", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            await TestServices.EditorVerifier.TextContainsAsync(@"Public Overridable Function ToString$$() As String", assertCaretPosition: true);
        }
    }
}
