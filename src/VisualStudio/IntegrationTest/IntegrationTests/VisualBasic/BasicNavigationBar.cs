﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Basic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicNavigationBar : AbstractEditorTest
    {
        private const string TestSource = @"
Class C
    Public WithEvents Domain As AppDomain
    Public Sub Foo()
    End Sub
End Class

Structure S
    Public Property A As Integer
    Public Property B As Integer
End Structure";

        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicNavigationBar(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicNavigationBar))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public void VerifyNavBar()
        {
            VisualStudio.Editor.SetText(TestSource);

            VisualStudio.Editor.PlaceCaret("Foo", charsOffset: 1);

            VerifyLeftSelected("C");
            VerifyRightSelected("Foo");

            VisualStudio.Editor.ExpandTypeNavBar();
            var expectedItems = new[]
            {
                "C",
                "Domain",
                "S"
            };

            Assert.Equal(expectedItems, VisualStudio.Editor.GetTypeNavBarItems());

            VisualStudio.Editor.SelectTypeNavBarItem("S");

            VisualStudio.Editor.Verify.CaretPosition(112);
            VisualStudio.Editor.Verify.CurrentLineText("Structure S$$", assertCaretPosition: true);

            VisualStudio.ExecuteCommand("Edit.LineDown");
            VerifyRightSelected("A");

            VisualStudio.Editor.ExpandMemberNavBar();
            expectedItems = new[]
            {
                "A",
                "B",
            };

            Assert.Equal(expectedItems, VisualStudio.Editor.GetMemberNavBarItems());
            VisualStudio.Editor.SelectMemberNavBarItem("B");
            VisualStudio.Editor.Verify.CaretPosition(169);
            VisualStudio.Editor.Verify.CurrentLineText("Public Property $$B As Integer", assertCaretPosition: true, trimWhitespace: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public void CodeSpit()
        {
            VisualStudio.Editor.SetText(TestSource);

            VisualStudio.Editor.PlaceCaret("C", charsOffset: 1);
            VerifyLeftSelected("C");
            VisualStudio.Editor.ExpandMemberNavBar();
            Assert.Equal(new[] { "New", "Finalize", "Foo" }, VisualStudio.Editor.GetMemberNavBarItems());
            VisualStudio.Editor.SelectMemberNavBarItem("New");
            VisualStudio.Editor.Verify.TextContains(@"
    Public Sub New()

    End Sub");
            VisualStudio.Editor.Verify.CaretPosition(78); // Caret is between New() and End Sub() in virtual whitespace
            VisualStudio.Editor.Verify.CurrentLineText("$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public void VerifyOption()
        {
            VisualStudio.Workspace.SetFeatureOption("NavigationBarOptions", "ShowNavigationBar", "Visual Basic", "False");
            Assert.False(VisualStudio.Editor.IsNavBarEnabled());

            VisualStudio.Workspace.SetFeatureOption("NavigationBarOptions", "ShowNavigationBar", "Visual Basic", "True");
            Assert.True(VisualStudio.Editor.IsNavBarEnabled());
        }

        private void VerifyLeftSelected(string expected)
        {
            Assert.Equal(expected, VisualStudio.Editor.GetTypeNavBarSelection());
        }

        private void VerifyRightSelected(string expected)
        {
            Assert.Equal(expected, VisualStudio.Editor.GetMemberNavBarSelection());
        }
    }
}