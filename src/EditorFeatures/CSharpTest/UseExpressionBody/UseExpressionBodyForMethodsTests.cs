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
    public class UseExpressionBodyForMethodsTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
            => new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new UseExpressionBodyForMethodsDiagnosticAnalyzer(),
                new UseExpressionBodyForMethodsCodeFixProvider());

        private static readonly Dictionary<OptionKey, object> UseExpressionBody =
            new Dictionary<OptionKey, object>
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CodeStyleOptions.TrueWithNoneEnforcement }
            };

        private static readonly Dictionary<OptionKey, object> UseBlockBody =
            new Dictionary<OptionKey, object>
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CodeStyleOptions.FalseWithNoneEnforcement }
            };

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody1()
        {
            await TestAsync(
@"class C
{
    void Foo()
    {
        [|Bar|]();
    }
}",
@"class C
{
    void Foo() => Bar();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody2()
        {
            await TestAsync(
@"class C
{
    int Foo()
    {
        return [|Bar|]();
    }
}",
@"class C
{
    int Foo() => Bar();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody3()
        {
            await TestAsync(
@"class C
{
    int Foo()
    {
        [|throw|] new NotImplementedException();
    }
}",
@"class C
{
    int Foo() => throw new NotImplementedException();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody4()
        {
            await TestAsync(
@"class C
{
    int Foo()
    {
        [|throw|] new NotImplementedException(); // comment
    }
}",
@"class C
{
    int Foo() => throw new NotImplementedException(); // comment
}", compareTokens: false, options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody1()
        {
            await TestAsync(
@"class C
{
    void Foo() [|=>|] Bar();
}",
@"class C
{
    void Foo()
    {
        Bar();
    }
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody2()
        {
            await TestAsync(
@"class C
{
    int Foo() [|=>|] Bar();
}",
@"class C
{
    int Foo() 
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
    int Foo() [|=>|] throw new NotImplementedException();
}",
@"class C
{
    int Foo()
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
    int Foo() [|=>|] throw new NotImplementedException(); // comment
}",
@"class C
{
    int Foo()
    {
        throw new NotImplementedException(); // comment
    }
}", compareTokens: false, options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments1()
        {
            await TestAsync(
@"class C
{
    void Foo()
    {
        // Comment
        [|Bar|]();
    }
}",
@"class C
{
    void Foo() =>
        // Comment
        Bar();
}", options: UseExpressionBody, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments2()
        {
            await TestAsync(
@"class C
{
    void Foo()
    {
        // Comment
        return [|Bar|]();
    }
}",
@"class C
{
    void Foo() =>
        // Comment
        Bar();
}", options: UseExpressionBody, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments3()
        {
            await TestAsync(
@"class C
{
    void Foo()
    {
        // Comment
        throw [|Bar|]();
    }
}",
@"class C
{
    void Foo() =>
        // Comment
        throw Bar();
}", options: UseExpressionBody, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments4()
        {
            await TestAsync(
@"class C
{
    void Foo()
    {
        [|Bar|](); // Comment
    }
}",
@"class C
{
    void Foo() => Bar(); // Comment
}", options: UseExpressionBody, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments5()
        {
            await TestAsync(
@"class C
{
    void Foo()
    {
        return [|Bar|](); // Comment
    }
}",
@"class C
{
    void Foo() => Bar(); // Comment
}", options: UseExpressionBody, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments6()
        {
            await TestAsync(
@"class C
{
    void Foo()
    {
        throw [|Bar|](); // Comment
    }
}",
@"class C
{
    void Foo() => throw Bar(); // Comment
}", options: UseExpressionBody, compareTokens: false);
        }
    }
}