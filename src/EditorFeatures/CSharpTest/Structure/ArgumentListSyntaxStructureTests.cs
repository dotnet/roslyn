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
public sealed class ArgumentListSyntaxStructureTests : AbstractCSharpSyntaxNodeStructureTests<ArgumentListSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new ArgumentListStructureProvider();

    [Fact]
    public Task TestInvocationExpressionSingleLine()
        => VerifyBlockSpansAsync("""
            var x = M$$();
            """);

    [Fact]
    public Task TestInvocationExpressionTwoArgumentsInTwoLines()
        => VerifyBlockSpansAsync("""
            var x = M$$("Hello",
                "World");
            """);

    [Fact]
    public Task TestInvocationExpressionThreeLines()
        => VerifyBlockSpansAsync("""
            var x = M$${|span:(
                "",
                "")|};
            """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public async Task TestTwoInvocationExpressionsThreeLines()
    {
        // The inner argument list should be collapsible, but the outer one shouldn't.
        // While this test shows both as collapsible, they will be deduplicated by AbstractBlockStructureProvider
        // This test only tests ArgumentListStructureProvider specifically, so it doesn't show the deduplication.
        // Tests in BlockStructureServiceTests show the overall behavior accurately.

        await VerifyBlockSpansAsync("""
            var x = M(M$${|span:(
                "",
                "")|});
            """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        await VerifyBlockSpansAsync("""
            var x = M$${|span:(M(
                "",
                ""))|};
            """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public Task TestObjectCreationSingleLine()
        => VerifyBlockSpansAsync("""
            var x = new C$$();
            """);

    [Fact]
    public Task TestObjectCreationThreeLines()
        => VerifyBlockSpansAsync("""
            var x = new C$${|span:(
                "",
                "")|};
            """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestImplicitObjectCreationSingleLine()
        => VerifyBlockSpansAsync("""
            C x = new$$();
            """);

    [Fact]
    public Task TestImplicitObjectCreationThreeLines()
        => VerifyBlockSpansAsync("""
            C x = new$${|span:(
                "",
                "")|};
            """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
}
