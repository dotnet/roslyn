// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public class StringLiteralExpressionStructureTests : AbstractCSharpSyntaxNodeStructureTests<LiteralExpressionSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider() => new StringLiteralExpressoinStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestVerbatimString()
        {
            await VerifyBlockSpansAsync(
@"
class C
{
    void M()
    {
        var v =
{|hint:{|textspan:$$@""
class 
{
}
""|}|};
    }
}",
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }
    }
}
