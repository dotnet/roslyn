// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
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

        private AddParameterDialog_OutOfProc AddParameterDialog => VisualStudio.AddParameterDialog;

        public CSharpChangeSignatureDialog(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpChangeSignatureDialog))
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void VerifyAddParameter()
        {
            SetUpEditor(@"
class C
{
    public void Method$$(int a, string b) { }

    public void NewMethod()
    {
        Method(1, ""stringB"");
    }
    
}");

            ChangeSignatureDialog.Invoke();
            ChangeSignatureDialog.VerifyOpen();
            ChangeSignatureDialog.ClickAddButton();

            // Add 'c'
            AddParameterDialog.VerifyOpen();
            AddParameterDialog.FillTypeField("int");
            AddParameterDialog.FillNameField("c");
            AddParameterDialog.FillCallSiteField("2");
            AddParameterDialog.ClickOK();
            AddParameterDialog.VerifyClosed();

            ChangeSignatureDialog.VerifyOpen();
            ChangeSignatureDialog.ClickAddButton();

            // Add 'd'
            AddParameterDialog.VerifyOpen();
            AddParameterDialog.FillTypeField("int");
            AddParameterDialog.FillNameField("d");
            AddParameterDialog.FillCallSiteField("3");
            AddParameterDialog.ClickOK();
            AddParameterDialog.VerifyClosed();

            // Remove 'c'
            ChangeSignatureDialog.VerifyOpen();
            ChangeSignatureDialog.SelectParameter("int c");
            ChangeSignatureDialog.ClickRemoveButton();

            // Move 'd' between 'a' and 'b'
            ChangeSignatureDialog.SelectParameter("int d");
            ChangeSignatureDialog.ClickUpButton();
            ChangeSignatureDialog.ClickUpButton();
            ChangeSignatureDialog.ClickDownButton();

            ChangeSignatureDialog.ClickAddButton();

            // Add 'c' (as a String instead of an Integer this time)
            // Note that 'c' does not have a callsite value.
            AddParameterDialog.VerifyOpen();
            AddParameterDialog.FillTypeField("string");
            AddParameterDialog.FillNameField("c");
            AddParameterDialog.SetCallSiteTodo();
            AddParameterDialog.ClickOK();
            AddParameterDialog.VerifyClosed();

            ChangeSignatureDialog.ClickOK();
            ChangeSignatureDialog.VerifyClosed();
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"
class C
{
    public void Method(int a, int d, string b, string c) { }

    public void NewMethod()
    {
        Method(1, 3, ""stringB"", TODO);
    }
    
}", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void VerifyAddParameterRefactoringCancelled()
        {
            SetUpEditor(@"
class C
{
    public void Method$$(int a, string b) { }
}");

            ChangeSignatureDialog.Invoke();
            ChangeSignatureDialog.VerifyOpen();
            ChangeSignatureDialog.ClickAddButton();

            AddParameterDialog.VerifyOpen();
            AddParameterDialog.ClickCancel();
            AddParameterDialog.VerifyClosed();

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
        public void VerifyAddParametersAcrossLanguages()
        {
            SetUpEditor(@"
using VBProject;

class CSharpTest
{
    public void TestMethod()
    {
        VBClass x = new VBClass();
        x.Method$$(0, ""str"", 3.0);
    }
}");
            var vbProject = new ProjectUtils.Project("VBProject");
            VisualStudio.SolutionExplorer.AddProject(vbProject, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            VisualStudio.Editor.SetText(@"
Public Class VBClass
    Public Function Method(a As Integer, b As String, c As Double) As Integer
        Return 1
    End Function
End Class
");
            VisualStudio.SolutionExplorer.SaveAll();
            var project = new ProjectUtils.Project(ProjectName);
            var vbProjectReference = new ProjectUtils.ProjectReference("VBProject");
            VisualStudio.SolutionExplorer.AddProjectReference(project, vbProjectReference);
            VisualStudio.SolutionExplorer.OpenFile(project, "Class1.cs");

            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);

            ChangeSignatureDialog.Invoke();
            ChangeSignatureDialog.VerifyOpen();
            ChangeSignatureDialog.ClickAddButton();

            AddParameterDialog.VerifyOpen();
            AddParameterDialog.FillTypeField("String");
            AddParameterDialog.FillNameField("d");
            AddParameterDialog.FillCallSiteField(@"""str2""");
            AddParameterDialog.ClickOK();
            AddParameterDialog.VerifyClosed();

            ChangeSignatureDialog.ClickOK();
            ChangeSignatureDialog.VerifyClosed();
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"x.Method(0, ""str"", 3.0, ""str2"")", actualText);
            VisualStudio.SolutionExplorer.OpenFile(vbProject, "Class1.vb");
            actualText = VisualStudio.Editor.GetText();
            var expectedText = @"
Public Class VBClass
    Public Function Method(a As Integer, b As String, c As Double, d As String) As Integer
        Return 1
    End Function
End Class";
            Assert.Contains(expectedText, actualText);
        }
    }
}
