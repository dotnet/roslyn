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
public sealed partial class CSharpAsAndNullCheckTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
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

    private Task TestStatement(string input, string output, LanguageVersion version = LanguageVersion.CSharp8)
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMissingInCSharp6()
        => TestMissingAsync(
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

    [Fact]
    public Task TestMissingInWrongName()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestInSwitchSection()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33345")]
    public Task TestRemoveNewLinesInSwitchStatement()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMissingOnNonDeclaration()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25237")]
    public Task TestMissingOnReturnStatement()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|return;|]
                }
            }
            """);

    [Fact]
    public Task TestMissingOnIsExpression()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task InlineTypeCheckComplexExpression1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestInlineTypeCheckWithElse()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestComments1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestComments2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestComments3()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33345")]
    public Task TestRemoveNewLines()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33345")]
    public Task TestRemoveNewLinesWhereBlankLineIsNotEmpty()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33345")]
    public Task TestRemoveNewLines2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task InlineTypeCheckComplexCondition1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task InlineTypeCheckComplexCondition2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task InlineTypeCheckComplexCondition3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestDefiniteAssignment1()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestDefiniteAssignment2()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestDefiniteAssignment3()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21097")]
    public Task TestDefiniteAssignment4()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24286")]
    public Task TestDefiniteAssignment5()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestDefiniteAssignment6()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestDefiniteAssignment7()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28821")]
    public Task TestDefiniteAssignment8()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28866")]
    public Task TestWrittenExpressionBeforeNullCheck()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15957")]
    public Task TestTrivia1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17129")]
    public Task TestTrivia2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17122")]
    public Task TestMissingOnNullableType()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18053")]
    public Task TestMissingWhenTypesDoNotMatch()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMissingOnWhileNoInline()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestWhileDefiniteAssignment1()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestWhileDefiniteAssignment2()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestWhileDefiniteAssignment3()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestWhileDefiniteAssignment4()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23504")]
    public Task DoNotChangeOriginalFormatting1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23504")]
    public Task DoNotChangeOriginalFormatting2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21172")]
    public Task TestMissingWithDynamic()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21551")]
    public Task TestOverloadedUserOperator()
        => TestMissingAsync(
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

    [Fact]
    public Task TestNegativeDefiniteAssignment1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestNegativeDefiniteAssignment2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25993")]
    public Task TestEmbeddedStatement1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25993")]
    public Task TestEmbeddedStatement2()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestUseBeforeDeclaration()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestPossiblyUnassigned()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOutOfScope()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestDeclarationOnOuterBlock()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestConditionalExpression()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestConditionalExpression_OppositeBranch()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestForStatement_NoInlineTypeCheck()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestForStatement_InlineTypeCheck()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestForStatement_InScope()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestForStatement_NotAssignedBeforeAccess()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestForStatement_AssignedBeforeAccess()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestForStatement_MultipleDeclarators()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestForStatement_UseBeforeDeclaration()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestForStatement_Initializer()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestLocalFunction()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestLocalFunction_UseOutOfScope()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestExpressionLambda()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestExpressionLambda_UseOutOfScope()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31388")]
    public Task TestUseBetweenAssignmentAndIfCondition()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40007")]
    public Task TestSpaceAfterGenericType()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45596")]
    public Task TestMissingInUsingDeclaration()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45596")]
    public Task TestMissingInUsingStatement()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37398")]
    public Task TestPrecedingDirectiveTrivia()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40006")]
    public Task TestArrayOfNullables()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55782")]
    public Task TestLocalReferencedAcrossScopes1()
        => TestMissingInRegularAndScriptAsync("""
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55782")]
    public Task TestLocalReferencedAcrossScopes2()
        => TestInRegularAndScriptAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37875")]
    public Task TestNullableWhenWrittenTo1()
        => TestInRegularAndScriptAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37875")]
    public Task TestNullableWhenWrittenTo2()
        => TestInRegularAndScriptAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37875")]
    public Task TestNullableWhenWrittenTo3()
        => TestMissingInRegularAndScriptAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37875")]
    public Task TestNullableWhenWrittenTo4()
        => TestMissingInRegularAndScriptAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39600")]
    public Task TestNotWithInterveningMutation()
        => TestMissingInRegularAndScriptAsync("""
            using System;

            class Program
            {
                void Goo(object[] values, int index)
                {
                    [|var|] v1 = values[index++] as string;
                    index++;

                    if (v1 != null)
                    {
                        Console.WriteLine(v1);
                    }
                }
            }
            """);
}
