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
public class ConstructorDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<ConstructorDeclarationSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new ConstructorDeclarationStructureProvider();

    [Fact]
    public async Task TestConstructor1()
    {
        var code = """
                class C
                {
                    {|hint:$$public C(){|textspan:
                    {
                    }|}|}
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestConstructor2()
    {
        var code = """
                class C
                {
                    {|hint:$$public C(){|textspan:
                    {
                    }                 |}|}
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestConstructor3()
    {
        var code = """
                class C
                {
                    {|hint:$$public C(){|textspan:
                    {
                    }|}|} // .ctor
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestConstructor4()
    {
        var code = """
                class C
                {
                    {|hint:$$public C(){|textspan:
                    {
                    }|}|} /* .ctor */
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestConstructor5()
    {
        var code = """
                class C
                {
                    {|hint:$$public C() // .ctor{|textspan:
                    {
                    }|}|} // .ctor
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestConstructor6()
    {
        var code = """
                class C
                {
                    {|hint:$$public C() /* .ctor */{|textspan:
                    {
                    }|}|} // .ctor
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68778")]
    public async Task TestConstructor7()
    {
        var code = """
                class C
                {
                    {|hint:$$public C(){|textspan:
                    // .ctor
                    {
                    }|}|} // .ctor
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68778")]
    public async Task TestConstructor8()
    {
        var code = """
                class C
                {
                    {|hint:$$public C(){|textspan:
                    /* .ctor */
                    {
                    }|}|} // .ctor
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestConstructor9()
    {
        var code = """
                class C
                {
                    {|hint:$$public C(){|textspan:
                    {
                    }|}|}
                    public C()
                    {
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestConstructor10()
    {
        var code = """
                class C
                {
                    {|hint:$$public C(){|textspan:
                    {
                    }|}|}

                    public C(int x)
                    {
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestConstructorWithComments()
    {
        var code = """
                class C
                {
                    {|span1:// Goo
                    // Bar|}
                    {|hint2:$$public C(){|textspan2:
                    {
                    }|}|} // .ctor
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestConstructorMissingCloseParenAndBody()
    {
        // Expected behavior is that the class should be outlined, but the constructor should not.

        var code = """
                class C
                {
                    $$C(
                }
                """;

        await VerifyNoBlockSpansAsync(code);
    }
}
