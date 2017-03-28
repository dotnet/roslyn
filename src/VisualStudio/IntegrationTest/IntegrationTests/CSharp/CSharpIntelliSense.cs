// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Options;
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

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AtNamespaceLevel()
        {
            SetUpEditor(@"$$");

            this.SendKeys("usi");
            this.VerifyCompletionItemExists("using");

            this.SendKeys(VirtualKey.Tab);
            this.VerifyCurrentLineText("using$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SpeculativeTInList()
        {
            SetUpEditor(@"
class C
{
    $$
}");

            this.SendKeys("pub");
            this.VerifyCompletionItemExists("public");

            this.SendKeys(' ');
            this.VerifyCurrentLineText("public $$", assertCaretPosition: true);

            this.SendKeys('t');
            this.VerifyCompletionItemExists("T");

            this.SendKeys(' ');
            this.SendKeys("Foo<T>() { }");
            this.VerifyTextContains(@"
class C
{
    public T Foo<T>() { }$$
}",
assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
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

            this.SendKeys('.');
            this.VerifyCompletionItemExists("Search", "Navigate");

            this.SendKeys('S', VirtualKey.Tab);
            this.VerifyCurrentLineText("NavigateTo.Search$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CtrlAltSpace()
        {
            this.SetUseSuggestionMode(false);

            this.SendKeys("nam Foo", VirtualKey.Enter);
            this.SendKeys('{', VirtualKey.Enter, '}', VirtualKey.Up, VirtualKey.Enter);
            this.SendKeys("pu cla Program", VirtualKey.Enter);
            this.SendKeys('{', VirtualKey.Enter, '}', VirtualKey.Up, VirtualKey.Enter);
            this.SendKeys("pub stati voi Main(string[] args)", VirtualKey.Enter);
            this.SendKeys('{', VirtualKey.Enter, '}', VirtualKey.Up, VirtualKey.Enter);
            this.SendKeys("System.Console.writeline();");
            this.VerifyCurrentLineText("System.Console.WriteLine();$$", assertCaretPosition: true);
            this.SendKeys(VirtualKey.Home, Shift(VirtualKey.End), VirtualKey.Delete);

            this.ExecuteCommand(WellKnownCommandNames.Edit_ToggleCompletionMode);

            this.SendKeys("System.Console.writeline();");
            this.VerifyCurrentLineText("System.Console.writeline();$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CtrlAltSpaceOption()
        {
            this.SetUseSuggestionMode(false);

            this.SendKeys("nam Foo");
            this.VerifyCurrentLineText("namespace Foo$$", assertCaretPosition: true);

            ClearEditor();
            this.SetUseSuggestionMode(true);

            this.SendKeys("nam Foo");
            this.VerifyCurrentLineText("nam Foo$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CtrlSpace()
        {
            SetUpEditor("class c { void M() {$$ } }");
            this.SendKeys(Ctrl(VirtualKey.Space));
            this.VerifyCompletionItemExists("System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NavigatingWithDownKey()
        {
            SetUpEditor("class c { void M() {$$ } }");
            this.SendKeys('c');
            this.VerifyCurrentCompletionItem("c");
            this.VerifyCompletionItemExists("c");

            this.SendKeys(VirtualKey.Down);
            this.VerifyCurrentCompletionItem("char");
            this.VerifyCompletionItemExists("char");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
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

            this.SendKeys("<s");
            this.VerifyCompletionItemExists("see", "seealso", "summary");

            this.SendKeys(VirtualKey.Enter);
            this.VerifyCurrentLineText("///<see cref=\"$$\"/>", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void XmlTagCompletion()
        {
            SetUpEditor(@"
/// $$
class C { }
");

            this.SendKeys("<summary>");
            this.VerifyCurrentLineText("/// <summary>$$</summary>", assertCaretPosition: true);

            SetUpEditor(@"
/// <summary>$$
class C { }
");

            this.SendKeys("</");
            this.VerifyCurrentLineText("/// <summary></summary>$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
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

            this.SetUseSuggestionMode(false);

            this.SendKeys("Mai(");

            this.VerifyCurrentSignature("void Class1.Main(string[] args)");
            this.VerifyCurrentParameter("args", "");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CompletionUsesTrackingPointsInTheFaceOfAutomaticBraceCompletion()
        {
            SetUpEditor(@"
class Class1
{
    void Main(string[] args)
    $$
}");

            this.SetUseSuggestionMode(false);

            this.SendKeys(
                '{',
                VirtualKey.Enter,
                "                 ");

            this.InvokeCompletionList();

            this.SendKeys('}');

            this.VerifyTextContains(@"
class Class1
{
    void Main(string[] args)
    {
    }$$
}",
assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
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

            this.SetUseSuggestionMode(false);

            this.SendKeys(
                'M',
                Shift(VirtualKey.Enter));

            this.VerifyTextContains(@"
class Class1
{
    void Main(string[] args)
    {
        Main$$
    }
}",
assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitOnLeftCurly()
        {
            SetUpEditor(@"
class Class1
{
    $$
}");

            this.SetUseSuggestionMode(false);

            this.SendKeys("int P { g{");

            this.VerifyTextContains(@"
class Class1
{
    int P { get { $$} }
}",
assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
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

            this.SendKeys(
                VirtualKey.Delete,
                "aaa",
                VirtualKey.Tab);
            var actualText = Editor.GetText();
            Assert.Contains("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa = aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", actualText);
            Assert.True(Editor.IsCaretOnScreen());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DismissOnSelect()
        {
            SetUpEditor(@"$$");

            this.SendKeys(Ctrl(VirtualKey.Space));
            Assert.Equal(true, Editor.IsCompletionActive());

            this.SendKeys(Ctrl(VirtualKey.A));
            Assert.Equal(false, Editor.IsCompletionActive());
        }
    }
}