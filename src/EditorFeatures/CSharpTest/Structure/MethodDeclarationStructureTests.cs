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
public sealed class MethodDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<MethodDeclarationSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new MethodDeclarationStructureProvider();

    [Fact]
    public Task TestMethod1()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public string Goo(){|textspan:
                    {
                    }|}|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestMethod2()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public string Goo(){|textspan:
                    {
                    }|}|}
                    public string Goo2()
                    {
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestMethod3()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public string Goo(){|textspan:
                    {
                    }|}|}

                    public string Goo2()
                    {
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestMethod4()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public string Goo(){|textspan:
                    {
                    }|}|}

                    public string Goo2 => null;
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68778")]
    public Task TestMethod5()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public void Goo(){|textspan:
                    // .ctor
                    {
                    }|}|} // .ctor
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68778")]
    public Task TestMethod6()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public void Goo(){|textspan:
                    /* .ctor */
                    {
                    }|}|} // .ctor
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestMethodWithTrailingSpaces()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$public string Goo()    {|textspan:
                    {
                    }|}|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestMethodWithLeadingComments()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|span1:// Goo
                    // Bar|}
                    {|hint2:$$public string Goo(){|textspan2:
                    {
                    }|}|}
                }
                """,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestMethodWithWithExpressionBodyAndComments()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|span:// Goo
                    // Bar|}
                    $$public string Goo() => "Goo";
                }
                """,
            Region("span", "// Goo ...", autoCollapse: true));
}
