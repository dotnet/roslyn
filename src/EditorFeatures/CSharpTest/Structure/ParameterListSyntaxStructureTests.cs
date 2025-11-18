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
public sealed class ParameterListSyntaxStructureTests : AbstractCSharpSyntaxNodeStructureTests<ParameterListSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new ParameterListStructureProvider();

    [Fact]
    public Task TestMethodDeclarationSingleLine()
        => VerifyBlockSpansAsync("""
            void M$$()
            {
            }
            """);

    [Fact]
    public Task TestMethodDeclarationTwoParametersInTwoLines()
        => VerifyBlockSpansAsync("""
            void M$$(string a,
                string b)
            {
            }
            """);

    [Fact]
    public Task TestMethodDeclarationThreeLines()
        => VerifyBlockSpansAsync("""
            void M$${|span:(
                string a,
                string b)|} 
            {
            }
            """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestConstructorDeclarationSingleLine()
        => VerifyBlockSpansAsync("""
            class C
            {
                C$$() { }
            }
            """);

    [Fact]
    public Task TestConstructorDeclarationThreeLines()
        => VerifyBlockSpansAsync("""
            class C
            {
                C$${|span:(
                    string a,
                    string b)|} { }
            }
            """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestDelegateDeclarationSingleLine()
        => VerifyBlockSpansAsync("""
            delegate void D$$();
            """);

    [Fact]
    public Task TestDelegateDeclarationThreeLines()
        => VerifyBlockSpansAsync("""
            delegate void D$${|span:(
                string a,
                string b)|};
            """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestLambdaExpressionSingleLine()
        => VerifyBlockSpansAsync("""
            var x = $$() => { };
            """);

    [Fact]
    public Task TestLambdaExpressionThreeLines()
        => VerifyBlockSpansAsync("""
            var x = $${|span:(
                string a,
                string b)|} => { };
            """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestAnonymousMethodSingleLine()
        => VerifyBlockSpansAsync("""
            var x = delegate$$() { };
            """);

    [Fact]
    public Task TestAnonymousMethodThreeLines()
        => VerifyBlockSpansAsync("""
            var x = delegate$${|span:(
                string a,
                string b)|} { };
            """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestLocalFunctionSingleLine()
        => VerifyBlockSpansAsync("""
            void M()
            {
                void LocalFunction$$() { }
            }
            """);

    [Fact]
    public Task TestLocalFunctionThreeLines()
        => VerifyBlockSpansAsync("""
            void M()
            {
                void LocalFunction$${|span:(
                    string a,
                    string b)|} { }
            }
            """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
}
