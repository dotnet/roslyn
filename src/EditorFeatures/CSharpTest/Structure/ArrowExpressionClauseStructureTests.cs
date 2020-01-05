// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public class ArrowExpressionClauseStructureTests : AbstractCSharpSyntaxNodeStructureTests<ArrowExpressionClauseSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider()
            => new ArrowExpressionClauseStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestArrowExpressionClause_Method1()
        {
            await VerifyBlockSpansAsync(
@"
class C
{
    {|hintspan:void M(){|textspan: $$=> expression
        ? trueCase
        : falseCase;|}|};
}
",
                Region("textspan", "hintspan", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestArrowExpressionClause_Property1()
        {
            await VerifyBlockSpansAsync(
@"
class C
{
    {|hintspan:int M{|textspan: $$=> expression
        ? trueCase
        : falseCase;|}|};
}
",
                Region("textspan", "hintspan", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestArrowExpressionClause_LocalFunction()
        {
            await VerifyBlockSpansAsync(
@"
class C
{
    void M()
    {
        {|hintspan:void F(){|textspan: $$=> expression
            ? trueCase
            : falseCase;|}|};
    }
}
",
                Region("textspan", "hintspan", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }
    }
}
