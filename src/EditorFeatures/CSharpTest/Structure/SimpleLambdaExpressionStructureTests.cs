// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure;

[Trait(Traits.Feature, Traits.Features.Outlining)]
public sealed class SimpleLambdaExpressionStructureTests : AbstractCSharpSyntaxNodeStructureTests<SimpleLambdaExpressionSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new SimpleLambdaExpressionStructureProvider();

    [Fact]
    public Task TestLambda()
        => VerifyBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        {|hint:$$f => {|textspan:{
                            x();
                        };|}|}
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestLambdaInForLoop()
        => VerifyNoBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        for (Action a = x$$ => { }; true; a()) { }
                    }
                }
                """);

    [Fact]
    public Task TestLambdaInMethodCall1()
        => VerifyBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        someMethod(42, "test", false, {|hint:$$x => {|textspan:{
                            return x;
                        }|}|}, "other arguments}");
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestLambdaInMethodCall2()
        => VerifyBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        someMethod(42, "test", false, {|hint:$$x => {|textspan:{
                            return x;
                        }|}|});
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
}
