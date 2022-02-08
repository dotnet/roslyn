// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic
{
    [Trait(Traits.Feature, Traits.Features.NavigationBar)]
    public class BasicNavigationBar : AbstractEditorTest
    {
        private const string TestSource = @"
Class C
    Public WithEvents Domain As AppDomain
    Public Sub $$Goo()
    End Sub
End Class

Structure S
    Public Property A As Integer
    Public Property B As Integer
End Structure";

        public BasicNavigationBar()
            : base(nameof(BasicNavigationBar))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact]
        public async Task VerifyNavBar()
        {
            await SetUpEditorAsync(TestSource, HangMitigatingCancellationToken);

            await TestServices.Editor.PlaceCaretAsync("Goo", charsOffset: 1, HangMitigatingCancellationToken);

            Assert.Equal("C", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Type, HangMitigatingCancellationToken));
            Assert.Equal("Goo", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken));

            await TestServices.Editor.ExpandNavigationBarAsync(NavigationBarDropdownKind.Type, HangMitigatingCancellationToken);
            var expectedItems = new[]
            {
                "C",
                "Domain",
                "S",
            };

            Assert.Equal(expectedItems, await TestServices.Editor.GetNavigationBarItemsAsync(NavigationBarDropdownKind.Type, HangMitigatingCancellationToken));

            await TestServices.Editor.SelectNavigationBarItemAsync(NavigationBarDropdownKind.Type, "S", HangMitigatingCancellationToken);

            await TestServices.EditorVerifier.CaretPositionAsync(112, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("Structure $$S", assertCaretPosition: true);

            var view = await TestServices.Editor.GetActiveTextViewAsync(HangMitigatingCancellationToken);
            var editorOperationsFactory = await TestServices.Shell.GetComponentModelServiceAsync<IEditorOperationsFactoryService>(HangMitigatingCancellationToken);
            var editorOperations = editorOperationsFactory.GetEditorOperations(view);
            editorOperations.MoveLineDown(extendSelection: false);

            Assert.Equal("A", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken));

            await TestServices.Editor.ExpandNavigationBarAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken);
            expectedItems = new[]
            {
                "A",
                "B",
            };

            Assert.Equal(expectedItems, await TestServices.Editor.GetNavigationBarItemsAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken));
            await TestServices.Editor.SelectNavigationBarItemAsync(NavigationBarDropdownKind.Member, "B", HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CaretPositionAsync(169, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("    Public Property $$B As Integer", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task CodeSpit()
        {
            await SetUpEditorAsync(TestSource, HangMitigatingCancellationToken);

            await TestServices.Editor.PlaceCaretAsync("C", charsOffset: 1, HangMitigatingCancellationToken);
            Assert.Equal("C", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Type, HangMitigatingCancellationToken));
            await TestServices.Editor.ExpandNavigationBarAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken);
            Assert.Equal(new[] { "New", "Finalize", "Goo" }, await TestServices.Editor.GetNavigationBarItemsAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken));
            await TestServices.Editor.SelectNavigationBarItemAsync(NavigationBarDropdownKind.Member, "New", HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.TextContainsAsync(@"
    Public Sub New()

    End Sub", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CaretPositionAsync(78, HangMitigatingCancellationToken); // Caret is between New() and End Sub() in virtual whitespace
            await TestServices.EditorVerifier.CurrentLineTextAsync("$$", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task VerifyOption()
        {
            var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);

            globalOptions.SetGlobalOption(new OptionKey(NavigationBarViewOptions.ShowNavigationBar, LanguageNames.VisualBasic), false);
            Assert.False(await TestServices.Editor.IsNavigationBarEnabledAsync(HangMitigatingCancellationToken));

            globalOptions.SetGlobalOption(new OptionKey(NavigationBarViewOptions.ShowNavigationBar, LanguageNames.VisualBasic), true);
            Assert.True(await TestServices.Editor.IsNavigationBarEnabledAsync(HangMitigatingCancellationToken));
        }
    }
}
