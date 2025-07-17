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

public sealed class IndexerDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<IndexerDeclarationSyntax>
{
    protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
    internal override AbstractSyntaxStructureProvider CreateProvider() => new IndexerDeclarationStructureProvider();

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public Task NoCommentsOrAttributes()
        => VerifyBlockSpansAsync("""
                class Goo
                {
                    {|hint:public string $$this[int x] {|textspan:{ get; set; }|}|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public Task WithAttributes()
        => VerifyBlockSpansAsync("""
                class Goo
                {
                    {|hint1:{|textspan1:[Goo]
                    |}{|hint2:public string $$this[int x] {|textspan2:{ get; set; }|}|}|}
                }
                """,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public Task WithCommentsAndAttributes()
        => VerifyBlockSpansAsync("""
                class Goo
                {
                    {|hint1:{|textspan1:// Summary:
                    //     This is a summary.
                    [Goo]
                    |}{|hint2:string $$this[int x] {|textspan2:{ get; set; }|}|}|}
                }
                """,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public Task WithCommentsAttributesAndmodifiers()
        => VerifyBlockSpansAsync("""
                class Goo
                {
                    {|hint1:{|textspan1:// Summary:
                    //     This is a summary.
                    [Goo]
                    |}{|hint2:public string $$this[int x] {|textspan2:{ get; set; }|}|}|}
                }
                """,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
    public Task TestIndexer3()
        => VerifyBlockSpansAsync("""
                class C
                {
                    $${|#0:public string this[int index]{|textspan:
                    {
                        get { }
                    }|#0}
                |}
                    int Value => 0;
                }
                """,
            Region("textspan", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
}
