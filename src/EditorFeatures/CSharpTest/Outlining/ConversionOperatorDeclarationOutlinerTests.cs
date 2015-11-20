// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class ConversionOperatorDeclarationOutlinerTests : AbstractCSharpSyntaxNodeOutlinerTests<ConversionOperatorDeclarationSyntax>
    {
        internal override AbstractSyntaxOutliner CreateOutliner() => new ConversionOperatorDeclarationOutliner();

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestOperator()
        {
            const string code = @"
class C
{
    {|hint:$$public static explicit operator C(byte i){|collapse:
    {
    }|}|}
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact,
         Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestOperatorWithLeadingComments()
        {
            const string code = @"
class C
{
    {|span1:// Foo
    // Bar|}
    {|hint2:$$public static explicit operator C(byte i){|collapse2:
    {
    }|}|}
}";

            Regions(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }
    }
}
