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
    public class UseExpressionBodyForPropertiesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
            => new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new UseExpressionBodyForPropertiesDiagnosticAnalyzer(),
                new UseExpressionBodyForPropertiesCodeFixProvider());

        private static readonly Dictionary<OptionKey, object> UseExpressionBody =
            new Dictionary<OptionKey, object>
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CodeStyleOptions.TrueWithNoneEnforcement },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CodeStyleOptions.FalseWithNoneEnforcement },
            };

        private static readonly Dictionary<OptionKey, object> UseBlockBody =
            new Dictionary<OptionKey, object>
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CodeStyleOptions.FalseWithNoneEnforcement },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CodeStyleOptions.FalseWithNoneEnforcement }
            };

        private static readonly Dictionary<OptionKey, object> UseBlockBodyExceptAccessor =
            new Dictionary<OptionKey, object>
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CodeStyleOptions.FalseWithNoneEnforcement },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CodeStyleOptions.TrueWithNoneEnforcement }
            };

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody1()
        {
            await TestAsync(
@"class C
{
    int Foo
    {
        get
        {
            [|return|] Bar();
        }
    }
}",
@"class C
{
    int Foo => Bar();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestMissingWithSetter()
        {
            await TestMissingAsync(
@"class C
{
    int Foo
    {
        get
        {
            [|return|] Bar();
        }

        set
        {
        }
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestMissingWithAttribute()
        {
            await TestMissingAsync(
@"class C
{
    int Foo
    {
        [A]
        get
        {
            [|return|] Bar();
        }
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestMissingOnSetter1()
        {
            await TestMissingAsync(
@"class C
{
    int Foo
    {
        set
        {
            [|Bar|]();
        }
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody3()
        {
            await TestAsync(
@"class C
{
    int Foo
    {
        get
        {
            [|throw|] new NotImplementedException();
        }
    }
}",
@"class C
{
    int Foo => throw new NotImplementedException();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody4()
        {
            await TestAsync(
@"class C
{
    int Foo
    {
        get
        {
            [|throw|] new NotImplementedException(); // comment
        }
    }
}",
@"class C
{
    int Foo => throw new NotImplementedException(); // comment
}", compareTokens: false, options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody1()
        {
            await TestAsync(
@"class C
{
    int Foo [|=>|] Bar();
}",
@"class C
{
    int Foo
    {
        get
        {
            return Bar();
        }
    }
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyIfAccessorWantExpression1()
        {
            await TestAsync(
@"class C
{
    int Foo [|=>|] Bar();
}",
@"class C
{
    int Foo
    {
        get => Bar();
        }
    }", options: UseBlockBodyExceptAccessor);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody3()
        {
            await TestAsync(
@"class C
{
    int Foo [|=>|] throw new NotImplementedException();
}",
@"class C
{
    int Foo
    {
        get
        {
            throw new NotImplementedException();
        }
    }
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody4()
        {
            await TestAsync(
@"class C
{
    int Foo [|=>|] throw new NotImplementedException(); // comment
}",
@"class C
{
    int Foo
    {
        get
        {
            throw new NotImplementedException(); // comment
        }
    }
}", compareTokens: false, options: UseBlockBody);
        }
    }
}