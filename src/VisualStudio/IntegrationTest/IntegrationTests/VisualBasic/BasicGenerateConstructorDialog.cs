// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public void VerifyCodeRefactoringOfferedAndCanceled()
        {
            SetUpEditor(@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

$$
End Class");

            InvokeCodeActionList();
            VerifyCodeAction("Generate constructor...", applyFix: true, blockUntilComplete: false);
            VerifyDialog(isOpen: true);
            Dialog_ClickCancel();
            VerifyTextContains(
@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean


End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
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

            InvokeCodeActionList();
            VerifyCodeAction("Generate constructor...", applyFix: true, blockUntilComplete: false);
            VerifyDialog(isOpen: true);
            Dialog_ClickOk();
            WaitForAsyncOperations(FeatureAttribute.LightBulb);
            VerifyTextContains(
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
End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
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

            InvokeCodeActionList();
            VerifyCodeAction("Generate constructor...", applyFix: true, blockUntilComplete: false);
            VerifyDialog(isOpen: true);
            Editor.DialogSendKeys(DialogName, "{TAB}");
            PressDialogButton(DialogName, "Down");
            Dialog_ClickOk();
            WaitForAsyncOperations(FeatureAttribute.LightBulb);
            VerifyTextContains(
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
End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
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

            InvokeCodeActionList();
            VerifyCodeAction("Generate constructor...", applyFix: true, blockUntilComplete: false);
            VerifyDialog(isOpen: true);
            Editor.DialogSendKeys(DialogName, "{TAB}");
            Editor.DialogSendKeys(DialogName, " ");
            Dialog_ClickOk();
            WaitForAsyncOperations(FeatureAttribute.LightBulb);
            VerifyTextContains(
@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

    Public Sub New(j As String, k As Boolean)
        Me.j = j
        Me.k = k
    End Sub
End Class");
        }

        private void VerifyDialog(bool isOpen)
            => VerifyDialog(DialogName, isOpen);

        private void Dialog_ClickCancel()
            => PressDialogButton(DialogName, "CancelButton");

        private void Dialog_ClickOk()
            => PressDialogButton(DialogName, "OkButton");
    }
}