// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicIntelliSense : AbstractIdeEditorTest
    {
        public BasicIntelliSense()
            : base(nameof(BasicIntelliSense))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IntelliSenseTriggersOnParenWithBraceCompletionAndCorrectUndoMergingAsync()
        {
            await SetUpEditorAsync(@"
Module Module1
    Sub Main()
        $$
    End Sub
End Module");

            await VisualStudio.Workspace.SetUseSuggestionModeAsync(false);

            await VisualStudio.SendKeys.SendAsync("dim q as lis(");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("Of");

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Module Module1
    Sub Main()
        Dim q As List($$)
    End Sub
End Module",
assertCaretPosition: true);

            await VisualStudio.SendKeys.SendAsync(
                VirtualKey.Down,
                VirtualKey.Tab);

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Module Module1
    Sub Main()
        Dim q As List(Of$$)
    End Sub
End Module",
assertCaretPosition: true);

            await VisualStudio.SendKeys.SendAsync(" inte");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("Integer");

            await VisualStudio.SendKeys.SendAsync(')');

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Module Module1
    Sub Main()
        Dim q As List(Of Integer)$$
    End Sub
End Module",
assertCaretPosition: true);

            await VisualStudio.SendKeys.SendAsync(Ctrl(VirtualKey.Z));

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Module Module1
    Sub Main()
        Dim q As List(Of inte)$$
    End Sub
End Module",
assertCaretPosition: true);

            await VisualStudio.SendKeys.SendAsync(Ctrl(VirtualKey.Z));

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Module Module1
    Sub Main()
        Dim q As List(Of inte$$)
    End Sub
End Module",
assertCaretPosition: true);

            await VisualStudio.SendKeys.SendAsync(Ctrl(VirtualKey.Z));

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Module Module1
    Sub Main()
        Dim q As List(Of$$)
    End Sub
End Module",
assertCaretPosition: true);

            await VisualStudio.SendKeys.SendAsync(Ctrl(VirtualKey.Z));

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Module Module1
    Sub Main()
        Dim q As lis($$)
    End Sub
End Module",
assertCaretPosition: true);

            await VisualStudio.SendKeys.SendAsync(Ctrl(VirtualKey.Z));

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Module Module1
    Sub Main()
        Dim q As lis($$
    End Sub
End Module",
assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeAVariableDeclarationAsync()
        {
            await SetUpEditorAsync(@"
Module Module1
    Sub Main()
        $$
    End Sub
End Module");

            await VisualStudio.Workspace.SetUseSuggestionModeAsync(false);

            await VisualStudio.SendKeys.SendAsync("dim");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("Dim", "ReDim");

            await VisualStudio.SendKeys.SendAsync(' ');
            Assert.Equal(false, await VisualStudio.Editor.IsCompletionActiveAsync());

            await VisualStudio.SendKeys.SendAsync('i');
            Assert.Equal(false, await VisualStudio.Editor.IsCompletionActiveAsync());

            await VisualStudio.SendKeys.SendAsync(' ');
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("As");

            await VisualStudio.SendKeys.SendAsync("a ");
            await VisualStudio.SendKeys.SendAsync("intege");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("Integer", "UInteger");

            await VisualStudio.SendKeys.SendAsync(' ');
            Assert.Equal(false, await VisualStudio.Editor.IsCompletionActiveAsync());

            await VisualStudio.SendKeys.SendAsync('=');
            Assert.Equal(true, await VisualStudio.Editor.IsCompletionActiveAsync());

            await VisualStudio.SendKeys.SendAsync(' ');
            Assert.Equal(true, await VisualStudio.Editor.IsCompletionActiveAsync());

            await VisualStudio.SendKeys.SendAsync("fooo");
            Assert.Equal(false, await VisualStudio.Editor.IsCompletionActiveAsync());

            await VisualStudio.SendKeys.SendAsync(' ');
            Assert.Equal(true, await VisualStudio.Editor.IsCompletionActiveAsync());

            await VisualStudio.SendKeys.SendAsync(VirtualKey.Backspace);
            Assert.Equal(false, await VisualStudio.Editor.IsCompletionActiveAsync());

            await VisualStudio.SendKeys.SendAsync(VirtualKey.Backspace);
            Assert.Equal(true, await VisualStudio.Editor.IsCompletionActiveAsync());

            await VisualStudio.SendKeys.SendAsync(
                VirtualKey.Left,
                VirtualKey.Delete);
            Assert.Equal(true, await VisualStudio.Editor.IsCompletionActiveAsync());
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DismissIntelliSenseOnApostropheAsync()
        {
            await SetUpEditorAsync(@"
Module Module1
    Sub Main()
        $$
    End Sub
End Module");

            await VisualStudio.Workspace.SetUseSuggestionModeAsync(false);

            await VisualStudio.SendKeys.SendAsync("dim q as ");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("_AppDomain");

            await VisualStudio.SendKeys.SendAsync("'");
            Assert.Equal(false, await VisualStudio.Editor.IsCompletionActiveAsync());
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"Module Module1
    Sub Main()
        Dim q As '
    End Sub
End Module", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeLeftAngleAfterImportsAsync()
        {
            await SetUpEditorAsync(@"
Imports$$");

            await VisualStudio.Workspace.SetUseSuggestionModeAsync(false);

            await VisualStudio.SendKeys.SendAsync(' ');
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("Microsoft", "System");

            await VisualStudio.SendKeys.SendAsync('<');
            Assert.Equal(false, await VisualStudio.Editor.IsCompletionActiveAsync());
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DismissAndRetriggerIntelliSenseOnEqualsAsync()
        {
            await SetUpEditorAsync(@"
Module Module1
    Function M(val As Integer) As Integer
        $$
    End Function
End Module");

            await VisualStudio.Workspace.SetUseSuggestionModeAsync(false);

            await VisualStudio.SendKeys.SendAsync('M');
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("M");

            await VisualStudio.SendKeys.SendAsync("=v");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("val");

            await VisualStudio.SendKeys.SendAsync(' ');
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Module Module1
    Function M(val As Integer) As Integer
        M=val $$
    End Function
End Module",
assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CtrlAltSpaceOptionAsync()
        {
            await VisualStudio.Workspace.SetUseSuggestionModeAsync(false);

            await VisualStudio.SendKeys.SendAsync("Nam Foo");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Namespace Foo$$", assertCaretPosition: true);

            await ClearEditorAsync();

            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_ToggleCompletionMode);

            await VisualStudio.SendKeys.SendAsync("Nam Foo");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Nam Foo$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EnterTriggerCompletionListAndImplementInterfaceAsync()
        {
            await SetUpEditorAsync(@"
Interface UFoo
    Sub FooBar()
End Interface

Public Class Bar
    Implements$$

End Class");

            await VisualStudio.Workspace.SetUseSuggestionModeAsync(false);

            await VisualStudio.SendKeys.SendAsync(" UF");
            await VisualStudio.Editor.Verify.CompletionItemsExistAsync("UFoo");

            await VisualStudio.SendKeys.SendAsync(VirtualKey.Enter);
            Assert.Equal(false, await VisualStudio.Editor.IsCompletionActiveAsync());
            var actualText = await VisualStudio.Editor.GetTextAsync();
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
