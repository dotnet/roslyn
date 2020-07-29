// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.LanguageServices.Implementation.Log;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

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

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_InsertionAndTabCompleting(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class C {
    void Goo() {
        $$
    }
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("if (true) {");
            VisualStudio.Editor.Verify.CurrentLineText("if (true) { $$}", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("if (true) { }$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_Overtyping(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class C {
    void Goo() {
        $$
    }
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("if (true) {");
            VisualStudio.Editor.Verify.CurrentLineText("if (true) { $$}", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys("}");
            VisualStudio.Editor.Verify.CurrentLineText("if (true) { }$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_OnReturnNoFormattingOnlyIndentationBeforeCloseBrace(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class C {
    void Goo() {
        $$
    }
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys(
                "if (true) {",
                VirtualKey.Enter,
                "var a = 1;");

            VisualStudio.Editor.Verify.TextContains(@"
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

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_OnReturnOvertypingTheClosingBrace(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class C {
    void Goo() {
        $$
    }
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys(
                "if (true) {",
                VirtualKey.Enter,
                "var a = 1;",
                '}');

            VisualStudio.Editor.Verify.TextContains(@"
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
        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_OnReturnWithNonWhitespaceSpanInside(bool showCompletionInArgumentLists)
        {
            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys(
                "class A { int i;",
                VirtualKey.Enter);

            VisualStudio.Editor.Verify.TextContains(@"class A { int i;
$$}",
assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Paren_InsertionAndTabCompleting(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("void Goo(");
            VisualStudio.Editor.Verify.CurrentLineText("void Goo($$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys("int x", VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("void Goo(int x)$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Paren_Overtyping(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys(
                "void Goo(",
                VirtualKey.Escape,
                ")");

            VisualStudio.Editor.Verify.CurrentLineText("void Goo()$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void SquareBracket_Insertion(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("int [");
            VisualStudio.Editor.Verify.CurrentLineText("int [$$]", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void SquareBracket_Overtyping(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("int [", ']');
            VisualStudio.Editor.Verify.CurrentLineText("int []$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void DoubleQuote_InsertionAndTabCompletion(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("string str = \"", VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("string str = \"\"$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void DoubleQuote_InsertionAndOvertyping(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("string str = \"Hi Roslyn!", '"');
            VisualStudio.Editor.Verify.CurrentLineText("string str = \"Hi Roslyn!\"$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void DoubleQuote_FixedInterpolatedVerbatimString(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class C
{
    void M()
    {
        $$
    }
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("var v = @$\"");
            VisualStudio.Editor.Verify.CurrentLineText("var v = $@\"$$\"", assertCaretPosition: true);

            // Backspace removes quotes
            VisualStudio.Editor.SendKeys(VirtualKey.Backspace);
            VisualStudio.Editor.Verify.CurrentLineText("var v = $@$$", assertCaretPosition: true);

            // Undo puts them back
            VisualStudio.Editor.Undo();
            // Incorrect assertion: https://github.com/dotnet/roslyn/issues/33672
            VisualStudio.Editor.Verify.CurrentLineText("var v = $@\"\"$$", assertCaretPosition: true);

            // First, the FixInterpolatedVerbatimString action is undone (@$ reordering)
            VisualStudio.Editor.Undo();
            // Incorrect assertion: https://github.com/dotnet/roslyn/issues/33672
            VisualStudio.Editor.Verify.CurrentLineText("var v = @$\"\"$$", assertCaretPosition: true);

            // Then the automatic quote completion is undone
            VisualStudio.Editor.Undo();
            VisualStudio.Editor.Verify.CurrentLineText("var v = @$\"$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void AngleBracket_PossibleGenerics_InsertionAndCompletion(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class C {
    //field
    $$
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

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

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void SingleQuote_InsertionAndCompletion(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("char c = '");
            VisualStudio.Editor.Verify.CurrentLineText("char c = '$$'", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Delete, VirtualKey.Backspace);
            VisualStudio.Editor.SendKeys("'\u6666", "'");

            VisualStudio.Editor.Verify.CurrentLineText("char c = '\u6666'$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Nested_AllKinds(bool showCompletionInArgumentLists)
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

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys(
                "var arr=new object[,]{{Goo(0");

            if (showCompletionInArgumentLists)
            {
                Assert.False(VisualStudio.Editor.IsCompletionActive());
            }

            VisualStudio.Editor.SendKeys(
                VirtualKey.Tab,
                VirtualKey.Tab,
                ",{Goo(Goo(\"hello",
                VirtualKey.Tab,
                VirtualKey.Tab,
                VirtualKey.Tab,
                VirtualKey.Tab,
                VirtualKey.Tab,
                ';');

            VisualStudio.Editor.Verify.CurrentLineText("var arr = new object[,] { { Goo(0) }, { Goo(Goo(\"hello\")) } };$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInSingleLineComments(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class C {
    // $$
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("{([\"'");
            VisualStudio.Editor.Verify.CurrentLineText("// {([\"'$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInMultiLineComments(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class C {
    /*
     $$
    */
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("{([\"'");
            VisualStudio.Editor.Verify.CurrentLineText("{([\"'$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionStringVerbatimStringOrCharLiterals(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class C {
    $$
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("string s = \"{([<'");
            VisualStudio.Editor.Verify.CurrentLineText("string s = \"{([<'$$\"", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.End, ';', VirtualKey.Enter);

            VisualStudio.Editor.SendKeys("string y = @\"{([<'");
            VisualStudio.Editor.Verify.CurrentLineText("string y = @\"{([<'$$\"", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.End, ';', VirtualKey.Enter);

            VisualStudio.Editor.SendKeys("char ch = '{([<\"");
            VisualStudio.Editor.Verify.CurrentLineText("char ch = '{([<\"$$'", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInXmlDocComments(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
$$
class C { }");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys(
                "///",
                "{([<\"'");

            VisualStudio.Editor.Verify.CurrentLineText("/// {([<\"'$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInDisabledPreprocesser(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class C {
#if false
$$
#endif
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("void Goo(");
            VisualStudio.Editor.Verify.CurrentLineText("void Goo($$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterRegionPreprocesser(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
#region $$

#endregion
");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("{([<\"'");
            VisualStudio.Editor.Verify.CurrentLineText("#region {([<\"'$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterEndregionPreprocesser(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
#region

#endregion $$
");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("{([<\"'");
            VisualStudio.Editor.Verify.CurrentLineText("#endregion {([<\"'$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterIfPreprocesser(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
#if $$
");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("{([<\"'");
            VisualStudio.Editor.Verify.CurrentLineText("#if {([<\"'$$", assertCaretPosition: true);
        }

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionAfterPragmaPreprocesser(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
#pragma $$
");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("{([<\"'");
            VisualStudio.Editor.Verify.CurrentLineText("#pragma {([<\"'$$", assertCaretPosition: true);
        }

        [WorkItem(651954, "DevDiv")]
        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InteractionWithOverrideStubGeneration(bool showCompletionInArgumentLists)
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

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("override ");
            Assert.True(VisualStudio.Editor.IsCompletionActive());

            VisualStudio.Editor.SendKeys("Goo(");
            var actualText = VisualStudio.Editor.GetText();
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
        [WpfTheory, CombinatorialData]
        [Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InteractionWithCompletionList(bool showCompletionInArgumentLists)
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

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("new Li");
            Assert.True(VisualStudio.Editor.IsCompletionActive());

            if (showCompletionInArgumentLists)
            {
                VisualStudio.Editor.SendKeys("(", ")");
            }
            else
            {
                VisualStudio.Editor.SendKeys("(", VirtualKey.Tab);
            }

            VisualStudio.Editor.Verify.CurrentLineText("List<int> li = new List<int>()$$", assertCaretPosition: true);
        }

        [WorkItem(823958, "DevDiv")]
        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void AutoBraceCompleteDoesNotFormatBracePairInInitializers(bool showCompletionInArgumentLists)
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

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("new int[]{");
            VisualStudio.Editor.Verify.CurrentLineText("var x = new int[] {$$}", assertCaretPosition: true);
        }

        [WorkItem(823958, "DevDiv")]
        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void AutoBraceCompleteDoesNotFormatBracePairInObjectCreationExpression(bool showCompletionInArgumentLists)
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

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("new {");
            VisualStudio.Editor.Verify.CurrentLineText("var x = new {$$}", assertCaretPosition: true);
        }

        [WorkItem(823958, "DevDiv")]
        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void AutoBraceCompleteFormatsBracePairInClassDeclarationAndAutoProperty(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class $$
");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

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

        [WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        [Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        [WorkItem(18104, "https://github.com/dotnet/roslyn/issues/18104")]
        public void CompleteStatementTriggersCompletion(bool showCompletionInArgumentLists)
        {
            SetUpEditor(@"
class Program
{
    static void Main(string[] args)
    {
        Main$$
    }
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(showCompletionInArgumentLists);

            VisualStudio.Editor.SendKeys("(ar");
            VisualStudio.Editor.Verify.CurrentLineText("Main(ar$$)", assertCaretPosition: true);

            if (showCompletionInArgumentLists)
            {
                Assert.True(VisualStudio.Editor.IsCompletionActive());
            }

            VisualStudio.Editor.SendKeys(";");
            VisualStudio.Editor.Verify.CurrentLineText("Main(args);$$", assertCaretPosition: true);
        }
    }
}
