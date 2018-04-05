// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public class InitializerExpressionStructureTests : AbstractCSharpSyntaxNodeStructureTests<InitializerExpressionSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider()
            => new InitializerExpressionStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestOuterInitializer()
        {
            await VerifyBlockSpansAsync(
@"
class C
{
    void M()
    {
        var v = {|hint:new Dictionary<int, int>{|textspan: $${
            { 1, 2 },
            { 1, 2 },
        }|}|};
    }
}
",
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestInnerInitializer()
        {
            await VerifyBlockSpansAsync(
@"
class C
{
    void M()
    {
        var v = new Dictionary<int, int>{
            {|hint:{|textspan:$${
                1, 2
            },|}|}
            { 1, 2 },
        };
    }
}
",
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }
    }
}
