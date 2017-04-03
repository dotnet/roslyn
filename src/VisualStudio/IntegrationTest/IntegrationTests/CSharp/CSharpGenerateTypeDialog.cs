// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpGenerateTypeDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        private GenerateTypeDialog_OutOfProc GenerateTypeDialog => VisualStudio.Instance.GenerateTypeDialog;

        public CSharpGenerateTypeDialog(VisualStudioInstanceFactory instanceFactory)
                    : base(instanceFactory, nameof(CSharpGenerateTypeDialog))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
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

            this.VerifyCodeAction("Generate new type...",
                applyFix: true,
                blockUntilComplete: false);

            GenerateTypeDialog.VerifyOpen();
            GenerateTypeDialog.ClickCancel();
            GenerateTypeDialog.VerifyClosed();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public void CSharpToBasic()
        {
            VisualStudio.Instance.SolutionExplorer.AddProject("VBProj", WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);

            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "Class1.cs");

            SetUpEditor(@"class C
{
    void Method() 
    { 
        $$A a;    
    }
}
");

            this.VerifyCodeAction("Generate new type...",
                applyFix: true,
                blockUntilComplete: false);

            GenerateTypeDialog.VerifyOpen();
            GenerateTypeDialog.SetAccessibility("public");
            GenerateTypeDialog.SetKind("interface");
            GenerateTypeDialog.SetTargetProject("VBProj");
            GenerateTypeDialog.SetTargetFileToNewName("GenerateTypeTest");
            GenerateTypeDialog.ClickOK();
            GenerateTypeDialog.VerifyClosed();

            VisualStudio.Instance.SolutionExplorer.OpenFile("VBProj", "GenerateTypeTest.vb");
            var actualText = Editor.GetText();
            Assert.Contains(@"Public Interface A
End Interface
", actualText);

            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "Class1.cs");
            actualText = Editor.GetText();
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
