// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicExtractInterfaceDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        private ExtractInterfaceDialog_OutOfProc ExtractInterfaceDialog => VisualStudio.Instance.ExtractInterfaceDialog;

        public BasicExtractInterfaceDialog(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicExtractInterfaceDialog))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractInterface)]
        public void CoreScenario()
        {
            SetUpEditor(@"Class C$$
    Public Sub M()
    End Sub
End Class");

            InvokeCodeActionList();
            VerifyCodeAction("Extract Interface...",
                applyFix: true,
                blockUntilComplete: false);

            ExtractInterfaceDialog.VerifyOpen();
            ExtractInterfaceDialog.ClickOK();
            ExtractInterfaceDialog.VerifyClosed();

            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "Class1.vb");

            VerifyTextContains(@"Class C
    Implements IC
    Public Sub M() Implements IC.M
    End Sub
End Class");

            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "IC.vb");

            VerifyTextContains(@"Interface IC
    Sub M()
End Interface");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractInterface)]
        public void CheckFileName()
        {
            SetUpEditor(@"Class C2$$
    Public Sub M()
    End Sub
End Class");

            InvokeCodeActionList();
            VerifyCodeAction("Extract Interface...",
                applyFix: true,
                blockUntilComplete: false);

            ExtractInterfaceDialog.VerifyOpen();

            var fileName = ExtractInterfaceDialog.GetTargetFileName();

            Assert.Equal(expected: "IC2.vb", actual: fileName);

            ExtractInterfaceDialog.ClickCancel();
        }
    }
}
