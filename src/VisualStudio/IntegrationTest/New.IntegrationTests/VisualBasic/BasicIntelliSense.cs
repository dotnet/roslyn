// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class BasicIntelliSense : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicIntelliSense()
            : base(nameof(BasicIntelliSense))
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

        [IdeFact]
        public async Task IntelliSenseTriggersOnParenWithBraceCompletionAndCorrectUndoMerging()
        {
            await SetUpEditorAsync(@"
Module Module1
    Sub Main()
        $$
    End Sub
End Module", HangMitigatingCancellationToken);

            await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync("dim q as lis(", HangMitigatingCancellationToken);
            Assert.Contains("Of", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));

            await TestServices.EditorVerifier.TextContainsAsync(@"
Module Module1
    Sub Main()
        Dim q As List($$)
    End Sub
End Module",
assertCaretPosition: true,
HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(
                [
                    VirtualKeyCode.DOWN,
                    VirtualKeyCode.TAB,
                ],
                HangMitigatingCancellationToken);

            await TestServices.EditorVerifier.TextContainsAsync(@"
Module Module1
    Sub Main()
        Dim q As List(Of$$)
    End Sub
End Module",
assertCaretPosition: true,
HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(" inte", HangMitigatingCancellationToken);
            Assert.Contains("Integer", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));

            await TestServices.Input.SendAsync(')', HangMitigatingCancellationToken);

            await TestServices.EditorVerifier.TextContainsAsync(@"
Module Module1
    Sub Main()
        Dim q As List(Of Integer)$$
    End Sub
End Module",
assertCaretPosition: true,
HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync((VirtualKeyCode.VK_Z, VirtualKeyCode.CONTROL), HangMitigatingCancellationToken);

            await TestServices.EditorVerifier.TextContainsAsync(@"
Module Module1
    Sub Main()
        Dim q As List(Of inte)$$
    End Sub
End Module",
assertCaretPosition: true,
HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync((VirtualKeyCode.VK_Z, VirtualKeyCode.CONTROL), HangMitigatingCancellationToken);

            await TestServices.EditorVerifier.TextContainsAsync(@"
Module Module1
    Sub Main()
        Dim q As List(Of inte$$)
    End Sub
End Module",
assertCaretPosition: true,
HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync((VirtualKeyCode.VK_Z, VirtualKeyCode.CONTROL), HangMitigatingCancellationToken);

            await TestServices.EditorVerifier.TextContainsAsync(@"
Module Module1
    Sub Main()
        Dim q As List(Of$$)
    End Sub
End Module",
assertCaretPosition: true,
HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync((VirtualKeyCode.VK_Z, VirtualKeyCode.CONTROL), HangMitigatingCancellationToken);

            await TestServices.EditorVerifier.TextContainsAsync(@"
Module Module1
    Sub Main()
        Dim q As List($$)
    End Sub
End Module",
assertCaretPosition: true,
HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync((VirtualKeyCode.VK_Z, VirtualKeyCode.CONTROL), HangMitigatingCancellationToken);

            await TestServices.EditorVerifier.TextContainsAsync(@"
Module Module1
    Sub Main()
        Dim q As lis($$)
    End Sub
End Module",
assertCaretPosition: true,
HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync((VirtualKeyCode.VK_Z, VirtualKeyCode.CONTROL), HangMitigatingCancellationToken);

            await TestServices.EditorVerifier.TextContainsAsync(@"
Module Module1
    Sub Main()
        Dim q As lis($$
    End Sub
End Module",
assertCaretPosition: true,
HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task TypeAVariableDeclaration()
        {
            await SetUpEditorAsync(@"
Module Module1
    Sub Main()
        $$
    End Sub
End Module", HangMitigatingCancellationToken);

            await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync("dim", HangMitigatingCancellationToken);
            Assert.Contains("Dim", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));
            Assert.Contains("ReDim", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));

            await TestServices.Input.SendAsync(' ', HangMitigatingCancellationToken);
            Assert.False(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

            await TestServices.Input.SendAsync('i', HangMitigatingCancellationToken);
            Assert.False(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

            await TestServices.Input.SendAsync(' ', HangMitigatingCancellationToken);
            Assert.Contains("As", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));

            await TestServices.Input.SendAsync("a ", HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync("intege", HangMitigatingCancellationToken);
            Assert.Contains("Integer", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));
            Assert.Contains("UInteger", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));

            await TestServices.Input.SendAsync(' ', HangMitigatingCancellationToken);
            Assert.False(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

            await TestServices.Input.SendAsync('=', HangMitigatingCancellationToken);
            Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

            await TestServices.Input.SendAsync(' ', HangMitigatingCancellationToken);
            Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

            await TestServices.Input.SendAsync("fooo", HangMitigatingCancellationToken);
            Assert.False(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

            await TestServices.Input.SendAsync(' ', HangMitigatingCancellationToken);
            Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

            await TestServices.Input.SendAsync(VirtualKeyCode.BACK, HangMitigatingCancellationToken);
            Assert.False(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

            await TestServices.Input.SendAsync(VirtualKeyCode.BACK, HangMitigatingCancellationToken);
            Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

            await TestServices.Input.SendAsync(
                [
                    VirtualKeyCode.LEFT,
                    VirtualKeyCode.DELETE,
                ],
                HangMitigatingCancellationToken);
            Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task DismissIntelliSenseOnApostrophe()
        {
            await SetUpEditorAsync(@"
Module Module1
    Sub Main()
        $$
    End Sub
End Module", HangMitigatingCancellationToken);

            await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync("dim q as ", HangMitigatingCancellationToken);
            Assert.Contains("_AppDomain", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));

            await TestServices.Input.SendAsync("'", HangMitigatingCancellationToken);
            Assert.False(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));
            var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
            Assert.Contains(@"Module Module1
    Sub Main()
        Dim q As '
    End Sub
End Module", actualText);
        }

        [IdeFact]
        public async Task TypeLeftAngleAfterImports()
        {
            await SetUpEditorAsync(@"
Imports$$", HangMitigatingCancellationToken);

            await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(' ', HangMitigatingCancellationToken);
            Assert.Contains("Microsoft", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));
            Assert.Contains("System", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));

            await TestServices.Input.SendAsync('<', HangMitigatingCancellationToken);
            Assert.False(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task DismissAndRetriggerIntelliSenseOnEquals()
        {
            await SetUpEditorAsync(@"
Module Module1
    Function M(val As Integer) As Integer
        $$
    End Function
End Module", HangMitigatingCancellationToken);

            await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync('M', HangMitigatingCancellationToken);
            Assert.Contains("M", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));

            await TestServices.Input.SendAsync("=v", HangMitigatingCancellationToken);
            Assert.Contains("val", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));

            await TestServices.Input.SendAsync(' ', HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.TextContainsAsync(@"
Module Module1
    Function M(val As Integer) As Integer
        M=val $$
    End Function
End Module",
assertCaretPosition: true,
HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task CtrlAltSpace()
        {
            await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync("Nam Foo", HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("Namespace Foo$$", assertCaretPosition: true, HangMitigatingCancellationToken);

            await ClearEditorAsync(HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(new InputKey(VirtualKeyCode.SPACE, ImmutableArray.Create(VirtualKeyCode.CONTROL, VirtualKeyCode.MENU)), HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync("Nam Foo", HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("Nam Foo$$", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task CtrlAltSpaceOption()
        {
            await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync("Nam Foo", HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("Namespace Foo$$", assertCaretPosition: true, HangMitigatingCancellationToken);

            await ClearEditorAsync(HangMitigatingCancellationToken);

            await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.ToggleCompletionMode, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync("Nam Foo", HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("Nam Foo$$", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task EnterTriggerCompletionListAndImplementInterface()
        {
            await SetUpEditorAsync(@"
Interface UFoo
    Sub FooBar()
End Interface

Public Class Bar
    Implements$$

End Class", HangMitigatingCancellationToken);

            await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(" UF", HangMitigatingCancellationToken);
            Assert.Contains("UFoo", (await TestServices.Editor.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(completion => completion.DisplayText));

            await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
            Assert.False(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));
            var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
            Assert.Contains(@"
Interface UFoo
    Sub FooBar()
End Interface

Public Class Bar
    Implements UFoo

    Public Sub FooBar() Implements UFoo.FooBar
        Throw New NotImplementedException()
    End Sub
End Class", actualText);
        }
    }
}
