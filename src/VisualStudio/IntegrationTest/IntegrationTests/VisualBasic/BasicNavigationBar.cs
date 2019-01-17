// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.VisualStudio.IntegrationTests;

namespace Roslyn.VisualStudio.IntegrationTests.Basic
{
    [TestClass]
    public class BasicNavigationBar : AbstractEditorTest
    {
        private const string TestSource = @"
Class C
    Public WithEvents Domain As AppDomain
    Public Sub $$Goo()
    End Sub
End Class

Structure S
    Public Property A As Integer
    Public Property B As Integer
End Structure";

        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicNavigationBar()
            : base(nameof(BasicNavigationBar))
        {
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.NavigationBar)]
        public void VerifyNavBar()
        {
            SetUpEditor(TestSource);

            VisualStudioInstance.Editor.PlaceCaret("Goo", charsOffset: 1);

            VerifyLeftSelected("C");
            VerifyRightSelected("Goo");

            VisualStudioInstance.Editor.ExpandTypeNavBar();
            var expectedItems = new[]
            {
                "C",
                "Domain",
                "S"
            };

            Assert.AreEqual(expectedItems, VisualStudioInstance.Editor.GetTypeNavBarItems());

            VisualStudioInstance.Editor.SelectTypeNavBarItem("S");

            VisualStudioInstance.Editor.Verify.CaretPosition(112);
            VisualStudioInstance.Editor.Verify.CurrentLineText("Structure S$$", assertCaretPosition: true);

            VisualStudioInstance.ExecuteCommand("Edit.LineDown");
            VerifyRightSelected("A");

            VisualStudioInstance.Editor.ExpandMemberNavBar();
            expectedItems = new[]
            {
                "A",
                "B",
            };

            Assert.AreEqual(expectedItems, VisualStudioInstance.Editor.GetMemberNavBarItems());
            VisualStudioInstance.Editor.SelectMemberNavBarItem("B");
            VisualStudioInstance.Editor.Verify.CaretPosition(169);
            VisualStudioInstance.Editor.Verify.CurrentLineText("Public Property $$B As Integer", assertCaretPosition: true, trimWhitespace: true);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.NavigationBar)]
        public void CodeSpit()
        {
            SetUpEditor(TestSource);

            VisualStudioInstance.Editor.PlaceCaret("C", charsOffset: 1);
            VerifyLeftSelected("C");
            VisualStudioInstance.Editor.ExpandMemberNavBar();
            Assert.AreEqual(new[] { "New", "Finalize", "Goo" }, VisualStudioInstance.Editor.GetMemberNavBarItems());
            VisualStudioInstance.Editor.SelectMemberNavBarItem("New");
            VisualStudioInstance.Editor.Verify.TextContains(@"
    Public Sub New()

    End Sub");
            VisualStudioInstance.Editor.Verify.CaretPosition(78); // Caret is between New() and End Sub() in virtual whitespace
            VisualStudioInstance.Editor.Verify.CurrentLineText("$$", assertCaretPosition: true);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.NavigationBar)]
        public void VerifyOption()
        {
            VisualStudioInstance.Workspace.SetFeatureOption("NavigationBarOptions", "ShowNavigationBar", "Visual Basic", "False");
            Assert.IsFalse(VisualStudioInstance.Editor.IsNavBarEnabled());

            VisualStudioInstance.Workspace.SetFeatureOption("NavigationBarOptions", "ShowNavigationBar", "Visual Basic", "True");
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
