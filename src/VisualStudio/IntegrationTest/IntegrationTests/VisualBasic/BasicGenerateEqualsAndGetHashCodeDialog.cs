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
    public class BasicGenerateEqualsAndGetHashCodeDialog : AbstractEditorTest
    {
        private const string DialogName = "PickMembersDialog";

        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicGenerateEqualsAndGetHashCodeDialog( )
            : base( nameof(BasicGenerateEqualsAndGetHashCodeDialog))
        {
        }

        [TestMethod, TestCategory(Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
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
            VisualStudioInstance.Editor.Verify.CodeAction("Generate Equals(object)...", applyFix: true, blockUntilComplete: false);
            VerifyDialog(isOpen: true);
            Dialog_ClickCancel();
            var actualText = VisualStudioInstance.Editor.GetText();
            var expectedText = @"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean


End Class";
            ExtendedAssert.Contains(expectedText, actualText);
        }

        [TestMethod, TestCategory(Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public void VerifyCodeRefactoringOfferedAndAccepted()
        {
            SetUpEditor(@"
Imports TestProj

Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

$$
End Class");

            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction("Generate Equals(object)...", applyFix: true, blockUntilComplete: false);
            VerifyDialog(isOpen: true);
            Dialog_ClickOk();
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.LightBulb);
            var actualText = VisualStudioInstance.Editor.GetText();
            var expectedText = @"
Imports TestProj

Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim c = TryCast(obj, C)
        Return c IsNot Nothing AndAlso
               i = c.i AndAlso
               j = c.j AndAlso
               k = c.k
    End Function
End Class";
            ExtendedAssert.Contains(expectedText, actualText);
        }

        private void VerifyDialog(bool isOpen)
            => VisualStudioInstance.Editor.Verify.Dialog(DialogName, isOpen);

        private void Dialog_ClickCancel()
            => VisualStudioInstance.Editor.PressDialogButton(DialogName, "CancelButton");

        private void Dialog_ClickOk()
            => VisualStudioInstance.Editor.PressDialogButton(DialogName, "OkButton");
    }
}
