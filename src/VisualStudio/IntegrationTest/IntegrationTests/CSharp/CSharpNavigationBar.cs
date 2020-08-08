// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
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

        public CSharpNavigationBar(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpNavigationBar))
        {
        }

        public override async Task DisposeAsync()
        {
            VisualStudio.Workspace.SetFeatureOption("NavigationBarOptions", "ShowNavigationBar", "C#", "True");
            await base.DisposeAsync();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public void VerifyNavBar()
        {
            SetUpEditor(TestSource);
            VisualStudio.Editor.PlaceCaret("this", charsOffset: 1);
            VisualStudio.Editor.ExpandMemberNavBar();
            var expectedItems = new[]
            {
                "M(int i)",
                "operator !=(C c1, C c2)",
                "operator ==(C c1, C c2)",
                "this[int index]"
            };

            Assert.Equal(expectedItems, VisualStudio.Editor.GetMemberNavBarItems());
            VisualStudio.Editor.SelectMemberNavBarItem("operator !=(C c1, C c2)");

            VisualStudio.Editor.Verify.CurrentLineText("public static bool operator $$!=(C c1, C c2) { return false; }", assertCaretPosition: true, trimWhitespace: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public void VerifyNavBar2()
        {
            SetUpEditor(TestSource);

            VerifyLeftSelected("C");
            VerifyRightSelected("this[int index]");

            VisualStudio.Editor.ExpandTypeNavBar();

            VisualStudio.Editor.SelectTypeNavBarItem("S");

            VerifyLeftSelected("S");
            VerifyRightSelected("Goo()");
            VisualStudio.Editor.Verify.CurrentLineText("struct $$S", assertCaretPosition: true, trimWhitespace: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public void VerifyNavBar3()
        {
            SetUpEditor(@"
struct S$$
{
    int Goo() { }
    void Bar() { }
}");
            VisualStudio.Editor.ExpandMemberNavBar();
            var expectedItems = new[]
            {
                "Bar()",
                "Goo()",
            };
            Assert.Equal(expectedItems, VisualStudio.Editor.GetMemberNavBarItems());
            VisualStudio.Editor.SelectMemberNavBarItem("Bar()");
            VisualStudio.Editor.Verify.CurrentLineText("void $$Bar() { }", assertCaretPosition: true, trimWhitespace: true);

            VisualStudio.ExecuteCommand("Edit.LineUp");
            VerifyRightSelected("Goo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
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
    int Goo() { }
    void Bar() { }
}");
            VisualStudio.ExecuteCommand("Window.Split");
            VisualStudio.Editor.PlaceCaret("this", charsOffset: 1);
            VerifyLeftSelected("C");
            VerifyRightSelected("this[int index]");
            VisualStudio.ExecuteCommand("Window.NextSplitPane");
            VisualStudio.Editor.PlaceCaret("Goo", charsOffset: 1);
            VerifyLeftSelected("S");
            VerifyRightSelected("Goo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public void VerifyOption()
        {
            VisualStudio.Workspace.SetFeatureOption("NavigationBarOptions", "ShowNavigationBar", "C#", "False");
            Assert.False(VisualStudio.Editor.IsNavBarEnabled());

            VisualStudio.Workspace.SetFeatureOption("NavigationBarOptions", "ShowNavigationBar", "C#", "True");
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
