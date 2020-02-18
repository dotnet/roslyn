// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.AddRequiredParentheses.CSharpAddRequiredParenthesesDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.AddRequiredParentheses.AddRequiredParenthesesCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddRequiredParentheses
{
    public class AddRequiredParenthesesTests
    {
        private static IDictionary<OptionKey, object> RequireAllParenthesesForClarity
            => AbstractDiagnosticProviderBasedUserDiagnosticTest.RequireAllParenthesesForClarity(LanguageNames.CSharp);

        private static IDictionary<OptionKey, object> RequireOtherBinaryParenthesesForClarity
            => AbstractDiagnosticProviderBasedUserDiagnosticTest.RequireOtherBinaryParenthesesForClarity(LanguageNames.CSharp);

        private static IDictionary<OptionKey, object> RequireArithmeticBinaryParenthesesForClarity
            => AbstractDiagnosticProviderBasedUserDiagnosticTest.RequireArithmeticBinaryParenthesesForClarity(LanguageNames.CSharp);

        private static IDictionary<OptionKey, object> RequireRelationalBinaryParenthesesForClarity
            => AbstractDiagnosticProviderBasedUserDiagnosticTest.RequireRelationalBinaryParenthesesForClarity(LanguageNames.CSharp);

        private static IDictionary<OptionKey, object> IgnoreAllParentheses
            => AbstractDiagnosticProviderBasedUserDiagnosticTest.IgnoreAllParentheses(LanguageNames.CSharp);

        private static IDictionary<OptionKey, object> RemoveAllUnnecessaryParentheses
            => AbstractDiagnosticProviderBasedUserDiagnosticTest.RemoveAllUnnecessaryParentheses(LanguageNames.CSharp);

        private async Task TestMissingAsync(string initialMarkup, IDictionary<OptionKey, object> options, params DiagnosticResult[] expected)
        {
            await TestAsync(initialMarkup, expected: initialMarkup, options, expected);
        }

        private async Task TestAsync(string initialMarkup, string expected, IDictionary<OptionKey, object> options, params DiagnosticResult[] expectedDiagnostics)
        {
            var test = new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expected,
            };

            test.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            foreach (var (key, value) in options)
            {
                test.Options.Add(key, value);
            }

            await test.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public void TestStandardProperties()
            => VerifyCS.VerifyStandardProperties();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestArithmeticPrecedence()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = 1 + 2 [|*|] 3;
    }
}",
@"class C
{
    void M()
    {
        int x = 1 + (2 * 3);
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotIfArithmeticPrecedenceStaysTheSame()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 + 2 + 3;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotIfArithmeticPrecedenceIsNotEnforced1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 + 2 + 3;
    }
}", RequireOtherBinaryParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotIfArithmeticPrecedenceIsNotEnforced2()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 + 2 * 3;
    }
}", RequireOtherBinaryParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestRelationalPrecedence()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = {|CS0103:a|} [|>|] {|CS0103:b|} == {|CS0103:c|};
    }
}",
@"class C
{
    void M()
    {
        int x = ({|CS0103:a|} > {|CS0103:b|}) == {|CS0103:c|};
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestLogicalPrecedence()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = {|CS0103:a|} || {|CS0103:b|} [|&&|] {|CS0103:c|};
    }
}",
@"class C
{
    void M()
    {
        int x = {|CS0103:a|} || ({|CS0103:b|} && {|CS0103:c|});
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotIfLogicalPrecedenceStaysTheSame()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a || b || c;
    }
}", RequireAllParenthesesForClarity,
                // Test0.cs(5,17): error CS0103: The name 'a' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 17, 5, 18).WithArguments("a"),
                // Test0.cs(5,22): error CS0103: The name 'b' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 22, 5, 23).WithArguments("b"),
                // Test0.cs(5,27): error CS0103: The name 'c' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 27, 5, 28).WithArguments("c"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotIfLogicalPrecedenceIsNotEnforced()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a || b || c;
    }
}", RequireArithmeticBinaryParenthesesForClarity,
                // Test0.cs(5,17): error CS0103: The name 'a' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 17, 5, 18).WithArguments("a"),
                // Test0.cs(5,22): error CS0103: The name 'b' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 22, 5, 23).WithArguments("b"),
                // Test0.cs(5,27): error CS0103: The name 'c' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 27, 5, 28).WithArguments("c"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestMixedArithmeticAndLogical()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a == b && c == d;
    }
}", RequireAllParenthesesForClarity,
                // Test0.cs(5,17): error CS0103: The name 'a' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 17, 5, 18).WithArguments("a"),
                // Test0.cs(5,22): error CS0103: The name 'b' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 22, 5, 23).WithArguments("b"),
                // Test0.cs(5,27): error CS0103: The name 'c' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 27, 5, 28).WithArguments("c"),
                // Test0.cs(5,32): error CS0103: The name 'd' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 32, 5, 33).WithArguments("d"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestLogicalPrecedenceMultipleEqualPrecedenceParts1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = {|CS0103:a|} || {|CS0103:b|} [|&&|] {|CS0103:c|} [|&&|] {|CS0103:d|};
    }
}",
@"class C
{
    void M()
    {
        int x = {|CS0103:a|} || ({|CS0103:b|} && {|CS0103:c|} && {|CS0103:d|});
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestShiftPrecedence1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = 1 [|+|] 2 << 3;
    }
}",
@"class C
{
    void M()
    {
        int x = (1 + 2) << 3;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestShiftPrecedence2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = 1 [|+|] 2 << 3;
    }
}",
@"class C
{
    void M()
    {
        int x = (1 + 2) << 3;
    }
}", RequireArithmeticBinaryParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestShiftPrecedence3()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 + 2 << 3;
    }
}", RequireOtherBinaryParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotIfShiftPrecedenceStaysTheSame1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 << 2 << 3;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotIfShiftPrecedenceStaysTheSame2()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 << 2 << 3;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestEqualityPrecedence1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 + 2 == 2 + 3;
    }
}", RequireOtherBinaryParenthesesForClarity,
                // Test0.cs(5,17): error CS0029: Cannot implicitly convert type 'bool' to 'int'
                DiagnosticResult.CompilerError("CS0029").WithSpan(5, 17, 5, 31).WithArguments("bool", "int"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestEqualityPrecedence2()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 + 2 == 2 + 3;
    }
}", RequireRelationalBinaryParenthesesForClarity,
                // Test0.cs(5,17): error CS0029: Cannot implicitly convert type 'bool' to 'int'
                DiagnosticResult.CompilerError("CS0029").WithSpan(5, 17, 5, 31).WithArguments("bool", "int"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestCoalescePrecedence1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a + b ?? c;
    }
}", RequireAllParenthesesForClarity,
                // Test0.cs(5,17): error CS0103: The name 'a' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 17, 5, 18).WithArguments("a"),
                // Test0.cs(5,21): error CS0103: The name 'b' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 21, 5, 22).WithArguments("b"),
                // Test0.cs(5,26): error CS0103: The name 'c' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 26, 5, 27).WithArguments("c"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestCoalescePrecedence2()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a ?? b ?? c;
    }
}", RequireAllParenthesesForClarity,
                // Test0.cs(5,17): error CS0103: The name 'a' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 17, 5, 18).WithArguments("a"),
                // Test0.cs(5,22): error CS0103: The name 'b' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 22, 5, 23).WithArguments("b"),
                // Test0.cs(5,27): error CS0103: The name 'c' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 27, 5, 28).WithArguments("c"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestBitwisePrecedence1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = 1 [|+|] 2 & 3;
    }
}",
@"class C
{
    void M()
    {
        int x = (1 + 2) & 3;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestBitwisePrecedence2()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a | b | c;
    }
}", RequireAllParenthesesForClarity,
                // Test0.cs(5,17): error CS0103: The name 'a' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 17, 5, 18).WithArguments("a"),
                // Test0.cs(5,21): error CS0103: The name 'b' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 21, 5, 22).WithArguments("b"),
                // Test0.cs(5,25): error CS0103: The name 'c' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 25, 5, 26).WithArguments("c"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestBitwisePrecedence3()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = {|CS0103:a|} | {|CS0103:b|} [|&|] {|CS0103:c|};
    }
}",
@"class C
{
    void M()
    {
        int x = {|CS0103:a|} | ({|CS0103:b|} & {|CS0103:c|});
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotForEqualityAfterEquals()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 == 2;
    }
}", RequireAllParenthesesForClarity,
                // Test0.cs(5,17): error CS0029: Cannot implicitly convert type 'bool' to 'int'
                DiagnosticResult.CompilerError("CS0029").WithSpan(5, 17, 5, 23).WithArguments("bool", "int"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotForAssignmentEqualsAfterLocal()
        {
            await TestMissingAsync(
@"class C
{
    void M(int a)
    {
        int x = a += 2;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestForAssignmentAndEquality1()
        {
            await TestMissingAsync(
@"class C
{
    void M(bool x, bool y, bool z)
    {
        x = y == z;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestMissingForAssignmentAndEquality2()
        {
            await TestMissingAsync(
@"class C
{
    void M(bool x, bool y, bool z)
    {
        x = y == z;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestUnclearCast1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (int)-y;
    }
}", RequireAllParenthesesForClarity,
                // Test0.cs(5,23): error CS0103: The name 'y' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 23, 5, 24).WithArguments("y"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestUnclearCast_NotOfferedWithIgnore()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (int)-y;
    }
}", IgnoreAllParentheses,
                // Test0.cs(5,23): error CS0103: The name 'y' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 23, 5, 24).WithArguments("y"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestUnclearCast_NotOfferedWithRemoveForClarity()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (int)-y;
    }
}", RemoveAllUnnecessaryParentheses,
                // Test0.cs(5,23): error CS0103: The name 'y' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 23, 5, 24).WithArguments("y"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestUnclearCast2()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (int)+y;
    }
}", RequireAllParenthesesForClarity,
                // Test0.cs(5,23): error CS0103: The name 'y' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 23, 5, 24).WithArguments("y"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestUnclearCast3()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (int)&y;
    }
}", RequireAllParenthesesForClarity,
                // Test0.cs(5,23): error CS0103: The name 'y' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 23, 5, 24).WithArguments("y"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestUnclearCast4()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (int)*y;
    }
}", RequireAllParenthesesForClarity,
                // Test0.cs(5,23): error CS0103: The name 'y' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 23, 5, 24).WithArguments("y"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotForPrimary()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (int)y;
    }
}", RequireAllParenthesesForClarity,
                // Test0.cs(5,22): error CS0103: The name 'y' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 22, 5, 23).WithArguments("y"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotForMemberAccess()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (int)y.z;
    }
}", RequireAllParenthesesForClarity,
                // Test0.cs(5,22): error CS0103: The name 'y' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 22, 5, 23).WithArguments("y"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotForCastOfCast()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (int)(y);
    }
}", RequireAllParenthesesForClarity,
                // Test0.cs(5,23): error CS0103: The name 'y' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 23, 5, 24).WithArguments("y"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotForNonAmbiguousUnary()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (int)!y;
    }
}", RequireAllParenthesesForClarity,
                // Test0.cs(5,23): error CS0103: The name 'y' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 23, 5, 24).WithArguments("y"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestFixAll1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        if (0 >= 3 [|*|] 2 + 4)
        {
        }
    }
}",
@"class C
{
    void M()
    {
        if (0 >= (3 * 2) + 4)
        {
        }
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestFixAll2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        if (3 [|*|] 2 + 4 >= 3 [|*|] 2 + 4)
        {
        }
    }
}",
@"class C
{
    void M()
    {
        if ((3 * 2) + 4 >= (3 * 2) + 4)
        {
        }
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestSeams1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = {|CS0029:1 + 2 [|*|] 3 == 1 + 2 [|*|] 3|};
    }
}",
@"class C
{
    void M()
    {
        int x = {|CS0029:1 + (2 * 3) == 1 + (2 * 3)|};
    }
}", RequireAllParenthesesForClarity);
        }
    }
}
