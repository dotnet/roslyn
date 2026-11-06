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
        => await TestInRegularAndScriptAsync(GetTreeText(initial), GetTreeText(expected));

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
    public Task NotOnField()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task WithRefInitializer1()
        => TestMissingInRegularAndScriptAsync(
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
    public Task Escaping1()
        => TestFixOneAsync(
            """
            { int [||]x = 0;

            Console.WriteLine(x); }
            """,
            """
            {
                    Console.WriteLine(0); }
            """);

    [Fact]
    public Task Escaping2()
        => TestFixOneAsync(
            """
            { int [||]@x = 0;

            Console.WriteLine(x); }
            """,
            """
            {
                    Console.WriteLine(0); }
            """);

    [Fact]
    public Task Escaping3()
        => TestFixOneAsync(
            """
            { int [||]@x = 0;

            Console.WriteLine(@x); }
            """,
            """
            {
                    Console.WriteLine(0); }
            """);

    [Fact]
    public Task Escaping4()
        => TestFixOneAsync(
            """
            { int [||]x = 0;

            Console.WriteLine(@x); }
            """,
            """
            {
                    Console.WriteLine(0); }
            """);

    [Fact]
    public Task Escaping5()
        => TestInRegularAndScriptAsync("""
            using System.Linq;
            class C
            {
                static void Main()
                {
                    var @where[||] = 0;
                    var q = from e in "" let a = new { @where } select a;
                }
            }
            """, """
            using System.Linq;
            class C
            {
                static void Main()
                {
                    var q = from e in "" let a = new { @where = 0 } select a;
                }
            }
            """);

    [Fact]
    public Task Call()
        => TestInRegularAndScriptAsync("""
            using System;
            class C 
            {
                public void M()
                {
                    int [||]x = 1 + 1;
                    x.ToString();
                }
            }
            """, """
            using System;
            class C 
            {
                public void M()
                {
                    (1 + 1).ToString();
                }
            }
            """);

    [Fact]
    public Task Conversion_NoChange()
        => TestInRegularAndScriptAsync("""
            using System;
            class C 
            {
                public void M()
                {
                    double [||]x = 3;
                    x.ToString();
                }
            }
            """, """
            using System;
            class C 
            {
                public void M()
                {
                    ((double)3).ToString();
                }
            }
            """);

    [Fact]
    public Task Conversion_NoConversion()
        => TestAsync(
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
            new(CSharpParseOptions.Default));

    [Fact]
    public Task Conversion_DifferentOverload()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task Conversion_DifferentMethod()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task Conversion_SameMethod()
        => TestInRegularAndScriptAsync(
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

    [Theory]
    [InlineData(LanguageVersion.CSharp8)]
    [InlineData(LanguageVersion.CSharp9)]
    public Task Conversion_NonTargetTypedConditionalExpression(LanguageVersion languageVersion)
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(languageVersion)));

    [Theory]
    [InlineData(LanguageVersion.CSharp8, " (int?)42")]
    [InlineData(LanguageVersion.CSharp9, " 42")] // In C# 9, target-typed conditionals makes this work
    public Task Conversion_TargetTypedConditionalExpression(LanguageVersion languageVersion, string expectedSubstitution)
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(languageVersion)));

    [Fact]
    public Task NoCastOnVar()
        => TestFixOneAsync(
            """
            { var [||]x = 0;

            Console.WriteLine(x); }
            """,
            """
            {
                    Console.WriteLine(0); }
            """);

    [Fact]
    public Task DoubleAssignment()
        => TestMissingAsync("""
            class C
            {
                void M()
                {
                    int [||]x = x = 1;
                    int y = x;
                }
            }
            """);

    [Fact]
    public Task TestAnonymousType1()
        => TestFixOneAsync(
            """
            { int [||]x = 42;
            var a = new { x }; }
            """,
            @"{ var a = new { x = 42 }; }");

    [Fact]
    public Task TestParenthesizedAtReference_Case3()
        => TestFixOneAsync(
            """
            { int [||]x = 1 + 1;
            int y = x * 2; }
            """,
            @"{ int y = (1 + 1) * 2; }");

    [Fact]
    public Task DoNotBreakOverloadResolution_Case5()
        => TestInRegularAndScriptAsync("""
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
            """, """
            class C
            {
                void Goo(object o) { }
                void Goo(int i) { }

                void M()
                {
                    Goo((object)(1 + 1));
                }
            }
            """);

    [Fact]
    public Task DoNotTouchUnrelatedBlocks()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    int [||]x = 1;
                    { Unrelated(); }
                    Goo(x);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    { Unrelated(); }
                    Goo(1);
                }
            }
            """);

    [Fact]
    public Task TestLambdaParenthesizeAndCast_Case7()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    System.Func<int> [||]x = () => 1;
                    int y = x();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    int y = ((System.Func<int>)(() => 1))();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538094")]
    public Task ParseAmbiguity1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538094"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541462")]
    public Task ParseAmbiguity2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538094")]
    public Task ParseAmbiguity3()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544924")]
    public Task ParseAmbiguity4()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544613")]
    public Task ParseAmbiguity5()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538131")]
    public Task TestArrayInitializer()
        => TestFixOneAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545657")]
    public Task TestArrayInitializer2()
        => TestInRegularAndScriptAsync("""
            class Program
            { 
                static void Main()
                {
                    int[] [||]x = { 3, 4, 5 };
                    System.Array a = x;
                }
            }
            """, """
            class Program
            { 
                static void Main()
                {
                    System.Array a = new int[] { 3, 4, 5 };
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545657")]
    public Task TestArrayInitializer3()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestConflict_RefParameter1()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestConflict_RefParameter2()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestConflict_AssignExpression()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestConflict_AddAssignExpression1()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestConflict_AddAssignExpression2()
        => TestMissingAsync("""
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
            """);

    [Fact]
    public Task TestConflict_SubtractAssignExpression()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestConflict_MultiplyAssignExpression()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestConflict_DivideAssignExpression()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestConflict_ModuloAssignExpression()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestConflict_AndAssignExpression()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestConflict_OrAssignExpression()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestConflict_ExclusiveOrAssignExpression()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestConflict_LeftShiftAssignExpression()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestConflict_RightShiftAssignExpression()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestConflict_PostIncrementExpression()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestConflict_PreIncrementExpression()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestConflict_PostDecrementExpression()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestConflict_PreDecrementExpression()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestConflict_AddressOfExpression()
        => TestInRegularAndScriptAsync("""
            class C
            {
                unsafe void M()
                {
                    int x = 0;
                    var y[||] = &x;
                    var z = &y;
                }
            }
            """, """
            class C
            {
                unsafe void M()
                {
                    int x = 0;
                    var y = &x;
                    var z = &{|Conflict:y|};
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545342")]
    public Task TestConflict_UsedBeforeDeclaration()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    var x = y;
                    var y[||] = 45;
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    var x = {|Conflict:y|};
                    var y = 45;
                }
            }
            """);

    [Fact]
    public Task Preprocessor1()
        => TestFixOneAsync("""
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

    [Fact]
    public Task Preprocessor2()
        => TestFixOneAsync("""
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

    [Fact]
    public Task Preprocessor3()
        => TestFixOneAsync("""
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540164")]
    public Task TriviaOnArrayInitializer()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    int[] [||]a = /**/{ 1 };
                    Goo(a);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    Goo(new int[]/**/{ 1 });
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540156")]
    public Task ProperlyFormatWhenRemovingDeclarator1()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    int [||]i = 1, j = 2, k = 3;
                    System.Console.Write(i);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    int j = 2, k = 3;
                    System.Console.Write(1);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540156")]
    public Task ProperlyFormatWhenRemovingDeclarator2()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    int i = 1, [||]j = 2, k = 3;
                    System.Console.Write(j);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    int i = 1, k = 3;
                    System.Console.Write(2);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540156")]
    public Task ProperlyFormatWhenRemovingDeclarator3()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    int i = 1, j = 2, [||]k = 3;
                    System.Console.Write(k);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    int i = 1, j = 2;
                    System.Console.Write(3);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540186")]
    public Task ProperlyFormatAnonymousTypeMember()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    var [||]x = 123;
                    var y = new { x };
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var y = new { x = 123 };
                }
            }
            """);

    [Fact, WorkItem(6356, "DevDiv_Projects/Roslyn")]
    public Task InlineToAnonymousTypeProperty()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    var [||]x = 123;
                    var y = new { x = x };
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var y = new { x = 123 };
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528075")]
    public Task InlineIntoDelegateInvocation()
        => TestInRegularAndScriptAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Action<string[]> [||]del = Main;
                    del(null);
                }
            }
            """, """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    ((Action<string[]>)Main)(null);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541341")]
    public Task InlineAnonymousMethodIntoNullCoalescingExpression()
        => TestInRegularAndScriptAsync("""
            using System;

            class Program
            {
                static void Main()
                {
                    Action [||]x = delegate { };
                    Action y = x ?? null;
                }
            }
            """, """
            using System;

            class Program
            {
                static void Main()
                {
                    Action y = (Action)delegate { } ?? null;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541341")]
    public Task InlineLambdaIntoNullCoalescingExpression()
        => TestInRegularAndScriptAsync("""
            using System;

            class Program
            {
                static void Main()
                {
                    Action [||]x = () => { };
                    Action y = x ?? null;
                }
            }
            """, """
            using System;

            class Program
            {
                static void Main()
                {
                    Action y = (Action)(() => { }) ?? null;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538079")]
    public Task InsertCastForBoxingOperation1()
        => TestInRegularAndScriptAsync("""
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
            """, """
            using System;
            class A
            {
                static void Main()
                {
                    object z = (long)1;
                    Console.WriteLine((long)z);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538079")]
    public Task InsertCastForBoxingOperation2()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538079")]
    public Task InsertCastForBoxingOperation3()
        => TestInRegularAndScriptAsync("""
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
            """, """
            using System;
            class A
            {
                static void Main()
                {
                    object z = (byte)1;
                    Console.WriteLine((byte)z);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538079")]
    public Task InsertCastForBoxingOperation4()
        => TestInRegularAndScriptAsync("""
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
            """, """
            using System;
            class A
            {
                static void Main()
                {
                    object z = (sbyte)1;
                    Console.WriteLine((sbyte)z);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538079")]
    public Task InsertCastForBoxingOperation5()
        => TestInRegularAndScriptAsync("""
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
            """, """
            using System;
            class A
            {
                static void Main()
                {
                    object z = (short)1;
                    Console.WriteLine((short)z);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540278")]
    public Task TestLeadingTrivia()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540278")]
    public Task TestLeadingAndTrailingTrivia()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540278")]
    public Task TestTrailingTrivia()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540278")]
    public Task TestPreprocessor()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540277")]
    public Task TestFormatting()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541694")]
    public Task TestSwitchSection()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542647")]
    public Task UnparenthesizeExpressionIfNeeded1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545619")]
    public Task UnparenthesizeExpressionIfNeeded2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542656")]
    public Task ParenthesizeIfNecessary1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544626")]
    public Task ParenthesizeIfNecessary2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544415")]
    public Task ParenthesizeAddressOf1()
        => TestInRegularAndScriptAsync(
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
                var i = (Int32)(int*)&x;
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544922")]
    public Task ParenthesizeAddressOf2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544921")]
    public Task ParenthesizePointerIndirection1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544614")]
    public Task ParenthesizePointerIndirection2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544563")]
    public Task DoNotInlineStackAlloc()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543744")]
    public Task InlineTempLambdaExpressionCastingError()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task InsertCastForNull()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task InsertCastIfNeeded1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545161")]
    public Task InsertCastIfNeeded2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544612")]
    public Task InlineIntoBracketedList()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542648")]
    public Task ParenthesizeAfterCastIfNeeded()
        => TestAsync(
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
        new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544635")]
    public Task InsertCastForEnumZeroIfBoxed()
        => TestAsync(
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
        new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554010")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544636")]
    public Task InsertCastForMethodGroupIfNeeded1()
        => TestAsync(
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
        new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554010")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544978")]
    public Task InsertCastForMethodGroupIfNeeded2()
        => TestAsync(
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
        new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545103")]
    public Task DoNotInsertCastForTypeThatNoLongerBindsToTheSameType()
        => TestAsync(
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
        new(parseOptions: null));

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/56938")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545170")]
    public Task InsertCorrectCastForDelegateCreationExpression()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545523")]
    public Task DoNotInsertCastForObjectCreationIfUnneeded()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task DoNotInsertCastInForeachIfUnneeded01()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task InsertCastInForeachIfNeeded01()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task InsertCastInForeachIfNeeded02()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545601")]
    public Task InsertCastToKeepGenericMethodInference()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545601")]
    public Task InsertCastForKeepImplicitArrayInference()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545601")]
    public Task InsertASingleCastToNotBreakOverloadResolution()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545601")]
    public Task InsertASingleCastToNotBreakOverloadResolutionInLambdas()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545601")]
    public Task InsertASingleCastToNotBreakResolutionOfOperatorOverloads()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545561")]
    public Task InsertCastToNotBreakOverloadResolutionInUncheckedContext()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545564")]
    public Task InsertCastToNotBreakOverloadResolutionInUnsafeContext()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545783")]
    public Task InsertCastToNotBreakOverloadResolutionInNestedLambdas()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546069")]
    public Task TestBrokenVariableDeclarator()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestHiddenRegion1()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestHiddenRegion2()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestHiddenRegion3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestHiddenRegion4()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestHiddenRegion5()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530743")]
    public Task InlineFromLabeledStatement()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529698")]
    public Task InlineCompoundAssignmentIntoInitializer()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609497")]
    public Task Bugfix_609497()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636319")]
    public Task Bugfix_636319()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609492")]
    public Task Bugfix_609492()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529950")]
    public Task InlineTempDoesNotInsertUnnecessaryExplicitTypeInLambdaParameter()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/619425")]
    public Task Bugfix_619425_RestrictedSimpleNameExpansion()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529840")]
    public Task Bugfix_529840_DetectSemanticChangesAtInlineSite()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1091946")]
    public Task TestConditionalAccessWithConversion()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestSimpleConditionalAccess()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestConditionalAccessWithConditionalExpression()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2593")]
    public Task TestConditionalAccessWithExtensionMethodInvocation()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2593")]
    public Task TestConditionalAccessWithExtensionMethodInvocation_2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestAliasQualifiedNameIntoInterpolation()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestConditionalExpressionIntoInterpolation()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestConditionalExpressionIntoInterpolationWithFormatClause()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestInvocationExpressionIntoInterpolation()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4583")]
    public Task DoNotParenthesizeInterpolatedStringWithNoInterpolation_CSharp7()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: TestOptions.Regular7));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33108")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/4583")]
    public Task CastInterpolatedStringWhenInliningIntoInvalidCall()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33108")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/4583")]
    public Task DoNotCastInterpolatedStringWhenInliningIntoValidCall()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4583")]
    public Task DoNotParenthesizeInterpolatedStringWithInterpolation_CSharp7()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: TestOptions.Regular7));

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/33108")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/4583")]
    public Task DoNotParenthesizeInterpolatedStringWithInterpolation()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15530")]
    public Task PArenthesizeAwaitInlinedIntoReducedExtensionMethod()
        => TestInRegularAndScriptAsync(
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
    public Task InlineIntoLambdaWithReturnStatementWithNoExpression()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Theory]
    [InlineData(LanguageVersion.CSharp6)]
    [InlineData(LanguageVersion.CSharp12)]
    public Task Tuples(LanguageVersion version)
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                public void M()
                {
                    (int, string) [||]x = (1, "hello");
                    x.ToString();
                }
            }
            """,
            """
            using System;
            class C
            {
                public void M()
                {
                    (1, "hello").ToString();
                }
            }
            """,
            new TestParameters(parseOptions: TestOptions.Regular.WithLanguageVersion(version)));

    [Fact]
    public Task TuplesWithNames()
        => TestInRegularAndScriptAsync("""
            using System;
            class C
            {
                public void M()
                {
                    (int a, string b) [||]x = (a: 1, b: "hello");
                    x.ToString();
                }
            }
            """, """
            using System;
            class C
            {
                public void M()
                {
                    (a: 1, b: "hello").ToString();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11028")]
    public Task TuplesWithDifferentNames()
        => TestInRegularAndScriptAsync("""
            class C
            {
                public void M()
                {
                    (int a, string b) [||]x = (c: 1, d: "hello");
                    x.a.ToString();
                }
            }
            """, """
            class C
            {
                public void M()
                {
                    (((int a, string b))(c: 1, d: "hello")).a.ToString();
                }
            }
            """);

    [Fact]
    public Task Deconstruction()
        => TestInRegularAndScriptAsync("""
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
            """, """
            using System;
            class C
            {
                public void M()
                {
                    {|Warning:var (x1, x2) = new C()|};
                    var x3 = new C();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12802")]
    public Task Deconstruction2()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11958")]
    public Task EnsureParenthesesInStringConcatenation()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    int [||]i = 1 + 2;
                    string s = "a" + i;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    string s = "a" + (1 + 2);
                }
            }
            """);

    [Fact]
    public Task ExplicitTupleNameAdded()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    int [||]i = 1 + 2;
                    var t = (i, 3);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var t = (i: 1 + 2, 3);
                }
            }
            """);

    [Fact]
    public Task ExplicitTupleNameAdded_Trivia()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    int [||]i = 1 + 2;
                    var t = ( /*comment*/ i, 3);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var t = ( /*comment*/ i: 1 + 2, 3);
                }
            }
            """);

    [Fact]
    public Task ExplicitTupleNameAdded_Trivia2()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExplicitTupleNameAdded_NoDuplicateNames()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    int [||]i = 1 + 2;
                    var t = (i, i);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var t = (1 + 2, 1 + 2);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19047")]
    public Task ExplicitTupleNameAdded_DeconstructionDeclaration()
        => TestInRegularAndScriptAsync("""
            class C
            {
                static int y = 1;
                void M()
                {
                    int [||]i = C.y;
                    var t = ((i, (i, _)) = (1, (i, 3)));
                }
            }
            """, """
            class C
            {
                static int y = 1;
                void M()
                {
                    int i = C.y;
                    var t = (({|Conflict:i|}, ({|Conflict:i|}, _)) = (1, (i: C.y, 3)));
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19047")]
    public Task ExplicitTupleNameAdded_DeconstructionDeclaration2()
        => TestInRegularAndScriptAsync("""
            class C
            {
                static int y = 1;
                void M()
                {
                    int [||]i = C.y;
                    var t = ((i, _) = (1, 2));
                }
            }
            """, """
            class C
            {
                static int y = 1;
                void M()
                {
                    int i = C.y;
                    var t = (({|Conflict:i|}, _) = (1, 2));
                }
            }
            """);

    [Fact]
    public Task ExplicitTupleNameAdded_NoReservedNames()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    int [||]Rest = 1 + 2;
                    var t = (Rest, 3);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var t = (1 + 2, 3);
                }
            }
            """);

    [Fact]
    public Task ExplicitTupleNameAdded_NoReservedNames2()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    int [||]Item1 = 1 + 2;
                    var t = (Item1, 3);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var t = (1 + 2, 3);
                }
            }
            """);

    [Fact]
    public Task ExplicitTupleNameAdded_EscapeKeywords()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    int [||]@int = 1 + 2;
                    var t = (@int, 3);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var t = (@int: 1 + 2, 3);
                }
            }
            """);

    [Fact]
    public Task ExplicitTupleNameAdded_KeepEscapedName()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    int [||]@where = 1 + 2;
                    var t = (@where, 3);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var t = (@where: 1 + 2, 3);
                }
            }
            """);

    [Fact]
    public Task ExplicitAnonymousTypeMemberNameAdded_DuplicateNames()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    int [||]i = 1 + 2;
                    var t = new { i, i }; // error already
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var t = new { i = 1 + 2, i = 1 + 2 }; // error already
                }
            }
            """);

    [Fact]
    public Task ExplicitAnonymousTypeMemberNameAdded_AssignmentEpression()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    int j = 0;
                    int [||]i = j = 1;
                    var t = new { i, k = 3 };
                }
            }
            """, """
            class C
            {
                void M()
                {
                    int j = 0;
                    var t = new { i = j = 1, k = 3 };
                }
            }
            """);

    [Fact]
    public Task ExplicitAnonymousTypeMemberNameAdded_Comment()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    int [||]i = 1 + 2;
                    var t = new { /*comment*/ i, j = 3 };
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var t = new { /*comment*/ i = 1 + 2, j = 3 };
                }
            }
            """);

    [Fact]
    public Task ExplicitAnonymousTypeMemberNameAdded_Comment2()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19247")]
    public Task InlineTemporary_LocalFunction()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11712")]
    public Task InlineTemporary_RefParams()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11712")]
    public Task InlineTemporary_OutParams()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24791")]
    public Task InlineVariableDoesNotAddUnnecessaryCast()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16819")]
    public Task InlineVariableDoesNotAddsDuplicateCast()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30903")]
    public Task InlineVariableContainsAliasOfValueTupleType()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30903")]
    public Task InlineVariableContainsAliasOfMixedValueTupleType()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35645")]
    public Task UsingDeclaration()
        => TestMissingInRegularAndScriptAsync("""
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
            """, new TestParameters(parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp8)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task Selections1()
        => TestFixOneAsync(
"""
{ [|int x = 0;|]

Console.WriteLine(x); }
""",
"""
{
        Console.WriteLine(0); }
""");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task Selections2()
        => TestFixOneAsync(
"""
{ int [|x = 0|], y = 1;

Console.WriteLine(x); }
""",
"""
{
        int y = 1;

        Console.WriteLine(0); }
""");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task Selections3()
        => TestFixOneAsync(
"""
{ int x = 0, [|y = 1|], z = 2;

Console.WriteLine(y); }
""",
"""
{
        int x = 0, z = 2;

        Console.WriteLine(1); }
""");

    [Fact]
    public Task WarnOnInlineIntoConditional1()
        => TestFixOneAsync(
"""
{ var [|x = true|];

System.Diagnostics.Debug.Assert(x); }
""",
"""
{
        {|Warning:System.Diagnostics.Debug.Assert(true)|}; }
""");

    [Fact]
    public Task WarnOnInlineIntoConditional2()
        => TestFixOneAsync(
"""
{ var [|x = true|];

System.Diagnostics.Debug.Assert(x == true); }
""",
"""
{
        {|Warning:System.Diagnostics.Debug.Assert(true == true)|}; }
""");

    [Fact]
    public Task WarnOnInlineIntoMultipleConditionalLocations()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task OnlyWarnOnConditionalLocations()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40201")]
    public Task TestUnaryNegationOfDeclarationPattern()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18322")]
    public Task TestInlineIntoExtensionMethodInvokedOnThis()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8716")]
    public Task DoNotQualifyInlinedLocalFunction()
        => TestInRegularAndScriptAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22540")]
    public Task DoNotQualifyWhenInliningIntoPattern_01()
        => TestInRegularAndScriptAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45661")]
    public Task DoNotQualifyWhenInliningIntoPattern_02()
        => TestInRegularAndScriptAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42835")]
    public Task WarnWhenPossibleChangeInSemanticMeaning()
        => TestInRegularAndScriptAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42835")]
    public Task WarnWhenPossibleChangeInSemanticMeaning_IgnoreParentheses()
        => TestInRegularAndScriptAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42835")]
    public Task WarnWhenPossibleChangeInSemanticMeaning_MethodInvocation()
        => TestInRegularAndScriptAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42835")]
    public Task WarnWhenPossibleChangeInSemanticMeaning_MethodInvocation2()
        => TestInRegularAndScriptAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42835")]
    public Task WarnWhenPossibleChangeInSemanticMeaning_NestedObjectInitialization()
        => TestInRegularAndScriptAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42835")]
    public Task WarnWhenPossibleChangeInSemanticMeaning_NestedMethodCall()
        => TestInRegularAndScriptAsync("""
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

    [Fact]
    public Task InlineIntoWithExpression()
        => TestInRegularAndScriptAsync("""
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
            """, new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44263")]
    public Task Call_TopLevelStatement()
        => TestAsync("""
            using System;

            int [||]x = 1 + 1;
            x.ToString();
            """, """
            using System;

            (1 + 1).ToString();
            """, new(TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44263")]
    public Task TopLevelStatement()
        => TestAsync("""
            int val = 0;
            int [||]val2 = val + 1;
            System.Console.WriteLine(val2);
            """, """
            int val = 0;
            System.Console.WriteLine(val + 1);
            """, new(TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44263")]
    public Task TopLevelStatement_InScope()
        => TestAsync("""
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
            new(TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9)));

    [Fact]
    public Task TestWithLinkedFile()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50207")]
    public Task TestImplicitObjectCreation()
        => TestInRegularAndScriptAsync("""
            class MyClass
            {
                void Test()
                {
                    MyClass [||]myClass = new();
                    myClass.ToString();
                }
            }
            """, """
            class MyClass
            {
                void Test()
                {
                    new MyClass().ToString();
                }
            }
            """, new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34143")]
    public Task TestPreserveDestinationTrivia1()
        => TestInRegularAndScriptAsync("""
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
            """, """
            class MyClass
            {
                void Goo(bool b)
                {
                    SomeMethod(
                        "");
                }

                void SomeMethod(string _) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60552")]
    public Task TestNullable1()
        => TestInRegularAndScriptAsync("""
            #nullable enable

            public class C
            {
                private struct S
                {
                }

                public string M()
                {
                    S s;
                    var [||]a = "" + s; // "Inline temporary variable" for a
                    return a;
                }
            }
            """, """
            #nullable enable

            public class C
            {
                private struct S
                {
                }

                public string M()
                {
                    S s;
                    // "Inline temporary variable" for a
                    return "" + s;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69869")]
    public Task InlineTemporaryNoNeededVariable()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class A
            {
                void M(string[] args)
                {
                    var [||]a = Math.Round(1.1D);
                    var b = a;
                }
            }
            """,
            """
            using System;
            class A
            {
                void M(string[] args)
                {
                    var b = Math.Round(1.1D);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73148")]
    public Task InlineCollectionIntoSpread()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class A
            {
                void M(string[] args)
                {
                    string[] [||]eStaticSymbols = [
                        "System.Int32 E.StaticProperty { get; }",
                        "event System.Action E.StaticEvent",
                        .. args];

                    string[] allStaticSymbols = [
                        .. eStaticSymbols,
                        "System.Int32 E2.StaticProperty { get; }"];
                }
            }
            """,
            """
            using System;
            class A
            {
                void M(string[] args)
                {

                    string[] allStaticSymbols = [
                        "System.Int32 E.StaticProperty { get; }",
                        "event System.Action E.StaticEvent",
                        .. args,
                        "System.Int32 E2.StaticProperty { get; }"];
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80081")]
    public Task InlineCollectionIntoCast()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Command: List<int>;

            var commands = new[] {1,2,3,4,5,6,7,8 }
                .Chunk(3)
                .Select(chunk => {
                    Command [||]command = [..chunk];
                    return command;
                 })
                 .ToList();
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            
            class Command: List<int>;
            
            var commands = new[] {1,2,3,4,5,6,7,8 }
                .Chunk(3)
                .Select(chunk => {
                    return (Command)([..chunk]);
                 })
                 .ToList();
            """, new(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest)));
}
