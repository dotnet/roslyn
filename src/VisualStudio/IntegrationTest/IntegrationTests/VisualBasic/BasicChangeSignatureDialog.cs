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

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicChangeSignatureDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        private ChangeSignatureDialog_OutOfProc ChangeSignatureDialog => VisualStudio.ChangeSignatureDialog;

        private AddParameterDialog_OutOfProc AddParameterDialog => VisualStudio.AddParameterDialog;

        public BasicChangeSignatureDialog(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicChangeSignatureDialog))
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void VerifyAddParameter()
        {
            SetUpEditor(@"
Class C
    Sub Method$$(a As Integer, b As String)
    End Sub
    Sub NewMethod()
        Method(1, ""stringB"")
    End Sub
End Class");

            ChangeSignatureDialog.Invoke();
            ChangeSignatureDialog.VerifyOpen();
            ChangeSignatureDialog.ClickAddButton();

            // Add 'c'
            AddParameterDialog.VerifyOpen();
            AddParameterDialog.FillTypeField("Integer");
            AddParameterDialog.FillNameField("c");
            AddParameterDialog.FillCallSiteField("2");
            AddParameterDialog.ClickOK();
            AddParameterDialog.VerifyClosed();

            ChangeSignatureDialog.VerifyOpen();
            ChangeSignatureDialog.ClickAddButton();

            // Add 'd'
            AddParameterDialog.VerifyOpen();
            AddParameterDialog.FillTypeField("Integer");
            AddParameterDialog.FillNameField("d");
            AddParameterDialog.FillCallSiteField("3");
            AddParameterDialog.ClickOK();
            AddParameterDialog.VerifyClosed();

            // Remove 'c'
            ChangeSignatureDialog.VerifyOpen();
            ChangeSignatureDialog.SelectParameter("Integer c");
            ChangeSignatureDialog.ClickRemoveButton();

            // Move 'd' between 'a' and 'b'
            ChangeSignatureDialog.SelectParameter("Integer d");
            ChangeSignatureDialog.ClickUpButton();
            ChangeSignatureDialog.ClickUpButton();
            ChangeSignatureDialog.ClickDownButton();

            ChangeSignatureDialog.ClickAddButton();

            // Add 'c' (as a String instead of an Integer this time)
            // Note that 'c' does not have a callsite value.
            AddParameterDialog.VerifyOpen();
            AddParameterDialog.FillTypeField("String");
            AddParameterDialog.FillNameField("c");
            AddParameterDialog.SetCallSiteTodo();
            AddParameterDialog.ClickOK();
            AddParameterDialog.VerifyClosed();

            ChangeSignatureDialog.ClickOK();
            ChangeSignatureDialog.VerifyClosed();
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"
Class C
    Sub Method(a As Integer, d As Integer, b As String, c As String)
    End Sub
    Sub NewMethod()
        Method(1, 3, ""stringB"", TODO)
    End Sub
End Class", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void VerifyAddParameterRefactoringCancelled()
        {
            SetUpEditor(@"
Class C
    Sub Method$$(a As Integer, b As String)
    End Sub
End Class");

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
Class C
    Sub Method(a As Integer, b As String)
    End Sub
End Class", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void VerifyAddParametersAcrossLanguages()
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
            ChangeSignatureDialog.ClickAddButton();

            AddParameterDialog.VerifyOpen();
            AddParameterDialog.FillTypeField("string");
            AddParameterDialog.FillNameField("d");
            AddParameterDialog.FillCallSiteField(@"""str2""");
            AddParameterDialog.ClickOK();
            AddParameterDialog.VerifyClosed();

            ChangeSignatureDialog.ClickOK();
            ChangeSignatureDialog.VerifyClosed();
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"x.Method(0, ""str"", 3.0, ""str2"")", actualText);
            VisualStudio.SolutionExplorer.OpenFile(csharpProject, "Class1.cs");
            actualText = VisualStudio.Editor.GetText();
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
