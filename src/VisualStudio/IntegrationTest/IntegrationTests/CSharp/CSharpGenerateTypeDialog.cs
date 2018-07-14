// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpGenerateTypeDialog : AbstractIdeEditorTest
    {
        public CSharpGenerateTypeDialog()
            : base(nameof(CSharpGenerateTypeDialog))
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        private GenerateTypeDialog_InProc2 GenerateTypeDialog => VisualStudio.GenerateTypeDialog;

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task OpenAndCloseDialogAsync()
        {
            await SetUpEditorAsync(@"class C
{
    void Method() 
    { 
        $$A a;    
    }
}
");

            var codeAction = VisualStudio.Editor.Verify.CodeActionAsync("Generate new type...",
                applyFix: true,
                willBlockUntilComplete: false,
                cancellationToken: HangMitigatingCancellationToken);

            await GenerateTypeDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await GenerateTypeDialog.ClickCancelAsync();
            await GenerateTypeDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await codeAction;
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task CSharpToBasicAsync()
        {
            await VisualStudio.SolutionExplorer.AddProjectAsync("VBProj", WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs");

            await SetUpEditorAsync(@"class C
{
    void Method() 
    { 
        $$A a;    
    }
}
");

            var codeAction = VisualStudio.Editor.Verify.CodeActionAsync("Generate new type...",
                applyFix: true,
                willBlockUntilComplete: false,
                cancellationToken: HangMitigatingCancellationToken);

            await GenerateTypeDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await GenerateTypeDialog.SetAccessibilityAsync("public");
            await GenerateTypeDialog.SetKindAsync("interface");
            await GenerateTypeDialog.SetTargetProjectAsync("VBProj");
            await GenerateTypeDialog.SetTargetFileToNewNameAsync("GenerateTypeTest");
            await GenerateTypeDialog.ClickOkAsync();
            await GenerateTypeDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await codeAction;

            await VisualStudio.SolutionExplorer.OpenFileAsync("VBProj", "GenerateTypeTest.vb");
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"Public Interface A
End Interface
", actualText);

            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs");
            actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"using VBProj;

class C
{
    void Method() 
    { 
        A a;    
    }
}
", actualText);

        }
    }
}
