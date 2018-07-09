// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Roslyn.Test.Utilities;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicChangeSignatureDialog : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicChangeSignatureDialog()
            : base(nameof(BasicChangeSignatureDialog))
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task VerifyCodeRefactoringOfferedAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Method$$(a As Integer, b As Integer)
    End Sub
End Class");

            await Editor.InvokeCodeActionListAsync();
            await Editor.Verify.CodeActionAsync("Change signature...", applyFix: false);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task VerifyRefactoringCancelledAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Method$$(a As Integer, b As String)
    End Sub
End Class");

            var commandTask = ChangeSignatureDialog.InvokeAsync();
            await ChangeSignatureDialog.VerifyOpenAsync();
            await ChangeSignatureDialog.ClickCancelAsync();
            await ChangeSignatureDialog.VerifyClosedAsync();
            await commandTask;
            var actualText = await Editor.GetTextAsync();
            Assert.Contains(@"
Class C
    Sub Method(a As Integer, b As String)
    End Sub
End Class", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task VerifyReorderParametersAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Method$$(a As Integer, b As String)
    End Sub
End Class");

            var commandTask = ChangeSignatureDialog.InvokeAsync();
            await ChangeSignatureDialog.VerifyOpenAsync();
            await ChangeSignatureDialog.SelectParameterAsync("Integer a");
            await ChangeSignatureDialog.ClickDownAsync();
            await ChangeSignatureDialog.ClickOkAsync();
            await ChangeSignatureDialog.VerifyClosedAsync();
            await commandTask;
            var actualText = await Editor.GetTextAsync();
            Assert.Contains(@"
Class C
    Sub Method(b As String, a As Integer)
    End Sub
End Class", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task VerifyReorderAndRemoveParametersAcrossLanguagesAsync()
        {
            await SetUpEditorAsync(@"
Class VBTest
    Sub TestMethod()
        Dim x As New CSharpClass
        x.Method$$(0, ""str"", 3.0)
    End Sub
End Class");
            var csharpProject = new ProjectUtils.Project("CSharpProject");
            await SolutionExplorer.AddProjectAsync(csharpProject.Name, WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp);
            await Editor.SetTextAsync(@"
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
            await SolutionExplorer.SaveAllAsync();
            var project = new ProjectUtils.Project(ProjectName);
            var csharpProjectReference = new ProjectUtils.ProjectReference("CSharpProject");
            await SolutionExplorer.AddProjectReferenceAsync(project.Name, csharpProjectReference.Name);
            await SolutionExplorer.OpenFileAsync(ProjectName, "Class1.vb");

            await Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);

            var commandTask = ChangeSignatureDialog.InvokeAsync();
            await ChangeSignatureDialog.VerifyOpenAsync();
            await ChangeSignatureDialog.SelectParameterAsync("int a");
            await ChangeSignatureDialog.ClickRemoveAsync();
            await ChangeSignatureDialog.SelectParameterAsync("string b");
            await ChangeSignatureDialog.ClickRemoveAsync();
            await ChangeSignatureDialog.SelectParameterAsync("double c");
            await ChangeSignatureDialog.ClickRemoveAsync();
            await ChangeSignatureDialog.SelectParameterAsync("string b");
            await ChangeSignatureDialog.ClickDownAsync();
            await ChangeSignatureDialog.ClickRestoreAsync();
            await ChangeSignatureDialog.ClickOkAsync();
            await ChangeSignatureDialog.VerifyClosedAsync();
            await commandTask;
            var actualText = await Editor.GetTextAsync();
            Assert.Contains(@"x.Method(""str"")", actualText);
            await SolutionExplorer.OpenFileAsync(csharpProject.Name, "Class1.cs");
            actualText = await Editor.GetTextAsync();
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
