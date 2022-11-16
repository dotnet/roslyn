// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.NewLines.ConditionalExpressionPlacement;
using Microsoft.CodeAnalysis.CSharp.NewLines.ConstructorInitializerPlacement;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NewLines.ConditionalExpressionPlacement
{
    using Verify = CSharpCodeFixVerifier<
        ConditionalExpressionPlacementDiagnosticAnalyzer,
        ConditionalExpressionPlacementCodeFixProvider>;

    public class ConditionalExpressionPlacementTests
    {
        [Fact]
        public async Task TestNotWithOptionOff()
        {
            var code =
@"
class C
{
    public C()
    {
        var v = true ?
            0 :
            1;
    }
}";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterConditionalExpressionToken, CodeStyleOptions2.TrueWithSilentEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestBaseCase()
        {
            var code =
@"
class C
{
    public C()
    {
        var v = true [|?|]
            0 :
            1;
    }
}";

            var fixedCode =
@"
class C
{
    public C()
    {
        var v = true
            ? 0
            : 1;
    }
}";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterConditionalExpressionToken, CodeStyleOptions2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }
    }
}
