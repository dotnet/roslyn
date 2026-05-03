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
public sealed class AnonymousMethodExpressionStructureTests : AbstractCSharpSyntaxNodeStructureTests<AnonymousMethodExpressionSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new AnonymousMethodExpressionStructureProvider();

    [Fact]
    public Task TestAnonymousMethod()
        => VerifyBlockSpansAsync("""
                class C
                {
                    void Main()
                    {
                        $${|hint:delegate {|textspan:{
                            x();
                        };|}|}
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestAnonymousMethodInForLoop()
        => VerifyNoBlockSpansAsync("""
                class C
                {
                    void Main()
                    {
                        for (Action a = $$delegate { }; true; a()) { }
                    }
                }
                """);

    [Fact]
    public Task TestAnonymousMethodInMethodCall1()
        => VerifyBlockSpansAsync("""
                class C
                {
                    void Main()
                    {
                        someMethod(42, "test", false, {|hint:$$delegate(int x, int y, int z) {|textspan:{
                            return x + y + z;
                        }|}|}, "other arguments");
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestAnonymousMethodInMethodCall2()
        => VerifyBlockSpansAsync("""
                class C
                {
                    void Main()
                    {
                        someMethod(42, "test", false, {|hint:$$delegate(int x, int y, int z) {|textspan:{
                            return x + y + z;
                        }|}|});
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
}
