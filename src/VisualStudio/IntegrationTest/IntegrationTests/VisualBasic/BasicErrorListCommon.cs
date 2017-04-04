// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.VisualStudio.IntegrationTests.Extensions;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Roslyn.VisualStudio.IntegrationTests.Extensions.ErrorList;
using Roslyn.VisualStudio.IntegrationTests.Extensions.SolutionExplorer;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    public class BasicErrorListCommon : AbstractEditorTest
    {
        public BasicErrorListCommon(VisualStudioInstanceFactory instanceFactor, string templateName)
            : base(instanceFactor, nameof(BasicErrorListCommon), templateName)
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        public virtual void ErrorList()
        {
            Editor.SetText(@"
Module Module1

    Function Food() As P
        Return Nothing
    End Function

    Sub Main()
        Foo()
    End Sub

End Module
");
            this.ShowErrorList();
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
                    description: "'Foo' is not declared. It may be inaccessible due to its protection level.",
                    project: "TestProj.vbproj",
                    fileName: "Class1.vb",
                    line: 9,
                    column: 9)
            };
            var actualContents = this.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);
            this.NavigateToErrorListItem(0);
            this.VerifyCaretPosition(43);
            this.BuildSolution(waitForBuildToFinish: true);
            this.ShowErrorList();
            actualContents = this.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);
        }

        public virtual void ErrorsDuringMethodBodyEditing()
        {
            Editor.SetText(@"
Namespace N
    Class C
        Private F As Integer
        Sub S()
             ' Comment
        End Sub
    End Class
End Namespace
");
            this.PlaceCaret(" Comment", charsOffset: -2);
            this.SendKeys("F = 0");
            this.ShowErrorList();
            var expectedContents = new ErrorListItem[] { };
            var actualContents = this.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);

            Editor.Activate();
            this.PlaceCaret("F = 0 ' Comment", charsOffset: -1);
            this.SendKeys("F");
            this.ShowErrorList();
            expectedContents = new ErrorListItem[] {
                new ErrorListItem(
                    severity: "Error",
                    description: "'FF' is not declared. It may be inaccessible due to its protection level.",
                    project: "TestProj.vbproj",
                    fileName: "Class1.vb",
                    line: 6,
                    column: 13)
            };
            actualContents = this.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);

            Editor.Activate();
            this.PlaceCaret("FF = 0 ' Comment", charsOffset: -1);
            this.SendKeys(VirtualKey.Delete);
            this.ShowErrorList();
            expectedContents = new ErrorListItem[] { };
            actualContents = this.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);
        }
    }
}
