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
public sealed class ParenthesizedLambdaStructureTests : AbstractCSharpSyntaxNodeStructureTests<ParenthesizedLambdaExpressionSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new ParenthesizedLambdaExpressionStructureProvider();

    [Fact]
    public async Task TestLambda()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        {|hint:$$() => {|textspan:{
                            x();
                        };|}|}
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestLambdaInForLoop()
    {
        await VerifyNoBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        for (Action a = $$() => { }; true; a()) { }
                    }
                }
                """);
    }

    [Fact]
    public async Task TestLambdaInMethodCall1()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        someMethod(42, "test", false, {|hint:$$(x, y, z) => {|textspan:{
                            return x + y + z;
                        }|}|}, "other arguments");
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestLambdaInMethodCall2()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        someMethod(42, "test", false, {|hint:$$(x, y, z) => {|textspan:{
                            return x + y + z;
                        }|}|});
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }
}
