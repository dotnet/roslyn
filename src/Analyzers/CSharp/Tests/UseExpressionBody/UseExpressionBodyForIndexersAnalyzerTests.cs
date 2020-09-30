// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    public class UseExpressionBodyForIndexersAnalyzerTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public UseExpressionBodyForIndexersAnalyzerTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new UseExpressionBodyDiagnosticAnalyzer(), new UseExpressionBodyCodeFixProvider());

        private OptionsCollection UseExpressionBody =>
            new OptionsCollection(GetLanguage())
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
            };

        private OptionsCollection UseBlockBody =>
            new OptionsCollection(GetLanguage())
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
            };

        private OptionsCollection UseBlockBodyExceptAccessor =>
            new OptionsCollection(GetLanguage())
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
            };

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody1()
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
    int this[int i] => Bar();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestMissingWithSetter()
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

        set
        {
        }
    }
}", new TestParameters(options: UseExpressionBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestMissingOnSetter1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int this[int i]
    {
        set
        {
            [|Bar|]();
        }
    }
}", new TestParameters(options: UseExpressionBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int this[int i]
    {
        get
        {
            [|throw|] new NotImplementedException();
        }
    }
}",
@"class C
{
    int this[int i] => throw new NotImplementedException();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int this[int i]
    {
        get
        {
            [|throw|] new NotImplementedException(); // comment
        }
    }
}",
@"class C
{
    int this[int i] => throw new NotImplementedException(); // comment
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int this[int i] [|=>|] Bar();
}",
@"class C
{
    int this[int i]
    {
        get
        {
            return Bar();
        }
    }
}", options: UseBlockBody);
        }

        [WorkItem(20363, "https://github.com/dotnet/roslyn/issues/20363")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyForAccessorEventWhenAccessorWantExpression1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int this[int i] [|=>|] Bar();
}",
@"class C
{
    int this[int i]
    {
        get
        {
            return Bar();
        }
    }
}", options: UseBlockBodyExceptAccessor);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int this[int i] [|=>|] throw new NotImplementedException();
}",
@"class C
{
    int this[int i]
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
    int this[int i] [|=>|] throw new NotImplementedException(); // comment
}",
@"class C
{
    int this[int i]
    {
        get
        {
            throw new NotImplementedException(); // comment
        }
    }
}", options: UseBlockBody);
        }
    }
}
