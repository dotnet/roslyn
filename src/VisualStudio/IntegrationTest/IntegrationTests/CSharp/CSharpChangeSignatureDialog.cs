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
    public class CSharpChangeSignatureDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        private ChangeSignatureDialog_OutOfProc ChangeSignatureDialog => VisualStudio.Instance.ChangeSignatureDialog;

        public CSharpChangeSignatureDialog(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpChangeSignatureDialog))
        {
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/17393"),
         Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void VerifyCodeRefactoringOffered()
        {
            SetUpEditor(@"
class C
{
    public void Method(int a, string b) { }
}");

            this.InvokeCodeActionList();
            this.VerifyCodeAction("Change signature...", applyFix: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
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
            var actuaText = Editor.GetText();
            Assert.Contains(@"
class C
{
    public void Method(int a, string b) { }
}", actuaText);
        }

        [Fact (Skip = "https://github.com/dotnet/roslyn/issues/17640"),
         Trait(Traits.Feature, Traits.Features.ChangeSignature)]
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
            var actuaText = Editor.GetText();
            Assert.Contains(@"
class C
{
    public void Method(string b, int a) { }
}", actuaText);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/17680"),
         Trait(Traits.Feature, Traits.Features.ChangeSignature)]
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
            var actuaText = Editor.GetText();
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
}", actuaText);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/17680"),
         Trait(Traits.Feature, Traits.Features.ChangeSignature)]
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


            VisualStudio.Instance.SolutionExplorer.AddProject("VBProject", WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            Editor.SetText(@"
Public Class VBClass
    Public Sub Method(x As Integer, y As String)
    End Sub
End Class");

            VisualStudio.Instance.SolutionExplorer.SaveAll();
            VisualStudio.Instance.SolutionExplorer.AddProjectReference(fromProjectName: ProjectName, toProjectName: "VBProject");
            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "Class1.cs");

            ChangeSignatureDialog.Invoke();
            ChangeSignatureDialog.VerifyOpen();
            ChangeSignatureDialog.SelectParameter("String y");
            ChangeSignatureDialog.ClickUpButton();
            ChangeSignatureDialog.ClickOK();
            ChangeSignatureDialog.VerifyClosed();
            var actuaText = Editor.GetText();
            Assert.Contains(@"vb.Method(y: ""hello"", x: 1);", actuaText);

            VisualStudio.Instance.SolutionExplorer.OpenFile("VBProject", "Class1.vb");
            actuaText = Editor.GetText();
            Assert.Contains(@"Public Sub Method(y As String, x As Integer)", actuaText);

            Editor.Undo();
            actuaText = Editor.GetText();
            Assert.Contains(@"Public Sub Method(x As Integer, y As String)", actuaText);

            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "Class1.cs");
            actuaText = Editor.GetText();
            Assert.Contains(@"vb.Method(2, ""world"");", actuaText);
        }
    }
}
