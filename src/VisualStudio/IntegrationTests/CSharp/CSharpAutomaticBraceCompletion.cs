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
            EditorWindow.SetText(@"class C {
    void Foo() {
        // Marker
    }
}");

            EditorWindow.PlaceCursor("// Marker");

            await EditorWindow.TypeTextAsync("if (true) {");

            Assert.Equal("        if (true) { ", EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal("}", EditorWindow.GetLineTextAfterCaret());

            EditorWindow.SendKey(EditorWindow.VirtualKey.Tab);

            Assert.Equal("        if (true) { }", EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal(string.Empty, EditorWindow.GetLineTextAfterCaret());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Braces_Overtyping()
        {
            EditorWindow.SetText(@"class C {
    void Foo() {
        // Marker
    }
}");

            EditorWindow.PlaceCursor("// Marker");

            await EditorWindow.TypeTextAsync("if (true) {");
            await EditorWindow.TypeTextAsync("}");

            Assert.Equal("        if (true) { }", EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal(string.Empty, EditorWindow.GetLineTextAfterCaret());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Braces_OnReturnNoFormattingOnlyIndentationBeforeCloseBrace()
        {
            EditorWindow.SetText(@"class C {
    void Foo() {
        // Marker
    }
}");

            EditorWindow.PlaceCursor("// Marker");

            await EditorWindow.TypeTextAsync("if (true) {");
            EditorWindow.SendKey(EditorWindow.VirtualKey.Enter);
            await EditorWindow.TypeTextAsync("var a = 1;");

            Assert.Equal("            var a = 1;", EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal(string.Empty, EditorWindow.GetLineTextAfterCaret());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Braces_OnReturnOvertypingTheClosingBrace()
        {
            EditorWindow.SetText(@"class C {
    void Foo() {
        // Marker
    }
}");

            EditorWindow.PlaceCursor("// Marker");

            await EditorWindow.TypeTextAsync("if (true) {");
            EditorWindow.SendKey(EditorWindow.VirtualKey.Enter);
            await EditorWindow.TypeTextAsync("var a = 1;}");

            Assert.Equal("        }", EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal(string.Empty, EditorWindow.GetLineTextAfterCaret());

            Assert.Contains(@"if (true)
        {
            var a = 1;
        }
", EditorWindow.GetText());
        }

        [WorkItem(653540, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Braces_OnReturnWithNonWhitespaceSpanInside()
        {
            EditorWindow.SetText(string.Empty);

            await EditorWindow.TypeTextAsync("class A { int i;");
            EditorWindow.SendKey(EditorWindow.VirtualKey.Enter);

            Assert.Equal(string.Empty, EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal("}", EditorWindow.GetLineTextAfterCaret());

            Assert.Contains(@"class A { int i;
}", EditorWindow.GetText());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Paren_InsertionAndTabCompleting()
        {
            EditorWindow.SetText(@"class C {
    //Marker
}");

            EditorWindow.PlaceCursor("//Marker");

            await EditorWindow.TypeTextAsync("void Foo(");

            Assert.Equal("    void Foo(", EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal(")", EditorWindow.GetLineTextAfterCaret());

            await EditorWindow.TypeTextAsync("int x");
            EditorWindow.SendKey(EditorWindow.VirtualKey.Tab);

            Assert.Equal("    void Foo(int x)", EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal(string.Empty, EditorWindow.GetLineTextAfterCaret());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Paren_Overtyping()
        {
            EditorWindow.SetText(@"class C {
    //Marker
}");

            EditorWindow.PlaceCursor("//Marker");

            await EditorWindow.TypeTextAsync("void Foo(");
            EditorWindow.SendKey(EditorWindow.VirtualKey.Escape);
            await EditorWindow.TypeTextAsync(")");

            Assert.Equal("    void Foo()", EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal("", EditorWindow.GetLineTextAfterCaret());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task SquareBracket_Insertion()
        {
            EditorWindow.SetText(@"class C {
    //Marker
}");

            EditorWindow.PlaceCursor("//Marker");

            await EditorWindow.TypeTextAsync("int [");

            Assert.Equal("    int [", EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal("]", EditorWindow.GetLineTextAfterCaret());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task SquareBracket_Overtyping()
        {
            EditorWindow.SetText(@"class C {
    //Marker
}");

            EditorWindow.PlaceCursor("//Marker");

            await EditorWindow.TypeTextAsync("int [");
            await EditorWindow.TypeTextAsync("]");

            Assert.Equal("    int []", EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal(string.Empty, EditorWindow.GetLineTextAfterCaret());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task DoubleQuote_InsertionAndTabCompletion()
        {
            EditorWindow.SetText(@"class C {
    //Marker
}");

            EditorWindow.PlaceCursor("//Marker");

            await EditorWindow.TypeTextAsync("string str = \"");
            EditorWindow.SendKey(EditorWindow.VirtualKey.Tab);

            Assert.Equal("    string str = \"\"", EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal(string.Empty, EditorWindow.GetLineTextAfterCaret());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task DoubleQuote_InsertionAndOvertyping()
        {
            EditorWindow.SetText(@"class C {
    //Marker
}");

            EditorWindow.PlaceCursor("//Marker");

            await EditorWindow.TypeTextAsync("string str = \"Hi Roslyn!");
            await EditorWindow.TypeTextAsync("\"");

            Assert.Equal("    string str = \"Hi Roslyn!\"", EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal(string.Empty, EditorWindow.GetLineTextAfterCaret());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task AngleBracket_PossibleGenerics_InsertionAndCompletion()
        {
            EditorWindow.SetText(@"class C {
    //field
    //Marker
}");

            EditorWindow.PlaceCursor("//Marker");

            await EditorWindow.TypeTextAsync("System.Action<");
            WaitForAllAsyncOperations();
            EditorWindow.SendKey(EditorWindow.VirtualKey.Tab);

            Assert.Equal("    System.Action<>", EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal(string.Empty, EditorWindow.GetLineTextAfterCaret());

            EditorWindow.SetText(@"class C {
    //method decl
    //Marker
}");

            EditorWindow.PlaceCursor("//Marker");

            await EditorWindow.TypeTextAsync("void GenericMethod<");
            WaitForAllAsyncOperations();
            EditorWindow.SendKey(EditorWindow.VirtualKey.Tab);

            Assert.Equal("    void GenericMethod<>", EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal(string.Empty, EditorWindow.GetLineTextAfterCaret());

            EditorWindow.SetText(@"class C {
    //delegate
    //Marker
}");

            EditorWindow.PlaceCursor("//Marker");

            await EditorWindow.TypeTextAsync("delegate void Del<");

            Assert.Equal("    delegate void Del<", EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal(">", EditorWindow.GetLineTextAfterCaret());

            EditorWindow.SetText(@"
//using directive
//Marker
");

            EditorWindow.PlaceCursor("//Marker");

            await EditorWindow.TypeTextAsync("using ActionOfT = System.Action<");

            Assert.Equal("using ActionOfT = System.Action<", EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal(">", EditorWindow.GetLineTextAfterCaret());

            EditorWindow.SetText(@"
//class
//Marker
");

            EditorWindow.PlaceCursor("//Marker");

            await EditorWindow.TypeTextAsync("class GenericClass<");
            WaitForAllAsyncOperations();
            await EditorWindow.TypeTextAsync(">");

            Assert.Equal("class GenericClass<>", EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal(string.Empty, EditorWindow.GetLineTextAfterCaret());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task SingleQuote_InsertionAndCompletion()
        {
            EditorWindow.SetText(@"class C {
    //Marker
}");

            EditorWindow.PlaceCursor("//Marker");

            await EditorWindow.TypeTextAsync("char c = '");
            Assert.Equal("    char c = '", EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal("'", EditorWindow.GetLineTextAfterCaret());

            EditorWindow.SendKey(EditorWindow.VirtualKey.Delete);
            EditorWindow.SendKey(EditorWindow.VirtualKey.Backspace);

            await EditorWindow.TypeTextAsync("'\u6666");
            await EditorWindow.TypeTextAsync("'");

            Assert.Equal("    char c = '\u6666'", EditorWindow.GetLineTextBeforeCaret());
            Assert.Equal(string.Empty, EditorWindow.GetLineTextAfterCaret());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Nested_AllKinds()
        {
            EditorWindow.SetText(@"class Bar<U>
{
    T Foo<T>(T t) { return t; }
    void M()
    {
        //Marker
    }
}");

            EditorWindow.PlaceCursor("//Marker");

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

            Assert.Contains("        var arr = new object[,] { { Foo(0) }, { Foo(Foo(\"hello\")) } };", EditorWindow.GetText());
        }
    }
}
