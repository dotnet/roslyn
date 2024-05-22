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
public class UseCoalesceExpressionForTernaryConditionalCheckTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public UseCoalesceExpressionForTernaryConditionalCheckTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpUseCoalesceExpressionForTernaryConditionalCheckDiagnosticAnalyzer(),
            new UseCoalesceExpressionForTernaryConditionalCheckCodeFixProvider());

    [Fact]
    public async Task TestOnLeft_Equals()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestOnLeft_NotEquals()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestOnRight_Equals()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestOnRight_NotEquals()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestComplexExpression()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestParens1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestParens2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestParens3()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestParens4()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestFixAll1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestFixAll2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestFixAll3()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16025")]
    public async Task TestTrivia1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17028")]
    public async Task TestInExpressionOfT()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestUnconstrainedTypeParameter()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C<T>
            {
                void Main(T t)
                {
                    var v = [||]t == null ? throw new Exception() : t;
                }
            }
            """);
    }

    [Fact]
    public async Task TestStructConstrainedTypeParameter()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C<T> where T : struct
            {
                void Main(T t)
                {
                    var v = [||]t == null ? throw new Exception() : t;
                }
            }
            """);
    }

    [Fact]
    public async Task TestClassConstrainedTypeParameter()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestNotOnNullable()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void Main(int? t)
                {
                    var v = [||]t == null ? throw new Exception() : t;
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnArray()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestOnInterface()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestOnDynamic()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38066")]
    public async Task TestSemicolonPlacement()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38066")]
    public async Task TestParenthesisPlacement()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38066")]
    public async Task TestAnotherConditionalPlacement()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53190")]
    public async Task TestNotWithTargetTyping()
    {
        await TestMissingAsync(
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
}
