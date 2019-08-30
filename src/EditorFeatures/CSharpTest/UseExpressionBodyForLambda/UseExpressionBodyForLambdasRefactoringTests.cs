// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    public class UseExpressionBodyForLambdasRefactoringTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new UseExpressionBodyForLambdaCodeRefactoringProvider();

        private IDictionary<OptionKey, object> UseExpressionBody =>
            this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement);

        private IDictionary<OptionKey, object> UseExpressionBodyDisabledDiagnostic =>
            this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement);

        private IDictionary<OptionKey, object> UseBlockBody =>
            this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, CSharpCodeStyleOptions.NeverWithSuggestionEnforcement);

        private IDictionary<OptionKey, object> UseBlockBodyDisabledDiagnostic =>
            this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, CSharpCodeStyleOptions.NeverWithSilentEnforcement);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestNotOfferedIfUserPrefersExpressionBodiesAndInBlockBody()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x [|=>|]
        {
            return x.ToString();
        }
    }
}", parameters: new TestParameters(options: UseExpressionBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedIfUserPrefersExpressionBodiesWithoutDiagnosticAndInBlockBody()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x [||]=>
        {
            return x.ToString();
        };
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x => x.ToString();
    }
}", parameters: new TestParameters(options: UseExpressionBodyDisabledDiagnostic));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedIfUserPrefersBlockBodiesAndInBlockBody()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x [||]=>
        {
            return x.ToString();
        };
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x => x.ToString();
    }
}", parameters: new TestParameters(options: UseBlockBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestNotOfferedInMethod()
        {
            await TestMissingAsync(
@"class C
{
    int [|Goo|]()
    {
        return 1;
    }
}", parameters: new TestParameters(options: UseBlockBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestNotOfferedIfUserPrefersBlockBodiesAndInExpressionBody()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x [||]=> x.ToString();
    }
}", parameters: new TestParameters(options: UseBlockBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedIfUserPrefersBlockBodiesWithoutDiagnosticAndInExpressionBody()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x [||]=> x.ToString();
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x =>
        {
            return x.ToString();
        };
    }
}", parameters: new TestParameters(options: UseBlockBodyDisabledDiagnostic));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedIfUserPrefersExpressionBodiesAndInExpressionBody()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x [||]=> x.ToString();
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x =>
        {
            return x.ToString();
        };
    }
}", parameters: new TestParameters(options: UseExpressionBody));
        }
    }
}
