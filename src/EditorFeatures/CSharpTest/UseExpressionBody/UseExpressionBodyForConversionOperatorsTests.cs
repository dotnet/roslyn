// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    public class UseExpressionBodyForConversionOperatorsTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
            => new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new UseExpressionBodyForConversionOperatorsDiagnosticAnalyzer(),
                new UseExpressionBodyForConversionOperatorsCodeFixProvider());

        private static readonly Dictionary<OptionKey, object> UseExpressionBody =
            new Dictionary<OptionKey, object>
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CodeStyleOptions.TrueWithNoneEnforcement }
            };

        private static readonly Dictionary<OptionKey, object> UseBlockBody =
            new Dictionary<OptionKey, object>
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CodeStyleOptions.FalseWithNoneEnforcement }
            };

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody1()
        {
            await TestAsync(
@"class C
{
    public static implicit operator C(int i)
    {
        [|Bar|]();
    }
}",
@"class C
{
    public static implicit operator C(int i) => Bar();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody2()
        {
            await TestAsync(
@"class C
{
    public static implicit operator C(int i)
    {
        return [|Bar|]();
    }
}",
@"class C
{
    public static implicit operator C(int i) => Bar();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody3()
        {
            await TestAsync(
@"class C
{
    public static implicit operator C(int i)
    {
        [|throw|] new NotImplementedException();
    }
}",
@"class C
{
    public static implicit operator C(int i) => throw new NotImplementedException();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody4()
        {
            await TestAsync(
@"class C
{
    public static implicit operator C(int i)
    {
        [|throw|] new NotImplementedException(); // comment
    }
}",
@"class C
{
    public static implicit operator C(int i) => throw new NotImplementedException(); // comment
}", compareTokens: false, options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody1()
        {
            await TestAsync(
@"class C
{
    public static implicit operator C(int i) [|=>|] Bar();
}",
@"class C
{
    public static implicit operator C(int i)
    {
        return Bar();
    }
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody3()
        {
            await TestAsync(
@"class C
{
    public static implicit operator C(int i) [|=>|] throw new NotImplementedException();
}",
@"class C
{
    public static implicit operator C(int i)
    {
        throw new NotImplementedException();
    }
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody4()
        {
            await TestAsync(
@"class C
{
    public static implicit operator C(int i) [|=>|] throw new NotImplementedException(); // comment
}",
@"class C
{
    public static implicit operator C(int i)
    {
        throw new NotImplementedException(); // comment
    }
}", compareTokens: false, options: UseBlockBody);
        }
    }
}