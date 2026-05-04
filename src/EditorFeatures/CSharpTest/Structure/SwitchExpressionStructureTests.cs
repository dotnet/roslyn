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
public sealed class SwitchExpressionStructureTests : AbstractCSharpSyntaxNodeStructureTests<SwitchExpressionSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new SwitchExpressionStructureProvider();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69357")]
    public Task TestSwitchExpression1()
        => VerifyBlockSpansAsync("""
            class C
            {
                void M(int i)
                {
                    var v = {|hint:$$i switch{|textspan:
                    {
                    }|}|}
                }
            }
            """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
}
