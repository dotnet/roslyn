// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpIntelliSense : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpIntelliSense( )
            : base( nameof(CSharpIntelliSense))
        {
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void AtNamespaceLevel()
        {
            SetUpEditor(@"$$");

            VisualStudioInstance.Editor.SendKeys("usi");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("using");

            VisualStudioInstance.Editor.SendKeys(VirtualKey.Tab);
            VisualStudioInstance.Editor.Verify.CurrentLineText("using$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void SpeculativeTInList()
        {
            SetUpEditor(@"
class C
{
    $$
}");

            VisualStudioInstance.Editor.SendKeys("pub");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("public");

            VisualStudioInstance.Editor.SendKeys(' ');
            VisualStudioInstance.Editor.Verify.CurrentLineText("public $$", assertCaretPosition: true);

            VisualStudioInstance.Editor.SendKeys('t');
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("T");

            VisualStudioInstance.Editor.SendKeys(' ');
            VisualStudioInstance.Editor.SendKeys("Goo<T>() { }");
            VisualStudioInstance.Editor.Verify.TextContains(@"
class C
{
    public T Goo<T>() { }$$
}",
assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void VerifyCompletionListMembersOnStaticTypesAndCompleteThem()
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

            VisualStudioInstance.Editor.SendKeys('.');
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("Search", "Navigate");

            VisualStudioInstance.Editor.SendKeys('S', VirtualKey.Tab);
            VisualStudioInstance.Editor.Verify.CurrentLineText("NavigateTo.Search$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void CtrlAltSpace()
        {
            VisualStudioInstance.Workspace.SetUseSuggestionMode(false);

            VisualStudioInstance.Editor.SendKeys("nam Goo", VirtualKey.Enter);
            VisualStudioInstance.Editor.SendKeys('{', VirtualKey.Enter, '}', VirtualKey.Up, VirtualKey.Enter);
            VisualStudioInstance.Editor.SendKeys("pu cla Program", VirtualKey.Enter);
            VisualStudioInstance.Editor.SendKeys('{', VirtualKey.Enter, '}', VirtualKey.Up, VirtualKey.Enter);
            VisualStudioInstance.Editor.SendKeys("pub stati voi Main(string[] args)", VirtualKey.Enter);
            VisualStudioInstance.Editor.SendKeys('{', VirtualKey.Enter, '}', VirtualKey.Up, VirtualKey.Enter);
            VisualStudioInstance.Editor.SendKeys("System.Console.writeline();");
            VisualStudioInstance.Editor.Verify.CurrentLineText("System.Console.WriteLine();$$", assertCaretPosition: true);
            VisualStudioInstance.Editor.SendKeys(VirtualKey.Home, Shift(VirtualKey.End), VirtualKey.Delete);

            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_ToggleCompletionMode);

            VisualStudioInstance.Editor.SendKeys("System.Console.writeline();");
            VisualStudioInstance.Editor.Verify.CurrentLineText("System.Console.writeline();$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void CtrlAltSpaceOption()
        {
            VisualStudioInstance.Workspace.SetUseSuggestionMode(false);

            VisualStudioInstance.Editor.SendKeys("nam Goo");
            VisualStudioInstance.Editor.Verify.CurrentLineText("namespace Goo$$", assertCaretPosition: true);

            ClearEditor();
            VisualStudioInstance.Workspace.SetUseSuggestionMode(true);

            VisualStudioInstance.Editor.SendKeys("nam Goo");
            VisualStudioInstance.Editor.Verify.CurrentLineText("nam Goo$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void CtrlSpace()
        {
            SetUpEditor("class c { void M() {$$ } }");
            VisualStudioInstance.Editor.SendKeys(Ctrl(VirtualKey.Space));
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("System");
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void NavigatingWithDownKey()
        {
            SetUpEditor("class c { void M() {$$ } }");
            VisualStudioInstance.Editor.SendKeys('c');
            VisualStudioInstance.Editor.Verify.CurrentCompletionItem("c");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("c");

            VisualStudioInstance.Editor.SendKeys(VirtualKey.Down);
            VisualStudioInstance.Editor.Verify.CurrentCompletionItem("char");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("char");
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void XmlDocCommentIntelliSense()
        {
            SetUpEditor(@"
class Class1
{
    ///$$
    void Main(string[] args)
    {
    
    }
}");

            VisualStudioInstance.Editor.SendKeys("<s");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("see", "seealso", "summary");

            VisualStudioInstance.Editor.SendKeys(VirtualKey.Enter);
            VisualStudioInstance.Editor.Verify.CurrentLineText("///<see cref=\"$$\"/>", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void XmlTagCompletion()
        {
            SetUpEditor(@"
/// $$
class C { }
");

            VisualStudioInstance.Editor.SendKeys("<summary>");
            VisualStudioInstance.Editor.Verify.CurrentLineText("/// <summary>$$</summary>", assertCaretPosition: true);

            SetUpEditor(@"
/// <summary>$$
class C { }
");

            VisualStudioInstance.Editor.SendKeys("</");
            VisualStudioInstance.Editor.Verify.CurrentLineText("/// <summary></summary>$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void SignatureHelpShowsUp()
        {
            SetUpEditor(@"
class Class1
{
    void Main(string[] args)
    {
        $$
    }
}");

            VisualStudioInstance.Workspace.SetUseSuggestionMode(false);

            VisualStudioInstance.Editor.SendKeys("Mai(");

            VisualStudioInstance.Editor.Verify.CurrentSignature("void Class1.Main(string[] args)");
            VisualStudioInstance.Editor.Verify.CurrentParameter("args", "");
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void CompletionUsesTrackingPointsInTheFaceOfAutomaticBraceCompletion()
        {
            SetUpEditor(@"
class Class1
{
    void Main(string[] args)
    $$
}");

            VisualStudioInstance.Workspace.SetUseSuggestionMode(false);

            VisualStudioInstance.Editor.SendKeys(
                '{',
                VirtualKey.Enter,
                "                 ");

            VisualStudioInstance.Editor.InvokeCompletionList();

            VisualStudioInstance.Editor.SendKeys('}');

            VisualStudioInstance.Editor.Verify.TextContains(@"
class Class1
{
    void Main(string[] args)
    {
    }$$
}",
assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void CommitOnShiftEnter()
        {
            SetUpEditor(@"
class Class1
{
    void Main(string[] args)
    {
        $$
    }
}");

            VisualStudioInstance.Workspace.SetUseSuggestionMode(false);

            VisualStudioInstance.Editor.SendKeys(
                'M',
                Shift(VirtualKey.Enter));

            VisualStudioInstance.Editor.Verify.TextContains(@"
class Class1
{
    void Main(string[] args)
    {
        Main$$
    }
}",
assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void CommitOnLeftCurly()
        {
            SetUpEditor(@"
class Class1
{
    $$
}");

            VisualStudioInstance.Workspace.SetUseSuggestionMode(false);

            VisualStudioInstance.Editor.SendKeys("int P { g{");

            VisualStudioInstance.Editor.Verify.TextContains(@"
class Class1
{
    int P { get { $$} }
}",
assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void EnsureTheCaretIsVisibleAfterALongEdit()
        {
            SetUpEditor(@"
public class Program
{
    static void Main(string[] args)
    {
        var aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa = 0;
        aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa = $$
    }
}");

            VisualStudioInstance.Editor.SendKeys(
                VirtualKey.Delete,
                "aaa",
                VirtualKey.Tab);
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa = aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", actualText);
            Assert.IsTrue(VisualStudioInstance.Editor.IsCaretOnScreen());
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void DismissOnSelect()
        {
            SetUpEditor(@"$$");

            VisualStudioInstance.Editor.SendKeys(Ctrl(VirtualKey.Space));
            Assert.AreEqual(true, VisualStudioInstance.Editor.IsCompletionActive());

            VisualStudioInstance.Editor.SendKeys(Ctrl(VirtualKey.A));
            Assert.AreEqual(false, VisualStudioInstance.Editor.IsCompletionActive());
        }
    }
}
