// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure;

[Trait(Traits.Feature, Traits.Features.Outlining)]
public sealed class EnumDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<EnumDeclarationSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new EnumDeclarationStructureProvider();

    [Fact]
    public Task TestEnum1()
        => VerifyBlockSpansAsync("""
                {|hint:$$enum E{|textspan:
                {
                }|}|}
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Theory]
    [InlineData("enum")]
    [InlineData("struct")]
    [InlineData("class")]
    [InlineData("interface")]
    public Task TestEnum2(string typeKind)
        => VerifyBlockSpansAsync($$"""
            {|hint:$$enum E{|textspan:
            {
            }|}|}
            {{typeKind}} Following
            {
            }
            """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Theory]
    [InlineData("enum")]
    [InlineData("struct")]
    [InlineData("class")]
    [InlineData("interface")]
    public Task TestEnum3(string typeKind)
        => VerifyBlockSpansAsync($$"""
            {|hint:$$enum E{|textspan:
            {
            }|}|}

            {{typeKind}} Following
            {
            }
            """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestEnumWithLeadingComments()
        => VerifyBlockSpansAsync("""
                {|span1:// Goo
                // Bar|}
                {|hint2:$$enum E{|textspan2:
                {
                }|}|}
                """,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestEnumWithNestedComments()
        => VerifyBlockSpansAsync("""
                {|hint1:$$enum E{|textspan1:
                {
                    {|span2:// Goo
                    // Bar|}
                }|}|}
                """,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: false),
            Region("span2", "// Goo ...", autoCollapse: true));
}
