// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpChangeSignatureDialog : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpChangeSignatureDialog()
            : base(nameof(CSharpChangeSignatureDialog))
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task VerifyCodeRefactoringOfferedAsync()
        {
            await SetUpEditorAsync(@"
class C
{
    public void Method$$(int a, string b) { }
}");

            await VisualStudio.Editor.InvokeCodeActionListAsync();
            await VisualStudio.Editor.Verify.CodeActionAsync("Change signature...", applyFix: false);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task VerifyRefactoringCancelledAsync()
        {
            await SetUpEditorAsync(@"
class C
{
    public void Method$$(int a, string b) { }
}");

            await ChangeSignatureDialog.InvokeAsync();
            await ChangeSignatureDialog.VerifyOpenAsync();
            await ChangeSignatureDialog.ClickCancelAsync();
            await ChangeSignatureDialog.VerifyClosedAsync();
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"
class C
{
    public void Method(int a, string b) { }
}", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task VerifyReorderParametersAsync()
        {
            await SetUpEditorAsync(@"
class C
{
    public void Method$$(int a, string b) { }
}");

            await ChangeSignatureDialog.InvokeAsync();
            await ChangeSignatureDialog.VerifyOpenAsync();
            await ChangeSignatureDialog.SelectParameterAsync("int a");
            await ChangeSignatureDialog.ClickDownAsync();
            await ChangeSignatureDialog.ClickOkAsync();
            await ChangeSignatureDialog.VerifyClosedAsync();
            var actuaText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"
class C
{
    public void Method(string b, int a) { }
}", actuaText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task VerifyRemoveParameterAsync()
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
}");

            await ChangeSignatureDialog.InvokeAsync();
            await ChangeSignatureDialog.VerifyOpenAsync();
            await ChangeSignatureDialog.SelectParameterAsync("string b");
            await ChangeSignatureDialog.ClickUpAsync();
            await ChangeSignatureDialog.ClickRemoveAsync();
            await ChangeSignatureDialog.ClickOkAsync();
            await ChangeSignatureDialog.VerifyClosedAsync();
            var actuaText = await VisualStudio.Editor.GetTextAsync();
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

        [IdeFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task VerifyCrossLanguageGlobalUndoAsync()
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
}");

            var vbProject = new ProjectUtils.Project("VBProject");
            var vbProjectReference = new ProjectUtils.ProjectReference(vbProject.Name);
            var project = new ProjectUtils.Project(ProjectName);
            await VisualStudio.SolutionExplorer.AddProjectAsync(vbProject.Name, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            await VisualStudio.Editor.SetTextAsync(@"
Public Class VBClass
    Public Sub Method(x As Integer, y As String)
    End Sub
End Class");

            await VisualStudio.SolutionExplorer.SaveAllAsync();
            await VisualStudio.SolutionExplorer.AddProjectReferenceAsync(projectName: ProjectName, projectToReferenceName: vbProjectReference.Name);
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs");

            await ChangeSignatureDialog.InvokeAsync();
            await ChangeSignatureDialog.VerifyOpenAsync();
            await ChangeSignatureDialog.SelectParameterAsync("String y");
            await ChangeSignatureDialog.ClickUpAsync();
            await ChangeSignatureDialog.ClickOkAsync();
            await ChangeSignatureDialog.VerifyClosedAsync();
            var actuaText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"vb.Method(y: ""hello"", x: 1);", actuaText);

            await VisualStudio.SolutionExplorer.OpenFileAsync(vbProject.Name, "Class1.vb");
            actuaText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"Public Sub Method(y As String, x As Integer)", actuaText);

            await VisualStudio.Editor.UndoAsync();
            actuaText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"Public Sub Method(x As Integer, y As String)", actuaText);

            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs");
            actuaText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"vb.Method(2, ""world"");", actuaText);
        }
    }
}
