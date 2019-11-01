// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    public class CSharpErrorListCommon : AbstractEditorTest
    {
        public CSharpErrorListCommon(VisualStudioInstanceFactory instanceFactor, ITestOutputHelper testOutputHelper, string templateName, string targetFrameworkMoniker = null)
            : base(instanceFactor, testOutputHelper, nameof(CSharpErrorListCommon), templateName, targetFrameworkMoniker)
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        public virtual void ErrorList()
        {
            VisualStudio.Editor.SetText(@"
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
            VisualStudio.ErrorList.ShowErrorList();
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
            var actualContents = VisualStudio.ErrorList.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);
            var target = VisualStudio.ErrorList.NavigateToErrorListItem(0);
            Assert.Equal(expectedContents[0], target);
            VisualStudio.Editor.Verify.CaretPosition(25);
            VisualStudio.SolutionExplorer.BuildSolution(waitForBuildToFinish: true);
            VisualStudio.ErrorList.ShowErrorList();
            actualContents = VisualStudio.ErrorList.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);
        }

        public virtual void ErrorLevelWarning()
        {
            VisualStudio.Editor.SetText(@"
class C
{
    static void Main(string[] args)
    {
        int unused = 0;
    }
}
");
            VisualStudio.ErrorList.ShowErrorList();
            var expectedContents = new[] {
                new ErrorListItem(
                    severity: "Warning",
                    description: "The variable 'unused' is assigned but its value is never used",
                    project: "TestProj.csproj",
                    fileName: "Class1.cs",
                    line: 6,
                    column: 13)
            };
            var actualContents = VisualStudio.ErrorList.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);
        }

        public virtual void ErrorsDuringMethodBodyEditing()
        {
            VisualStudio.Editor.SetText(@"
class Program2
{
    static void Main(string[] args)
    {
        int aa = 7;
        int a = aa;
    }
}
");
            VisualStudio.ErrorList.ShowErrorList();
            var expectedContents = new ErrorListItem[] { };
            var actualContents = VisualStudio.ErrorList.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);

            VisualStudio.Editor.Activate();
            VisualStudio.Editor.PlaceCaret("a = aa", charsOffset: -1);
            VisualStudio.Editor.SendKeys("a");
            VisualStudio.ErrorList.ShowErrorList();
            expectedContents = new[] {
                new ErrorListItem(
                    severity: "Error",
                    description: "A local variable or function named 'aa' is already defined in this scope",
                    project: "TestProj.csproj",
                    fileName: "Class1.cs",
                    line: 7,
                    column: 13)
            };
            actualContents = VisualStudio.ErrorList.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);

            VisualStudio.Editor.Activate();
            VisualStudio.Editor.PlaceCaret("aa = aa", charsOffset: -1);
            VisualStudio.Editor.SendKeys(VirtualKey.Delete);
            VisualStudio.ErrorList.ShowErrorList();
            expectedContents = new ErrorListItem[] { };
            actualContents = VisualStudio.ErrorList.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);
        }
    }
}
