// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.Test.Utilities;
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
        public async Task Braces_InsertionAndTabCompleting()
        {
            SetUpEditor(@"class C {
    void Foo() {
        $$
    }
}");

            await EditorWindow.TypeTextAsync("if (true) {");
            VerifyCurrentLine("        if (true) { $$}");

            EditorWindow.SendKey(EditorWindow.VirtualKey.Tab);
            VerifyCurrentLine("        if (true) { }$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Braces_Overtyping()
        {
            SetUpEditor(@"class C {
    void Foo() {
        $$
    }
}");

            await EditorWindow.TypeTextAsync("if (true) {");
            VerifyCurrentLine("        if (true) { $$}");

            await EditorWindow.TypeTextAsync("}");
            VerifyCurrentLine("        if (true) { }$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Braces_OnReturnNoFormattingOnlyIndentationBeforeCloseBrace()
        {
            SetUpEditor(@"class C {
    void Foo() {
        $$
    }
}");

            await EditorWindow.TypeTextAsync("if (true) {");
            EditorWindow.SendKey(EditorWindow.VirtualKey.Enter);
            await EditorWindow.TypeTextAsync("var a = 1;");

            VerifyCurrentLine("            var a = 1;$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Braces_OnReturnOvertypingTheClosingBrace()
        {
            SetUpEditor(@"class C {
    void Foo() {
        $$
    }
}");

            await EditorWindow.TypeTextAsync("if (true) {");
            EditorWindow.SendKey(EditorWindow.VirtualKey.Enter);
            await EditorWindow.TypeTextAsync("var a = 1;}");

            VerifyCurrentLine("        }$$");

            VerifyTextContains(@"if (true)
        {
            var a = 1;
        }");
        }

        [WorkItem(653540, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Braces_OnReturnWithNonWhitespaceSpanInside()
        {
            SetUpEditor("$$");

            await EditorWindow.TypeTextAsync("class A { int i;");
            EditorWindow.SendKey(EditorWindow.VirtualKey.Enter);
            WaitForAllAsyncOperations();

            VerifyCurrentLine("$$}");

            VerifyTextContains(@"class A { int i;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Paren_InsertionAndTabCompleting()
        {
            SetUpEditor(@"class C {
    $$
}");

            await EditorWindow.TypeTextAsync("void Foo(");
            VerifyCurrentLine("    void Foo($$)");

            await EditorWindow.TypeTextAsync("int x");
            EditorWindow.SendKey(EditorWindow.VirtualKey.Tab);
            VerifyCurrentLine("    void Foo(int x)$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Paren_Overtyping()
        {
            SetUpEditor(@"class C {
    $$
}");

            await EditorWindow.TypeTextAsync("void Foo(");
            EditorWindow.SendKey(EditorWindow.VirtualKey.Escape);
            await EditorWindow.TypeTextAsync(")");

            VerifyCurrentLine("    void Foo()$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task SquareBracket_Insertion()
        {
            SetUpEditor(@"class C {
    $$
}");

            await EditorWindow.TypeTextAsync("int [");

            VerifyCurrentLine("    int [$$]");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task SquareBracket_Overtyping()
        {
            SetUpEditor(@"class C {
    $$
}");

            await EditorWindow.TypeTextAsync("int [");
            await EditorWindow.TypeTextAsync("]");

            VerifyCurrentLine("    int []$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task DoubleQuote_InsertionAndTabCompletion()
        {
            SetUpEditor(@"class C {
    $$
}");

            await EditorWindow.TypeTextAsync("string str = \"");
            EditorWindow.SendKey(EditorWindow.VirtualKey.Tab);

            VerifyCurrentLine("    string str = \"\"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task DoubleQuote_InsertionAndOvertyping()
        {
            SetUpEditor(@"class C {
    $$
}");

            await EditorWindow.TypeTextAsync("string str = \"Hi Roslyn!");
            await EditorWindow.TypeTextAsync("\"");

            VerifyCurrentLine("    string str = \"Hi Roslyn!\"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task AngleBracket_PossibleGenerics_InsertionAndCompletion()
        {
            SetUpEditor(@"class C {
    //field
    $$
}");

            await EditorWindow.TypeTextAsync("System.Action<");
            WaitForAllAsyncOperations();
            EditorWindow.SendKey(EditorWindow.VirtualKey.Tab);

            VerifyCurrentLine("    System.Action<>$$");

            SetUpEditor(@"class C {
    //method decl
    $$
}");

            await EditorWindow.TypeTextAsync("void GenericMethod<");
            WaitForAllAsyncOperations();
            EditorWindow.SendKey(EditorWindow.VirtualKey.Tab);

            VerifyCurrentLine("    void GenericMethod<>$$");

            SetUpEditor(@"class C {
    //delegate
    $$
}");

            await EditorWindow.TypeTextAsync("delegate void Del<");

            VerifyCurrentLine("    delegate void Del<$$>");

            SetUpEditor(@"
//using directive
$$
");

            await EditorWindow.TypeTextAsync("using ActionOfT = System.Action<");

            VerifyCurrentLine("using ActionOfT = System.Action<$$>");

            SetUpEditor(@"
//class
$$
");

            await EditorWindow.TypeTextAsync("class GenericClass<");
            WaitForAllAsyncOperations();
            await EditorWindow.TypeTextAsync(">");

            VerifyCurrentLine("class GenericClass<>$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task SingleQuote_InsertionAndCompletion()
        {
            SetUpEditor(@"class C {
    $$
}");

            await EditorWindow.TypeTextAsync("char c = '");
            VerifyCurrentLine("    char c = '$$'");

            EditorWindow.SendKey(EditorWindow.VirtualKey.Delete);
            EditorWindow.SendKey(EditorWindow.VirtualKey.Backspace);

            await EditorWindow.TypeTextAsync("'\u6666");
            await EditorWindow.TypeTextAsync("'");
            VerifyCurrentLine("    char c = '\u6666'$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Nested_AllKinds()
        {
            SetUpEditor(@"class Bar<U>
{
    T Foo<T>(T t) { return t; }
    void M()
    {
        $$
    }
}");

            await EditorWindow.TypeTextAsync("var arr=new object[,]{{Foo(0");
            EditorWindow.SendKey(EditorWindow.VirtualKey.Tab);
            EditorWindow.SendKey(EditorWindow.VirtualKey.Tab);
            await EditorWindow.TypeTextAsync(",{Foo(Foo(\"hello");
            EditorWindow.SendKey(EditorWindow.VirtualKey.Tab);
            EditorWindow.SendKey(EditorWindow.VirtualKey.Tab);
            EditorWindow.SendKey(EditorWindow.VirtualKey.Tab);
            EditorWindow.SendKey(EditorWindow.VirtualKey.Tab);
            EditorWindow.SendKey(EditorWindow.VirtualKey.Tab);
            await EditorWindow.TypeTextAsync(";");

            VerifyCurrentLine("        var arr = new object[,] { { Foo(0) }, { Foo(Foo(\"hello\")) } };$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionInSingleLineComments()
        {
            SetUpEditor(@"class C {
    // $$
}");

            await EditorWindow.TypeTextAsync("{([\"'");
            VerifyCurrentLine("    // {([\"'$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionInMultiLineComments()
        {
            SetUpEditor(@"class C {
    /*
     $$
    */
}");

            await EditorWindow.TypeTextAsync("{([\"'");
            VerifyCurrentLine("     {([\"'$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionStringVerbatimStringOrCharLiterals()
        {
            SetUpEditor(@"class C {
    $$
}");

            await EditorWindow.TypeTextAsync("string s = \"{([<'");
            VerifyCurrentLine("    string s = \"{([<'\"$$");

            EditorWindow.SendKey(EditorWindow.VirtualKey.End);
            await EditorWindow.TypeTextAsync(";");
            EditorWindow.SendKey(EditorWindow.VirtualKey.Enter);

            await EditorWindow.TypeTextAsync("string y = @\"{([<'");
            VerifyCurrentLine("    string y = @\"{([<'\"$$");

            EditorWindow.SendKey(EditorWindow.VirtualKey.End);
            await EditorWindow.TypeTextAsync(";");
            EditorWindow.SendKey(EditorWindow.VirtualKey.Enter);

            await EditorWindow.TypeTextAsync("char ch = '{([<\"");
            VerifyCurrentLine("    char ch = '{([<\"'$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionInXmlDocComments()
        {
            SetUpEditor(@"
$$
class C { }");

            await EditorWindow.TypeTextAsync("///");
            await EditorWindow.TypeTextAsync("{([<\"'");
            VerifyCurrentLine("/// {([<\"'$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionInDisabledPreprocesser()
        {
            SetUpEditor(@"
class C {
#if false
$$
#endif
}");

            await EditorWindow.TypeTextAsync("void Foo(");
            VerifyCurrentLine("void Foo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionAfterRegionPreprocesser()
        {
            SetUpEditor(@"
#region $$

#endregion
");

            await EditorWindow.TypeTextAsync("{([<\"'");
            VerifyCurrentLine("#region {([<\"'$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionAfterEndregionPreprocesser()
        {
            SetUpEditor(@"
#region

#endregion $$
");

            await EditorWindow.TypeTextAsync("{([<\"'");
            VerifyCurrentLine("#endregion {([<\"'$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionAfterIfPreprocesser()
        {
            SetUpEditor(@"
#if $$
");

            await EditorWindow.TypeTextAsync("{([<\"'");
            VerifyCurrentLine("#if {([<\"'$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionAfterPragmaPreprocesser()
        {
            SetUpEditor(@"
#pragma $$
");

            await EditorWindow.TypeTextAsync("{([<\"'");
            VerifyCurrentLine("#pragma {([<\"'$$");
        }

        [WorkItem(651954, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task InteractionWithOverrideStubGeneration()
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

            await EditorWindow.TypeTextAsync("override Foo(");

            VerifyTextContains(@"class B : A
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
        public async Task InteractionWithCompletionList()
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

            await EditorWindow.TypeTextAsync("new Li(");
            EditorWindow.SendKey(EditorWindow.VirtualKey.Tab);
            VerifyCurrentLine("        List<int> li = new List<int>($$)");
        }

        [WorkItem(823958, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task AutoBraceCompleteDoesNotFormatBracePairInInitializers()
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

            await EditorWindow.TypeTextAsync("new int[]{");
            VerifyCurrentLine("        var x = new int[] {$$}");
        }

        [WorkItem(823958, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task AutoBraceCompleteDoesNotFormatBracePairInObjectCreationExpression()
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

            await EditorWindow.TypeTextAsync("new {");
            VerifyCurrentLine("        var x = new {$$}");
        }

        [WorkItem(823958, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task AutoBraceCompleteFormatsBracePairInClassDeclarationAndAutoProperty()
        {
            SetUpEditor(@"
class $$
");

            await EditorWindow.TypeTextAsync("C{");
            VerifyCurrentLine("class C { $$}");

            EditorWindow.SendKey(EditorWindow.VirtualKey.Enter);
            await EditorWindow.TypeTextAsync("int Prop {");

            VerifyTextContains(@"
class C
{
    int Prop { $$}
}");
        }
    }
}