// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class EnumDeclarationOutlinerTests : AbstractCSharpSyntaxNodeOutlinerTests<EnumDeclarationSyntax>
    {
        internal override AbstractSyntaxOutliner CreateOutliner() => new EnumDeclarationOutliner();

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestEnum()
        {
            const string code = @"
{|hint:$$enum E{|collapse:
{
}|}|}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestEnumWithLeadingComments()
        {
            const string code = @"
{|span1:// Foo
// Bar|}
{|hint2:$$enum E{|collapse2:
{
}|}|}";

            Regions(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestEnumWithNestedComments()
        {
            const string code = @"
{|hint1:$$enum E{|collapse1:
{
    {|span2:// Foo
    // Bar|}
}|}|}";

            Regions(code,
                Region("collapse1", "hint1", CSharpOutliningHelpers.Ellipsis, autoCollapse: false),
                Region("span2", "// Foo ...", autoCollapse: true));
        }
    }
}
