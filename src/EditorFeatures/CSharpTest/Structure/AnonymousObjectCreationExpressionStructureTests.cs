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
public class AnonymousObjectCreationExpressionStructureTests : AbstractCSharpSyntaxNodeStructureTests<AnonymousObjectCreationExpressionSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider()
        => new AnonymousObjectCreationExpressionStructureProvider();

    [Fact]
    public async Task TestAnonymousObjectCreation()
    {
        await VerifyBlockSpansAsync(
            """
                class C
                {
                    void M()
                    {
                        var v = {|hint:new{|textspan: $${
                            Name = "John",
                            Age = 19
                        }|}|};
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }
}
