// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpNavigationBar : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpNavigationBar(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpNavigationBar))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public void VerifyNavBar()
        {
            VisualStudio.Editor.SetText(@"
class C
{
    public void M(int i) { }
    private C this[int index] { get { return null; } set { } }
    public static bool operator ==(C c1, C c2) { return true; }
    public static bool operator !=(C c1, C c2) { return false; }
}

struct S
{
    int Foo() { }
    void Bar() { }
}");
            VisualStudio.Editor.PlaceCaret("this", charsOffset: 1);
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
            VisualStudio.Editor.ExpandRightNavBar();
            var expectedItems = new[]
            {
                "M(int i)",
                "operator !=(C c1, C c2)",
                "operator ==(C c1, C c2)",
                "this[int index]"
            };

            Assert.Equal(expectedItems, VisualStudio.Editor.GetRightNavBarItems());
            VisualStudio.Editor.SelectRightNavBarItem("operator !=(C c1, C c2)");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);

            VisualStudio.Editor.Verify.CaretPosition(205);

            VisualStudio.Editor.PlaceCaret("this", charsOffset: 1);
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);

            VerifyLeftSelected("C");
            VerifyRightSelected("this[int index]");

            VisualStudio.Editor.ExpandLeftNavBar();
            expectedItems = new[]
            {
                "C",
                "S",
            };

            VisualStudio.Editor.SelectLeftNavBarItem("S");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);

            VerifyLeftSelected("S");
            VerifyRightSelected("Foo()");
            VisualStudio.Editor.Verify.CaretPosition(251);

            VisualStudio.Editor.ExpandRightNavBar();
            expectedItems = new[]
            {
                "Bar()",
                "Foo()",
            };
            Assert.Equal(expectedItems, VisualStudio.Editor.GetRightNavBarItems());
            VisualStudio.Editor.SelectRightNavBarItem("Bar()");
            VisualStudio.Editor.Verify.CaretPosition(285);

            VisualStudio.ExecuteCommand("Edit.LineUp");
            VerifyRightSelected("Foo()");

            VisualStudio.Editor.PlaceCaret("int i", charsOffset: 1);
            VerifyLeftSelected("C");
            VerifyRightSelected("M(int i)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public void TestSplitWindow()
        {
            VisualStudio.Editor.SetText(@"
class C
{
    public void M(int i) { }
    private C this[int index] { get { return null; } set { } }
}

struct S
{
    int Foo() { }
    void Bar() { }
}");
            VisualStudio.ExecuteCommand("Window.Split");
            VisualStudio.Editor.PlaceCaret("this", charsOffset: 1);
            VerifyLeftSelected("C");
            VerifyRightSelected("this[int index]");
            VisualStudio.ExecuteCommand("Window.NextSplitPane");
            VisualStudio.Editor.PlaceCaret("Foo", charsOffset: 1);
            VerifyLeftSelected("S");
            VerifyRightSelected("Foo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public void VerifyOption()
        {
            VisualStudio.Workspace.SetFeatureOption("NavigationBarOptions", "ShowNavigationBar", "C#", "False");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
            Assert.False(VisualStudio.Editor.IsNavBarEnabled());

            VisualStudio.Workspace.SetFeatureOption("NavigationBarOptions", "ShowNavigationBar", "C#", "True");
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