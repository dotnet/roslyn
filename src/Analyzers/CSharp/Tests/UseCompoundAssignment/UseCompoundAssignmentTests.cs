// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseCompoundAssignment;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseCompoundAssignmentDiagnosticAnalyzer,
    CSharpUseCompoundAssignmentCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
public sealed class UseCompoundAssignmentTests
{
    [Fact]
    public Task TestAddExpression()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a)
                {
                    a [|=|] a + 10;
                }
            }
            """, """
            public class C
            {
                void M(int a)
                {
                    a += 10;
                }
            }
            """);

    [Fact]
    public Task TestSubtractExpression()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a)
                {
                    a [|=|] a - 10;
                }
            }
            """, """
            public class C
            {
                void M(int a)
                {
                    a -= 10;
                }
            }
            """);

    [Fact]
    public Task TestMultiplyExpression()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a)
                {
                    a [|=|] a * 10;
                }
            }
            """, """
            public class C
            {
                void M(int a)
                {
                    a *= 10;
                }
            }
            """);

    [Fact]
    public Task TestDivideExpression()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a)
                {
                    a [|=|] a / 10;
                }
            }
            """, """
            public class C
            {
                void M(int a)
                {
                    a /= 10;
                }
            }
            """);

    [Fact]
    public Task TestModuloExpression()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a)
                {
                    a [|=|] a % 10;
                }
            }
            """, """
            public class C
            {
                void M(int a)
                {
                    a %= 10;
                }
            }
            """);

    [Fact]
    public Task TestBitwiseAndExpression()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a)
                {
                    a [|=|] a & 10;
                }
            }
            """, """
            public class C
            {
                void M(int a)
                {
                    a &= 10;
                }
            }
            """);

    [Fact]
    public Task TestExclusiveOrExpression()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a)
                {
                    a [|=|] a ^ 10;
                }
            }
            """, """
            public class C
            {
                void M(int a)
                {
                    a ^= 10;
                }
            }
            """);

    [Fact]
    public Task TestBitwiseOrExpression()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a)
                {
                    a [|=|] a | 10;
                }
            }
            """, """
            public class C
            {
                void M(int a)
                {
                    a |= 10;
                }
            }
            """);

    [Fact]
    public Task TestLeftShiftExpression()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a)
                {
                    a [|=|] a << 10;
                }
            }
            """, """
            public class C
            {
                void M(int a)
                {
                    a <<= 10;
                }
            }
            """);

    [Fact]
    public Task TestRightShiftExpression()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a)
                {
                    a [|=|] a >> 10;
                }
            }
            """, """
            public class C
            {
                void M(int a)
                {
                    a >>= 10;
                }
            }
            """);

    [Fact]
    public Task TestCoalesceExpressionCSharp8OrGreater()
        => new VerifyCS.Test()
        {
            TestCode = """
                public class C
                {
                    void M(int? a)
                    {
                        a [|=|] a ?? 10;
                    }
                }
                """,
            FixedCode = """
                public class C
                {
                    void M(int? a)
                    {
                        a ??= 10;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp8
        }.RunAsync();

    [Fact]
    public Task TestCoalesceExpressionCSharp7()
        => new VerifyCS.Test()
        {
            TestCode = """
            public class C
            {
                void M(int? a)
                {
                    a = a ?? 10;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7_3
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36467")]
    public Task TestNotSuggestedWhenRightHandIsThrowExpression()
        => new VerifyCS.Test()
        {
            TestCode = """
            using System;
            public class C
            {
                void M(int? a)
                {
                    a = a ?? throw new Exception();
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp8
        }.RunAsync();

    [Fact]
    public Task TestField()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                int a;

                void M()
                {
                    a [|=|] a + 10;
                }
            }
            """, """
            public class C
            {
                int a;

                void M()
                {
                    a += 10;
                }
            }
            """);

    [Fact]
    public Task TestFieldWithThis()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                int a;

                void M()
                {
                    this.a [|=|] this.a + 10;
                }
            }
            """, """
            public class C
            {
                int a;

                void M()
                {
                    this.a += 10;
                }
            }
            """);

    [Fact]
    public Task TestTriviaInsensitive()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                int a;

                void M()
                {
                    this  .  /*trivia*/ a [|=|] this /*comment*/ .a + 10;
                }
            }
            """, """
            public class C
            {
                int a;

                void M()
                {
                    this  .  /*trivia*/ a += 10;
                }
            }
            """);

    [Fact]
    public Task TestStaticFieldThroughType()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                static int a;

                void M()
                {
                    C.a [|=|] C.a + 10;
                }
            }
            """, """
            public class C
            {
                static int a;

                void M()
                {
                    C.a += 10;
                }
            }
            """);

    [Fact]
    public Task TestStaticFieldThroughNamespaceAndType()
        => VerifyCS.VerifyCodeFixAsync("""
            namespace NS
            {
                public class C
                {
                    static int a;

                    void M()
                    {
                        NS.C.a [|=|] NS.C.a + 10;
                    }
                }
            }
            """, """
            namespace NS
            {
                public class C
                {
                    static int a;

                    void M()
                    {
                        NS.C.a += 10;
                    }
                }
            }
            """);

    [Fact]
    public Task TestParenthesized()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                int a;

                void M()
                {
                    (a) [|=|] (a) + 10;
                }
            }
            """, """
            public class C
            {
                int a;

                void M()
                {
                    (a) += 10;
                }
            }
            """);

    [Fact]
    public Task TestThroughBase()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                public int a;
            }

            public class D : C
            {
                void M()
                {
                    base.a [|=|] base.a + 10;
                }
            }
            """, """
            public class C
            {
                public int a;
            }

            public class D : C
            {
                void M()
                {
                    base.a += 10;
                }
            }
            """);

    [Fact]
    public Task TestMultiAccess()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                public int a;
            }

            public class D
            {
                C c;

                void M()
                {
                    this.c.a [|=|] this.c.a + 10;
                }
            }
            """, """
            public class C
            {
                public int a;
            }

            public class D
            {
                C c;

                void M()
                {
                    this.c.a += 10;
                }
            }
            """);

    [Fact]
    public Task TestOnTopLevelProp1()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                int a { get; set; }

                void M()
                {
                    a [|=|] a + 10;
                }
            }
            """, """
            public class C
            {
                int a { get; set; }

                void M()
                {
                    a += 10;
                }
            }
            """);

    [Fact]
    public Task TestOnTopLevelProp2()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                int a { get; set; }

                void M()
                {
                    this.a [|=|] this.a + 10;
                }
            }
            """, """
            public class C
            {
                int a { get; set; }

                void M()
                {
                    this.a += 10;
                }
            }
            """);

    [Fact]
    public Task TestOnTopLevelProp3()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                int a { get; set; }

                void M()
                {
                    (this.a) [|=|] (this.a) + 10;
                }
            }
            """, """
            public class C
            {
                int a { get; set; }

                void M()
                {
                    (this.a) += 10;
                }
            }
            """);

    [Fact]
    public async Task TestNotOnTopLevelRefProp()
    {
        var code = """
            public class C
            {
                int x;
                ref int a { get { return ref x; } }

                void M()
                {
                    a = a + 10;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task TestNotOnNestedProp1()
    {
        var code = """
            public class A
            {
                public int x;
            }

            public class C
            {
                A a { get; }

                void M()
                {
                    a.x = a.x + 10;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task TestNotOnNestedProp2()
    {
        var code = """
            public class A
            {
                public int x;
            }

            public class C
            {
                A a { get; }

                void M()
                {
                    this.a.x = this.a.x + 10;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task TestNotOnNestedProp3()
    {
        var code = """
            public class A
            {
                public int x;
            }

            public class C
            {
                A a { get; }

                void M()
                {
                    (a.x) = (a.x) + 10;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task TestNotOnUnboundSymbol()
    {
        var code = """
            public class C
            {
                void M()
                {
                    {|CS0103:a|} = {|CS0103:a|} + 10;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task TestNotOnUnboundThisAccess()
    {
        var code = """
            public class C
            {
                void M()
                {
                    this.{|CS1061:a|} = this.{|CS1061:a|} + 10;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task TestNotWithSideEffects()
    {
        var code = """
            public class C
            {
                int i;

                C Goo() => this;

                void M()
                {
                    this.Goo().i = this.Goo().i + 10;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35870")]
    public Task TestRightExpressionOnNextLine()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a)
                {
                    a [|=|] a +
                        10;
                }
            }
            """, """
            public class C
            {
                void M(int a)
                {
                    a += 10;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35870")]
    public Task TestRightExpressionSeparatedWithSeveralLines()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a)
                {
                    a [|=|] a +

                        10;
                }
            }
            """, """
            public class C
            {
                void M(int a)
                {
                    a += 10;
                }
            }
            """);

    [Fact]
    public Task TestTrivia()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a)
                {
                    // before
                    a [|=|] a + 10; // after
                }
            }
            """, """
            public class C
            {
                void M(int a)
                {
                    // before
                    a += 10; // after
                }
            }
            """);

    [Fact]
    public Task TestTrivia2()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a)
                {
                    a /*mid1*/ [|=|] /*mid2*/ a + 10;
                }
            }
            """, """
            public class C
            {
                void M(int a)
                {
                    a /*mid1*/ += /*mid2*/ 10;
                }
            }
            """);

    [Fact]
    public Task TestFixAll()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a, int b)
                {
                    a [|=|] a + 10;
                    b [|=|] b - a;
                }
            }
            """, """
            public class C
            {
                void M(int a, int b)
                {
                    a += 10;
                    b -= a;
                }
            }
            """);

    [Fact]
    public Task TestNestedAssignment()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a, int b)
                {
                    b = (a [|=|] a + 10);
                }
            }
            """, """
            public class C
            {
                void M(int a, int b)
                {
                    b = (a += 10);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33382")]
    public async Task TestNotOnObjectInitializer()
    {
        var code = """
            struct InsertionPoint
            {
                int level;

                InsertionPoint Up()
                {
                    return new InsertionPoint
                    {
                        level = level - 1,
                    };
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49294")]
    public async Task TestNotOnImplicitObjectInitializer()
    {
        var code = """
            struct InsertionPoint
            {
                int level;

                InsertionPoint Up()
                {
                    return new InsertionPoint()
                    {
                        level = level - 1,
                    };
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49294")]
    public Task TestNotOnRecord()
        => new VerifyCS.Test
        {
            TestCode = """
            record InsertionPoint(int level)
            {
                InsertionPoint Up()
                {
                    return this with
                    {
                        level = level - 1,
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38137")]
    public Task TestParenthesizedExpression()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a)
                {
                    a [|=|] (a + 10);
                }
            }
            """, """
            public class C
            {
                void M(int a)
                {
                    a += 10;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38054")]
    public Task TestIncrement()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a)
                {
                    a [|=|] a + 1;
                }
            }
            """, """
            public class C
            {
                void M(int a)
                {
                    a++;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38054")]
    public Task TestDecrement()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a)
                {
                    a [|=|] a - 1;
                }
            }
            """, """
            public class C
            {
                void M(int a)
                {
                    a--;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38054")]
    public Task TestMinusIncrement()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int a)
                {
                    a [|=|] a + (-1);
                }
            }
            """, """
            public class C
            {
                void M(int a)
                {
                    a--;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38054")]
    public Task TestIncrementDouble()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(double a)
                {
                    a [|=|] a + 1.0;
                }
            }
            """, """
            public class C
            {
                void M(double a)
                {
                    a++;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38054")]
    public Task TestIncrementNotOnString()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(string a)
                {
                    a [|=|] a + "1";
                }
            }
            """, """
            public class C
            {
                void M(string a)
                {
                    a += "1";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38054")]
    public Task TestIncrementChar()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(char a)
                {
                    a [|=|] {|CS0266:a + 1|};
                }
            }
            """, """
            public class C
            {
                void M(char a)
                {
                    a++;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38054")]
    public Task TestIncrementEnum()
        => VerifyCS.VerifyCodeFixAsync("""
            public enum E {}
            public class C
            {
                void M(E a)
                {
                    a [|=|] a + 1;
                }
            }
            """, """
            public enum E {}
            public class C
            {
                void M(E a)
                {
                    a++;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38054")]
    public Task TestIncrementDecimal()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(decimal a)
                {
                    a [|=|] a + 1.0m;
                }
            }
            """, """
            public class C
            {
                void M(decimal a)
                {
                    a++;
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/38054")]
    [InlineData("byte")]
    [InlineData("short")]
    [InlineData("long")]
    [InlineData("float")]
    [InlineData("decimal")]
    public Task TestIncrementLiteralConversion(string typeName)
        => new VerifyCS.Test()
        {
            TestCode = $$"""
                public class C
                {
                    void M({{typeName}} a)
                    {
                        a [|=|] (a + ({{typeName}})1);
                    }
                }
                """,
            FixedCode = $$"""
                public class C
                {
                    void M({{typeName}} a)
                    {
                        a++;
                    }
                }
                """,
            CompilerDiagnostics = CompilerDiagnostics.None
        }.RunAsync();

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/38054")]
    [InlineData("byte")]
    [InlineData("short")]
    [InlineData("long")]
    [InlineData("float")]
    [InlineData("decimal")]
    public Task TestIncrementImplicitLiteralConversion(string typeName)
        => new VerifyCS.Test()
        {
            TestCode = $$"""
                public class C
                {
                    void M({{typeName}} a)
                    {
                        a [|=|] a + 1;
                    }
                }
                """,
            FixedCode = $$"""
                public class C
                {
                    void M({{typeName}} a)
                    {
                        a++;
                    }
                }
                """,
            CompilerDiagnostics = CompilerDiagnostics.None
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38054")]
    public Task TestIncrementLoopVariable()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M()
                {
                    for (int i = 0; i < 10; i [|=|] i + 1)
                    {
                    }
                }
            }
            """, """
            public class C
            {
                void M()
                {
                    for (int i = 0; i < 10; i++)
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53969")]
    public Task TestIncrementInExpressionContext()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                void M(int i)
                {
                    M(i [|=|] i + 1);
                }
            }
            """, """
            public class C
            {
                void M(int i)
                {
                    M(++i);
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/53969")]
    [InlineData("switch($$) { }")]
    [InlineData("while(($$) > 0) { }")]
    [InlineData("_ = true ? $$ : 0;")]
    [InlineData("_ = ($$);")]
    public async Task TestPrefixIncrement1(string expressionContext)
    {
        var before = expressionContext.Replace("$$", "i [|=|] i + 1");
        var after = expressionContext.Replace("$$", "++i");
        await VerifyCS.VerifyCodeFixAsync($$"""
            public class C
            {
                void M(int i)
                {
                    {{before}}
                }
            }
            """, $$"""
            public class C
            {
                void M(int i)
                {
                    {{after}}
                }
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/53969")]
    [InlineData("return $$;")]
    [InlineData("return true ? $$ : 0;")]
    [InlineData("return ($$);")]
    public async Task TestPrefixIncrement2(string expressionContext)
    {
        var before = expressionContext.Replace("$$", "i [|=|] i + 1");
        var after = expressionContext.Replace("$$", "++i");
        await VerifyCS.VerifyCodeFixAsync($$"""
            public class C
            {
                int M(int i)
                {
                    {{before}}
                }
            }
            """, $$"""
            public class C
            {
                int M(int i)
                {
                    {{after}}
                }
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/53969")]
    [InlineData(
        "/* Before */ i [|=|] i + 1; /* After */",
        "/* Before */ i++; /* After */")]
    [InlineData(
        "M( /* Before */ i [|=|] i + 1 /* After */ );",
        "M( /* Before */ ++i /* After */ );")]
    [InlineData(
        "M( /* Before */ i [|=|] i - 1 /* After */ );",
        "M( /* Before */ --i /* After */ );")]
    public Task TestTriviaPreserved(string before, string after)
        => VerifyCS.VerifyCodeFixAsync($$"""
            public class C
            {
                void M(int i)
                {
                    {{before}}
                }
            }
            """, $$"""
            public class C
            {
                void M(int i)
                {
                    {{after}}
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70651")]
    public Task TestIncrementWithUserDefinedOperators_IncrementOperatorNotDefined()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                int data;

                public C(int data)
                {
                    this.data = data;
                }

                public static C operator +(C left, int right)
                {
                    return new C(left.data + right);
                }

                void M()
                {
                    var c = new C(0);
                    c [|=|] c + 1;
                }
            }
            """, """
            class C
            {
                int data;
            
                public C(int data)
                {
                    this.data = data;
                }
            
                public static C operator +(C left, int right)
                {
                    return new C(left.data + right);
                }
            
                void M()
                {
                    var c = new C(0);
                    c += 1;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70651")]
    public Task TestIncrementWithUserDefinedOperators_IncrementOperatorDefined()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                int data;

                public C(int data)
                {
                    this.data = data;
                }

                public static C operator +(C left, int right)
                {
                    return new C(left.data + right);
                }

                public static C operator ++(C operand)
                {
                    return new C(operand.data + 1);
                }

                void M()
                {
                    var c = new C(0);
                    c [|=|] c + 1;
                }
            }
            """, """
            class C
            {
                int data;
            
                public C(int data)
                {
                    this.data = data;
                }
            
                public static C operator +(C left, int right)
                {
                    return new C(left.data + right);
                }

                public static C operator ++(C operand)
                {
                    return new C(operand.data + 1);
                }

                void M()
                {
                    var c = new C(0);
                    c++;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70651")]
    public Task TestDecrementWithUserDefinedOperators_DecrementOperatorNotDefined()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                int data;

                public C(int data)
                {
                    this.data = data;
                }

                public static C operator -(C left, int right)
                {
                    return new C(left.data - right);
                }

                void M()
                {
                    var c = new C(0);
                    c [|=|] c - 1;
                }
            }
            """, """
            class C
            {
                int data;
            
                public C(int data)
                {
                    this.data = data;
                }
            
                public static C operator -(C left, int right)
                {
                    return new C(left.data - right);
                }
            
                void M()
                {
                    var c = new C(0);
                    c -= 1;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70651")]
    public Task TestDecrementWithUserDefinedOperators_DecrementOperatorDefined()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                int data;

                public C(int data)
                {
                    this.data = data;
                }

                public static C operator -(C left, int right)
                {
                    return new C(left.data - right);
                }

                public static C operator --(C operand)
                {
                    return new C(operand.data - 1);
                }

                void M()
                {
                    var c = new C(0);
                    c [|=|] c - 1;
                }
            }
            """, """
            class C
            {
                int data;
            
                public C(int data)
                {
                    this.data = data;
                }
            
                public static C operator -(C left, int right)
                {
                    return new C(left.data - right);
                }

                public static C operator --(C operand)
                {
                    return new C(operand.data - 1);
                }

                void M()
                {
                    var c = new C(0);
                    c--;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76633")]
    public Task TestFieldKeyword1()
        => new VerifyCS.Test
        {
            TestCode = """
                public class C
                {
                    int M
                    {
                        get
                        {
                            field [|=|] field + 10;
                            return 0;
                        }
                    }
                }
                """,
            FixedCode = """
                public class C
                {
                    int M
                    {
                        get
                        {
                            field += 10;
                            return 0;
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();
}
