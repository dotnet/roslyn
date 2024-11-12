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
public class InterpolatedStringExpressionStructureTests : AbstractCSharpSyntaxNodeStructureTests<InterpolatedStringExpressionSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider()
        => new InterpolatedStringExpressionStructureProvider();

    [Fact]
    public async Task TestMultiLineStringLiteral()
    {
        await VerifyBlockSpansAsync(
            """
                class C
                {
                    void M()
                    {
                        var v =
                {|hint:{|textspan:$$$@"
                {123}
                "|}|};
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestMissingOnIncompleteStringLiteral()
    {
        await VerifyNoBlockSpansAsync(
            """
                class C
                {
                    void M()
                    {
                        var v = $$$";
                    }
                }
                """);
    }
}
