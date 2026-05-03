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

public sealed class ConstructorDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<ConstructorDeclarationSyntax>
{
    protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
    internal override AbstractSyntaxStructureProvider CreateProvider() => new ConstructorDeclarationStructureProvider();

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public Task NoCommentsOrAttributes()
        => VerifyNoBlockSpansAsync("""
                class C
                {
                    $$C();
                }
                """);

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public Task WithAttributes()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:{|textspan:[Bar]
                    |}$$C();|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public Task WithCommentsAndAttributes()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:{|textspan:// Summary:
                    //     This is a summary.
                    [Bar]
                    |}$$C();|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public Task WithCommentsAttributesAndModifiers()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:{|textspan:// Summary:
                    //     This is a summary.
                    [Bar]
                    |}$$public C();|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
    public Task TestConstructor10()
        => VerifyBlockSpansAsync("""
                class C
                {
                    $${|#0:public C(){|textspan:
                    {
                    }|#0}
                |}
                    public C(int x)
                    {
                    }
                }
                """,
            Region("textspan", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
}
