﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryParentheses
{
    public partial class RemoveUnnecessaryPatternParenthesesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public RemoveUnnecessaryPatternParenthesesTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpRemoveUnnecessaryPatternParenthesesDiagnosticAnalyzer(), new CSharpRemoveUnnecessaryParenthesesCodeFixProvider());

        private async Task TestAsync(string initial, string expected, bool offeredWhenRequireForClarityIsEnabled, int index = 0)
        {
            await TestInRegularAndScriptAsync(initial, expected, options: RemoveAllUnnecessaryParentheses, index: index);

            if (offeredWhenRequireForClarityIsEnabled)
            {
                await TestInRegularAndScriptAsync(initial, expected, options: RequireAllParenthesesForClarity, index: index);
            }
            else
            {
                await TestMissingAsync(initial, parameters: new TestParameters(options: RequireAllParenthesesForClarity));
            }
        }

        internal override bool ShouldSkipMessageDescriptionVerification(DiagnosticDescriptor descriptor)
            => descriptor.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary) && descriptor.DefaultSeverity == DiagnosticSeverity.Hidden;

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestArithmeticRequiredForClarity2()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(object o)
    {
        bool x = o is a or $$(b and c);
    }
}",
@"class C
{
    void M(object o)
    {
        bool x = o is a or b and c;
    }
}", parameters: new TestParameters(options: RequireArithmeticBinaryParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestLogicalRequiredForClarity1()
        {
            await TestMissingAsync(
@"class C
{
    void M(object o)
    {
        bool x = o is a or $$(b and c);
    }
}", new TestParameters(options: RequireOtherBinaryParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestLogicalNotRequiredForClarityWhenPrecedenceStaysTheSame1()
        {
            await TestAsync(
@"class C
{
    void M(object o)
    {
        bool x = o is a or $$(b or c);
    }
}",
@"class C
{
    void M(object o)
    {
        bool x = o is a or b or c;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestLogicalNotRequiredForClarityWhenPrecedenceStaysTheSame2()
        {
            await TestAsync(
@"class C
{
    void M(object o)
    {
        bool x = o is $$(a or b) or c;
    }
}",
@"class C
{
    void M(object o)
    {
        bool x = o is a or b or c;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestAlwaysUnnecessaryForIsPattern()
        {
            await TestAsync(
@"class C
{
    void M(object o)
    {
        bool x = o is $$(a or b);
    }
}",
@"class C
{
    void M(object o)
    {
        bool x = o is a or b;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestAlwaysUnnecessaryForCasePattern()
        {
            await TestAsync(
@"class C
{
    void M(object o)
    {
        switch (o)
        {
            case $$(a or b):
                return;
        }
    }
}",
@"class C
{
    void M(object o)
    {
        switch (o)
        {
            case a or b:
                return;
        }
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestAlwaysUnnecessaryForSwitchArmPattern()
        {
            await TestAsync(
@"class C
{
    int M(object o)
    {
        return o switch
        {
            $$(a or b) => 0,
        };
    }
}",
@"class C
{
    int M(object o)
    {
        return o switch
        {
            a or b => 0,
        };
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestAlwaysUnnecessaryForSubPattern()
        {
            await TestAsync(
@"class C
{
    void M(object o)
    {
        bool x = o is { X: $$(a or b) };
    }
}",
@"class C
{
    void M(object o)
    {
        bool x = o is { X: a or b };
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestNotAlwaysUnnecessaryForUnaryPattern1()
        {
            await TestAsync(
@"class C
{
    void M(object o)
    {
        bool x = o is a or $$(not b);
    }
}",
@"class C
{
    void M(object o)
    {
        bool x = o is a or not b;
    }
}", offeredWhenRequireForClarityIsEnabled: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestNotAlwaysUnnecessaryForUnaryPattern2()
        {
            await TestAsync(
@"class C
{
    void M(object o)
    {
        bool x = o is $$(not a) or b;
    }
}",
@"class C
{
    void M(object o)
    {
        bool x = o is not a or b;
    }
}", offeredWhenRequireForClarityIsEnabled: false);
        }
    }
}
