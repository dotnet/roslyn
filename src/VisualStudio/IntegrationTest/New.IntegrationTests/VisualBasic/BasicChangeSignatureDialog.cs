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

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic
{
    [Trait(Traits.Feature, Traits.Features.ChangeSignature)]
    public class BasicChangeSignatureDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicChangeSignatureDialog()
            : base(nameof(BasicChangeSignatureDialog))
        {
        }

        [IdeFact]
        public async Task VerifyCodeRefactoringOffered()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Method$$(a As Integer, b As Integer)
    End Sub
End Class", HangMitigatingCancellationToken);

            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Change signature...", applyFix: false, cancellationToken: HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task VerifyRefactoringCancelled()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Method$$(a As Integer, b As String)
    End Sub
End Class", HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.InvokeAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickCancelAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyClosedAsync(HangMitigatingCancellationToken);
            var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
            Assert.Contains(@"
Class C
    Sub Method(a As Integer, b As String)
    End Sub
End Class", actualText);
        }

        [IdeFact]
        public async Task VerifyReorderParameters()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Method$$(a As Integer, b As String)
    End Sub
End Class", HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.InvokeAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.SelectParameterAsync("Integer a", HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickDownButtonAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyClosedAsync(HangMitigatingCancellationToken);
            var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
            Assert.Contains(@"
Class C
    Sub Method(b As String, a As Integer)
    End Sub
End Class", actualText);
        }

        [IdeFact]
        public async Task VerifyReorderAndRemoveParametersAcrossLanguages()
        {
            await SetUpEditorAsync(@"
Class VBTest
    Sub TestMethod()
        Dim x As New CSharpClass
        x.Method$$(0, ""str"", 3.0)
    End Sub
End Class", HangMitigatingCancellationToken);
            var csharpProject = "CSharpProject";
            await TestServices.SolutionExplorer.AddProjectAsync(csharpProject, WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp, HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(@"
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
}", HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.SaveAllAsync(HangMitigatingCancellationToken);
            var project = ProjectName;
            var csharpProjectReference = "CSharpProject";
            await TestServices.SolutionExplorer.AddProjectReferenceAsync(project, csharpProjectReference, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "Class1.vb", HangMitigatingCancellationToken);

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.InvokeAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.SelectParameterAsync("int a", HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickRemoveButtonAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.SelectParameterAsync("string b", HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickRemoveButtonAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.SelectParameterAsync("double c", HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickRemoveButtonAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.SelectParameterAsync("string b", HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickDownButtonAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickRestoreButtonAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyClosedAsync(HangMitigatingCancellationToken);
            var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
            Assert.Contains(@"x.Method(""str"")", actualText);
            await TestServices.SolutionExplorer.OpenFileAsync(csharpProject, "Class1.cs", HangMitigatingCancellationToken);
            actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
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

        [IdeFact]
        public async Task VerifyAddParameter()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Method$$(a As Integer, b As String)
    End Sub
    Sub NewMethod()
        Method(1, ""stringB"")
    End Sub
End Class", HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.InvokeAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickAddButtonAsync(HangMitigatingCancellationToken);

            // Add 'c'
            await TestServices.AddParameterDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillTypeFieldAsync("Integer", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillNameFieldAsync("c", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillCallSiteFieldAsync("2", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickAddButtonAsync(HangMitigatingCancellationToken);

            // Add 'd'
            await TestServices.AddParameterDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillTypeFieldAsync("Integer", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillNameFieldAsync("d", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillCallSiteFieldAsync("3", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            // Remove 'c'
            await TestServices.ChangeSignatureDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.SelectParameterAsync("Integer c", HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickRemoveButtonAsync(HangMitigatingCancellationToken);

            // Move 'd' between 'a' and 'b'
            await TestServices.ChangeSignatureDialog.SelectParameterAsync("Integer d", HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickUpButtonAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickUpButtonAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickDownButtonAsync(HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.ClickAddButtonAsync(HangMitigatingCancellationToken);

            // Add 'c' (as a String instead of an Integer this time)
            // Note that 'c' does not have a callsite value.
            await TestServices.AddParameterDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillTypeFieldAsync("String", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillNameFieldAsync("c", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.SetCallSiteTodoAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyClosedAsync(HangMitigatingCancellationToken);
            var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
            Assert.Contains(@"
Class C
    Sub Method(a As Integer, d As Integer, b As String, c As String)
    End Sub
    Sub NewMethod()
        Method(1, 3, ""stringB"", TODO)
    End Sub
End Class", actualText);
        }

        [IdeFact]
        public async Task VerifyAddParameterRefactoringCancelled()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Method$$(a As Integer, b As String)
    End Sub
End Class", HangMitigatingCancellationToken);

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
Class C
    Sub Method(a As Integer, b As String)
    End Sub
End Class", actualText);
        }

        [IdeFact]
        public async Task VerifyAddParametersAcrossLanguages()
        {
            await SetUpEditorAsync(@"
Class VBTest
    Sub TestMethod()
        Dim x As New CSharpClass
        x.Method$$(0, ""str"", 3.0)
    End Sub
End Class", HangMitigatingCancellationToken);
            var csharpProject = "CSharpProject";
            await TestServices.SolutionExplorer.AddProjectAsync(csharpProject, WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp, HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(@"
public class CSharpClass
{
    public int Method(int a, string b, double c)
    {
        return 1;
    }
}", HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.SaveAllAsync(HangMitigatingCancellationToken);
            var project = ProjectName;
            var csharpProjectReference = "CSharpProject";
            await TestServices.SolutionExplorer.AddProjectReferenceAsync(project, csharpProjectReference, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "Class1.vb", HangMitigatingCancellationToken);

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.InvokeAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.ClickAddButtonAsync(HangMitigatingCancellationToken);

            await TestServices.AddParameterDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillTypeFieldAsync("string", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillNameFieldAsync("d", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.FillCallSiteFieldAsync(@"""str2""", HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.AddParameterDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

            await TestServices.ChangeSignatureDialog.ClickOKAsync(HangMitigatingCancellationToken);
            await TestServices.ChangeSignatureDialog.VerifyClosedAsync(HangMitigatingCancellationToken);
            var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
            Assert.Contains(@"x.Method(0, ""str"", 3.0, ""str2"")", actualText);
            await TestServices.SolutionExplorer.OpenFileAsync(csharpProject, "Class1.cs", HangMitigatingCancellationToken);
            actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
            var expectedText = @"
public class CSharpClass
{
    public int Method(int a, string b, double c, string d)
    {
        return 1;
    }
}";
            Assert.Contains(expectedText, actualText);
        }
    }
}
