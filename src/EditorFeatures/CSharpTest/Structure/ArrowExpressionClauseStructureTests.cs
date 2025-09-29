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
    public Task TestArrowExpressionClause_Method1()
        => VerifyBlockSpansAsync(
            """
            class C
            {
                {|hintspan:void M(){|textspan: $$=> expression
                    ? trueCase
                    : falseCase;|}|};
            }
            """,
            Region("textspan", "hintspan", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestArrowExpressionClause_Method2()
        => VerifyBlockSpansAsync(
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

    [Fact]
    public Task TestArrowExpressionClause_Method3()
        => VerifyBlockSpansAsync(
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

    [Fact]
    public Task TestArrowExpressionClause_Method4()
        => VerifyBlockSpansAsync(
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

    [Fact]
    public Task TestArrowExpressionClause_Method5()
        => VerifyBlockSpansAsync(
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

    [Fact]
    public Task TestArrowExpressionClause_Property1()
        => VerifyBlockSpansAsync(
            """
                class C
                {
                    {|hintspan:int M{|textspan: $$=> expression
                        ? trueCase
                        : falseCase;|}|};
                }
                """,
            Region("textspan", "hintspan", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestArrowExpressionClause_Property2()
        => VerifyBlockSpansAsync(
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

    [Fact]
    public Task TestArrowExpressionClause_Property3()
        => VerifyBlockSpansAsync(
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

    [Fact]
    public Task TestArrowExpressionClause_Property4()
        => VerifyBlockSpansAsync(
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

    [Fact]
    public Task TestArrowExpressionClause_Property5()
        => VerifyBlockSpansAsync(
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

    [Fact]
    public Task TestArrowExpressionClause_LocalFunction()
        => VerifyBlockSpansAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76820")]
    public Task TestArrowExpressionClause_DirectiveOutsideOfArrow()
        => VerifyBlockSpansAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76820")]
    public Task TestArrowExpressionClause_DirectiveInsideOfArrow()
        => VerifyBlockSpansAsync(
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
