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
public class TypeDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<TypeDeclarationSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new TypeDeclarationStructureProvider();

    [Fact]
    public async Task TestClass1()
    {
        var code = """
                {|hint:$$class C{|textspan:
                {
                }|}|}
                """;

        await VerifyBlockSpansAsync(code,
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
        var code = $@"
{{|hint:$$class C{{|textspan:
{{
}}|}}|}}
{typeKind}D
{{
}}";

        await VerifyBlockSpansAsync(code,
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
        var code = $@"
{{|hint:$$class C{{|textspan:
{{
}}|}}|}}

{typeKind}D
{{
}}";

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestClassWithLeadingComments()
    {
        var code = """
                {|span1:// Goo
                // Bar|}
                {|hint2:$$class C{|textspan2:
                {
                }|}|}
                """;

        await VerifyBlockSpansAsync(code,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestClassWithNestedComments()
    {
        var code = """
                {|hint1:$$class C{|textspan1:
                {
                    {|span2:// Goo
                    // Bar|}
                }|}|}
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: false),
            Region("span2", "// Goo ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestInterface1()
    {
        var code = """
                {|hint:$$interface I{|textspan:
                {
                }|}|}
                """;

        await VerifyBlockSpansAsync(code,
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
        var code = $@"
{{|hint:$$interface I{{|textspan:
{{
}}|}}|}}
{typeKind}D
{{
}}";

        await VerifyBlockSpansAsync(code,
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
        var code = $@"
{{|hint:$$interface I{{|textspan:
{{
}}|}}|}}

{typeKind}D
{{
}}";

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestInterfaceWithLeadingComments()
    {
        var code = """
                {|span1:// Goo
                // Bar|}
                {|hint2:$$interface I{|textspan2:
                {
                }|}|}
                """;

        await VerifyBlockSpansAsync(code,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestInterfaceWithNestedComments()
    {
        var code = """
                {|hint1:$$interface I{|textspan1:
                {
                    {|span2:// Goo
                    // Bar|}
                }|}|}
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: false),
            Region("span2", "// Goo ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestStruct1()
    {
        var code = """
                {|hint:$$struct S{|textspan:
                {
                }|}|}
                """;

        await VerifyBlockSpansAsync(code,
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
        var code = $@"
{{|hint:$$struct C{{|textspan:
{{
}}|}}|}}
{typeKind}D
{{
}}";

        await VerifyBlockSpansAsync(code,
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
        var code = $@"
{{|hint:$$struct C{{|textspan:
{{
}}|}}|}}

{typeKind}D
{{
}}";

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestStructWithLeadingComments()
    {
        var code = """
                {|span1:// Goo
                // Bar|}
                {|hint2:$$struct S{|textspan2:
                {
                }|}|}
                """;

        await VerifyBlockSpansAsync(code,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestStructWithNestedComments()
    {
        var code = """
                {|hint1:$$struct S{|textspan1:
                {
                    {|span2:// Goo
                    // Bar|}
                }|}|}
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: false),
            Region("span2", "// Goo ...", autoCollapse: true));
    }
}
