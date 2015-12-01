// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class SimpleLambdaExpressionOutlinerTests : AbstractCSharpSyntaxNodeOutlinerTests<SimpleLambdaExpressionSyntax>
    {
        internal override AbstractSyntaxOutliner CreateOutliner() => new SimpleLambdaExpressionOutliner();

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestLambda()
        {
            const string code = @"
class C
{
    void M()
    {
        {|hint:$$f => {|collapse:{
            x();
        };|}|}
    }
}";

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestLambdaInForLoop()
        {
            const string code = @"
class C
{
    void M()
    {
        for (Action a = x$$ => { }; true; a()) { }
    }
}";

            await VerifyNoRegionsAsync(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestLambdaInMethodCall1()
        {
            const string code = @"
class C
{
    void M()
    {
        someMethod(42, ""test"", false, {|hint:$$x => {|collapse:{
            return x;
        }|}|}, ""other arguments}"");
    }
}";

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestLambdaInMethodCall2()
        {
            const string code = @"
class C
{
    void M()
    {
        someMethod(42, ""test"", false, {|hint:$$x => {|collapse:{
            return x;
        }|}|});
    }
}";

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }
    }
}
