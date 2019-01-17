// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpErrorListCommon : AbstractEditorTest
    {
        public CSharpErrorListCommon(string templateName)
            : base(nameof(CSharpErrorListCommon), templateName)
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        public virtual void ErrorList()
        {
            VisualStudioInstance.Editor.SetText(@"
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
            VisualStudioInstance.ErrorList.ShowErrorList();
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
            var actualContents = VisualStudioInstance.ErrorList.GetErrorListContents();
            Assert.AreEqual(expectedContents, actualContents);
            var target = VisualStudioInstance.ErrorList.NavigateToErrorListItem(0);
            Assert.AreEqual(expectedContents[0], target);
            VisualStudioInstance.Editor.Verify.CaretPosition(25);
            VisualStudioInstance.SolutionExplorer.BuildSolution(waitForBuildToFinish: true);
            VisualStudioInstance.ErrorList.ShowErrorList();
            actualContents = VisualStudioInstance.ErrorList.GetErrorListContents();
            Assert.AreEqual(expectedContents, actualContents);
        }

        public virtual void ErrorLevelWarning()
        {
            VisualStudioInstance.Editor.SetText(@"
class C
{
    static void Main(string[] args)
    {
        int unused = 0;
    }
}
");
            VisualStudioInstance.ErrorList.ShowErrorList();
            var expectedContents = new[] {
                new ErrorListItem(
                    severity: "Warning",
                    description: "The variable 'unused' is assigned but its value is never used",
                    project: "TestProj.csproj",
                    fileName: "Class1.cs",
                    line: 6,
                    column: 13)
            };
            var actualContents = VisualStudioInstance.ErrorList.GetErrorListContents();
            Assert.AreEqual(expectedContents, actualContents);
        }

        public virtual void ErrorsDuringMethodBodyEditing()
        {
            VisualStudioInstance.Editor.SetText(@"
using System;

class Program2
{
    static void Main(string[] args)
    {
        Func<int, int> a = aa => 7;
    }
}
");
            VisualStudioInstance.ErrorList.ShowErrorList();
            var expectedContents = new ErrorListItem[] { };
            var actualContents = VisualStudioInstance.ErrorList.GetErrorListContents();
            Assert.AreEqual(expectedContents, actualContents);

            VisualStudioInstance.Editor.Activate();
            VisualStudioInstance.Editor.PlaceCaret("a = aa", charsOffset: -1);
            VisualStudioInstance.Editor.SendKeys("a");
            VisualStudioInstance.ErrorList.ShowErrorList();
            expectedContents = new[] {
                new ErrorListItem(
                    severity: "Error",
                    description: "A local or parameter named 'aa' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter",
                    project: "TestProj.csproj",
                    fileName: "Class1.cs",
                    line: 8,
                    column: 29)
            };
            actualContents = VisualStudioInstance.ErrorList.GetErrorListContents();
            Assert.AreEqual(expectedContents, actualContents);

            VisualStudioInstance.Editor.Activate();
            VisualStudioInstance.Editor.PlaceCaret("aa = aa", charsOffset: -1);
            VisualStudioInstance.Editor.SendKeys(VirtualKey.Delete);
            VisualStudioInstance.ErrorList.ShowErrorList();
            expectedContents = new ErrorListItem[] { };
            actualContents = VisualStudioInstance.ErrorList.GetErrorListContents();
            Assert.AreEqual(expectedContents, actualContents);
        }
    }
}
