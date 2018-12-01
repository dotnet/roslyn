// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpGenerateTypeDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        private GenerateTypeDialog_OutOfProc GenerateTypeDialog => VisualStudioInstance.GenerateTypeDialog;

        public CSharpGenerateTypeDialog( )
                    : base( nameof(CSharpGenerateTypeDialog))
        {
        }

        [TestMethod, TestCategory(Traits.Features.CodeActionsGenerateType)]
        public void OpenAndCloseDialog()
        {
            SetUpEditor(@"class C
{
    void Method() 
    { 
        $$A a;    
    }
}
");

            VisualStudioInstance.Editor.Verify.CodeAction("Generate new type...",
                applyFix: true,
                blockUntilComplete: false);

            GenerateTypeDialog.VerifyOpen();
            GenerateTypeDialog.ClickCancel();
            GenerateTypeDialog.VerifyClosed();
        }

        [TestMethod, TestCategory(Traits.Features.CodeActionsGenerateType)]
        public void CSharpToBasic()
        {
            var vbProj = new ProjectUtils.Project("VBProj");
            VisualStudioInstance.SolutionExplorer.AddProject(vbProj, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);

            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Class1.cs");

            SetUpEditor(@"class C
{
    void Method() 
    { 
        $$A a;    
    }
}
");

            VisualStudioInstance.Editor.Verify.CodeAction("Generate new type...",
                applyFix: true,
                blockUntilComplete: false);

            GenerateTypeDialog.VerifyOpen();
            GenerateTypeDialog.SetAccessibility("public");
            GenerateTypeDialog.SetKind("interface");
            GenerateTypeDialog.SetTargetProject("VBProj");
            GenerateTypeDialog.SetTargetFileToNewName("GenerateTypeTest");
            GenerateTypeDialog.ClickOK();
            GenerateTypeDialog.VerifyClosed();

            VisualStudioInstance.SolutionExplorer.OpenFile(vbProj, "GenerateTypeTest.vb");
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"Public Interface A
End Interface
", actualText);

            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Class1.cs");
            actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"using VBProj;

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
