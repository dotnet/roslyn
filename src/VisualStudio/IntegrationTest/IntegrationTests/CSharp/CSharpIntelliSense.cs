﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AtNamespaceLevel()
        {
            SetUpEditor(@"$$");

            VisualStudio.Editor.SendKeys("usi");
            VisualStudio.Editor.Verify.CompletionItemsExist("using");

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("using$$", assertCaretPosition: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SpeculativeTInList()
        {
            SetUpEditor(@"
class C
{
    $$
}");

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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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

            VisualStudio.Editor.SendKeys('.');
            VisualStudio.Editor.Verify.CompletionItemsExist("Search", "Navigate");

            VisualStudio.Editor.SendKeys('S', VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("NavigateTo.Search$$", assertCaretPosition: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CtrlAltSpace()
        {
            VisualStudio.Workspace.SetUseSuggestionMode(false);

            VisualStudio.Editor.SendKeys("nam Goo", VirtualKey.Enter);
            VisualStudio.Editor.SendKeys('{', VirtualKey.Enter, '}', VirtualKey.Up, VirtualKey.Enter);
            VisualStudio.Editor.SendKeys("pu cla Program", VirtualKey.Enter);
            VisualStudio.Editor.SendKeys('{', VirtualKey.Enter, '}', VirtualKey.Up, VirtualKey.Enter);
            VisualStudio.Editor.SendKeys("pub stati voi Main(string[] args)", VirtualKey.Enter);
            VisualStudio.Editor.SendKeys('{', VirtualKey.Enter, '}', VirtualKey.Up, VirtualKey.Enter);
            VisualStudio.Editor.SendKeys("System.Console.writeline();");
            VisualStudio.Editor.Verify.CurrentLineText("System.Console.WriteLine();$$", assertCaretPosition: true);
            VisualStudio.Editor.SendKeys(VirtualKey.Home, Shift(VirtualKey.End), VirtualKey.Delete);

            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_ToggleCompletionMode);

            VisualStudio.Editor.SendKeys("System.Console.writeline();");
            VisualStudio.Editor.Verify.CurrentLineText("System.Console.writeline();$$", assertCaretPosition: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CtrlAltSpaceOption()
        {
            VisualStudio.Workspace.SetUseSuggestionMode(false);

            VisualStudio.Editor.SendKeys("nam Goo");
            VisualStudio.Editor.Verify.CurrentLineText("namespace Goo$$", assertCaretPosition: true);

            ClearEditor();
            VisualStudio.Workspace.SetUseSuggestionMode(true);

            VisualStudio.Editor.SendKeys("nam Goo");
            VisualStudio.Editor.Verify.CurrentLineText("nam Goo$$", assertCaretPosition: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CtrlSpace()
        {
            SetUpEditor("class c { void M() {$$ } }");
            VisualStudio.Editor.SendKeys(Ctrl(VirtualKey.Space));
            VisualStudio.Editor.Verify.CompletionItemsExist("System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NavigatingWithDownKey()
        {
            SetUpEditor("class c { void M() {$$ } }");
            VisualStudio.Editor.SendKeys('c');
            VisualStudio.Editor.Verify.CurrentCompletionItem("c");
            VisualStudio.Editor.Verify.CompletionItemsExist("c");

            VisualStudio.Editor.SendKeys(VirtualKey.Down);
            VisualStudio.Editor.Verify.CurrentCompletionItem("char");
            VisualStudio.Editor.Verify.CompletionItemsExist("char");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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

            VisualStudio.Editor.SendKeys("<s");
            VisualStudio.Editor.Verify.CompletionItemsExist("see", "seealso", "summary");

            VisualStudio.Editor.SendKeys(VirtualKey.Enter);
            VisualStudio.Editor.Verify.CurrentLineText("///<see cref=\"$$\"/>", assertCaretPosition: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void XmlTagCompletion()
        {
            SetUpEditor(@"
/// $$
class C { }
");

            VisualStudio.Editor.SendKeys("<summary>");
            VisualStudio.Editor.Verify.CurrentLineText("/// <summary>$$</summary>", assertCaretPosition: true);

            SetUpEditor(@"
/// <summary>$$
class C { }
");

            VisualStudio.Editor.SendKeys("</");
            VisualStudio.Editor.Verify.CurrentLineText("/// <summary></summary>$$", assertCaretPosition: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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

            VisualStudio.Workspace.SetUseSuggestionMode(false);

            VisualStudio.Editor.SendKeys("Mai(");

            VisualStudio.Editor.Verify.CurrentSignature("void Class1.Main(string[] args)");
            VisualStudio.Editor.Verify.CurrentParameter("args", "");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CompletionUsesTrackingPointsInTheFaceOfAutomaticBraceCompletion()
        {
            SetUpEditor(@"
class Class1
{
    void Main(string[] args)
    $$
}");

            VisualStudio.Workspace.SetUseSuggestionMode(false);

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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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

            VisualStudio.Workspace.SetUseSuggestionMode(false);

            VisualStudio.Editor.SendKeys(
                'M',
                Shift(VirtualKey.Enter));

            VisualStudio.Editor.Verify.TextContains(@"
class Class1
{
    void Main(string[] args)
    {
        Main$$
    }
}",
assertCaretPosition: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitOnLeftCurly()
        {
            SetUpEditor(@"
class Class1
{
    $$
}");

            VisualStudio.Workspace.SetUseSuggestionMode(false);

            VisualStudio.Editor.SendKeys("int P { g{");

            VisualStudio.Editor.Verify.TextContains(@"
class Class1
{
    int P { get { $$} }
}",
assertCaretPosition: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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

            VisualStudio.Editor.SendKeys(
                VirtualKey.Delete,
                "aaa",
                VirtualKey.Tab);
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa = aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", actualText);
            Assert.True(VisualStudio.Editor.IsCaretOnScreen());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DismissOnSelect()
        {
            SetUpEditor(@"$$");

            VisualStudio.Editor.SendKeys(Ctrl(VirtualKey.Space));
            Assert.Equal(true, VisualStudio.Editor.IsCompletionActive());

            VisualStudio.Editor.SendKeys(Ctrl(VirtualKey.A));
            Assert.Equal(false, VisualStudio.Editor.IsCompletionActive());
        }
    }
}
