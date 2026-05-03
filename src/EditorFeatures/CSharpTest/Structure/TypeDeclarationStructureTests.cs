// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure;

[Trait(Traits.Feature, Traits.Features.Outlining)]
public sealed class TypeDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<TypeDeclarationSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new TypeDeclarationStructureProvider();

    [Fact]
    public Task TestClass1()
        => VerifyBlockSpansAsync("""
                {|hint:$$class C{|textspan:
                {
                }|}|}
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Theory]
    [InlineData("enum")]
    [InlineData("class")]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    [InlineData("struct")]
    [InlineData("interface")]
    public Task TestClass2(string typeKind)
        => VerifyBlockSpansAsync($$"""
            {|hint:$$class C{|textspan:
            {
            }|}|}
            {{typeKind}}D
            {
            }
            """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Theory]
    [InlineData("enum")]
    [InlineData("class")]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    [InlineData("struct")]
    [InlineData("interface")]
    public Task TestClass3(string typeKind)
        => VerifyBlockSpansAsync($$"""
            {|hint:$$class C{|textspan:
            {
            }|}|}

            {{typeKind}}D
            {
            }
            """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestClassWithLeadingComments()
        => VerifyBlockSpansAsync("""
                {|span1:// Goo
                // Bar|}
                {|hint2:$$class C{|textspan2:
                {
                }|}|}
                """,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestClassWithNestedComments()
        => VerifyBlockSpansAsync("""
                {|hint1:$$class C{|textspan1:
                {
                    {|span2:// Goo
                    // Bar|}
                }|}|}
                """,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: false),
            Region("span2", "// Goo ...", autoCollapse: true));

    [Fact]
    public Task TestInterface1()
        => VerifyBlockSpansAsync("""
                {|hint:$$interface I{|textspan:
                {
                }|}|}
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Theory]
    [InlineData("enum")]
    [InlineData("class")]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    [InlineData("struct")]
    [InlineData("interface")]
    public Task TestInterface2(string typeKind)
        => VerifyBlockSpansAsync($$"""
            {|hint:$$interface I{|textspan:
            {
            }|}|}
            {{typeKind}}D
            {
            }
            """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Theory]
    [InlineData("enum")]
    [InlineData("class")]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    [InlineData("struct")]
    [InlineData("interface")]
    public Task TestInterface3(string typeKind)
        => VerifyBlockSpansAsync($$"""
            {|hint:$$interface I{|textspan:
            {
            }|}|}

            {{typeKind}}D
            {
            }
            """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestInterfaceWithLeadingComments()
        => VerifyBlockSpansAsync("""
                {|span1:// Goo
                // Bar|}
                {|hint2:$$interface I{|textspan2:
                {
                }|}|}
                """,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestInterfaceWithNestedComments()
        => VerifyBlockSpansAsync("""
                {|hint1:$$interface I{|textspan1:
                {
                    {|span2:// Goo
                    // Bar|}
                }|}|}
                """,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: false),
            Region("span2", "// Goo ...", autoCollapse: true));

    [Fact]
    public Task TestStruct1()
        => VerifyBlockSpansAsync("""
                {|hint:$$struct S{|textspan:
                {
                }|}|}
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Theory]
    [InlineData("enum")]
    [InlineData("class")]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    [InlineData("struct")]
    [InlineData("interface")]
    public Task TestStruct2(string typeKind)
        => VerifyBlockSpansAsync($$"""
            {|hint:$$struct C{|textspan:
            {
            }|}|}
            {{typeKind}}D
            {
            }
            """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Theory]
    [InlineData("enum")]
    [InlineData("class")]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    [InlineData("struct")]
    [InlineData("interface")]
    public Task TestStruct3(string typeKind)
        => VerifyBlockSpansAsync($$"""
            {|hint:$$struct C{|textspan:
            {
            }|}|}

            {{typeKind}}D
            {
            }
            """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestStructWithLeadingComments()
        => VerifyBlockSpansAsync("""
                {|span1:// Goo
                // Bar|}
                {|hint2:$$struct S{|textspan2:
                {
                }|}|}
                """,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestStructWithNestedComments()
        => VerifyBlockSpansAsync("""
                {|hint1:$$struct S{|textspan1:
                {
                    {|span2:// Goo
                    // Bar|}
                }|}|}
                """,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: false),
            Region("span2", "// Goo ...", autoCollapse: true));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/62506")]
    public Task TestTypeDeclarationNoBraces1()
        => VerifyBlockSpansAsync("""
                {|comment:// comment|}
                $$struct S
                """,
            Region("comment", "// comment ...", autoCollapse: true));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/62506")]
    public Task TestTypeDeclarationNoBraces2()
        => VerifyBlockSpansAsync("""
                {|comment:// comment|}
                {|hint1:$$struct S{|textspan1:;|}|}
                """,
            Region("comment", "// comment ...", autoCollapse: true),
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81662")]
    public Task TestExtension1()
        => VerifyBlockSpansAsync("""
                {|hint:$$extension{|textspan:(string s)
                {
                }|}|}
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81662")]
    public Task TestExtension2()
        => VerifyBlockSpansAsync("""
                {|hint:$$extension<T>{|textspan:(string s)
                {
                }|}|}
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
}
