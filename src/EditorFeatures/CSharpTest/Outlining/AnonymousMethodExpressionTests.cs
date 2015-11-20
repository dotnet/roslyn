// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class AnonymousMethodExpressionTests : AbstractOutlinerTests<AnonymousMethodExpressionSyntax>
    {
        internal override AbstractSyntaxNodeOutliner<AnonymousMethodExpressionSyntax> CreateOutliner()
        {
            return new AnonymousMethodExpressionOutliner();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestAnonymousMethod()
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

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestAnonymousMethodInForLoop()
        {
            const string code = @"
class C
{
    void Main()
    {
        for (Action a = $$delegate { }; true; a()) { }
    }
}";

            NoRegions(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestAnonymousMethodInMethodCall1()
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

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestAnonymousMethodInMethodCall2()
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

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }
    }
}
