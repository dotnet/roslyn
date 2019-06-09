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
    public class CSharpChangeSignatureDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        private ChangeSignatureDialog_OutOfProc ChangeSignatureDialog => VisualStudio.ChangeSignatureDialog;

        public CSharpChangeSignatureDialog(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(CSharpChangeSignatureDialog))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void VerifyCodeRefactoringOffered()
        {
            SetUpEditor(@"
class C
{
    public void Method$$(int a, string b) { }
}");

            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Change signature...", applyFix: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
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
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"
class C
{
    public void Method(int a, string b) { }
}", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
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
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"
class C
{
    public void Method(string b, int a) { }
}", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
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
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
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
            VisualStudio.SolutionExplorer.AddProject(vbProject, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            VisualStudio.Editor.SetText(@"
Public Class VBClass
    Public Sub Method(x As Integer, y As String)
    End Sub
End Class");

            VisualStudio.SolutionExplorer.SaveAll();
            VisualStudio.SolutionExplorer.AddProjectReference(fromProjectName: project, toProjectName: vbProjectReference);
            VisualStudio.SolutionExplorer.OpenFile(project, "Class1.cs");

            ChangeSignatureDialog.Invoke();
            ChangeSignatureDialog.VerifyOpen();
            ChangeSignatureDialog.SelectParameter("String y");
            ChangeSignatureDialog.ClickUpButton();
            ChangeSignatureDialog.ClickOK();
            ChangeSignatureDialog.VerifyClosed();
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"vb.Method(y: ""hello"", x: 1);", actualText);

            VisualStudio.SolutionExplorer.OpenFile(vbProject, "Class1.vb");
            actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"Public Sub Method(y As String, x As Integer)", actualText);

            VisualStudio.Editor.Undo();
            actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"Public Sub Method(x As Integer, y As String)", actualText);

            VisualStudio.SolutionExplorer.OpenFile(project, "Class1.cs");
            actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"vb.Method(2, ""world"");", actualText);
        }
    }
}
