// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpIntelliSense : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpIntelliSense(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpIntelliSense))
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);

            // Disable import completion.
            VisualStudio.Workspace.SetImportCompletionOption(false);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AtNamespaceLevel(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"$$");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("usi");
            VisualStudio.Editor.Verify.CompletionItemsExist("using");

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("using$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SpeculativeTInList(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class C
{
    $$
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("pub");
            VisualStudio.Editor.Verify.CompletionItemsExist("public");

            VisualStudio.Editor.SendKeys(' ');
            VisualStudio.Editor.Verify.CurrentLineText("public $$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys('t');
            VisualStudio.Editor.Verify.CompletionItemsExist("T");

            VisualStudio.Editor.SendKeys(' ');
            VisualStudio.Editor.SendKeys("Goo<T>() { }");
            VisualStudio.Editor.Verify.TextContains(@"
class C
{
    public T Goo<T>() { }$$
}",
assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)]
        public void VerifyCompletionListMembersOnStaticTypesAndCompleteThem(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
public class Program
{
    static void Main(string[] args)
    {
        NavigateTo$$
    }
}

public static class NavigateTo
{
    public static void Search(string s){ }
    public static void Navigate(int i){ }
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys('.');
            VisualStudio.Editor.Verify.CompletionItemsExist("Search", "Navigate");

            VisualStudio.Editor.SendKeys('S', VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("NavigateTo.Search$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CtrlAltSpace(bool showCompletionInArgumentLists)
        {
            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SetUseSuggestionMode(false);

            VisualStudio.Editor.SendKeys("nam");
            Assert.True(VisualStudio.Editor.IsCompletionActive());

            VisualStudio.Editor.SendKeys(" Goo", VirtualKey.Enter);
            VisualStudio.Editor.SendKeys('{', VirtualKey.Enter, '}', VirtualKey.Up, VirtualKey.Enter);

            VisualStudio.Editor.SendKeys("pu");
            Assert.True(VisualStudio.Editor.IsCompletionActive());

            VisualStudio.Editor.SendKeys(" cla");
            Assert.True(VisualStudio.Editor.IsCompletionActive());

            VisualStudio.Editor.SendKeys(" Program", VirtualKey.Enter);
            VisualStudio.Editor.SendKeys('{', VirtualKey.Enter, '}', VirtualKey.Up, VirtualKey.Enter);

            VisualStudio.Editor.SendKeys("pub");
            Assert.True(VisualStudio.Editor.IsCompletionActive());

            VisualStudio.Editor.SendKeys(" stati");
            Assert.True(VisualStudio.Editor.IsCompletionActive());

            VisualStudio.Editor.SendKeys(" voi");
            Assert.True(VisualStudio.Editor.IsCompletionActive());

            VisualStudio.Editor.SendKeys(" Main(string[] args)", VirtualKey.Enter);
            VisualStudio.Editor.SendKeys('{', VirtualKey.Enter, '}', VirtualKey.Up, VirtualKey.Enter);

            VisualStudio.Editor.SendKeys("System.Console.");
            Assert.True(VisualStudio.Editor.IsCompletionActive());

            VisualStudio.Editor.SendKeys("writeline();");
            VisualStudio.Editor.Verify.CurrentLineText("System.Console.WriteLine();$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Home, Shift(VirtualKey.End), VirtualKey.Delete);
            VisualStudio.Editor.SendKeys(new KeyPress(VirtualKey.Space, ShiftState.Ctrl | ShiftState.Alt));

            VisualStudio.Editor.SendKeys("System.Console.");
            Assert.True(VisualStudio.Editor.IsCompletionActive());

            VisualStudio.Editor.SendKeys("writeline();");
            VisualStudio.Editor.Verify.CurrentLineText("System.Console.writeline();$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CtrlAltSpaceOption(bool showCompletionInArgumentLists)
        {
            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SetUseSuggestionMode(false);

            VisualStudio.Editor.SendKeys("names");
            Assert.True(VisualStudio.Editor.IsCompletionActive());

            VisualStudio.Editor.SendKeys(" Goo");
            VisualStudio.Editor.Verify.CurrentLineText("namespace Goo$$", assertCaretPosition: true);

            ClearEditor();
            VisualStudio.Editor.SetUseSuggestionMode(true);

            VisualStudio.Editor.SendKeys("nam");
            Assert.True(VisualStudio.Editor.IsCompletionActive());

            VisualStudio.Editor.SendKeys(" Goo");
            VisualStudio.Editor.Verify.CurrentLineText("nam Goo$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CtrlSpace(bool showCompletionInArgumentLists)
        {
            SetUpEditor("class c { void M() {$$ } }");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys(Ctrl(VirtualKey.Space));
            VisualStudio.Editor.Verify.CompletionItemsExist("System");
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NavigatingWithDownKey(bool showCompletionInArgumentLists)
        {
            SetUpEditor("class c { void M() {$$ } }");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys('c');
            VisualStudio.Editor.Verify.CurrentCompletionItem("c");
            VisualStudio.Editor.Verify.CompletionItemsExist("c");

            VisualStudio.Editor.SendKeys(VirtualKey.Down);
            VisualStudio.Editor.Verify.CurrentCompletionItem("char");
            VisualStudio.Editor.Verify.CompletionItemsExist("char");
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)]
        public void XmlDocCommentIntelliSense(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class Class1
{
    ///$$
    void Main(string[] args)
    {
    
    }
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("<s");
            VisualStudio.Editor.Verify.CompletionItemsExist("see", "seealso", "summary");

            // 🐛 Workaround for https://github.com/dotnet/roslyn/issues/33824
            var completionItems = VisualStudio.Editor.GetCompletionItems();
            var targetIndex = Array.IndexOf(completionItems, "see");
            var currentIndex = Array.IndexOf(completionItems, VisualStudio.Editor.GetCurrentCompletionItem());
            if (currentIndex != targetIndex)
            {
                var key = currentIndex < targetIndex ? VirtualKey.Down : VirtualKey.Up;
                var keys = Enumerable.Repeat(key, Math.Abs(currentIndex - targetIndex)).Cast<object>().ToArray();
                VisualStudio.Editor.SendKeys(keys);
            }

            VisualStudio.Editor.SendKeys(VirtualKey.Enter);
            VisualStudio.Editor.Verify.CurrentLineText("///<see cref=\"$$\"/>", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)]
        public void XmlTagCompletion(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
/// $$
class C { }
");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("<summary>");
            VisualStudio.Editor.Verify.CurrentLineText("/// <summary>$$</summary>", assertCaretPosition: true);

            SetUpEditor(@"
/// <summary>$$
class C { }
");

            VisualStudio.Editor.SendKeys("</");
            VisualStudio.Editor.Verify.CurrentLineText("/// <summary></summary>$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SignatureHelpShowsUp(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class Class1
{
    void Main(string[] args)
    {
        $$
    }
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SetUseSuggestionMode(false);

            VisualStudio.Editor.SendKeys("Mai");
            Assert.True(VisualStudio.Editor.IsCompletionActive());

            VisualStudio.Editor.SendKeys("(");

            VisualStudio.Editor.Verify.CurrentSignature("void Class1.Main(string[] args)");
            VisualStudio.Editor.Verify.CurrentParameter("args", "");
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(33825, "https://github.com/dotnet/roslyn/issues/33825")]
        public void CompletionUsesTrackingPointsInTheFaceOfAutomaticBraceCompletion(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class Class1
{
    void Main(string[] args)
    $$
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SetUseSuggestionMode(false);

            VisualStudio.Editor.SendKeys(
                '{',
                VirtualKey.Enter,
                "                 ");

            VisualStudio.Editor.InvokeCompletionList();

            VisualStudio.Editor.SendKeys('}');

            VisualStudio.Editor.Verify.TextContains(@"
class Class1
{
    void Main(string[] args)
    {
    }$$
}",
assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(33823, "https://github.com/dotnet/roslyn/issues/33823")]
        public void CommitOnShiftEnter(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class Class1
{
    void Main(string[] args)
    {
        $$
    }
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SetUseSuggestionMode(false);

            VisualStudio.Editor.SendKeys(
                'M',
                Shift(VirtualKey.Enter));

            VisualStudio.Editor.Verify.TextContains(@"
class Class1
{
    void Main(string[] args)
    {
        Main
$$
    }
}",
assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LineBreakOnShiftEnter(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class Class1
{
    void Main(string[] args)
    {
        $$
    }
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SetUseSuggestionMode(true);

            VisualStudio.Editor.SendKeys(
                'M',
                Shift(VirtualKey.Enter));

            VisualStudio.Editor.Verify.TextContains(@"
class Class1
{
    void Main(string[] args)
    {
        Main
$$
    }
}",
assertCaretPosition: true);

        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitOnLeftCurly(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class Class1
{
    $$
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SetUseSuggestionMode(false);

            VisualStudio.Editor.SendKeys("int P { g");
            Assert.True(VisualStudio.Editor.IsCompletionActive());

            VisualStudio.Editor.SendKeys("{");

            VisualStudio.Editor.Verify.TextContains(@"
class Class1
{
    int P { get { $$} }
}",
assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(33822, "https://github.com/dotnet/roslyn/issues/33822")]
        public void EnsureTheCaretIsVisibleAfterALongEdit(bool showCompletionInArgumentLists)
        {
            var visibleColumns = VisualStudio.Editor.GetVisibleColumnCount();
            var variableName = new string('a', (int)(0.75 * visibleColumns));
            SetUpEditor($@"
public class Program
{{
    static void Main(string[] args)
    {{
        var {variableName} = 0;
        {variableName} = $$
    }}
}}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            Assert.True(variableName.Length > 0);
            VisualStudio.Editor.SendKeys(
                VirtualKey.Delete,
                "aaa",
                VirtualKey.Tab);
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains($"{variableName} = {variableName}", actualText);
            Assert.True(VisualStudio.Editor.IsCaretOnScreen());
            Assert.True(VisualStudio.Editor.GetCaretColumn() > visibleColumns, "This test is inconclusive if the view didn't need to move to keep the caret on screen.");
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DismissOnSelect(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"$$");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys(Ctrl(VirtualKey.Space));
            Assert.True(VisualStudio.Editor.IsCompletionActive());

            VisualStudio.Editor.SendKeys(Ctrl(VirtualKey.A));
            Assert.False(VisualStudio.Editor.IsCompletionActive());
        }
    }
}
