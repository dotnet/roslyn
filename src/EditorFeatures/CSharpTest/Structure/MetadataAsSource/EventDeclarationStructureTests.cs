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

public sealed class EventDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<EventDeclarationSyntax>
{
    protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
    internal override AbstractSyntaxStructureProvider CreateProvider() => new EventDeclarationStructureProvider();

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public Task NoCommentsOrAttributes()
        => VerifyBlockSpansAsync("""
                class Goo
                {
                    {|hint:public event EventArgs $$goo {|textspan:{ add; remove; }|}|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public Task WithAttributes()
        => VerifyBlockSpansAsync("""
                class Goo
                {
                    {|hint1:{|textspan1:[Goo]
                    |}{|hint2:public event EventArgs $$goo {|textspan2:{ add; remove; }|}|}|}
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
                    |}{|hint2:event EventArgs $$goo {|textspan2:{ add; remove; }|}|}|}
                }
                """,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public Task WithCommentsAttributesAndModifiers()
        => VerifyBlockSpansAsync("""
                class Goo
                {
                    {|hint1:{|textspan1:// Summary:
                    //     This is a summary.
                    [Goo]
                    |}{|hint2:public event EventArgs $$goo {|textspan2:{ add; remove; }|}|}|}
                }
                """,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
    public Task TestEvent3()
        => VerifyBlockSpansAsync("""
                class C
                {
                    $${|#0:event EventHandler E{|textspan:
                    {
                        add { }
                        remove { }
                    }|#0}
                |}
                    event EventHandler E2
                    {
                        add { }
                        remove { }
                    }
                }
                """,
            Region("textspan", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
}
