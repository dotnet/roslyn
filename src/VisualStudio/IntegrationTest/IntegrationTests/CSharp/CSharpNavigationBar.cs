// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpNavigationBar : AbstractIdeEditorTest
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

        [IdeFact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public async Task VerifyNavBarAsync()
        {
            await SetUpEditorAsync(TestSource);
            await VisualStudio.Editor.PlaceCaretAsync("this", charsOffset: 1);
            await VisualStudio.Editor.ExpandMemberNavBarAsync();
            var expectedItems = new[]
            {
                "M(int i)",
                "operator !=(C c1, C c2)",
                "operator ==(C c1, C c2)",
                "this[int index]"
            };

            Assert.Equal(expectedItems, await VisualStudio.Editor.GetMemberNavBarItemsAsync());
            await VisualStudio.Editor.SelectMemberNavBarItemAsync("operator !=(C c1, C c2)");

            await VisualStudio.Editor.Verify.CurrentLineTextAsync("public static bool operator $$!=(C c1, C c2) { return false; }", assertCaretPosition: true, trimWhitespace: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public async Task VerifyNavBar2Async()
        {
            await SetUpEditorAsync(TestSource);

            await VerifyLeftSelectedAsync("C");
            await VerifyRightSelectedAsync("this[int index]");

            await VisualStudio.Editor.ExpandTypeNavBarAsync();
            var expectedItems = new[]
            {
                "C",
                "S",
            };

            await VisualStudio.Editor.SelectTypeNavBarItemAsync("S");

            await VerifyLeftSelectedAsync("S");
            await VerifyRightSelectedAsync("Goo()");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("$$struct S", assertCaretPosition: true, trimWhitespace: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public async Task VerifyNavBar3Async()
        {
            await SetUpEditorAsync(@"
struct S$$
{
    int Goo() { }
    void Bar() { }
}");
            await VisualStudio.Editor.ExpandMemberNavBarAsync();
            var expectedItems = new[]
            {
                "Bar()",
                "Goo()",
            };
            Assert.Equal(expectedItems, await VisualStudio.Editor.GetMemberNavBarItemsAsync());
            await VisualStudio.Editor.SelectMemberNavBarItemAsync("Bar()");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("void $$Bar() { }", assertCaretPosition: true, trimWhitespace: true);

            await VisualStudio.VisualStudio.ExecuteCommandAsync("Edit.LineUp");
            await VerifyRightSelectedAsync("Goo()");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public async Task TestSplitWindowAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"
class C
{
    public void M(int i) { }
    private C this[int index] { get { return null; } set { } }
}

struct S
{
    int Goo() { }
    void Bar() { }
}");
            await VisualStudio.VisualStudio.ExecuteCommandAsync("Window.Split");
            await VisualStudio.Editor.PlaceCaretAsync("this", charsOffset: 1);
            await VerifyLeftSelectedAsync("C");
            await VerifyRightSelectedAsync("this[int index]");
            await VisualStudio.VisualStudio.ExecuteCommandAsync("Window.NextSplitPane");
            await VisualStudio.Editor.PlaceCaretAsync("Goo", charsOffset: 1);
            await VerifyLeftSelectedAsync("S");
            await VerifyRightSelectedAsync("Goo()");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public async Task VerifyOptionAsync()
        {
            await VisualStudio.Workspace.SetFeatureOptionAsync(NavigationBarOptions.ShowNavigationBar, LanguageNames.CSharp, false);
            Assert.False(await VisualStudio.Editor.IsNavBarEnabledAsync());

            await VisualStudio.Workspace.SetFeatureOptionAsync(NavigationBarOptions.ShowNavigationBar, LanguageNames.CSharp, true);
            Assert.True(await VisualStudio.Editor.IsNavBarEnabledAsync());
        }

        private async Task VerifyLeftSelectedAsync(string expected)
        {
            Assert.Equal(expected, await VisualStudio.Editor.GetTypeNavBarSelectionAsync());
        }

        private async Task VerifyRightSelectedAsync(string expected)
        {
            Assert.Equal(expected, await VisualStudio.Editor.GetMemberNavBarSelectionAsync());
        }
    }
}
