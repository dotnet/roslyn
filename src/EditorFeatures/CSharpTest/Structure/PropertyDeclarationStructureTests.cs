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
    public async Task TestProperty1()
    {
        await VerifyBlockSpansAsync("""
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
    }

    [Fact]
    public async Task TestProperty2()
    {
        await VerifyBlockSpansAsync("""
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
    }

    [Fact]
    public async Task TestProperty3()
    {
        await VerifyBlockSpansAsync("""
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
    }

    [Fact]
    public async Task TestProperty4()
    {
        await VerifyBlockSpansAsync("""
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
    }

    [Fact]
    public async Task TestProperty5()
    {
        await VerifyBlockSpansAsync("""
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
    }

    [Fact]
    public async Task TestProperty6()
    {
        await VerifyBlockSpansAsync("""
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
    }

    [Fact]
    public async Task TestPropertyWithLeadingComments()
    {
        await VerifyBlockSpansAsync("""
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
    }

    [Fact]
    public async Task TestPropertyWithWithExpressionBodyAndComments()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    {|span:// Goo
                    // Bar|}
                    $$public int Goo => 0;
                }
                """,
            Region("span", "// Goo ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertyWithSpaceAfterIdentifier()
    {
        await VerifyBlockSpansAsync("""
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
}
