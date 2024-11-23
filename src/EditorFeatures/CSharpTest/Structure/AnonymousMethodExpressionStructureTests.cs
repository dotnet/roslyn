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
public class AnonymousMethodExpressionStructureTests : AbstractCSharpSyntaxNodeStructureTests<AnonymousMethodExpressionSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new AnonymousMethodExpressionStructureProvider();

    [Fact]
    public async Task TestAnonymousMethod()
    {
        var code = """
                class C
                {
                    void Main()
                    {
                        $${|hint:delegate {|textspan:{
                            x();
                        };|}|}
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestAnonymousMethodInForLoop()
    {
        var code = """
                class C
                {
                    void Main()
                    {
                        for (Action a = $$delegate { }; true; a()) { }
                    }
                }
                """;

        await VerifyNoBlockSpansAsync(code);
    }

    [Fact]
    public async Task TestAnonymousMethodInMethodCall1()
    {
        var code = """
                class C
                {
                    void Main()
                    {
                        someMethod(42, "test", false, {|hint:$$delegate(int x, int y, int z) {|textspan:{
                            return x + y + z;
                        }|}|}, "other arguments");
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task TestAnonymousMethodInMethodCall2()
    {
        var code = """
                class C
                {
                    void Main()
                    {
                        someMethod(42, "test", false, {|hint:$$delegate(int x, int y, int z) {|textspan:{
                            return x + y + z;
                        }|}|});
                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }
}
