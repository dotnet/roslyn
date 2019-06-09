// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpGenerateTypeDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        private GenerateTypeDialog_OutOfProc GenerateTypeDialog => VisualStudio.GenerateTypeDialog;

        public CSharpGenerateTypeDialog(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
                    : base(instanceFactory, testOutputHelper, nameof(CSharpGenerateTypeDialog))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
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

            VisualStudio.Editor.Verify.CodeAction("Generate new type...",
                applyFix: true,
                blockUntilComplete: false);

            GenerateTypeDialog.VerifyOpen();
            GenerateTypeDialog.ClickCancel();
            GenerateTypeDialog.VerifyClosed();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public void CSharpToBasic()
        {
            var vbProj = new ProjectUtils.Project("VBProj");
            VisualStudio.SolutionExplorer.AddProject(vbProj, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);

            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFile(project, "Class1.cs");

            SetUpEditor(@"class C
{
    void Method() 
    { 
        $$A a;    
    }
}
");

            VisualStudio.Editor.Verify.CodeAction("Generate new type...",
                applyFix: true,
                blockUntilComplete: false);

            GenerateTypeDialog.VerifyOpen();
            GenerateTypeDialog.SetAccessibility("public");
            GenerateTypeDialog.SetKind("interface");
            GenerateTypeDialog.SetTargetProject("VBProj");
            GenerateTypeDialog.SetTargetFileToNewName("GenerateTypeTest");
            GenerateTypeDialog.ClickOK();
            GenerateTypeDialog.VerifyClosed();

            VisualStudio.SolutionExplorer.OpenFile(vbProj, "GenerateTypeTest.vb");
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"Public Interface A
End Interface
", actualText);

            VisualStudio.SolutionExplorer.OpenFile(project, "Class1.cs");
            actualText = VisualStudio.Editor.GetText();
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
