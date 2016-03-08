// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class TypeDeclarationOutlinerTests : AbstractCSharpSyntaxNodeOutlinerTests<TypeDeclarationSyntax>
    {
        internal override AbstractSyntaxOutliner CreateOutliner() => new TypeDeclarationOutliner();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestClass()
        {
            const string code = @"
{|hint:$$class C{|collapse:
{
}|}|}";

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestClassWithLeadingComments()
        {
            const string code = @"
{|span1:// Foo
// Bar|}
{|hint2:$$class C{|collapse2:
{
}|}|}";

            await VerifyRegionsAsync(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestClassWithNestedComments()
        {
            const string code = @"
{|hint1:$$class C{|collapse1:
{
    {|span2:// Foo
    // Bar|}
}|}|}";

            await VerifyRegionsAsync(code,
                Region("collapse1", "hint1", CSharpOutliningHelpers.Ellipsis, autoCollapse: false),
                Region("span2", "// Foo ...", autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestInterface()
        {
            const string code = @"
{|hint:$$interface I{|collapse:
{
}|}|}";

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestInterfaceWithLeadingComments()
        {
            const string code = @"
{|span1:// Foo
// Bar|}
{|hint2:$$interface I{|collapse2:
{
}|}|}";

            await VerifyRegionsAsync(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestInterfaceWithNestedComments()
        {
            const string code = @"
{|hint1:$$interface I{|collapse1:
{
    {|span2:// Foo
    // Bar|}
}|}|}";

            await VerifyRegionsAsync(code,
                Region("collapse1", "hint1", CSharpOutliningHelpers.Ellipsis, autoCollapse: false),
                Region("span2", "// Foo ...", autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestStruct()
        {
            const string code = @"
{|hint:$$struct S{|collapse:
{
}|}|}";

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestStructWithLeadingComments()
        {
            const string code = @"
{|span1:// Foo
// Bar|}
{|hint2:$$struct S{|collapse2:
{
}|}|}";

            await VerifyRegionsAsync(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestStructWithNestedComments()
        {
            const string code = @"
{|hint1:$$struct S{|collapse1:
{
    {|span2:// Foo
    // Bar|}
}|}|}";

            await VerifyRegionsAsync(code,
                Region("collapse1", "hint1", CSharpOutliningHelpers.Ellipsis, autoCollapse: false),
                Region("span2", "// Foo ...", autoCollapse: true));
        }
    }
}
