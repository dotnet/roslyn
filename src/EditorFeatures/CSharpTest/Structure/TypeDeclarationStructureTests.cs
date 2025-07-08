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
public sealed class TypeDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<TypeDeclarationSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new TypeDeclarationStructureProvider();

    [Fact]
    public async Task TestClass1()
    {
        await VerifyBlockSpansAsync("""
                {|hint:$$class C{|textspan:
                {
                }|}|}
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Theory]
    [InlineData("enum")]
    [InlineData("class")]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    [InlineData("struct")]
    [InlineData("interface")]
    public async Task TestClass2(string typeKind)
    {
        await VerifyBlockSpansAsync($@"
{{|hint:$$class C{{|textspan:
{{
}}|}}|}}
{typeKind}D
{{
}}",
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Theory]
    [InlineData("enum")]
    [InlineData("class")]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    [InlineData("struct")]
    [InlineData("interface")]
    public async Task TestClass3(string typeKind)
    {
        await VerifyBlockSpansAsync($@"
{{|hint:$$class C{{|textspan:
{{
}}|}}|}}

{typeKind}D
{{
}}",
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestClassWithLeadingComments()
    {
        await VerifyBlockSpansAsync("""
                {|span1:// Goo
                // Bar|}
                {|hint2:$$class C{|textspan2:
                {
                }|}|}
                """,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestClassWithNestedComments()
    {
        await VerifyBlockSpansAsync("""
                {|hint1:$$class C{|textspan1:
                {
                    {|span2:// Goo
                    // Bar|}
                }|}|}
                """,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: false),
            Region("span2", "// Goo ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestInterface1()
    {
        await VerifyBlockSpansAsync("""
                {|hint:$$interface I{|textspan:
                {
                }|}|}
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Theory]
    [InlineData("enum")]
    [InlineData("class")]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    [InlineData("struct")]
    [InlineData("interface")]
    public async Task TestInterface2(string typeKind)
    {
        await VerifyBlockSpansAsync($@"
{{|hint:$$interface I{{|textspan:
{{
}}|}}|}}
{typeKind}D
{{
}}",
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Theory]
    [InlineData("enum")]
    [InlineData("class")]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    [InlineData("struct")]
    [InlineData("interface")]
    public async Task TestInterface3(string typeKind)
    {
        await VerifyBlockSpansAsync($@"
{{|hint:$$interface I{{|textspan:
{{
}}|}}|}}

{typeKind}D
{{
}}",
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestInterfaceWithLeadingComments()
    {
        await VerifyBlockSpansAsync("""
                {|span1:// Goo
                // Bar|}
                {|hint2:$$interface I{|textspan2:
                {
                }|}|}
                """,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestInterfaceWithNestedComments()
    {
        await VerifyBlockSpansAsync("""
                {|hint1:$$interface I{|textspan1:
                {
                    {|span2:// Goo
                    // Bar|}
                }|}|}
                """,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: false),
            Region("span2", "// Goo ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestStruct1()
    {
        await VerifyBlockSpansAsync("""
                {|hint:$$struct S{|textspan:
                {
                }|}|}
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Theory]
    [InlineData("enum")]
    [InlineData("class")]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    [InlineData("struct")]
    [InlineData("interface")]
    public async Task TestStruct2(string typeKind)
    {
        await VerifyBlockSpansAsync($@"
{{|hint:$$struct C{{|textspan:
{{
}}|}}|}}
{typeKind}D
{{
}}",
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Theory]
    [InlineData("enum")]
    [InlineData("class")]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    [InlineData("struct")]
    [InlineData("interface")]
    public async Task TestStruct3(string typeKind)
    {
        await VerifyBlockSpansAsync($@"
{{|hint:$$struct C{{|textspan:
{{
}}|}}|}}

{typeKind}D
{{
}}",
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestStructWithLeadingComments()
    {
        await VerifyBlockSpansAsync("""
                {|span1:// Goo
                // Bar|}
                {|hint2:$$struct S{|textspan2:
                {
                }|}|}
                """,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestStructWithNestedComments()
    {
        await VerifyBlockSpansAsync("""
                {|hint1:$$struct S{|textspan1:
                {
                    {|span2:// Goo
                    // Bar|}
                }|}|}
                """,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: false),
            Region("span2", "// Goo ...", autoCollapse: true));
    }
}
