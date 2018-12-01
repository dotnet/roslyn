// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

using WorkItemAttribute = Roslyn.Test.Utilities.WorkItemAttribute;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpAutomaticBraceCompletion : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpAutomaticBraceCompletion( )
            : base( nameof(CSharpAutomaticBraceCompletion))
        {
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void Braces_InsertionAndTabCompleting()
        {
            SetUpEditor(@"
class C {
    void Goo() {
        $$
    }
}");

            VisualStudioInstance.Editor.SendKeys("if (true) {");
            VisualStudioInstance.Editor.Verify.CurrentLineText("if (true) { $$}", assertCaretPosition: true);

            VisualStudioInstance.Editor.SendKeys(VirtualKey.Tab);
            VisualStudioInstance.Editor.Verify.CurrentLineText("if (true) { }$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void Braces_Overtyping()
        {
            SetUpEditor(@"
class C {
    void Goo() {
        $$
    }
}");

            VisualStudioInstance.Editor.SendKeys("if (true) {");
            VisualStudioInstance.Editor.Verify.CurrentLineText("if (true) { $$}", assertCaretPosition: true);

            VisualStudioInstance.Editor.SendKeys("}");
            VisualStudioInstance.Editor.Verify.CurrentLineText("if (true) { }$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void Braces_OnReturnNoFormattingOnlyIndentationBeforeCloseBrace()
        {
            SetUpEditor(@"
class C {
    void Goo() {
        $$
    }
}");

            VisualStudioInstance.Editor.SendKeys(
                "if (true) {",
                VirtualKey.Enter,
                "var a = 1;");

            VisualStudioInstance.Editor.Verify.TextContains(@"
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

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void Braces_OnReturnOvertypingTheClosingBrace()
        {
            SetUpEditor(@"
class C {
    void Goo() {
        $$
    }
}");

            VisualStudioInstance.Editor.SendKeys(
                "if (true) {",
                VirtualKey.Enter,
                "var a = 1;",
                '}');

            VisualStudioInstance.Editor.Verify.TextContains(@"
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
        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void Braces_OnReturnWithNonWhitespaceSpanInside()
        {
            VisualStudioInstance.Editor.SendKeys(
                "class A { int i;",
                VirtualKey.Enter);

            VisualStudioInstance.Editor.Verify.TextContains(@"class A { int i;
$$}",
assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void Paren_InsertionAndTabCompleting()
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudioInstance.Editor.SendKeys("void Goo(");
            VisualStudioInstance.Editor.Verify.CurrentLineText("void Goo($$)", assertCaretPosition: true);

            VisualStudioInstance.Editor.SendKeys("int x", VirtualKey.Tab);
            VisualStudioInstance.Editor.Verify.CurrentLineText("void Goo(int x)$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void Paren_Overtyping()
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudioInstance.Editor.SendKeys(
                "void Goo(",
                VirtualKey.Escape,
                ")");

            VisualStudioInstance.Editor.Verify.CurrentLineText("void Goo()$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void SquareBracket_Insertion()
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudioInstance.Editor.SendKeys("int [");
            VisualStudioInstance.Editor.Verify.CurrentLineText("int [$$]", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void SquareBracket_Overtyping()
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudioInstance.Editor.SendKeys("int [", ']');
            VisualStudioInstance.Editor.Verify.CurrentLineText("int []$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void DoubleQuote_InsertionAndTabCompletion()
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudioInstance.Editor.SendKeys("string str = \"", VirtualKey.Tab);
            VisualStudioInstance.Editor.Verify.CurrentLineText("string str = \"\"$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void DoubleQuote_InsertionAndOvertyping()
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudioInstance.Editor.SendKeys("string str = \"Hi Roslyn!", '"');
            VisualStudioInstance.Editor.Verify.CurrentLineText("string str = \"Hi Roslyn!\"$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void DoubleQuote_FixedInterpolatedVerbatimString()
        {
            SetUpEditor(@"
class C
{
    void M()
    {
        $$
    }
}");

            VisualStudioInstance.Editor.SendKeys("var v = @$\"");
            VisualStudioInstance.Editor.Verify.CurrentLineText("var v = $@\"$$\"", assertCaretPosition: true);

            // Backspace removes quotes
            VisualStudioInstance.Editor.SendKeys(VirtualKey.Backspace);
            VisualStudioInstance.Editor.Verify.CurrentLineText("var v = $@$$", assertCaretPosition: true);

            // Undo puts them back
            VisualStudioInstance.Editor.Undo();
            VisualStudioInstance.Editor.Verify.CurrentLineText("var v = $@\"$$\"", assertCaretPosition: true);

            // First, the FixInterpolatedVerbatimString action is undone (@$ reordering)
            VisualStudioInstance.Editor.Undo();
            VisualStudioInstance.Editor.Verify.CurrentLineText("var v = @$\"$$\"", assertCaretPosition: true);

            // Then the automatic quote completion is undone
            VisualStudioInstance.Editor.Undo();
            VisualStudioInstance.Editor.Verify.CurrentLineText("var v = @$\"$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void AngleBracket_PossibleGenerics_InsertionAndCompletion()
        {
            SetUpEditor(@"
class C {
    //field
    $$
}");

            VisualStudioInstance.Editor.SendKeys("System.Action<", VirtualKey.Tab);
            VisualStudioInstance.Editor.Verify.CurrentLineText("System.Action<>$$", assertCaretPosition: true);

            SetUpEditor(@"
class C {
    //method decl
    $$
}");

            VisualStudioInstance.Editor.SendKeys("void GenericMethod<", VirtualKey.Tab);
            VisualStudioInstance.Editor.Verify.CurrentLineText("void GenericMethod<>$$", assertCaretPosition: true);

            SetUpEditor(@"
class C {
    //delegate
    $$
}");

            VisualStudioInstance.Editor.SendKeys("delegate void Del<");
            VisualStudioInstance.Editor.Verify.CurrentLineText("delegate void Del<$$>", assertCaretPosition: true);

            SetUpEditor(@"
//using directive
$$
");

            VisualStudioInstance.Editor.SendKeys("using ActionOfT = System.Action<");
            VisualStudioInstance.Editor.Verify.CurrentLineText("using ActionOfT = System.Action<$$>", assertCaretPosition: true);

            SetUpEditor(@"
//class
$$
");

            VisualStudioInstance.Editor.SendKeys("class GenericClass<", '>');
            VisualStudioInstance.Editor.Verify.CurrentLineText("class GenericClass<>$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void SingleQuote_InsertionAndCompletion()
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudioInstance.Editor.SendKeys("char c = '");
            VisualStudioInstance.Editor.Verify.CurrentLineText("char c = '$$'", assertCaretPosition: true);

            VisualStudioInstance.Editor.SendKeys(VirtualKey.Delete, VirtualKey.Backspace);
            VisualStudioInstance.Editor.SendKeys("'\u6666", "'");

            VisualStudioInstance.Editor.Verify.CurrentLineText("char c = '\u6666'$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void Nested_AllKinds()
        {
            SetUpEditor(@"
class Bar<U>
{
    T Goo<T>(T t) { return t; }
    void M()
    {
        $$
    }
}");

            VisualStudioInstance.Editor.SendKeys(
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

            VisualStudioInstance.Editor.Verify.CurrentLineText("var arr = new object[,] { { Goo(0) }, { Goo(Goo(\"hello\")) } };$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInSingleLineComments()
        {
            SetUpEditor(@"
class C {
    // $$
}");

            VisualStudioInstance.Editor.SendKeys("{([\"'");
            VisualStudioInstance.Editor.Verify.CurrentLineText("// {([\"'$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInMultiLineComments()
        {
            SetUpEditor(@"
class C {
    /*
     $$
    */
}");

            VisualStudioInstance.Editor.SendKeys("{([\"'");
            VisualStudioInstance.Editor.Verify.CurrentLineText("{([\"'$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionStringVerbatimStringOrCharLiterals()
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudioInstance.Editor.SendKeys("string s = \"{([<'");
            VisualStudioInstance.Editor.Verify.CurrentLineText("string s = \"{([<'\"$$", assertCaretPosition: true);

            VisualStudioInstance.Editor.SendKeys(VirtualKey.End, ';', VirtualKey.Enter);

            VisualStudioInstance.Editor.SendKeys("string y = @\"{([<'");
            VisualStudioInstance.Editor.Verify.CurrentLineText("string y = @\"{([<'\"$$", assertCaretPosition: true);

            VisualStudioInstance.Editor.SendKeys(VirtualKey.End, ';', VirtualKey.Enter);

            VisualStudioInstance.Editor.SendKeys("char ch = '{([<\"");
            VisualStudioInstance.Editor.Verify.CurrentLineText("char ch = '{([<\"'$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInXmlDocComments()
        {
            SetUpEditor(@"
$$
class C { }");

            VisualStudioInstance.Editor.SendKeys(
                "///",
                "{([<\"'");

            VisualStudioInstance.Editor.Verify.CurrentLineText("/// {([<\"'$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInDisabledPreprocesser()
        {
            SetUpEditor(@"
class C {
#if false
$$
#endif
}");

            VisualStudioInstance.Editor.SendKeys("void Goo(");
            VisualStudioInstance.Editor.Verify.CurrentLineText("void Goo($$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterRegionPreprocesser()
        {
            SetUpEditor(@"
#region $$

#endregion
");

            VisualStudioInstance.Editor.SendKeys("{([<\"'");
            VisualStudioInstance.Editor.Verify.CurrentLineText("#region {([<\"'$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterEndregionPreprocesser()
        {
            SetUpEditor(@"
#region

#endregion $$
");

            VisualStudioInstance.Editor.SendKeys("{([<\"'");
            VisualStudioInstance.Editor.Verify.CurrentLineText("#endregion {([<\"'$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterIfPreprocesser()
        {
            SetUpEditor(@"
#if $$
");

            VisualStudioInstance.Editor.SendKeys("{([<\"'");
            VisualStudioInstance.Editor.Verify.CurrentLineText("#if {([<\"'$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterPragmaPreprocesser()
        {
            SetUpEditor(@"
#pragma $$
");

            VisualStudioInstance.Editor.SendKeys("{([<\"'");
            VisualStudioInstance.Editor.Verify.CurrentLineText("#pragma {([<\"'$$", assertCaretPosition: true);
        }

        [WorkItem(651954, "DevDiv")]
        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void InteractionWithOverrideStubGeneration()
        {
            SetUpEditor(@"
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

            VisualStudioInstance.Editor.SendKeys("override Goo(");
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"
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
        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
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

            VisualStudioInstance.Editor.SendKeys("new Li(", VirtualKey.Tab);
            VisualStudioInstance.Editor.Verify.CurrentLineText("List<int> li = new List<int>($$)", assertCaretPosition: true);
        }

        [WorkItem(823958, "DevDiv")]
        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
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

            VisualStudioInstance.Editor.SendKeys("new int[]{");
            VisualStudioInstance.Editor.Verify.CurrentLineText("var x = new int[] {$$}", assertCaretPosition: true);
        }

        [WorkItem(823958, "DevDiv")]
        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
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

            VisualStudioInstance.Editor.SendKeys("new {");
            VisualStudioInstance.Editor.Verify.CurrentLineText("var x = new {$$}", assertCaretPosition: true);
        }

        [WorkItem(823958, "DevDiv")]
        [TestMethod, TestCategory(Traits.Features.AutomaticCompletion)]
        public void AutoBraceCompleteFormatsBracePairInClassDeclarationAndAutoProperty()
        {
            SetUpEditor(@"
class $$
");

            VisualStudioInstance.Editor.SendKeys("C{");
            VisualStudioInstance.Editor.Verify.CurrentLineText("class C { $$}", assertCaretPosition: true);

            VisualStudioInstance.Editor.SendKeys(
                VirtualKey.Enter,
                "int Prop {");
            VisualStudioInstance.Editor.Verify.TextContains(@"
class C
{
    int Prop { $$}
}",
assertCaretPosition: true);
        }
    }
}
