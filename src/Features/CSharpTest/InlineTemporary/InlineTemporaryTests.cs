// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineTemporary;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.InlineTemporary;

[Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
public sealed class InlineTemporaryTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpInlineTemporaryCodeRefactoringProvider();

    private async Task TestFixOneAsync(string initial, string expected)
        => await TestInRegularAndScript1Async(GetTreeText(initial), GetTreeText(expected));

    private static string GetTreeText(string initial)
    {
        return """
            class C
            {
                void F()
            """ + initial + """
            }
            """;
    }

    private static SyntaxNode GetNodeToFix(dynamic initialRoot, int declaratorIndex)
        => initialRoot.Members[0].Members[0].Body.Statements[0].Declaration.Variables[declaratorIndex];

    private static SyntaxNode GetFixedNode(dynamic fixedRoot)
        => fixedRoot.Members[0].Members[0].BodyOpt;

    [Fact]
    public async Task NotWithNoInitializer1()
        => await TestMissingInRegularAndScriptAsync(GetTreeText(@"{ int [||]x; System.Console.WriteLine(x); }"));

    [Fact]
    public async Task NotWithNoInitializer2()
        => await TestMissingInRegularAndScriptAsync(GetTreeText(@"{ int [||]x = ; System.Console.WriteLine(x); }"));

    [Fact]
    public async Task NotOnSecondWithNoInitializer()
        => await TestMissingInRegularAndScriptAsync(GetTreeText(@"{ int x = 42, [||]y; System.Console.WriteLine(y); }"));

    [Fact]
    public async Task NotOnField()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int [||]x = 42;

                void M()
                {
                    System.Console.WriteLine(x);
                }
            }
            """);
    }

    [Fact]
    public async Task WithRefInitializer1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                ref int M()
                {
                    int[] arr = new[] { 1, 2, 3 };
                    ref int [||]x = ref arr[2];
                    return ref x;
                }
            }
            """);
    }

    [Fact]
    public async Task SingleStatement()
        => await TestMissingInRegularAndScriptAsync(GetTreeText(@"{ int [||]x = 27; }"));

    [Fact]
    public async Task MultipleDeclarators_First()
        => await TestMissingInRegularAndScriptAsync(GetTreeText(@"{ int [||]x = 0, y = 1, z = 2; }"));

    [Fact]
    public async Task MultipleDeclarators_Second()
        => await TestMissingInRegularAndScriptAsync(GetTreeText(@"{ int x = 0, [||]y = 1, z = 2; }"));

    [Fact]
    public async Task MultipleDeclarators_Last()
        => await TestMissingInRegularAndScriptAsync(GetTreeText(@"{ int x = 0, y = 1, [||]z = 2; }"));

    [Fact]
    public async Task Escaping1()
    {
        await TestFixOneAsync(
            """
            { int [||]x = 0;

            Console.WriteLine(x); }
            """,
            """
            {
                    Console.WriteLine(0); }
            """);
    }

    [Fact]
    public async Task Escaping2()
    {
        await TestFixOneAsync(
            """
            { int [||]@x = 0;

            Console.WriteLine(x); }
            """,
            """
            {
                    Console.WriteLine(0); }
            """);
    }

    [Fact]
    public async Task Escaping3()
    {
        await TestFixOneAsync(
            """
            { int [||]@x = 0;

            Console.WriteLine(@x); }
            """,
            """
            {
                    Console.WriteLine(0); }
            """);
    }

    [Fact]
    public async Task Escaping4()
    {
        await TestFixOneAsync(
            """
            { int [||]x = 0;

            Console.WriteLine(@x); }
            """,
            """
            {
                    Console.WriteLine(0); }
            """);
    }

    [Fact]
    public async Task Escaping5()
    {
        var code = """
            using System.Linq;
            class C
            {
                static void Main()
                {
                    var @where[||] = 0;
                    var q = from e in "" let a = new { @where } select a;
                }
            }
            """;

        var expected = """
            using System.Linq;
            class C
            {
                static void Main()
                {
                    var q = from e in "" let a = new { @where = 0 } select a;
                }
            }
            """;

        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact]
    public async Task Call()
    {
        var code = """
            using System;
            class C 
            {
                public void M()
                {
                    int [||]x = 1 + 1;
                    x.ToString();
                }
            }
            """;

        var expected = """
            using System;
            class C 
            {
                public void M()
                {
                    (1 + 1).ToString();
                }
            }
            """;

        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact]
    public async Task Conversion_NoChange()
    {
        var code = """
            using System;
            class C 
            {
                public void M()
                {
                    double [||]x = 3;
                    x.ToString();
                }
            }
            """;

        var expected = """
            using System;
            class C 
            {
                public void M()
                {
                    ((double)3).ToString();
                }
            }
            """;

        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact]
    public async Task Conversion_NoConversion()
    {
        await TestAsync(
            """
            class C
            {
                void F(){ int [||]x = 3;

            x.ToString(); }
            }
            """,
            """
            class C
            {
                void F(){
                    3.ToString(); }
            }
            """,
            CSharpParseOptions.Default);
    }

    [Fact]
    public async Task Conversion_DifferentOverload()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    double [||]x = 3;
                    Console.WriteLine(x);
                }
            }
            """,

            """
            using System;
            class C
            {
                void M()
                {
                    Console.WriteLine((double)3);
                }
            }
            """);
    }

    [Fact]
    public async Task Conversion_DifferentMethod()
    {
        await TestInRegularAndScriptAsync(
            """
            class Base 
            {
                public void M(object o) { }
            }
            class Derived : Base
            {
                public void M(string s) { }
            }
            class C
            {
                void F()
                {
                    Base [||]b = new Derived();
                    b.M("hi");
                }
            }
            """,

            """
            class Base 
            {
                public void M(object o) { }
            }
            class Derived : Base
            {
                public void M(string s) { }
            }
            class C
            {
                void F()
                {
                    ((Base)new Derived()).M("hi");
                }
            }
            """);
    }

    [Fact]
    public async Task Conversion_SameMethod()
    {
        await TestInRegularAndScriptAsync(
            """
            class Base 
            {
                public void M(int i) { }
            }
            class Derived : Base
            {
                public void M(string s) { }
            }
            class C
            {
                void F()
                {
                    Base [||]b = new Derived();
                    b.M(3);
                }
            }
            """,

            """
            class Base 
            {
                public void M(int i) { }
            }
            class Derived : Base
            {
                public void M(string s) { }
            }
            class C
            {
                void F()
                {
                    new Derived().M(3);
                }
            }
            """);
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp8)]
    [InlineData(LanguageVersion.CSharp9)]
    public async Task Conversion_NonTargetTypedConditionalExpression(LanguageVersion languageVersion)
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void F()
                {
                    int? [||]x = 42;
                    var y = true ? x : null;
                }
            }
            """,

            """
            class C
            {
                void F()
                {
                    var y = true ? (int?)42 : null;
                }
            }
            """, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(languageVersion));
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp8, " (int?)42")]
    [InlineData(LanguageVersion.CSharp9, " 42")] // In C# 9, target-typed conditionals makes this work
    public async Task Conversion_TargetTypedConditionalExpression(LanguageVersion languageVersion, string expectedSubstitution)
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void F()
                {
                    int? [||]x = 42;
                    int? y = true ? x : null;
                }
            }
            """,

            """
            class C
            {
                void F()
                {
                    int? y = true ?
            """ + expectedSubstitution + """
             : null;
                }
            }
            """, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(languageVersion));
    }

    [Fact]
    public async Task NoCastOnVar()
    {
        await TestFixOneAsync(
            """
            { var [||]x = 0;

            Console.WriteLine(x); }
            """,
            """
            {
                    Console.WriteLine(0); }
            """);
    }

    [Fact]
    public async Task DoubleAssignment()
    {
        var code = """
            class C
            {
                void M()
                {
                    int [||]x = x = 1;
                    int y = x;
                }
            }
            """;

        await TestMissingAsync(code);
    }

    [Fact]
    public async Task TestAnonymousType1()
    {
        await TestFixOneAsync(
            """
            { int [||]x = 42;
            var a = new { x }; }
            """,
                   @"{ var a = new { x = 42 }; }");
    }

    [Fact]
    public async Task TestParenthesizedAtReference_Case3()
    {
        await TestFixOneAsync(
            """
            { int [||]x = 1 + 1;
            int y = x * 2; }
            """,
                   @"{ int y = (1 + 1) * 2; }");
    }

    [Fact]
    public async Task DoNotBreakOverloadResolution_Case5()
    {
        var code = """
            class C
            {
                void Goo(object o) { }
                void Goo(int i) { }

                void M()
                {
                    object [||]x = 1 + 1;
                    Goo(x);
                }
            }
            """;

        var expected = """
            class C
            {
                void Goo(object o) { }
                void Goo(int i) { }

                void M()
                {
                    Goo((object)(1 + 1));
                }
            }
            """;

        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact]
    public async Task DoNotTouchUnrelatedBlocks()
    {
        var code = """
            class C
            {
                void M()
                {
                    int [||]x = 1;
                    { Unrelated(); }
                    Goo(x);
                }
            }
            """;

        var expected = """
            class C
            {
                void M()
                {
                    { Unrelated(); }
                    Goo(1);
                }
            }
            """;

        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact]
    public async Task TestLambdaParenthesizeAndCast_Case7()
    {
        var code = """
            class C
            {
                void M()
                {
                    System.Func<int> [||]x = () => 1;
                    int y = x();
                }
            }
            """;

        var expected = """
            class C
            {
                void M()
                {
                    int y = ((System.Func<int>)(() => 1))();
                }
            }
            """;

        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538094")]
    public async Task ParseAmbiguity1()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void F(object a, object b)
                {
                    int x = 2;
                    bool [||]y = x > (f);
                    F(x < x, y);
                }
                int f = 0;
            }
            """,
            """
            class C
            {
                void F(object a, object b)
                {
                    int x = 2;
                    F(x < x, (x > (f)));
                }
                int f = 0;
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538094"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541462")]
    public async Task ParseAmbiguity2()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void F(object a, object b)
                {
                    int x = 2;
                    object [||]y = x > (f);
                    F(x < x, y);
                }
                int f = 0;
            }
            """,
            """
            class C
            {
                void F(object a, object b)
                {
                    int x = 2;
                    F(x < x, (x > (f)));
                }
                int f = 0;
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538094")]
    public async Task ParseAmbiguity3()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void F(object a, object b)
                {
                    int x = 2;
                    bool [||]y = x > (int)1;
                    F(x < x, y);
                }
                int f = 0;
            }
            """,
            """
            class C
            {
                void F(object a, object b)
                {
                    int x = 2;
                    F(x < x, (x > (int)1));
                }
                int f = 0;
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544924")]
    public async Task ParseAmbiguity4()
    {
        await TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    int x = 2;
                    int y[||] = (1+2);
                    Bar(x < x, x > y);
                }

                static void Bar(object a, object b)
                {
                }
            }
            """,
            """
            class Program
            {
                static void Main()
                {
                    int x = 2;
                    Bar(x < x, x > 1 + 2);
                }

                static void Bar(object a, object b)
                {
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544613")]
    public async Task ParseAmbiguity5()
    {
        await TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    int x = 2;
                    int y[||] = (1 + 2);
                    var z = new[] { x < x, x > y };
                }
            }
            """,
            """
            class Program
            {
                static void Main()
                {
                    int x = 2;
                    var z = new[] { x < x, x > 1 + 2 };
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538131")]
    public async Task TestArrayInitializer()
    {
        await TestFixOneAsync(
            """
            { int[] [||]x = {
                3,
                4,
                5
            };
            int a = Array.IndexOf(x, 3); }
            """,
            """
            {
                    int a = Array.IndexOf(new int[] {
                    3,
                    4,
                    5
                }, 3); }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545657")]
    public async Task TestArrayInitializer2()
    {
        var initial = """
            class Program
            { 
                static void Main()
                {
                    int[] [||]x = { 3, 4, 5 };
                    System.Array a = x;
                }
            }
            """;

        var expected = """
            class Program
            { 
                static void Main()
                {
                    System.Array a = new int[] { 3, 4, 5 };
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545657")]
    public async Task TestArrayInitializer3()
    {
        var initial = """
            class Program
            { 
                static void Main()
                {
                    int[] [||]x = {
                                      3,
                                      4,
                                      5
                                  };
                    System.Array a = x;
                }
            }
            """;

        var expected = """
            class Program
            { 
                static void Main()
                {
                    System.Array a = new int[] {
                        3,
                        4,
                        5
                    };
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestConflict_RefParameter1()
    {
        var initial =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int [||]x = 0;
                    Goo(ref x);
                    Goo(x);
                }

                void Goo(int x)
                {
                }

                void Goo(ref int x)
                {
                }
            }
            """;

        var expected =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int x = 0;
                    Goo(ref {|Conflict:x|});
                    Goo(0);
                }

                void Goo(int x)
                {
                }

                void Goo(ref int x)
                {
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestConflict_RefParameter2()
    {
        var initial =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int [||]x = 0;
                    Goo(x, ref x);
                }

                void Goo(int x, ref int y)
                {
                }
            }
            """;

        var expected =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int x = 0;
                    Goo(0, ref {|Conflict:x|});
                }

                void Goo(int x, ref int y)
                {
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestConflict_AssignExpression()
    {
        var initial =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int [||]i = 1;
                    i = 2;
                    Console.WriteLine(i);
                }
            }
            """;

        var expected =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int i = 1;
                    {|Conflict:i|} = 2;
                    Console.WriteLine(1);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestConflict_AddAssignExpression1()
    {
        var initial =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int [||]i = 1;
                    i += 2;
                    Console.WriteLine(i);
                }
            }
            """;

        var expected =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int i = 1;
                    {|Conflict:i|} += 2;
                    Console.WriteLine(1);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestConflict_AddAssignExpression2()
    {
        var initial =
            """
            using System;
            class C
            {
                static int x;

                static void M()
                {
                    int [||]x = (x = 0) + (x += 1);
                    int y = x;
                }
            }
            """;

        await TestMissingAsync(initial);
    }

    [Fact]
    public async Task TestConflict_SubtractAssignExpression()
    {
        var initial =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int [||]i = 1;
                    i -= 2;
                    Console.WriteLine(i);
                }
            }
            """;

        var expected =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int i = 1;
                    {|Conflict:i|} -= 2;
                    Console.WriteLine(1);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestConflict_MultiplyAssignExpression()
    {
        var initial =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int [||]i = 1;
                    i *= 2;
                    Console.WriteLine(i);
                }
            }
            """;

        var expected =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int i = 1;
                    {|Conflict:i|} *= 2;
                    Console.WriteLine(1);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestConflict_DivideAssignExpression()
    {
        var initial =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int [||]i = 1;
                    i /= 2;
                    Console.WriteLine(i);
                }
            }
            """;

        var expected =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int i = 1;
                    {|Conflict:i|} /= 2;
                    Console.WriteLine(1);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestConflict_ModuloAssignExpression()
    {
        var initial =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int [||]i = 1;
                    i %= 2;
                    Console.WriteLine(i);
                }
            }
            """;

        var expected =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int i = 1;
                    {|Conflict:i|} %= 2;
                    Console.WriteLine(1);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestConflict_AndAssignExpression()
    {
        var initial =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int [||]i = 1;
                    i &= 2;
                    Console.WriteLine(i);
                }
            }
            """;

        var expected =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int i = 1;
                    {|Conflict:i|} &= 2;
                    Console.WriteLine(1);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestConflict_OrAssignExpression()
    {
        var initial =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int [||]i = 1;
                    i |= 2;
                    Console.WriteLine(i);
                }
            }
            """;

        var expected =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int i = 1;
                    {|Conflict:i|} |= 2;
                    Console.WriteLine(1);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestConflict_ExclusiveOrAssignExpression()
    {
        var initial =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int [||]i = 1;
                    i ^= 2;
                    Console.WriteLine(i);
                }
            }
            """;

        var expected =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int i = 1;
                    {|Conflict:i|} ^= 2;
                    Console.WriteLine(1);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestConflict_LeftShiftAssignExpression()
    {
        var initial =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int [||]i = 1;
                    i <<= 2;
                    Console.WriteLine(i);
                }
            }
            """;

        var expected =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int i = 1;
                    {|Conflict:i|} <<= 2;
                    Console.WriteLine(1);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestConflict_RightShiftAssignExpression()
    {
        var initial =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int [||]i = 1;
                    i >>= 2;
                    Console.WriteLine(i);
                }
            }
            """;

        var expected =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int i = 1;
                    {|Conflict:i|} >>= 2;
                    Console.WriteLine(1);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestConflict_PostIncrementExpression()
    {
        var initial =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int [||]i = 1;
                    i++;
                    Console.WriteLine(i);
                }
            }
            """;

        var expected =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int i = 1;
                    {|Conflict:i|}++;
                    Console.WriteLine(1);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestConflict_PreIncrementExpression()
    {
        var initial =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int [||]i = 1;
                    ++i;
                    Console.WriteLine(i);
                }
            }
            """;

        var expected =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int i = 1;
                    ++{|Conflict:i|};
                    Console.WriteLine(1);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestConflict_PostDecrementExpression()
    {
        var initial =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int [||]i = 1;
                    i--;
                    Console.WriteLine(i);
                }
            }
            """;

        var expected =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int i = 1;
                    {|Conflict:i|}--;
                    Console.WriteLine(1);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestConflict_PreDecrementExpression()
    {
        var initial =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int [||]i = 1;
                    --i;
                    Console.WriteLine(i);
                }
            }
            """;

        var expected =
            """
            using System;
            class Program
            {
                void Main()
                {
                    int i = 1;
                    --{|Conflict:i|};
                    Console.WriteLine(1);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestConflict_AddressOfExpression()
    {
        var initial = """
            class C
            {
                unsafe void M()
                {
                    int x = 0;
                    var y[||] = &x;
                    var z = &y;
                }
            }
            """;

        var expected = """
            class C
            {
                unsafe void M()
                {
                    int x = 0;
                    var y = &x;
                    var z = &{|Conflict:y|};
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545342")]
    public async Task TestConflict_UsedBeforeDeclaration()
    {
        var initial =
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var x = y;
                    var y[||] = 45;
                }
            }
            """;

        var expected =
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var x = {|Conflict:y|};
                    var y = 45;
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task Preprocessor1()
    {
        await TestFixOneAsync("""
            {
                int [||]x = 1,
            #if true
                    y,
            #endif
                    z;

                int a = x;
            }
            """,
            """
            {
                    int
            #if true
                        y,
            #endif
                        z;

                    int a = 1;
            }
            """);
    }

    [Fact]
    public async Task Preprocessor2()
    {
        await TestFixOneAsync("""
            {
                int y,
            #if true
                    [||]x = 1,
            #endif
                    z;

                int a = x;
            }
            """,
            """
            {
                    int y,
            #if true

            #endif
                        z;

                    int a = 1;
            }
            """);
    }

    [Fact]
    public async Task Preprocessor3()
    {
        await TestFixOneAsync("""
            {
                int y,
            #if true
                    z,
            #endif
                    [||]x = 1;

                int a = x;
            }
            """,
            """
            {
                    int y,
            #if true
                        z
            #endif
                        ;

                    int a = 1;
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540164")]
    public async Task TriviaOnArrayInitializer()
    {
        var initial =
            """
            class C
            {
                void M()
                {
                    int[] [||]a = /**/{ 1 };
                    Goo(a);
                }
            }
            """;

        var expected =
            """
            class C
            {
                void M()
                {
                    Goo(new int[]/**/{ 1 });
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540156")]
    public async Task ProperlyFormatWhenRemovingDeclarator1()
    {
        var initial =
            """
            class C
            {
                void M()
                {
                    int [||]i = 1, j = 2, k = 3;
                    System.Console.Write(i);
                }
            }
            """;

        var expected =
            """
            class C
            {
                void M()
                {
                    int j = 2, k = 3;
                    System.Console.Write(1);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540156")]
    public async Task ProperlyFormatWhenRemovingDeclarator2()
    {
        var initial =
            """
            class C
            {
                void M()
                {
                    int i = 1, [||]j = 2, k = 3;
                    System.Console.Write(j);
                }
            }
            """;

        var expected =
            """
            class C
            {
                void M()
                {
                    int i = 1, k = 3;
                    System.Console.Write(2);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540156")]
    public async Task ProperlyFormatWhenRemovingDeclarator3()
    {
        var initial =
            """
            class C
            {
                void M()
                {
                    int i = 1, j = 2, [||]k = 3;
                    System.Console.Write(k);
                }
            }
            """;

        var expected =
            """
            class C
            {
                void M()
                {
                    int i = 1, j = 2;
                    System.Console.Write(3);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540186")]
    public async Task ProperlyFormatAnonymousTypeMember()
    {
        var initial =
            """
            class C
            {
                void M()
                {
                    var [||]x = 123;
                    var y = new { x };
                }
            }
            """;

        var expected =
            """
            class C
            {
                void M()
                {
                    var y = new { x = 123 };
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem(6356, "DevDiv_Projects/Roslyn")]
    public async Task InlineToAnonymousTypeProperty()
    {
        var initial =
            """
            class C
            {
                void M()
                {
                    var [||]x = 123;
                    var y = new { x = x };
                }
            }
            """;

        var expected =
            """
            class C
            {
                void M()
                {
                    var y = new { x = 123 };
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528075")]
    public async Task InlineIntoDelegateInvocation()
    {
        var initial =
            """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Action<string[]> [||]del = Main;
                    del(null);
                }
            }
            """;

        var expected =
            """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    ((Action<string[]>)Main)(null);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541341")]
    public async Task InlineAnonymousMethodIntoNullCoalescingExpression()
    {
        var initial =
            """
            using System;

            class Program
            {
                static void Main()
                {
                    Action [||]x = delegate { };
                    Action y = x ?? null;
                }
            }
            """;

        var expected =
            """
            using System;

            class Program
            {
                static void Main()
                {
                    Action y = (Action)delegate { } ?? null;
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541341")]
    public async Task InlineLambdaIntoNullCoalescingExpression()
    {
        var initial =
            """
            using System;

            class Program
            {
                static void Main()
                {
                    Action [||]x = () => { };
                    Action y = x ?? null;
                }
            }
            """;

        var expected =
            """
            using System;

            class Program
            {
                static void Main()
                {
                    Action y = (Action)(() => { }) ?? null;
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538079")]
    public async Task InsertCastForBoxingOperation1()
    {
        var initial =
            """
            using System;
            class A
            {
                static void Main()
                {
                    long x[||] = 1;
                    object z = x;
                    Console.WriteLine((long)z);
                }
            }
            """;

        var expected =
            """
            using System;
            class A
            {
                static void Main()
                {
                    object z = (long)1;
                    Console.WriteLine((long)z);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538079")]
    public async Task InsertCastForBoxingOperation2()
    {
        var initial =
            """
            using System;
            class A
            {
                static void Main()
                {
                    int y = 1;
                    long x[||] = y;
                    object z = x;
                    Console.WriteLine((long)z);
                }
            }
            """;

        var expected =
            """
            using System;
            class A
            {
                static void Main()
                {
                    int y = 1;
                    object z = (long)y;
                    Console.WriteLine((long)z);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538079")]
    public async Task InsertCastForBoxingOperation3()
    {
        var initial =
            """
            using System;
            class A
            {
                static void Main()
                {
                    byte x[||] = 1;
                    object z = x;
                    Console.WriteLine((byte)z);
                }
            }
            """;

        var expected =
            """
            using System;
            class A
            {
                static void Main()
                {
                    object z = (byte)1;
                    Console.WriteLine((byte)z);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538079")]
    public async Task InsertCastForBoxingOperation4()
    {
        var initial =
            """
            using System;
            class A
            {
                static void Main()
                {
                    sbyte x[||] = 1;
                    object z = x;
                    Console.WriteLine((sbyte)z);
                }
            }
            """;

        var expected =
            """
            using System;
            class A
            {
                static void Main()
                {
                    object z = (sbyte)1;
                    Console.WriteLine((sbyte)z);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538079")]
    public async Task InsertCastForBoxingOperation5()
    {
        var initial =
            """
            using System;
            class A
            {
                static void Main()
                {
                    short x[||] = 1;
                    object z = x;
                    Console.WriteLine((short)z);
                }
            }
            """;

        var expected =
            """
            using System;
            class A
            {
                static void Main()
                {
                    object z = (short)1;
                    Console.WriteLine((short)z);
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540278")]
    public async Task TestLeadingTrivia()
    {
        await TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    // Leading
                    int [||]i = 10;
                    //print
                    Console.Write(i);
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    // Leading
                    //print
                    Console.Write(10);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540278")]
    public async Task TestLeadingAndTrailingTrivia()
    {
        await TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    // Leading
                    int [||]i = 10; // Trailing
                    //print
                    Console.Write(i);
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    // Leading
                    // Trailing
                    //print
                    Console.Write(10);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540278")]
    public async Task TestTrailingTrivia()
    {
        await TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    int [||]i = 10; // Trailing
                    //print
                    Console.Write(i);
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    // Trailing
                    //print
                    Console.Write(10);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540278")]
    public async Task TestPreprocessor()
    {
        await TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
            #if true
                    int [||]i = 10; 
                    //print
                    Console.Write(i);
            #endif
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
            #if true
                    //print
                    Console.Write(10);
            #endif
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540277")]
    public async Task TestFormatting()
    {
        await TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    int [||]i = 5; int j = 110;
                    Console.Write(i + j);
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    int j = 110;
                    Console.Write(5 + j);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541694")]
    public async Task TestSwitchSection()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    switch (10)
                    {
                        default:
                            int i[||] = 10;
                            Console.WriteLine(i);
                            break;
                    }
                }
            }
            """,
            """
            using System;
            class C
            {
                void M()
                {
                    switch (10)
                    {
                        default:
                            Console.WriteLine(10);
                            break;
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542647")]
    public async Task UnparenthesizeExpressionIfNeeded1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                static Action X;
                static void M()
                {
                    var [||]y = (X);
                    y();
                }
            }
            """,

            """
            using System;
            class C
            {
                static Action X;
                static void M()
                {
                    X();
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545619")]
    public async Task UnparenthesizeExpressionIfNeeded2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            class Program
            {
                static void Main()
                {
                    Action x = Console.WriteLine;
                    Action y[||] = x;
                    y();
                }
            }
            """,

            """
            using System;
            class Program
            {
                static void Main()
                {
                    Action x = Console.WriteLine;
                    x();
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542656")]
    public async Task ParenthesizeIfNecessary1()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;
        using System.Collections;
        using System.Linq;

        class A
        {
            static void Main()
            {
                var [||]q = from x in "" select x;
                if (q is IEnumerable)
                {
                }
            }
        }
        """,
        """
        using System;
        using System.Collections;
        using System.Linq;

        class A
        {
            static void Main()
            {
                if ((from x in "" select x) is IEnumerable)
                {
                }
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544626")]
    public async Task ParenthesizeIfNecessary2()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;
        class C
        {
            static void Main()
            {
                Action<string> f[||] = Goo<string>;
                Action<string> g = null;
                var h = f + g;
            }

            static void Goo<T>(T y) { }
        }
        """,
        """
        using System;
        class C
        {
            static void Main()
            {
                Action<string> g = null;
                var h = (Goo<string>) + g;
            }

            static void Goo<T>(T y) { }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544415")]
    public async Task ParenthesizeAddressOf1()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;
        unsafe class C
        {
            static void M()
            {
                int x;
                int* p[||] = &x;
                var i = (Int32)p;
            }
        }
        """,
        """
        using System;
        unsafe class C
        {
            static void M()
            {
                int x;
                var i = (Int32)(&x);
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544922")]
    public async Task ParenthesizeAddressOf2()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;
        unsafe class C
        {
            static void M()
            {
                int x;
                int* p[||] = &x;
                var i = p->ToString();
            }
        }
        """,
        """
        using System;
        unsafe class C
        {
            static void M()
            {
                int x;
                var i = (&x)->ToString();
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544921")]
    public async Task ParenthesizePointerIndirection1()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;
        unsafe class C
        {
            static void M()
            {
                int* x = null;
                int p[||] = *x;
                var i = (Int64)p;
            }
        }
        """,
        """
        using System;
        unsafe class C
        {
            static void M()
            {
                int* x = null;
                var i = (Int64)(*x);
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544614")]
    public async Task ParenthesizePointerIndirection2()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;
        unsafe class C
        {
            static void M()
            {
                int** x = null;
                int* p[||] = *x;
                var i = p[1].ToString();
            }
        }
        """,
        """
        using System;
        unsafe class C
        {
            static void M()
            {
                int** x = null;
                var i = (*x)[1].ToString();
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544563")]
    public async Task DoNotInlineStackAlloc()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            unsafe class C
            {
                static void M()
                {
                    int* values[||] = stackalloc int[20];
                    int* copy = values;
                    int* p = &values[1];
                    int* q = &values[15];
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543744")]
    public async Task InlineTempLambdaExpressionCastingError()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;
        class Program
        {
            static void Main(string[] args)
            {
                Func<int?,int?> [||]lam = (int? s) => { return s; };
                Console.WriteLine(lam);
            }
        }
        """,

        """
        using System;
        class Program
        {
            static void Main(string[] args)
            {
                Console.WriteLine((int? s) => { return s; });
            }
        }
        """);
    }

    [Fact]
    public async Task InsertCastForNull()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;
        class C
        {
            void M()
            {
                string [||]x = null;
                Console.WriteLine(x);
            }
        }
        """,

        """
        using System;
        class C
        {
            void M()
            {
                Console.WriteLine((string)null);
            }
        }
        """);
    }

    [Fact]
    public async Task InsertCastIfNeeded1()
    {
        await TestInRegularAndScriptAsync(
        """
        class C
        {
            void M()
            {
                long x[||] = 1;
                System.IComparable<long> y = x;
            }
        }
        """,
        """
        class C
        {
            void M()
            {
                System.IComparable<long> y = (long)1;
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545161")]
    public async Task InsertCastIfNeeded2()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;

        class C
        {
            static void Main()
            {
                Goo(x => { int [||]y = x[0]; x[1] = y; });
            }

            static void Goo(Action<int[]> x) { }
            static void Goo(Action<string[]> x) { }
        }
        """,
        """
        using System;

        class C
        {
            static void Main()
            {
                Goo(x => { x[1] = (int)x[0]; });
            }

            static void Goo(Action<int[]> x) { }
            static void Goo(Action<string[]> x) { }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544612")]
    public async Task InlineIntoBracketedList()
    {
        await TestInRegularAndScriptAsync(
        """
        class C
        {
            void M()
            {
                var c = new C();
                int x[||] = 1;
                c[x] = 2;
            }

            int this[object x] { set { } }
        }
        """,

        """
        class C
        {
            void M()
            {
                var c = new C();
                c[1] = 2;
            }

            int this[object x] { set { } }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542648")]
    public async Task ParenthesizeAfterCastIfNeeded()
    {
        await TestAsync(
        """
        using System;

        enum E { }

        class Program
        {
            static void Main()
            {
                E x[||] = (global::E) -1;
                object y = x;
            }
        }
        """,

        """
        using System;

        enum E { }

        class Program
        {
            static void Main()
            {
                object y = (global::E) -1;
            }
        }
        """,
        parseOptions: null);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544635")]
    public async Task InsertCastForEnumZeroIfBoxed()
    {
        await TestAsync(
        """
        using System;
        class Program
        {
            static void M()
            {
                DayOfWeek x[||] = 0;
                object y = x;
                Console.WriteLine(y);
            }
        }
        """,

        """
        using System;
        class Program
        {
            static void M()
            {
                object y = (DayOfWeek)0;
                Console.WriteLine(y);
            }
        }
        """,
        parseOptions: null);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554010")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544636")]
    public async Task InsertCastForMethodGroupIfNeeded1()
    {
        await TestAsync(
        """
        using System;
        class Program
        {
            static void M()
            {
                Action a[||] = Console.WriteLine;
                Action b = a + Console.WriteLine;
            }
        }
        """,

        """
        using System;
        class Program
        {
            static void M()
            {
                Action b = (Action)Console.WriteLine + Console.WriteLine;
            }
        }
        """,
        parseOptions: null);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554010")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544978")]
    public async Task InsertCastForMethodGroupIfNeeded2()
    {
        await TestAsync(
        """
        using System;
        class Program
        {
            static void Main()
            {
                Action a[||] = Console.WriteLine;
                object b = a;
            }
        }
        """,

        """
        using System;
        class Program
        {
            static void Main()
            {
                object b = (Action)Console.WriteLine;
            }
        }
        """,
        parseOptions: null);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545103")]
    public async Task DoNotInsertCastForTypeThatNoLongerBindsToTheSameType()
    {
        await TestAsync(
        """
        class A<T>
        {
            static T x;
            class B<U>
            {
                static void Goo()
                {
                    var y[||] = x;
                    var z = y;
                }
            }
        }
        """,

        """
        class A<T>
        {
            static T x;
            class B<U>
            {
                static void Goo()
                {
                    var z = x;
                }
            }
        }
        """,
        parseOptions: null);
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/56938")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545170")]
    public async Task InsertCorrectCastForDelegateCreationExpression()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;

        class Program
        {
            static void Main()
            {
                Predicate<object> x[||] = y => true;
                var z = new Func<string, bool>(x);
            }
        }
        """,

        """
        using System;

        class Program
        {
            static void Main()
            {
                var z = new Func<string, bool>(y => true);
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545523")]
    public async Task DoNotInsertCastForObjectCreationIfUnneeded()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;
        class Program
        {
            static void Main()
            {
                Exception e[||] = new ArgumentException();
                Type b = e.GetType();
            }
        }
        """,

        """
        using System;
        class Program
        {
            static void Main()
            {
                Type b = new ArgumentException().GetType();
            }
        }
        """);
    }

    [Fact]
    public async Task DoNotInsertCastInForeachIfUnneeded01()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;
        using System.Collections.Generic;

        class Program
        {
            static void Main()
            {
                IEnumerable<char> s[||] = "abc";
                foreach (var x in s)
                    Console.WriteLine(x);
            }
        }
        """,

        """
        using System;
        using System.Collections.Generic;

        class Program
        {
            static void Main()
            {
                foreach (var x in (IEnumerable<char>)"abc")
                    Console.WriteLine(x);
            }
        }
        """);
    }

    [Fact]
    public async Task InsertCastInForeachIfNeeded01()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;
        using System.Collections;

        class Program
        {
            static void Main()
            {
                IEnumerable s[||] = "abc";
                foreach (object x in s)
                    Console.WriteLine(x);
            }
        }
        """,

        """
        using System;
        using System.Collections;

        class Program
        {
            static void Main()
            {
                foreach (object x in (IEnumerable)"abc")
                    Console.WriteLine(x);
            }
        }
        """);
    }

    [Fact]
    public async Task InsertCastInForeachIfNeeded02()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;
        using System.Collections;

        class Program
        {
            static void Main()
            {
                IEnumerable s[||] = "abc";
                foreach (char x in s)
                    Console.WriteLine(x);
            }
        }
        """,

        """
        using System;
        using System.Collections;

        class Program
        {
            static void Main()
            {
                foreach (char x in (IEnumerable)"abc")
                    Console.WriteLine(x);
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545601")]
    public async Task InsertCastToKeepGenericMethodInference()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;
        class C
        {
            static T Goo<T>(T x, T y) { return default(T); }

            static void M()
            {
                long [||]x = 1;
                IComparable<long> c = Goo(x, x);
            }
        }
        """,

        """
        using System;
        class C
        {
            static T Goo<T>(T x, T y) { return default(T); }

            static void M()
            {
                IComparable<long> c = Goo(1, (long)1);
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545601")]
    public async Task InsertCastForKeepImplicitArrayInference()
    {
        await TestInRegularAndScriptAsync(
        """
        class C
        {
            static void M()
            {
                object x[||] = null;
                var a = new[] { x, x };
                Goo(a);
            }

            static void Goo(object[] o) { }
        }
        """,

        """
        class C
        {
            static void M()
            {
                var a = new[] { null, (object)null };
                Goo(a);
            }

            static void Goo(object[] o) { }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545601")]
    public async Task InsertASingleCastToNotBreakOverloadResolution()
    {
        await TestInRegularAndScriptAsync(
        """
        class C
        {
            static void M()
            {
                long x[||] = 42;
                Goo(x, x);
            }

            static void Goo(int x, int y) { }
            static void Goo(long x, long y) { }
        }
        """,

        """
        class C
        {
            static void M()
            {
                Goo(42, (long)42);
            }

            static void Goo(int x, int y) { }
            static void Goo(long x, long y) { }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545601")]
    public async Task InsertASingleCastToNotBreakOverloadResolutionInLambdas()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;
        class C
        {
            static void M()
            {
                long x[||] = 42;
                Goo(() => { return x; }, () => { return x; });
            }

            static void Goo(Func<int> x, Func<int> y) { }
            static void Goo(Func<long> x, Func<long> y) { }
        }
        """,

        """
        using System;
        class C
        {
            static void M()
            {
                Goo(() => { return 42; }, () => { return (long)42; });
            }

            static void Goo(Func<int> x, Func<int> y) { }
            static void Goo(Func<long> x, Func<long> y) { }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545601")]
    public async Task InsertASingleCastToNotBreakResolutionOfOperatorOverloads()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;
        class C
        {
            private int value;

            void M()
            {
                C x[||] = 42;
                Console.WriteLine(x + x);
            }

            public static int operator +(C x, C y)
            {
                return x.value + y.value;
            }

            public static implicit operator C(int l)
            {
                var c = new C();
                c.value = l;
                return c;
            }

            static void Main()
            {
                new C().M();
            }
        }
        """,

        """
        using System;
        class C
        {
            private int value;

            void M()
            {
                Console.WriteLine((C)42 + (C)42);
            }

            public static int operator +(C x, C y)
            {
                return x.value + y.value;
            }

            public static implicit operator C(int l)
            {
                var c = new C();
                c.value = l;
                return c;
            }

            static void Main()
            {
                new C().M();
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545561")]
    public async Task InsertCastToNotBreakOverloadResolutionInUncheckedContext()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;

        class X
        {
            static int Goo(Func<int?, byte> x, object y) { return 1; }
            static int Goo(Func<X, byte> x, string y) { return 2; }

            const int Value = 1000;
            static void Main()
            {
                var a[||] = Goo(X => (byte)X.Value, null);
                unchecked
                {
                    Console.WriteLine(a);
                }
            }
        }
        """,

        """
        using System;

        class X
        {
            static int Goo(Func<int?, byte> x, object y) { return 1; }
            static int Goo(Func<X, byte> x, string y) { return 2; }

            const int Value = 1000;
            static void Main()
            {
                unchecked
                {
                    Console.WriteLine(Goo(X => (byte)X.Value, null));
                }
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545564")]
    public async Task InsertCastToNotBreakOverloadResolutionInUnsafeContext()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;

        static class C
        {
            static int Outer(Action<int> x, object y) { return 1; }
            static int Outer(Action<string> x, string y) { return 2; }

            static void Inner(int x, int[] y) { }
            unsafe static void Inner(string x, int*[] y) { }

            static void Main()
            {
                var a[||] = Outer(x => Inner(x, null), null);
                unsafe
                {
                    Console.WriteLine(a);
                }
            }
        }
        """,

        """
        using System;

        static class C
        {
            static int Outer(Action<int> x, object y) { return 1; }
            static int Outer(Action<string> x, string y) { return 2; }

            static void Inner(int x, int[] y) { }
            unsafe static void Inner(string x, int*[] y) { }

            static void Main()
            {
                unsafe
                {
                    Console.WriteLine(Outer(x => Inner(x, null), null));
                }
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545783")]
    public async Task InsertCastToNotBreakOverloadResolutionInNestedLambdas()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;

        class C
        {
            static void Goo(Action<object> a) { }
            static void Goo(Action<string> a) { }

            static void Main()
            {
                Goo(x =>
                {
                    string s[||] = x;
                    var y = s;
                });
            }
        }
        """,

        """
        using System;

        class C
        {
            static void Goo(Action<object> a) { }
            static void Goo(Action<string> a) { }

            static void Main()
            {
                Goo(x =>
                {
                    var y = (string)x;
                });
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546069")]
    public async Task TestBrokenVariableDeclarator()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                static void M()
                {
                    int [||]a[10] = {
                        0,
                        0
                    };
                    System.Console.WriteLine(a);
                }
            }
            """);
    }

    [Fact]
    public async Task TestHiddenRegion1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    int [|x|] = 0;

            #line hidden
                    Goo(x);
            #line default
                }
            }
            """);
    }

    [Fact]
    public async Task TestHiddenRegion2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    int [|x|] = 0;
                    Goo(x);
            #line hidden
                    Goo(x);
            #line default
                }
            }
            """);
    }

    [Fact]
    public async Task TestHiddenRegion3()
    {
        await TestInRegularAndScriptAsync(
            """
            #line default
            class Program
            {
                void Main()
                {
                    int [|x|] = 0;

                    Goo(x);
                    #line hidden
                    Goo();
                    #line default
                }
            }
            """,
            """
            #line default
            class Program
            {
                void Main()
                {

                    Goo(0);
                    #line hidden
                    Goo();
                    #line default
                }
            }
            """);
    }

    [Fact]
    public async Task TestHiddenRegion4()
    {
        await TestInRegularAndScriptAsync(
            """
            #line default
            class Program
            {
                void Main()
                {
                    int [||]x = 0;

                    Goo(x);
            #line hidden
                    Goo();
            #line default
                    Goo(x);
                }
            }
            """,
            """
            #line default
            class Program
            {
                void Main()
                {

                    Goo(0);
            #line hidden
                    Goo();
            #line default
                    Goo(0);
                }
            }
            """);
    }

    [Fact]
    public async Task TestHiddenRegion5()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    int [||]x = 0;
                    Goo(x);
            #line hidden
                    Goo(x);
            #line default
                    Goo(x);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530743")]
    public async Task InlineFromLabeledStatement()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;

        class Program
        {
            static void Main()
            {
            label:
                int [||]x = 1;
                Console.WriteLine();
                int y = x;        
            }
        }
        """,

        """
        using System;

        class Program
        {
            static void Main()
            {
            label:
                Console.WriteLine();
                int y = 1;        
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529698")]
    public async Task InlineCompoundAssignmentIntoInitializer()
    {
        await TestInRegularAndScriptAsync(
        """
        using System.Collections.Generic;

        class Program
        {
            static void Main()
            {
                int x = 0;
                int y[||] = x += 1;
                var z = new List<int> { y };
            }
        }
        """,

        """
        using System.Collections.Generic;

        class Program
        {
            static void Main()
            {
                int x = 0;
                var z = new List<int> { (x += 1) };
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609497")]
    public async Task Bugfix_609497()
    {
        await TestInRegularAndScriptAsync(
        """
        using System.Collections.Generic;

        class Program
        {
            static void Main()
            {
                IList<dynamic> x[||] = new List<object>();
                IList<object> y = x;
            }
        }
        """,

        """
        using System.Collections.Generic;

        class Program
        {
            static void Main()
            {
                IList<object> y = new List<object>();
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636319")]
    public async Task Bugfix_636319()
    {
        await TestInRegularAndScriptAsync(
        """
        using System.Collections.Generic;

        class Program
        {
            static void Main()
            {
                IList<object> x[||] = new List<dynamic>();
                IList<dynamic> y = x;
            }
        }
        """,

        """
        using System.Collections.Generic;

        class Program
        {
            static void Main()
            {
                IList<dynamic> y = new List<dynamic>();
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609492")]
    public async Task Bugfix_609492()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;

        class Program
        {
            static void Main()
            {
                ValueType x[||] = 1;
                object y = x;
            }
        }
        """,

        """
        using System;

        class Program
        {
            static void Main()
            {
                object y = 1;
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529950")]
    public async Task InlineTempDoesNotInsertUnnecessaryExplicitTypeInLambdaParameter()
    {
        await TestInRegularAndScript1Async(
        """
        using System;

        static class C
        {
            static void Inner(Action<string> x, string y) { }
            static void Inner(Action<string> x, int y) { }
            static void Inner(Action<int> x, int y) { }

            static void Outer(Action<string> x, object y) { Console.WriteLine(1); }
            static void Outer(Action<int> x, string y) { Console.WriteLine(2); }

            static void Main()
            {
                Outer(y => Inner(x => { var z[||] = x; Action a = () => z.GetType(); }, y), null);
            }
        }
        """,

        """
        using System;

        static class C
        {
            static void Inner(Action<string> x, string y) { }
            static void Inner(Action<string> x, int y) { }
            static void Inner(Action<int> x, int y) { }

            static void Outer(Action<string> x, object y) { Console.WriteLine(1); }
            static void Outer(Action<int> x, string y) { Console.WriteLine(2); }

            static void Main()
            {
                Outer(y => Inner(x => { Action a = () => ((string)x).GetType(); }, y), null);
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/619425")]
    public async Task Bugfix_619425_RestrictedSimpleNameExpansion()
    {
        await TestInRegularAndScriptAsync(
        """
        class A<B>
        {
            class C : A<C>
            {
                class B : C
                {
                    void M()
                    {
                        var x[||] = new C[0];
                        C[] y = x;
                    }
                }
            }
        }
        """,

        """
        class A<B>
        {
            class C : A<C>
            {
                class B : C
                {
                    void M()
                    {
                        C[] y = new C[0];
                    }
                }
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529840")]
    public async Task Bugfix_529840_DetectSemanticChangesAtInlineSite()
    {
        await TestInRegularAndScriptAsync(
        """
        using System;

        class A
        {
            static void Main()
            {
                var a[||] = new A(); // Inline a
                Goo(a);
            }

            static void Goo(long x)
            {
                Console.WriteLine(x);
            }

            public static implicit operator int (A x)
            {
                return 1;
            }

            public static explicit operator long (A x)
            {
                return 2;
            }
        }
        """,

        """
        using System;

        class A
        {
            static void Main()
            {
                // Inline a
                Goo(new A());
            }

            static void Goo(long x)
            {
                Console.WriteLine(x);
            }

            public static implicit operator int (A x)
            {
                return 1;
            }

            public static explicit operator long (A x)
            {
                return 2;
            }
        }
        """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1091946")]
    public async Task TestConditionalAccessWithConversion()
    {
        await TestInRegularAndScriptAsync(
            """
            class A
            {
                bool M(string[] args)
                {
                    var [|x|] = args[0];
                    return x?.Length == 0;
                }
            }
            """,
            """
            class A
            {
                bool M(string[] args)
                {
                    return args[0]?.Length == 0;
                }
            }
            """);
    }

    [Fact]
    public async Task TestSimpleConditionalAccess()
    {
        await TestInRegularAndScriptAsync(
            """
            class A
            {
                void M(string[] args)
                {
                    var [|x|] = args.Length.ToString();
                    var y = x?.ToString();
                }
            }
            """,
            """
            class A
            {
                void M(string[] args)
                {
                    var y = args.Length.ToString()?.ToString();
                }
            }
            """);
    }

    [Fact]
    public async Task TestConditionalAccessWithConditionalExpression()
    {
        await TestInRegularAndScriptAsync(
            """
            class A
            {
                void M(string[] args)
                {
                    var [|x|] = args[0]?.Length ?? 10;
                    var y = x == 10 ? 10 : 4;
                }
            }
            """,
            """
            class A
            {
                void M(string[] args)
                {
                    var y = (args[0]?.Length ?? 10) == 10 ? 10 : 4;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2593")]
    public async Task TestConditionalAccessWithExtensionMethodInvocation()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            static class M
            {
                public static IEnumerable<string> Something(this C cust)
                {
                    throw new NotImplementedException();
                }
            }

            class C
            {
                private object GetAssemblyIdentity(IEnumerable<C> types)
                {
                    foreach (var t in types)
                    {
                        var [|assembly|] = t?.Something().First();
                        var identity = assembly?.ToArray();
                    }

                    return null;
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            static class M
            {
                public static IEnumerable<string> Something(this C cust)
                {
                    throw new NotImplementedException();
                }
            }

            class C
            {
                private object GetAssemblyIdentity(IEnumerable<C> types)
                {
                    foreach (var t in types)
                    {
                        var identity = (t?.Something().First())?.ToArray();
                    }

                    return null;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2593")]
    public async Task TestConditionalAccessWithExtensionMethodInvocation_2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            static class M
            {
                public static IEnumerable<string> Something(this C cust)
                {
                    throw new NotImplementedException();
                }

                public static Func<C> Something2(this C cust)
                {
                    throw new NotImplementedException();
                }
            }

            class C
            {
                private object GetAssemblyIdentity(IEnumerable<C> types)
                {
                    foreach (var t in types)
                    {
                        var [|assembly|] = (t?.Something2())()?.Something().First();
                        var identity = assembly?.ToArray();
                    }

                    return null;
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            static class M
            {
                public static IEnumerable<string> Something(this C cust)
                {
                    throw new NotImplementedException();
                }

                public static Func<C> Something2(this C cust)
                {
                    throw new NotImplementedException();
                }
            }

            class C
            {
                private object GetAssemblyIdentity(IEnumerable<C> types)
                {
                    foreach (var t in types)
                    {
                        var identity = ((t?.Something2())()?.Something().First())?.ToArray();
                    }

                    return null;
                }
            }
            """);
    }

    [Fact]
    public async Task TestAliasQualifiedNameIntoInterpolation()
    {
        await TestInRegularAndScriptAsync(
            """
            class A
            {
                void M()
                {
                    var [|g|] = global::System.Guid.Empty;
                    var s = $"{g}";
                }
            }
            """,
            """
            class A
            {
                void M()
                {
                    var s = $"{(global::System.Guid.Empty)}";
                }
            }
            """);
    }

    [Fact]
    public async Task TestConditionalExpressionIntoInterpolation()
    {
        await TestInRegularAndScriptAsync(
            """
            class A
            {
                bool M(bool b)
                {
                    var [|x|] = b ? 19 : 23;
                    var s = $"{x}";
                }
            }
            """,
            """
            class A
            {
                bool M(bool b)
                {
                    var s = $"{(b ? 19 : 23)}";
                }
            }
            """);
    }

    [Fact]
    public async Task TestConditionalExpressionIntoInterpolationWithFormatClause()
    {
        await TestInRegularAndScriptAsync(
            """
            class A
            {
                bool M(bool b)
                {
                    var [|x|] = b ? 19 : 23;
                    var s = $"{x:x}";
                }
            }
            """,
            """
            class A
            {
                bool M(bool b)
                {
                    var s = $"{(b ? 19 : 23):x}";
                }
            }
            """);
    }

    [Fact]
    public async Task TestInvocationExpressionIntoInterpolation()
    {
        await TestInRegularAndScriptAsync(
            """
            class A
            {
                public static void M(string s)
                {
                    var [|x|] = s.ToUpper();
                    var y = $"{x}";
                }
            }
            """,
            """
            class A
            {
                public static void M(string s)
                {
                    var y = $"{s.ToUpper()}";
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4583")]
    public async Task DoNotParenthesizeInterpolatedStringWithNoInterpolation_CSharp7()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                public void M()
                {
                    var [|s1|] = $"hello";
                    var s2 = string.Replace(s1, "world");
                }
            }
            """,
            """
            class C
            {
                public void M()
                {
                    var s2 = string.Replace($"hello", "world");
                }
            }
            """, parseOptions: TestOptions.Regular7);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33108")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/4583")]
    public async Task CastInterpolatedStringWhenInliningIntoInvalidCall()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                public void M()
                {
                    var [|s1|] = $"hello";
                    var s2 = string.Replace(s1, "world");
                }
            }
            """,
            """
            class C
            {
                public void M()
                {
                    var s2 = string.Replace($"hello", "world");
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33108")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/4583")]
    public async Task DoNotCastInterpolatedStringWhenInliningIntoValidCall()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                public void M()
                {
                    var [|s1|] = $"hello";
                    var s2 = Replace(s1, "world");
                }

                void Replace(string s1, string s2) { }
            }
            """,
            """
            class C
            {
                public void M()
                {
                    var s2 = Replace($"hello", "world");
                }

                void Replace(string s1, string s2) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4583")]
    public async Task DoNotParenthesizeInterpolatedStringWithInterpolation_CSharp7()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                public void M(int x)
                {
                    var [|s1|] = $"hello {x}";
                    var s2 = string.Replace(s1, "world");
                }
            }
            """,
            """
            class C
            {
                public void M(int x)
                {
                    var s2 = string.Replace($"hello {x}", "world");
                }
            }
            """, parseOptions: TestOptions.Regular7);
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/33108")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/4583")]
    public async Task DoNotParenthesizeInterpolatedStringWithInterpolation()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                public void M(int x)
                {
                    var [|s1|] = $"hello {x}";
                    var s2 = string.Replace(s1, "world");
                }
            }
            """,
            """
            class C
            {
                public void M(int x)
                {
                    var s2 = string.Replace($"hello {x}", "world");
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15530")]
    public async Task PArenthesizeAwaitInlinedIntoReducedExtensionMethod()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Linq;
            using System.Threading.Tasks;

            internal class C
            {
                async Task M()
                {
                    var [|t|] = await Task.FromResult("");
                    t.Any();
                }
            }
            """,
            """
            using System.Linq;
            using System.Threading.Tasks;

            internal class C
            {
                async Task M()
                {
                    (await Task.FromResult("")).Any();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4583")]
    public async Task InlineFormattableStringIntoCallSiteRequiringFormattableString()
    {
        const string initial = """
            using System;
            """ + CodeSnippets.FormattableStringType + """
            class C
            {
                static void M(FormattableString s)
                {
                }

                static void N(int x, int y)
                {
                    FormattableString [||]s = $"{x}, {y}";
                    M(s);
                }
            }
            """;

        const string expected = """
            using System;
            """ + CodeSnippets.FormattableStringType + """
            class C
            {
                static void M(FormattableString s)
                {
                }

                static void N(int x, int y)
                {
                    M($"{x}, {y}");
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4624")]
    public async Task InlineFormattableStringIntoCallSiteWithFormattableStringOverload()
    {
        const string initial = """
            using System;
            """ + CodeSnippets.FormattableStringType + """
            class C
            {
                static void M(string s) { }
                static void M(FormattableString s) { }

                static void N(int x, int y)
                {
                    FormattableString [||]s = $"{x}, {y}";
                    M(s);
                }
            }
            """;

        const string expected = """
            using System;
            """ + CodeSnippets.FormattableStringType + """
            class C
            {
                static void M(string s) { }
                static void M(FormattableString s) { }

                static void N(int x, int y)
                {
                    M((FormattableString)$"{x}, {y}");
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9576")]
    public async Task InlineIntoLambdaWithReturnStatementWithNoExpression()
    {
        const string initial = """
            using System;
            class C
            {
                static void M(Action a) { }

                static void N()
                {
                    var [||]x = 42;
                    M(() =>
                    {
                        Console.WriteLine(x);
                        return;
                    });
                }
            }
            """;

        const string expected = """
            using System;
            class C
            {
                static void M(Action a) { }

                static void N()
                {
                    M(() =>
                    {
                        Console.WriteLine(42);
                        return;
                    });
                }
            }
            """;

        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp6)]
    [InlineData(LanguageVersion.CSharp12)]
    public async Task Tuples(LanguageVersion version)
    {
        var code = """
            using System;
            class C
            {
                public void M()
                {
                    (int, string) [||]x = (1, "hello");
                    x.ToString();
                }
            }
            """;

        var expected = """
            using System;
            class C
            {
                public void M()
                {
                    (1, "hello").ToString();
                }
            }
            """;

        await TestInRegularAndScript1Async(
            code,
            expected,
            new TestParameters(parseOptions: TestOptions.Regular.WithLanguageVersion(version)));
    }

    [Fact]
    public async Task TuplesWithNames()
    {
        var code = """
            using System;
            class C
            {
                public void M()
                {
                    (int a, string b) [||]x = (a: 1, b: "hello");
                    x.ToString();
                }
            }
            """;

        var expected = """
            using System;
            class C
            {
                public void M()
                {
                    (a: 1, b: "hello").ToString();
                }
            }
            """;

        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11028")]
    public async Task TuplesWithDifferentNames()
    {
        var code = """
            class C
            {
                public void M()
                {
                    (int a, string b) [||]x = (c: 1, d: "hello");
                    x.a.ToString();
                }
            }
            """;

        var expected = """
            class C
            {
                public void M()
                {
                    (((int a, string b))(c: 1, d: "hello")).a.ToString();
                }
            }
            """;

        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact]
    public async Task Deconstruction()
    {
        var code = """
            using System;
            class C
            {
                public void M()
                {
                    var [||]temp = new C();
                    var (x1, x2) = temp;
                    var x3 = temp;
                }
            }
            """;

        var expected = """
            using System;
            class C
            {
                public void M()
                {
                    {|Warning:var (x1, x2) = new C()|};
                    var x3 = new C();
                }
            }
            """;

        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12802")]
    public async Task Deconstruction2()
    {
        var code = """
            class Program
            {
                static void Main()
                {
                    var [||]kvp = KVP.Create(42, "hello");
                    var(x1, x2) = kvp;
                }
            }
            public static class KVP
            {
                public static KVP<T1, T2> Create<T1, T2>(T1 item1, T2 item2) { return null; }
            }
            public class KVP<T1, T2>
            {
                public void Deconstruct(out T1 item1, out T2 item2) { item1 = default(T1); item2 = default(T2); }
            }
            """;

        var expected = """
            class Program
            {
                static void Main()
                {
                    var (x1, x2) = KVP.Create(42, "hello");
                }
            }
            public static class KVP
            {
                public static KVP<T1, T2> Create<T1, T2>(T1 item1, T2 item2) { return null; }
            }
            public class KVP<T1, T2>
            {
                public void Deconstruct(out T1 item1, out T2 item2) { item1 = default(T1); item2 = default(T2); }
            }
            """;

        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11958")]
    public async Task EnsureParenthesesInStringConcatenation()
    {
        var code = """
            class C
            {
                void M()
                {
                    int [||]i = 1 + 2;
                    string s = "a" + i;
                }
            }
            """;

        var expected = """
            class C
            {
                void M()
                {
                    string s = "a" + (1 + 2);
                }
            }
            """;

        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact]
    public async Task ExplicitTupleNameAdded()
    {
        var code = """
            class C
            {
                void M()
                {
                    int [||]i = 1 + 2;
                    var t = (i, 3);
                }
            }
            """;

        var expected = """
            class C
            {
                void M()
                {
                    var t = (i: 1 + 2, 3);
                }
            }
            """;

        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact]
    public async Task ExplicitTupleNameAdded_Trivia()
    {
        var code = """
            class C
            {
                void M()
                {
                    int [||]i = 1 + 2;
                    var t = ( /*comment*/ i, 3);
                }
            }
            """;

        var expected = """
            class C
            {
                void M()
                {
                    var t = ( /*comment*/ i: 1 + 2, 3);
                }
            }
            """;

        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact]
    public async Task ExplicitTupleNameAdded_Trivia2()
    {
        var code = """
            class C
            {
                void M()
                {
                    int [||]i = 1 + 2;
                    var t = (
                        /*comment*/ i,
                        /*comment*/ 3
                    );
                }
            }
            """;

        var expected = """
            class C
            {
                void M()
                {
                    var t = (
                        /*comment*/ i: 1 + 2,
                        /*comment*/ 3
                    );
                }
            }
            """;

        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact]
    public async Task ExplicitTupleNameAdded_NoDuplicateNames()
    {
        var code = """
            class C
            {
                void M()
                {
                    int [||]i = 1 + 2;
                    var t = (i, i);
                }
            }
            """;

        var expected = """
            class C
            {
                void M()
                {
                    var t = (1 + 2, 1 + 2);
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19047")]
    public async Task ExplicitTupleNameAdded_DeconstructionDeclaration()
    {
        var code = """
            class C
            {
                static int y = 1;
                void M()
                {
                    int [||]i = C.y;
                    var t = ((i, (i, _)) = (1, (i, 3)));
                }
            }
            """;
        var expected = """
            class C
            {
                static int y = 1;
                void M()
                {
                    int i = C.y;
                    var t = (({|Conflict:i|}, ({|Conflict:i|}, _)) = (1, (i: C.y, 3)));
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19047")]
    public async Task ExplicitTupleNameAdded_DeconstructionDeclaration2()
    {
        var code = """
            class C
            {
                static int y = 1;
                void M()
                {
                    int [||]i = C.y;
                    var t = ((i, _) = (1, 2));
                }
            }
            """;
        var expected = """
            class C
            {
                static int y = 1;
                void M()
                {
                    int i = C.y;
                    var t = (({|Conflict:i|}, _) = (1, 2));
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact]
    public async Task ExplicitTupleNameAdded_NoReservedNames()
    {
        var code = """
            class C
            {
                void M()
                {
                    int [||]Rest = 1 + 2;
                    var t = (Rest, 3);
                }
            }
            """;

        var expected = """
            class C
            {
                void M()
                {
                    var t = (1 + 2, 3);
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact]
    public async Task ExplicitTupleNameAdded_NoReservedNames2()
    {
        var code = """
            class C
            {
                void M()
                {
                    int [||]Item1 = 1 + 2;
                    var t = (Item1, 3);
                }
            }
            """;

        var expected = """
            class C
            {
                void M()
                {
                    var t = (1 + 2, 3);
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact]
    public async Task ExplicitTupleNameAdded_EscapeKeywords()
    {
        var code = """
            class C
            {
                void M()
                {
                    int [||]@int = 1 + 2;
                    var t = (@int, 3);
                }
            }
            """;

        var expected = """
            class C
            {
                void M()
                {
                    var t = (@int: 1 + 2, 3);
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact]
    public async Task ExplicitTupleNameAdded_KeepEscapedName()
    {
        var code = """
            class C
            {
                void M()
                {
                    int [||]@where = 1 + 2;
                    var t = (@where, 3);
                }
            }
            """;

        var expected = """
            class C
            {
                void M()
                {
                    var t = (@where: 1 + 2, 3);
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact]
    public async Task ExplicitAnonymousTypeMemberNameAdded_DuplicateNames()
    {
        var code = """
            class C
            {
                void M()
                {
                    int [||]i = 1 + 2;
                    var t = new { i, i }; // error already
                }
            }
            """;

        var expected = """
            class C
            {
                void M()
                {
                    var t = new { i = 1 + 2, i = 1 + 2 }; // error already
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact]
    public async Task ExplicitAnonymousTypeMemberNameAdded_AssignmentEpression()
    {
        var code = """
            class C
            {
                void M()
                {
                    int j = 0;
                    int [||]i = j = 1;
                    var t = new { i, k = 3 };
                }
            }
            """;

        var expected = """
            class C
            {
                void M()
                {
                    int j = 0;
                    var t = new { i = j = 1, k = 3 };
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact]
    public async Task ExplicitAnonymousTypeMemberNameAdded_Comment()
    {
        var code = """
            class C
            {
                void M()
                {
                    int [||]i = 1 + 2;
                    var t = new { /*comment*/ i, j = 3 };
                }
            }
            """;

        var expected = """
            class C
            {
                void M()
                {
                    var t = new { /*comment*/ i = 1 + 2, j = 3 };
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact]
    public async Task ExplicitAnonymousTypeMemberNameAdded_Comment2()
    {
        var code = """
            class C
            {
                void M()
                {
                    int [||]i = 1 + 2;
                    var t = new {
                        /*comment*/ i,
                        /*comment*/ j = 3
                    };
                }
            }
            """;

        var expected = """
            class C
            {
                void M()
                {
                    var t = new {
                        /*comment*/ i = 1 + 2,
                        /*comment*/ j = 3
                    };
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19247")]
    public async Task InlineTemporary_LocalFunction()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    var [|testStr|] = "test";
                    expand(testStr);

                    void expand(string str)
                    {

                    }
                }
            }
            """,

            """
            using System;
            class C
            {
                void M()
                {
                    expand("test");

                    void expand(string str)
                    {

                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11712")]
    public async Task InlineTemporary_RefParams()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                bool M<T>(ref T x) 
                {
                    var [||]b = M(ref x);
                    return b || b;
                }
            }
            """,

            """
            class C
            {
                bool M<T>(ref T x) 
                {
                    return {|Warning:M(ref x) || M(ref x)|};
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11712")]
    public async Task InlineTemporary_OutParams()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                bool M<T>(out T x) 
                {
                    var [||]b = M(out x);
                    return b || b;
                }
            }
            """,

            """
            class C
            {
                bool M<T>(out T x) 
                {
                    return {|Warning:M(out x) || M(out x)|};
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24791")]
    public async Task InlineVariableDoesNotAddUnnecessaryCast()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                bool M()
                {
                    var [||]o = M();
                    if (!o) throw null;
                    throw null;
                }
            }
            """,
            """
            class C
            {
                bool M()
                {
                    if (!M()) throw null;
                    throw null;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16819")]
    public async Task InlineVariableDoesNotAddsDuplicateCast()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    var [||]o = (Exception)null;
                    Console.Write(o == new Exception());
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    Console.Write((Exception)null == new Exception());
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30903")]
    public async Task InlineVariableContainsAliasOfValueTupleType()
    {
        await TestInRegularAndScriptAsync(
            """
            using X = System.ValueTuple<int, int>;

            class C
            {
                void M()
                {
                    var [|x|] = (X)(0, 0);
                    var x2 = x;
                }
            }
            """,
            """
            using X = System.ValueTuple<int, int>;

            class C
            {
                void M()
                {
                    var x2 = (X)(0, 0);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30903")]
    public async Task InlineVariableContainsAliasOfMixedValueTupleType()
    {
        await TestInRegularAndScriptAsync(
            """
            using X = System.ValueTuple<int, (int, int)>;

            class C
            {
                void M()
                {
                    var [|x|] = (X)(0, (0, 0));
                    var x2 = x;
                }
            }
            """,
            """
            using X = System.ValueTuple<int, (int, int)>;

            class C
            {
                void M()
                {
                    var x2 = (X)(0, (0, 0));
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35645")]
    public async Task UsingDeclaration()
    {
        var code = """
            using System;
            class C : IDisposable
            {
                public void M()
                {
                    using var [||]c = new C();
                    c.ToString();
                }
                public void Dispose() { }
            }
            """;

        await TestMissingInRegularAndScriptAsync(code, new TestParameters(parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp8)));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task Selections1()
    {
        await TestFixOneAsync(
"""
{ [|int x = 0;|]

Console.WriteLine(x); }
""",
"""
{
        Console.WriteLine(0); }
""");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task Selections2()
    {
        await TestFixOneAsync(
"""
{ int [|x = 0|], y = 1;

Console.WriteLine(x); }
""",
"""
{
        int y = 1;

        Console.WriteLine(0); }
""");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task Selections3()
    {
        await TestFixOneAsync(
"""
{ int x = 0, [|y = 1|], z = 2;

Console.WriteLine(y); }
""",
"""
{
        int x = 0, z = 2;

        Console.WriteLine(1); }
""");
    }

    [Fact]
    public async Task WarnOnInlineIntoConditional1()
    {
        await TestFixOneAsync(
"""
{ var [|x = true|];

System.Diagnostics.Debug.Assert(x); }
""",
"""
{
        {|Warning:System.Diagnostics.Debug.Assert(true)|}; }
""");
    }

    [Fact]
    public async Task WarnOnInlineIntoConditional2()
    {
        await TestFixOneAsync(
"""
{ var [|x = true|];

System.Diagnostics.Debug.Assert(x == true); }
""",
"""
{
        {|Warning:System.Diagnostics.Debug.Assert(true == true)|}; }
""");
    }

    [Fact]
    public async Task WarnOnInlineIntoMultipleConditionalLocations()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var [|x = true|];
                    System.Diagnostics.Debug.Assert(x);
                    System.Diagnostics.Debug.Assert(x);
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    {|Warning:System.Diagnostics.Debug.Assert(true)|};
                    {|Warning:System.Diagnostics.Debug.Assert(true)|};
                }
            }
            """);
    }

    [Fact]
    public async Task OnlyWarnOnConditionalLocations()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    var [|x = true|];
                    System.Diagnostics.Debug.Assert(x);
                    Console.Writeline(x);
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    {|Warning:System.Diagnostics.Debug.Assert(true)|};
                    Console.Writeline(true);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40201")]
    public async Task TestUnaryNegationOfDeclarationPattern()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Threading;

            class C
            {
                void Test()
                {
                    var [|ct|] = CancellationToken.None;
                    if (!(Helper(ct) is string notDiscard)) { }
                }

                object Helper(CancellationToken ct) { return null; }
            }
            """,
            """
            using System.Threading;

            class C
            {
                void Test()
                {
                    if (!(Helper(CancellationToken.None) is string notDiscard)) { }
                }

                object Helper(CancellationToken ct) { return null; }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18322")]
    public async Task TestInlineIntoExtensionMethodInvokedOnThis()
    {
        await TestInRegularAndScriptAsync(
            """
            public class Class1
            {
                void M()
                {
                    var [|c|] = 8;
                    this.DoStuff(c);
                }
            }

            public static class Class1Extensions { public static void DoStuff(this Class1 c, int x) { } }
            """,
            """
            public class Class1
            {
                void M()
                {
                    this.DoStuff(8);
                }
            }

            public static class Class1Extensions { public static void DoStuff(this Class1 c, int x) { } }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8716")]
    public async Task DoNotQualifyInlinedLocalFunction()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class C
            {
                void Main()
                {
                    void LocalFunc()
                    {
                        Console.Write(2);
                    }
                    var [||]local = new Action(LocalFunc);
                    local();
                }
            }
            """,
            """
            using System;
            class C
            {
                void Main()
                {
                    void LocalFunc()
                    {
                        Console.Write(2);
                    }
                    new Action(LocalFunc)();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22540")]
    public async Task DoNotQualifyWhenInliningIntoPattern_01()
    {
        await TestInRegularAndScriptAsync("""
            using Syntax;

            namespace Syntax
            {
                class AwaitExpressionSyntax : ExpressionSyntax { public ExpressionSyntax Expression; }
                class ExpressionSyntax { }
                class ParenthesizedExpressionSyntax : ExpressionSyntax { }
            }

            static class Goo
            {
                static void Bar(AwaitExpressionSyntax awaitExpression)
                {
                    ExpressionSyntax [||]expression = awaitExpression.Expression;

                    if (!(expression is ParenthesizedExpressionSyntax parenthesizedExpression))
                        return;
                }
            }
            """,
            """
            using Syntax;

            namespace Syntax
            {
                class AwaitExpressionSyntax : ExpressionSyntax { public ExpressionSyntax Expression; }
                class ExpressionSyntax { }
                class ParenthesizedExpressionSyntax : ExpressionSyntax { }
            }

            static class Goo
            {
                static void Bar(AwaitExpressionSyntax awaitExpression)
                {

                    if (!(awaitExpression.Expression is ParenthesizedExpressionSyntax parenthesizedExpression))
                        return;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45661")]
    public async Task DoNotQualifyWhenInliningIntoPattern_02()
    {
        await TestInRegularAndScriptAsync("""
            using Syntax;

            namespace Syntax
            {
                class AwaitExpressionSyntax : ExpressionSyntax { public ExpressionSyntax Expression; }
                class ExpressionSyntax { }
                class ParenthesizedExpressionSyntax : ExpressionSyntax { }
            }

            static class Goo
            {
                static void Bar(AwaitExpressionSyntax awaitExpression)
                {
                    ExpressionSyntax [||]expression = awaitExpression.Expression;

                    if (!(expression is ParenthesizedExpressionSyntax { } parenthesizedExpression))
                        return;
                }
            }
            """,
            """
            using Syntax;

            namespace Syntax
            {
                class AwaitExpressionSyntax : ExpressionSyntax { public ExpressionSyntax Expression; }
                class ExpressionSyntax { }
                class ParenthesizedExpressionSyntax : ExpressionSyntax { }
            }

            static class Goo
            {
                static void Bar(AwaitExpressionSyntax awaitExpression)
                {

                    if (!(awaitExpression.Expression is ParenthesizedExpressionSyntax { } parenthesizedExpression))
                        return;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42835")]
    public async Task WarnWhenPossibleChangeInSemanticMeaning()
    {
        await TestInRegularAndScriptAsync("""
            class C
            {
                int P { get; set; }

                void M()
                {
                    var [||]c = new C();
                    c.P = 1;
                    var c2 = c;
                }
            }
            """,
            """
            class C
            {
                int P { get; set; }

                void M()
                {
                    {|Warning:new C().P = 1|};
                    var c2 = new C();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42835")]
    public async Task WarnWhenPossibleChangeInSemanticMeaning_IgnoreParentheses()
    {
        await TestInRegularAndScriptAsync("""
            class C
            {
                int P { get; set; }

                void M()
                {
                    var [||]c = (new C());
                    c.P = 1;
                    var c2 = c;
                }
            }
            """,
            """
            class C
            {
                int P { get; set; }

                void M()
                {
                    {|Warning:new C().P = 1|};
                    var c2 = new C();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42835")]
    public async Task WarnWhenPossibleChangeInSemanticMeaning_MethodInvocation()
    {
        await TestInRegularAndScriptAsync("""
            class C
            {
                int P { get; set; }

                void M()
                {
                    var [||]c = M2();
                    c.P = 1;
                    var c2 = c;
                }

                C M2()
                {
                    return new C();
                }
            }
            """,
            """
            class C
            {
                int P { get; set; }

                void M()
                {
                    {|Warning:M2().P = 1|};
                    var c2 = M2();
                }

                C M2()
                {
                    return new C();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42835")]
    public async Task WarnWhenPossibleChangeInSemanticMeaning_MethodInvocation2()
    {
        await TestInRegularAndScriptAsync("""
            class C
            {
                int P { get; set; }

                void M()
                {
                    var [||]c = new C();
                    c.M2();
                    var c2 = c;
                }

                void M2()
                {
                    P = 1;
                }
            }
            """,
            """
            class C
            {
                int P { get; set; }

                void M()
                {
                    {|Warning:new C().M2()|};
                    var c2 = new C();
                }

                void M2()
                {
                    P = 1;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42835")]
    public async Task WarnWhenPossibleChangeInSemanticMeaning_NestedObjectInitialization()
    {
        await TestInRegularAndScriptAsync("""
            class C
            {
                int P { get; set; }

                void M()
                {
                    var [||]c = new C[1] { new C() };
                    c[0].P = 1;
                    var c2 = c;
                }
            }
            """,
            """
            class C
            {
                int P { get; set; }

                void M()
                {
                    {|Warning:(new C[1] { new C() })[0].P = 1|};
                    var c2 = new C[1] { new C() };
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42835")]
    public async Task WarnWhenPossibleChangeInSemanticMeaning_NestedMethodCall()
    {
        await TestInRegularAndScriptAsync("""
            class C
            {
                int P { get; set; }

                void M()
                {
                    var [||]c = new C[1] { M2() };
                    c[0].P = 1;
                    var c2 = c;
                }

                C M2()
                {
                    P += 1;
                    return new C();
                }
            }
            """,
            """
            class C
            {
                int P { get; set; }

                void M()
                {
                    {|Warning:(new C[1] { M2() })[0].P = 1|};
                    var c2 = new C[1] { M2() };
                }

                C M2()
                {
                    P += 1;
                    return new C();
                }
            }
            """);
    }

    [Fact]
    public async Task InlineIntoWithExpression()
    {
        await TestInRegularAndScriptAsync("""
            record Person(string Name)
            {
                void M(Person p)
                {
                    string [||]x = "";
                    _ = p with { Name = x };
                }
            }

            namespace System.Runtime.CompilerServices
            {
                public sealed class IsExternalInit
                {
                }
            }
            """,
            """
            record Person(string Name)
            {
                void M(Person p)
                {
                    _ = p with { Name = "" };
                }
            }

            namespace System.Runtime.CompilerServices
            {
                public sealed class IsExternalInit
                {
                }
            }
            """, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44263")]
    public async Task Call_TopLevelStatement()
    {
        var code = """
            using System;

            int [||]x = 1 + 1;
            x.ToString();
            """;

        var expected = """
            using System;

            (1 + 1).ToString();
            """;

        // Global statements in regular code are local variables, so Inline Temporary works. Script code is not
        // tested because global statements in script code are field declarations, which are not considered
        // temporary.
        await TestAsync(code, expected, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44263")]
    public async Task TopLevelStatement()
    {
        var code = """
            int val = 0;
            int [||]val2 = val + 1;
            System.Console.WriteLine(val2);
            """;

        var expected = """
            int val = 0;
            System.Console.WriteLine(val + 1);
            """;

        // Global statements in regular code are local variables, so Inline Temporary works. Script code is not
        // tested because global statements in script code are field declarations, which are not considered
        // temporary.
        await TestAsync(code, expected, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44263")]
    public async Task TopLevelStatement_InScope()
    {
        await TestAsync("""
            {
                int val = 0;
                int [||]val2 = val + 1;
                System.Console.WriteLine(val2);
            }
            """,
            """
            {
                int val = 0;
                System.Console.WriteLine(val + 1);
            }
            """,
            TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
    }

    [Fact]
    public async Task TestWithLinkedFile()
    {
        await TestInRegularAndScript1Async(
            """
            <Workspace>
                <Project Language='C#' CommonReferences='true' AssemblyName='LinkedProj' Name='CSProj.1'>
                    <Document FilePath='C.cs'>
            using System.Collections.Generic;
            namespace Whatever
            {
                public class Goo
                {
                    public void Bar()
                    {
                        var target = new List&lt;object&gt;();
                        var [||]newItems = new List&lt;Goo&gt;();
                        target.AddRange(newItems);
                    }
                }
            }
                    </Document>
                </Project>
                <Project Language='C#' CommonReferences='true' AssemblyName='LinkedProj' Name='CSProj.2'>
                    <Document IsLinkFile='true' LinkProjectName='CSProj.1' LinkFilePath='C.cs'/>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language='C#' CommonReferences='true' AssemblyName='LinkedProj' Name='CSProj.1'>
                    <Document FilePath='C.cs'>
            using System.Collections.Generic;
            namespace Whatever
            {
                public class Goo
                {
                    public void Bar()
                    {
                        var target = new List&lt;object&gt;();
                        target.AddRange(new List&lt;Goo&gt;());
                    }
                }
            }
                    </Document>
                </Project>
                <Project Language='C#' CommonReferences='true' AssemblyName='LinkedProj' Name='CSProj.2'>
                    <Document IsLinkFile='true' LinkProjectName='CSProj.1' LinkFilePath='C.cs'/>
                </Project>
            </Workspace>
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50207")]
    public async Task TestImplicitObjectCreation()
    {
        var code = """
            class MyClass
            {
                void Test()
                {
                    MyClass [||]myClass = new();
                    myClass.ToString();
                }
            }
            """;

        var expected = """
            class MyClass
            {
                void Test()
                {
                    new MyClass().ToString();
                }
            }
            """;

        await TestInRegularAndScriptAsync(code, expected, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34143")]
    public async Task TestPreserveDestinationTrivia1()
    {
        var code = """
            class MyClass
            {
                void Goo(bool b)
                {
                    var [||]s = "";
                    SomeMethod(
                        s);
                }

                void SomeMethod(string _) { }
            }
            """;

        var expected = """
            class MyClass
            {
                void Goo(bool b)
                {
                    SomeMethod(
                        "");
                }

                void SomeMethod(string _) { }
            }
            """;

        await TestInRegularAndScriptAsync(code, expected);
    }
}
