// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicNavigationBar : AbstractIdeEditorTest
    {
        private const string TestSource = @"
Class C
    Public WithEvents Domain As AppDomain
    Public Sub Goo()
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

        [IdeFact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public async Task VerifyNavBarAsync()
        {
            await VisualStudio.Editor.SetTextAsync(TestSource);

            await VisualStudio.Editor.PlaceCaretAsync("Goo", charsOffset: 1);

            await VerifyLeftSelectedAsync("C");
            await VerifyRightSelectedAsync("Goo");

            await VisualStudio.Editor.ExpandTypeNavBarAsync();
            var expectedItems = new[]
            {
                "C",
                "Domain",
                "S"
            };

            Assert.Equal(expectedItems, await VisualStudio.Editor.GetTypeNavBarItemsAsync());

            await VisualStudio.Editor.SelectTypeNavBarItemAsync("S");

            await VisualStudio.Editor.Verify.CaretPositionAsync(112);
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Structure S$$", assertCaretPosition: true);

            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_LineDown);
            await VerifyRightSelectedAsync("A");

            await VisualStudio.Editor.ExpandMemberNavBarAsync();
            expectedItems = new[]
            {
                "A",
                "B",
            };

            Assert.Equal(expectedItems, await VisualStudio.Editor.GetMemberNavBarItemsAsync());
            await VisualStudio.Editor.SelectMemberNavBarItemAsync("B");
            await VisualStudio.Editor.Verify.CaretPositionAsync(169);
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Public Property $$B As Integer", assertCaretPosition: true, trimWhitespace: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public async Task CodeSpitAsync()
        {
            await VisualStudio.Editor.SetTextAsync(TestSource);

            await VisualStudio.Editor.PlaceCaretAsync("C", charsOffset: 1);
            await VerifyLeftSelectedAsync("C");
            await VisualStudio.Editor.ExpandMemberNavBarAsync();
            Assert.Equal(new[] { "New", "Finalize", "Goo" }, await VisualStudio.Editor.GetMemberNavBarItemsAsync());
            await VisualStudio.Editor.SelectMemberNavBarItemAsync("New");
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
    Public Sub New()

    End Sub");
            await VisualStudio.Editor.Verify.CaretPositionAsync(78); // Caret is between New() and End Sub() in virtual whitespace
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public async Task VerifyOptionAsync()
        {
            await VisualStudio.Workspace.SetFeatureOptionAsync(NavigationBarOptions.ShowNavigationBar, LanguageNames.VisualBasic, false);
            Assert.False(await VisualStudio.Editor.IsNavBarEnabledAsync());

            await VisualStudio.Workspace.SetFeatureOptionAsync(NavigationBarOptions.ShowNavigationBar, LanguageNames.VisualBasic, true);
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
