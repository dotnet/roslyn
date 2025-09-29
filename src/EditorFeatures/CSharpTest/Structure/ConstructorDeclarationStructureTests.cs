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
    public Task TestConstructor1()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public C(){|textspan:
                    {
                    }|}|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestConstructor2()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public C(){|textspan:
                    {
                    }                 |}|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestConstructor3()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public C(){|textspan:
                    {
                    }|}|} // .ctor
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestConstructor4()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public C(){|textspan:
                    {
                    }|}|} /* .ctor */
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestConstructor5()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public C() // .ctor{|textspan:
                    {
                    }|}|} // .ctor
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestConstructor6()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public C() /* .ctor */{|textspan:
                    {
                    }|}|} // .ctor
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68778")]
    public Task TestConstructor7()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public C(){|textspan:
                    // .ctor
                    {
                    }|}|} // .ctor
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68778")]
    public Task TestConstructor8()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public C(){|textspan:
                    /* .ctor */
                    {
                    }|}|} // .ctor
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestConstructor9()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestConstructor10()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestConstructorWithComments()
        => VerifyBlockSpansAsync("""
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

    [Fact]
    public Task TestConstructorMissingCloseParenAndBody()
        // Expected behavior is that the class should be outlined, but the constructor should not.
        => VerifyNoBlockSpansAsync("""
                class C
                {
                    $$C(
                }
                """);
}
