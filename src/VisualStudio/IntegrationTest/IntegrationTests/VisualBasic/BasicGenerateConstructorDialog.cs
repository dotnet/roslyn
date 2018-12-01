// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicGenerateConstructorDialog : AbstractEditorTest
    {
        private const string DialogName = "PickMembersDialog";

        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicGenerateConstructorDialog( )
            : base(nameof(BasicGenerateConstructorDialog))
        {
        }

        [TestMethod, TestCategory(Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public void VerifyCodeRefactoringOfferedAndCanceled()
        {
            SetUpEditor(@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

$$
End Class");

            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction("Generate constructor...", applyFix: true, blockUntilComplete: false);
            VerifyDialog(isOpen: true);
            Dialog_ClickCancel();
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(
@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean


End Class", actualText);
        }

        [TestMethod, TestCategory(Traits.Features.CodeActionsGenerateConstructorFromMembers)]
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

            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction("Generate constructor...", applyFix: true, blockUntilComplete: false);
            VerifyDialog(isOpen: true);
            Dialog_ClickOk();
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.LightBulb);
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(
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

        [TestMethod, TestCategory(Traits.Features.CodeActionsGenerateConstructorFromMembers)]
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

            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction("Generate constructor...", applyFix: true, blockUntilComplete: false);
            VerifyDialog(isOpen: true);
            VisualStudioInstance.Editor.DialogSendKeys(DialogName, "{TAB}");
            VisualStudioInstance.Editor.PressDialogButton(DialogName, "Down");
            Dialog_ClickOk();
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.LightBulb);
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(
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

        [TestMethod, TestCategory(Traits.Features.CodeActionsGenerateConstructorFromMembers)]
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

            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction("Generate constructor...", applyFix: true, blockUntilComplete: false);
            VerifyDialog(isOpen: true);
            VisualStudioInstance.Editor.DialogSendKeys(DialogName, "{TAB}");
            VisualStudioInstance.Editor.DialogSendKeys(DialogName, " ");
            Dialog_ClickOk();
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.LightBulb);
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(
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
            => VisualStudioInstance.Editor.Verify.Dialog(DialogName, isOpen);

        private void Dialog_ClickCancel()
            => VisualStudioInstance.Editor.PressDialogButton(DialogName, "CancelButton");

        private void Dialog_ClickOk()
            => VisualStudioInstance.Editor.PressDialogButton(DialogName, "OkButton");
    }
}
