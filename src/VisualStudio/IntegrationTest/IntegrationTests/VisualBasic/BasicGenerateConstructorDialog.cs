﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
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
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.LightBulb);
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
            VisualStudio.Editor.DialogSendKeys(DialogName, "{TAB}");
            VisualStudio.Editor.PressDialogButton(DialogName, "Down");
            Dialog_ClickOk();
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.LightBulb);
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
            VisualStudio.Editor.DialogSendKeys(DialogName, "{TAB}");
            VisualStudio.Editor.DialogSendKeys(DialogName, " ");
            Dialog_ClickOk();
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.LightBulb);
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
