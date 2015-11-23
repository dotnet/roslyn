// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class PropertyDeclarationOutlinerTests : AbstractCSharpSyntaxNodeOutlinerTests<PropertyDeclarationSyntax>
    {
        internal override AbstractSyntaxOutliner CreateOutliner() => new PropertyDeclarationOutliner();

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestProperty()
        {
            const string code = @"
class C
{
    {|hint:$$public int Foo{|collapse:
    {
        get { }
        set { }
    }|}|}
}";

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertyWithLeadingComments()
        {
            const string code = @"
class C
{
    {|span1:// Foo
    // Bar|}
    {|hint2:$$public int Foo{|collapse2:
    {
        get { }
        set { }
    }|}|}
}";

            await VerifyRegionsAsync(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertyWithWithExpressionBodyAndComments()
        {
            const string code = @"
class C
{
    {|span:// Foo
    // Bar|}
    $$public int Foo => 0;
}";

            await VerifyRegionsAsync(code,
                Region("span", "// Foo ...", autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertyWithSpaceAfterIdentifier()
        {
            const string code = @"
class C
{
    {|hint:$$public int Foo    {|collapse:
    {
        get { }
        set { }
    }|}|}
}";

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }
    }
}
