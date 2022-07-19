// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.NavigationBar)]
    public class CSharpNavigationBar : AbstractEditorTest
    {
        private const string TestSource = @"
class C
{
    public void M(int i) { }
    private C $$this[int index] { get { return null; } set { } }
    public static bool operator ==(C c1, C c2) { return true; }
    public static bool operator !=(C c1, C c2) { return false; }
}

struct S
{
    int Goo() { }
    void Bar() { }
}";

        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpNavigationBar()
            : base(nameof(CSharpNavigationBar))
        {
        }

        [IdeFact]
        public async Task VerifyNavBar()
        {
            await SetUpEditorAsync(TestSource, HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("this", charsOffset: 1, HangMitigatingCancellationToken);
            await TestServices.Editor.ExpandNavigationBarAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken);
            var expectedItems = new[]
            {
                "M(int i)",
                "operator !=(C c1, C c2)",
                "operator ==(C c1, C c2)",
                "this[int index]"
            };

            Assert.Equal(expectedItems, await TestServices.Editor.GetNavigationBarItemsAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken));
            await TestServices.Editor.SelectNavigationBarItemAsync(NavigationBarDropdownKind.Member, "operator !=(C c1, C c2)", HangMitigatingCancellationToken);

            await TestServices.EditorVerifier.CurrentLineTextAsync("    public static bool operator $$!=(C c1, C c2) { return false; }", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task VerifyNavBar2()
        {
            await SetUpEditorAsync(TestSource, HangMitigatingCancellationToken);

            Assert.Equal("C", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Type, HangMitigatingCancellationToken));
            Assert.Equal("this[int index]", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken));

            await TestServices.Editor.ExpandNavigationBarAsync(NavigationBarDropdownKind.Type, HangMitigatingCancellationToken);

            await TestServices.Editor.SelectNavigationBarItemAsync(NavigationBarDropdownKind.Type, "S", HangMitigatingCancellationToken);

            Assert.Equal("S", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Type, HangMitigatingCancellationToken));
            Assert.Equal("Goo()", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken));
            await TestServices.EditorVerifier.CurrentLineTextAsync("struct $$S", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task VerifyNavBar3()
        {
            await SetUpEditorAsync(@"
struct S$$
{
    int Goo() { }
    void Bar() { }
}", HangMitigatingCancellationToken);
            await TestServices.Editor.ExpandNavigationBarAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken);
            var expectedItems = new[]
            {
                "Bar()",
                "Goo()",
            };
            Assert.Equal(expectedItems, await TestServices.Editor.GetNavigationBarItemsAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken));
            await TestServices.Editor.SelectNavigationBarItemAsync(NavigationBarDropdownKind.Member, "Bar()", HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("    void $$Bar() { }", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd2KCmdID.UP, HangMitigatingCancellationToken);
            Assert.Equal("Goo()", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task TestSplitWindow()
        {
            await TestServices.Editor.SetTextAsync(@"
class C
{
    public void M(int i) { }
    private C this[int index] { get { return null; } set { } }
}

struct S
{
    int Goo() { }
    void Bar() { }
}", HangMitigatingCancellationToken);
            await TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd97CmdID.Split, HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("this", charsOffset: 1, HangMitigatingCancellationToken);
            Assert.Equal("C", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Type, HangMitigatingCancellationToken));
            Assert.Equal("this[int index]", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken));
            await TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd97CmdID.SplitNext, HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("Goo", charsOffset: 1, HangMitigatingCancellationToken);
            Assert.Equal("S", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Type, HangMitigatingCancellationToken));
            Assert.Equal("Goo()", await TestServices.Editor.GetNavigationBarSelectionAsync(NavigationBarDropdownKind.Member, HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task VerifyOption()
        {
            var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
            globalOptions.SetGlobalOption(new OptionKey(NavigationBarViewOptions.ShowNavigationBar, LanguageNames.CSharp), false);
            Assert.False(await TestServices.Editor.IsNavigationBarEnabledAsync(HangMitigatingCancellationToken));

            globalOptions.SetGlobalOption(new OptionKey(NavigationBarViewOptions.ShowNavigationBar, LanguageNames.CSharp), true);
            Assert.True(await TestServices.Editor.IsNavigationBarEnabledAsync(HangMitigatingCancellationToken));
        }
    }
}
