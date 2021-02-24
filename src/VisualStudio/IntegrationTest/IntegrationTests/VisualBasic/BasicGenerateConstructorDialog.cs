// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicGenerateConstructorDialog : AbstractEditorTest
    {
        private const string DialogName = "PickMembersDialog";

        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicGenerateConstructorDialog(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicGenerateConstructorDialog))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public void VerifyCodeRefactoringOfferedAndCanceled()
        {
            SetUpEditor(@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

$$
End Class");

            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Generate constructor...", applyFix: true, blockUntilComplete: false);
            VerifyDialog(isOpen: true);
            Dialog_ClickCancel();
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(
@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean


End Class", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public void VerifyCodeRefactoringOfferedAndAccepted()
        {
            SetUpEditor(
@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

$$
End Class");

            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Generate constructor...", applyFix: true, blockUntilComplete: false);
            VerifyDialog(isOpen: true);
            Dialog_ClickOk();
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.LightBulb);
            var actualText = VisualStudio.Editor.GetText();
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public void VerifyReordering()
        {
            SetUpEditor(
@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

$$
End Class");

            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Generate constructor...", applyFix: true, blockUntilComplete: false);
            VerifyDialog(isOpen: true);
            VisualStudio.Editor.DialogSendKeys(DialogName, VirtualKey.Tab);
            VisualStudio.Editor.PressDialogButton(DialogName, "Down");
            Dialog_ClickOk();
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.LightBulb);
            var actualText = VisualStudio.Editor.GetText();
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public void VerifyDeselect()
        {
            SetUpEditor(
@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

$$
End Class");

            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Generate constructor...", applyFix: true, blockUntilComplete: false);
            VerifyDialog(isOpen: true);
            VisualStudio.Editor.DialogSendKeys(DialogName, VirtualKey.Tab);
            VisualStudio.Editor.DialogSendKeys(DialogName, VirtualKey.Space);
            Dialog_ClickOk();
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.LightBulb);
            var actualText = VisualStudio.Editor.GetText();
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

        private void VerifyDialog(bool isOpen)
            => VisualStudio.Editor.Verify.Dialog(DialogName, isOpen);

        private void Dialog_ClickCancel()
            => VisualStudio.Editor.PressDialogButton(DialogName, "CancelButton");

        private void Dialog_ClickOk()
            => VisualStudio.Editor.PressDialogButton(DialogName, "OkButton");
    }
}
