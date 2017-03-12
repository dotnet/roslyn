// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new UseExpressionBodyForMethodsDiagnosticAnalyzer(), new UseExpressionBodyForMethodsCodeFixProvider());

        private IDictionary<OptionKey, object> UseExpressionBody =>
            this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithNoneEnforcement);

        private IDictionary<OptionKey, object> UseBlockBody =>
            this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.NeverWithNoneEnforcement);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody1()
        {
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
}", ignoreTrivia: false, options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody1()
        {
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
}", ignoreTrivia: false, options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments1()
        {
            await TestInRegularAndScriptAsync(
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
}", options: UseExpressionBody, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments2()
        {
            await TestInRegularAndScriptAsync(
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
}", options: UseExpressionBody, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments3()
        {
            await TestInRegularAndScriptAsync(
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
}", options: UseExpressionBody, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments4()
        {
            await TestInRegularAndScriptAsync(
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
}", options: UseExpressionBody, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments5()
        {
            await TestInRegularAndScriptAsync(
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
}", options: UseExpressionBody, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments6()
        {
            await TestInRegularAndScriptAsync(
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
}", options: UseExpressionBody, ignoreTrivia: false);
        }

        [WorkItem(17120, "https://github.com/dotnet/roslyn/issues/17120")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestDirectives1()
        {
            await TestInRegularAndScriptAsync(
@"
#define DEBUG
using System;

class Program
{
    void Method()
    {
#if DEBUG
        [|Console|].WriteLine();
#endif
    }
}",
@"
#define DEBUG
using System;

class Program
{
    void Method() =>
#if DEBUG
        Console.WriteLine();
#endif

}", options: UseExpressionBody, ignoreTrivia: false);
        }

        [WorkItem(17120, "https://github.com/dotnet/roslyn/issues/17120")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestDirectives2()
        {
            await TestInRegularAndScriptAsync(
@"
#define DEBUG
using System;

class Program
{
    void Method()
    {
#if DEBUG
        [|Console|].WriteLine(a);
#else
        Console.WriteLine(b);
#endif
    }
}",
@"
#define DEBUG
using System;

class Program
{
    void Method() =>
#if DEBUG
        Console.WriteLine(a);
#else
        Console.WriteLine(b);
#endif

}", options: UseExpressionBody, ignoreTrivia: false);
        }
    }
}