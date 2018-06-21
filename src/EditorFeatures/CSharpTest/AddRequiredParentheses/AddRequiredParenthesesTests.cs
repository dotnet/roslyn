// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddRequiredParentheses;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.AddRequiredParentheses;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddRequiredParentheses
{
    public partial class AddRequiredParenthesesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpAddRequiredParenthesesDiagnosticAnalyzer(), new AddRequiredParenthesesCodeFixProvider());
        
        private Task TestMissingAsync(string initialMarkup, IDictionary<OptionKey, object> options)
            => TestMissingInRegularAndScriptAsync(initialMarkup, new TestParameters(options: options));

        private Task TestAsync(string initialMarkup, string expected, IDictionary<OptionKey, object> options)
            => TestInRegularAndScript1Async(initialMarkup, expected, parameters: new TestParameters(options: options));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestArithmeticPrecedence()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = 1 + 2 $$* 3;
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
        public async Task TestNoArithmeticOnLowerPrecedence()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 $$+ 2 * 3;
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
        int x = 1 + 2 $$+ 3;
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
        int x = 1 + 2 $$+ 3;
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
        int x = 1 + 2 $$* 3;
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
        int x = a $$> b == c;
    }
}",
@"class C
{
    void M()
    {
        int x = (a > b) == c;
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
        int x = a || b $$&& c;
    }
}",
@"class C
{
    void M()
    {
        int x = a || (b && c);
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNoLogicalOnLowerPrecedence()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a $$|| b && c;
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
        int x = a || b $$|| c;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotIfLogicalPrecedenceIsNotEnforced()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a || b $$|| c;
    }
}", RequireArithmeticBinaryParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestMixedArithmeticAndLogical()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a == b $$&& c == d;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestLogicalPrecedenceMultipleEqualPrecedenceParts1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = a || b $$&& c && d;
    }
}",
@"class C
{
    void M()
    {
        int x = a || (b && c && d);
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestLogicalPrecedenceMultipleEqualPrecedenceParts2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = a || b && c $$&& d;
    }
}",
@"class C
{
    void M()
    {
        int x = a || (b && c && d);
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
        int x = 1 $$+ 2 << 3;
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
        int x = 1 $$+ 2 << 3;
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
        int x = 1 $$+ 2 << 3;
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
        int x = 1 $$<< 2 << 3;
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
        int x = 1 << 2 $$<< 3;
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
        int x = 1 $$+ 2 == 2 + 3;
    }
}", RequireOtherBinaryParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestEqualityPrecedence2()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 + 2 == 2 $$+ 3;
    }
}", RequireOtherBinaryParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestEqualityPrecedence3()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 $$+ 2 == 2 + 3;
    }
}", RequireRelationalBinaryParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestEqualityPrecedence4()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 + 2 == 2 $$+ 3;
    }
}", RequireRelationalBinaryParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestCoalescePrecedence1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a $$+ b ?? c;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestCoalescePrecedence2()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a $$?? b ?? c;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestCoalescePrecedence3()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a ?? b $$?? c;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestBitwisePrecedence1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = 1 $$+ 2 & 3;
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
        int x = a $$| b | c;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestBitwisePrecedence3()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = a | b $$& c;
    }
}",
@"class C
{
    void M()
    {
        int x = a | (b & c);
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestBitwisePrecedence4()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a $$| b & c;
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
        int x = 1 $$== 2;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotForAssignmentEqualsAfterLocal()
        {
            await TestMissingAsync(
@"class C
{
    void M(int a)
    {
        int x = a $$+= 2;
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
        x $$= y == z;
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
        x = y $$== z;
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
        int x = (int)$$-y;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestUnclearCast_NotOfferedWithIgnore()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (int)$$-y;
    }
}", IgnoreAllParentheses);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestUnclearCast_NotOfferedWithRemoveForClarity()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (int)$$-y;
    }
}", RemoveAllUnnecessaryParentheses);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestUnclearCast2()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (int)$$+y;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestUnclearCast3()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (int)$$&y;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestUnclearCast4()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (int)$$*y;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotForPrimary()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (int)$$y;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotForMemberAccess()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (int)$$y.z;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotForCastOfCast()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (int)$$(y);
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotForNonAmbiguousUnary()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (int)$$!y;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestFixAll1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        if (0 {|FixAllInDocument:>=|} 3 * 2 + 4)
        {
        }
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestFixAll2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (3 * 2 + 4 >= 3 {|FixAllInDocument:*|} 2 + 4)
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
}", options: RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestFixAll3()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        if (3 * 2 + 4 >= 3 * 2 {|FixAllInDocument:+|} 4)
        {
        }
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestSeams1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int x = 1 + 2 {|FixAllInDocument:*|} 3 == 1 + 2 * 3;
    }
}",
@"class C
{
    void M()
    {
        int x = 1 + (2 * 3) == 1 + (2 * 3);
    }
}", options: RequireAllParenthesesForClarity);
        }
    }
}
