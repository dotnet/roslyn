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
public sealed class EventFieldDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<EventFieldDeclarationSyntax>
{
    protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
    internal override AbstractSyntaxStructureProvider CreateProvider() => new EventFieldDeclarationStructureProvider();

    [Fact]
    public Task NoCommentsOrAttributes()
        => VerifyNoBlockSpansAsync("""
                class Goo
                {
                    public event EventArgs $$goo
                }
                """);

    [Fact]
    public Task WithAttributes()
        => VerifyBlockSpansAsync("""
                class Goo
                {
                    {|hint:{|textspan:[Goo]
                    |}event EventArgs $$goo|}
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
                    [Goo]
                    |}event EventArgs $$goo|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task WithCommentsAttributesAndModifiers()
        => VerifyBlockSpansAsync("""
                class Goo
                {
                    {|hint:{|textspan:// Summary:
                    //     This is a summary.
                    [Goo]
                    |}public event EventArgs $$goo|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
}
