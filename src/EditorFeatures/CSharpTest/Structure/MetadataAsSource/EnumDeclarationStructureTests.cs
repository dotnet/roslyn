// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure.MetadataAsSource
{
    public class EnumDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<EnumDeclarationSyntax>
    {
        protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
        internal override AbstractSyntaxStructureProvider CreateProvider() => new EnumDeclarationStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task NoCommentsOrAttributes()
        {
            const string code = @"
{|hint:enum $$E{|textspan:
{
    A,
    B
}|}|}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task WithAttributes()
        {
            const string code = @"
{|hint:{|textspan:[Bar]
|}enum $$E|}
{
    A,
    B
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
                new BlockSpan(
                    isCollapsible: true,
                    textSpan: TextSpan.FromBounds(15, 36),
                    hintSpan: TextSpan.FromBounds(9, 36),
                    type: BlockTypes.Nonstructural,
                    bannerText: CSharpStructureHelpers.Ellipsis,
                    autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task WithCommentsAndAttributes()
        {
            const string code = @"
{|hint:{|textspan:// Summary:
//     This is a summary.
[Bar]
|}enum $$E|}
{
    A,
    B
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
                new BlockSpan(
                    isCollapsible: true,
                    textSpan: TextSpan.FromBounds(55, 76),
                    hintSpan: TextSpan.FromBounds(49, 76),
                    type: BlockTypes.Nonstructural,
                    bannerText: CSharpStructureHelpers.Ellipsis,
                    autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task WithCommentsAttributesAndModifiers()
        {
            const string code = @"
{|hint:{|textspan:// Summary:
//     This is a summary.
[Bar]
|}public enum $$E|}
{
    A,
    B
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
                new BlockSpan(
                    isCollapsible: true,
                    textSpan: TextSpan.FromBounds(62, 83),
                    hintSpan: TextSpan.FromBounds(49, 83),
                    type: BlockTypes.Nonstructural,
                    bannerText: CSharpStructureHelpers.Ellipsis,
                    autoCollapse: false));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Outlining)]
        [InlineData("enum")]
        [InlineData("struct")]
        [InlineData("class")]
        [InlineData("interface")]
        public async Task TestEnum3(string typeKind)
        {
            var code = $@"
$$enum E
{{
}}

{typeKind} Following
{{
}}";

            await VerifyBlockSpansAsync(code,
                new BlockSpan(
                    isCollapsible: true,
                    textSpan: TextSpan.FromBounds(8, 16),
                    hintSpan: TextSpan.FromBounds(2, 14),
                    type: BlockTypes.Nonstructural,
                    bannerText: CSharpStructureHelpers.Ellipsis,
                    autoCollapse: false));
        }
    }
}
