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
public sealed class DestructorDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<DestructorDeclarationSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new DestructorDeclarationStructureProvider();

    [Fact]
    public Task TestDestructor()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$~C(){|textspan:
                    {
                    }|}|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestDestructorWithComments()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|span1:// Goo
                    // Bar|}
                    {|hint2:$$~C(){|textspan2:
                    {
                    }|}|}
                }
                """,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestDestructorMissingCloseParenAndBody()
        // Expected behavior is that the class should be outlined, but the destructor should not.
        => VerifyNoBlockSpansAsync("""
                class C
                {
                    $$~C(
                }
                """);
}
