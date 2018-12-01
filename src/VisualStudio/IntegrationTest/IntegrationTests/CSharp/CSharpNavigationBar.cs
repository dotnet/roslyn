// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpNavigationBar : AbstractEditorTest
    {
        private const string TestSource = @"
class C
{
    public void M(int i) { }
    private C $$this[int index] { get { return null; } set { } }
    public static bool operator ==(C c1, C c2) { return true; }
    public static bool operator !=(C c1, C c2) { return false; }
}

struct S
{
    int Goo() { }
    void Bar() { }
}";

        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpNavigationBar( )
            : base( nameof(CSharpNavigationBar))
        {
        }

        [TestMethod, TestCategory(Traits.Features.NavigationBar)]
        public void VerifyNavBar()
        {
            SetUpEditor(TestSource);
            VisualStudioInstance.Editor.PlaceCaret("this", charsOffset: 1);
            VisualStudioInstance.Editor.ExpandMemberNavBar();
            var expectedItems = new[]
            {
                "M(int i)",
                "operator !=(C c1, C c2)",
                "operator ==(C c1, C c2)",
                "this[int index]"
            };

            Assert.AreEqual(expectedItems, VisualStudioInstance.Editor.GetMemberNavBarItems());
            VisualStudioInstance.Editor.SelectMemberNavBarItem("operator !=(C c1, C c2)");

            VisualStudioInstance.Editor.Verify.CurrentLineText("public static bool operator $$!=(C c1, C c2) { return false; }", assertCaretPosition: true, trimWhitespace: true);
        }

        [TestMethod, TestCategory(Traits.Features.NavigationBar)]
        public void VerifyNavBar2()
        {
            SetUpEditor(TestSource);

            VerifyLeftSelected("C");
            VerifyRightSelected("this[int index]");

            VisualStudioInstance.Editor.ExpandTypeNavBar();
            var expectedItems = new[]
            {
                "C",
                "S",
            };

            VisualStudioInstance.Editor.SelectTypeNavBarItem("S");

            VerifyLeftSelected("S");
            VerifyRightSelected("Goo()");
            VisualStudioInstance.Editor.Verify.CurrentLineText("$$struct S", assertCaretPosition: true, trimWhitespace: true);
        }

        [TestMethod, TestCategory(Traits.Features.NavigationBar)]
        public void VerifyNavBar3()
        {
            SetUpEditor(@"
struct S$$
{
    int Goo() { }
    void Bar() { }
}");
            VisualStudioInstance.Editor.ExpandMemberNavBar();
            var expectedItems = new[]
            {
                "Bar()",
                "Goo()",
            };
            Assert.AreEqual(expectedItems, VisualStudioInstance.Editor.GetMemberNavBarItems());
            VisualStudioInstance.Editor.SelectMemberNavBarItem("Bar()");
            VisualStudioInstance.Editor.Verify.CurrentLineText("void $$Bar() { }", assertCaretPosition: true, trimWhitespace: true);

            VisualStudioInstance.ExecuteCommand("Edit.LineUp");
            VerifyRightSelected("Goo()");
        }

        [TestMethod, TestCategory(Traits.Features.NavigationBar)]
        public void TestSplitWindow()
        {
            VisualStudioInstance.Editor.SetText(@"
class C
{
    public void M(int i) { }
    private C this[int index] { get { return null; } set { } }
}

struct S
{
    int Goo() { }
    void Bar() { }
}");
            VisualStudioInstance.ExecuteCommand("Window.Split");
            VisualStudioInstance.Editor.PlaceCaret("this", charsOffset: 1);
            VerifyLeftSelected("C");
            VerifyRightSelected("this[int index]");
            VisualStudioInstance.ExecuteCommand("Window.NextSplitPane");
            VisualStudioInstance.Editor.PlaceCaret("Goo", charsOffset: 1);
            VerifyLeftSelected("S");
            VerifyRightSelected("Goo()");
        }

        [TestMethod, TestCategory(Traits.Features.NavigationBar)]
        public void VerifyOption()
        {
            VisualStudioInstance.Workspace.SetFeatureOption("NavigationBarOptions", "ShowNavigationBar", "C#", "False");
            Assert.IsFalse(VisualStudioInstance.Editor.IsNavBarEnabled());

            VisualStudioInstance.Workspace.SetFeatureOption("NavigationBarOptions", "ShowNavigationBar", "C#", "True");
            Assert.IsTrue(VisualStudioInstance.Editor.IsNavBarEnabled());
        }

        private void VerifyLeftSelected(string expected)
        {
            Assert.AreEqual(expected, VisualStudioInstance.Editor.GetTypeNavBarSelection());
        }

        private void VerifyRightSelected(string expected)
        {
            Assert.AreEqual(expected, VisualStudioInstance.Editor.GetMemberNavBarSelection());
        }
    }
}
