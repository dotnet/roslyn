// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpAutomaticBraceCompletion : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpAutomaticBraceCompletion(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpAutomaticBraceCompletion))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_InsertionAndTabCompleting()
        {
            SetUpEditor(@"
class C {
    void Foo() {
        $$
    }
}");

            this.SendKeys("if (true) {");
            this.VerifyCurrentLineText("if (true) { $$}", assertCaretPosition: true);

            this.SendKeys(VirtualKey.Tab);
            this.VerifyCurrentLineText("if (true) { }$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_Overtyping()
        {
            SetUpEditor(@"
class C {
    void Foo() {
        $$
    }
}");

            this.SendKeys("if (true) {");
            this.VerifyCurrentLineText("if (true) { $$}", assertCaretPosition: true);

            this.SendKeys("}");
            this.VerifyCurrentLineText("if (true) { }$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_OnReturnNoFormattingOnlyIndentationBeforeCloseBrace()
        {
            SetUpEditor(@"
class C {
    void Foo() {
        $$
    }
}");

            this.SendKeys(
                "if (true) {",
                VirtualKey.Enter,
                "var a = 1;");

            this.VerifyTextContains(@"
class C {
    void Foo() {
        if (true)
        {
            var a = 1;$$
        }
    }
}",
assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_OnReturnOvertypingTheClosingBrace()
        {
            SetUpEditor(@"
class C {
    void Foo() {
        $$
    }
}");

            this.SendKeys(
                "if (true) {",
                VirtualKey.Enter,
                "var a = 1;",
                '}');

            this.VerifyTextContains(@"
class C {
    void Foo() {
        if (true)
        {
            var a = 1;
        }$$
    }
}",
assertCaretPosition: true);
        }

        [WorkItem(653540, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_OnReturnWithNonWhitespaceSpanInside()
        {
            this.SendKeys(
                "class A { int i;",
                VirtualKey.Enter);

            this.VerifyTextContains(@"class A { int i;
$$}",
assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Paren_InsertionAndTabCompleting()
        {
            SetUpEditor(@"
class C {
    $$
}");

            this.SendKeys("void Foo(");
            this.VerifyCurrentLineText("void Foo($$)", assertCaretPosition: true);

            this.SendKeys("int x", VirtualKey.Tab);
            this.VerifyCurrentLineText("void Foo(int x)$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Paren_Overtyping()
        {
            SetUpEditor(@"
class C {
    $$
}");

            this.SendKeys(
                "void Foo(",
                VirtualKey.Escape,
                ")");

            this.VerifyCurrentLineText("void Foo()$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void SquareBracket_Insertion()
        {
            SetUpEditor(@"
class C {
    $$
}");

            this.SendKeys("int [");
            this.VerifyCurrentLineText("int [$$]", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void SquareBracket_Overtyping()
        {
            SetUpEditor(@"
class C {
    $$
}");

            this.SendKeys("int [", ']');
            this.VerifyCurrentLineText("int []$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void DoubleQuote_InsertionAndTabCompletion()
        {
            SetUpEditor(@"
class C {
    $$
}");

            this.SendKeys("string str = \"", VirtualKey.Tab);
            this.VerifyCurrentLineText("string str = \"\"$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void DoubleQuote_InsertionAndOvertyping()
        {
            SetUpEditor(@"
class C {
    $$
}");

            this.SendKeys("string str = \"Hi Roslyn!", '"');
            this.VerifyCurrentLineText("string str = \"Hi Roslyn!\"$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void AngleBracket_PossibleGenerics_InsertionAndCompletion()
        {
            SetUpEditor(@"
class C {
    //field
    $$
}");

            this.SendKeys("System.Action<", VirtualKey.Tab);
            this.VerifyCurrentLineText("System.Action<>$$", assertCaretPosition: true);

            SetUpEditor(@"
class C {
    //method decl
    $$
}");

            this.SendKeys("void GenericMethod<", VirtualKey.Tab);
            this.VerifyCurrentLineText("void GenericMethod<>$$", assertCaretPosition: true);

            SetUpEditor(@"
class C {
    //delegate
    $$
}");

            this.SendKeys("delegate void Del<");
            this.VerifyCurrentLineText("delegate void Del<$$>", assertCaretPosition: true);

            SetUpEditor(@"
//using directive
$$
");

            this.SendKeys("using ActionOfT = System.Action<");
            this.VerifyCurrentLineText("using ActionOfT = System.Action<$$>", assertCaretPosition: true);

            SetUpEditor(@"
//class
$$
");

            this.SendKeys("class GenericClass<", '>');
            this.VerifyCurrentLineText("class GenericClass<>$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void SingleQuote_InsertionAndCompletion()
        {
            SetUpEditor(@"
class C {
    $$
}");

            this.SendKeys("char c = '");
            this.VerifyCurrentLineText("char c = '$$'", assertCaretPosition: true);

            this.SendKeys(VirtualKey.Delete, VirtualKey.Backspace);
            this.SendKeys("'\u6666", "'");

            this.VerifyCurrentLineText("char c = '\u6666'$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Nested_AllKinds()
        {
            SetUpEditor(@"
class Bar<U>
{
    T Foo<T>(T t) { return t; }
    void M()
    {
        $$
    }
}");

            this.SendKeys(
                "var arr=new object[,]{{Foo(0",
                VirtualKey.Tab,
                VirtualKey.Tab,
                ",{Foo(Foo(\"hello",
                VirtualKey.Tab,
                VirtualKey.Tab,
                VirtualKey.Tab,
                VirtualKey.Tab,
                VirtualKey.Tab,
                ';');

            this.VerifyCurrentLineText("var arr = new object[,] { { Foo(0) }, { Foo(Foo(\"hello\")) } };$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInSingleLineComments()
        {
            SetUpEditor(@"
class C {
    // $$
}");

            this.SendKeys("{([\"'");
            this.VerifyCurrentLineText("// {([\"'$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInMultiLineComments()
        {
            SetUpEditor(@"
class C {
    /*
     $$
    */
}");

            this.SendKeys("{([\"'");
            this.VerifyCurrentLineText("{([\"'$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionStringVerbatimStringOrCharLiterals()
        {
            SetUpEditor(@"
class C {
    $$
}");

            this.SendKeys("string s = \"{([<'");
            this.VerifyCurrentLineText("string s = \"{([<'\"$$", assertCaretPosition: true);

            this.SendKeys(VirtualKey.End, ';', VirtualKey.Enter);

            this.SendKeys("string y = @\"{([<'");
            this.VerifyCurrentLineText("string y = @\"{([<'\"$$", assertCaretPosition: true);

            this.SendKeys(VirtualKey.End, ';', VirtualKey.Enter);

            this.SendKeys("char ch = '{([<\"");
            this.VerifyCurrentLineText("char ch = '{([<\"'$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInXmlDocComments()
        {
            SetUpEditor(@"
$$
class C { }");

            this.SendKeys(
                "///",
                "{([<\"'");

            this.VerifyCurrentLineText("/// {([<\"'$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInDisabledPreprocesser()
        {
            SetUpEditor(@"
class C {
#if false
$$
#endif
}");

            this.SendKeys("void Foo(");
            this.VerifyCurrentLineText("void Foo($$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterRegionPreprocesser()
        {
            SetUpEditor(@"
#region $$

#endregion
");

            this.SendKeys("{([<\"'");
            this.VerifyCurrentLineText("#region {([<\"'$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterEndregionPreprocesser()
        {
            SetUpEditor(@"
#region

#endregion $$
");

            this.SendKeys("{([<\"'");
            this.VerifyCurrentLineText("#endregion {([<\"'$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterIfPreprocesser()
        {
            SetUpEditor(@"
#if $$
");

            this.SendKeys("{([<\"'");
            this.VerifyCurrentLineText("#if {([<\"'$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterPragmaPreprocesser()
        {
            SetUpEditor(@"
#pragma $$
");

            this.SendKeys("{([<\"'");
            this.VerifyCurrentLineText("#pragma {([<\"'$$", assertCaretPosition: true);
        }

        [WorkItem(651954, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InteractionWithOverrideStubGeneration()
        {
            SetUpEditor(@"
class A
{
    public virtual void Foo() { }
}
class B : A
{
    // type ""override Foo(""
    $$
}
");

            this.SendKeys("override Foo(");
            var actualText = Editor.GetText();
            Assert.Contains(@"
class B : A
{
    // type ""override Foo(""
    public override void Foo()
    {
        base.Foo();
    }
}", actualText);
        }

        [WorkItem(531107, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InteractionWithCompletionList()
        {
            SetUpEditor(@"
using System.Collections.Generic;
class C 
{
    void M()
    {
        List<int> li = $$
    }
}
");

            this.SendKeys("new Li(", VirtualKey.Tab);
            this.VerifyCurrentLineText("List<int> li = new List<int>($$)", assertCaretPosition: true);
        }

        [WorkItem(823958, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void AutoBraceCompleteDoesNotFormatBracePairInInitializers()
        {
            SetUpEditor(@"
class C 
{
    void M()
    {
        var x = $$
    }
}
");

            this.SendKeys("new int[]{");
            this.VerifyCurrentLineText("var x = new int[] {$$}", assertCaretPosition: true);
        }

        [WorkItem(823958, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void AutoBraceCompleteDoesNotFormatBracePairInObjectCreationExpression()
        {
            SetUpEditor(@"
class C 
{
    void M()
    {
        var x = $$
    }
}
");

            this.SendKeys("new {");
            this.VerifyCurrentLineText("var x = new {$$}", assertCaretPosition: true);
        }

        [WorkItem(823958, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void AutoBraceCompleteFormatsBracePairInClassDeclarationAndAutoProperty()
        {
            SetUpEditor(@"
class $$
");

            this.SendKeys("C{");
            this.VerifyCurrentLineText("class C { $$}", assertCaretPosition: true);

            this.SendKeys(
                VirtualKey.Enter,
                "int Prop {");
            this.VerifyTextContains(@"
class C
{
    int Prop { $$}
}",
assertCaretPosition: true);
        }
    }
}