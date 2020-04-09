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
    public class AccessorDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<AccessorDeclarationSyntax>
    {
        protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
        internal override AbstractSyntaxStructureProvider CreateProvider() => new AccessorDeclarationStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertyGetter3()
        {
            const string code = @"
class C
{
    public string Text
    {
        $${|hint:get{|textspan:
        {
        }|}
|}
        set
        {
        }
    }
}";

            await VerifyBlockSpansAsync(code,
                new BlockSpan(
                    isCollapsible: true,
                    textSpan: TextSpan.FromBounds(56, 80),
                    hintSpan: TextSpan.FromBounds(53, 78),
                    type: BlockTypes.Nonstructural,
                    bannerText: CSharpStructureHelpers.Ellipsis,
                    autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertyGetterWithSingleLineComments3()
        {
            const string code = @"
class C
{
    public string Text
    {
        {|span1:// My
        // Getter|}
        $${|hint2:get{|textspan2:
        {
        }|}
|}
        set
        {
        }
    }
}
";

            await VerifyBlockSpansAsync(code,
                Region("span1", "// My ...", autoCollapse: true),
                new BlockSpan(
                    isCollapsible: true,
                    textSpan: TextSpan.FromBounds(90, 114),
                    hintSpan: TextSpan.FromBounds(87, 112),
                    type: BlockTypes.Nonstructural,
                    bannerText: CSharpStructureHelpers.Ellipsis,
                    autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertyGetterWithMultiLineComments3()
        {
            const string code = @"
class C
{
    public string Text
    {
        {|span1:/* My
           Getter */|}
        $${|hint2:get{|textspan2:
        {
        }|}
|}
        set
        {
        }
    }
}
";

            await VerifyBlockSpansAsync(code,
                Region("span1", "/* My ...", autoCollapse: true),
                new BlockSpan(
                    isCollapsible: true,
                    textSpan: TextSpan.FromBounds(93, 117),
                    hintSpan: TextSpan.FromBounds(90, 115),
                    type: BlockTypes.Nonstructural,
                    bannerText: CSharpStructureHelpers.Ellipsis,
                    autoCollapse: true));
        }
    }
}
