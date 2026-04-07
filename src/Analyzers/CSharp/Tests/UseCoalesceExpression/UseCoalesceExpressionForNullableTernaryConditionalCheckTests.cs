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
public sealed class UseCoalesceExpressionForNullableTernaryConditionalCheckTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpUseCoalesceExpressionForNullableTernaryConditionalCheckDiagnosticAnalyzer(),
            new UseCoalesceExpressionForNullableTernaryConditionalCheckCodeFixProvider());

    [Fact]
    public Task TestOnLeft_Equals()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(int? x, int? y)
                {
                    var z = [||]!x.HasValue ? y : x.Value;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(int? x, int? y)
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
                void M(int? x, int? y)
                {
                    var z = [||]x.HasValue ? x.Value : y;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(int? x, int? y)
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
                void M(int? x, int? y)
                {
                    var z = [||]!(x + y).HasValue ? y : (x + y).Value;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(int? x, int? y)
                {
                    var z = (x + y) ?? y;
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
                void M(int? x, int? y)
                {
                    var z = [||](x.HasValue) ? x.Value : y;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(int? x, int? y)
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
                void M(int? x, int? y)
                {
                    var z1 = {|FixAllInDocument:x|}.HasValue ? x.Value : y;
                    var z2 = !x.HasValue ? y : x.Value;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(int? x, int? y)
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
                void M(int? x, int? y, int? z)
                {
                    var w = {|FixAllInDocument:x|}.HasValue ? x.Value : y.ToString(z.HasValue ? z.Value : y);
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(int? x, int? y, int? z)
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
                void M(int? x, int? y, int? z)
                {
                    var w = {|FixAllInDocument:x|}.HasValue ? x.Value : y.HasValue ? y.Value : z;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(int? x, int? y, int? z)
                {
                    var w = x ?? y ?? z;
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
                void M(int? x, int? y)
                {
                    Expression<Func<int>> e = () => [||]!x.HasValue ? y : x.Value;
                }
            }
            """,
            """
            using System;
            using System.Linq.Expressions;

            class C
            {
                void M(int? x, int? y)
                {
                    Expression<Func<int>> e = () => {|Warning:x ?? y|};
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69087")]
    public Task TestNotWithTargetTyping1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(int? x)
                {
                    object z = [||]x.HasValue ? x.Value : "";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69087")]
    public Task TestWithNonTargetTyping1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(int? x)
                {
                    object z = [||]x.HasValue ? x.Value : 0;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(int? x)
                {
                    object z = x ?? 0;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69087")]
    public Task TestNotWithTargetTyping2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public int? Index { get; set; }

                public string InterpolatedText => $"{([||]Index.HasValue ? Index.Value : "???")}: rest of the text";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69087")]
    public Task TestWithNonTargetTyping2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public int? Index { get; set; }

                public string InterpolatedText => $"{([||]Index.HasValue ? Index.Value : 0)}: rest of the text";
            }
            """,
            """
            using System;

            class C
            {
                public int? Index { get; set; }

                public string InterpolatedText => $"{(Index ?? 0)}: rest of the text";
            }
            """);
}
