// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure;

[Trait(Traits.Feature, Traits.Features.Outlining)]
public sealed class ArrowExpressionClauseStructureTests : AbstractCSharpSyntaxNodeStructureTests<ArrowExpressionClauseSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider()
        => new ArrowExpressionClauseStructureProvider();

    [Fact]
    public async Task TestArrowExpressionClause_Method1()
    {
        await VerifyBlockSpansAsync(
            """
            class C
            {
                {|hintspan:void M(){|textspan: $$=> expression
                    ? trueCase
                    : falseCase;|}|};
            }
            """,
            Region("textspan", "hintspan", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestArrowExpressionClause_Method2()
    {
        await VerifyBlockSpansAsync(
            """
                class C
                {
                    {|hintspan:void M(){|textspan: $$=> expression
                        ? trueCase
                        : falseCase;|}|}
                    void N() => 0;
                }
                """,
            Region("textspan", "hintspan", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestArrowExpressionClause_Method3()
    {
        await VerifyBlockSpansAsync(
            """
                class C
                {
                    {|hintspan:void M(){|textspan: $$=> expression
                        ? trueCase
                        : falseCase;|}|}

                    void N() => 0;
                }
                """,
            Region("textspan", "hintspan", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestArrowExpressionClause_Method4()
    {
        await VerifyBlockSpansAsync(
            """
                class C
                {
                    {|hintspan:void M(){|textspan: $$=> expression
                        ? trueCase
                        : falseCase;|}|}
                    int N => 0;
                }
                """,
            Region("textspan", "hintspan", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestArrowExpressionClause_Method5()
    {
        await VerifyBlockSpansAsync(
            """
                class C
                {
                    {|hintspan:void M(){|textspan: $$=> expression
                        ? trueCase
                        : falseCase;|}|}

                    int N => 0;
                }
                """,
            Region("textspan", "hintspan", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestArrowExpressionClause_Property1()
    {
        await VerifyBlockSpansAsync(
            """
                class C
                {
                    {|hintspan:int M{|textspan: $$=> expression
                        ? trueCase
                        : falseCase;|}|};
                }
                """,
            Region("textspan", "hintspan", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestArrowExpressionClause_Property2()
    {
        await VerifyBlockSpansAsync(
            """
                class C
                {
                    {|hintspan:int M{|textspan: $$=> expression
                        ? trueCase
                        : falseCase;|}|}
                    int N => 0;
                }
                """,
            Region("textspan", "hintspan", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestArrowExpressionClause_Property3()
    {
        await VerifyBlockSpansAsync(
            """
                class C
                {
                    {|hintspan:int M{|textspan: $$=> expression
                        ? trueCase
                        : falseCase;|}|}

                    int N => 0;
                }
                """,
            Region("textspan", "hintspan", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestArrowExpressionClause_Property4()
    {
        await VerifyBlockSpansAsync(
            """
                class C
                {
                    {|hintspan:int M{|textspan: $$=> expression
                        ? trueCase
                        : falseCase;|}|}
                    int N() => 0;
                }
                """,
            Region("textspan", "hintspan", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestArrowExpressionClause_Property5()
    {
        await VerifyBlockSpansAsync(
            """
                class C
                {
                    {|hintspan:int M{|textspan: $$=> expression
                        ? trueCase
                        : falseCase;|}|}

                    int N() => 0;
                }
                """,
            Region("textspan", "hintspan", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestArrowExpressionClause_LocalFunction()
    {
        await VerifyBlockSpansAsync(
            """
                class C
                {
                    void M()
                    {
                        {|hintspan:void F(){|textspan: $$=> expression
                            ? trueCase
                            : falseCase;|}|};
                    }
                }
                """,
            Region("textspan", "hintspan", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76820")]
    public async Task TestArrowExpressionClause_DirectiveOutsideOfArrow()
    {
        await VerifyBlockSpansAsync(
            """
            class C
            {
            #if true
                {|hintspan:int M(){|textspan: $$=>
                    0;|}|};
            #else
                int M() =>
                    1;
            #endif
            }
            """,
            Region("textspan", "hintspan", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76820")]
    public async Task TestArrowExpressionClause_DirectiveInsideOfArrow()
    {
        await VerifyBlockSpansAsync(
            """
            class C
            {
                {|hintspan:int M(){|textspan: $$=>
            #if true
                    0;
            #else
                    1;
            #endif|}|}
            }
            """,
            Region("textspan", "hintspan", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }
}
