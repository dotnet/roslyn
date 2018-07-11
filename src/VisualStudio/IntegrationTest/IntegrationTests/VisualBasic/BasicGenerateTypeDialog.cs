// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicGenerateTypeDialog : AbstractIdeEditorTest
    {
        public BasicGenerateTypeDialog()
            : base(nameof(BasicGenerateTypeDialog))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        private GenerateTypeDialog_InProc2 GenerateTypeDialog => VisualStudio.GenerateTypeDialog;

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task BasicToCSharpAsync()
        {
            await VisualStudio.SolutionExplorer.AddProjectAsync("CSProj", WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp);
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.vb");

            await SetUpEditorAsync(@"
Class C
    Sub Method()
        $$Dim _A As A
    End Sub
End Class
");
            var codeAction = VisualStudio.Editor.Verify.CodeActionAsync("Generate new type...",
                applyFix: true,
                willBlockUntilComplete: false);

            await GenerateTypeDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await GenerateTypeDialog.SetAccessibilityAsync("Public");
            await GenerateTypeDialog.SetKindAsync("Structure");
            await GenerateTypeDialog.SetTargetProjectAsync("CSProj");
            await GenerateTypeDialog.SetTargetFileToNewNameAsync("GenerateTypeTest.cs");
            await GenerateTypeDialog.ClickOkAsync();
            await GenerateTypeDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await codeAction;

            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"Imports CSProj

Class C
    Sub Method()
        Dim _A As A
    End Sub
End Class
", actualText);

            await VisualStudio.SolutionExplorer.OpenFileAsync("CSProj", "GenerateTypeTest.cs");
            actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"namespace CSProj
{
    public struct A
    {
    }
}", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task SameProjectAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Method()
        $$Dim _A As A
    End Sub
End Class
");

            var codeAction = VisualStudio.Editor.Verify.CodeActionAsync("Generate new type...",
                applyFix: true,
                willBlockUntilComplete: false);

            await GenerateTypeDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await GenerateTypeDialog.SetAccessibilityAsync("Public");
            await GenerateTypeDialog.SetKindAsync("Structure");
            await GenerateTypeDialog.SetTargetFileToNewNameAsync("GenerateTypeTest");
            await GenerateTypeDialog.ClickOkAsync();
            await GenerateTypeDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await codeAction;

            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "GenerateTypeTest.vb");
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"Public Structure A
End Structure
", actualText);

            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.vb");
            actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"Class C
    Sub Method()
        Dim _A As A
    End Sub
End Class
", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task CheckFoldersPopulateComboBoxAsync()
        {
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, @"folder1\folder2\GenerateTypeTests.vb", open: true);

            await SetUpEditorAsync(@"Class C
    Sub Method() 
        $$Dim _A As A
    End Sub
End Class
");
            var codeAction = VisualStudio.Editor.Verify.CodeActionAsync("Generate new type...",
                applyFix: true,
                willBlockUntilComplete: false);

            await GenerateTypeDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await GenerateTypeDialog.SetTargetFileToNewNameAsync("Other");

            var folders = await GenerateTypeDialog.GetNewFileComboBoxItemsAsync();

            Assert.Contains(@"\folder1\", folders);
            Assert.Contains(@"\folder1\folder2\", folders);

            await GenerateTypeDialog.ClickCancelAsync();

            await codeAction;
        }
    }
}
