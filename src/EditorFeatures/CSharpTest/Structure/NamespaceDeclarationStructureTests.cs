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
public sealed class NamespaceDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<NamespaceDeclarationSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new NamespaceDeclarationStructureProvider();

    [Fact]
    public Task TestNamespace()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$namespace N{|textspan:
                    {
                    }|}|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestNamespaceWithLeadingComments()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|span1:// Goo
                    // Bar|}
                    {|hint2:$$namespace N{|textspan2:
                    {
                    }|}|}
                }
                """,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestNamespaceWithNestedUsings()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint1:$$namespace N{|textspan1:
                    {
                        {|hint2:using {|textspan2:System;
                        using System.Linq;|}|}
                    }|}|}
                }
                """,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: false),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestNamespaceWithNestedUsingsWithLeadingComments()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint1:$$namespace N{|textspan1:
                    {
                        {|span2:// Goo
                        // Bar|}
                        {|hint3:using {|textspan3:System;
                        using System.Linq;|}|}
                    }|}|}
                }
                """,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: false),
            Region("span2", "// Goo ...", autoCollapse: true),
            Region("textspan3", "hint3", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestNamespaceWithNestedComments()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint1:$$namespace N{|textspan1:
                    {
                        {|span2:// Goo
                        // Bar|}
                    }|}|}
                }
                """,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: false),
            Region("span2", "// Goo ...", autoCollapse: true));
}
