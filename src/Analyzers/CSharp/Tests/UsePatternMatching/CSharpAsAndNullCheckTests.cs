// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UsePatternMatching;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UsePatternMatching;

[Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
public partial class CSharpAsAndNullCheckTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public CSharpAsAndNullCheckTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpAsAndNullCheckDiagnosticAnalyzer(), new CSharpAsAndNullCheckCodeFixProvider());

    [Theory]
    [InlineData("x != null", "o is string x")]
    [InlineData("null != x", "o is string x")]
    [InlineData("(object)x != null", "o is string x")]
    [InlineData("null != (object)x", "o is string x")]
    [InlineData("x is object", "o is string x")]
    [InlineData("x == null", "!(o is string x)")]
    [InlineData("null == x", "!(o is string x)")]
    [InlineData("(object)x == null", "!(o is string x)")]
    [InlineData("null == (object)x", "!(o is string x)")]
    [InlineData("x is null", "!(o is string x)")]
    [InlineData("(x = o as string) != null", "o is string x")]
    [InlineData("null != (x = o as string)", "o is string x")]
    [InlineData("(x = o as string) is object", "o is string x")]
    [InlineData("(x = o as string) == null", "!(o is string x)")]
    [InlineData("null == (x = o as string)", "!(o is string x)")]
    [InlineData("(x = o as string) is null", "!(o is string x)")]
    [InlineData("x == null", "o is not string x", LanguageVersion.CSharp9)]
    public async Task InlineTypeCheck1(string input, string output, LanguageVersion version = LanguageVersion.CSharp8)
    {
        await TestStatement($"if ({input}) {{ }}", $"if ({output}) {{ }}", version);
        await TestStatement($"var y = {input};", $"var y = {output};", version);
        await TestStatement($"return {input};", $"return {output};", version);
    }

    [Theory]
    [InlineData("(x = o as string) != null", "o is string x")]
    [InlineData("null != (x = o as string)", "o is string x")]
    [InlineData("(x = o as string) is object", "o is string x")]
    [InlineData("(x = o as string) == null", "!(o is string x)")]
    [InlineData("null == (x = o as string)", "!(o is string x)")]
    public async Task InlineTypeCheck2(string input, string output)
        => await TestStatement($"while ({input}) {{ }}", $"while ({output}) {{ }}");

    private async Task TestStatement(string input, string output, LanguageVersion version = LanguageVersion.CSharp8)
    {
        await TestInRegularAndScript1Async(
            $$"""
            class C
            {
                void M(object o)
                {
                    [|var|] x = o as string;
                    {{input}}
                }
            }
            """,
            $$"""
            class C
            {
                void M(object o)
                {
                    {{output}}
                }
            }
            """, new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(version)));
    }

    [Fact]
    public async Task TestMissingInCSharp6()
    {
        await TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    [|var|] x = o as string;
                    if (x != null)
                    {
                    }
                }
            }
            """, new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));
    }

    [Fact]
    public async Task TestMissingInWrongName()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|var|] y = o as string;
                    if (x != null)
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestInSwitchSection()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    switch (o)
                    {
                        default:
                            [|var|] x = o as string;
                            if (x != null)
                            {
                            }
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    switch (o)
                    {
                        default:
                            if (o is string x)
                            {
                            }
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33345")]
    public async Task TestRemoveNewLinesInSwitchStatement()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    switch (o)
                    {
                        default:
                            [|var|] x = o as string;

                            //a comment
                            if (x != null)
                            {
                            }
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    switch (o)
                    {
                        default:
                            //a comment
                            if (o is string x)
                            {
                            }
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMissingOnNonDeclaration()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|y|] = o as string;
                    if (x != null)
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25237")]
    public async Task TestMissingOnReturnStatement()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|return;|]
                }
            }
            """);
    }

    [Fact]
    public async Task TestMissingOnIsExpression()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|var|] x = o is string;
                    if (x != null)
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task InlineTypeCheckComplexExpression1()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    [|var|] x = (o ? z : w) as string;
                    if (x != null)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if ((o ? z : w) is string x)
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestInlineTypeCheckWithElse()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    [|var|] x = o as string;
                    if (null != x)
                    {
                    }
                    else
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if (o is string x)
                    {
                    }
                    else
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestComments1()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    // prefix comment
                    [|var|] x = o as string;
                    if (x != null)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    // prefix comment
                    if (o is string x)
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestComments2()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    [|var|] x = o as string; // suffix comment
                    if (x != null)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    // suffix comment
                    if (o is string x)
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestComments3()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    // prefix comment
                    [|var|] x = o as string; // suffix comment
                    if (x != null)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    // prefix comment
                    // suffix comment
                    if (o is string x)
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33345")]
    public async Task TestRemoveNewLines()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    [|var|] x = o as string;

                    //suffix comment
                    if (x != null)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    //suffix comment
                    if (o is string x)
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33345")]
    public async Task TestRemoveNewLinesWhereBlankLineIsNotEmpty()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    [|var|] x = o as string;

                    //suffix comment
                    if (x != null)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    //suffix comment
                    if (o is string x)
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33345")]
    public async Task TestRemoveNewLines2()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    int a = 0;
                    [|var|] x = o as string;

                    //suffix comment
                    if (x != null)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int a = 0;

                    //suffix comment
                    if (o is string x)
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task InlineTypeCheckComplexCondition1()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    [|var|] x = o as string;
                    if (x != null ? 0 : 1)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if (o is string x ? 0 : 1)
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task InlineTypeCheckComplexCondition2()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    [|var|] x = o as string;
                    if ((x != null))
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if ((o is string x))
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task InlineTypeCheckComplexCondition3()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    [|var|] x = o as string;
                    if (x != null && x.Length > 0)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if (o is string x && x.Length > 0)
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestDefiniteAssignment1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|var|] x = o as string;
                    if (x != null && x.Length > 0)
                    {
                    }
                    else if (x != null)
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestDefiniteAssignment2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|var|] x = o as string;
                    if (x != null && x.Length > 0)
                    {
                    }

                    Console.WriteLine(x);
                }
            }
            """);
    }

    [Fact]
    public async Task TestDefiniteAssignment3()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    [|var|] x = o as string;
                    if (x != null && x.Length > 0)
                    {
                    }

                    x = null;
                    Console.WriteLine(x);
                }
            }
            """,

            """
            class C
            {
                void M()
                {
                    if (o is string x && x.Length > 0)
                    {
                    }

                    x = null;
                    Console.WriteLine(x);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21097")]
    public async Task TestDefiniteAssignment4()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(object o)
                {
                    [|var|] s = o as string;
                    if (s != null)
                    {

                    }
                    else
                    {
                        if (o is int?)
                            s = null;
                        s.ToString();
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24286")]
    public async Task TestDefiniteAssignment5()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            public class Test
            {
                public void TestIt(object o1, object o2)
                {
                    [|var|] test = o1 as Test;
                    if (test != null || o2 != null)
                    {
                        var o3 = test ?? o2;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestDefiniteAssignment6()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                string Use(string x) => x;

                void M()
                {
                    [|var|] x = o as string;
                    if (x != null && x.Length > 0)
                    {
                    }

                    Console.WriteLine(x = Use(x));
                }
            }
            """);
    }

    [Fact]
    public async Task TestDefiniteAssignment7()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|var|] x = o as string;
                    if (x != null && x.Length > 0)
                    {
                    }

                    Console.WriteLine(x);
                    x = "writeAfter";
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28821")]
    public async Task TestDefiniteAssignment8()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Goo(System.Activator bar)
                {
                }

                static void Main(string[] args)
                {
                    var a = new object();
                    [|var|] b = a as System.Activator;
                    if ((b == null) && false)
                    {
                    }
                    else
                    {
                        Goo(b);
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28866")]
    public async Task TestWrittenExpressionBeforeNullCheck()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Goo
            {
                object Data { get; set; }

                void DoGoo()
                {
                    [|var|] oldData = this.Data as string;

                    Data = null;

                    if (oldData != null)
                    {
                        // Do something
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15957")]
    public async Task TestTrivia1()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M(object y)
                {
                    if (y != null)
                    {
                    }

                    [|var|] x = o as string;
                    if (x != null)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M(object y)
                {
                    if (y != null)
                    {
                    }

                    if (o is string x)
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17129")]
    public async Task TestTrivia2()
    {
        await TestInRegularAndScript1Async(
            """
            using System;
            namespace N
            {
                class Program
                {
                    public static void Main()
                    {
                        object o = null;
                        int i = 0;
                        [|var|] s = o as string;
                        if (s != null && i == 0 && i == 1 &&
                            i == 2 && i == 3 &&
                            i == 4 && i == 5)
                        {
                            Console.WriteLine();
                        }
                    }
                }
            }
            """,
            """
            using System;
            namespace N
            {
                class Program
                {
                    public static void Main()
                    {
                        object o = null;
                        int i = 0;
                        if (o is string s && i == 0 && i == 1 &&
                            i == 2 && i == 3 &&
                            i == 4 && i == 5)
                        {
                            Console.WriteLine();
                        }
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17122")]
    public async Task TestMissingOnNullableType()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;
            namespace N
            {
                class Program
                {
                    public static void Main()
                    {
                        object o = null;
                        [|var|] i = o as int?;
                        if (i != null)
                            Console.WriteLine(i);
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18053")]
    public async Task TestMissingWhenTypesDoNotMatch()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class SyntaxNode
            {
                public SyntaxNode Parent;
            }

            class BaseParameterListSyntax : SyntaxNode
            {
            }

            class ParameterSyntax : SyntaxNode
            {

            }

            public static class C
            {
                static void M(ParameterSyntax parameter)
                {
                    [|SyntaxNode|] parent = parameter.Parent as BaseParameterListSyntax;

                    if (parent != null)
                    {
                        parent = parent.Parent;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMissingOnWhileNoInline()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(object o)
                {
                    [|string|] x = o as string;
                    while (x != null)
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestWhileDefiniteAssignment1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(object o)
                {
                    [|string|] x;
                    while ((x = o as string) != null)
                    {
                    }

                    var readAfterWhile = x;
                }
            }
            """);
    }

    [Fact]
    public async Task TestWhileDefiniteAssignment2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(object o)
                {
                    [|string|] x;
                    while ((x = o as string) != null)
                    {
                    }

                    x = "writeAfterWhile";
                }
            }
            """);
    }

    [Fact]
    public async Task TestWhileDefiniteAssignment3()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(object o)
                {
                    [|string|] x;
                    x = "writeBeforeWhile";
                    while ((x = o as string) != null)
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestWhileDefiniteAssignment4()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(object o)
                {
                    [|string|] x = null;
                    var readBeforeWhile = x;
                    while ((x = o as string) != null)
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23504")]
    public async Task DoNotChangeOriginalFormatting1()
    {
        await TestInRegularAndScript1Async(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    object obj = "test";

                    [|var|] str = obj as string;
                    var title = str != null
                        ? str
                        : ";
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    object obj = "test";

                    var title = obj is string str
                        ? str
                        : ";
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23504")]
    public async Task DoNotChangeOriginalFormatting2()
    {
        await TestInRegularAndScript1Async(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    object obj = "test";

                    [|var|] str = obj as string;
                    var title = str != null ? str : ";
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    object obj = "test";

                    var title = obj is string str ? str : ";
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21172")]
    public async Task TestMissingWithDynamic()
    {
        await TestMissingAsync(
            """
            class C
            {
                void M(object o)
                {
                    [|var|] x = o as dynamic;
                    if (x != null)
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21551")]
    public async Task TestOverloadedUserOperator()
    {
        await TestMissingAsync(
            """
            class C
            {
              public static void Main()
              {
                object o = new C();
                [|var|] c = o as C;
                if (c != null)
                  System.Console.WriteLine();
              }

              public static bool operator ==(C c1, C c2) => false;
              public static bool operator !=(C c1, C c2) => false;
            }
            """);
    }

    [Fact]
    public async Task TestNegativeDefiniteAssignment1()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                string M(object o)
                {
                    [|var|] x = o as string;
                    if (x == null) return null;
                    return x;
                }
            }
            """,
            """
            class C
            {
                string M(object o)
                {
                    if (!(o is string x)) return null;
                    return x;
                }
            }
            """, parameters: new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8)));
    }

    [Fact]
    public async Task TestNegativeDefiniteAssignment2()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                string M(object o, bool b)
                {
                    [|var|] x = o as string;
                    if (((object)x == null) || b)
                    {
                        return null;
                    }
                    else
                    {
                        return x;
                    }
                }
            }
            """,
            """
            class C
            {
                string M(object o, bool b)
                {
                    if ((!(o is string x)) || b)
                    {
                        return null;
                    }
                    else
                    {
                        return x;
                    }
                }
            }
            """, parameters: new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8)));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25993")]
    public async Task TestEmbeddedStatement1()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M(object e)
                {
                    var fe = e as C;
                    [|var|] ae = e as C;
                    if (fe != null)
                    {
                        M(fe); // fe is used
                    }
                    else if (ae != null)
                    {
                        M(ae); // ae is used
                    }
                }
            }
            """,
            """
            class C
            {
                void M(object e)
                {
                    var fe = e as C;
                    if (fe != null)
                    {
                        M(fe); // fe is used
                    }
                    else if (e is C ae)
                    {
                        M(ae); // ae is used
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25993")]
    public async Task TestEmbeddedStatement2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(object e)
                {
                    var fe = e as C;
                    [|var|] ae = e as C;
                    if (fe != null)
                    {
                        M(ae); // ae is used
                    }
                    else if (ae != null)
                    {
                        M(ae); // ae is used
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestUseBeforeDeclaration()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(object e)
                {
                    [|var|] c = e as C;
                    {
                        {
                            var x1 = c;

                            if (c != null)
                            {

                            }
                        }
                    }
                }

            }
            """);
    }

    [Fact]
    public async Task TestPossiblyUnassigned()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(object e)
                {
                    [|var|] c = e as C;
                    {
                        {
                            if (c != null)
                            {

                            }

                            var x2 = c;
                        }

                        // out of scope
                        var x3 = c;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestOutOfScope()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(object e)
                {
                    [|var|] c = e as C;
                    {
                        {
                            if (c != null)
                            {

                            }
                        }

                        // out of scope
                        var x3 = c;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestDeclarationOnOuterBlock()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M(object e)
                {
                    [|var|] c = e as C;
                    {
                        {
                            if (c != null)
                            {

                            }
                        }
                    }
                }
            }
            """,
            """
            class C
            {
                void M(object e)
                {
                    {
                        {
                            if (e is C c)
                            {

                            }
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestConditionalExpression()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M(object e)
                {
                    [|var|] c = e as C;
                    M(c != null ? c : null);
                }
            }
            """,
            """
            class C
            {
                void M(object e)
                {
                    M(e is C c ? c : null);
                }
            }
            """);
    }

    [Fact]
    public async Task TestConditionalExpression_OppositeBranch()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(object e)
                {
                    [|var|] c = e as C;
                    M(c != null ? c : null, c);
                }
            }
            """);
    }

    [Fact]
    public async Task TestForStatement_NoInlineTypeCheck()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(object e)
                {
                    [|var|] c = e as C;
                    for (;(c)!=null;) {}
                }
            }
            """);
    }

    [Fact]
    public async Task TestForStatement_InlineTypeCheck()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M(object e)
                {
                    [|C|] c;
                    for (; !((c = e as C)==null);) { }
                }
            }
            """,
            """
            class C
            {
                void M(object e)
                {
                    for (; !(!(e is C c));) { }
                }
            }
            """, parameters: new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8)));
    }

    [Fact]
    public async Task TestForStatement_InScope()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M(object e)
                {
                    [|C|] c = null;
                    for (; !((c = e as C)==null);)
                    {
                        M(c);
                    }
                }
            }
            """,
            """
            class C
            {
                void M(object e)
                {
                    for (; !(!(e is C c));)
                    {
                        M(c);
                    }
                }
            }
            """, parameters: new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8)));
    }

    [Fact]
    public async Task TestForStatement_NotAssignedBeforeAccess()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(object e)
                {
                    [|C|] c = null;
                    for (; ((c = e as C)==null);)
                    {
                        if (b) c = null;
                        M(c);
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestForStatement_AssignedBeforeAccess()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M(object e, bool b)
                {
                    [|C|] c = null;
                    for (; (c = e as C)==null;)
                    {
                        if (b) c = null;
                        else c = null;
                        M(c);
                    }
                }
            }
            """,
            """
            class C
            {
                void M(object e, bool b)
                {
                    for (; !(e is C c);)
                    {
                        if (b) c = null;
                        else c = null;
                        M(c);
                    }
                }
            }
            """, parameters: new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8)));
    }

    [Fact]
    public async Task TestForStatement_MultipleDeclarators()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M(object e)
                {
                    [|C|] c = null, x = null;
                    for (; !((c = e as C)==null);)
                    {
                        M(c);
                    }
                }
            }
            """,
            """
            class C
            {
                void M(object e)
                {
                    C x = null;
                    for (; !(!(e is C c));)
                    {
                        M(c);
                    }
                }
            }
            """, parameters: new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8)));
    }

    [Fact]
    public async Task TestForStatement_UseBeforeDeclaration()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(object e)
                {
                    [|C|] c = null, x = c;
                    for (; !((c = e as C)==null);)
                    {
                        M(c);
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestForStatement_Initializer()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(object e)
                {
                    [|C|] c;
                    for (var i = !((c = e as C)==null); i != null; )
                    {
                        M(c);
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestLocalFunction()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M(object e)
                {
                    [||]var c = e as C;
                    C F() => c == null ? null : c;
                }
            }
            """,
            """
            class C
            {
                void M(object e)
                {
                    C F() => !(e is C c) ? null : c;
                }
            }
            """, parameters: new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8)));
    }

    [Fact]
    public async Task TestLocalFunction_UseOutOfScope()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                C M(object e)
                {
                    [||]var c = e as C;
                    C F() => c == null ? null : c;
                    return c;
                }
            }
            """);
    }

    [Fact]
    public async Task TestExpressionLambda()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M(object e)
                {
                    [||]var c = e as C;
                    System.Func<C> f = () => c == null ? null : c;
                }
            }
            """,
            """
            class C
            {
                void M(object e)
                {
                    System.Func<C> f = () => !(e is C c) ? null : c;
                }
            }
            """, parameters: new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8)));
    }

    [Fact]
    public async Task TestExpressionLambda_UseOutOfScope()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                C M(object e)
                {
                    [||]var c = e as C;
                    System.Func<C> f = () => c == null ? null : c;
                    return c;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31388")]
    public async Task TestUseBetweenAssignmentAndIfCondition()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(object o)
                {
                    [|var|] c = o as C;
                    M2(c != null);
                    if (c == null)
                    {
                        return;
                    }
                }

                void M2(bool b) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40007")]
    public async Task TestSpaceAfterGenericType()
    {
        await TestInRegularAndScript1Async(
            """
            #nullable enable

            using System.Collections.Generic;

            class Program
            {
                static void Goo<TKey, TValue>(object items)
                {
                    [|var|] itemsAsDictionary = items as IDictionary<TKey, TValue>;
                    SortedDictionary<TKey, TValue>? dictionary = null;
                    if (itemsAsDictionary != null)
                    {
                        dictionary = new SortedDictionary<TKey, TValue>();
                    }
                    return dictionary;
                }
            }
            """,
            """
            #nullable enable

            using System.Collections.Generic;

            class Program
            {
                static void Goo<TKey, TValue>(object items)
                {
                    SortedDictionary<TKey, TValue>? dictionary = null;
                    if (items is IDictionary<TKey, TValue> itemsAsDictionary)
                    {
                        dictionary = new SortedDictionary<TKey, TValue>();
                    }
                    return dictionary;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45596")]
    public async Task TestMissingInUsingDeclaration()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    using [|var|] x = o as IDisposable;
                    if (x != null)
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45596")]
    public async Task TestMissingInUsingStatement()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    using ([|var|] x = o as IDisposable)
                    {
                        if (x != null)
                        {
                        }
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37398")]
    public async Task TestPrecedingDirectiveTrivia()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                public static void M(object o)
                {
            #if DEBUG
                    Console.WriteLine("in debug");
            #endif

                    [|string|] s = o as string;
                    if (s != null)
                    {

                    }
                }
            }
            """,
            """
            class C
            {
                public static void M(object o)
                {
            #if DEBUG
                    Console.WriteLine("in debug");
            #endif

                    if (o is string s)
                    {

                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40006")]
    public async Task TestArrayOfNullables()
    {
        await TestInRegularAndScript1Async(
            """
            #nullable enable

            class Program
            {
                static void Set(object obj, object? item)
                {
                    [|object?[]?|] arr = obj as object[];
                    if (arr != null)
                    {
                        arr[0] = item;
                    }
                }
            }
            """,
            """
            #nullable enable

            class Program
            {
                static void Set(object obj, object? item)
                {
                    if (obj is object?[] arr)
                    {
                        arr[0] = item;
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55782")]
    public async Task TestLocalReferencedAcrossScopes1()
    {
        var code = """
            using System.Transactions;

            class BaseObject { }
            class ObjectFactory
            {
                internal static BaseObject CreateObject(int x)
                {
                    throw new NotImplementedException();
                }
            }

            struct Repro
            {
                static int Main(string[] args)
                {
                    int x = 0;
                    [|BaseObject|] obj;

                    var tso = new TransactionOptions { IsolationLevel = IsolationLevel.RepeatableRead };
                    using (var trans = new TransactionScope(TransactionScopeOption.Required, tso))
                    {
                        try
                        {
                            if ((obj = ObjectFactory.CreateObject(x) as BaseObject) == null)
                            {
                                return -1;
                            }
                            // uses of obj in the transaction
                        }
                        catch (TransactionAbortedException)
                        {
                            return -1;
                        }
                    }

                    // local used here.
                    Console.WriteLine(obj);
                    return 0;
                }
            }
            """;

        await TestMissingInRegularAndScriptAsync(code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55782")]
    public async Task TestLocalReferencedAcrossScopes2()
    {
        await TestInRegularAndScript1Async("""
            using System.Transactions;

            class BaseObject { }
            class ObjectFactory
            {
                internal static BaseObject CreateObject(int x)
                {
                    throw new NotImplementedException();
                }
            }

            struct Repro
            {
                static int Main(string[] args)
                {
                    int x = 0;
                    [|BaseObject|] obj;

                    var tso = new TransactionOptions { IsolationLevel = IsolationLevel.RepeatableRead };
                    using (var trans = new TransactionScope(TransactionScopeOption.Required, tso))
                    {
                        try
                        {
                            if ((obj = ObjectFactory.CreateObject(x) as BaseObject) == null)
                            {
                                return -1;
                            }
                            // uses of obj in the transaction
                        }
                        catch (TransactionAbortedException)
                        {
                            return -1;
                        }
                    }

                    // not used
                    return 0;
                }
            }
            """,
            """
            using System.Transactions;

            class BaseObject { }
            class ObjectFactory
            {
                internal static BaseObject CreateObject(int x)
                {
                    throw new NotImplementedException();
                }
            }

            struct Repro
            {
                static int Main(string[] args)
                {
                    int x = 0;

                    var tso = new TransactionOptions { IsolationLevel = IsolationLevel.RepeatableRead };
                    using (var trans = new TransactionScope(TransactionScopeOption.Required, tso))
                    {
                        try
                        {
                            if (ObjectFactory.CreateObject(x) is not BaseObject obj)
                            {
                                return -1;
                            }
                            // uses of obj in the transaction
                        }
                        catch (TransactionAbortedException)
                        {
                            return -1;
                        }
                    }

                    // not used
                    return 0;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37875")]
    public async Task TestNullableWhenWrittenTo1()
    {
        await TestInRegularAndScript1Async("""
            #nullable enable
            using System;

            class Program
            {
                static void Goo(object o1, object o2)
                {
                    [|string?|] s = o1 as string;
                    if (s == null)
                    {
                    }
                }
            }
            """,
            """
            #nullable enable
            using System;
            
            class Program
            {
                static void Goo(object o1, object o2)
                {
                    if (o1 is not string s)
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37875")]
    public async Task TestNullableWhenWrittenTo2()
    {
        await TestInRegularAndScript1Async("""
            #nullable enable
            using System;

            class Program
            {
                static void Goo(object o1, object o2)
                {
                    [|string?|] s = o1 as string;
                    if (s == null)
                    {
                        s = "";
                    }
                }
            }
            """,
            """
            #nullable enable
            using System;
            
            class Program
            {
                static void Goo(object o1, object o2)
                {
                    if (o1 is not string s)
                    {
                        s = "";
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37875")]
    public async Task TestNullableWhenWrittenTo3()
    {
        await TestMissingInRegularAndScriptAsync("""
            #nullable enable
            using System;

            class Program
            {
                static void Goo(object o1, object o2)
                {
                    [|string?|] s = o1 as string;
                    if (s == null)
                    {
                        s = o2 as string;
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37875")]
    public async Task TestNullableWhenWrittenTo4()
    {
        await TestMissingInRegularAndScriptAsync("""
            #nullable enable
            using System;

            class Program
            {
                static void Goo(object o1, object o2)
                {
                    [|string?|] s = o1 as string;
                    if (s == null)
                    {
                        s = null;
                    }
                }
            }
            """);
    }
}
