// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure.MetadataAsSource;

public class EnumDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<EnumDeclarationSyntax>
{
    protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
    internal override AbstractSyntaxStructureProvider CreateProvider() => new EnumDeclarationStructureProvider();

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public async Task NoCommentsOrAttributes()
    {
        var code = """
                {|hint:enum $$E{|textspan:
                {
                    A,
                    B
                }|}|}
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public async Task WithAttributes()
    {
        var code = """
                {|hint:{|textspan:[Bar]
                |}{|#0:enum $$E|}{|textspan2:
                {
                    A,
                    B
                }|}|#0}
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public async Task WithCommentsAndAttributes()
    {
        var code = """
                {|hint:{|textspan:// Summary:
                //     This is a summary.
                [Bar]
                |}{|#0:enum $$E|}{|textspan2:
                {
                    A,
                    B
                }|}|#0}
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public async Task WithCommentsAttributesAndModifiers()
    {
        var code = """
                {|hint:{|textspan:// Summary:
                //     This is a summary.
                [Bar]
                |}{|#0:public enum $$E|}{|textspan2:
                {
                    A,
                    B
                }|}|#0}
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Theory, Trait(Traits.Feature, Traits.Features.Outlining)]
    [InlineData("enum")]
    [InlineData("struct")]
    [InlineData("class")]
    [InlineData("interface")]
    public async Task TestEnum3(string typeKind)
    {
        var code = $@"
{{|#0:$$enum E{{|textspan:
{{
}}|#0}}
|}}
{typeKind} Following
{{
}}";

        await VerifyBlockSpansAsync(code,
            Region("textspan", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }
}
