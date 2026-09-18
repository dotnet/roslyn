// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.Completion)]
public class CSharpIntelliSense : AbstractEditorTest
{
    private readonly ITestOutputHelper _output;

    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpIntelliSense(ITestOutputHelper output)
        : base(nameof(CSharpIntelliSense))
    {
        _output = output;
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Try disable the responsive completion option again: https://github.com/dotnet/roslyn/issues/70787
        await TestServices.StateReset.DisableResponsiveCompletion(HangMitigatingCancellationToken);

        // Disable import completion.
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, false);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.VisualBasic, false);
    }

    [IdeTheory, CombinatorialData]
    public async Task AtNamespaceLevel(bool showCompletionInArgumentLists)
    {
        _output.WriteLine($"CSharpIntelliSense.AtNamespaceLevel(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 1");
        await SetUpEditorAsync(@"$$", HangMitigatingCancellationToken);

        _output.WriteLine($"CSharpIntelliSense.AtNamespaceLevel(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 2");
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.AtNamespaceLevel(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 3");
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        _output.WriteLine($"CSharpIntelliSense.AtNamespaceLevel(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 4");
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        _output.WriteLine($"CSharpIntelliSense.AtNamespaceLevel(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 5");
        await TestServices.Input.SendAsync("usi", HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.AtNamespaceLevel(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 6");
        Assert.Contains("using", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));

        _output.WriteLine($"CSharpIntelliSense.AtNamespaceLevel(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 7");
        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.AtNamespaceLevel(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 8");
        await TestServices.EditorVerifier.CurrentLineTextAsync("using$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task SpeculativeTInList(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C
            {
                $$
            }
            """, HangMitigatingCancellationToken);

        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        await TestServices.Input.SendAsync("pub", HangMitigatingCancellationToken);
        Assert.Contains("public", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));

        await TestServices.Input.SendAsync(' ', HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    public $$", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync('t', HangMitigatingCancellationToken);
        Assert.Contains("T", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));

        await TestServices.Input.SendAsync(' ', HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync("Goo<T>() { }", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync("""

            class C
            {
                public T Goo<T>() { }$$
            }
            """,
assertCaretPosition: true,
HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task VerifyCompletionListMembersOnStaticTypesAndCompleteThem(bool showCompletionInArgumentLists)
    {
        _output.WriteLine($"CSharpIntelliSense.VerifyCompletionListMembersOnStaticTypesAndCompleteThem(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 1");
        await SetUpEditorAsync("""

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
            }
            """, HangMitigatingCancellationToken);

        _output.WriteLine($"CSharpIntelliSense.VerifyCompletionListMembersOnStaticTypesAndCompleteThem(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 2");
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.VerifyCompletionListMembersOnStaticTypesAndCompleteThem(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 3");
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        _output.WriteLine($"CSharpIntelliSense.VerifyCompletionListMembersOnStaticTypesAndCompleteThem(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 4");
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        _output.WriteLine($"CSharpIntelliSense.VerifyCompletionListMembersOnStaticTypesAndCompleteThem(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 5");
        await TestServices.Input.SendAsync('.', HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.VerifyCompletionListMembersOnStaticTypesAndCompleteThem(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 6");
        Assert.Contains("Search", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));
        _output.WriteLine($"CSharpIntelliSense.VerifyCompletionListMembersOnStaticTypesAndCompleteThem(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 7");
        Assert.Contains("Navigate", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));

        _output.WriteLine($"CSharpIntelliSense.VerifyCompletionListMembersOnStaticTypesAndCompleteThem(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 8");
        await TestServices.Input.SendAsync(['S', VirtualKeyCode.TAB], HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.VerifyCompletionListMembersOnStaticTypesAndCompleteThem(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 9");
        await TestServices.EditorVerifier.CurrentLineTextAsync("        NavigateTo.Search$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task CtrlAltSpace(bool showCompletionInArgumentLists)
    {
        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 1");
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 2");
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 3");
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 4");
        await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);

        // Note: the completion needs to be unambiguous for the test to be deterministic.
        // Otherwise the result might depend on the state of MRU list.

        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 5");
        await TestServices.Input.SendAsync("names", HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 6");
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 7");
        await TestServices.Input.SendAsync([" Goo", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 8");
        await TestServices.Input.SendAsync(['{', VirtualKeyCode.RETURN, '}', VirtualKeyCode.UP, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);

        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 9");
        await TestServices.Input.SendAsync("pu", HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 10");
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 11");
        await TestServices.Input.SendAsync(" cla", HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 12");
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 13");
        await TestServices.Input.SendAsync([" Program", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 14");
        await TestServices.Input.SendAsync(['{', VirtualKeyCode.RETURN, '}', VirtualKeyCode.UP, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);

        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 15");
        await TestServices.Input.SendAsync("pub", HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 16");
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 17");
        await TestServices.Input.SendAsync(" stati", HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 18");
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 19");
        await TestServices.Input.SendAsync(" voi", HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 20");
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 21");
        await TestServices.Input.SendAsync([" Main(string[] args)", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 22");
        await TestServices.Input.SendAsync(['{', VirtualKeyCode.RETURN, '}', VirtualKeyCode.UP, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);

        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 23");
        await TestServices.Input.SendAsync("System.Console.", HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 24");
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 25");
        await TestServices.Input.SendAsync("writeline();", HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 26");
        await TestServices.EditorVerifier.CurrentLineTextAsync("            System.Console.WriteLine();$$", assertCaretPosition: true, HangMitigatingCancellationToken);

        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 27");
        await TestServices.Input.SendAsync([VirtualKeyCode.HOME, (VirtualKeyCode.END, VirtualKeyCode.SHIFT), VirtualKeyCode.DELETE], HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 28");
        await TestServices.Input.SendAsync(new InputKey(VirtualKeyCode.SPACE, [VirtualKeyCode.CONTROL, VirtualKeyCode.MENU]), HangMitigatingCancellationToken);

        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 29");
        await TestServices.Input.SendAsync("System.Console.", HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 30");
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 31");
        await TestServices.Input.SendAsync("writeline();", HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.CtrlAltSpace(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 32");
        await TestServices.EditorVerifier.CurrentLineTextAsync("            System.Console.writeline();$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task CtrlAltSpaceOption(bool showCompletionInArgumentLists)
    {
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("names", HangMitigatingCancellationToken);
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        await TestServices.Input.SendAsync(" Goo", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("namespace Goo$$", assertCaretPosition: true, HangMitigatingCancellationToken);

        await ClearEditorAsync(HangMitigatingCancellationToken);
        await TestServices.Editor.SetUseSuggestionModeAsync(true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("nam", HangMitigatingCancellationToken);
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        await TestServices.Input.SendAsync(" Goo", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("nam Goo$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task CtrlSpace(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("class c { void M() {$$ } }", HangMitigatingCancellationToken);

        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        await TestServices.Input.SendAsync((VirtualKeyCode.SPACE, VirtualKeyCode.CONTROL), HangMitigatingCancellationToken);
        Assert.Contains("System", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));
    }

    [IdeTheory, CombinatorialData]
    public async Task NavigatingWithDownKey(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("class c { void M() {$$ } }", HangMitigatingCancellationToken);

        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        await TestServices.Input.SendAsync('c', HangMitigatingCancellationToken);
        Assert.Equal("c", (await TestServices.Editor.GetCurrentCompletionItemAsync(HangMitigatingCancellationToken)).DisplayText);
        Assert.Contains("c", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));

        await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
        Assert.Equal("char", (await TestServices.Editor.GetCurrentCompletionItemAsync(HangMitigatingCancellationToken)).DisplayText);
        Assert.Contains("char", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));
    }

    [IdeTheory, CombinatorialData]
    public async Task XmlDocCommentIntelliSense(bool showCompletionInArgumentLists)
    {
        _output.WriteLine($"CSharpIntelliSense.XmlDocCommentIntelliSense(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 1");
        await SetUpEditorAsync("""

            class Class1
            {
                ///$$
                void Main(string[] args)
                {
                
                }
            }
            """, HangMitigatingCancellationToken);

        _output.WriteLine($"CSharpIntelliSense.XmlDocCommentIntelliSense(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 2");
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.XmlDocCommentIntelliSense(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 3");
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        _output.WriteLine($"CSharpIntelliSense.XmlDocCommentIntelliSense(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 4");
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        _output.WriteLine($"CSharpIntelliSense.XmlDocCommentIntelliSense(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 5");
        await TestServices.Input.SendAsync("<s", HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.XmlDocCommentIntelliSense(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 6");
        Assert.Contains("see", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));
        _output.WriteLine($"CSharpIntelliSense.XmlDocCommentIntelliSense(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 7");
        Assert.Contains("seealso", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));
        _output.WriteLine($"CSharpIntelliSense.XmlDocCommentIntelliSense(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 8");
        Assert.Contains("summary", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));

        // 🐛 Workaround for https://github.com/dotnet/roslyn/issues/33824
        _output.WriteLine($"CSharpIntelliSense.XmlDocCommentIntelliSense(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 9");
        var completionItems = (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).SelectAsArray(item => item.DisplayText);
        _output.WriteLine($"CSharpIntelliSense.XmlDocCommentIntelliSense(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 10");
        var targetIndex = completionItems.IndexOf("see");
        _output.WriteLine($"CSharpIntelliSense.XmlDocCommentIntelliSense(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 11");
        var currentIndex = completionItems.IndexOf((await TestServices.Editor.GetCurrentCompletionItemAsync(HangMitigatingCancellationToken)).DisplayText);
        _output.WriteLine($"CSharpIntelliSense.XmlDocCommentIntelliSense(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 12");
        if (currentIndex != targetIndex)
        {
            _output.WriteLine($"CSharpIntelliSense.XmlDocCommentIntelliSense(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 13");
            InputKey key = currentIndex < targetIndex ? VirtualKeyCode.DOWN : VirtualKeyCode.UP;
            _output.WriteLine($"CSharpIntelliSense.XmlDocCommentIntelliSense(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 14");
            var keys = Enumerable.Repeat(key, Math.Abs(currentIndex - targetIndex)).ToArray();
            _output.WriteLine($"CSharpIntelliSense.XmlDocCommentIntelliSense(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 15");
            await TestServices.Input.SendAsync(keys, HangMitigatingCancellationToken);
        }

        _output.WriteLine($"CSharpIntelliSense.XmlDocCommentIntelliSense(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 16");
        await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.XmlDocCommentIntelliSense(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 17");
        await TestServices.EditorVerifier.CurrentLineTextAsync("    ///<see cref=\"$$\"/>", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task XmlTagCompletion(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            /// $$
            class C { }

            """, HangMitigatingCancellationToken);

        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        await TestServices.Input.SendAsync("<summary>", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("/// <summary>$$</summary>", assertCaretPosition: true, HangMitigatingCancellationToken);

        await SetUpEditorAsync("""

            /// <summary>$$
            class C { }

            """, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("</", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("/// <summary></summary>$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task SignatureHelpShowsUp(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class Class1
            {
                void Main(string[] args)
                {
                    $$
                }
            }
            """, HangMitigatingCancellationToken);

        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("Mai", HangMitigatingCancellationToken);
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        await TestServices.Input.SendAsync("(", HangMitigatingCancellationToken);

        var currentSignature = await TestServices.Editor.GetCurrentSignatureAsync(HangMitigatingCancellationToken);
        Assert.Equal("void Class1.Main(string[] args)", currentSignature.Content);
        Assert.NotNull(currentSignature.CurrentParameter);
        Assert.Equal("args", currentSignature.CurrentParameter.Name);
        Assert.Equal("", currentSignature.CurrentParameter.Documentation);
    }

    [IdeTheory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/33825")]
    public async Task CompletionUsesTrackingPointsInTheFaceOfAutomaticBraceCompletion(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class Class1
            {
                void Main(string[] args)
                $$
            }
            """, HangMitigatingCancellationToken);

        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(
            [
                '{',
                VirtualKeyCode.RETURN,
                "                 ",
            ],
            HangMitigatingCancellationToken);

        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.ListMembers, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.CompletionSet, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync('}', HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync("""

            class Class1
            {
                void Main(string[] args)
                {
                }$$
            }
            """,
assertCaretPosition: true,
HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    [Trait(Traits.TestGate, Traits.TestGates.RoslynVSIntegration)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/33823")]
    public async Task CommitOnShiftEnter(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class Class1
            {
                void Main(string[] args)
                {
                    $$
                }
            }
            """, HangMitigatingCancellationToken);

        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(
            [
                'M',
                (VirtualKeyCode.RETURN, VirtualKeyCode.SHIFT),
            ],
            HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync("""

            class Class1
            {
                void Main(string[] args)
                {
                    Main
            $$
                }
            }
            """,
assertCaretPosition: true,
HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task LineBreakOnShiftEnter(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class Class1
            {
                void Main(string[] args)
                {
                    $$
                }
            }
            """, HangMitigatingCancellationToken);

        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        await TestServices.Editor.SetUseSuggestionModeAsync(true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(
            [
                'M',
                (VirtualKeyCode.RETURN, VirtualKeyCode.SHIFT),
            ],
            HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync("""

            class Class1
            {
                void Main(string[] args)
                {
                    Main
            $$
                }
            }
            """,
assertCaretPosition: true,
HangMitigatingCancellationToken);

    }

    [IdeTheory, CombinatorialData]
    public async Task CommitOnLeftCurly(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class Class1
            {
                $$
            }
            """, HangMitigatingCancellationToken);

        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("int P { g", HangMitigatingCancellationToken);
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        await TestServices.Input.SendAsync("{", HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync("""

            class Class1
            {
                int P { get { $$} }
            }
            """,
assertCaretPosition: true,
HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/33822")]
    public async Task EnsureTheCaretIsVisibleAfterALongEdit(bool showCompletionInArgumentLists)
    {
        _output.WriteLine($"CSharpIntelliSense.EnsureTheCaretIsVisibleAfterALongEdit(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 1");
        var visibleColumns = await TestServices.Editor.GetVisibleColumnCountAsync(HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.EnsureTheCaretIsVisibleAfterALongEdit(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 2");
        var variableName = new string('a', (int)(0.75 * visibleColumns));
        _output.WriteLine($"CSharpIntelliSense.EnsureTheCaretIsVisibleAfterALongEdit(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 3");
        await SetUpEditorAsync($$"""

            public class Program
            {
                static void Main(string[] args)
                {
                    var {{variableName}} = 0;
                    {{variableName}} = $$
                }
            }
            """, HangMitigatingCancellationToken);

        _output.WriteLine($"CSharpIntelliSense.EnsureTheCaretIsVisibleAfterALongEdit(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 4");
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.EnsureTheCaretIsVisibleAfterALongEdit(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 5");
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        _output.WriteLine($"CSharpIntelliSense.EnsureTheCaretIsVisibleAfterALongEdit(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 6");
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        _output.WriteLine($"CSharpIntelliSense.EnsureTheCaretIsVisibleAfterALongEdit(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 7");
        Assert.True(variableName.Length > 0);
        _output.WriteLine($"CSharpIntelliSense.EnsureTheCaretIsVisibleAfterALongEdit(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 8");
        await TestServices.Input.SendAsync(
            [
                VirtualKeyCode.DELETE,
                "aaa",
                VirtualKeyCode.TAB,
            ],
            HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.EnsureTheCaretIsVisibleAfterALongEdit(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 9");
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        _output.WriteLine($"CSharpIntelliSense.EnsureTheCaretIsVisibleAfterALongEdit(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 10");
        Assert.Contains($"{variableName} = {variableName}", actualText);
        _output.WriteLine($"CSharpIntelliSense.EnsureTheCaretIsVisibleAfterALongEdit(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 11");
        Assert.True(await TestServices.Editor.IsCaretOnScreenAsync(HangMitigatingCancellationToken));
        _output.WriteLine($"CSharpIntelliSense.EnsureTheCaretIsVisibleAfterALongEdit(showCompletionInArgumentLists: {showCompletionInArgumentLists}): action 12");
        Assert.True(await TestServices.Editor.GetCaretColumnAsync(HangMitigatingCancellationToken) > visibleColumns, "This test is inconclusive if the view didn't need to move to keep the caret on screen.");
    }

    [IdeTheory, CombinatorialData]
    public async Task DismissOnSelect(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync(@"$$", HangMitigatingCancellationToken);

        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        await TestServices.Input.SendAsync((VirtualKeyCode.SPACE, VirtualKeyCode.CONTROL), HangMitigatingCancellationToken);
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        await TestServices.Input.SendAsync((VirtualKeyCode.VK_A, VirtualKeyCode.CONTROL), HangMitigatingCancellationToken);
        Assert.False(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));
    }
}
