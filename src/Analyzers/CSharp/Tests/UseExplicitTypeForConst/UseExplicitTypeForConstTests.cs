// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.UseExplicitTypeForConst;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExplicitTypeForConst;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTypeForConst)]
public sealed class UseExplicitTypeForConstTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public UseExplicitTypeForConstTests(ITestOutputHelper logger)
        : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new UseExplicitTypeForConstCodeFixProvider());

    [Fact]
    public Task TestWithIntLiteral()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    const [|var|] v = 0;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    const int v = 0;
                }
            }
            """);

    [Fact]
    public Task TestWithStringConstant()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                const string s = null;
                void M()
                {
                    const [|var|] v = s;
                }
            }
            """,
            """
            class C
            {
                const string s = null;
                void M()
                {
                    const string v = s;
                }
            }
            """);

    [Fact]
    public Task TestWithQualifiedType()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    const [|var|] v = default(System.Action);
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    const System.Action v = default(System.Action);
                }
            }
            """);

    [Fact]
    public Task TestWithNonConstantInitializer()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    const [|var|] v = System.Console.ReadLine();
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    const string v = System.Console.ReadLine();
                }
            }
            """);

    [Fact]
    public Task TestWithNonConstantTupleType()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    const [|var|] v = (0, true);
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    const (int, bool) v = (0, true);
                }
            }
            """);

    [Fact]
    public Task TestNotWithNullLiteral()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    const [|var|] v = null;
                }
            }
            """);

    [Fact]
    public Task TestNotWithDefaultLiteral()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    const [|var|] v = default;
                }
            }
            """);

    [Fact]
    public Task TestWithLambda()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    const [|var|] v = () => { };
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    const System.Action v = () => { };
                }
            }
            """);

    [Fact]
    public Task TestNotWithAnonymousType()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    const [|var|] v = new { a = 0 };
                }
            }
            """);

    [Fact]
    public Task TestNotWithArrayInitializer()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    const [|var|] v = { 0, 1 };
                }
            }
            """);

    [Fact]
    public Task TestNotWithMissingInitializer()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    const [|var|] v =
                }
            }
            """);

    [Fact]
    public Task TestNotWithClassVar()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                class var { }
                void M()
                {
                    const [|var|] v = 0;
                }
            }
            """);

    [Fact]
    public Task TestNotOnField()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                const [|var|] v = 0;
            }
            """);

    [Fact]
    public Task TestNotWithMultipleDeclarators()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    const [|var|] a = 0, b = 0;
                }
            }
            """);
}
