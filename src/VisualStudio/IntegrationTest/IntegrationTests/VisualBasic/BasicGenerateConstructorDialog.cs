// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicGenerateConstructorDialog : AbstractIdeEditorTest
    {
        public BasicGenerateConstructorDialog()
            : base(nameof(BasicGenerateConstructorDialog))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task VerifyCodeRefactoringOfferedAndCanceledAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

$$
End Class");

            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            var codeAction = VisualStudio.Editor.Verify.CodeActionAsync("Generate constructor...", applyFix: true, willBlockUntilComplete: false, cancellationToken: HangMitigatingCancellationToken);
            await VisualStudio.PickMembersDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await VisualStudio.PickMembersDialog.ClickCancelAsync();

            await codeAction;

            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(
@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean


End Class", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task VerifyCodeRefactoringOfferedAndAcceptedAsync()
        {
            await SetUpEditorAsync(
@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

$$
End Class");

            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            var codeAction = VisualStudio.Editor.Verify.CodeActionAsync("Generate constructor...", applyFix: true, willBlockUntilComplete: false, cancellationToken: HangMitigatingCancellationToken);
            await VisualStudio.PickMembersDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await VisualStudio.PickMembersDialog.ClickOkAsync();

            await codeAction;

            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(
@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

    Public Sub New(i As Integer, j As String, k As Boolean)
        Me.i = i
        Me.j = j
        Me.k = k
    End Sub
End Class", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task VerifyReorderingAsync()
        {
            await SetUpEditorAsync(
@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

$$
End Class");

            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            var codeAction = VisualStudio.Editor.Verify.CodeActionAsync("Generate constructor...", applyFix: true, willBlockUntilComplete: false, cancellationToken: HangMitigatingCancellationToken);
            var dialog = await VisualStudio.PickMembersDialog.VerifyOpenAsync(HangMitigatingCancellationToken);

            var peer = new ListViewAutomationPeer(dialog.GetTestAccessor().Members);
            var firstItem = peer.GetChildren().OfType<ISelectionItemProvider>().First();
            firstItem.Select();

            // Wait for changes to propagate
            await Task.Yield();

            await VisualStudio.PickMembersDialog.ClickDownAsync();
            await VisualStudio.PickMembersDialog.ClickOkAsync();

            await codeAction;

            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(
@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

    Public Sub New(j As String, i As Integer, k As Boolean)
        Me.j = j
        Me.i = i
        Me.k = k
    End Sub
End Class", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task VerifyDeselectAsync()
        {
            await SetUpEditorAsync(
@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

$$
End Class");

            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            var codeAction = VisualStudio.Editor.Verify.CodeActionAsync("Generate constructor...", applyFix: true, willBlockUntilComplete: false, cancellationToken: HangMitigatingCancellationToken);
            var dialog = await VisualStudio.PickMembersDialog.VerifyOpenAsync(HangMitigatingCancellationToken);

            var peer = new ListViewAutomationPeer(dialog.GetTestAccessor().Members);
            var firstItem = peer.GetChildren().Where(child => child is ISelectionItemProvider).First();
            var firstItemToggle = firstItem.GetChildren().OfType<IToggleProvider>().First();
            firstItemToggle.Toggle();

            // Wait for changes to propagate
            await Task.Yield();

            Assert.Equal(ToggleState.Off, (ToggleState)firstItemToggle.ToggleState);

            await VisualStudio.PickMembersDialog.ClickOkAsync();

            await codeAction;

            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(
@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

    Public Sub New(j As String, k As Boolean)
        Me.j = j
        Me.k = k
    End Sub
End Class", actualText);
        }
    }
}
