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
    public class CSharpChangeSignatureDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        private ChangeSignatureDialog_OutOfProc ChangeSignatureDialog => VisualStudioInstance.ChangeSignatureDialog;

        public CSharpChangeSignatureDialog( )
            : base( nameof(CSharpChangeSignatureDialog))
        {
        }

        [TestMethod, TestCategory(Traits.Features.ChangeSignature)]
        public void VerifyCodeRefactoringOffered()
        {
            SetUpEditor(@"
class C
{
    public void Method$$(int a, string b) { }
}");

            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction("Change signature...", applyFix: false);
        }

        [TestMethod, TestCategory(Traits.Features.ChangeSignature)]
        public void VerifyRefactoringCancelled()
        {
            SetUpEditor(@"
class C
{
    public void Method$$(int a, string b) { }
}");

            ChangeSignatureDialog.Invoke();
            ChangeSignatureDialog.VerifyOpen();
            ChangeSignatureDialog.ClickCancel();
            ChangeSignatureDialog.VerifyClosed();
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"
class C
{
    public void Method(int a, string b) { }
}", actualText);
        }

        [TestMethod, TestCategory(Traits.Features.ChangeSignature)]
        public void VerifyReorderParameters()
        {
            SetUpEditor(@"
class C
{
    public void Method$$(int a, string b) { }
}");

            ChangeSignatureDialog.Invoke();
            ChangeSignatureDialog.VerifyOpen();
            ChangeSignatureDialog.SelectParameter("int a");
            ChangeSignatureDialog.ClickDownButton();
            ChangeSignatureDialog.ClickOK();
            ChangeSignatureDialog.VerifyClosed();
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"
class C
{
    public void Method(string b, int a) { }
}", actualText);
        }

        [TestMethod, TestCategory(Traits.Features.ChangeSignature)]
        public void VerifyRemoveParameter()
        {
            SetUpEditor(@"
class C
{
    /// <summary>
    /// A method.
    /// </summary>
    /// <param name=""a""></param>
    /// <param name=""b""></param>
    public void Method$$(int a, string b) { }

    void Test()
    {
        Method(1, ""s"");
    }
}");

            ChangeSignatureDialog.Invoke();
            ChangeSignatureDialog.VerifyOpen();
            ChangeSignatureDialog.SelectParameter("string b");
            ChangeSignatureDialog.ClickUpButton();
            ChangeSignatureDialog.ClickRemoveButton();
            ChangeSignatureDialog.ClickOK();
            ChangeSignatureDialog.VerifyClosed();
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"
class C
{
    /// <summary>
    /// A method.
    /// </summary>
    /// <param name=""a""></param>
    /// 
    public void Method(int a) { }

    void Test()
    {
        Method(1);
    }
}", actualText);
        }

        [TestMethod, TestCategory(Traits.Features.ChangeSignature)]
        public void VerifyCrossLanguageGlobalUndo()
        {
            SetUpEditor(@"using VBProject;

class Program
{
    static void Main(string[] args)
    {
        VBClass vb = new VBClass();
        vb.Method$$(1, y: ""hello"");
        vb.Method(2, ""world"");
    }
}");

            var vbProject = new ProjectUtils.Project("VBProject");
            var vbProjectReference = new ProjectUtils.ProjectReference(vbProject.Name);
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddProject(vbProject, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            VisualStudioInstance.Editor.SetText(@"
Public Class VBClass
    Public Sub Method(x As Integer, y As String)
    End Sub
End Class");

            VisualStudioInstance.SolutionExplorer.SaveAll();
            VisualStudioInstance.SolutionExplorer.AddProjectReference(fromProjectName: project, toProjectName: vbProjectReference);
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Class1.cs");

            ChangeSignatureDialog.Invoke();
            ChangeSignatureDialog.VerifyOpen();
            ChangeSignatureDialog.SelectParameter("String y");
            ChangeSignatureDialog.ClickUpButton();
            ChangeSignatureDialog.ClickOK();
            ChangeSignatureDialog.VerifyClosed();
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"vb.Method(y: ""hello"", x: 1);", actualText);

            VisualStudioInstance.SolutionExplorer.OpenFile(vbProject, "Class1.vb");
            actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"Public Sub Method(y As String, x As Integer)", actualText);

            VisualStudioInstance.Editor.Undo();
            actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"Public Sub Method(x As Integer, y As String)", actualText);

            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Class1.cs");
            actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"vb.Method(2, ""world"");", actualText);
        }
    }
}
