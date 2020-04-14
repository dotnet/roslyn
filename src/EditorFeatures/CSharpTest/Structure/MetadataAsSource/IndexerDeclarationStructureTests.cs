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
    public class IndexerDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<IndexerDeclarationSyntax>
    {
        protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
        internal override AbstractSyntaxStructureProvider CreateProvider() => new IndexerDeclarationStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task NoCommentsOrAttributes()
        {
            const string code = @"
class Goo
{
    {|hint:public string $$this[int x] {|textspan:{ get; set; }|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task WithAttributes()
        {
            const string code = @"
class Goo
{
    {|hint1:{|textspan1:[Goo]
    |}{|hint2:public string $$this[int x] {|textspan2:{ get; set; }|}|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
                Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task WithCommentsAndAttributes()
        {
            const string code = @"
class Goo
{
    {|hint1:{|textspan1:// Summary:
    //     This is a summary.
    [Goo]
    |}{|hint2:string $$this[int x] {|textspan2:{ get; set; }|}|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
                Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task WithCommentsAttributesAndmodifiers()
        {
            const string code = @"
class Goo
{
    {|hint1:{|textspan1:// Summary:
    //     This is a summary.
    [Goo]
    |}{|hint2:public string $$this[int x] {|textspan2:{ get; set; }|}|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
                Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIndexer3()
        {
            const string code = @"
class C
{
    $$public string this[int index]
    {
        get { }
    }

    int Value => 0;
}";

            await VerifyBlockSpansAsync(code,
                new BlockSpan(
                    isCollapsible: true,
                    textSpan: TextSpan.FromBounds(47, 80),
                    hintSpan: TextSpan.FromBounds(18, 78),
                    type: BlockTypes.Nonstructural,
                    bannerText: CSharpStructureHelpers.Ellipsis,
                    autoCollapse: true));
        }
    }
}
