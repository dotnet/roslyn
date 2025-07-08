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
public sealed class ConstructorDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<ConstructorDeclarationSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new ConstructorDeclarationStructureProvider();

    [Fact]
    public async Task TestConstructor1()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public C(){|textspan:
                    {
                    }|}|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestConstructor2()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public C(){|textspan:
                    {
                    }                 |}|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestConstructor3()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public C(){|textspan:
                    {
                    }|}|} // .ctor
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestConstructor4()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public C(){|textspan:
                    {
                    }|}|} /* .ctor */
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestConstructor5()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public C() // .ctor{|textspan:
                    {
                    }|}|} // .ctor
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestConstructor6()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public C() /* .ctor */{|textspan:
                    {
                    }|}|} // .ctor
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68778")]
    public async Task TestConstructor7()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public C(){|textspan:
                    // .ctor
                    {
                    }|}|} // .ctor
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68778")]
    public async Task TestConstructor8()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public C(){|textspan:
                    /* .ctor */
                    {
                    }|}|} // .ctor
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestConstructor9()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public C(){|textspan:
                    {
                    }|}|}
                    public C()
                    {
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestConstructor10()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public C(){|textspan:
                    {
                    }|}|}

                    public C(int x)
                    {
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestConstructorWithComments()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    {|span1:// Goo
                    // Bar|}
                    {|hint2:$$public C(){|textspan2:
                    {
                    }|}|} // .ctor
                }
                """,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestConstructorMissingCloseParenAndBody()
    {
        // Expected behavior is that the class should be outlined, but the constructor should not.


        await VerifyNoBlockSpansAsync("""
                class C
                {
                    $$C(
                }
                """);
    }
}
