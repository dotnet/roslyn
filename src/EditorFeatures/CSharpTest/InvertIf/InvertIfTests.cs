// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.InvertIf;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InvertIf
{
    public partial class InvertIfTests : AbstractCSharpCodeActionTest
    {
        private async Task TestFixOneAsync(
            string initial,
            string expected)
        {
            await TestInRegularAndScriptAsync(CreateTreeText(initial), CreateTreeText(expected));
        }

        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpInvertIfCodeRefactoringProvider();

        private static string CreateTreeText(string initial)
        {
            return
@"class A
{
    bool a = true;
    bool b = true;
    bool c = true;
    bool d = true;

    void Goo()
    {
" + initial + @"
    }
}";
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_Identifier()
        {
            await TestFixOneAsync(
@"[||]if (a) { a(); } else { b(); }",
@"if (!a) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_IdentifierWithTrivia()
        {
            await TestFixOneAsync(
@"[||]if /*0*/(/*1*/a/*2*/)/*3*/ { a(); } else { b(); }",
@"if /*0*/(/*1*/!a/*2*/)/*3*/ { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_NotIdentifier()
        {
            await TestFixOneAsync(
@"[||]if (!a) { a(); } else { b(); }",
@"if (a) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_NotIdentifierWithTrivia()
        {
            await TestFixOneAsync(
@"[||]if /*0*/(/*1*/!/*1b*/a/*2*/)/*3*/ { a(); } else { b(); }",
@"if /*0*/(/*1*/a/*2*/)/*3*/ { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_EqualsEquals()
        {
            await TestFixOneAsync(
@"[||]if (a == b) { a(); } else { b(); }",
@"if (a != b) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_NotEquals()
        {
            await TestFixOneAsync(
@"[||]if (a != b) { a(); } else { b(); }",
@"if (a == b) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_GreaterThan()
        {
            await TestFixOneAsync(
@"[||]if (a > b) { a(); } else { b(); }",
@"if (a <= b) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_GreaterThanEquals()
        {
            await TestFixOneAsync(
@"[||]if (a >= b) { a(); } else { b(); }",
@"if (a < b) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_LessThan()
        {
            await TestFixOneAsync(
@"[||]if (a < b) { a(); } else { b(); }",
@"if (a >= b) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_LessThanEquals()
        {
            await TestFixOneAsync(
@"[||]if (a <= b) { a(); } else { b(); }",
@"if (a > b) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_DoubleParentheses()
        {
            await TestFixOneAsync(
@"[||]if ((a)) { a(); } else { b(); }",
@"if (!a) { b(); } else { a(); }");
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/26427"), Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_DoubleParenthesesWithInnerTrivia()
        {
            await TestFixOneAsync(
@"[||]if ((/*1*/a/*2*/)) { a(); } else { b(); }",
@"if (/*1*/!a/*2*/) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_DoubleParenthesesWithMiddleTrivia()
        {
            await TestFixOneAsync(
@"[||]if (/*1*/(a)/*2*/) { a(); } else { b(); }",
@"if (/*1*/!a/*2*/) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_DoubleParenthesesWithOutsideTrivia()
        {
            await TestFixOneAsync(
@"[||]if /*before*/((a))/*after*/ { a(); } else { b(); }",
@"if /*before*/(!a)/*after*/ { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_Is()
        {
            await TestFixOneAsync(
@"[||]if (a is Goo) { a(); } else { b(); }",
@"if (a is not Goo) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_MethodCall()
        {
            await TestFixOneAsync(
@"[||]if (a.Goo()) { a(); } else { b(); }",
@"if (!a.Goo()) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_Or()
        {
            await TestFixOneAsync(
@"[||]if (a || b) { a(); } else { b(); }",
@"if (!a && !b) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_Or2()
        {
            await TestFixOneAsync(
@"[||]if (!a || !b) { a(); } else { b(); }",
@"if (a && b) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_Or3()
        {
            await TestFixOneAsync(
@"[||]if (!a || b) { a(); } else { b(); }",
@"if (a && !b) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_Or4()
        {
            await TestFixOneAsync(
@"[||]if (a | b) { a(); } else { b(); }",
@"if (!a & !b) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_And()
        {
            await TestFixOneAsync(
@"[||]if (a && b) { a(); } else { b(); }",
@"if (!a || !b) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_And2()
        {
            await TestFixOneAsync(
@"[||]if (!a && !b) { a(); } else { b(); }",
@"if (a || b) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_And3()
        {
            await TestFixOneAsync(
@"[||]if (!a && b) { a(); } else { b(); }",
@"if (a || !b) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_And4()
        {
            await TestFixOneAsync(
@"[||]if (a & b) { a(); } else { b(); }",
@"if (!a | !b) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_ParenthesizeAndForPrecedence()
        {
            await TestFixOneAsync(
@"[||]if (a && b || c) { a(); } else { b(); }",
@"if ((!a || !b) && !c) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_Plus()
        {
            await TestFixOneAsync(
@"[||]if (a + b) { a(); } else { b(); }",
@"if (!(a + b)) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_True()
        {
            await TestFixOneAsync(
@"[||]if (true) { a(); } else { b(); }",
@"if (false) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_TrueWithTrivia()
        {
            await TestFixOneAsync(
@"[||]if (/*1*/true/*2*/) { a(); } else { b(); }",
@"if (/*1*/false/*2*/) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_False()
        {
            await TestFixOneAsync(
@"[||]if (false) { a(); } else { b(); }",
@"if (true) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_OtherLiteralExpression()
        {
            await TestFixOneAsync(
@"[||]if (literalexpression) { a(); } else { b(); }",
@"if (!literalexpression) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_TrueAndFalse()
        {
            await TestFixOneAsync(
@"[||]if (true && false) { a(); } else { b(); }",
@"if (false || true) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_NoCurlyBraces()
        {
            await TestFixOneAsync(
@"[||]if (a) a(); else b();",
@"if (!a) b(); else a();");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_CurlyBracesOnIf()
        {
            await TestFixOneAsync(
@"[||]if (a) { a(); } else b();",
@"if (!a) b(); else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_CurlyBracesOnElse()
        {
            await TestFixOneAsync(
@"[||]if (a) a(); else { b(); }",
@"if (!a) { b(); } else a();");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_IfElseIf()
        {
            await TestFixOneAsync(
@"[||]if (a) { a(); } else if (b) { b(); }",
@"if (!a) { if (b) { b(); } } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_IfElseIfElse()
        {
            await TestFixOneAsync(
@"[||]if (a) { a(); } else if (b) { b(); } else { c(); }",
@"if (!a) { if (b) { b(); } else { c(); } } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_CompoundConditional()
        {
            await TestFixOneAsync(
@"[||]if (((a == b) && (c != d)) || ((e < f) && (!g))) { a(); } else { b(); }",
@"if ((a != b || c == d) && (e >= f || g)) { b(); } else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_Trivia()
        {
            await TestFixOneAsync(
@"[||]if /*1*/ (a) /*2*/ { /*3*/ a() /*4*/; /*5*/ } /*6*/ else if /*7*/ (b) /*8*/ { /*9*/ b(); /*10*/ } /*11*/ else /*12*/ { /*13*/ c(); /*14*/} /*15*/",
@"if /*1*/ (!a) /*2*/ { if /*7*/ (b) /*8*/ { /*9*/ b(); /*10*/ } /*11*/ else /*12*/ { /*13*/ c(); /*14*/} /*6*/ } else { /*3*/ a() /*4*/; /*5*/ } /*15*/");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestKeepTriviaWithinExpression_BrokenCode()
        {
            await TestInRegularAndScriptAsync(
@"class A
{
    void Goo()
    {
        [||]if (a ||
        b &&
        c < // comment
        d)
        {
            a();
        }
        else
        {
            b();
        }
    }
}",
@"class A
{
    void Goo()
    {
        if (!a &&
        (!b ||
        c >= // comment
        d))
        {
            b();
        }
        else
        {
            a();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestKeepTriviaWithinExpression()
        {
            await TestInRegularAndScriptAsync(
@"class A
{
    void Goo()
    {
        bool a = true;
        bool b = true;
        bool c = true;
        bool d = true;

        [||]if (a ||
        b &&
        c < // comment
        d)
        {
            a();
        }
        else
        {
            b();
        }
    }
}",
@"class A
{
    void Goo()
    {
        bool a = true;
        bool b = true;
        bool c = true;
        bool d = true;

        if (!a &&
        (!b ||
        c >= // comment
        d))
        {
            b();
        }
        else
        {
            a();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestMultiline_IfElseIfElse()
        {
            await TestInRegularAndScriptAsync(
@"class A
{
    void Goo()
    {
        [||]if (a)
        {
            a();
        }
        else if (b)
        {
            b();
        }
        else
        {
            c();
        }
    }
}",
@"class A
{
    void Goo()
    {
        if (!a)
        {
            if (b)
            {
                b();
            }
            else
            {
                c();
            }
        }
        else
        {
            a();
        }
    }
}");
        }

        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestMultiline_IfElseIfElseSelection1()
        {
            await TestInRegularAndScriptAsync(
@"class A
{
    void Goo()
    {
        [|if (a)
        {
            a();
        }
        else if (b)
        {
            b();
        }
        else
        {
            c();
        }|]
    }
}",
@"class A
{
    void Goo()
    {
        if (!a)
        {
            if (b)
            {
                b();
            }
            else
            {
                c();
            }
        }
        else
        {
            a();
        }
    }
}");
        }

        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestMultiline_IfElseIfElseSelection2()
        {
            await TestInRegularAndScriptAsync(
@"class A
{
    void Goo()
    {
        [|if (a)
        {
            a();
        }|]
        else if (b)
        {
            b();
        }
        else
        {
            c();
        }
    }
}",
@"class A
{
    void Goo()
    {
        if (!a)
        {
            if (b)
            {
                b();
            }
            else
            {
                c();
            }
        }
        else
        {
            a();
        }
    }
}");
        }

        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestMultilineMissing_IfElseIfElseSubSelection()
        {
            await TestMissingInRegularAndScriptAsync(
@"class A
{
    void Goo()
    {
        if (a)
        {
            a();
        }
        [|else if (b)
        {
            b();
        }
        else
        {
            c();
        }|]
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestMultiline_IfElse()
        {
            await TestInRegularAndScriptAsync(
@"class A
{
    void Goo()
    {
        [||]if (foo) 
            bar();
        else
            if (baz)
                Quux();
    }
}",
@"class A
{
    void Goo()
    {
        if (!foo)
        {
            if (baz)
                Quux();
        }
        else
            bar();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestMultiline_OpenCloseBracesSameLine()
        {
            await TestInRegularAndScriptAsync(
@"class A
{
    void Goo()
    {
        [||]if (foo) {
           x();
           x();
        } else {
           y();
           y();
        }
    }
}",
@"class A
{
    void Goo()
    {
        if (!foo)
        {
            y();
            y();
        }
        else
        {
            x();
            x();
        }
    }
}");
        }
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestMultiline_Trivia()
        {
            await TestInRegularAndScriptAsync(
@"class A
{
    void Goo()
    { /*1*/
        [||]if (a) /*2*/
        { /*3*/
            /*4*/
            goo(); /*5*/
            /*6*/
        } /*7*/
        else if (b) /*8*/
        { /*9*/
            /*10*/
            goo(); /*11*/
            /*12*/
        } /*13*/
        else /*14*/
        { /*15*/
            /*16*/
            goo(); /*17*/
            /*18*/
        } /*19*/
        /*20*/
    }
}",
@"class A
{
    void Goo()
    { /*1*/
        if (!a) /*2*/
        {
            if (b) /*8*/
            { /*9*/
                /*10*/
                goo(); /*11*/
                /*12*/
            } /*13*/
            else /*14*/
            { /*15*/
                /*16*/
                goo(); /*17*/
                /*18*/
            } /*19*/
        }
        else
        { /*3*/
            /*4*/
            goo(); /*5*/
            /*6*/
        } /*7*/
        /*20*/
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestOverlapsHiddenPosition1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void F()
    {
#line hidden
        [||]if (a)
        {
            a();
        }
        else
        {
            b();
        }
#line default
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestOverlapsHiddenPosition2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void F()
    {
        [||]if (a)
        {
#line hidden
            a();
#line default
        }
        else
        {
            b();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestOverlapsHiddenPosition3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void F()
    {
        [||]if (a)
        {
            a();
        }
        else
        {
#line hidden
            b();
#line default
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestOverlapsHiddenPosition4()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void F()
    {
        [||]if (a)
        {
#line hidden
            a();
        }
        else
        {
            b();
#line default
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestOverlapsHiddenPosition5()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void F()
    {
        [||]if (a)
        {
            a();
#line hidden
        }
        else
        {
#line default
            b();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestOverlapsHiddenPosition6()
        {
            await TestInRegularAndScriptAsync(
@"
#line hidden
class C 
{
    void F()
    {
#line default
        [||]if (a)
        {
            a();
        }
        else
        {
            b();
        }
    }
}",

@"
#line hidden
class C 
{
    void F()
    {
#line default
        if (!a)
        {
            b();
        }
        else
        {
            a();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestOverlapsHiddenPosition7()
        {
            await TestInRegularAndScriptAsync(
@"
#line hidden
class C 
{
    void F()
    {
#line default
        [||]if (a)
        {
            a();
        }
        else
        {
            b();
        }
#line hidden
    }
}
#line default",

@"
#line hidden
class C 
{
    void F()
    {
#line default
        if (!a)
        {
            b();
        }
        else
        {
            a();
        }
#line hidden
    }
}
#line default");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_SimplifyToLengthEqualsZero()
        {
            await TestFixOneAsync(
@"string x; [||]if (x.Length > 0) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string x; if (x.Length == 0) { EqualsZero(); } else { GreaterThanZero(); } } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_SimplifyToLengthEqualsZero2()
        {
            await TestFixOneAsync(
@"string[] x; [||]if (x.Length > 0) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string[] x; if (x.Length == 0) { EqualsZero(); } else { GreaterThanZero(); } } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_SimplifyToLengthEqualsZero3()
        {
            await TestFixOneAsync(
@"string x; [||]if (x.Length > 0x0) { a(); } else { b(); } } } ",
@"string x; if (x.Length == 0x0) { b(); } else { a(); } } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_SimplifyToLengthEqualsZero4()
        {
            await TestFixOneAsync(
@"string x; [||]if (0 < x.Length) { a(); } else { b(); } } } ",
@"string x; if (0 == x.Length) { b(); } else { a(); } } } ");
        }

        [WorkItem(545986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_SimplifyToEqualsZero1()
        {
            await TestFixOneAsync(
@"byte x = 1; [||]if (0 < x) { a(); } else { b(); } } } ",
@"byte x = 1; if (0 == x) { b(); } else { a(); } } } ");
        }

        [WorkItem(545986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_SimplifyToEqualsZero2()
        {
            await TestFixOneAsync(
@"ushort x = 1; [||]if (0 < x) { a(); } else { b(); } } } ",
@"ushort x = 1; if (0 == x) { b(); } else { a(); } } } ");
        }

        [WorkItem(545986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_SimplifyToEqualsZero3()
        {
            await TestFixOneAsync(
@"uint x = 1; [||]if (0 < x) { a(); } else { b(); } } } ",
@"uint x = 1; if (0 == x) { b(); } else { a(); } } } ");
        }

        [WorkItem(545986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_SimplifyToEqualsZero4()
        {
            await TestFixOneAsync(
@"ulong x = 1; [||]if (x > 0) { a(); } else { b(); } } } ",
@"ulong x = 1; if (x == 0) { b(); } else { a(); } } } ");
        }

        [WorkItem(545986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_SimplifyToNotEqualsZero1()
        {
            await TestFixOneAsync(
@"ulong x = 1; [||]if (0 == x) { a(); } else { b(); } } } ",
@"ulong x = 1; if (0 != x) { b(); } else { a(); } } } ");
        }

        [WorkItem(545986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_SimplifyToNotEqualsZero2()
        {
            await TestFixOneAsync(
@"ulong x = 1; [||]if (x == 0) { a(); } else { b(); } } } ",
@"ulong x = 1; if (x != 0) { b(); } else { a(); } } } ");
        }

        [WorkItem(530505, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530505")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_SimplifyLongLengthEqualsZero()
        {
            await TestFixOneAsync(
@"string[] x; [||]if (x.LongLength > 0) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string[] x; if (x.LongLength == 0) { EqualsZero(); } else { GreaterThanZero(); } } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_DoesNotSimplifyToLengthEqualsZero()
        {
            await TestFixOneAsync(
@"string x; [||]if (x.Length >= 0) { a(); } else { b(); } } } ",
@"string x; if (x.Length < 0) { b(); } else { a(); } } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSingleLine_DoesNotSimplifyToLengthEqualsZero2()
        {
            await TestFixOneAsync(
@"string x; [||]if (x.Length > 0.0f) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string x; if (x.Length <= 0.0f) { EqualsZero(); } else { GreaterThanZero(); } } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        [WorkItem(29434, "https://github.com/dotnet/roslyn/issues/29434")]
        public async Task TestIsExpression()
        {
            await TestInRegularAndScriptAsync(
@"class C { void M(object o) { [||]if (o is C) { a(); } else { } } }",
@"class C { void M(object o) { if (o is not C) { } else { a(); } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        [WorkItem(43224, "https://github.com/dotnet/roslyn/issues/43224")]
        public async Task TestEmptyIf()
        {
            await TestInRegularAndScriptAsync(
                @"class C { void M(string s){ [||]if (s == ""a""){}else{ s = ""b""}}}",
                @"class C { void M(string s){ if (s != ""a""){ s = ""b""}}}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        [WorkItem(43224, "https://github.com/dotnet/roslyn/issues/43224")]
        public async Task TestOnlySingleLineCommentIf()
        {
            await TestInRegularAndScriptAsync(
                @"
class C 
{
    void M(string s)
    {
        [||]if (s == ""a"")
        {
            // A single line comment
        }
        else
        {
            s = ""b""
        }
    }
}",
                @"
class C 
{
    void M(string s)
    {
        if (s != ""a"")
        {
            s = ""b""
        }
        else
        {
            // A single line comment
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        [WorkItem(43224, "https://github.com/dotnet/roslyn/issues/43224")]
        public async Task TestOnlyMultilineLineCommentIf()
        {
            await TestInRegularAndScriptAsync(
                @"
class C 
{ 
    void M(string s)
    {
        [||]if (s == ""a"")
        {
            /*
            * This is
            * a multiline
            * comment with
            * two words
            * per line.
            */
        }
        else
        {
            s = ""b""
        }
    }
}",
                @"
class C 
{ 
    void M(string s)
    {
        if (s != ""a"")
        {
            s = ""b""
        }
        else
        {
            /*
            * This is
            * a multiline
            * comment with
            * two words
            * per line.
            */
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        [WorkItem(51359, "https://github.com/dotnet/roslyn/issues/51359")]
        public async Task TestIsCheck_CSharp6()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        [||]if (c is object)
        {
            return 1;
        }
        else
        {
            return 2;
        }
    }
}",
@"class C
{
    int M()
    {
        if (!(c is object))
        {
            return 2;
        }
        else
        {
            return 1;
        }
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        [WorkItem(51359, "https://github.com/dotnet/roslyn/issues/51359")]
        public async Task TestIsCheck_CSharp8()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        [||]if (c is object)
        {
            return 1;
        }
        else
        {
            return 2;
        }
    }
}",
@"class C
{
    int M()
    {
        if (c is null)
        {
            return 2;
        }
        else
        {
            return 1;
        }
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        [WorkItem(51359, "https://github.com/dotnet/roslyn/issues/51359")]
        public async Task TestIsCheck_CSharp9()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        [||]if (c is object)
        {
            return 1;
        }
        else
        {
            return 2;
        }
    }
}",
@"class C
{
    int M()
    {
        if (c is null)
        {
            return 2;
        }
        else
        {
            return 1;
        }
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        [WorkItem(51359, "https://github.com/dotnet/roslyn/issues/51359")]
        public async Task TestIsNotObjectCheck_CSharp8()
        {
            // Not terrific.  But the starting code is not legal C#8 either.  In this case because we don't even support
            // 'not' patterns wee dont' bother diving into the pattern to negate it, and we instead just negate the
            // expression.
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        [||]if (c is not object)
        {
            return 1;
        }
        else
        {
            return 2;
        }
    }
}",
@"class C
{
    int M()
    {
        if (c is object)
        {
            return 2;
        }
        else
        {
            return 1;
        }
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        [WorkItem(51359, "https://github.com/dotnet/roslyn/issues/51359")]
        public async Task TestIsNotObjectCheck_CSharp9()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        [||]if (c is not object)
        {
            return 1;
        }
        else
        {
            return 2;
        }
    }
}",
@"class C
{
    int M()
    {
        if (c is not null)
        {
            return 2;
        }
        else
        {
            return 1;
        }
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9));
        }
    }
}
