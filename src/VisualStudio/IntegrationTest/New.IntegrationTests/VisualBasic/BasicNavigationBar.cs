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
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.NavigationBar)]
public class BasicNavigationBar : AbstractEditorTest
{
    private const string TestSource = """

        Class C
            Public WithEvents Domain As AppDomain
            Public Sub $$Goo()
            End Sub
        End Class

        Structure S
            Public Property A As Integer
            Public Property B As Integer
        End Structure
        """;

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
        expectedItems =
        [
            "A",
            "B",
        ];

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
        await TestServices.EditorVerifier.TextContainsAsync("""

                Public Sub New()

                End Sub
            """, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CaretPositionAsync(78, HangMitigatingCancellationToken); // Caret is between New() and End Sub() in virtual whitespace
        await TestServices.EditorVerifier.CurrentLineTextAsync("$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyOption()
    {
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);

        globalOptions.SetGlobalOption(NavigationBarViewOptionsStorage.ShowNavigationBar, LanguageNames.VisualBasic, false);
        Assert.False(await TestServices.Editor.IsNavigationBarEnabledAsync(HangMitigatingCancellationToken));

        globalOptions.SetGlobalOption(NavigationBarViewOptionsStorage.ShowNavigationBar, LanguageNames.VisualBasic, true);
        Assert.True(await TestServices.Editor.IsNavigationBarEnabledAsync(HangMitigatingCancellationToken));
    }

    [IdeFact]
    public async Task VerifyEvents()
    {
        await SetUpEditorAsync("""

            $$Class Item1
                Public Event EvA As Action
                Public Event EvB As Action
            End Class

            Class Item2
                Public Event EvX As Action
                Public Event EvY As Action
            End Class

            Partial Class C
                WithEvents item1 As Item1
                WithEvents item2 As Item2
            End Class

            Partial Class C
                Private Sub item1_EvA() Handles item1.EvA
                    ' 1
                End Sub

                Private Sub item1_EvB() Handles item1.EvB
                    ' 2
                End Sub

                Private Sub item2_EvX() Handles item2.EvX
                    ' 3
                End Sub

                Private Sub item2_EvY() Handles item2.EvY
                    ' 4
                End Sub
            End Class
            """, HangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("' 1", charsOffset: 0, HangMitigatingCancellationToken);

        Assert.Equal("item1", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Type, HangMitigatingCancellationToken));
        Assert.Equal("EvA", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken));

        await TestServices.Editor.PlaceCaretAsync("' 2", charsOffset: 0, HangMitigatingCancellationToken);

        Assert.Equal("item1", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Type, HangMitigatingCancellationToken));
        Assert.Equal("EvB", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken));

        await TestServices.Editor.PlaceCaretAsync("' 3", charsOffset: 0, HangMitigatingCancellationToken);

        Assert.Equal("item2", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Type, HangMitigatingCancellationToken));
        Assert.Equal("EvX", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken));

        await TestServices.Editor.PlaceCaretAsync("' 4", charsOffset: 0, HangMitigatingCancellationToken);

        Assert.Equal("item2", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Type, HangMitigatingCancellationToken));
        Assert.Equal("EvY", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken));

        // Selecting an event should update the selected member in the type list.
        await TestServices.Editor.SelectNavigationBarItemAsync(NavigationBarDropdownKind.Member, "EvX", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        $$' 3", assertCaretPosition: true, HangMitigatingCancellationToken);

        // Selecting an WithEvents member in the type list should have no impact on position.
        // But it should update the items in the member list.
        await TestServices.Editor.SelectNavigationBarItemAsync(NavigationBarDropdownKind.Type, "item1", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        $$' 3", assertCaretPosition: true, HangMitigatingCancellationToken);
        Assert.Equal(new[] { "EvA", "EvB" }, await TestServices.Editor.GetNavigationBarItemsAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken));
    }
}
