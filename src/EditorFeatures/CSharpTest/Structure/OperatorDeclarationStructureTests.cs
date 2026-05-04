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
public sealed class OperatorDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<OperatorDeclarationSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new OperatorDeclarationStructureProvider();

    [Fact]
    public Task TestOperator1()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public static int operator +(int i){|textspan:
                    {
                    }|}|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestOperator2()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public static int operator +(int i){|textspan:
                    {
                    }|}|}
                    public static int operator -(int i)
                    {
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestOperator3()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public static int operator +(int i){|textspan:
                    {
                    }|}|}

                    public static int operator -(int i)
                    {
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestOperator4()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public static int operator +(int i){|textspan:
                    {
                    }|}|}
                    public static explicit operator C(int i)
                    {
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestOperator5()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public static int operator +(int i){|textspan:
                    {
                    }|}|}
                    public static explicit operator C(int i)
                    {
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestOperatorWithLeadingComments()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|span1:// Goo
                    // Bar|}
                    {|hint2:$$public static int operator +(int i){|textspan2:
                    {
                    }|}|}
                }
                """,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
}
