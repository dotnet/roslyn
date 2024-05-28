// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
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

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.Completion)]
public class CSharpIntelliSense : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpIntelliSense()
        : base(nameof(CSharpIntelliSense))
    {
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
        await SetUpEditorAsync(@"$$", HangMitigatingCancellationToken);

        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        await TestServices.Input.SendAsync("usi", HangMitigatingCancellationToken);
        Assert.Contains("using", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));

        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("using$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task SpeculativeTInList(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync(@"
class C
{
    $$
}", HangMitigatingCancellationToken);

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
        await TestServices.EditorVerifier.TextContainsAsync(@"
class C
{
    public T Goo<T>() { }$$
}",
assertCaretPosition: true,
HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task VerifyCompletionListMembersOnStaticTypesAndCompleteThem(bool showCompletionInArgumentLists)
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
}", HangMitigatingCancellationToken);

        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        await TestServices.Input.SendAsync('.', HangMitigatingCancellationToken);
        Assert.Contains("Search", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));
        Assert.Contains("Navigate", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));

        await TestServices.Input.SendAsync(['S', VirtualKeyCode.TAB], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        NavigateTo.Search$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task CtrlAltSpace(bool showCompletionInArgumentLists)
    {
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);

        // Note: the completion needs to be unambiguous for the test to be deterministic.
        // Otherwise the result might depend on the state of MRU list.

        await TestServices.Input.SendAsync("names", HangMitigatingCancellationToken);
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        await TestServices.Input.SendAsync([" Goo", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync(['{', VirtualKeyCode.RETURN, '}', VirtualKeyCode.UP, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("pu", HangMitigatingCancellationToken);
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        await TestServices.Input.SendAsync(" cla", HangMitigatingCancellationToken);
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        await TestServices.Input.SendAsync([" Program", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync(['{', VirtualKeyCode.RETURN, '}', VirtualKeyCode.UP, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("pub", HangMitigatingCancellationToken);
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        await TestServices.Input.SendAsync(" stati", HangMitigatingCancellationToken);
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        await TestServices.Input.SendAsync(" voi", HangMitigatingCancellationToken);
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        await TestServices.Input.SendAsync([" Main(string[] args)", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync(['{', VirtualKeyCode.RETURN, '}', VirtualKeyCode.UP, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("System.Console.", HangMitigatingCancellationToken);
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        await TestServices.Input.SendAsync("writeline();", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("            System.Console.WriteLine();$$", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync([VirtualKeyCode.HOME, (VirtualKeyCode.END, VirtualKeyCode.SHIFT), VirtualKeyCode.DELETE], HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync(new InputKey(VirtualKeyCode.SPACE, ImmutableArray.Create(VirtualKeyCode.CONTROL, VirtualKeyCode.MENU)), HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("System.Console.", HangMitigatingCancellationToken);
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        await TestServices.Input.SendAsync("writeline();", HangMitigatingCancellationToken);
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
        await SetUpEditorAsync(@"
class Class1
{
    ///$$
    void Main(string[] args)
    {
    
    }
}", HangMitigatingCancellationToken);

        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        await TestServices.Input.SendAsync("<s", HangMitigatingCancellationToken);
        Assert.Contains("see", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));
        Assert.Contains("seealso", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));
        Assert.Contains("summary", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));

        // 🐛 Workaround for https://github.com/dotnet/roslyn/issues/33824
        var completionItems = (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).SelectAsArray(item => item.DisplayText);
        var targetIndex = completionItems.IndexOf("see");
        var currentIndex = completionItems.IndexOf((await TestServices.Editor.GetCurrentCompletionItemAsync(HangMitigatingCancellationToken)).DisplayText);
        if (currentIndex != targetIndex)
        {
            InputKey key = currentIndex < targetIndex ? VirtualKeyCode.DOWN : VirtualKeyCode.UP;
            var keys = Enumerable.Repeat(key, Math.Abs(currentIndex - targetIndex)).ToArray();
            await TestServices.Input.SendAsync(keys, HangMitigatingCancellationToken);
        }

        await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    ///<see cref=\"$$\"/>", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task XmlTagCompletion(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync(@"
/// $$
class C { }
", HangMitigatingCancellationToken);

        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        await TestServices.Input.SendAsync("<summary>", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("/// <summary>$$</summary>", assertCaretPosition: true, HangMitigatingCancellationToken);

        await SetUpEditorAsync(@"
/// <summary>$$
class C { }
", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("</", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("/// <summary></summary>$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task SignatureHelpShowsUp(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync(@"
class Class1
{
    void Main(string[] args)
    {
        $$
    }
}", HangMitigatingCancellationToken);

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
        await SetUpEditorAsync(@"
class Class1
{
    void Main(string[] args)
    $$
}", HangMitigatingCancellationToken);

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

        await TestServices.EditorVerifier.TextContainsAsync(@"
class Class1
{
    void Main(string[] args)
    {
    }$$
}",
assertCaretPosition: true,
HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/33823")]
    public async Task CommitOnShiftEnter(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync(@"
class Class1
{
    void Main(string[] args)
    {
        $$
    }
}", HangMitigatingCancellationToken);

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

        await TestServices.EditorVerifier.TextContainsAsync(@"
class Class1
{
    void Main(string[] args)
    {
        Main
$$
    }
}",
assertCaretPosition: true,
HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task LineBreakOnShiftEnter(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync(@"
class Class1
{
    void Main(string[] args)
    {
        $$
    }
}", HangMitigatingCancellationToken);

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

        await TestServices.EditorVerifier.TextContainsAsync(@"
class Class1
{
    void Main(string[] args)
    {
        Main
$$
    }
}",
assertCaretPosition: true,
HangMitigatingCancellationToken);

    }

    [IdeTheory, CombinatorialData]
    public async Task CommitOnLeftCurly(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync(@"
class Class1
{
    $$
}", HangMitigatingCancellationToken);

        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("int P { g", HangMitigatingCancellationToken);
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        await TestServices.Input.SendAsync("{", HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync(@"
class Class1
{
    int P { get { $$} }
}",
assertCaretPosition: true,
HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/33822")]
    public async Task EnsureTheCaretIsVisibleAfterALongEdit(bool showCompletionInArgumentLists)
    {
        var visibleColumns = await TestServices.Editor.GetVisibleColumnCountAsync(HangMitigatingCancellationToken);
        var variableName = new string('a', (int)(0.75 * visibleColumns));
        await SetUpEditorAsync($@"
public class Program
{{
    static void Main(string[] args)
    {{
        var {variableName} = 0;
        {variableName} = $$
    }}
}}", HangMitigatingCancellationToken);

        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists);
        globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.VisualBasic, showCompletionInArgumentLists);

        Assert.True(variableName.Length > 0);
        await TestServices.Input.SendAsync(
            [
                VirtualKeyCode.DELETE,
                "aaa",
                VirtualKeyCode.TAB,
            ],
            HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains($"{variableName} = {variableName}", actualText);
        Assert.True(await TestServices.Editor.IsCaretOnScreenAsync(HangMitigatingCancellationToken));
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
