﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddRequiredParentheses;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddRequiredParentheses;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddRequiredParentheses
{
    public partial class AddRequiredPatternParenthesesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public AddRequiredPatternParenthesesTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpAddRequiredPatternParenthesesDiagnosticAnalyzer(), new AddRequiredParenthesesCodeFixProvider());

        private Task TestMissingAsync(string initialMarkup, OptionsCollection options)
            => TestMissingInRegularAndScriptAsync(initialMarkup, new TestParameters(options: options));

        private Task TestAsync(string initialMarkup, string expected, OptionsCollection options)
            => TestInRegularAndScript1Async(initialMarkup, expected, parameters: new TestParameters(options: options));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestLogicalPrecedence()
        {
            await TestAsync(
@"class C
{
    void M(object o)
    {
        object x = o is a or b $$and c;
    }
}",
@"class C
{
    void M(object o)
    {
        object x = o is a or (b and c);
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNoLogicalOnLowerPrecedence()
        {
            await TestMissingAsync(
@"class C
{
    void M(object o)
    {
        object x = o is a $$or b and c;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotIfLogicalPrecedenceStaysTheSame()
        {
            await TestMissingAsync(
@"class C
{
    void M(object o)
    {
        object x = o is a or b $$or c;
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotIfLogicalPrecedenceIsNotEnforced()
        {
            await TestMissingAsync(
@"class C
{
    void M(object o)
    {
        object x = o is a or b $$or c;
    }
}", RequireArithmeticBinaryParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestLogicalPrecedenceMultipleEqualPrecedenceParts1()
        {
            await TestAsync(
@"class C
{
    void M(object o)
    {
        object x = o is a or b $$and c and d;
    }
}",
@"class C
{
    void M(object o)
    {
        object x = o is a or (b and c and d);
    }
}", RequireAllParenthesesForClarity);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestLogicalPrecedenceMultipleEqualPrecedenceParts2()
        {
            await TestAsync(
@"class C
{
    void M(object o)
    {
        object x = o is a or b and c $$and d;
    }
}",
@"class C
{
    void M(object o)
    {
        object x = o is a or (b and c and d);
    }
}", RequireAllParenthesesForClarity);
        }
    }
}
