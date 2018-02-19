﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicGenerateEqualsAndGetHashCodeDialog : AbstractEditorTest
    {
        private const string DialogName = "PickMembersDialog";

        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicGenerateEqualsAndGetHashCodeDialog(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicGenerateEqualsAndGetHashCodeDialog))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
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
            VisualStudio.Editor.Verify.CodeAction("Generate Equals(object)...", applyFix: true, blockUntilComplete: false);
            VerifyDialog(isOpen: true);
            Dialog_ClickCancel();
            var actualText = VisualStudio.Editor.GetText();
            var expectedText = @"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean


End Class";
            Assert.Contains(expectedText, actualText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
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

            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Generate Equals(object)...", applyFix: true, blockUntilComplete: false);
            VerifyDialog(isOpen: true);
            Dialog_ClickOk();
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.LightBulb);
            var actualText = VisualStudio.Editor.GetText();
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
            Assert.Contains(expectedText, actualText);
        }

        private void VerifyDialog(bool isOpen)
            => VisualStudio.Editor.Verify.Dialog(DialogName, isOpen);

        private void Dialog_ClickCancel()
            => VisualStudio.Editor.PressDialogButton(DialogName, "CancelButton");

        private void Dialog_ClickOk()
            => VisualStudio.Editor.PressDialogButton(DialogName, "OkButton");
    }
}
