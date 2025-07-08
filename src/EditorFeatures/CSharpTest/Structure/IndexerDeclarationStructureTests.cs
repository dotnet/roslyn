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
public sealed class IndexerDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<IndexerDeclarationSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new IndexerDeclarationStructureProvider();

    [Fact]
    public async Task TestIndexer1()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public string this[int index]{|textspan:
                    {
                        get { }
                    }|}|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestIndexer2()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public string this[int index]{|textspan:
                    {
                        get { }
                    }|}|}
                    int Value => 0;
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestIndexer3()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public string this[int index]{|textspan:
                    {
                        get { }
                    }|}|}

                    int Value => 0;
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestIndexerWithComments()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    {|span1:// Goo
                    // Bar|}
                    {|hint2:$$public string this[int index]{|textspan2:
                    {
                        get { }
                    }|}|}
                }
                """,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestIndexerWithWithExpressionBodyAndComments()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    {|span:// Goo
                    // Bar|}
                    $$public string this[int index] => 0;
                }
                """,
            Region("span", "// Goo ...", autoCollapse: true));
    }
}
