// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpIntelliSense : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpIntelliSense()
            : base(nameof(CSharpIntelliSense))
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AtNamespaceLevelAsync()
        {
            await SetUpEditorAsync(@"$$");

            await VisualStudio.Editor.SendKeysAsync("usi");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("using");

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Tab);
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("using$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SpeculativeTInListAsync()
        {
            await SetUpEditorAsync(@"
class C
{
    $$
}");

            await VisualStudio.Editor.SendKeysAsync("pub");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("public");

            await VisualStudio.Editor.SendKeysAsync(' ');
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("public $$", assertCaretPosition: true);

            await VisualStudio.Editor.SendKeysAsync('t');
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("T");

            await VisualStudio.Editor.SendKeysAsync(' ');
            await VisualStudio.Editor.SendKeysAsync("Goo<T>() { }");
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
class C
{
    public T Goo<T>() { }$$
}",
assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task VerifyCompletionListMembersOnStaticTypesAndCompleteThemAsync()
        {
            await SetUpEditorAsync(@"
public class Program
{
    static void Main(string[] args)
    {
        NavigateTo$$
    }
}

public static class NavigateTo
{
    public static void Search(string s){ }
    public static void Navigate(int i){ }
}");

            await VisualStudio.Editor.SendKeysAsync('.');
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("Search", "Navigate");

            await VisualStudio.Editor.SendKeysAsync('S', VirtualKey.Tab);
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("NavigateTo.Search$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CtrlAltSpaceAsync()
        {
            await VisualStudio.Workspace.SetUseSuggestionModeAsync(false);

            await VisualStudio.Editor.SendKeysAsync("nam Goo", VirtualKey.Enter);
            await VisualStudio.Editor.SendKeysAsync('{', VirtualKey.Enter, '}', VirtualKey.Up, VirtualKey.Enter);
            await VisualStudio.Editor.SendKeysAsync("pu cla Program", VirtualKey.Enter);
            await VisualStudio.Editor.SendKeysAsync('{', VirtualKey.Enter, '}', VirtualKey.Up, VirtualKey.Enter);
            await VisualStudio.Editor.SendKeysAsync("pub stati voi Main(string[] args)", VirtualKey.Enter);
            await VisualStudio.Editor.SendKeysAsync('{', VirtualKey.Enter, '}', VirtualKey.Up, VirtualKey.Enter);
            await VisualStudio.Editor.SendKeysAsync("System.Console.writeline();");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("System.Console.WriteLine();$$", assertCaretPosition: true);
            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Home, Shift(VirtualKey.End), VirtualKey.Delete);

            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_ToggleCompletionMode);

            await VisualStudio.Editor.SendKeysAsync("System.Console.writeline();");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("System.Console.writeline();$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CtrlAltSpaceOptionAsync()
        {
            await VisualStudio.Workspace.SetUseSuggestionModeAsync(false);

            await VisualStudio.Editor.SendKeysAsync("nam Goo");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("namespace Goo$$", assertCaretPosition: true);

            await ClearEditorAsync();
            await VisualStudio.Workspace.SetUseSuggestionModeAsync(true);

            await VisualStudio.Editor.SendKeysAsync("nam Goo");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("nam Goo$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CtrlSpaceAsync()
        {
            await SetUpEditorAsync("class c { void M() {$$ } }");
            await VisualStudio.Editor.SendKeysAsync(Ctrl(VirtualKey.Space));
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("System");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NavigatingWithDownKeyAsync()
        {
            await SetUpEditorAsync("class c { void M() {$$ } }");
            await VisualStudio.Editor.SendKeysAsync('c');
            await VisualStudio.Editor.Verify.CurrentCompletionItemAsync("c");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("c");

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Down);
            await VisualStudio.Editor.Verify.CurrentCompletionItemAsync("char");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("char");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task XmlDocCommentIntelliSenseAsync()
        {
            await SetUpEditorAsync(@"
class Class1
{
    ///$$
    void Main(string[] args)
    {
    
    }
}");

            await VisualStudio.Editor.SendKeysAsync("<s");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("see", "seealso", "summary");

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Enter);
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("///<see cref=\"$$\"/>", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task XmlTagCompletionAsync()
        {
            await SetUpEditorAsync(@"
/// $$
class C { }
");

            await VisualStudio.Editor.SendKeysAsync("<summary>");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("/// <summary>$$</summary>", assertCaretPosition: true);

            await SetUpEditorAsync(@"
/// <summary>$$
class C { }
");

            await VisualStudio.Editor.SendKeysAsync("</");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("/// <summary></summary>$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SignatureHelpShowsUpAsync()
        {
            await SetUpEditorAsync(@"
class Class1
{
    void Main(string[] args)
    {
        $$
    }
}");

            await VisualStudio.Workspace.SetUseSuggestionModeAsync(false);

            await VisualStudio.Editor.SendKeysAsync("Mai(");

            await VisualStudio.Editor.Verify.CurrentSignatureAsync("void Class1.Main(string[] args)");
            await VisualStudio.Editor.Verify.CurrentParameterAsync("args", "");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionUsesTrackingPointsInTheFaceOfAutomaticBraceCompletionAsync()
        {
            await SetUpEditorAsync(@"
class Class1
{
    void Main(string[] args)
    $$
}");

            await VisualStudio.Workspace.SetUseSuggestionModeAsync(false);

            await VisualStudio.Editor.SendKeysAsync(
                '{',
                VirtualKey.Enter,
                "                 ");

            await VisualStudio.Editor.InvokeCompletionListAsync();

            await VisualStudio.Editor.SendKeysAsync('}');

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
class Class1
{
    void Main(string[] args)
    {
    }$$
}",
assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitOnShiftEnterAsync()
        {
            await SetUpEditorAsync(@"
class Class1
{
    void Main(string[] args)
    {
        $$
    }
}");

            await VisualStudio.Workspace.SetUseSuggestionModeAsync(false);

            await VisualStudio.Editor.SendKeysAsync(
                'M',
                Shift(VirtualKey.Enter));

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
class Class1
{
    void Main(string[] args)
    {
        Main$$
    }
}",
assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitOnLeftCurlyAsync()
        {
            await SetUpEditorAsync(@"
class Class1
{
    $$
}");

            await VisualStudio.Workspace.SetUseSuggestionModeAsync(false);

            await VisualStudio.Editor.SendKeysAsync("int P { g{");

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
class Class1
{
    int P { get { $$} }
}",
assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EnsureTheCaretIsVisibleAfterALongEditAsync()
        {
            await SetUpEditorAsync(@"
public class Program
{
    static void Main(string[] args)
    {
        var aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa = 0;
        aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa = $$
    }
}");

            await VisualStudio.Editor.SendKeysAsync(
                VirtualKey.Delete,
                "aaa",
                VirtualKey.Tab);
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa = aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", actualText);
            Assert.True(await VisualStudio.Editor.IsCaretOnScreenAsync());
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DismissOnSelectAsync()
        {
            await SetUpEditorAsync(@"$$");

            await VisualStudio.Editor.SendKeysAsync(Ctrl(VirtualKey.Space));
            Assert.Equal(true, await VisualStudio.Editor.IsCompletionActiveAsync());

            await VisualStudio.Editor.SendKeysAsync(Ctrl(VirtualKey.A));
            Assert.Equal(false, await VisualStudio.Editor.IsCompletionActiveAsync());
        }
    }
}
