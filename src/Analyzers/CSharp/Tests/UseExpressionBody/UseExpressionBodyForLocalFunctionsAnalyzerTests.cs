// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    using VerifyCS = CSharpCodeFixVerifier<
        UseExpressionBodyDiagnosticAnalyzer,
        UseExpressionBodyCodeFixProvider>;

    public class UseExpressionBodyForLocalFunctionsAnalyzerTests
    {
        private static async Task TestWithUseExpressionBody(string code, string fixedCode)
        {
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, ExpressionBodyPreference.WhenPossible } }
            }.RunAsync();
        }

        private static async Task TestWithUseExpressionBodyWhenOnSingleLine(string code, string fixedCode)
        {
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, ExpressionBodyPreference.WhenOnSingleLine } }
            }.RunAsync();
        }

        private static async Task TestWithUseBlockBody(string code, string fixedCode, ReferenceAssemblies? referenceAssemblies = null)
        {
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, ExpressionBodyPreference.Never } },
                ReferenceAssemblies = referenceAssemblies ?? ReferenceAssemblies.Default,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody1()
        {
            var code = @"
class C
{
    void Test() { }

    void Goo()
    {
        {|IDE0061:void Bar()
        {
            Test();
        }|}
    }
}";
            var fixedCode = @"
class C
{
    void Test() { }

    void Goo()
    {
        void Bar() => Test();
    }
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody2()
        {
            var code = @"
class C
{
    int Test() { return 0; }

    void Goo()
    {
        {|IDE0061:int Bar()
        {
            return Test();
        }|}
    }
}";
            var fixedCode = @"
class C
{
    int Test() { return 0; }

    void Goo()
    {
        int Bar() => Test();
    }
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody3()
        {
            var code = @"
using System;

class C
{
    void Goo()
    {
        {|IDE0061:int Bar()
        {
            throw new NotImplementedException();
        }|}
    }
}";
            var fixedCode = @"
using System;

class C
{
    void Goo()
    {
        int Bar() => throw new NotImplementedException();
    }
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody4()
        {
            var code = @"
using System;

class C
{
    void Goo()
    {
        {|IDE0061:int Bar()
        {
            throw new NotImplementedException(); // comment
        }|}
    }
}";
            var fixedCode = @"
using System;

class C
{
    void Goo()
    {
        int Bar() => throw new NotImplementedException(); // comment
    }
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBodyWhenOnSingleLineMissing()
        {
            var code = @"
class C
{
    void Goo()
    {
        int Bar()
        {
            return 1 +
                2 +
                3;
        }
    }
}";
            await TestWithUseExpressionBodyWhenOnSingleLine(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBodyWhenOnSingleLine()
        {
            var code = @"
class C
{
    void Goo()
    {
        {|IDE0061:int Bar()
        {
            return 1 + 2 + 3;
        }|}
    }
}";
            var fixedCode = @"
class C
{
    void Goo()
    {
        int Bar() => 1 + 2 + 3;
    }
}";
            await TestWithUseExpressionBodyWhenOnSingleLine(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody1()
        {
            var code = @"
class C
{
    void Test() { }

    void Goo()
    {
        {|IDE0061:void Bar() => Test();|}
    }
}";
            var fixedCode = @"
class C
{
    void Test() { }

    void Goo()
    {
        void Bar()
        {
            Test();
        }
    }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody2()
        {
            var code = @"
class C
{
    int Test() { return 0; }

    void Goo()
    {
        {|IDE0061:int Bar() => Test();|}
    }
}";
            var fixedCode = @"
class C
{
    int Test() { return 0; }

    void Goo()
    {
        int Bar()
        {
            return Test();
        }
    }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody3()
        {
            var code = @"
using System;

class C
{
    void Goo()
    {
        {|IDE0061:int Bar() => throw new NotImplementedException();|}
    }
}";
            var fixedCode = @"
using System;

class C
{
    void Goo()
    {
        int Bar()
        {
            throw new NotImplementedException();
        }
    }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody4()
        {
            var code = @"
using System;

class C
{
    void Goo()
    {
        {|IDE0061:int Bar() => throw new NotImplementedException();|} // comment
    }
}";
            var fixedCode = @"
using System;

class C
{
    void Goo()
    {
        int Bar()
        {
            throw new NotImplementedException(); // comment
        }
    }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments1()
        {
            var code = @"
class C
{
    void Test() { }

    void Goo()
    {
        {|IDE0061:void Bar()
        {
            // Comment
            Test();
        }|}
    }
}";
            var fixedCode = @"
class C
{
    void Test() { }

    void Goo()
    {
        void Bar() =>
            // Comment
            Test();
    }
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments2()
        {
            var code = @"
class C
{
    int Test() { return 0; }

    void Goo()
    {
        {|IDE0061:int Bar()
        {
            // Comment
            return Test();
        }|}
    }
}";
            var fixedCode = @"
class C
{
    int Test() { return 0; }

    void Goo()
    {
        int Bar() =>
            // Comment
            Test();
    }
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments3()
        {
            var code = @"
using System;

class C
{
    Exception Test() { return new Exception(); }

    void Goo()
    {
        {|IDE0061:void Bar()
        {
            // Comment
            throw Test();
        }|}
    }
}";
            var fixedCode = @"
using System;

class C
{
    Exception Test() { return new Exception(); }

    void Goo()
    {
        void Bar() =>
            // Comment
            throw Test();
    }
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments4()
        {
            var code = @"
class C
{
    void Test() { }

    void Goo()
    {
        {|IDE0061:void Bar()
        {
            Test(); // Comment
        }|}
    }
}";
            var fixedCode = @"
class C
{
    void Test() { }

    void Goo()
    {
        void Bar() => Test(); // Comment
    }
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments5()
        {
            var code = @"
class C
{
    int Test() { return 0; }

    void Goo()
    {
        {|IDE0061:int Bar()
        {
            return Test(); // Comment
        }|}
    }
}";
            var fixedCode = @"
class C
{
    int Test() { return 0; }

    void Goo()
    {
        int Bar() => Test(); // Comment
    }
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments6()
        {
            var code = @"
using System;

class C
{
    Exception Test() { return new Exception(); }

    void Goo()
    {
        {|IDE0061:void Bar()
        {
            throw Test(); // Comment
        }|}
    }
}";
            var fixedCode = @"
using System;

class C
{
    Exception Test() { return new Exception(); }

    void Goo()
    {
        void Bar() => throw Test(); // Comment
    }
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestDirectives1()
        {
            var code = @"
#define DEBUG
using System;

class Program
{
    void Method()
    {
        {|IDE0061:void Bar()
        {
#if DEBUG
            Console.WriteLine();
#endif
        }|}
    }
}";
            var fixedCode = @"
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
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestDirectives2()
        {
            var code = @"
#define DEBUG
using System;

class Program
{
    void Method()
    {
        {|IDE0061:void Bar()
        {
#if DEBUG
            Console.WriteLine(0);
#else
            Console.WriteLine(1);
#endif
        }|}
    }
}";
            var fixedCode = @"
#define DEBUG
using System;

class Program
{
    void Method()
    {
        void Bar() =>
#if DEBUG
            Console.WriteLine(0);
#else
            Console.WriteLine(1);
#endif

    }
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync1()
        {
            var code = @"
using System.Threading.Tasks;

class C
{
    async Task Goo()
    {
        {|IDE0061:async Task Bar() => await Test();|}
    }

    Task Test() { return Task.CompletedTask; }
}";
            var fixedCode = @"
using System.Threading.Tasks;

class C
{
    async Task Goo()
    {
        async Task Bar()
        {
            await Test();
        }
    }

    Task Test() { return Task.CompletedTask; }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync2()
        {
            var code = @"
using System.Threading.Tasks;

class C
{
    async void Goo()
    {
        {|IDE0061:async void Bar() => await Test();|}
    }

    Task Test() { return Task.CompletedTask; }
}";
            var fixedCode = @"
using System.Threading.Tasks;

class C
{
    async void Goo()
    {
        async void Bar()
        {
            await Test();
        }
    }

    Task Test() { return Task.CompletedTask; }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync3()
        {
            var code = @"
using System.Threading.Tasks;

class C
{
    void Goo() 
    {
        {|IDE0061:async ValueTask Test() => await Bar();|}
    }

    Task Bar() { return Task.CompletedTask; }
}";
            var fixedCode = @"
using System.Threading.Tasks;

class C
{
    void Goo() 
    {
        async ValueTask Test()
        {
            await Bar();
        }
    }

    Task Bar() { return Task.CompletedTask; }
}";
            await TestWithUseBlockBody(code, fixedCode, ReferenceAssemblies.NetStandard.NetStandard21);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync4()
        {
            var code = @"
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        {|IDE0061:Task<int> Test() => Bar();|}
    }

    Task<int> Bar() { return Task.FromResult(0); }
}";
            var fixedCode = @"
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Task<int> Test()
        {
            return Bar();
        }
    }

    Task<int> Bar() { return Task.FromResult(0); }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync5()
        {
            var code = @"
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        {|IDE0061:Task Test() => Bar();|}
    }

    Task Bar() { return Task.CompletedTask; }
}";
            var fixedCode = @"
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Task Test()
        {
            return Bar();
        }
    }

    Task Bar() { return Task.CompletedTask; }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyNestedLocalFunction()
        {
            var code = @"
class C
{
    void NestedTest() { }

    void Goo()
    {
        void Bar()
        {
            {|IDE0061:void Test() => NestedTest();|}
        }
    }
}";
            var fixedCode = @"
class C
{
    void NestedTest() { }

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
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBodyNestedLocalFunction()
        {
            var code = @"
class C
{
    void NestedTest() { }

    void Goo()
    {
        void Bar()
        {
            {|IDE0061:void Test()
            {
                NestedTest();
            }|}
        }
    }
}";
            var fixedCode = @"
class C
{
    void NestedTest() { }

    void Goo()
    {
        void Bar()
        {
            void Test() => NestedTest();
        }
    }
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }
    }
}
