// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.Async;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.CodeFixes.Async.CSharpAddAwaitCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Async
{
    public class AddAwaitTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task BadAsyncReturnOperand1()
        {
            var initial =
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> Test()
    {
        return 3;
    }

    async Task<int> Test2()
    {
        return {|CS4016:Test()|};
    }
}";

            var expected =
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> Test()
    {
        return 3;
    }

    async Task<int> Test2()
    {
        return await Test();
    }
}";
            await VerifyCS.VerifyCodeFixAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task BadAsyncReturnOperand_WithLeadingTrivia1()
        {
            var initial =
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> Test()
    {
        return 3;
    }

    async Task<int> Test2()
    {
        return
        // Useful comment
        {|CS4016:Test()|};
    }
}";

            var expected =
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> Test()
    {
        return 3;
    }

    async Task<int> Test2()
    {
        return
        // Useful comment
        await Test();
    }
}";
            await VerifyCS.VerifyCodeFixAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task BadAsyncReturnOperand_ConditionalExpressionWithTrailingTrivia_SingleLine()
        {
            var initial =
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> Test() => 3;

    async Task<int> Test2()
    {
        return {|CS4016:true ? Test() /* true */ : Test()|} /* false */;
    }
}";

            var expected =
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> Test() => 3;

    async Task<int> Test2()
    {
        return await (true ? Test() /* true */ : Test() /* false */);
    }
}";
            await VerifyCS.VerifyCodeFixAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task BadAsyncReturnOperand_ConditionalExpressionWithTrailingTrivia_Multiline()
        {
            var initial =
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> Test() => 3;

    async Task<int> Test2()
    {
        return {|CS4016:true ? Test() // aaa
                    : Test()|} // bbb
                    ;
    }
}";

            var expected =
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> Test() => 3;

    async Task<int> Test2()
    {
        return await (true ? Test() // aaa
                    : Test()) // bbb
                    ;
    }
}";
            await VerifyCS.VerifyCodeFixAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task BadAsyncReturnOperand_NullCoalescingExpressionWithTrailingTrivia_SingleLine()
        {
            var initial =
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> Test() => 3;

    async Task<int> Test2()
    {
        return {|CS4016:null /* 0 */ ?? Test()|} /* 1 */;
    }
}";

            var expected =
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> Test() => 3;

    async Task<int> Test2()
    {
        return await (null /* 0 */ ?? Test() /* 1 */);
    }
}";
            await VerifyCS.VerifyCodeFixAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task BadAsyncReturnOperand_NullCoalescingExpressionWithTrailingTrivia_Multiline()
        {
            var initial =
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> Test() => 3;

    async Task<int> Test2()
    {
        return {|CS4016:null   // aaa
            ?? Test()|} // bbb
            ;
    }
}";

            var expected =
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> Test() => 3;

    async Task<int> Test2()
    {
        return await (null   // aaa
            ?? Test()) // bbb
            ;
    }
}";
            await VerifyCS.VerifyCodeFixAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task BadAsyncReturnOperand_AsExpressionWithTrailingTrivia_SingleLine()
        {
            var initial =
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> Test2()
    {
        return {|CS4016:null /* 0 */ as Task<int>|} /* 1 */;
    }
}";

            var expected =
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> Test2()
    {
        return await (null /* 0 */ as Task<int> /* 1 */);
    }
}";
            await VerifyCS.VerifyCodeFixAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task BadAsyncReturnOperand_AsExpressionWithTrailingTrivia_Multiline()
        {
            var initial =
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> Test() => 3;

    async Task<int> Test2()
    {
        return {|CS4016:null      // aaa
            as Task<int>|} // bbb
            ;
    }
}";

            var expected =
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> Test() => 3;

    async Task<int> Test2()
    {
        return await (null      // aaa
            as Task<int>) // bbb
            ;
    }
}";
            await VerifyCS.VerifyCodeFixAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task TaskNotAwaited()
        {
            var initial =
@"using System;
using System.Threading.Tasks;
class Program
{
    async void Test()
    {
        {|CS4014:Task.Delay(3)|};
    }
}";

            var expected =
@"using System;
using System.Threading.Tasks;
class Program
{
    async void Test()
    {
        await Task.Delay(3);
    }
}";
            await VerifyCS.VerifyCodeFixAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task TaskNotAwaited_WithLeadingTrivia()
        {
            var initial =
@"using System;
using System.Threading.Tasks;
class Program
{
    async void Test()
    {

        // Useful comment
        {|CS4014:Task.Delay(3)|};
    }
}";

            var expected =
@"using System;
using System.Threading.Tasks;
class Program
{
    async void Test()
    {

        // Useful comment
        await Task.Delay(3);
    }
}";
            await VerifyCS.VerifyCodeFixAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task FunctionNotAwaited()
        {
            var initial =
@"using System;
using System.Threading.Tasks;
class Program
{
    Task AwaitableFunction()
    {
        return Task.FromResult(true);
    }

    async void Test()
    {
        {|CS4014:AwaitableFunction()|};
    }
}";

            var expected =
@"using System;
using System.Threading.Tasks;
class Program
{
    Task AwaitableFunction()
    {
        return Task.FromResult(true);
    }

    async void Test()
    {
        await AwaitableFunction();
    }
}";
            await VerifyCS.VerifyCodeFixAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task FunctionNotAwaited_WithLeadingTrivia()
        {
            var initial =
@"using System;
using System.Threading.Tasks;
class Program
{
    Task AwaitableFunction()
    {
        return Task.FromResult(true);
    }

    async void Test()
    {

        // Useful comment
        {|CS4014:AwaitableFunction()|};
    }
}";

            var expected =
@"using System;
using System.Threading.Tasks;
class Program
{
    Task AwaitableFunction()
    {
        return Task.FromResult(true);
    }

    async void Test()
    {

        // Useful comment
        await AwaitableFunction();
    }
}";
            await VerifyCS.VerifyCodeFixAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task FunctionNotAwaited_WithLeadingTrivia1()
        {
            var initial =
@"using System;
using System.Threading.Tasks;
class Program
{
    Task AwaitableFunction()
    {
        return Task.FromResult(true);
    }

    async void Test()
    {
        var i = 0;

        {|CS4014:AwaitableFunction()|};
    }
}";

            var expected =
@"using System;
using System.Threading.Tasks;
class Program
{
    Task AwaitableFunction()
    {
        return Task.FromResult(true);
    }

    async void Test()
    {
        var i = 0;

        await AwaitableFunction();
    }
}";
            await VerifyCS.VerifyCodeFixAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task TestAssignmentExpression()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System.Threading.Tasks;

class TestClass
{
    private async Task MyTestMethod1Async()
    {
        int myInt = {|CS0029:MyIntMethodAsync()|};
    }

    private Task<int> MyIntMethodAsync()
    {
        return Task.FromResult(result: 1);
    }
}",
@"using System.Threading.Tasks;

class TestClass
{
    private async Task MyTestMethod1Async()
    {
        int myInt = await MyIntMethodAsync();
    }

    private Task<int> MyIntMethodAsync()
    {
        return Task.FromResult(result: 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task TestAssignmentExpressionWithConversion()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System.Threading.Tasks;

class TestClass
{
    private async Task MyTestMethod1Async()
    {
        long myInt = {|CS0029:MyIntMethodAsync()|};
    }

    private Task<int> MyIntMethodAsync()
    {
        return Task.FromResult(result: 1);
    }
}",
@"using System.Threading.Tasks;

class TestClass
{
    private async Task MyTestMethod1Async()
    {
        long myInt = await MyIntMethodAsync();
    }

    private Task<int> MyIntMethodAsync()
    {
        return Task.FromResult(result: 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task TestAssignmentExpressionWithConversionInNonAsyncFunction()
        {
            var code = @"using System.Threading.Tasks;

class TestClass
{
    private Task MyTestMethod1Async()
    {
        long myInt = {|CS0029:MyIntMethodAsync()|};
    }

    private Task<int> MyIntMethodAsync()
    {
        return Task.FromResult(result: 1);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                // Test0.cs(5,18): error CS0161: 'TestClass.MyTestMethod1Async()': not all code paths return a value
                DiagnosticResult.CompilerError("CS0161").WithSpan(5, 18, 5, 36).WithArguments("TestClass.MyTestMethod1Async()"),
                fixedSource: code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task TestAssignmentExpressionWithConversionInAsyncFunction()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System.Threading.Tasks;

class TestClass
{
    private async Task MyTestMethod1Async()
    {
        long myInt = {|CS0029:MyIntMethodAsync()|};
    }

    private Task<object> MyIntMethodAsync()
    {
        return Task.FromResult(new object());
    }
}",
@"using System.Threading.Tasks;

class TestClass
{
    private async Task MyTestMethod1Async()
    {
        long myInt = {|CS0266:await MyIntMethodAsync()|};
    }

    private Task<object> MyIntMethodAsync()
    {
        return Task.FromResult(new object());
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task TestAssignmentExpression1()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
using System.Threading.Tasks;

class TestClass
{
    private async Task MyTestMethod1Async()
    {
        Action lambda = async () => {
            int myInt = {|CS0029:MyIntMethodAsync()|};
        };
    }

    private Task<int> MyIntMethodAsync()
    {
        return Task.FromResult(result: 1);
    }
}",
@"using System;
using System.Threading.Tasks;

class TestClass
{
    private async Task MyTestMethod1Async()
    {
        Action lambda = async () => {
            int myInt = await MyIntMethodAsync();
        };
    }

    private Task<int> MyIntMethodAsync()
    {
        return Task.FromResult(result: 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task TestAssignmentExpression2()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
using System.Threading.Tasks;

class TestClass
{
    private async Task MyTestMethod1Async()
    {
        Func<Task> lambda = async () => {
            int myInt = {|CS0029:MyIntMethodAsync()|};
        };
    }

    private Task<int> MyIntMethodAsync()
    {
        return Task.FromResult(result: 1);
    }
}",
@"using System;
using System.Threading.Tasks;

class TestClass
{
    private async Task MyTestMethod1Async()
    {
        Func<Task> lambda = async () => {
            int myInt = await MyIntMethodAsync();
        };
    }

    private Task<int> MyIntMethodAsync()
    {
        return Task.FromResult(result: 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task TestAssignmentExpression3()
        {
            var code = @"using System;
using System.Threading.Tasks;

class TestClass
{
    private async Task MyTestMethod1Async()
    {
        Func<Task> lambda = () => {
            int myInt = MyInt  MethodAsync();
        };
    }

    private Task<int> MyIntMethodAsync()
    {
        return Task.FromResult(result: 1);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                new[]
                {
                    // Test0.cs(8,32): error CS1643: Not all code paths return a value in lambda expression of type 'Func<Task>'
                    DiagnosticResult.CompilerError("CS1643").WithSpan(8, 32, 8, 34).WithArguments("lambda expression", "System.Func<System.Threading.Tasks.Task>"),
                    // Test0.cs(9,25): error CS0103: The name 'MyInt' does not exist in the current context
                    DiagnosticResult.CompilerError("CS0103").WithSpan(9, 25, 9, 30).WithArguments("MyInt"),
                    // Test0.cs(9,32): error CS0103: The name 'MethodAsync' does not exist in the current context
                    DiagnosticResult.CompilerError("CS0103").WithSpan(9, 32, 9, 43).WithArguments("MethodAsync"),
                    // Test0.cs(9,32): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(9, 32, 9, 43),
                },
                fixedSource: code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task TestAssignmentExpression4()
        {
            var code = @"using System;
using System.Threading.Tasks;

class TestClass
{
    private async Task MyTestMethod1Async()
    {
        Action lambda = () => {
            int myInt = MyIntM  ethodAsync();
        };
    }

    private Task<int> MyIntMethodAsync()
    {
        return Task.FromResult(result: 1);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                new[]
                {
                    // Test0.cs(9,25): error CS0103: The name 'MyIntM' does not exist in the current context
                    DiagnosticResult.CompilerError("CS0103").WithSpan(9, 25, 9, 31).WithArguments("MyIntM"),
                    // Test0.cs(9,33): error CS0103: The name 'ethodAsync' does not exist in the current context
                    DiagnosticResult.CompilerError("CS0103").WithSpan(9, 33, 9, 43).WithArguments("ethodAsync"),
                    // Test0.cs(9,33): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(9, 33, 9, 43),
                },
                fixedSource: code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task TestAssignmentExpression5()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
using System.Threading.Tasks;

class TestClass
{
    private async Task MyTestMethod1Async()
    {
        Action @delegate = async delegate {
            int myInt = {|CS0029:MyIntMethodAsync()|};
        };
    }

    private Task<int> MyIntMethodAsync()
    {
        return Task.FromResult(result: 1);
    }
}",
@"using System;
using System.Threading.Tasks;

class TestClass
{
    private async Task MyTestMethod1Async()
    {
        Action @delegate = async delegate {
            int myInt = await MyIntMethodAsync();
        };
    }

    private Task<int> MyIntMethodAsync()
    {
        return Task.FromResult(result: 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task TestAssignmentExpression6()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
using System.Threading.Tasks;

class TestClass
{
    private async Task MyTestMethod1Async()
    {
        Func<Task> @delegate = async delegate {
            int myInt = {|CS0029:MyIntMethodAsync()|};
        };
    }

    private Task<int> MyIntMethodAsync()
    {
        return Task.FromResult(result: 1);
    }
}",
@"using System;
using System.Threading.Tasks;

class TestClass
{
    private async Task MyTestMethod1Async()
    {
        Func<Task> @delegate = async delegate {
            int myInt = await MyIntMethodAsync();
        };
    }

    private Task<int> MyIntMethodAsync()
    {
        return Task.FromResult(result: 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task TestAssignmentExpression7()
        {
            var code = @"using System;
using System.Threading.Tasks;

class TestClass
{
    private async Task MyTestMethod1Async()
    {
        Action @delegate = delegate {
            int myInt = MyInt  MethodAsync();
        };
    }

    private Task<int> MyIntMethodAsync()
    {
        return Task.FromResult(result: 1);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                new[]
                {
                    // Test0.cs(9,25): error CS0103: The name 'MyInt' does not exist in the current context
                    DiagnosticResult.CompilerError("CS0103").WithSpan(9, 25, 9, 30).WithArguments("MyInt"),
                    // Test0.cs(9,32): error CS0103: The name 'MethodAsync' does not exist in the current context
                    DiagnosticResult.CompilerError("CS0103").WithSpan(9, 32, 9, 43).WithArguments("MethodAsync"),
                    // Test0.cs(9,32): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(9, 32, 9, 43),
                },
                fixedSource: code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task TestAssignmentExpression8()
        {
            var code = @"using System;
using System.Threading.Tasks;

class TestClass
{
    private async Task MyTestMethod1Async()
    {
        Func<Task> @delegate = delegate {
            int myInt = MyIntM  ethodAsync();
        };
    }

    private Task<int> MyIntMethodAsync()
    {
        return Task.FromResult(result: 1);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                new[]
                {
                    // Test0.cs(8,32): error CS1643: Not all code paths return a value in anonymous method of type 'Func<Task>'
                    DiagnosticResult.CompilerError("CS1643").WithSpan(8, 32, 8, 40).WithArguments("anonymous method", "System.Func<System.Threading.Tasks.Task>"),
                    // Test0.cs(9,25): error CS0103: The name 'MyIntM' does not exist in the current context
                    DiagnosticResult.CompilerError("CS0103").WithSpan(9, 25, 9, 31).WithArguments("MyIntM"),
                    // Test0.cs(9,33): error CS0103: The name 'ethodAsync' does not exist in the current context
                    DiagnosticResult.CompilerError("CS0103").WithSpan(9, 33, 9, 43).WithArguments("ethodAsync"),
                    // Test0.cs(9,33): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(9, 33, 9, 43),
                },
                fixedSource: code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task TestTernaryOperator()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> A()
    {
        return {|CS4016:true ? Task.FromResult(0) : Task.FromResult(1)|};
    }
}",
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> A()
    {
        return await (true ? Task.FromResult(0) : Task.FromResult(1));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task TestNullCoalescingOperator()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> A()
    {
        return {|CS4016:null ?? Task.FromResult(1)|} {|CS1002:}|}
}",
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> A()
    {
        return await (null ?? Task.FromResult(1)){|CS1002:}|}
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public async Task TestAsExpression()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> A()
    {
        return {|CS4016:null as Task<int>|} {|CS1002:}|}
}",
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<int> A()
    {
        return await (null as Task<int>){|CS1002:}|}
}");
        }
    }
}
