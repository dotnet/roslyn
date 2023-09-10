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
public class PropertyDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<PropertyDeclarationSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new PropertyDeclarationStructureProvider();

    [Fact]
    public async Task TestProperty1()
    {
        var code = """
                class C
                {
                    {|hint:$$public int Goo{|textspan:
                    {
                        get { }
                        set { }
                    }|}|}
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestProperty2()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestProperty3()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestProperty4()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestProperty5()
    {
        var code = """
                class C
                {
                    {|hint:$$public int Goo{|textspan:
                    {
                        get { }
                        set { }
                    }|}|}

                    public event EventHandler Event;
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestProperty6()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertyWithLeadingComments()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertyWithWithExpressionBodyAndComments()
    {
        var code = """
                class C
                {
                    {|span:// Goo
                    // Bar|}
                    $$public int Goo => 0;
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "// Goo ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertyWithSpaceAfterIdentifier()
    {
        var code = """
                class C
                {
                    {|hint:$$public int Goo    {|textspan:
                    {
                        get { }
                        set { }
                    }|}|}
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }
}
