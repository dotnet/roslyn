// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicGenerateTypeDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        private GenerateTypeDialog_OutOfProc GenerateTypeDialog => VisualStudio.Instance.GenerateTypeDialog;

        public BasicGenerateTypeDialog(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicGenerateTypeDialog))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public void BasicToCSharp()
        {
            VisualStudio.Instance.SolutionExplorer.AddProject("CSProj", WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp);

            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "Class1.vb");

            SetUpEditor(@"
Class C
    Sub Method()
        $$Dim _A As A
    End Sub
End Class
");
            VerifyCodeAction("Generate new type...",
                applyFix: true,
                blockUntilComplete: false);

            GenerateTypeDialog.VerifyOpen();
            GenerateTypeDialog.SetAccessibility("Public");
            GenerateTypeDialog.SetKind("Structure");
            GenerateTypeDialog.SetTargetProject("CSProj");
            GenerateTypeDialog.SetTargetFileToNewName("GenerateTypeTest.cs");
            GenerateTypeDialog.ClickOK();
            GenerateTypeDialog.VerifyClosed();

            VerifyTextContains(@"Imports CSProj

Class C
    Sub Method()
        Dim _A As A
    End Sub
End Class
");

            VisualStudio.Instance.SolutionExplorer.OpenFile("CSProj", "GenerateTypeTest.cs");
            VerifyTextContains(@"namespace CSProj
{
    public struct A
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public void SameProject()
        {
            SetUpEditor(@"
Class C
    Sub Method()
        $$Dim _A As A
    End Sub
End Class
");

            VerifyCodeAction("Generate new type...",
                applyFix: true,
                blockUntilComplete: false);

            GenerateTypeDialog.VerifyOpen();
            GenerateTypeDialog.SetAccessibility("Public");
            GenerateTypeDialog.SetKind("Structure");
            GenerateTypeDialog.SetTargetFileToNewName("GenerateTypeTest");
            GenerateTypeDialog.ClickOK();
            GenerateTypeDialog.VerifyClosed();

            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "GenerateTypeTest.vb");
            VerifyTextContains(@"Public Structure A
End Structure
");

            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "Class1.vb");
            VerifyTextContains(@"Class C
    Sub Method()
        Dim _A As A
    End Sub
End Class
");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/17680"), 
         Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public void CheckFoldersPopulateComboBox()
        {
            VisualStudio.Instance.SolutionExplorer.AddFile(ProjectName, @"folder1\folder2\GenerateTypeTests.vb", open: true);

            SetUpEditor(@"Class C
    Sub Method() 
        $$Dim _A As A
    End Sub
End Class
");
            VerifyCodeAction("Generate new type...",
                applyFix: true,
                blockUntilComplete: false);

            GenerateTypeDialog.VerifyOpen();
            GenerateTypeDialog.SetTargetFileToNewName("Other");

            var folders = GenerateTypeDialog.GetNewFileComboBoxItems();

            Assert.Contains(@"\folder1\", folders);
            Assert.Contains(@"\folder1\folder2\", folders);

            GenerateTypeDialog.ClickCancel();
        }
    }
}
