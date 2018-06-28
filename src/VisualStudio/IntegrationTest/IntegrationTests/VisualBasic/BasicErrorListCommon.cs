// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    public class BasicErrorListCommon : AbstractIdeEditorTest
    {
        public BasicErrorListCommon(string templateName)
            : base(nameof(BasicErrorListCommon), templateName)
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        public virtual async Task ErrorListAsync()
        {
            await Editor.SetTextAsync(@"
Module Module1

    Function Good() As P
        Return Nothing
    End Function

    Sub Main()
        Goo()
    End Sub

End Module
");
            await ErrorList.ShowErrorListAsync();
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
            var actualContents = await ErrorList.GetErrorListContentsAsync();
            Assert.Equal(expectedContents, actualContents);
            await ErrorList.NavigateToErrorListItemAsync(0);
            await Editor.Verify.CaretPositionAsync(43);
            await SolutionExplorer.BuildSolutionAsync(waitForBuildToFinish: true);
            await ErrorList.ShowErrorListAsync();
            actualContents = await ErrorList.GetErrorListContentsAsync();
            Assert.Equal(expectedContents, actualContents);
        }

        public virtual async Task ErrorsDuringMethodBodyEditingAsync()
        {
            await Editor.SetTextAsync(@"
Namespace N
    Class C
        Private F As Integer
        Sub S()
             ' Comment
        End Sub
    End Class
End Namespace
");
            await Editor.PlaceCaretAsync(" Comment", charsOffset: -2);
            await SendKeys.SendAsync("F = 0");
            await ErrorList.ShowErrorListAsync();
            var expectedContents = new ErrorListItem[] { };
            var actualContents = await ErrorList.GetErrorListContentsAsync();
            Assert.Equal(expectedContents, actualContents);

            await Editor.ActivateAsync();
            await Editor.PlaceCaretAsync("F = 0 ' Comment", charsOffset: -1);
            await Editor.SendKeysAsync("F");
            await ErrorList.ShowErrorListAsync();
            expectedContents = new ErrorListItem[] {
                new ErrorListItem(
                    severity: "Error",
                    description: "'FF' is not declared. It may be inaccessible due to its protection level.",
                    project: "TestProj.vbproj",
                    fileName: "Class1.vb",
                    line: 6,
                    column: 13)
            };
            actualContents = await ErrorList.GetErrorListContentsAsync();
            Assert.Equal(expectedContents, actualContents);

            await Editor.ActivateAsync();
            await Editor.PlaceCaretAsync("FF = 0 ' Comment", charsOffset: -1);
            await Editor.SendKeysAsync(VirtualKey.Delete);
            await ErrorList.ShowErrorListAsync();
            expectedContents = new ErrorListItem[] { };
            actualContents = await ErrorList.GetErrorListContentsAsync();
            Assert.Equal(expectedContents, actualContents);
        }
    }
}
