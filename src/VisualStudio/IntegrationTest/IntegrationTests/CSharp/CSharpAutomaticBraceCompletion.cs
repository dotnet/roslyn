﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
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

            VisualStudio.Editor.SendKeys("if (true) {");
            VisualStudio.Editor.Verify.CurrentLineText("if (true) { $$}", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("if (true) { }$$", assertCaretPosition: true);
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

            VisualStudio.Editor.SendKeys("if (true) {");
            VisualStudio.Editor.Verify.CurrentLineText("if (true) { $$}", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys("}");
            VisualStudio.Editor.Verify.CurrentLineText("if (true) { }$$", assertCaretPosition: true);
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

            VisualStudio.Editor.SendKeys(
                "if (true) {",
                VirtualKey.Enter,
                "var a = 1;");

            VisualStudio.Editor.Verify.TextContains(@"
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

            VisualStudio.Editor.SendKeys(
                "if (true) {",
                VirtualKey.Enter,
                "var a = 1;",
                '}');

            VisualStudio.Editor.Verify.TextContains(@"
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
            VisualStudio.Editor.SendKeys(
                "class A { int i;",
                VirtualKey.Enter);

            VisualStudio.Editor.Verify.TextContains(@"class A { int i;
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

            VisualStudio.Editor.SendKeys("void Foo(");
            VisualStudio.Editor.Verify.CurrentLineText("void Foo($$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys("int x", VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("void Foo(int x)$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Paren_Overtyping()
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudio.Editor.SendKeys(
                "void Foo(",
                VirtualKey.Escape,
                ")");

            VisualStudio.Editor.Verify.CurrentLineText("void Foo()$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void SquareBracket_Insertion()
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudio.Editor.SendKeys("int [");
            VisualStudio.Editor.Verify.CurrentLineText("int [$$]", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void SquareBracket_Overtyping()
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudio.Editor.SendKeys("int [", ']');
            VisualStudio.Editor.Verify.CurrentLineText("int []$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void DoubleQuote_InsertionAndTabCompletion()
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudio.Editor.SendKeys("string str = \"", VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("string str = \"\"$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void DoubleQuote_InsertionAndOvertyping()
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudio.Editor.SendKeys("string str = \"Hi Roslyn!", '"');
            VisualStudio.Editor.Verify.CurrentLineText("string str = \"Hi Roslyn!\"$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void AngleBracket_PossibleGenerics_InsertionAndCompletion()
        {
            SetUpEditor(@"
class C {
    //field
    $$
}");

            VisualStudio.Editor.SendKeys("System.Action<", VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("System.Action<>$$", assertCaretPosition: true);

            SetUpEditor(@"
class C {
    //method decl
    $$
}");

            VisualStudio.Editor.SendKeys("void GenericMethod<", VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("void GenericMethod<>$$", assertCaretPosition: true);

            SetUpEditor(@"
class C {
    //delegate
    $$
}");

            VisualStudio.Editor.SendKeys("delegate void Del<");
            VisualStudio.Editor.Verify.CurrentLineText("delegate void Del<$$>", assertCaretPosition: true);

            SetUpEditor(@"
//using directive
$$
");

            VisualStudio.Editor.SendKeys("using ActionOfT = System.Action<");
            VisualStudio.Editor.Verify.CurrentLineText("using ActionOfT = System.Action<$$>", assertCaretPosition: true);

            SetUpEditor(@"
//class
$$
");

            VisualStudio.Editor.SendKeys("class GenericClass<", '>');
            VisualStudio.Editor.Verify.CurrentLineText("class GenericClass<>$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void SingleQuote_InsertionAndCompletion()
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudio.Editor.SendKeys("char c = '");
            VisualStudio.Editor.Verify.CurrentLineText("char c = '$$'", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Delete, VirtualKey.Backspace);
            VisualStudio.Editor.SendKeys("'\u6666", "'");

            VisualStudio.Editor.Verify.CurrentLineText("char c = '\u6666'$$", assertCaretPosition: true);
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

            VisualStudio.Editor.SendKeys(
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

            VisualStudio.Editor.Verify.CurrentLineText("var arr = new object[,] { { Foo(0) }, { Foo(Foo(\"hello\")) } };$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInSingleLineComments()
        {
            SetUpEditor(@"
class C {
    // $$
}");

            VisualStudio.Editor.SendKeys("{([\"'");
            VisualStudio.Editor.Verify.CurrentLineText("// {([\"'$$", assertCaretPosition: true);
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

            VisualStudio.Editor.SendKeys("{([\"'");
            VisualStudio.Editor.Verify.CurrentLineText("{([\"'$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionStringVerbatimStringOrCharLiterals()
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudio.Editor.SendKeys("string s = \"{([<'");
            VisualStudio.Editor.Verify.CurrentLineText("string s = \"{([<'\"$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.End, ';', VirtualKey.Enter);

            VisualStudio.Editor.SendKeys("string y = @\"{([<'");
            VisualStudio.Editor.Verify.CurrentLineText("string y = @\"{([<'\"$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.End, ';', VirtualKey.Enter);

            VisualStudio.Editor.SendKeys("char ch = '{([<\"");
            VisualStudio.Editor.Verify.CurrentLineText("char ch = '{([<\"'$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInXmlDocComments()
        {
            SetUpEditor(@"
$$
class C { }");

            VisualStudio.Editor.SendKeys(
                "///",
                "{([<\"'");

            VisualStudio.Editor.Verify.CurrentLineText("/// {([<\"'$$", assertCaretPosition: true);
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

            VisualStudio.Editor.SendKeys("void Foo(");
            VisualStudio.Editor.Verify.CurrentLineText("void Foo($$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterRegionPreprocesser()
        {
            SetUpEditor(@"
#region $$

#endregion
");

            VisualStudio.Editor.SendKeys("{([<\"'");
            VisualStudio.Editor.Verify.CurrentLineText("#region {([<\"'$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterEndregionPreprocesser()
        {
            SetUpEditor(@"
#region

#endregion $$
");

            VisualStudio.Editor.SendKeys("{([<\"'");
            VisualStudio.Editor.Verify.CurrentLineText("#endregion {([<\"'$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterIfPreprocesser()
        {
            SetUpEditor(@"
#if $$
");

            VisualStudio.Editor.SendKeys("{([<\"'");
            VisualStudio.Editor.Verify.CurrentLineText("#if {([<\"'$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterPragmaPreprocesser()
        {
            SetUpEditor(@"
#pragma $$
");

            VisualStudio.Editor.SendKeys("{([<\"'");
            VisualStudio.Editor.Verify.CurrentLineText("#pragma {([<\"'$$", assertCaretPosition: true);
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

            VisualStudio.Editor.SendKeys("override Foo(");
            var actualText = VisualStudio.Editor.GetText();
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

            VisualStudio.Editor.SendKeys("new Li(", VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("List<int> li = new List<int>($$)", assertCaretPosition: true);
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

            VisualStudio.Editor.SendKeys("new int[]{");
            VisualStudio.Editor.Verify.CurrentLineText("var x = new int[] {$$}", assertCaretPosition: true);
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

            VisualStudio.Editor.SendKeys("new {");
            VisualStudio.Editor.Verify.CurrentLineText("var x = new {$$}", assertCaretPosition: true);
        }

        [WorkItem(823958, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void AutoBraceCompleteFormatsBracePairInClassDeclarationAndAutoProperty()
        {
            SetUpEditor(@"
class $$
");

            VisualStudio.Editor.SendKeys("C{");
            VisualStudio.Editor.Verify.CurrentLineText("class C { $$}", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(
                VirtualKey.Enter,
                "int Prop {");
            VisualStudio.Editor.Verify.TextContains(@"
class C
{
    int Prop { $$}
}",
assertCaretPosition: true);
        }
    }
}