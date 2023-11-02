// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.ChangeSignature)]
    public class CSharpChangeSignatureDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpChangeSignatureDialog()
            : base(nameof(CSharpChangeSignatureDialog))
        {
        }

        [IdeFact]
        public async Task VerifyCodeRefactoringOffered()
        {
            await SetUpEditorAsync(@"
class C
{
    public void Method$$(int a, string b) { }
}", HangMitigatingCancellationToken);

            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Change signature...", applyFix: false, cancellationToken: HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task VerifyRefactoringCancelled()
        {
            await SetUpEditorAsync(@"
class C
{
    public void Method$$(int a, string b) { }
}", HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.InvokeAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickCancelAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyClosedAsync(HangMitigatingCancellationToken);
            var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
            Assert.Contains(@"
class C
{
    public void Method(int a, string b) { }
}", actualText);
        }

        [IdeFact]
        public async Task VerifyReorderParameters()
        {
            await SetUpEditorAsync(@"
class C
{
    public void Method$$(int a, string b) { }
}", HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.InvokeAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.SelectParameterAsync("int a", HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickDownButtonAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyClosedAsync(HangMitigatingCancellationToken);
            var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
            Assert.Contains(@"
class C
{
    public void Method(string b, int a) { }
}", actualText);
        }

        [IdeFact]
        public async Task VerifyRemoveParameter()
        {
            await SetUpEditorAsync(@"
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
}", HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.InvokeAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.SelectParameterAsync("string b", HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickUpButtonAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickRemoveButtonAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyClosedAsync(HangMitigatingCancellationToken);
            var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
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

        [IdeFact]
        public async Task VerifyCrossLanguageGlobalUndo()
        {
            await SetUpEditorAsync(@"using VBProject;

class Program
{
    static void Main(string[] args)
    {
        VBClass vb = new VBClass();
        vb.Method$$(1, y: ""hello"");
        vb.Method(2, ""world"");
    }
}", HangMitigatingCancellationToken);

            var vbProject = "VBProject";
            var vbProjectReference = vbProject;
            var project = ProjectName;
            await TestServices.SolutionExplorer.AddProjectAsync(vbProject, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic, HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(@"
Public Class VBClass
    Public Sub Method(x As Integer, y As String)
    End Sub
End Class", HangMitigatingCancellationToken);

            await TestServices.SolutionExplorer.SaveAllAsync(HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddProjectReferenceAsync(projectName: project, projectToReferenceName: vbProjectReference, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "Class1.cs", HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.InvokeAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.SelectParameterAsync("String y", HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickUpButtonAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyClosedAsync(HangMitigatingCancellationToken);
            var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
            Assert.Contains(@"vb.Method(y: ""hello"", x: 1);", actualText);

            await TestServices.SolutionExplorer.OpenFileAsync(vbProject, "Class1.vb", HangMitigatingCancellationToken);
            actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
            Assert.Contains(@"Public Sub Method(y As String, x As Integer)", actualText);

            await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Undo, HangMitigatingCancellationToken);
            actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
            Assert.Contains(@"Public Sub Method(x As Integer, y As String)", actualText);

            await TestServices.SolutionExplorer.OpenFileAsync(project, "Class1.cs", HangMitigatingCancellationToken);
            actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
            Assert.Contains(@"vb.Method(2, ""world"");", actualText);
        }

        [IdeFact]
        public async Task VerifyAddParameter()
        {
            await SetUpEditorAsync(@"
class C
{
    public void Method$$(int a, string b) { }

    public void NewMethod()
    {
        Method(1, ""stringB"");
    }
    
}", HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.InvokeAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickAddButtonAsync(HangMitigatingCancellationToken);

            // Add 'c'
            await TestServices.AddParameterDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillTypeFieldAsync("int", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillNameFieldAsync("c", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillCallSiteFieldAsync("2", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickAddButtonAsync(HangMitigatingCancellationToken);

            // Add 'd'
            await TestServices.AddParameterDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillTypeFieldAsync("int", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillNameFieldAsync("d", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillCallSiteFieldAsync("3", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            // Remove 'c'
            await TestServices.ChangeSignatureDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.SelectParameterAsync("int c", HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickRemoveButtonAsync(HangMitigatingCancellationToken);

            // Move 'd' between 'a' and 'b'
            await TestServices.ChangeSignatureDialog.SelectParameterAsync("int d", HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickUpButtonAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickUpButtonAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickDownButtonAsync(HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.ClickAddButtonAsync(HangMitigatingCancellationToken);

            // Add 'c' (as a String instead of an Integer this time)
            // Note that 'c' does not have a callsite value.
            await TestServices.AddParameterDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillTypeFieldAsync("string", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillNameFieldAsync("c", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.SetCallSiteTodoAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyClosedAsync(HangMitigatingCancellationToken);
            var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
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

        [IdeFact]
        public async Task VerifyAddParameterRefactoringCancelled()
        {
            await SetUpEditorAsync(@"
class C
{
    public void Method$$(int a, string b) { }
}", HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.InvokeAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickAddButtonAsync(HangMitigatingCancellationToken);

            await TestServices.AddParameterDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.ClickCancelAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickCancelAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyClosedAsync(HangMitigatingCancellationToken);
            var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
            Assert.Contains(@"
class C
{
    public void Method(int a, string b) { }
}", actualText);
        }

        [IdeFact]
        public async Task VerifyAddParametersAcrossLanguages()
        {
            await SetUpEditorAsync(@"
using VBProject;

class CSharpTest
{
    public void TestMethod()
    {
        VBClass x = new VBClass();
        x.Method$$(0, ""str"", 3.0);
    }
}", HangMitigatingCancellationToken);
            var vbProject = "VBProject";
            await TestServices.SolutionExplorer.AddProjectAsync(vbProject, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic, HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(@"
Public Class VBClass
    Public Function Method(a As Integer, b As String, c As Double) As Integer
        Return 1
    End Function
End Class
", HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.SaveAllAsync(HangMitigatingCancellationToken);
            var project = ProjectName;
            var vbProjectReference = "VBProject";
            await TestServices.SolutionExplorer.AddProjectReferenceAsync(project, vbProjectReference, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "Class1.cs", HangMitigatingCancellationToken);

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.InvokeAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickAddButtonAsync(HangMitigatingCancellationToken);

            await TestServices.AddParameterDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillTypeFieldAsync("String", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillNameFieldAsync("d", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillCallSiteFieldAsync(@"""str2""", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyClosedAsync(HangMitigatingCancellationToken);
            var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
            Assert.Contains(@"x.Method(0, ""str"", 3.0, ""str2"")", actualText);
            await TestServices.SolutionExplorer.OpenFileAsync(vbProject, "Class1.vb", HangMitigatingCancellationToken);
            actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
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
