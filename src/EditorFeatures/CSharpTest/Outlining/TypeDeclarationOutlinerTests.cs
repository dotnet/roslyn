// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class TypeDeclarationOutlinerTests : AbstractOutlinerTests<TypeDeclarationSyntax>
    {
        internal override AbstractSyntaxNodeOutliner<TypeDeclarationSyntax> CreateOutliner()
        {
            return new TypeDeclarationOutliner();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestClass()
        {
            const string code = @"
{|hint:$$class C{|collapse:
{
}|}|}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestClassWithLeadingComments()
        {
            const string code = @"
{|span1:// Foo
// Bar|}
{|hint2:$$class C{|collapse2:
{
}|}|}";

            Regions(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestClassWithNestedComments()
        {
            const string code = @"
{|hint1:$$class C{|collapse1:
{
    {|span2:// Foo
    // Bar|}
}|}|}";

            Regions(code,
                Region("collapse1", "hint1", CSharpOutliningHelpers.Ellipsis, autoCollapse: false),
                Region("span2", "// Foo ...", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestInterface()
        {
            const string code = @"
{|hint:$$interface I{|collapse:
{
}|}|}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestInterfaceWithLeadingComments()
        {
            const string code = @"
{|span1:// Foo
// Bar|}
{|hint2:$$interface I{|collapse2:
{
}|}|}";

            Regions(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestInterfaceWithNestedComments()
        {
            const string code = @"
{|hint1:$$interface I{|collapse1:
{
    {|span2:// Foo
    // Bar|}
}|}|}";

            Regions(code,
                Region("collapse1", "hint1", CSharpOutliningHelpers.Ellipsis, autoCollapse: false),
                Region("span2", "// Foo ...", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestStruct()
        {
            const string code = @"
{|hint:$$struct S{|collapse:
{
}|}|}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestStructWithLeadingComments()
        {
            const string code = @"
{|span1:// Foo
// Bar|}
{|hint2:$$struct S{|collapse2:
{
}|}|}";

            Regions(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestStructWithNestedComments()
        {
            const string code = @"
{|hint1:$$struct S{|collapse1:
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
