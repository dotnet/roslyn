// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicGenerateTypeDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        private GenerateTypeDialog_OutOfProc GenerateTypeDialog => VisualStudioInstance.GenerateTypeDialog;

        public BasicGenerateTypeDialog( )
            : base( nameof(BasicGenerateTypeDialog))
        {
        }

        [TestMethod, TestCategory(Traits.Features.CodeActionsGenerateType)]
        public void BasicToCSharp()
        {
            var csProj = new ProjectUtils.Project("CSProj");
            VisualStudioInstance.SolutionExplorer.AddProject(csProj, WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp);

            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Class1.vb");

            SetUpEditor(@"
Class C
    Sub Method()
        $$Dim _A As A
    End Sub
End Class
");
            VisualStudioInstance.Editor.Verify.CodeAction("Generate new type...",
                applyFix: true,
                blockUntilComplete: false);

            GenerateTypeDialog.VerifyOpen();
            GenerateTypeDialog.SetAccessibility("Public");
            GenerateTypeDialog.SetKind("Structure");
            GenerateTypeDialog.SetTargetProject(csProj.Name);
            GenerateTypeDialog.SetTargetFileToNewName("GenerateTypeTest.cs");
            GenerateTypeDialog.ClickOK();
            GenerateTypeDialog.VerifyClosed();
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"Imports CSProj

Class C
    Sub Method()
        Dim _A As A
    End Sub
End Class
", actualText);

            VisualStudioInstance.SolutionExplorer.OpenFile(csProj, "GenerateTypeTest.cs");
            actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"namespace CSProj
{
    public struct A
    {
    }
}", actualText);
        }

        [TestMethod, TestCategory(Traits.Features.CodeActionsGenerateType)]
        public void SameProject()
        {
            SetUpEditor(@"
Class C
    Sub Method()
        $$Dim _A As A
    End Sub
End Class
");

            VisualStudioInstance.Editor.Verify.CodeAction("Generate new type...",
                applyFix: true,
                blockUntilComplete: false);
            var project = new ProjectUtils.Project(ProjectName);

            GenerateTypeDialog.VerifyOpen();
            GenerateTypeDialog.SetAccessibility("Public");
            GenerateTypeDialog.SetKind("Structure");
            GenerateTypeDialog.SetTargetFileToNewName("GenerateTypeTest");
            GenerateTypeDialog.ClickOK();
            GenerateTypeDialog.VerifyClosed();

            VisualStudioInstance.SolutionExplorer.OpenFile(project, "GenerateTypeTest.vb");
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"Public Structure A
End Structure
", actualText);

            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Class1.vb");
            actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"Class C
    Sub Method()
        Dim _A As A
    End Sub
End Class
", actualText);
        }

        [TestMethod, TestCategory(Traits.Features.CodeActionsGenerateType)]
        public void CheckFoldersPopulateComboBox()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddFile(project, @"folder1\folder2\GenerateTypeTests.vb", open: true);

            SetUpEditor(@"Class C
    Sub Method() 
        $$Dim _A As A
    End Sub
End Class
");
            VisualStudioInstance.Editor.Verify.CodeAction("Generate new type...",
                applyFix: true,
                blockUntilComplete: false);

            GenerateTypeDialog.VerifyOpen();
            GenerateTypeDialog.SetTargetFileToNewName("Other");

            var folders = GenerateTypeDialog.GetNewFileComboBoxItems();

            ExtendedAssert.Contains(@"\folder1\", folders);
            ExtendedAssert.Contains(@"\folder1\folder2\", folders);

            GenerateTypeDialog.ClickCancel();
        }
    }
}
