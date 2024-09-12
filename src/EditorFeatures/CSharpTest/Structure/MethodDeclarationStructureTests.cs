// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure;

[Trait(Traits.Feature, Traits.Features.Outlining)]
public class MethodDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<MethodDeclarationSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new MethodDeclarationStructureProvider();

    [Fact]
    public async Task TestMethod1()
    {
        var code = """
                class C
                {
                    {|hint:$$public string Goo(){|textspan:
                    {
                    }|}|}
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestMethod2()
    {
        var code = """
                class C
                {
                    {|hint:$$public string Goo(){|textspan:
                    {
                    }|}|}
                    public string Goo2()
                    {
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestMethod3()
    {
        var code = """
                class C
                {
                    {|hint:$$public string Goo(){|textspan:
                    {
                    }|}|}

                    public string Goo2()
                    {
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestMethod4()
    {
        var code = """
                class C
                {
                    {|hint:$$public string Goo(){|textspan:
                    {
                    }|}|}

                    public string Goo2 => null;
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68778")]
    public async Task TestMethod5()
    {
        var code = """
                class C
                {
                    {|hint:$$public void Goo(){|textspan:
                    // .ctor
                    {
                    }|}|} // .ctor
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68778")]
    public async Task TestMethod6()
    {
        var code = """
                class C
                {
                    {|hint:$$public void Goo(){|textspan:
                    /* .ctor */
                    {
                    }|}|} // .ctor
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestMethodWithTrailingSpaces()
    {
        var code = """
                class C
                {
                    {|hint:$$public string Goo()    {|textspan:
                    {
                    }|}|}
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestMethodWithLeadingComments()
    {
        var code = """
                class C
                {
                    {|span1:// Goo
                    // Bar|}
                    {|hint2:$$public string Goo(){|textspan2:
                    {
                    }|}|}
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestMethodWithWithExpressionBodyAndComments()
    {
        var code = """
                class C
                {
                    {|span:// Goo
                    // Bar|}
                    $$public string Goo() => "Goo";
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "// Goo ...", autoCollapse: true));
    }
}
