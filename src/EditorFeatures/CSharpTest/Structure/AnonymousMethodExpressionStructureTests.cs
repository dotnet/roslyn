// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public class AnonymousMethodExpressionTests : AbstractCSharpSyntaxNodeStructureTests<AnonymousMethodExpressionSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider() => new AnonymousMethodExpressionStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestAnonymousMethod()
        {
            const string code = @"
class C
{
    void Main()
    {
        $${|hint:delegate {|textspan:{
            x();
        };|}|}
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestAnonymousMethodInForLoop()
        {
            const string code = @"
class C
{
    void Main()
    {
        for (Action a = $$delegate { }; true; a()) { }
    }
}";

            await VerifyNoBlockSpansAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestAnonymousMethodInMethodCall1()
        {
            const string code = @"
class C
{
    void Main()
    {
        someMethod(42, ""test"", false, {|hint:$$delegate(int x, int y, int z) {|textspan:{
            return x + y + z;
        }|}|}, ""other arguments"");
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestAnonymousMethodInMethodCall2()
        {
            const string code = @"
class C
{
    void Main()
    {
        someMethod(42, ""test"", false, {|hint:$$delegate(int x, int y, int z) {|textspan:{
            return x + y + z;
        }|}|});
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }
    }
}
