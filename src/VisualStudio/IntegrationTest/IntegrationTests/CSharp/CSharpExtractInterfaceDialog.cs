// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpExtractInterfaceDialog : AbstractIdeEditorTest
    {
        public CSharpExtractInterfaceDialog()
            : base(nameof(CSharpExtractInterfaceDialog))
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        private ExtractInterfaceDialog_InProc2 ExtractInterfaceDialog => VisualStudio.ExtractInterfaceDialog;

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractInterface)]
        public async Task VerifyCancellationAsync()
        {
            await SetUpEditorAsync(@"class C$$
{
    public void M() { }
}
");
            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            var codeAction = VisualStudio.Editor.Verify.CodeActionAsync("Extract Interface...",
                applyFix: true,
                willBlockUntilComplete: false,
                cancellationToken: HangMitigatingCancellationToken);

            await ExtractInterfaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await ExtractInterfaceDialog.ClickCancelAsync();
            await ExtractInterfaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await codeAction;

            await VisualStudio.Editor.Verify.TextContainsAsync(@"class C
{
    public void M() { }
}
");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractInterface)]
        public async Task CheckFileNameAsync()
        {
            await SetUpEditorAsync(@"class C$$
{
    public void M() { }
}
");
            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            var codeAction = VisualStudio.Editor.Verify.CodeActionAsync("Extract Interface...",
                applyFix: true,
                willBlockUntilComplete: false,
                cancellationToken: HangMitigatingCancellationToken);

            await ExtractInterfaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);

            var targetFileName = await ExtractInterfaceDialog.GetTargetFileNameAsync();
            Assert.Equal(expected: "IC.cs", actual: targetFileName);

            await ExtractInterfaceDialog.ClickCancelAsync();
            await ExtractInterfaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await codeAction;
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractInterface)]
        public async Task VerifySelectAndDeselectAllButtonsAsync()
        {
            await SetUpEditorAsync(@"class C$$
{
    public void M1() { }
    public void M2() { }
}
");

            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            var codeAction = VisualStudio.Editor.Verify.CodeActionAsync("Extract Interface...",
                applyFix: true,
                willBlockUntilComplete: false,
                cancellationToken: HangMitigatingCancellationToken);

            await ExtractInterfaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);

            var selectedItems = await ExtractInterfaceDialog.GetSelectedItemsAsync();
            Assert.Equal(
                expected: new[] { "M1()", "M2()" },
                actual: selectedItems);

            await ExtractInterfaceDialog.ClickDeselectAllAsync();

            selectedItems = await ExtractInterfaceDialog.GetSelectedItemsAsync();
            Assert.Empty(selectedItems);

            await ExtractInterfaceDialog.ClickSelectAllAsync();

            selectedItems = await ExtractInterfaceDialog.GetSelectedItemsAsync();
            Assert.Equal(
                expected: new[] { "M1()", "M2()" },
                actual: selectedItems);

            await ExtractInterfaceDialog.ClickCancelAsync();
            await ExtractInterfaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await codeAction;
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractInterface)]
        public async Task OnlySelectedItemsAreGeneratedAsync()
        {
            await SetUpEditorAsync(@"class C$$
{
    public void M1() { }
    public void M2() { }
}
");

            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            var codeAction = VisualStudio.Editor.Verify.CodeActionAsync("Extract Interface...",
                applyFix: true,
                willBlockUntilComplete: false,
                cancellationToken: HangMitigatingCancellationToken);

            await ExtractInterfaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await ExtractInterfaceDialog.ClickDeselectAllAsync();
            await ExtractInterfaceDialog.ToggleItemAsync("M2()");
            await ExtractInterfaceDialog.ClickOkAsync();
            await ExtractInterfaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await codeAction;

            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs");
            await VisualStudio.Editor.Verify.TextContainsAsync(@"class C : IC
{
    public void M1() { }
    public void M2() { }
}
");

            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "IC.cs");
            await VisualStudio.Editor.Verify.TextContainsAsync(@"interface IC
{
    void M2();
}");
        }
    }
}
