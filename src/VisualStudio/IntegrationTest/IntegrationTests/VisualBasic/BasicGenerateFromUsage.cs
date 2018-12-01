// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicGenerateFromUsage : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicGenerateFromUsage( )
            : base( nameof(BasicGenerateFromUsage))
        {
        }

        [TestMethod, TestCategory(Traits.Features.CodeActionsGenerateLocal)]
        public void GenerateLocal()
        {
            SetUpEditor(
@"Module Program
    Sub Main(args As String())
        Dim x As String = $$xyz
    End Sub
End Module");
            VisualStudioInstance.Editor.Verify.CodeAction("Generate local 'xyz'", applyFix: true);
            VisualStudioInstance.Editor.Verify.TextContains(
@"Module Program
    Sub Main(args As String())
        Dim xyz As String = Nothing
        Dim x As String = xyz
    End Sub
End Module");
        }

        [TestMethod, TestCategory(Traits.Features.CodeActionsGenerateType)]
        public void GenerateTypeInNewFile()
        {
            SetUpEditor(
@"Module Program
    Sub Main(args As String())
        Dim x As New $$ClassInNewFile()
    End Sub
End Module");
            VisualStudioInstance.Editor.Verify.CodeAction("Generate class 'ClassInNewFile' in new file", applyFix: true);
            VisualStudioInstance.SolutionExplorer.OpenFile(new ProjectUtils.Project(ProjectName), "ClassInNewFile.vb");
            VisualStudioInstance.Editor.Verify.TextContains(
@"Friend Class ClassInNewFile
    Public Sub New()
    End Sub
End Class");
        }
    }
}
