// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
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
public sealed partial class InvertIfTests
{
    private static Task TestInsideMethodAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initial,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expected)
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

    private static Task TestAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initial,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expected,
        LanguageVersion languageVersion = LanguageVersion.Latest)
        => new CSharpCodeRefactoringVerifier<CSharpInvertIfCodeRefactoringProvider>.Test
        {
            TestCode = initial,
            FixedCode = expected,
            LanguageVersion = languageVersion,
            CompilerDiagnostics = CompilerDiagnostics.None,
        }.RunAsync();

    [Fact]
    public Task TestSingleLine_Identifier()
        => TestInsideMethodAsync(
@"[||]if (a) { a(); } else { b(); }",
@"if (!a) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_IdentifierWithTrivia()
        => TestInsideMethodAsync(
@"[||]if /*0*/(/*1*/a/*2*/)/*3*/ { a(); } else { b(); }",
@"if /*0*/(/*1*/!a/*2*/)/*3*/ { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_NotIdentifier()
        => TestInsideMethodAsync(
@"[||]if (!a) { a(); } else { b(); }",
@"if (a) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_NotIdentifierWithTrivia()
        => TestInsideMethodAsync(
@"[||]if /*0*/(/*1*/!/*1b*/a/*2*/)/*3*/ { a(); } else { b(); }",
@"if /*0*/(/*1*/a/*2*/)/*3*/ { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_EqualsEquals()
        => TestInsideMethodAsync(
@"[||]if (a == b) { a(); } else { b(); }",
@"if (a != b) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_NotEquals()
        => TestInsideMethodAsync(
@"[||]if (a != b) { a(); } else { b(); }",
@"if (a == b) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_GreaterThan()
        => TestInsideMethodAsync(
@"[||]if (a > b) { a(); } else { b(); }",
@"if (a <= b) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_GreaterThanEquals()
        => TestInsideMethodAsync(
@"[||]if (a >= b) { a(); } else { b(); }",
@"if (a < b) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_LessThan()
        => TestInsideMethodAsync(
@"[||]if (a < b) { a(); } else { b(); }",
@"if (a >= b) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_LessThanEquals()
        => TestInsideMethodAsync(
@"[||]if (a <= b) { a(); } else { b(); }",
@"if (a > b) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_DoubleParentheses()
        => TestInsideMethodAsync(
@"[||]if ((a)) { a(); } else { b(); }",
@"if (!a) { b(); } else { a(); }");

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/26427")]
    public Task TestSingleLine_DoubleParenthesesWithInnerTrivia()
        => TestInsideMethodAsync(
@"[||]if ((/*1*/a/*2*/)) { a(); } else { b(); }",
@"if (/*1*/!a/*2*/) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_DoubleParenthesesWithMiddleTrivia()
        => TestInsideMethodAsync(
@"[||]if (/*1*/(a)/*2*/) { a(); } else { b(); }",
@"if (/*1*/!a/*2*/) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_DoubleParenthesesWithOutsideTrivia()
        => TestInsideMethodAsync(
@"[||]if /*before*/((a))/*after*/ { a(); } else { b(); }",
@"if /*before*/(!a)/*after*/ { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_Is()
        => TestInsideMethodAsync(
@"[||]if (a is Goo) { a(); } else { b(); }",
@"if (a is not Goo) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_MethodCall()
        => TestInsideMethodAsync(
@"[||]if (a.Goo()) { a(); } else { b(); }",
@"if (!a.Goo()) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_Or()
        => TestInsideMethodAsync(
@"[||]if (a || b) { a(); } else { b(); }",
@"if (!a && !b) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_Or2()
        => TestInsideMethodAsync(
@"[||]if (!a || !b) { a(); } else { b(); }",
@"if (a && b) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_Or3()
        => TestInsideMethodAsync(
@"[||]if (!a || b) { a(); } else { b(); }",
@"if (a && !b) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_Or4()
        => TestInsideMethodAsync(
@"[||]if (a | b) { a(); } else { b(); }",
@"if (!a & !b) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_And()
        => TestInsideMethodAsync(
@"[||]if (a && b) { a(); } else { b(); }",
@"if (!a || !b) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_And2()
        => TestInsideMethodAsync(
@"[||]if (!a && !b) { a(); } else { b(); }",
@"if (a || b) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_And3()
        => TestInsideMethodAsync(
@"[||]if (!a && b) { a(); } else { b(); }",
@"if (a || !b) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_And4()
        => TestInsideMethodAsync(
@"[||]if (a & b) { a(); } else { b(); }",
@"if (!a | !b) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_ParenthesizeAndForPrecedence()
        => TestInsideMethodAsync(
@"[||]if (a && b || c) { a(); } else { b(); }",
@"if ((!a || !b) && !c) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_Plus()
        => TestInsideMethodAsync(
@"[||]if (a + b) { a(); } else { b(); }",
@"if (!(a + b)) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_True()
        => TestInsideMethodAsync(
@"[||]if (true) { a(); } else { b(); }",
@"if (false) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_TrueWithTrivia()
        => TestInsideMethodAsync(
@"[||]if (/*1*/true/*2*/) { a(); } else { b(); }",
@"if (/*1*/false/*2*/) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_False()
        => TestInsideMethodAsync(
@"[||]if (false) { a(); } else { b(); }",
@"if (true) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_OtherLiteralExpression()
        => TestInsideMethodAsync(
@"[||]if (literalexpression) { a(); } else { b(); }",
@"if (!literalexpression) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_TrueAndFalse()
        => TestInsideMethodAsync(
@"[||]if (true && false) { a(); } else { b(); }",
@"if (false || true) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_NoCurlyBraces()
        => TestInsideMethodAsync(
@"[||]if (a) a(); else b();",
@"if (!a) b(); else a();");

    [Fact]
    public Task TestSingleLine_CurlyBracesOnIf()
        => TestInsideMethodAsync(
@"[||]if (a) { a(); } else b();",
@"if (!a) b(); else { a(); }");

    [Fact]
    public Task TestSingleLine_CurlyBracesOnElse()
        => TestInsideMethodAsync(
@"[||]if (a) a(); else { b(); }",
@"if (!a) { b(); } else a();");

    [Fact]
    public Task TestSingleLine_IfElseIf()
        => TestInsideMethodAsync(
@"[||]if (a) { a(); } else if (b) { b(); }",
@"if (!a) { if (b) { b(); } } else { a(); }");

    [Fact]
    public Task TestSingleLine_IfElseIfElse()
        => TestInsideMethodAsync(
@"[||]if (a) { a(); } else if (b) { b(); } else { c(); }",
@"if (!a) { if (b) { b(); } else { c(); } } else { a(); }");

    [Fact]
    public Task TestSingleLine_CompoundConditional()
        => TestInsideMethodAsync(
@"[||]if (((a == b) && (c != d)) || ((e < f) && (!g))) { a(); } else { b(); }",
@"if ((a != b || c == d) && (e >= f || g)) { b(); } else { a(); }");

    [Fact]
    public Task TestSingleLine_Trivia()
        => TestInsideMethodAsync(
@"[||]if /*1*/ (a) /*2*/ { /*3*/ a() /*4*/; /*5*/ } /*6*/ else if /*7*/ (b) /*8*/ { /*9*/ b(); /*10*/ } /*11*/ else /*12*/ { /*13*/ c(); /*14*/} /*15*/",
@"if /*1*/ (!a) /*2*/ { if /*7*/ (b) /*8*/ { /*9*/ b(); /*10*/ } /*11*/ else /*12*/ { /*13*/ c(); /*14*/} /*6*/ } else { /*3*/ a() /*4*/; /*5*/ } /*15*/");

    [Fact]
    public Task TestKeepTriviaWithinExpression_BrokenCode()
        => TestAsync("""
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

    [Fact]
    public Task TestKeepTriviaWithinExpression()
        => TestAsync("""
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

    [Fact]
    public Task TestMultiline_IfElseIfElse()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMultiline_IfElseIfElseSelection1()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMultiline_IfElseIfElseSelection2()
        => TestAsync("""
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
    public Task TestMultiline_IfElse()
        => TestAsync("""
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

    [Fact]
    public Task TestMultiline_OpenCloseBracesSameLine()
        => TestAsync("""
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

    [Fact]
    public Task TestMultiline_Trivia()
        => TestAsync("""
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
    public Task TestOverlapsHiddenPosition6()
        => TestAsync("""
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

    [Fact]
    public Task TestOverlapsHiddenPosition7()
        => TestAsync("""
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

    [Fact]
    public Task TestSingleLine_SimplifyToLengthEqualsZero()
        => TestInsideMethodAsync(
@"string x; [||]if (x.Length > 0) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string x; if (x.Length == 0) { EqualsZero(); } else { GreaterThanZero(); } } } ");

    [Fact]
    public Task TestSingleLine_SimplifyToLengthEqualsZero2()
        => TestInsideMethodAsync(
@"string[] x; [||]if (x.Length > 0) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string[] x; if (x.Length == 0) { EqualsZero(); } else { GreaterThanZero(); } } } ");

    [Fact]
    public Task TestSingleLine_SimplifyToLengthEqualsZero3()
        => TestInsideMethodAsync(
@"string x; [||]if (x.Length > 0x0) { a(); } else { b(); } } } ",
@"string x; if (x.Length == 0x0) { b(); } else { a(); } } } ");

    [Fact]
    public Task TestSingleLine_SimplifyToLengthEqualsZero4()
        => TestInsideMethodAsync(
@"string x; [||]if (0 < x.Length) { a(); } else { b(); } } } ",
@"string x; if (0 == x.Length) { b(); } else { a(); } } } ");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
    public Task TestSingleLine_SimplifyToEqualsZero1()
        => TestInsideMethodAsync(
@"byte x = 1; [||]if (0 < x) { a(); } else { b(); } } } ",
@"byte x = 1; if (0 == x) { b(); } else { a(); } } } ");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
    public Task TestSingleLine_SimplifyToEqualsZero2()
        => TestInsideMethodAsync(
@"ushort x = 1; [||]if (0 < x) { a(); } else { b(); } } } ",
@"ushort x = 1; if (0 == x) { b(); } else { a(); } } } ");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
    public Task TestSingleLine_SimplifyToEqualsZero3()
        => TestInsideMethodAsync(
@"uint x = 1; [||]if (0 < x) { a(); } else { b(); } } } ",
@"uint x = 1; if (0 == x) { b(); } else { a(); } } } ");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
    public Task TestSingleLine_SimplifyToEqualsZero4()
        => TestInsideMethodAsync(
@"ulong x = 1; [||]if (x > 0) { a(); } else { b(); } } } ",
@"ulong x = 1; if (x == 0) { b(); } else { a(); } } } ");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
    public Task TestSingleLine_SimplifyToNotEqualsZero1()
        => TestInsideMethodAsync(
@"ulong x = 1; [||]if (0 == x) { a(); } else { b(); } } } ",
@"ulong x = 1; if (0 != x) { b(); } else { a(); } } } ");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
    public Task TestSingleLine_SimplifyToNotEqualsZero2()
        => TestInsideMethodAsync(
@"ulong x = 1; [||]if (x == 0) { a(); } else { b(); } } } ",
@"ulong x = 1; if (x != 0) { b(); } else { a(); } } } ");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530505")]
    public Task TestSingleLine_SimplifyLongLengthEqualsZero()
        => TestInsideMethodAsync(
@"string[] x; [||]if (x.LongLength > 0) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string[] x; if (x.LongLength == 0) { EqualsZero(); } else { GreaterThanZero(); } } } ");

    [Fact]
    public Task TestSingleLine_DoesNotSimplifyToLengthEqualsZero()
        => TestInsideMethodAsync(
@"string x; [||]if (x.Length >= 0) { a(); } else { b(); } } } ",
@"string x; if (x.Length < 0) { b(); } else { a(); } } } ");

    [Fact]
    public Task TestSingleLine_DoesNotSimplifyToLengthEqualsZero2()
        => TestInsideMethodAsync(
@"string x; [||]if (x.Length > 0.0f) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string x; if (x.Length <= 0.0f) { EqualsZero(); } else { GreaterThanZero(); } } } ");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29434")]
    public Task TestIsExpression()
        => TestAsync(
@"class C { void M(object o) { [||]if (o is C) { a(); } else { } } }",
@"class C { void M(object o) { if (o is not C) { } else { a(); } } }");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43224")]
    public Task TestEmptyIf()
        => TestAsync(
            @"class C { void M(string s){ [||]if (s == ""a""){}else{ s = ""b""}}}",
            @"class C { void M(string s){ if (s != ""a"") { s = ""b""}}}");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43224")]
    public Task TestOnlySingleLineCommentIf()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43224")]
    public Task TestOnlyMultilineLineCommentIf()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51359")]
    public Task TestIsCheck_CSharp6()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51359")]
    public Task TestIsCheck_CSharp8()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51359")]
    public Task TestIsCheck_CSharp9()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51359")]
    public Task TestIsNotObjectCheck_CSharp8()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51359")]
    public Task TestIsNotObjectCheck_CSharp9()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63311")]
    public Task TestLiftedNullable_GreaterThan()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63311")]
    public Task TestLiftedNullable_GreaterThanOrEqual()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63311")]
    public Task TestLiftedNullable_LessThan()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63311")]
    public Task TestLiftedNullable_LessThanOrEqual()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63311")]
    public Task TestNullableReference_GreaterThan()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40585")]
    public Task TestYieldBreak()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42715")]
    public Task PreserveSpacing()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42715")]
    public Task PreserveSpacing_WithComments()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42715")]
    public Task PreserveSpacing_NoTrivia()
        => TestAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75438")]
    public Task TestIfDirective1()
        => TestAsync("""
            [||]#if true
            #else
            #endif
            """, """
            #if false
            #else
            #endif
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75438")]
    public Task TestIfDirective2()
        => TestAsync("""
            [||]#if true
            #else

            #endif
            """, """
            #if false

            #else
            #endif
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75438")]
    public Task TestIfDirective3()
        => TestAsync("""
            [||]#if true

            #else
            #endif
            """, """
            #if false
            #else

            #endif
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75438")]
    public Task TestIfDirective4()
        => TestAsync("""
            [||]#if true
            class C
            {
            }
            #else
            record D();
            #endif
            """, """
            #if false
            record D();
            #else
            class C
            {
            }
            #endif
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75438")]
    public Task TestIfDirective5()
        => TestAsync("""
            [||]#if !true
            class C
            {
            }
            #else
            record D();
            #endif
            """, """
            #if true
            record D();
            #else
            class C
            {
            }
            #endif
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75438")]
    public Task TestIfDirective6()
        => TestAsync("""
            [||]#if NAME
            class C
            {
            }
            #else
            record D();
            #endif
            """, """
            #if !NAME
            record D();
            #else
            class C
            {
            }
            #endif
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75438")]
    public Task TestIfDirective7()
        => TestAsync("""
            [||]#if A && B
            class C
            {
            }
            #else
            record D();
            #endif
            """, """
            #if !(A && B)
            record D();
            #else
            class C
            {
            }
            #endif
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75438")]
    public Task TestIfDirective8()
        => TestAsync("""
            [||]#if (true)
            class C
            {
            }
            #else
            record D();
            #endif
            """, """
            #if (false)
            record D();
            #else
            class C
            {
            }
            #endif
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75438")]
    public Task TestIfDirective9()
        => TestAsync("""
                [||]#if (true)
                class C
                {
                }
                #else
                record D();
                #endif
            """, """
                #if (false)
                record D();
                #else
                class C
                {
                }
                #endif
            """);

    [Fact]
    public Task TestMultiLine_ConditionOnNextLine()
        => TestInsideMethodAsync(
@"[||]if (
    b) { }",
@"if (
    !b) { }");

    [Fact]
    public Task TestMultiLine_AndConditionOnNextLine()
        => TestInsideMethodAsync(
@"[||]if (a &&
    b) { }",
@"if (!a ||
    !b) { }");
}
