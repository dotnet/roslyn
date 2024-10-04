// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.InvertIf;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InvertIf;

[UseExportProvider, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
public partial class InvertIfTests
{
    private static Task TestInsideMethodAsync(
        string initial,
        string expected)
    {
        return TestAsync(CreateTreeText(initial), CreateTreeText(expected));

        static string CreateTreeText(string initial)
        {
            return $$"""
                class A
                {
                    bool a = true;
                    bool b = true;
                    bool c = true;
                    bool d = true;

                    void Goo()
                    {
                        {{initial}}
                    }
                }
                """;
        }
    }

    private static async Task TestAsync(string initial, string expected, LanguageVersion languageVersion = LanguageVersion.Latest)
    {
        await new CSharpCodeRefactoringVerifier<CSharpInvertIfCodeRefactoringProvider>.Test
        {
            TestCode = initial,
            FixedCode = expected,
            LanguageVersion = languageVersion,
            CompilerDiagnostics = CompilerDiagnostics.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSingleLine_Identifier()
    {
        await TestInsideMethodAsync(
@"[||]if (a) { a(); } else { b(); }",
@"if (!a) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_IdentifierWithTrivia()
    {
        await TestInsideMethodAsync(
@"[||]if /*0*/(/*1*/a/*2*/)/*3*/ { a(); } else { b(); }",
@"if /*0*/(/*1*/!a/*2*/)/*3*/ { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_NotIdentifier()
    {
        await TestInsideMethodAsync(
@"[||]if (!a) { a(); } else { b(); }",
@"if (a) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_NotIdentifierWithTrivia()
    {
        await TestInsideMethodAsync(
@"[||]if /*0*/(/*1*/!/*1b*/a/*2*/)/*3*/ { a(); } else { b(); }",
@"if /*0*/(/*1*/a/*2*/)/*3*/ { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_EqualsEquals()
    {
        await TestInsideMethodAsync(
@"[||]if (a == b) { a(); } else { b(); }",
@"if (a != b) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_NotEquals()
    {
        await TestInsideMethodAsync(
@"[||]if (a != b) { a(); } else { b(); }",
@"if (a == b) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_GreaterThan()
    {
        await TestInsideMethodAsync(
@"[||]if (a > b) { a(); } else { b(); }",
@"if (a <= b) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_GreaterThanEquals()
    {
        await TestInsideMethodAsync(
@"[||]if (a >= b) { a(); } else { b(); }",
@"if (a < b) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_LessThan()
    {
        await TestInsideMethodAsync(
@"[||]if (a < b) { a(); } else { b(); }",
@"if (a >= b) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_LessThanEquals()
    {
        await TestInsideMethodAsync(
@"[||]if (a <= b) { a(); } else { b(); }",
@"if (a > b) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_DoubleParentheses()
    {
        await TestInsideMethodAsync(
@"[||]if ((a)) { a(); } else { b(); }",
@"if (!a) { b(); } else { a(); }");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/26427")]
    public async Task TestSingleLine_DoubleParenthesesWithInnerTrivia()
    {
        await TestInsideMethodAsync(
@"[||]if ((/*1*/a/*2*/)) { a(); } else { b(); }",
@"if (/*1*/!a/*2*/) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_DoubleParenthesesWithMiddleTrivia()
    {
        await TestInsideMethodAsync(
@"[||]if (/*1*/(a)/*2*/) { a(); } else { b(); }",
@"if (/*1*/!a/*2*/) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_DoubleParenthesesWithOutsideTrivia()
    {
        await TestInsideMethodAsync(
@"[||]if /*before*/((a))/*after*/ { a(); } else { b(); }",
@"if /*before*/(!a)/*after*/ { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_Is()
    {
        await TestInsideMethodAsync(
@"[||]if (a is Goo) { a(); } else { b(); }",
@"if (a is not Goo) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_MethodCall()
    {
        await TestInsideMethodAsync(
@"[||]if (a.Goo()) { a(); } else { b(); }",
@"if (!a.Goo()) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_Or()
    {
        await TestInsideMethodAsync(
@"[||]if (a || b) { a(); } else { b(); }",
@"if (!a && !b) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_Or2()
    {
        await TestInsideMethodAsync(
@"[||]if (!a || !b) { a(); } else { b(); }",
@"if (a && b) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_Or3()
    {
        await TestInsideMethodAsync(
@"[||]if (!a || b) { a(); } else { b(); }",
@"if (a && !b) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_Or4()
    {
        await TestInsideMethodAsync(
@"[||]if (a | b) { a(); } else { b(); }",
@"if (!a & !b) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_And()
    {
        await TestInsideMethodAsync(
@"[||]if (a && b) { a(); } else { b(); }",
@"if (!a || !b) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_And2()
    {
        await TestInsideMethodAsync(
@"[||]if (!a && !b) { a(); } else { b(); }",
@"if (a || b) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_And3()
    {
        await TestInsideMethodAsync(
@"[||]if (!a && b) { a(); } else { b(); }",
@"if (a || !b) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_And4()
    {
        await TestInsideMethodAsync(
@"[||]if (a & b) { a(); } else { b(); }",
@"if (!a | !b) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_ParenthesizeAndForPrecedence()
    {
        await TestInsideMethodAsync(
@"[||]if (a && b || c) { a(); } else { b(); }",
@"if ((!a || !b) && !c) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_Plus()
    {
        await TestInsideMethodAsync(
@"[||]if (a + b) { a(); } else { b(); }",
@"if (!(a + b)) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_True()
    {
        await TestInsideMethodAsync(
@"[||]if (true) { a(); } else { b(); }",
@"if (false) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_TrueWithTrivia()
    {
        await TestInsideMethodAsync(
@"[||]if (/*1*/true/*2*/) { a(); } else { b(); }",
@"if (/*1*/false/*2*/) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_False()
    {
        await TestInsideMethodAsync(
@"[||]if (false) { a(); } else { b(); }",
@"if (true) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_OtherLiteralExpression()
    {
        await TestInsideMethodAsync(
@"[||]if (literalexpression) { a(); } else { b(); }",
@"if (!literalexpression) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_TrueAndFalse()
    {
        await TestInsideMethodAsync(
@"[||]if (true && false) { a(); } else { b(); }",
@"if (false || true) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_NoCurlyBraces()
    {
        await TestInsideMethodAsync(
@"[||]if (a) a(); else b();",
@"if (!a) b(); else a();");
    }

    [Fact]
    public async Task TestSingleLine_CurlyBracesOnIf()
    {
        await TestInsideMethodAsync(
@"[||]if (a) { a(); } else b();",
@"if (!a) b(); else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_CurlyBracesOnElse()
    {
        await TestInsideMethodAsync(
@"[||]if (a) a(); else { b(); }",
@"if (!a) { b(); } else a();");
    }

    [Fact]
    public async Task TestSingleLine_IfElseIf()
    {
        await TestInsideMethodAsync(
@"[||]if (a) { a(); } else if (b) { b(); }",
@"if (!a) { if (b) { b(); } } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_IfElseIfElse()
    {
        await TestInsideMethodAsync(
@"[||]if (a) { a(); } else if (b) { b(); } else { c(); }",
@"if (!a) { if (b) { b(); } else { c(); } } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_CompoundConditional()
    {
        await TestInsideMethodAsync(
@"[||]if (((a == b) && (c != d)) || ((e < f) && (!g))) { a(); } else { b(); }",
@"if ((a != b || c == d) && (e >= f || g)) { b(); } else { a(); }");
    }

    [Fact]
    public async Task TestSingleLine_Trivia()
    {
        await TestInsideMethodAsync(
@"[||]if /*1*/ (a) /*2*/ { /*3*/ a() /*4*/; /*5*/ } /*6*/ else if /*7*/ (b) /*8*/ { /*9*/ b(); /*10*/ } /*11*/ else /*12*/ { /*13*/ c(); /*14*/} /*15*/",
@"if /*1*/ (!a) /*2*/ { if /*7*/ (b) /*8*/ { /*9*/ b(); /*10*/ } /*11*/ else /*12*/ { /*13*/ c(); /*14*/} /*6*/ } else { /*3*/ a() /*4*/; /*5*/ } /*15*/");
    }

    [Fact]
    public async Task TestKeepTriviaWithinExpression_BrokenCode()
    {
        await TestAsync("""
            class A
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
            }
            """, """
            class A
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
            }
            """);
    }

    [Fact]
    public async Task TestKeepTriviaWithinExpression()
    {
        await TestAsync("""
            class A
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
            }
            """, """
            class A
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
            }
            """);
    }

    [Fact]
    public async Task TestMultiline_IfElseIfElse()
    {
        await TestAsync("""
            class A
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
            }
            """, """
            class A
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMultiline_IfElseIfElseSelection1()
    {
        await TestAsync("""
            class A
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
            }
            """, """
            class A
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMultiline_IfElseIfElseSelection2()
    {
        await TestAsync("""
            class A
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
            }
            """, """
            class A
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMultilineMissing_IfElseIfElseSubSelection()
    {
        var code = """
            class A
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
            }
            """;

        await TestAsync(code, code);
    }

    [Fact]
    public async Task TestMultiline_IfElse()
    {
        await TestAsync("""
            class A
            {
                void Goo()
                {
                    [||]if (foo) 
                        bar();
                    else
                        if (baz)
                            Quux();
                }
            }
            """, """
            class A
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
            }
            """);
    }

    [Fact]
    public async Task TestMultiline_OpenCloseBracesSameLine()
    {
        await TestAsync("""
            class A
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
            }
            """, """
            class A
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
            }
            """);
    }

    [Fact]
    public async Task TestMultiline_Trivia()
    {
        await TestAsync("""
            class A
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
            }
            """, """
            class A
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
            }
            """);
    }

    [Fact]
    public async Task TestOverlapsHiddenPosition1()
    {
        var code = """
            class C
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
            }
            """;

        await TestAsync(code, code);
    }

    [Fact]
    public async Task TestOverlapsHiddenPosition2()
    {
        var code = """
            class C
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
            }
            """;

        await TestAsync(code, code);
    }

    [Fact]
    public async Task TestOverlapsHiddenPosition3()
    {
        var code = """
            class C
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
            }
            """;

        await TestAsync(code, code);
    }

    [Fact]
    public async Task TestOverlapsHiddenPosition4()
    {
        var code = """
            class C
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
            }
            """;

        await TestAsync(code, code);
    }

    [Fact]
    public async Task TestOverlapsHiddenPosition5()
    {
        var code = """
            class C
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
            }
            """;

        await TestAsync(code, code);
    }

    [Fact]
    public async Task TestOverlapsHiddenPosition6()
    {
        await TestAsync("""
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
            }
            """, """
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
            }
            """);

    }

    [Fact]
    public async Task TestOverlapsHiddenPosition7()
    {
        await TestAsync("""
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
            #line default
            """, """
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
            #line default
            """);
    }

    [Fact]
    public async Task TestSingleLine_SimplifyToLengthEqualsZero()
    {
        await TestInsideMethodAsync(
@"string x; [||]if (x.Length > 0) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string x; if (x.Length == 0) { EqualsZero(); } else { GreaterThanZero(); } } } ");
    }

    [Fact]
    public async Task TestSingleLine_SimplifyToLengthEqualsZero2()
    {
        await TestInsideMethodAsync(
@"string[] x; [||]if (x.Length > 0) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string[] x; if (x.Length == 0) { EqualsZero(); } else { GreaterThanZero(); } } } ");
    }

    [Fact]
    public async Task TestSingleLine_SimplifyToLengthEqualsZero3()
    {
        await TestInsideMethodAsync(
@"string x; [||]if (x.Length > 0x0) { a(); } else { b(); } } } ",
@"string x; if (x.Length == 0x0) { b(); } else { a(); } } } ");
    }

    [Fact]
    public async Task TestSingleLine_SimplifyToLengthEqualsZero4()
    {
        await TestInsideMethodAsync(
@"string x; [||]if (0 < x.Length) { a(); } else { b(); } } } ",
@"string x; if (0 == x.Length) { b(); } else { a(); } } } ");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
    public async Task TestSingleLine_SimplifyToEqualsZero1()
    {
        await TestInsideMethodAsync(
@"byte x = 1; [||]if (0 < x) { a(); } else { b(); } } } ",
@"byte x = 1; if (0 == x) { b(); } else { a(); } } } ");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
    public async Task TestSingleLine_SimplifyToEqualsZero2()
    {
        await TestInsideMethodAsync(
@"ushort x = 1; [||]if (0 < x) { a(); } else { b(); } } } ",
@"ushort x = 1; if (0 == x) { b(); } else { a(); } } } ");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
    public async Task TestSingleLine_SimplifyToEqualsZero3()
    {
        await TestInsideMethodAsync(
@"uint x = 1; [||]if (0 < x) { a(); } else { b(); } } } ",
@"uint x = 1; if (0 == x) { b(); } else { a(); } } } ");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
    public async Task TestSingleLine_SimplifyToEqualsZero4()
    {
        await TestInsideMethodAsync(
@"ulong x = 1; [||]if (x > 0) { a(); } else { b(); } } } ",
@"ulong x = 1; if (x == 0) { b(); } else { a(); } } } ");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
    public async Task TestSingleLine_SimplifyToNotEqualsZero1()
    {
        await TestInsideMethodAsync(
@"ulong x = 1; [||]if (0 == x) { a(); } else { b(); } } } ",
@"ulong x = 1; if (0 != x) { b(); } else { a(); } } } ");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
    public async Task TestSingleLine_SimplifyToNotEqualsZero2()
    {
        await TestInsideMethodAsync(
@"ulong x = 1; [||]if (x == 0) { a(); } else { b(); } } } ",
@"ulong x = 1; if (x != 0) { b(); } else { a(); } } } ");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530505")]
    public async Task TestSingleLine_SimplifyLongLengthEqualsZero()
    {
        await TestInsideMethodAsync(
@"string[] x; [||]if (x.LongLength > 0) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string[] x; if (x.LongLength == 0) { EqualsZero(); } else { GreaterThanZero(); } } } ");
    }

    [Fact]
    public async Task TestSingleLine_DoesNotSimplifyToLengthEqualsZero()
    {
        await TestInsideMethodAsync(
@"string x; [||]if (x.Length >= 0) { a(); } else { b(); } } } ",
@"string x; if (x.Length < 0) { b(); } else { a(); } } } ");
    }

    [Fact]
    public async Task TestSingleLine_DoesNotSimplifyToLengthEqualsZero2()
    {
        await TestInsideMethodAsync(
@"string x; [||]if (x.Length > 0.0f) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string x; if (x.Length <= 0.0f) { EqualsZero(); } else { GreaterThanZero(); } } } ");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29434")]
    public async Task TestIsExpression()
    {
        await TestAsync(
@"class C { void M(object o) { [||]if (o is C) { a(); } else { } } }",
@"class C { void M(object o) { if (o is not C) { } else { a(); } } }");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43224")]
    public async Task TestEmptyIf()
    {
        await TestAsync(
            @"class C { void M(string s){ [||]if (s == ""a""){}else{ s = ""b""}}}",
            @"class C { void M(string s){ if (s != ""a"") { s = ""b""}}}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43224")]
    public async Task TestOnlySingleLineCommentIf()
    {
        await TestAsync("""
            class C 
            {
                void M(string s)
                {
                    [||]if (s == "a")
                    {
                        // A single line comment
                    }
                    else
                    {
                        s = "b"
                    }
                }
            }
            """, """
            class C 
            {
                void M(string s)
                {
                    if (s != "a")
                    {
                        s = "b"
                    }
                    else
                    {
                        // A single line comment
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43224")]
    public async Task TestOnlyMultilineLineCommentIf()
    {
        await TestAsync("""
            class C 
            { 
                void M(string s)
                {
                    [||]if (s == "a")
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
                        s = "b"
                    }
                }
            }
            """, """
            class C 
            { 
                void M(string s)
                {
                    if (s != "a")
                    {
                        s = "b"
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51359")]
    public async Task TestIsCheck_CSharp6()
    {
        await TestAsync("""
            class C
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
            }
            """, """
            class C
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
            }
            """, LanguageVersion.CSharp6);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51359")]
    public async Task TestIsCheck_CSharp8()
    {
        await TestAsync("""
            class C
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
            }
            """, """
            class C
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
            }
            """, LanguageVersion.CSharp8);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51359")]
    public async Task TestIsCheck_CSharp9()
    {
        await TestAsync("""
            class C
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
            }
            """, """
            class C
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
            }
            """, LanguageVersion.CSharp9);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51359")]
    public async Task TestIsNotObjectCheck_CSharp8()
    {
        // Not terrific.  But the starting code is not legal C#8 either.  In this case because we don't even support
        // 'not' patterns wee don't bother diving into the pattern to negate it, and we instead just negate the
        // expression.
        await TestAsync("""
            class C
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
            }
            """, """
            class C
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
            }
            """, LanguageVersion.CSharp8);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51359")]
    public async Task TestIsNotObjectCheck_CSharp9()
    {
        await TestAsync("""
            class C
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
            }
            """, """
            class C
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
            }
            """, LanguageVersion.CSharp9);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63311")]
    public async Task TestLiftedNullable_GreaterThan()
    {
        await TestAsync("""
            class C
            {
                void M(int? p)
                {
                    [||]if (p > 10)
                    {
                        System.Console.WriteLine("p is not null and p.Value > 10");
                    }
                }
            }
            """, """
            class C
            {
                void M(int? p)
                {
                    if (!(p > 10))
                    {
                        return;
                    }
                    System.Console.WriteLine("p is not null and p.Value > 10");
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63311")]
    public async Task TestLiftedNullable_GreaterThanOrEqual()
    {
        await TestAsync("""
            class C
            {
                void M(int? p)
                {
                    [||]if (p >= 10)
                    {
                        System.Console.WriteLine("p is not null and p.Value >= 10");
                    }
                }
            }
            """, """
            class C
            {
                void M(int? p)
                {
                    if (!(p >= 10))
                    {
                        return;
                    }
                    System.Console.WriteLine("p is not null and p.Value >= 10");
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63311")]
    public async Task TestLiftedNullable_LessThan()
    {
        await TestAsync("""
            class C
            {
                void M(int? p)
                {
                    [||]if (p < 10)
                    {
                        System.Console.WriteLine("p is not null and p.Value < 10");
                    }
                }
            }
            """, """
            class C
            {
                void M(int? p)
                {
                    if (!(p < 10))
                    {
                        return;
                    }
                    System.Console.WriteLine("p is not null and p.Value < 10");
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63311")]
    public async Task TestLiftedNullable_LessThanOrEqual()
    {
        await TestAsync("""
            class C
            {
                void M(int? p)
                {
                    [||]if (p <= 10)
                    {
                        System.Console.WriteLine("p is not null and p.Value <= 10");
                    }
                }
            }
            """, """
            class C
            {
                void M(int? p)
                {
                    if (!(p <= 10))
                    {
                        return;
                    }
                    System.Console.WriteLine("p is not null and p.Value <= 10");
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63311")]
    public async Task TestNullableReference_GreaterThan()
    {
        await TestAsync("""
            #nullable enable
            using System;
            class C
            {
                void M(C? p)
                {
                    [||]if (p > new C())
                    {
                        Console.WriteLine("Null-handling semantics may actually change depending on the operator implementation");
                    }
                }

                public static bool operator <(C? left, C? right) => throw new NotImplementedException();
                public static bool operator >(C? left, C? right) => throw new NotImplementedException();
                public static bool operator <=(C? left, C? right) => throw new NotImplementedException();
                public static bool operator >=(C? left, C? right) => throw new NotImplementedException();
            }
            """, """
            #nullable enable
            using System;
            class C
            {
                void M(C? p)
                {
                    if (p <= new C())
                    {
                        return;
                    }
                    Console.WriteLine("Null-handling semantics may actually change depending on the operator implementation");
                }

                public static bool operator <(C? left, C? right) => throw new NotImplementedException();
                public static bool operator >(C? left, C? right) => throw new NotImplementedException();
                public static bool operator <=(C? left, C? right) => throw new NotImplementedException();
                public static bool operator >=(C? left, C? right) => throw new NotImplementedException();
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40585")]
    public async Task TestYieldBreak()
    {
        await TestAsync("""
            using System.Collections;

            class Program
            {
                public static IEnumerable Method(bool condition)
                {
                    [||]if (condition)
                    {
                        yield return 1;
                    }
                }
            }
            """, """
            using System.Collections;

            class Program
            {
                public static IEnumerable Method(bool condition)
                {
                    if (!condition)
                    {
                        yield break;
                    }
                    yield return 1;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42715")]
    public async Task PreserveSpacing()
    {
        await TestAsync("""
            class C
            {
                string? M(string s)
                {
                    var l = s.ToLowerCase();

                    [||]if (l == "hello")
                    {
                        return null;
                    }

                    return l;

                }
            }
            """, """
            class C
            {
                string? M(string s)
                {
                    var l = s.ToLowerCase();

                    if (l != "hello")
                    {
                        return l;
                    }

                    return null;

                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42715")]
    public async Task PreserveSpacing_WithComments()
    {
        await TestAsync("""
            class C
            {
                string? M(string s)
                {
                    var l = s.ToLowerCase();

                    [||]if (l == "hello")
                    {
                        // null 1
                        return null; // null 2
                        // null 3
                    }

                    // l 1
                    return l; // l 2
                    // l 3

                }
            }
            """, """
            class C
            {
                string? M(string s)
                {
                    var l = s.ToLowerCase();

                    if (l != "hello")
                    {
                        // l 1
                        return l; // l 2
                        // null 3
                    }

                    // null 1
                    return null; // null 2
                    // l 3

                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42715")]
    public async Task PreserveSpacing_NoTrivia()
    {
        await TestAsync("""
            class C
            {
                string? M(bool b)
                {[||]if(b){return(true);}return(false);}
            }
            """, """
            class C
            {
                string? M(bool b)
                { if (!b) { return (false); } return (true); }
            }
            """);
    }
}
