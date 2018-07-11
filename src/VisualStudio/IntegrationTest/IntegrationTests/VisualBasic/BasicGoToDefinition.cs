// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicGoToDefinition : AbstractIdeEditorTest
    {
        public BasicGoToDefinition()
            : base(nameof(BasicGoToDefinition))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)]
        public async Task GoToClassDeclarationAsync()
        {
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "FileDef.vb");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "FileDef.vb");
            await VisualStudio.Editor.SetTextAsync(
@"Class SomeClass
End Class");
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "FileConsumer.vb");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "FileConsumer.vb");
            await VisualStudio.Editor.SetTextAsync(
@"Class SomeOtherClass
    Dim gibberish As SomeClass
End Class");
            await VisualStudio.Editor.PlaceCaretAsync("SomeClass");
            await VisualStudio.Editor.GoToDefinitionAsync();
            await VisualStudio.Editor.Verify.TextContainsAsync(@"Class SomeClass$$", assertCaretPosition: true);
            Assert.False(await VisualStudio.Shell.IsActiveTabProvisionalAsync());
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)]
        public async Task ObjectBrowserNavigationAsync()
        {
            await SetUpEditorAsync(
@"Class C
    Dim i As Integer$$
End Class");
            await VisualStudio.Workspace.SetFeatureOptionAsync(VisualStudioNavigationOptions.NavigateToObjectBrowser, LanguageName, true);

            await VisualStudio.Editor.GoToDefinitionAsync();
            Assert.Equal("Object Browser", await VisualStudio.Shell.GetActiveWindowCaptionAsync());

            await VisualStudio.Workspace.SetFeatureOptionAsync(VisualStudioNavigationOptions.NavigateToObjectBrowser, LanguageName, false);

            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.vb");
            await VisualStudio.Editor.GoToDefinitionAsync();
            await VisualStudio.Editor.Verify.TextContainsAsync("Public Structure Int32");
        }
    }
}
