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
public sealed class CollectionExpressionStructureTests : AbstractCSharpSyntaxNodeStructureTests<CollectionExpressionSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider()
        => new CollectionExpressionStructureProvider();

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/71932")]
    public Task TestOuterCollectionExpression()
        => VerifyBlockSpansAsync(
             """
                class C
                {
                    void M()
                    {
                        int[] a ={|hint:{|textspan: $$[
                            1,
                            2,
                            3
                        ]|}|};
                    }
                }
                """,
             Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/71932")]
    public Task TestInnerCollectionExpressionWithoutComma()
        => VerifyBlockSpansAsync(
             """
                class C
                {
                    void M()
                    {
                        List<List<int>> b = 
                        [
                            [1],
                            [2, 3],
                            {|hint:{|textspan:$$[
                                3, 5, 6
                            ]|}|}
                        ];
                    }
                }
                """,
             Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/71932")]
    public Task TestInnerCollectionExpressionWithComma()
        => VerifyBlockSpansAsync(
             """
                class C
                {
                    void M()
                    {
                        List<List<int>> c = 
                        [
                            [1],
                            [2, 3],
                            {|hint:{|textspan:$$[
                                3, 5, 6
                            ],|}|}
                        ];
                    }
                }
                """,
             Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
}
