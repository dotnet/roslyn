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
    public class ConversionOperatorDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<ConversionOperatorDeclarationSyntax>
    {
        protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
        internal override AbstractSyntaxStructureProvider CreateProvider() => new ConversionOperatorDeclarationStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task NoCommentsOrAttributes()
        {
            const string code = @"
class C
{
    public static explicit operator $$Goo(byte b);
}";

            await VerifyNoBlockSpansAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task WithAttributes()
        {
            const string code = @"
class C
{
    {|hint:{|textspan:[Blah]
    |}public static explicit operator $$Goo(byte b);|}
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task WithCommentsAndAttributes()
        {
            const string code = @"
class C
{
    {|hint:{|textspan:// Summary:
    //     This is a summary.
    [Blah]
    |}public static explicit operator $$Goo(byte b);|}
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestOperator3()
        {
            const string code = @"
class C
{
    $$public static explicit operator C(byte i)
    {
    }

    public static explicit operator C(short i)
    {
    }
}";

            await VerifyBlockSpansAsync(code,
                new BlockSpan(
                    isCollapsible: true,
                    textSpan: TextSpan.FromBounds(59, 75),
                    hintSpan: TextSpan.FromBounds(18, 73),
                    type: BlockTypes.Nonstructural,
                    bannerText: CSharpStructureHelpers.Ellipsis,
                    autoCollapse: true));
        }
    }
}
