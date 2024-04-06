// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsExtractInterface)]
    public class CSharpExtractInterfaceDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpExtractInterfaceDialog()
            : base(nameof(CSharpExtractInterfaceDialog))
        {
        }

        [IdeFact]
        public async Task VerifyCancellation()
        {
            await SetUpEditorAsync(@"class C$$
{
    public void M() { }
}
", HangMitigatingCancellationToken);
            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Extract interface...",
                applyFix: true,
                blockUntilComplete: false,
                cancellationToken: HangMitigatingCancellationToken);

            await TestServices.ExtractInterfaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ExtractInterfaceDialog.ClickCancelAsync(HangMitigatingCancellationToken);
            await TestServices.ExtractInterfaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await TestServices.EditorVerifier.TextContainsAsync(@"class C
{
    public void M() { }
}
", cancellationToken: HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task CheckFileName()
        {
            await SetUpEditorAsync(@"class C$$
{
    public void M() { }
}
", HangMitigatingCancellationToken);
            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Extract interface...",
                applyFix: true,
                blockUntilComplete: false,
                cancellationToken: HangMitigatingCancellationToken);

            await TestServices.ExtractInterfaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);

            var targetFileName = await TestServices.ExtractInterfaceDialog.GetTargetFileNameAsync(HangMitigatingCancellationToken);
            Assert.Equal(expected: "IC.cs", actual: targetFileName);

            await TestServices.ExtractInterfaceDialog.ClickCancelAsync(HangMitigatingCancellationToken);
            await TestServices.ExtractInterfaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task VerifySelectAndDeselectAllButtons()
        {
            await SetUpEditorAsync(@"class C$$
{
    public void M1() { }
    public void M2() { }
}
", HangMitigatingCancellationToken);

            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Extract interface...",
                applyFix: true,
                blockUntilComplete: false,
                cancellationToken: HangMitigatingCancellationToken);

            await TestServices.ExtractInterfaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);

            var selectedItems = await TestServices.ExtractInterfaceDialog.GetSelectedItemsAsync(HangMitigatingCancellationToken);
            Assert.Equal(
                expected: new[] { "M1()", "M2()" },
                actual: selectedItems.Select(item => item.SymbolName));

            await TestServices.ExtractInterfaceDialog.ClickDeselectAllAsync(HangMitigatingCancellationToken);

            selectedItems = await TestServices.ExtractInterfaceDialog.GetSelectedItemsAsync(HangMitigatingCancellationToken);
            Assert.Empty(selectedItems);

            await TestServices.ExtractInterfaceDialog.ClickSelectAllAsync(HangMitigatingCancellationToken);

            selectedItems = await TestServices.ExtractInterfaceDialog.GetSelectedItemsAsync(HangMitigatingCancellationToken);
            Assert.Equal(
                expected: new[] { "M1()", "M2()" },
                actual: selectedItems.Select(item => item.SymbolName));

            await TestServices.ExtractInterfaceDialog.ClickCancelAsync(HangMitigatingCancellationToken);
            await TestServices.ExtractInterfaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task OnlySelectedItemsAreGenerated()
        {
            await SetUpEditorAsync(@"class C$$
{
    public void M1() { }
    public void M2() { }
}
", HangMitigatingCancellationToken);

            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Extract interface...",
                applyFix: true,
                blockUntilComplete: false,
                cancellationToken: HangMitigatingCancellationToken);

            await TestServices.ExtractInterfaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ExtractInterfaceDialog.ClickDeselectAllAsync(HangMitigatingCancellationToken);
            await TestServices.ExtractInterfaceDialog.ToggleItemAsync("M2()", HangMitigatingCancellationToken);
            await TestServices.ExtractInterfaceDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.ExtractInterfaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs", HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.TextContainsAsync(@"class C : IC
{
    public void M1() { }
    public void M2() { }
}
", cancellationToken: HangMitigatingCancellationToken);

            await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "IC.cs", HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.TextContainsAsync(@"interface IC
{
    void M2();
}", cancellationToken: HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task CheckSameFile()
        {
            await SetUpEditorAsync(@"class C$$
{
    public void M() { }
}
", HangMitigatingCancellationToken);
            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Extract interface...",
                applyFix: true,
                blockUntilComplete: false,
                cancellationToken: HangMitigatingCancellationToken);

            await TestServices.ExtractInterfaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);

            await TestServices.ExtractInterfaceDialog.SelectSameFileAsync(HangMitigatingCancellationToken);

            await TestServices.ExtractInterfaceDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.ExtractInterfaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await TestServices.EditorVerifier.TextContainsAsync(@"interface IC
{
    void M();
}

class C : IC
{
    public void M() { }
}
", cancellationToken: HangMitigatingCancellationToken);

        }

        [IdeFact]
        public async Task CheckSameFileOnlySelectedItems()
        {
            await SetUpEditorAsync(@"class C$$
{
    public void M1() { }
    public void M2() { }
}
", HangMitigatingCancellationToken);

            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Extract interface...",
                applyFix: true,
                blockUntilComplete: false,
                cancellationToken: HangMitigatingCancellationToken);

            await TestServices.ExtractInterfaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ExtractInterfaceDialog.ClickDeselectAllAsync(HangMitigatingCancellationToken);
            await TestServices.ExtractInterfaceDialog.ToggleItemAsync("M2()", HangMitigatingCancellationToken);
            await TestServices.ExtractInterfaceDialog.SelectSameFileAsync(HangMitigatingCancellationToken);
            await TestServices.ExtractInterfaceDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.ExtractInterfaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await TestServices.EditorVerifier.TextContainsAsync(@"interface IC
{
    void M2();
}

class C : IC
{
    public void M1() { }
    public void M2() { }
}
", cancellationToken: HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task CheckSameFileNamespace()
        {
            await SetUpEditorAsync(@"namespace A
{
    class C$$
    {
        public void M() { }
    }
}
", HangMitigatingCancellationToken);
            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Extract interface...",
                applyFix: true,
                blockUntilComplete: false,
                cancellationToken: HangMitigatingCancellationToken);

            await TestServices.ExtractInterfaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);

            await TestServices.ExtractInterfaceDialog.SelectSameFileAsync(HangMitigatingCancellationToken);

            await TestServices.ExtractInterfaceDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.ExtractInterfaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await TestServices.EditorVerifier.TextContainsAsync(@"namespace A
{
    interface IC
    {
        void M();
    }

    class C : IC
    {
        public void M() { }
    }
}
", cancellationToken: HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task CheckSameWithTypes()
        {
            await SetUpEditorAsync(@"class C$$
{
    public bool M() => false;
}
", HangMitigatingCancellationToken);
            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Extract interface...",
                applyFix: true,
                blockUntilComplete: false,
                cancellationToken: HangMitigatingCancellationToken);

            await TestServices.ExtractInterfaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);

            await TestServices.ExtractInterfaceDialog.SelectSameFileAsync(HangMitigatingCancellationToken);

            await TestServices.ExtractInterfaceDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.ExtractInterfaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await TestServices.EditorVerifier.TextContainsAsync(@"interface IC
{
    bool M();
}

class C : IC
{
    public bool M() => false;
}
", cancellationToken: HangMitigatingCancellationToken);
        }
    }
}
