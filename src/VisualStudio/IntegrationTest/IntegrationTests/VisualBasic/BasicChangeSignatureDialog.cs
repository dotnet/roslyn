// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicChangeSignatureDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        private ChangeSignatureDialog_OutOfProc ChangeSignatureDialog => VisualStudio.Instance.ChangeSignatureDialog;

        public BasicChangeSignatureDialog(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicChangeSignatureDialog))
        {
        }
        
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/17393"),
         Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void VerifyCodeRefactoringOffered()
        {
            SetUpEditor(@"
Class C
    Sub Method$$(a As Integer, b As Integer)
    End Sub
End Class");

            InvokeCodeActionList();
            VerifyCodeAction("Change signature...", applyFix: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
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

            VerifyTextContains(@"
Class C
    Sub Method(a As Integer, b As String)
    End Sub
End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
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

            VerifyTextContains(@"
Class C
    Sub Method(b As String, a As Integer)
    End Sub
End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void VerifyReorderAndRemoveParametersAcrossLanguages()
        {
            SetUpEditor(@"
Class VBTest
    Sub TestMethod()
        Dim x As New CSharpClass
        x.Method$$(0, ""str"", 3.0)
    End Sub
End Class");

            VisualStudio.Instance.SolutionExplorer.AddProject("CSharpProject", WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp);
            Editor.SetText(@"
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
            VisualStudio.Instance.SolutionExplorer.SaveAll();
            VisualStudio.Instance.SolutionExplorer.AddProjectReference(ProjectName, "CSharpProject");
            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "Class1.vb");

            WaitForAsyncOperations(FeatureAttribute.Workspace);

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

            VerifyTextContains(@"x.Method(""str"")");

            VisualStudio.Instance.SolutionExplorer.OpenFile("CSharpProject", "Class1.cs");
            VerifyTextContains(@"
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
}");
        }
    }
}
