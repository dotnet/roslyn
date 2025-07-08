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

[Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
public sealed class DestructorDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<DestructorDeclarationSyntax>
{
    protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
    internal override AbstractSyntaxStructureProvider CreateProvider() => new DestructorDeclarationStructureProvider();

    [Fact]
    public Task NoCommentsOrAttributes()
        => VerifyNoBlockSpansAsync("""
                class Goo
                {
                    $$~Goo();
                }
                """);

    [Fact]
    public Task WithAttributes()
        => VerifyBlockSpansAsync("""
                class Goo
                {
                    {|hint:{|textspan:[Bar]
                    |}$$~Goo();|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task WithCommentsAndAttributes()
        => VerifyBlockSpansAsync("""
                class Goo
                {
                    {|hint:{|textspan:// Summary:
                    //     This is a summary.
                    [Bar]
                    |}$$~Goo();|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
}
