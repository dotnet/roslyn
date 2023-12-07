// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Extensibility.Testing;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
    public class BasicGenerateConstructorDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicGenerateConstructorDialog()
            : base(nameof(BasicGenerateConstructorDialog))
        {
        }

        [IdeFact]
        public async Task VerifyCodeRefactoringOfferedAndCanceled()
        {
            await SetUpEditorAsync(@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

$$
End Class", HangMitigatingCancellationToken);

            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Generate constructor...", applyFix: true, blockUntilComplete: false, cancellationToken: HangMitigatingCancellationToken);
            await TestServices.PickMembersDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.PickMembersDialog.ClickCancelAsync(HangMitigatingCancellationToken);
            var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
            Assert.Contains(
@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean


End Class", actualText);
        }

        [IdeFact]
        public async Task VerifyCodeRefactoringOfferedAndAccepted()
        {
            await SetUpEditorAsync(
@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

$$
End Class", HangMitigatingCancellationToken);

            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Generate constructor...", applyFix: true, blockUntilComplete: false, cancellationToken: HangMitigatingCancellationToken);
            await TestServices.PickMembersDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.PickMembersDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LightBulb, HangMitigatingCancellationToken);
            var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
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

        [IdeFact]
        public async Task VerifyReordering()
        {
            await SetUpEditorAsync(
@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

$$
End Class", HangMitigatingCancellationToken);

            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Generate constructor...", applyFix: true, blockUntilComplete: false, cancellationToken: HangMitigatingCancellationToken);
            await TestServices.PickMembersDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.PickMembersDialog.ClickDownAsync(HangMitigatingCancellationToken);
            await TestServices.PickMembersDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LightBulb, HangMitigatingCancellationToken);
            var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
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

        [IdeFact]
        public async Task VerifyDeselect()
        {
            await SetUpEditorAsync(
@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

$$
End Class", HangMitigatingCancellationToken);

            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Generate constructor...", applyFix: true, blockUntilComplete: false, cancellationToken: HangMitigatingCancellationToken);
            await TestServices.PickMembersDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.SPACE, HangMitigatingCancellationToken);
            await TestServices.PickMembersDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LightBulb, HangMitigatingCancellationToken);
            var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
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
