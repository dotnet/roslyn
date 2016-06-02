// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.Test.Utilities;
using Roslyn.VisualStudio.Test.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.IntegrationTests
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpAutomaticBraceCompletion : EditorTestFixture
    {
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

            SendKeys("if (true) {");
            VerifyCurrentLineText("if (true) { $$}");

            SendKeys(VirtualKey.Tab);
            VerifyCurrentLineText("if (true) { }$$");
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

            SendKeys("if (true) {");
            VerifyCurrentLineText("if (true) { $$}");

            SendKeys("}");
            VerifyCurrentLineText("if (true) { }$$");
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

            SendKeys(
                "if (true) {",
                VirtualKey.Enter,
                "var a = 1;");

            VerifyTextContains(@"
class C {
    void Foo() {
        if (true)
        {
            var a = 1;$$
        }
    }
}");
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

            SendKeys(
                "if (true) {",
                VirtualKey.Enter,
                "var a = 1;",
                '}');

            VerifyTextContains(@"
class C {
    void Foo() {
        if (true)
        {
            var a = 1;
        }$$
    }
}");
        }

        [WorkItem(653540, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_OnReturnWithNonWhitespaceSpanInside()
        {
            SetUpEditor("$$");

            SendKeys(
                "class A { int i;",
                VirtualKey.Enter);

            VerifyTextContains(@"class A { int i;
$$}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Paren_InsertionAndTabCompleting()
        {
            SetUpEditor(@"
class C {
    $$
}");

            SendKeys("void Foo(");
            VerifyCurrentLineText("void Foo($$)");

            SendKeys("int x", VirtualKey.Tab);
            VerifyCurrentLineText("void Foo(int x)$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Paren_Overtyping()
        {
            SetUpEditor(@"
class C {
    $$
}");

            SendKeys(
                "void Foo(",
                VirtualKey.Escape,
                ")");

            VerifyCurrentLineText("void Foo()$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void SquareBracket_Insertion()
        {
            SetUpEditor(@"
class C {
    $$
}");

            SendKeys("int [");
            VerifyCurrentLineText("int [$$]");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void SquareBracket_Overtyping()
        {
            SetUpEditor(@"
class C {
    $$
}");

            SendKeys("int [", ']');
            VerifyCurrentLineText("int []$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void DoubleQuote_InsertionAndTabCompletion()
        {
            SetUpEditor(@"
class C {
    $$
}");

            SendKeys("string str = \"", VirtualKey.Tab);
            VerifyCurrentLineText("string str = \"\"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void DoubleQuote_InsertionAndOvertyping()
        {
            SetUpEditor(@"
class C {
    $$
}");

            SendKeys("string str = \"Hi Roslyn!", '"');
            VerifyCurrentLineText("string str = \"Hi Roslyn!\"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void AngleBracket_PossibleGenerics_InsertionAndCompletion()
        {
            SetUpEditor(@"
class C {
    //field
    $$
}");

            SendKeys("System.Action<", VirtualKey.Tab);
            VerifyCurrentLineText("System.Action<>$$");

            SetUpEditor(@"
class C {
    //method decl
    $$
}");

            SendKeys("void GenericMethod<", VirtualKey.Tab);
            VerifyCurrentLineText("void GenericMethod<>$$");

            SetUpEditor(@"
class C {
    //delegate
    $$
}");

            SendKeys("delegate void Del<");
            VerifyCurrentLineText("delegate void Del<$$>");

            SetUpEditor(@"
//using directive
$$
");

            SendKeys("using ActionOfT = System.Action<");
            VerifyCurrentLineText("using ActionOfT = System.Action<$$>");

            SetUpEditor(@"
//class
$$
");

            SendKeys("class GenericClass<", '>');
            VerifyCurrentLineText("class GenericClass<>$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void SingleQuote_InsertionAndCompletion()
        {
            SetUpEditor(@"
class C {
    $$
}");

            SendKeys("char c = '");
            VerifyCurrentLineText("char c = '$$'");

            SendKeys(VirtualKey.Delete, VirtualKey.Backspace);
            SendKeys("'\u6666", "'");

            VerifyCurrentLineText("char c = '\u6666'$$");
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

            SendKeys(
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

            VerifyCurrentLineText("var arr = new object[,] { { Foo(0) }, { Foo(Foo(\"hello\")) } };$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInSingleLineComments()
        {
            SetUpEditor(@"
class C {
    // $$
}");

            SendKeys("{([\"'");
            VerifyCurrentLineText("// {([\"'$$");
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

            SendKeys("{([\"'");
            VerifyCurrentLineText("{([\"'$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionStringVerbatimStringOrCharLiterals()
        {
            SetUpEditor(@"
class C {
    $$
}");

            SendKeys("string s = \"{([<'");
            VerifyCurrentLineText("string s = \"{([<'\"$$");

            SendKeys(VirtualKey.End, ';', VirtualKey.Enter);

            SendKeys("string y = @\"{([<'");
            VerifyCurrentLineText("string y = @\"{([<'\"$$");

            SendKeys(VirtualKey.End, ';', VirtualKey.Enter);

            SendKeys("char ch = '{([<\"");
            VerifyCurrentLineText("char ch = '{([<\"'$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInXmlDocComments()
        {
            SetUpEditor(@"
$$
class C { }");

            SendKeys(
                "///",
                "{([<\"'");

            VerifyCurrentLineText("/// {([<\"'$$");
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

            SendKeys("void Foo(");
            VerifyCurrentLineText("void Foo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterRegionPreprocesser()
        {
            SetUpEditor(@"
#region $$

#endregion
");

            SendKeys("{([<\"'");
            VerifyCurrentLineText("#region {([<\"'$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterEndregionPreprocesser()
        {
            SetUpEditor(@"
#region

#endregion $$
");

            SendKeys("{([<\"'");
            VerifyCurrentLineText("#endregion {([<\"'$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterIfPreprocesser()
        {
            SetUpEditor(@"
#if $$
");

            SendKeys("{([<\"'");
            VerifyCurrentLineText("#if {([<\"'$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterPragmaPreprocesser()
        {
            SetUpEditor(@"
#pragma $$
");

            SendKeys("{([<\"'");
            VerifyCurrentLineText("#pragma {([<\"'$$");
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

            SendKeys("override Foo(");

            VerifyTextContains(@"
class B : A
{
    // type ""override Foo(""
    public override void Foo()
    {
        base.Foo();
    }
}");
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

            SendKeys("new Li(", VirtualKey.Tab);
            VerifyCurrentLineText("List<int> li = new List<int>($$)");
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

            SendKeys("new int[]{");
            VerifyCurrentLineText("var x = new int[] {$$}");
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

            SendKeys("new {");
            VerifyCurrentLineText("var x = new {$$}");
        }

        [WorkItem(823958, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void AutoBraceCompleteFormatsBracePairInClassDeclarationAndAutoProperty()
        {
            SetUpEditor(@"
class $$
");

            SendKeys("C{");
            VerifyCurrentLineText("class C { $$}");

            SendKeys(
                VirtualKey.Enter,
                "int Prop {");

            VerifyTextContains(@"
class C
{
    int Prop { $$}
}");
        }
    }
}