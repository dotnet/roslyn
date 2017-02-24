// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicChangeSignatureDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicChangeSignatureDialog(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicChangeSignatureDialog))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void VerifyCodeRefactoringOffered()
        {
            SetUpEditor(@"
Class C
    Sub Method$$(a As Integer, b As Integer)
    End Sub
End Class");

            InvokeCodeActionList();
            VerifyCodeAction("Change signature...", applyFix: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void VerifyRefactoringCancelled()
        {
            SetUpEditor(@"
Class C
    Sub Method$$(a As Integer, b As String)
    End Sub
End Class");

            OpenDialog();
            VerifyChangeSignatureDialog(isOpen: true);
            ChangeSignatureDialog_ClickCancel();
            VerifyChangeSignatureDialog(isOpen: false);
        }

        private void OpenDialog()
        {
            Editor.SendKeys(Ctrl(VirtualKey.R), Ctrl(VirtualKey.V));
        }

        private void VerifyChangeSignatureDialog(bool isOpen)
        {
            VerifyDialog("ChangeSignatureDialog", isOpen);
        }

        private void ChangeSignatureDialog_ClickCancel()
        {
            PressDialogButton("ChangeSignatureDialog", "CancelButton");
        }
    }
}
