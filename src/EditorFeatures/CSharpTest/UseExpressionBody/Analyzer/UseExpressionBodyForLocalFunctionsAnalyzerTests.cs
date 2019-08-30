// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    public class UseExpressionBodyForLocalFunctionsAnalyzerTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new UseExpressionBodyDiagnosticAnalyzer(), new UseExpressionBodyCodeFixProvider());

        private IDictionary<OptionKey, object> UseExpressionBody =>
            Option(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement);

        private IDictionary<OptionKey, object> UseExpressionBodyWhenOnSingleLine =>
            Option(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement);

        private IDictionary<OptionKey, object> UseBlockBody =>
            Option(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, CSharpCodeStyleOptions.NeverWithSilentEnforcement);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        void Bar()
        {
            [|Test|]();
        }
    }
}",
@"class C
{
    void Goo()
    {
        void Bar() => Test();
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        int Bar()
        {
            return [|Test|]();
        }
    }
}",
@"class C
{
    void Goo()
    {
        int Bar() => Test();
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Goo()
    {
        int Bar()
        {
            [|throw|] new NotImplementedException();
        }
    }
}",
@"class C
{
    int Goo()
    {
        int Bar() => [|throw|] new NotImplementedException();
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Goo()
    {
        int Bar()
        {
            [|throw|] new NotImplementedException(); // comment
        }
    }
}",
@"class C
{
    int Goo()
    {
        int Bar() => [|throw|] new NotImplementedException(); // comment
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBodyWhenOnSingleLineMissing()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int Goo()
    {
        int Bar()
        {
            [|return|] 1 +
                2 +
                3;
        }
    }
}", new TestParameters(options: UseExpressionBodyWhenOnSingleLine));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBodyWhenOnSingleLine()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Goo()
    {
        int Bar()
        {
            [|return|] 1 + 2 + 3;
        }
    }
}",
@"class C
{
    int Goo()
    {
        int Bar() => 1 + 2 + 3;
    }
}", options: UseExpressionBodyWhenOnSingleLine);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        void Bar() => [|Test()|];
    }
}",
@"class C
{
    void Goo()
    {
        void Bar()
        {
            Test();
        }
    }
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Goo()
    {
        int Bar() => [|Test|]();
    }
}",
@"class C
{
    int Goo()
    {
        int Bar()
        {
            return Test();
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
    int Goo()
    {
        int Bar() => [|throw|] new NotImplementedException();
    }
}",
@"class C
{
    int Goo()
    {
        int Bar()
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
    int Goo()
    {
        int Bar() => [|throw|] new NotImplementedException(); // comment
    }
}",
@"class C
{
    int Goo()
    {
        int Bar()
        {
            throw new NotImplementedException(); // comment
        }
    }
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        void Bar()
        {
            // Comment
            [|Test|]();
        }
    }
}",
@"class C
{
    void Goo()
    {
        void Bar() =>
            // Comment
            Test();
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Goo()
    {
        int Bar()
        {
            // Comment
            return [|Test|]();
        }
    }
}",
@"class C
{
    int Goo()
    {
        int Bar() =>
            // Comment
            Test();
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        void Bar()
        {
            // Comment
            throw [|Test|]();
        }
    }
}",
@"class C
{
    void Goo()
    {
        void Bar() =>
            // Comment
            throw Test();
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        void Bar()
        {
            [|Test|](); // Comment
        }
    }
}",
@"class C
{
    void Goo()
    {
        void Bar() => Test(); // Comment
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Goo()
    {
        int Bar()
        {
            return [|Test|](); // Comment
        }
    }
}",
@"class C
{
    int Goo()
    {
        int Bar() => Test(); // Comment
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments6()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        void Bar()
        {
            throw [|Test|](); // Comment
        }
    }
}",
@"class C
{
    void Goo()
    {
        void Bar() => throw Test(); // Comment
    }
}", options: UseExpressionBody);
        }

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
        void Bar()
        {
#if DEBUG
            [|Console|].WriteLine();
#endif
        }
    }
}",
@"
#define DEBUG
using System;

class Program
{
    void Method()
    {
        void Bar() =>
#if DEBUG
            Console.WriteLine();
#endif

    }
}", options: UseExpressionBody);
        }

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
        void Bar()
        {
#if DEBUG
            [|Console|].WriteLine(a);
#else
            Console.WriteLine(b);
#endif
        }
    }
}",
@"
#define DEBUG
using System;

class Program
{
    void Method()
    {
        void Bar() =>
#if DEBUG
            Console.WriteLine(a);
#else
            Console.WriteLine(b);
#endif

    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync1()
        {
            await TestInRegularAndScriptAsync(
@"using System.Threading.Tasks;

class C
{
    async Task Goo()
    {
        async Task Bar() [|=>|] await Test();
    }

    Task Test() { }
}",
@"using System.Threading.Tasks;

class C
{
    async Task Goo()
    {
        async Task Bar()
        {
            await Test();
        }
    }

    Task Test() { }
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync2()
        {
            await TestInRegularAndScriptAsync(
@"using System.Threading.Tasks;

class C
{
    async void Goo()
    {
        async void Bar() [|=>|] await Test();
    }

    Task Test() { }
}",
@"using System.Threading.Tasks;

class C
{
    async void Goo()
    {
        async void Bar()
        {
            await Test();
        }
    }

    Task Test() { }
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync3()
        {
            await TestInRegularAndScriptAsync(
@"using System.Threading.Tasks;

class C
{
    async ValueTask Goo() 
    {
        async ValueTask Test() [|=>|] await Bar();
    }

    Task Bar() { }
}",
@"using System.Threading.Tasks;

class C
{
    async ValueTask Goo() 
    {
        async ValueTask Test()
        {
            await Bar();
        }
    }

    Task Bar() { }
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync4()
        {
            await TestInRegularAndScriptAsync(
@"using System.Threading.Tasks;

class C
{
    async Task<int> Goo()
    {
        Task<int> Test() [|=>|] Bar();
    }

    Task<int> Bar() { }
}",
@"using System.Threading.Tasks;

class C
{
    async Task<int> Goo()
    {
        Task<int> Test()
        {
            return Bar();
        }
    }

    Task<int> Bar() { }
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync5()
        {
            await TestInRegularAndScriptAsync(
@"using System.Threading.Tasks;

class C
{
    Task Goo() 
    {
        Task Test() [|=>|] Bar();
    }

    Task Bar() { }
}",
@"using System.Threading.Tasks;

class C
{
    Task Goo() 
    {
        Task Test()
        {
            return Bar();
        }
    }

    Task Bar() { }
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyNestedLocalFunction()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        void Bar()
        {
            void Test() => [|NestedTest()|];
        }
    }
}",
@"class C
{
    void Goo()
    {
        void Bar()
        {
            void Test()
            {
                NestedTest();
            }
        }
    }
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBodyNestedLocalFunction()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        void Bar()
        {
            void Test()
            {
                [|NestedTest()|];
            }
        }
    }
}",
@"class C
{
    void Goo()
    {
        void Bar()
        {
            void Test() => NestedTest();
        }
    }
}", options: UseExpressionBody);
        }
    }
}
