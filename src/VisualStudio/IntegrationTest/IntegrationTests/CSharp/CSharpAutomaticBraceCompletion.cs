// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpAutomaticBraceCompletion : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpAutomaticBraceCompletion()
            : base(nameof(CSharpAutomaticBraceCompletion))
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Braces_InsertionAndTabCompletingAsync()
        {
            await SetUpEditorAsync(@"
class C {
    void Goo() {
        $$
    }
}");

            await VisualStudio.Editor.SendKeysAsync("if (true) {");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("if (true) { $$}", assertCaretPosition: true);

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Tab);
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("if (true) { }$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Braces_OvertypingAsync()
        {
            await SetUpEditorAsync(@"
class C {
    void Goo() {
        $$
    }
}");

            await VisualStudio.Editor.SendKeysAsync("if (true) {");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("if (true) { $$}", assertCaretPosition: true);

            await VisualStudio.Editor.SendKeysAsync("}");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("if (true) { }$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Braces_OnReturnNoFormattingOnlyIndentationBeforeCloseBraceAsync()
        {
            await SetUpEditorAsync(@"
class C {
    void Goo() {
        $$
    }
}");

            await VisualStudio.Editor.SendKeysAsync(
                "if (true) {",
                VirtualKey.Enter,
                "var a = 1;");

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
class C {
    void Goo() {
        if (true)
        {
            var a = 1;$$
        }
    }
}",
assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Braces_OnReturnOvertypingTheClosingBraceAsync()
        {
            await SetUpEditorAsync(@"
class C {
    void Goo() {
        $$
    }
}");

            await VisualStudio.Editor.SendKeysAsync(
                "if (true) {",
                VirtualKey.Enter,
                "var a = 1;",
                '}');

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
class C {
    void Goo() {
        if (true)
        {
            var a = 1;
        }$$
    }
}",
assertCaretPosition: true);
        }

        [WorkItem(653540, "DevDiv")]
        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Braces_OnReturnWithNonWhitespaceSpanInsideAsync()
        {
            await VisualStudio.Editor.SendKeysAsync(
                "class A { int i;",
                VirtualKey.Enter);

            await VisualStudio.Editor.Verify.TextContainsAsync(@"class A { int i;
$$}",
assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Paren_InsertionAndTabCompletingAsync()
        {
            await SetUpEditorAsync(@"
class C {
    $$
}");

            await VisualStudio.Editor.SendKeysAsync("void Goo(");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("void Goo($$)", assertCaretPosition: true);

            await VisualStudio.Editor.SendKeysAsync("int x", VirtualKey.Tab);
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("void Goo(int x)$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Paren_OvertypingAsync()
        {
            await SetUpEditorAsync(@"
class C {
    $$
}");

            await VisualStudio.Editor.SendKeysAsync(
                "void Goo(",
                VirtualKey.Escape,
                ")");

            await VisualStudio.Editor.Verify.CurrentLineTextAsync("void Goo()$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task SquareBracket_InsertionAsync()
        {
            await SetUpEditorAsync(@"
class C {
    $$
}");

            await VisualStudio.Editor.SendKeysAsync("int [");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("int [$$]", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task SquareBracket_OvertypingAsync()
        {
            await SetUpEditorAsync(@"
class C {
    $$
}");

            await VisualStudio.Editor.SendKeysAsync("int [", ']');
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("int []$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task DoubleQuote_InsertionAndTabCompletionAsync()
        {
            await SetUpEditorAsync(@"
class C {
    $$
}");

            await VisualStudio.Editor.SendKeysAsync("string str = \"", VirtualKey.Tab);
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("string str = \"\"$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task DoubleQuote_InsertionAndOvertypingAsync()
        {
            await SetUpEditorAsync(@"
class C {
    $$
}");

            await VisualStudio.Editor.SendKeysAsync("string str = \"Hi Roslyn!", '"');
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("string str = \"Hi Roslyn!\"$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task AngleBracket_PossibleGenerics_InsertionAndCompletionAsync()
        {
            await SetUpEditorAsync(@"
class C {
    //field
    $$
}");

            await VisualStudio.Editor.SendKeysAsync("System.Action<", VirtualKey.Tab);
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("System.Action<>$$", assertCaretPosition: true);

            await SetUpEditorAsync(@"
class C {
    //method decl
    $$
}");

            await VisualStudio.Editor.SendKeysAsync("void GenericMethod<", VirtualKey.Tab);
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("void GenericMethod<>$$", assertCaretPosition: true);

            await SetUpEditorAsync(@"
class C {
    //delegate
    $$
}");

            await VisualStudio.Editor.SendKeysAsync("delegate void Del<");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("delegate void Del<$$>", assertCaretPosition: true);

            await SetUpEditorAsync(@"
//using directive
$$
");

            await VisualStudio.Editor.SendKeysAsync("using ActionOfT = System.Action<");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("using ActionOfT = System.Action<$$>", assertCaretPosition: true);

            await SetUpEditorAsync(@"
//class
$$
");

            await VisualStudio.Editor.SendKeysAsync("class GenericClass<", '>');
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("class GenericClass<>$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task SingleQuote_InsertionAndCompletionAsync()
        {
            await SetUpEditorAsync(@"
class C {
    $$
}");

            await VisualStudio.Editor.SendKeysAsync("char c = '");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("char c = '$$'", assertCaretPosition: true);

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Delete, VirtualKey.Backspace);
            await VisualStudio.Editor.SendKeysAsync("'\u6666", "'");

            await VisualStudio.Editor.Verify.CurrentLineTextAsync("char c = '\u6666'$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Nested_AllKindsAsync()
        {
            await SetUpEditorAsync(@"
class Bar<U>
{
    T Goo<T>(T t) { return t; }
    void M()
    {
        $$
    }
}");

            await VisualStudio.Editor.SendKeysAsync(
                "var arr=new object[,]{{Goo(0",
                VirtualKey.Tab,
                VirtualKey.Tab,
                ",{Goo(Goo(\"hello",
                VirtualKey.Tab,
                VirtualKey.Tab,
                VirtualKey.Tab,
                VirtualKey.Tab,
                VirtualKey.Tab,
                ';');

            await VisualStudio.Editor.Verify.CurrentLineTextAsync("var arr = new object[,] { { Goo(0) }, { Goo(Goo(\"hello\")) } };$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionInSingleLineCommentsAsync()
        {
            await SetUpEditorAsync(@"
class C {
    // $$
}");

            await VisualStudio.Editor.SendKeysAsync("{([\"'");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("// {([\"'$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionInMultiLineCommentsAsync()
        {
            await SetUpEditorAsync(@"
class C {
    /*
     $$
    */
}");

            await VisualStudio.Editor.SendKeysAsync("{([\"'");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("{([\"'$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionStringVerbatimStringOrCharLiteralsAsync()
        {
            await SetUpEditorAsync(@"
class C {
    $$
}");

            await VisualStudio.Editor.SendKeysAsync("string s = \"{([<'");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("string s = \"{([<'\"$$", assertCaretPosition: true);

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.End, ';', VirtualKey.Enter);

            await VisualStudio.Editor.SendKeysAsync("string y = @\"{([<'");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("string y = @\"{([<'\"$$", assertCaretPosition: true);

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.End, ';', VirtualKey.Enter);

            await VisualStudio.Editor.SendKeysAsync("char ch = '{([<\"");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("char ch = '{([<\"'$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionInXmlDocCommentsAsync()
        {
            await SetUpEditorAsync(@"
$$
class C { }");

            await VisualStudio.Editor.SendKeysAsync(
                "///",
                "{([<\"'");

            await VisualStudio.Editor.Verify.CurrentLineTextAsync("/// {([<\"'$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionInDisabledPreprocesserAsync()
        {
            await SetUpEditorAsync(@"
class C {
#if false
$$
#endif
}");

            await VisualStudio.Editor.SendKeysAsync("void Goo(");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("void Goo($$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionAfterRegionPreprocesserAsync()
        {
            await SetUpEditorAsync(@"
#region $$

#endregion
");

            await VisualStudio.Editor.SendKeysAsync("{([<\"'");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("#region {([<\"'$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionAfterEndregionPreprocesserAsync()
        {
            await SetUpEditorAsync(@"
#region

#endregion $$
");

            await VisualStudio.Editor.SendKeysAsync("{([<\"'");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("#endregion {([<\"'$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionAfterIfPreprocesserAsync()
        {
            await SetUpEditorAsync(@"
#if $$
");

            await VisualStudio.Editor.SendKeysAsync("{([<\"'");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("#if {([<\"'$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionAfterPragmaPreprocesserAsync()
        {
            await SetUpEditorAsync(@"
#pragma $$
");

            await VisualStudio.Editor.SendKeysAsync("{([<\"'");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("#pragma {([<\"'$$", assertCaretPosition: true);
        }

        [WorkItem(651954, "DevDiv")]
        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task InteractionWithOverrideStubGenerationAsync()
        {
            await SetUpEditorAsync(@"
class A
{
    public virtual void Goo() { }
}
class B : A
{
    // type ""override Goo(""
    $$
}
");

            await VisualStudio.Editor.SendKeysAsync("override Goo(");
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains(@"
class B : A
{
    // type ""override Goo(""
    public override void Goo()
    {
        base.Goo();
    }
}", actualText);
        }

        [WorkItem(531107, "DevDiv")]
        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task InteractionWithCompletionListAsync()
        {
            await SetUpEditorAsync(@"
using System.Collections.Generic;
class C 
{
    void M()
    {
        List<int> li = $$
    }
}
");

            await VisualStudio.Editor.SendKeysAsync("new Li(", VirtualKey.Tab);
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("List<int> li = new List<int>($$)", assertCaretPosition: true);
        }

        [WorkItem(823958, "DevDiv")]
        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task AutoBraceCompleteDoesNotFormatBracePairInInitializersAsync()
        {
            await SetUpEditorAsync(@"
class C 
{
    void M()
    {
        var x = $$
    }
}
");

            await VisualStudio.Editor.SendKeysAsync("new int[]{");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("var x = new int[] {$$}", assertCaretPosition: true);
        }

        [WorkItem(823958, "DevDiv")]
        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task AutoBraceCompleteDoesNotFormatBracePairInObjectCreationExpressionAsync()
        {
            await SetUpEditorAsync(@"
class C 
{
    void M()
    {
        var x = $$
    }
}
");

            await VisualStudio.Editor.SendKeysAsync("new {");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("var x = new {$$}", assertCaretPosition: true);
        }

        [WorkItem(823958, "DevDiv")]
        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task AutoBraceCompleteFormatsBracePairInClassDeclarationAndAutoPropertyAsync()
        {
            await SetUpEditorAsync(@"
class $$
");

            await VisualStudio.Editor.SendKeysAsync("C{");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("class C { $$}", assertCaretPosition: true);

            await VisualStudio.Editor.SendKeysAsync(
                VirtualKey.Enter,
                "int Prop {");
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
class C
{
    int Prop { $$}
}",
assertCaretPosition: true);
        }
    }
}
