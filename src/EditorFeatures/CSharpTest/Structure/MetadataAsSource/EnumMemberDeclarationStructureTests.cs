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
public sealed class EnumMemberDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<EnumMemberDeclarationSyntax>
{
    protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
    internal override AbstractSyntaxStructureProvider CreateProvider() => new EnumMemberDeclarationStructureProvider();

    [Fact]
    public Task NoCommentsOrAttributes()
        => VerifyNoBlockSpansAsync("""
                enum E
                {
                    $$Goo,
                    Bar
                }
                """);

    [Fact]
    public Task WithAttributes()
        => VerifyBlockSpansAsync("""
                enum E
                {
                    {|hint:{|textspan:[Blah]
                    |}$$Goo|},
                    Bar
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task WithCommentsAndAttributes()
        => VerifyBlockSpansAsync("""
                enum E
                {
                    {|hint:{|textspan:// Summary:
                    //     This is a summary.
                    [Blah]
                    |}$$Goo|},
                    Bar
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
}
