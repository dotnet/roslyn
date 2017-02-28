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
    public class UseExpressionBodyForAccessorsTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new UseExpressionBodyForAccessorsDiagnosticAnalyzer(),
                new UseExpressionBodyForAccessorsCodeFixProvider());

        private static readonly Dictionary<OptionKey, object> UseExpressionBody =
            new Dictionary<OptionKey, object>
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CodeStyleOptions.TrueWithSuggestionEnforcement },
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CodeStyleOptions.FalseWithSuggestionEnforcement },
                { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CodeStyleOptions.FalseWithSuggestionEnforcement }
            };

        private static readonly Dictionary<OptionKey, object> UseExpressionBodyIncludingPropertiesAndIndexers =
            new Dictionary<OptionKey, object>
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CodeStyleOptions.TrueWithSuggestionEnforcement },
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CodeStyleOptions.TrueWithSuggestionEnforcement },
                { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CodeStyleOptions.TrueWithSuggestionEnforcement }
            };

        private static readonly Dictionary<OptionKey, object> UseBlockBody =
            new Dictionary<OptionKey, object>
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CodeStyleOptions.FalseWithSuggestionEnforcement }
            };

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody1()
        {
            await TestInRegularAndScriptAsync(
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
    int Foo
    {
        get => Bar();
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestMissingIfPropertyIsOn()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        get
        {
            [|return|] Bar();
        }
    }
}", options: UseExpressionBodyIncludingPropertiesAndIndexers);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOnIndexer1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int this[int i]
    {
        get
        {
            [|return|] Bar();
        }
    }
}",
@"class C
{
    int this[int i]
    {
        get => Bar();
        }
    }", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestMissingIfIndexerIsOn()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int this[int i]
    {
        get
        {
            [|return|] Bar();
        }
    }
}", options: UseExpressionBodyIncludingPropertiesAndIndexers);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOnSetter1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        set
        {
            [|Bar|]();
        }
    }
}",
@"class C
{
    int Foo
    {
        set => [|Bar|]();
        }
    }", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestMissingWithOnlySetter()
        {
            await TestActionCountAsync(
@"class C
{
    int Foo
    {
        set => [|Bar|]();
    }
}", count: 1, featureOptions: UseExpressionBody);

            // There is a hidden diagnostic that still offers to convert expression-body to block-body.
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        set => [|Bar|]();
    }
}",
@"class C
{
    int Foo
    {
        set { Bar(); }
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody3()
        {
            await TestInRegularAndScriptAsync(
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
    int Foo
    {
        get => throw new NotImplementedException();
        }
    }", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody4()
        {
            await TestInRegularAndScriptAsync(
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
    int Foo
    {
        get => throw new NotImplementedException(); // comment
    }
}", compareTokens: false, options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        get [|=>|] Bar();
        }
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
        public async Task TestUseBlockBodyForSetter1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        set [|=>|] Bar();
        }
    }",
@"class C
{
    int Foo
    {
        set
        {
            Bar();
        }
    }
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        get [|=>|] throw new NotImplementedException();
        }
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
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        get [|=>|] throw new NotImplementedException(); // comment
    }
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