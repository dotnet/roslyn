// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public class ParenthesizedLambdaStructureTests : AbstractCSharpSyntaxNodeStructureTests<ParenthesizedLambdaExpressionSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider() => new ParenthesizedLambdaExpressionStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestLambda()
        {
            const string code = @"
class C
{
    void M()
    {
        {|hint:$$() => {|textspan:{
            x();
        };|}|}
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestLambdaInForLoop()
        {
            const string code = @"
class C
{
    void M()
    {
        for (Action a = $$() => { }; true; a()) { }
    }
}";

            await VerifyNoBlockSpansAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestLambdaInMethodCall1()
        {
            const string code = @"
class C
{
    void M()
    {
        someMethod(42, ""test"", false, {|hint:$$(x, y, z) => {|textspan:{
            return x + y + z;
        }|}|}, ""other arguments"");
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestLambdaInMethodCall2()
        {
            const string code = @"
class C
{
    void M()
    {
        someMethod(42, ""test"", false, {|hint:$$(x, y, z) => {|textspan:{
            return x + y + z;
        }|}|});
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }
    }
}
