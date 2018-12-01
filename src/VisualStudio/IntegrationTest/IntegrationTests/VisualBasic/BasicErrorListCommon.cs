// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    public class BasicErrorListCommon : AbstractEditorTest
    {
        public BasicErrorListCommon(string templateName) : base(nameof(BasicErrorListCommon), templateName) { }

        protected override string LanguageName => LanguageNames.VisualBasic;

        public virtual void ErrorList()
        {
            VisualStudioInstance.Editor.SetText(@"
Module Module1

    Function Good() As P
        Return Nothing
    End Function

    Sub Main()
        Goo()
    End Sub

End Module
");
            VisualStudioInstance.ErrorList.ShowErrorList();
            var expectedContents = new[] {
                new ErrorListItem(
                    severity: "Error",
                    description: "Type 'P' is not defined.",
                    project: "TestProj.vbproj",
                    fileName: "Class1.vb",
                    line: 4,
                    column: 24),
                new ErrorListItem(
                    severity: "Error",
                    description: "'Goo' is not declared. It may be inaccessible due to its protection level.",
                    project: "TestProj.vbproj",
                    fileName: "Class1.vb",
                    line: 9,
                    column: 9)
            };
            var actualContents = VisualStudioInstance.ErrorList.GetErrorListContents();
            Assert.AreEqual(expectedContents, actualContents);
            VisualStudioInstance.ErrorList.NavigateToErrorListItem(0);
            VisualStudioInstance.Editor.Verify.CaretPosition(43);
            VisualStudioInstance.SolutionExplorer.BuildSolution(waitForBuildToFinish: true);
            VisualStudioInstance.ErrorList.ShowErrorList();
            actualContents = VisualStudioInstance.ErrorList.GetErrorListContents();
            Assert.AreEqual(expectedContents, actualContents);
        }

        public virtual void ErrorsDuringMethodBodyEditing()
        {
            VisualStudioInstance.Editor.SetText(@"
Namespace N
    Class C
        Private F As Integer
        Sub S()
             ' Comment
        End Sub
    End Class
End Namespace
");
            VisualStudioInstance.Editor.PlaceCaret(" Comment", charsOffset: -2);
            VisualStudioInstance.SendKeys.Send("F = 0");
            VisualStudioInstance.ErrorList.ShowErrorList();
            var expectedContents = new ErrorListItem[] { };
            var actualContents = VisualStudioInstance.ErrorList.GetErrorListContents();
            Assert.AreEqual(expectedContents, actualContents);

            VisualStudioInstance.Editor.Activate();
            VisualStudioInstance.Editor.PlaceCaret("F = 0 ' Comment", charsOffset: -1);
            VisualStudioInstance.Editor.SendKeys("F");
            VisualStudioInstance.ErrorList.ShowErrorList();
            expectedContents = new ErrorListItem[] {
                new ErrorListItem(
                    severity: "Error",
                    description: "'FF' is not declared. It may be inaccessible due to its protection level.",
                    project: "TestProj.vbproj",
                    fileName: "Class1.vb",
                    line: 6,
                    column: 13)
            };
            actualContents = VisualStudioInstance.ErrorList.GetErrorListContents();
            Assert.AreEqual(expectedContents, actualContents);

            VisualStudioInstance.Editor.Activate();
            VisualStudioInstance.Editor.PlaceCaret("FF = 0 ' Comment", charsOffset: -1);
            VisualStudioInstance.Editor.SendKeys(VirtualKey.Delete);
            VisualStudioInstance.ErrorList.ShowErrorList();
            expectedContents = new ErrorListItem[] { };
            actualContents = VisualStudioInstance.ErrorList.GetErrorListContents();
            Assert.AreEqual(expectedContents, actualContents);
        }
    }
}
