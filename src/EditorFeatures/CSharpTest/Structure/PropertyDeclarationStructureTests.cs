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
public sealed class PropertyDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<PropertyDeclarationSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new PropertyDeclarationStructureProvider();

    [Fact]
    public Task TestProperty1()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public int Goo{|textspan:
                    {
                        get { }
                        set { }
                    }|}|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestProperty2()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public int Goo{|textspan:
                    {
                        get { }
                        set { }
                    }|}|}
                    public int Goo2
                    {
                        get { }
                        set { }
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestProperty3()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public int Goo{|textspan:
                    {
                        get { }
                        set { }
                    }|}|}

                    public int Goo2
                    {
                        get { }
                        set { }
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestProperty4()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public int Goo{|textspan:
                    {
                        get { }
                        set { }
                    }|}|}

                    public int this[int value]
                    {
                        get { }
                        set { }
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestProperty5()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public int Goo{|textspan:
                    {
                        get { }
                        set { }
                    }|}|}

                    public event EventHandler Event;
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestProperty6()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public int Goo{|textspan:
                    {
                        get { }
                        set { }
                    }|}|}

                    public event EventHandler Event
                    {
                        add { }
                        remove { }
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestPropertyWithLeadingComments()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|span1:// Goo
                    // Bar|}
                    {|hint2:$$public int Goo{|textspan2:
                    {
                        get { }
                        set { }
                    }|}|}
                }
                """,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestPropertyWithWithExpressionBodyAndComments()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|span:// Goo
                    // Bar|}
                    $$public int Goo => 0;
                }
                """,
            Region("span", "// Goo ...", autoCollapse: true));

    [Fact]
    public Task TestPropertyWithSpaceAfterIdentifier()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public int Goo    {|textspan:
                    {
                        get { }
                        set { }
                    }|}|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
}
