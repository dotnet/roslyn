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
public sealed class SwitchStatementStructureTests : AbstractCSharpSyntaxNodeStructureTests<SwitchStatementSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new SwitchStatementStructureProvider();

    [Fact]
    public Task TestSwitchStatement1()
        => VerifyBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        {|hint:$$switch (expr){|textspan:
                        {
                        }|}|}
                    }
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task TestSwitchStatement2()
        => VerifyBlockSpansAsync("""
                class C
                {
                    void M()
                    {
                        {|hint1:$$switch (expr){|textspan1:
                        {
                            {|hint2:case 0:{|textspan2:
                                if (true)
                                {
                                }
                                break;|}|}
                            {|hint3:default:{|textspan3:
                                if (false)
                                {
                                }
                                break;|}|}
                        }|}|}
                    }
                }
                """,
            Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: false),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: false),
            Region("textspan3", "hint3", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
}
