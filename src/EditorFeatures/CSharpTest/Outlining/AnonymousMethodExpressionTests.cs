// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class AnonymousMethodExpressionTests : AbstractCSharpSyntaxNodeOutlinerTests<AnonymousMethodExpressionSyntax>
    {
        internal override AbstractSyntaxOutliner CreateOutliner() => new AnonymousMethodExpressionOutliner();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestAnonymousMethod()
        {
            const string code = @"
class C
{
    void Main()
    {
        $${|hint:delegate {|collapse:{
            x();
        };|}|}
    }
}";

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
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

            await VerifyNoRegionsAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestAnonymousMethodInMethodCall1()
        {
            const string code = @"
class C
{
    void Main()
    {
        someMethod(42, ""test"", false, {|hint:$$delegate(int x, int y, int z) {|collapse:{
            return x + y + z;
        }|}|}, ""other arguments"");
    }
}";

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestAnonymousMethodInMethodCall2()
        {
            const string code = @"
class C
{
    void Main()
    {
        someMethod(42, ""test"", false, {|hint:$$delegate(int x, int y, int z) {|collapse:{
            return x + y + z;
        }|}|});
    }
}";

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }
    }
}
