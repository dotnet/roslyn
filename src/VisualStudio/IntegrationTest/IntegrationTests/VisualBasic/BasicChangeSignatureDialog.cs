// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicChangeSignatureDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        private ChangeSignatureDialog_OutOfProc ChangeSignatureDialog => VisualStudio.ChangeSignatureDialog;

        public BasicChangeSignatureDialog(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(BasicChangeSignatureDialog))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void VerifyCodeRefactoringOffered()
        {
            SetUpEditor(@"
Class C
    Sub Method$$(a As Integer, b As Integer)
    End Sub
End Class");

            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Change signature...", applyFix: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void VerifyRefactoringCancelled()
        {
            SetUpEditor(@"
Class C
    Sub Method$$(a As Integer, b As String)
    End Sub
End Class");

            ChangeSignatureDialog.Invoke();
            ChangeSignatureDialog.VerifyOpen();
            ChangeSignatureDialog.ClickCancel();
            ChangeSignatureDialog.VerifyClosed();
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"
Class C
    Sub Method(a As Integer, b As String)
    End Sub
End Class", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void VerifyReorderParameters()
        {
            SetUpEditor(@"
Class C
    Sub Method$$(a As Integer, b As String)
    End Sub
End Class");

            ChangeSignatureDialog.Invoke();
            ChangeSignatureDialog.VerifyOpen();
            ChangeSignatureDialog.SelectParameter("Integer a");
            ChangeSignatureDialog.ClickDownButton();
            ChangeSignatureDialog.ClickOK();
            ChangeSignatureDialog.VerifyClosed();
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"
Class C
    Sub Method(b As String, a As Integer)
    End Sub
End Class", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void VerifyReorderAndRemoveParametersAcrossLanguages()
        {
            SetUpEditor(@"
Class VBTest
    Sub TestMethod()
        Dim x As New CSharpClass
        x.Method$$(0, ""str"", 3.0)
    End Sub
End Class");
            var csharpProject = new ProjectUtils.Project("CSharpProject");
            VisualStudio.SolutionExplorer.AddProject(csharpProject, WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp);
            VisualStudio.Editor.SetText(@"
public class CSharpClass
{
    /// <summary>
    /// A method in CSharp.
    /// </summary>
    /// <param name=""a"">parameter a</param>
    /// <param name=""b"">parameter b</param>
    /// <param name=""c"">parameter c</param>
    /// <returns>One</returns>
    public int Method(int a, string b, double c)
    {
        return 1;
    }
}");
            VisualStudio.SolutionExplorer.SaveAll();
            var project = new ProjectUtils.Project(ProjectName);
            var csharpProjectReference = new ProjectUtils.ProjectReference("CSharpProject");
            VisualStudio.SolutionExplorer.AddProjectReference(project, csharpProjectReference);
            VisualStudio.SolutionExplorer.OpenFile(project, "Class1.vb");

            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);

            ChangeSignatureDialog.Invoke();
            ChangeSignatureDialog.VerifyOpen();
            ChangeSignatureDialog.SelectParameter("int a");
            ChangeSignatureDialog.ClickRemoveButton();
            ChangeSignatureDialog.SelectParameter("string b");
            ChangeSignatureDialog.ClickRemoveButton();
            ChangeSignatureDialog.SelectParameter("double c");
            ChangeSignatureDialog.ClickRemoveButton();
            ChangeSignatureDialog.SelectParameter("string b");
            ChangeSignatureDialog.ClickDownButton();
            ChangeSignatureDialog.ClickRestoreButton();
            ChangeSignatureDialog.ClickOK();
            ChangeSignatureDialog.VerifyClosed();
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"x.Method(""str"")", actualText);
            VisualStudio.SolutionExplorer.OpenFile(csharpProject, "Class1.cs");
            actualText = VisualStudio.Editor.GetText();
            var expectedText = @"
public class CSharpClass
{
    /// <summary>
    /// A method in CSharp.
    /// </summary>
    /// <param name=""b"">parameter b</param>
    /// 
    /// 
    /// <returns>One</returns>
    public int Method(string b)
    {
        return 1;
    }
}";
            Assert.Contains(expectedText, actualText);
        }
    }
}
