// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.UseCoalesceExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UseCoalesceExpression;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseCoalesceExpression;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)]
public sealed class UseCoalesceExpressionForTernaryConditionalCheckTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public UseCoalesceExpressionForTernaryConditionalCheckTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpUseCoalesceExpressionForTernaryConditionalCheckDiagnosticAnalyzer(),
            new UseCoalesceExpressionForTernaryConditionalCheckCodeFixProvider());

    [Fact]
    public Task TestOnLeft_Equals()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z = [||]x == null ? y : x;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z = x ?? y;
                }
            }
            """);

    [Fact]
    public Task TestOnLeft_NotEquals()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z = [||]x != null ? x : y;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z = x ?? y;
                }
            }
            """);

    [Fact]
    public Task TestOnRight_Equals()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z = [||]null == x ? y : x;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z = x ?? y;
                }
            }
            """);

    [Fact]
    public Task TestOnRight_NotEquals()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z = [||]null != x ? x : y;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z = x ?? y;
                }
            }
            """);

    [Fact]
    public Task TestComplexExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z = [||]x.ToString() == null ? y : x.ToString();
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z = x.ToString() ?? y;
                }
            }
            """);

    [Fact]
    public Task TestParens1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z = [||](x == null) ? y : x;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z = x ?? y;
                }
            }
            """);

    [Fact]
    public Task TestParens2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z = [||](x) == null ? y : x;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z = x ?? y;
                }
            }
            """);

    [Fact]
    public Task TestParens3()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z = [||]x == null ? y : (x);
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z = x ?? y;
                }
            }
            """);

    [Fact]
    public Task TestParens4()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z = [||]x == null ? (y) : x;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z = x ?? y;
                }
            }
            """);

    [Fact]
    public Task TestFixAll1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z1 = {|FixAllInDocument:x|} == null ? y : x;
                    var z2 = x != null ? x : y;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string x, string y)
                {
                    var z1 = x ?? y;
                    var z2 = x ?? y;
                }
            }
            """);

    [Fact]
    public Task TestFixAll2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string x, string y, string z)
                {
                    var w = {|FixAllInDocument:x|} != null ? x : y.ToString(z != null ? z : y);
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string x, string y, string z)
                {
                    var w = x ?? y.ToString(z ?? y);
                }
            }
            """);

    [Fact]
    public Task TestFixAll3()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string x, string y, string z)
                {
                    var w = {|FixAllInDocument:x|} != null ? x : y != null ? y : z;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string x, string y, string z)
                {
                    var w = x ?? y ?? z;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16025")]
    public Task TestTrivia1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                public Program()
                {
                    string x = ";

                    string y = [|x|] == null ? string.Empty : x;
                }
            }
            """,
            """
            using System;

            class Program
            {
                public Program()
                {
                    string x = ";

                    string y = x ?? string.Empty;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17028")]
    public Task TestInExpressionOfT()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Linq.Expressions;

            class C
            {
                void Main(string s, string y)
                {
                    Expression<Func<string>> e = () => [||]s != null ? s : y;
                }
            }
            """,
            """
            using System;
            using System.Linq.Expressions;

            class C
            {
                void Main(string s, string y)
                {
                    Expression<Func<string>> e = () => {|Warning:s ?? y|};
                }
            }
            """);

    [Fact]
    public Task TestUnconstrainedTypeParameter()
        => TestMissingInRegularAndScriptAsync(
            """
            class C<T>
            {
                void Main(T t)
                {
                    var v = [||]t == null ? throw new Exception() : t;
                }
            }
            """);

    [Fact]
    public Task TestStructConstrainedTypeParameter()
        => TestMissingInRegularAndScriptAsync(
            """
            class C<T> where T : struct
            {
                void Main(T t)
                {
                    var v = [||]t == null ? throw new Exception() : t;
                }
            }
            """);

    [Fact]
    public Task TestClassConstrainedTypeParameter()
        => TestInRegularAndScriptAsync(
            """
            class C<T> where T : class
            {
                void Main(T t)
                {
                    var v = [||]t == null ? throw new Exception() : t;
                }
            }
            """,
            """
            class C<T> where T : class
            {
                void Main(T t)
                {
                    var v = t ?? throw new Exception();
                }
            }
            """);

    [Fact]
    public Task TestNotOnNullable()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void Main(int? t)
                {
                    var v = [||]t == null ? throw new Exception() : t;
                }
            }
            """);

    [Fact]
    public Task TestOnArray()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void Main(int[] t)
                {
                    var v = [||]t == null ? throw new Exception() : t;
                }
            }
            """,
            """
            class C
            {
                void Main(int[] t)
                {
                    var v = t ?? throw new Exception();
                }
            }
            """);

    [Fact]
    public Task TestOnInterface()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void Main(System.ICloneable t)
                {
                    var v = [||]t == null ? throw new Exception() : t;
                }
            }
            """,
            """
            class C
            {
                void Main(System.ICloneable t)
                {
                    var v = t ?? throw new Exception();
                }
            }
            """);

    [Fact]
    public Task TestOnDynamic()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void Main(dynamic t)
                {
                    var v = [||]t == null ? throw new Exception() : t;
                }
            }
            """,
            """
            class C
            {
                void Main(dynamic t)
                {
                    var v = t ?? throw new Exception();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38066")]
    public Task TestSemicolonPlacement()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string s)
                {
                    _ = [||]s == null
                        ? ""
                        : s;
                }
            }
            """,
            """
            class C
            {
                void M(string s)
                {
                    _ = s ?? "";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38066")]
    public Task TestParenthesisPlacement()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string s)
                {
                    M([||]s == null
                        ? ""
                        : s);
                }
            }
            """,
            """
            class C
            {
                void M(string s)
                {
                    M(s ?? "");
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38066")]
    public Task TestAnotherConditionalPlacement()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string s)
                {
                    _ = cond
                        ? [||]s == null
                        ? ""
                        : s
                        : "";
                }
            }
            """,
            """
            class C
            {
                void M(string s)
                {
                    _ = cond
                        ? s ?? ""
                        : "";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53190")]
    public Task TestNotWithTargetTyping()
        => TestMissingAsync(
            """
            class Program
            {
                class A { }
                class B { }

                static void Main(string[] args)
                {
                    var a = new A();
                    var b = new B();

                    object x = [||]a != null ? a : b;
                }
            }
            """);
}
