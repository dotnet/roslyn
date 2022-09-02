// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    using VerifyCS = CSharpCodeRefactoringVerifier<UseExpressionBodyForLambdaCodeRefactoringProvider>;

    public class UseExpressionBodyForLambdasRefactoringTests
    {
        private static readonly CodeStyleOption2<ExpressionBodyPreference> s_useExpressionBody = new(ExpressionBodyPreference.WhenPossible, NotificationOption2.Suggestion);
        private static readonly CodeStyleOption2<ExpressionBodyPreference> s_useExpressionBodyDisabledDiagnostic = new(ExpressionBodyPreference.WhenPossible, NotificationOption2.None);
        private static readonly CodeStyleOption2<ExpressionBodyPreference> s_useBlockBody = new(ExpressionBodyPreference.Never, NotificationOption2.Suggestion);
        private static readonly CodeStyleOption2<ExpressionBodyPreference> s_useBlockBodyDisabledDiagnostic = new(ExpressionBodyPreference.Never, NotificationOption2.None);

        private static async Task TestMissingAsync(string code, CodeStyleOption2<ExpressionBodyPreference> option)
        {
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, option } },
            }.RunAsync();
        }

        private static async Task TestInRegularAndScript1Async(string code, string fixedCode, CodeStyleOption2<ExpressionBodyPreference> option)
        {
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, option } },
            }.RunAsync();
        }

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
        };
    }
}", s_useExpressionBody);
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
}", s_useExpressionBodyDisabledDiagnostic);
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
}", s_useBlockBody);
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
}", s_useBlockBody);
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
}", s_useBlockBody);
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
}", s_useBlockBodyDisabledDiagnostic);
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
}", s_useExpressionBody);
        }
    }
}
