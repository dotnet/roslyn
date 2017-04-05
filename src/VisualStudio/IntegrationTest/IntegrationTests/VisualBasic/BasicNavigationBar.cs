// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
            VisualStudio.Editor.ExpandLeftNavBar();
            var expectedItems = new[]
            {
                "C",
                "Domain",
                "S"
            };

            Assert.Equal(expectedItems, VisualStudio.Editor.GetLeftNavBarItems());

            VisualStudio.Editor.SelectLeftNavBarItem("S");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);

            VisualStudio.Editor.Verify.CaretPosition(112);

            VisualStudio.ExecuteCommand("Edit.LineDown");
            VerifyRightSelected("A");

            VisualStudio.Editor.ExpandRightNavBar();
            expectedItems = new[]
            {
                "A",
                "B",
            };

            Assert.Equal(expectedItems, VisualStudio.Editor.GetRightNavBarItems());
            VisualStudio.Editor.SelectRightNavBarItem("B");
            VisualStudio.Editor.Verify.CaretPosition(169);

        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public void CodeSpit()
        {
            VisualStudio.Editor.SetText(TestSource);

            VisualStudio.Editor.PlaceCaret("C", charsOffset: 1);
            VerifyLeftSelected("C");
            VisualStudio.Editor.ExpandRightNavBar();
            Assert.Equal(new[] { "New", "Finalize", "Foo" }, VisualStudio.Editor.GetRightNavBarItems());
            VisualStudio.Editor.SelectRightNavBarItem("New");
            VisualStudio.Editor.Verify.TextContains(@"
    Public Sub New()

    End Sub");
            VisualStudio.Editor.Verify.CaretPosition(78);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public void VerifyOption()
        {
            VisualStudio.Workspace.SetFeatureOption("NavigationBarOptions", "ShowNavigationBar", "Visual Basic", "False");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
            Assert.False(VisualStudio.Editor.IsNavBarEnabled());

            VisualStudio.Workspace.SetFeatureOption("NavigationBarOptions", "ShowNavigationBar", "Visual Basic", "True");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
            Assert.True(VisualStudio.Editor.IsNavBarEnabled());
        }

        private void VerifyLeftSelected(string expected)
        {
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
            Assert.Equal(expected, VisualStudio.Editor.GetLeftNavBarSelection());
        }

        private void VerifyRightSelected(string expected)
        {
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
            Assert.Equal(expected, VisualStudio.Editor.GetRightNavBarSelection());
        }
    }
}