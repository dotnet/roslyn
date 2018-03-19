// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddRequiredParentheses;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddRequiredParentheses;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddRequiredParentheses
{
    public partial class AddRequiredParenthesesForCastExpressionTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpAddRequiredParenthesesForCastExpressionDiagnosticAnalyzer(), new AddRequiredParenthesesCodeFixProvider());
        
        private Task TestMissingAsync(string initialMarkup, IDictionary<OptionKey, object> options)
            => TestMissingInRegularAndScriptAsync(initialMarkup, new TestParameters(options: options));

        private Task TestAsync(string initialMarkup, string expected, IDictionary<OptionKey, object> options)
            => TestInRegularAndScript1Async(initialMarkup, expected, parameters: new TestParameters(options: options));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestUnclearCast1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (int)$$-y;
    }
}",
@"class C
{
    void M()
    {
        int x = (int)(-y);
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
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (int)$$+y;
    }
}",
@"class C
{
    void M()
    {
        int x = (int)(+y);
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestUnclearCast3()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (int)$$&y;
    }
}",
@"class C
{
    void M()
    {
        int x = (int)(&y);
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestUnclearCast4()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (int)$$*y;
    }
}",
@"class C
{
    void M()
    {
        int x = (int)(*y);
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
    }
}
