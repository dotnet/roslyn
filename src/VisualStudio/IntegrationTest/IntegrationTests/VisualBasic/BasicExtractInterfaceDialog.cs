// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicExtractInterfaceDialog : AbstractIdeEditorTest
    {
        public BasicExtractInterfaceDialog()
            : base(nameof(BasicExtractInterfaceDialog))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        private ExtractInterfaceDialog_InProc2 ExtractInterfaceDialog => TestServices.ExtractInterfaceDialog;

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractInterface)]
        public async Task CoreScenarioAsync()
        {
            await SetUpEditorAsync(@"Class C$$
    Public Sub M()
    End Sub
End Class");

            await VisualStudio.Editor.InvokeCodeActionListAsync();
            var codeAction = VisualStudio.Editor.Verify.CodeActionAsync("Extract Interface...",
                applyFix: true,
                willBlockUntilComplete: false);

            await ExtractInterfaceDialog.VerifyOpenAsync();
            await ExtractInterfaceDialog.ClickOkAsync();
            await ExtractInterfaceDialog.VerifyClosedAsync();

            await codeAction;

            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.vb");

            await VisualStudio.Editor.Verify.TextContainsAsync(@"Class C
    Implements IC
    Public Sub M() Implements IC.M
    End Sub
End Class");

            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "IC.vb");

            await VisualStudio.Editor.Verify.TextContainsAsync(@"Interface IC
    Sub M()
End Interface");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractInterface)]
        public async Task CheckFileNameAsync()
        {
            await SetUpEditorAsync(@"Class C2$$
    Public Sub M()
    End Sub
End Class");

            await VisualStudio.Editor.InvokeCodeActionListAsync();
            var codeAction = VisualStudio.Editor.Verify.CodeActionAsync("Extract Interface...",
                applyFix: true,
                willBlockUntilComplete: false);

            await ExtractInterfaceDialog.VerifyOpenAsync();

            var fileName = await ExtractInterfaceDialog.GetTargetFileNameAsync();

            Assert.Equal(expected: "IC2.vb", actual: fileName);

            await ExtractInterfaceDialog.ClickCancelAsync();

            await codeAction;
        }
    }
}
