// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public class EnumDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<EnumDeclarationSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider() => new EnumDeclarationStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestEnum1()
        {
            const string code = @"
{|hint:$$enum E{|textspan:
{
}|}|}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Outlining)]
        [InlineData("enum")]
        [InlineData("struct")]
        [InlineData("class")]
        [InlineData("interface")]
        public async Task TestEnum2(string typeKind)
        {
            var code = $@"
{{|hint:$$enum E{{|textspan:
{{
}}|}}|}}
{typeKind} Following
{{
}}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Outlining)]
        [InlineData("enum")]
        [InlineData("struct")]
        [InlineData("class")]
        [InlineData("interface")]
        public async Task TestEnum3(string typeKind)
        {
            var code = $@"
{{|hint:$$enum E{{|textspan:
{{
}}|}}|}}

{typeKind} Following
{{
}}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestEnumWithLeadingComments()
        {
            const string code = @"
{|span1:// Goo
// Bar|}
{|hint2:$$enum E{|textspan2:
{
}|}|}";

            await VerifyBlockSpansAsync(code,
                Region("span1", "// Goo ...", autoCollapse: true),
                Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestEnumWithNestedComments()
        {
            const string code = @"
{|hint1:$$enum E{|textspan1:
{
    {|span2:// Goo
    // Bar|}
}|}|}";

            await VerifyBlockSpansAsync(code,
                Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: false),
                Region("span2", "// Goo ...", autoCollapse: true));
        }
    }
}
