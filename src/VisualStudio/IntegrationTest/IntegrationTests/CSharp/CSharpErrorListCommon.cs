// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    public abstract class CSharpErrorListCommon : AbstractIdeEditorTest
    {
        public CSharpErrorListCommon(string templateName)
            : base(nameof(CSharpErrorListCommon), templateName)
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        public virtual async Task ErrorListAsync()
        {
            await Editor.SetTextAsync(@"
class C
{
    void M(P p)
    {
        System.Console.WriteLin();
    }

    static void Main(string[] args)
    {
    }
}
");
            await ErrorList.ShowErrorListAsync();
            var expectedContents = new[] {
                new ErrorListItem(
                    severity: "Error",
                    description: "The type or namespace name 'P' could not be found (are you missing a using directive or an assembly reference?)",
                    project: "TestProj.csproj",
                    fileName: "Class1.cs",
                    line: 4,
                    column: 12),
                new ErrorListItem(
                    severity: "Error",
                    description: "'Console' does not contain a definition for 'WriteLin'",
                    project: "TestProj.csproj",
                    fileName: "Class1.cs",
                    line: 6,
                    column: 24)
            };
            var actualContents = await ErrorList.GetErrorListContentsAsync();
            Assert.Equal(expectedContents, actualContents);
            await ErrorList.NavigateToErrorListItemAsync(0);
            await Editor.Verify.CaretPositionAsync(25);
            await SolutionExplorer.BuildSolutionAsync(waitForBuildToFinish: true);
            await ErrorList.ShowErrorListAsync();
            actualContents = await ErrorList.GetErrorListContentsAsync();
            Assert.Equal(expectedContents, actualContents);
        }

        public virtual async Task ErrorLevelWarningAsync()
        {
            await Editor.SetTextAsync(@"
class C
{
    static void Main(string[] args)
    {
        int unused = 0;
    }
}
");
            await ErrorList.ShowErrorListAsync();
            var expectedContents = new[] {
                new ErrorListItem(
                    severity: "Warning",
                    description: "The variable 'unused' is assigned but its value is never used",
                    project: "TestProj.csproj",
                    fileName: "Class1.cs",
                    line: 6,
                    column: 13)
            };
            var actualContents = await ErrorList.GetErrorListContentsAsync();
            Assert.Equal(expectedContents, actualContents);
        }

        public virtual async Task ErrorsDuringMethodBodyEditingAsync()
        {
            await Editor.SetTextAsync(@"
using System;

class Program2
{
    static void Main(string[] args)
    {
        Func<int, int> a = aa => 7;
    }
}
");
            await ErrorList.ShowErrorListAsync();
            var expectedContents = new ErrorListItem[] { };
            var actualContents = await ErrorList.GetErrorListContentsAsync();
            Assert.Equal(expectedContents, actualContents);

            await Editor.ActivateAsync();
            await Editor.PlaceCaretAsync("a = aa", charsOffset: -1);
            await Editor.SendKeysAsync("a");
            await ErrorList.ShowErrorListAsync();
            expectedContents = new[] {
                new ErrorListItem(
                    severity: "Error",
                    description: "A local or parameter named 'aa' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter",
                    project: "TestProj.csproj",
                    fileName: "Class1.cs",
                    line: 8,
                    column: 29)
            };
            actualContents = await ErrorList.GetErrorListContentsAsync();
            Assert.Equal(expectedContents, actualContents);

            await Editor.ActivateAsync();
            await Editor.PlaceCaretAsync("aa = aa", charsOffset: -1);
            await Editor.SendKeysAsync(VirtualKey.Delete);
            await ErrorList.ShowErrorListAsync();
            expectedContents = new ErrorListItem[] { };
            actualContents = await ErrorList.GetErrorListContentsAsync();
            Assert.Equal(expectedContents, actualContents);
        }
    }
}
