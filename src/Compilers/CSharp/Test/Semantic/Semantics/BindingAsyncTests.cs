// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class BindingAsyncTests : CompilingTestBase
    {
        [Fact]
        public void AsyncMethod()
        {
            var source = @"
using System.Threading.Tasks;
class C
{
    async void M(Task t)
    {
        await t;
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
            var method = (SourceMethodSymbol)compilation.GlobalNamespace.GetTypeMembers("C").Single().GetMembers("M").Single();
            Assert.True(method.IsAsync);
        }

        [Fact]
        public void AsyncLambdas()
        {
            var source = @"
using System;
using System.Threading.Tasks;
class C
{
    public static void Main()
    {
        Action<Task> f1 = async (Task t) => { await t; };
        Action<Task> f2 = async t => { await t; };
        Action<Task> f3 = async (t) => { await t; };
    }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var compilation = CreateCompilationWithMscorlib45(new SyntaxTree[] { tree }).VerifyDiagnostics();

            var model = compilation.GetSemanticModel(tree);

            var simple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SimpleLambdaExpressionSyntax>().Single();
            Assert.True(((LambdaSymbol)model.GetSymbolInfo(simple).Symbol).IsAsync);

            var parens = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>();
            Assert.True(parens.Count() == 2, "Expect exactly two parenthesized lambda expressions in the syntax tree.");
            foreach (var paren in parens)
            {
                Assert.True(((LambdaSymbol)model.GetSymbolInfo(paren).Symbol).IsAsync);
            }
        }

        [Fact]
        public void AsyncDelegates()
        {
            var source = @"
using System;
using System.Threading.Tasks;
class C
{
    public static void Main()
    {
        Action<Task> f4 = async delegate(Task t) { await t; };
    }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var compilation = CreateCompilationWithMscorlib45(new SyntaxTree[] { tree }).VerifyDiagnostics();

            var model = compilation.GetSemanticModel(tree);

            var del = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AnonymousMethodExpressionSyntax>().Single();
            Assert.True(((LambdaSymbol)model.GetSymbolInfo(del).Symbol).IsAsync);
        }

        [Fact]
        public void BadAsyncConstructor()
        {
            var source = @"
class C {
    async public C() { } 
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("async"));
        }

        [Fact]
        public void BadAsyncDestructor()
        {
            var source = @"
class C
{
    async extern ~C();
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("async"),
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "C").WithArguments("C.~C()"));
        }

        [Fact]
        public void BadAsyncEvent()
        {
            var source = @"
public delegate void MyDelegate();

class C
{
    public C() {
        MyEvent.Invoke();
    }

    async event MyDelegate MyEvent;
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "MyEvent").WithArguments("async"));
        }

        [Fact]
        public void BadAsyncField()
        {
            var source = @"
class C
{
    public C(int i)
    {
        this.i = i;
    }

    async int i;
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "i").WithArguments("async"));
        }

        [Fact]
        public void BadAsyncClass()
        {
            var source = @"
public async class C
{
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("async"));
        }

        [Fact]
        public void BadAsyncStruct()
        {
            var source = @"
internal async struct S
{
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S").WithArguments("async"));
        }

        [Fact]
        public void BadAsyncInterface()
        {
            var source = @"
internal async interface I
{
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "I").WithArguments("async"));
        }

        [Fact]
        public void BadAsyncDelegate()
        {
            var source = @"
public async delegate void MyDelegate();
";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "MyDelegate").WithArguments("async"));
        }

        [Fact]
        public void BadAsyncProperty()
        {
            var source = @"
public async delegate void MyDelegate();
";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "MyDelegate").WithArguments("async"));
        }

        [Fact]
        public void BadAsyncPropertyAccessor()
        {
            var source = @"
public async delegate void MyDelegate();
";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "MyDelegate").WithArguments("async"));
        }

        [Fact]
        public void TaskRetNoObjectRequired()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void InferTask(Func<Task> x) { }
    
    static void InferTaskOrTaskT(Func<Task> x) { }
    static void InferTaskOrTaskT(Func<Task<int>> x) { }

    static async Task F1()
    {
        return await Task.Factory.StartNew(() => 1);
    }

    static void Main()
    {
        Func<Task> F2 = async () => { await Task.Factory.StartNew(() => { }); return 1; };
        InferTask(async () => { return await Task.Factory.StartNew(() => 1); });
        InferTaskOrTaskT(async () => { return await Task.Factory.StartNew(() => 1); });
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
    // (14,9): error CS1997: Since 'C.F1()' is an async method that returns 'Task', a return keyword must not be followed by an object expression. Did you intend to return 'Task<T>'?
    //         return await Task.Factory.StartNew(() => 1);
    Diagnostic(ErrorCode.ERR_TaskRetNoObjectRequired, "return").WithArguments("C.F1()").WithLocation(14, 9),
    // (19,79): error CS8030: Async lambda expression converted to a 'Task' returning delegate cannot return a value. Did you intend to return 'Task<T>'?
    //         Func<Task> F2 = async () => { await Task.Factory.StartNew(() => { }); return 1; };
    Diagnostic(ErrorCode.ERR_TaskRetNoObjectRequiredLambda, "return").WithLocation(19, 79),
    // (20,33): error CS8030: Async lambda expression converted to a 'Task' returning delegate cannot return a value. Did you intend to return 'Task<T>'?
    //         InferTask(async () => { return await Task.Factory.StartNew(() => 1); });
    Diagnostic(ErrorCode.ERR_TaskRetNoObjectRequiredLambda, "return").WithLocation(20, 33)
    );
        }

        [Fact]
        public void BadAsyncReturn()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class MyTask : Task
{
    public MyTask(Action a) : base(a) { }
}

class C
{
    async int F1()
    {
        await Task.Factory.StartNew(() => { });
    }

    async MyTask F2()
    {
        await Task.Factory.StartNew(() => { });
    }

    async T F3<T>()
    {
        await Task.Factory.StartNew(() => { });
    }

    async T F4<T>() where T : Task
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (17,18): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async MyTask F2()
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "F2"),
                // (22,13): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T F3<T>()
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "F3"),
                // (27,13): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T F4<T>() where T : Task
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "F4"),
                // (12,15): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async int F1()
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "F1"),
                // (12,15): error CS0161: 'C.F1()': not all code paths return a value
                //     async int F1()
                Diagnostic(ErrorCode.ERR_ReturnExpected, "F1").WithArguments("C.F1()"),
                // (17,18): error CS0161: 'C.F2()': not all code paths return a value
                //     async MyTask F2()
                Diagnostic(ErrorCode.ERR_ReturnExpected, "F2").WithArguments("C.F2()"),
                // (22,13): error CS0161: 'C.F3<T>()': not all code paths return a value
                //     async T F3<T>()
                Diagnostic(ErrorCode.ERR_ReturnExpected, "F3").WithArguments("C.F3<T>()"),
                // (27,13): error CS0161: 'C.F4<T>()': not all code paths return a value
                //     async T F4<T>() where T : Task
                Diagnostic(ErrorCode.ERR_ReturnExpected, "F4").WithArguments("C.F4<T>()"));
        }

        [Fact]
        public void CantConvAsyncAnonFuncReturns()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void Main()
    {
        Func<int> f1 = async () => await Task.Factory.StartNew(() => 1);
        Func<int> f2 = async () => { return await Task.Factory.StartNew(() => 1); };
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (9,24): error CS4010: Cannot convert async lambda expression to delegate type 'System.Func<int>'. An async lambda expression may return void, Task or Task<T>, none of which are convertible to 'System.Func<int>'.
                //         Func<int> f1 = async () => await Task.Factory.StartNew(() => 1);
                Diagnostic(ErrorCode.ERR_CantConvAsyncAnonFuncReturns, "async () => await Task.Factory.StartNew(() => 1)").WithArguments("lambda expression", "System.Func<int>").WithLocation(9, 24),
                // (10,24): error CS4010: Cannot convert async lambda expression to delegate type 'System.Func<int>'. An async lambda expression may return void, Task or Task<T>, none of which are convertible to 'System.Func<int>'.
                //         Func<int> f2 = async () => { return await Task.Factory.StartNew(() => 1); };
                Diagnostic(ErrorCode.ERR_CantConvAsyncAnonFuncReturns, "async () => { return await Task.Factory.StartNew(() => 1); }").WithArguments("lambda expression", "System.Func<int>").WithLocation(10, 24)
            );
        }

        [Fact]
        public void BadAsyncReturnExpression()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void InferTask_T(Func<Task<int>> x) { }

    static void Main()
    {
        Func<Task<int>> f1 = async () => await Task.Factory.StartNew(() => new Task<int>(null));
        Func<Task<int>> f2 = async () => { return await Task.Factory.StartNew(() => new Task<int>(null)); };

        InferTask_T(async () => await Task.Factory.StartNew(() => new Task<int>(() => 1)));
        InferTask_T(async () => { return await Task.Factory.StartNew(() => new Task<int>(() => 1)); });
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (11,42): error CS4016: Since this is an async method, the return expression must be of type 'int' rather than 'Task<int>'
                //         Func<Task<int>> f1 = async () => await Task.Factory.StartNew(() => new Task<int>(null));
                Diagnostic(ErrorCode.ERR_BadAsyncReturnExpression, "await Task.Factory.StartNew(() => new Task<int>(null))").WithArguments("int"),
                // (12,51): error CS4016: Since this is an async method, the return expression must be of type 'int' rather than 'Task<int>'
                //         Func<Task<int>> f2 = async () => { return await Task.Factory.StartNew(() => new Task<int>(null)); };
                Diagnostic(ErrorCode.ERR_BadAsyncReturnExpression, "await Task.Factory.StartNew(() => new Task<int>(null))").WithArguments("int"),
                // (14,33): error CS4016: Since this is an async method, the return expression must be of type 'int' rather than 'Task<int>'
                //         InferTask_T(async () => await Task.Factory.StartNew(() => new Task<int>(() => 1)));
                Diagnostic(ErrorCode.ERR_BadAsyncReturnExpression, "await Task.Factory.StartNew(() => new Task<int>(() => 1))").WithArguments("int"),
                // (15,42): error CS4016: Since this is an async method, the return expression must be of type 'int' rather than 'Task<int>'
                //         InferTask_T(async () => { return await Task.Factory.StartNew(() => new Task<int>(() => 1)); });
                Diagnostic(ErrorCode.ERR_BadAsyncReturnExpression, "await Task.Factory.StartNew(() => new Task<int>(() => 1))").WithArguments("int"));
        }

        [Fact]
        public void AsyncCantReturnVoid()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void InferVoid(Action x) { }
    static void InferTask_T<T>(Func<Task<T>> x) { }
    static void Infer_T<T>(Func<T> x) { }

    static void Main()
    {
        InferVoid(async () => { return await Task.Factory.StartNew(() => { }); });
        InferTask_T(async () => { return await Task.Factory.StartNew(() => { }); });
        Infer_T(async () => { return await Task.Factory.StartNew(() => { }); });
    }
}";
            CreateCompilationWithMscorlib45(source, new MetadataReference[] { LinqAssemblyRef, SystemRef }).VerifyDiagnostics(
    // (13,33): error CS8029: Anonymous function converted to a void returning delegate cannot return a value
    //         InferVoid(async () => { return await Task.Factory.StartNew(() => { }); });
    Diagnostic(ErrorCode.ERR_RetNoObjectRequiredLambda, "return").WithLocation(13, 33),
    // (14,42): error CS4029: Cannot return an expression of type 'void'
    //         InferTask_T(async () => { return await Task.Factory.StartNew(() => { }); });
    Diagnostic(ErrorCode.ERR_CantReturnVoid, "await Task.Factory.StartNew(() => { })").WithLocation(14, 42),
    // (15,38): error CS4029: Cannot return an expression of type 'void'
    //         Infer_T(async () => { return await Task.Factory.StartNew(() => { }); });
    Diagnostic(ErrorCode.ERR_CantReturnVoid, "await Task.Factory.StartNew(() => { })").WithLocation(15, 38)
    );
        }

        [Fact]
        public void InferAsyncReturn()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void InferVoid(Action x) { }

    static void InferTask(Func<Task> x) { }

    static void InferTask_T<T>(Func<Task<T>> x) { }

    static void Main()
    {
        InferVoid(async () => { await Task.Factory.StartNew(() => { }); });

        InferTask(async () => { await Task.Factory.StartNew(() => { return; }); });

        InferTask_T(async () => { return await Task.Factory.StartNew(() => 1); });
    }
}";
            CreateCompilationWithMscorlib45(source, new MetadataReference[] { LinqAssemblyRef, SystemRef }).VerifyDiagnostics();
        }

        [Fact]
        public void BadInferAsyncReturn_T()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void Infer<T>(Func<bool, T> x) { }

    static void Main()
    {
        Infer(async (x) =>
        {
            await Task.Factory.StartNew(() => { });
            if (x)
            {
                return 1;
            }
            else
            {
                return;
            }
        });
    }
}";
            CreateCompilationWithMscorlib45(source, new MetadataReference[] { LinqAssemblyRef, SystemRef }).VerifyDiagnostics(
                // (19,17): error CS0126: An object of a type convertible to 'int' is required
                //                 return;
                Diagnostic(ErrorCode.ERR_RetObjectRequired, "return").WithArguments("int"));
        }

        [Fact]
        public void BadInferAsyncReturnVoid()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void Infer(Action<bool> x) { }

    static void Main()
    {
        Infer(async (x) =>
        {
            await Task.Factory.StartNew(() => { });
            if (x)
            {
                return 1;
            }
            else
            {
                return;
            }
        });
    }
}";
            CreateCompilationWithMscorlib45(source, new MetadataReference[] { LinqAssemblyRef, SystemRef }).VerifyDiagnostics(
    // (16,17): error CS8029: Anonymous function converted to a void returning delegate cannot return a value
    //                 return 1;
    Diagnostic(ErrorCode.ERR_RetNoObjectRequiredLambda, "return").WithLocation(16, 17)
    );
        }

        [Fact]
        public void BadInferAsyncReturnTask()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void Infer(Func<bool, Task> x) { }

    static void Main()
    {
        Infer(async (x) =>
        {
            await Task.Factory.StartNew(() => { });
            if (x)
            {
                return 1;
            }
            else
            {
                return;
            }
        });
    }
}";
            CreateCompilationWithMscorlib45(source, new MetadataReference[] { LinqAssemblyRef, SystemRef }).VerifyDiagnostics(
    // (16,17): error CS8030: Async lambda expression converted to a 'Task' returning delegate cannot return a value. Did you intend to return 'Task<T>'?
    //                 return 1;
    Diagnostic(ErrorCode.ERR_TaskRetNoObjectRequiredLambda, "return").WithLocation(16, 17)
    );
        }

        [Fact]
        public void BadInferAsyncReturnTask_T()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void Infer<T>(Func<bool, Task<T>> x) { }

    static void Main()
    {
        Infer(async (x) =>
        {
            await Task.Factory.StartNew(() => { });
            if (x)
            {
                return 1;
            }
            else
            {
                return;
            }
        });
    }
}";
            CreateCompilationWithMscorlib45(source, new MetadataReference[] { LinqAssemblyRef, SystemRef }).VerifyDiagnostics(
                // (19,17): error CS0126: An object of a type convertible to 'int' is required
                //                 return;
                Diagnostic(ErrorCode.ERR_RetObjectRequired, "return").WithArguments("int"));
        }

        [Fact]
        public void TaskReturningAsyncWithoutReturn()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    async static Task F()
    {
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (6,23): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async static Task F()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "F"));
        }

        [Fact]
        public void TestAsyncReturnsNullableT()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    async static Task<int?> F()
    {
        await Task.Factory.StartNew(() => { });
        return null;
    }

    static void Main()
    {
        Func<Task<int?>> f1 = async () => await Task.Factory.StartNew<int?>(() => { return null; });
        Func<Task<int?>> f2 = async () => { return await Task.Factory.StartNew<int?>(() => { return null; }); };
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }

        [Fact]
        public void VarargsAsync()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    async static Task M1(__arglist)
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (6,23): error CS4006: __arglist is not allowed in the parameter list of async methods
                //     async static Task M1(__arglist)
                Diagnostic(ErrorCode.ERR_VarargsAsync, "M1"));
        }

        [Fact]
        public void VarargsAsyncGeneric()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    async static Task M1<T>(__arglist)
    {
        await Task.Factory.StartNew(() => { });
        return;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (6,23): error CS0224: A method with vararg cannot be generic, be in a generic type, or have a params parameter
                //     async static Task M1<T>(__arglist)
                Diagnostic(ErrorCode.ERR_BadVarargs, "M1"));
        }

        [Fact]
        public void VarargsAsyncInGenericClass()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    async static Task M1<T>(__arglist)
    {
        await Task.Factory.StartNew(() => { });
        return;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (6,23): error CS0224: A method with vararg cannot be generic, be in a generic type, or have a params parameter
                //     async static Task M1<T>(__arglist)
                Diagnostic(ErrorCode.ERR_BadVarargs, "M1"));
        }

        [Fact]
        public void UnsafeAsyncArgType()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    unsafe async static Task M1(int* i) { }
}";
            CreateCompilationWithMscorlib45(source, null, TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,38): error CS4005: Async methods cannot have unsafe parameters or return types
                //     unsafe async static Task M1(int* i)
                Diagnostic(ErrorCode.ERR_UnsafeAsyncArgType, "i"),
                // (6,30): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     unsafe async static Task M1(ref int* i)
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M1"));
        }

        [Fact]
        public void Ref_and_UnsafeAsyncArgType()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    unsafe async static Task M1(ref int* i) { }
}";
            CreateCompilationWithMscorlib45(source, null, TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,42): error CS1988: Async methods cannot have ref or out parameters
                //     unsafe async static Task M1(ref int* i)
                Diagnostic(ErrorCode.ERR_BadAsyncArgType, "i"),
                // (6,30): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     unsafe async static Task M1(ref int* i)
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M1"));
        }

        [Fact]
        public void RefAsyncArgType()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    async static Task M1(ref int i)
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (6,42): error CS1988: Async methods cannot have ref or out parameters
                //     unsafe async static Task M1(ref int* i) { }
                Diagnostic(ErrorCode.ERR_BadAsyncArgType, "i"));
        }

        [Fact]
        public void OutAsyncArgType()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    async static Task M1(out int i)
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (6,34): error CS1988: Async methods cannot have ref or out parameters
                //     async static Task M1(out int i) { }
                Diagnostic(ErrorCode.ERR_BadAsyncArgType, "i"));
        }

        [Fact]
        public void BadAwaitWithoutAsync()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class MyAttribute : Attribute {
    public MyAttribute(int i) { }
}

[MyAttribute(await C.t)]
class C
{
    public static Task<int> t = new Task<int>(() => 1);

    int i = await t;
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (9,14): error CS1992: The 'await' operator can only be used when contained within a method or lambda expression marked with the 'async' modifier
                // [MyAttribute(await C.t)]
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsync, "await C.t"),
                // (14,13): error CS1992: The 'await' operator can only be used when contained within a method or lambda expression marked with the 'async' modifier
                //     int i = await t;
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsync, "await t"));
        }

        [Fact]
        public void BadAwaitWithoutAsync_AnonMeth_Lambda()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    public static void Main()
    {
        Action f1 = delegate() { await Task.Factory.StartNew(() => { }); };
        Action f2 = () => await Task.Factory.StartNew(() => { });
        Action f3 = () => { await Task.Factory.StartNew(() => { }); };
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (9,34): error CS4034: The 'await' operator can only be used within an async anonymous method. Consider marking this anonymous method with the 'async' modifier.
                //         Action f1 = delegate() { await Task.Factory.StartNew(() => { }); };
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncLambda, "await Task.Factory.StartNew(() => { })").WithArguments("anonymous method"),
                // (10,27): error CS4034: The 'await' operator can only be used within an async lambda expression. Consider marking this lambda expression with the 'async' modifier.
                //         Action f2 = () => await Task.Factory.StartNew(() => { });
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncLambda, "await Task.Factory.StartNew(() => { })").WithArguments("lambda expression"),
                // (11,29): error CS4034: The 'await' operator can only be used within an async lambda expression. Consider marking this lambda expression with the 'async' modifier.
                //         Action f3 = () => { await Task.Factory.StartNew(() => { }); };
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncLambda, "await Task.Factory.StartNew(() => { })").WithArguments("lambda expression"));
        }

        [Fact]
        public void IDS_BadAwaitWithoutAsyncMethod_VoidMethod()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    public static void F()
    {
        await Task.Factory.StartNew(() => { });
    }

    public static int G()
    {
        return await Task.Factory.StartNew(() => 1);
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (8,9): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
                //         await Task.Factory.StartNew(() => { });
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, "await Task.Factory.StartNew(() => { })"),
                // (13,16): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task<int>'.
                //         return await Task.Factory.StartNew(() => 1);
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, "await Task.Factory.StartNew(() => 1)").WithArguments("int"));
        }

        [Fact]
        public void IDS_BadAwaitAsDefaultParam()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    static Task t = new Task(null);

    class await { }

    static int Foo(int[] arr = await t)
    {
        return arr.Length;
    }

    static int Main()
    {
        return 1;
    }
}";

            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                /// (10,32): error CS4032: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task<int>'.
                //     static int Foo(int[] arr = await t)
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, "await t").WithArguments("int"),
                // (10,26): error CS1750: A value of type '?' cannot be used as a default parameter because there are no standard conversions to type 'int[]'
                //     static int Foo(int[] arr = await t)
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "arr").WithArguments("?", "int[]"));
        }

        [Fact]
        public void BadAwaitInFinally()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    async static Task M1() {
        try
        {
        }
        catch
        {
            try
            {
            }
            catch
            {
            }
            finally
            {
                await Task.Factory.StartNew(() => { });
            }
        }
        finally
        {
            try
            {
            }
            catch
            {
                await Task.Factory.StartNew(() => { });
            }
            finally
            {
            }
        }
    }
}";
            CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp5)).VerifyDiagnostics(
                // (20,17): error CS1984: Cannot await in the body of a finally clause
                //                 await Task.Factory.StartNew(() => { });
                Diagnostic(ErrorCode.ERR_BadAwaitInFinally, "await Task.Factory.StartNew(() => { })").WithLocation(20, 17),
                // (30,17): error CS1984: Cannot await in the body of a finally clause
                //                 await Task.Factory.StartNew(() => { });
                Diagnostic(ErrorCode.ERR_BadAwaitInFinally, "await Task.Factory.StartNew(() => { })").WithLocation(30, 17)
                );
            CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)).VerifyDiagnostics();
        }

        [Fact]
        public void BadAwaitInCatch()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    async static Task M1() {
        try
        {
        }
        catch
        {
            await Task.Factory.StartNew(() => { });
        }
        finally
        {
        }
    }
}";
            CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp5)).VerifyDiagnostics(
                // (12,13): error CS1985: Cannot await in a catch clause
                //             await Task.Factory.StartNew(() => { });
                Diagnostic(ErrorCode.ERR_BadAwaitInCatch, "await Task.Factory.StartNew(() => { })").WithLocation(12, 13)
                );
            CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)).VerifyDiagnostics();
        }

        [Fact]
        public void BadAwaitInCatchFilter()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    async static Task M1() 
    {
        try
        {
        }
        catch when (await Task.Factory.StartNew(() => false))
        {
        }
        finally
        {
        }
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (11,19): error CS7094: Cannot await in the filter expression of a catch clause
                //         catch when (await Task.Factory.StartNew(() => false))
                Diagnostic(ErrorCode.ERR_BadAwaitInCatchFilter, "await Task.Factory.StartNew(() => false)").WithLocation(11, 21)
                );
        }

        [Fact]
        public void BadAwaitInLock()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    async static Task M1() {

        lock (new object())
        {
            await Task.Factory.StartNew(() => { });

            try
            {
            }
            catch
            {
                await Task.Factory.StartNew(() => { });
            }
            finally
            {
                await Task.Factory.StartNew(() => { });
            }
        }

        try
        {
        }
        catch
        {
            lock (new object())
            {
                await Task.Factory.StartNew(() => { });
            }
        }
        finally
        {
            lock (new object())
            {
                await Task.Factory.StartNew(() => { });
            }
        }
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,13): error CS1996: Cannot await in the body of a lock statement
                //             await Task.Factory.StartNew(() => { });
                Diagnostic(ErrorCode.ERR_BadAwaitInLock, "await Task.Factory.StartNew(() => { })"),
                // (17,17): error CS1996: Cannot await in the body of a lock statement
                //                 await Task.Factory.StartNew(() => { });
                Diagnostic(ErrorCode.ERR_BadAwaitInLock, "await Task.Factory.StartNew(() => { })"),
                // (21,17): error CS1996: Cannot await in the body of a lock statement
                //                 await Task.Factory.StartNew(() => { });
                Diagnostic(ErrorCode.ERR_BadAwaitInLock, "await Task.Factory.StartNew(() => { })"),
                // (32,17): error CS1996: Cannot await in the body of a lock statement
                //                 await Task.Factory.StartNew(() => { });
                Diagnostic(ErrorCode.ERR_BadAwaitInLock, "await Task.Factory.StartNew(() => { })"),
                // (39,17): error CS1996: Cannot await in the body of a lock statement
                //                 await Task.Factory.StartNew(() => { });
                Diagnostic(ErrorCode.ERR_BadAwaitInLock, "await Task.Factory.StartNew(() => { })"));
        }

        [Fact]
        public void BadAwaitInLock2()
        {
            var source = @"
using System.Threading.Tasks;

class Program
{
    async void Test()
    {
        lock(await M()) // fine, not in body of lock
            lock (await M()) // error, in body of outer lock
            {
                await M(); // error, in body of inner lock
            }
    }

    async Task<object> M()
    {
        return await M();
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (9,19): error CS1996: Cannot await in the body of a lock statement
                //             lock (await M()) // error, in body of outer lock
                Diagnostic(ErrorCode.ERR_BadAwaitInLock, "await M()"),
                // (11,17): error CS1996: Cannot await in the body of a lock statement
                //                 await M(); // error, in body of inner lock
                Diagnostic(ErrorCode.ERR_BadAwaitInLock, "await M()"));
        }

        [Fact]
        public void AwaitingInLockExpressionsIsActuallyOK()
        {
            var source = @"
using System.Threading.Tasks;

class Driver
{
    public static async void F()
    {
        object o = new object();
        lock(await Task.Factory.StartNew(() => o))
        {
            
        }
    }

    static void Main() { }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }

        [WorkItem(611150, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611150")]
        [Fact]
        public void AwaitInLambdaInLock()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    static void Main()
    {
        lock (new object())
        {
            Task.Run(async () => { await Task.Factory.StartNew(() => { }); });
            Task.Run(async () => await Task.Factory.StartNew(() => { }));
            Task.Run(async delegate () { await Task.Factory.StartNew(() => { }); });
        }
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }

        [Fact]
        public void BadAwaitInUnsafeContext()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    unsafe async static Task M1() {
        await Task.Factory.StartNew(() => { });  // not OK
    }

    async static Task M2()
    {
        await Task.Factory.StartNew(() => { }); // OK

        unsafe
        {
            await Task.Factory.StartNew(() => { }); // not OK
        }
    }
}";
            CreateCompilationWithMscorlib45(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,9): error CS4004: Cannot await in an unsafe context
                //         await Task.Factory.StartNew(() => { });  // not OK
                Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await Task.Factory.StartNew(() => { })"),
                // (16,13): error CS4004: Cannot await in an unsafe context
                //             await Task.Factory.StartNew(() => { }); // not OK
                Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await Task.Factory.StartNew(() => { })"));
        }

        [Fact]
        public void BadAwaitWithoutAsyncInBadContext()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    static Task M2()
    {
        await Task.Factory.StartNew(() => { });

        unsafe
        {
            await Task.Factory.StartNew(() => { });
        }

        try
        {
        }
        catch
        {
            await Task.Factory.StartNew(() => { });
        }
        finally
        {
            await Task.Factory.StartNew(() => { });
        }
    }
}";
            CreateCompilationWithMscorlib45(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,9): error CS4032: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task<System.Threading.Tasks.Task>'.
                //         await Task.Factory.StartNew(() => { });
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, "await Task.Factory.StartNew(() => { })").WithArguments("System.Threading.Tasks.Task").WithLocation(8, 9),
                // (12,13): error CS4032: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task<System.Threading.Tasks.Task>'.
                //             await Task.Factory.StartNew(() => { });
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, "await Task.Factory.StartNew(() => { })").WithArguments("System.Threading.Tasks.Task").WithLocation(12, 13),
                // (12,13): error CS4004: Cannot await in an unsafe context
                //             await Task.Factory.StartNew(() => { });
                Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await Task.Factory.StartNew(() => { })").WithLocation(12, 13),
                // (20,13): error CS4032: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task<System.Threading.Tasks.Task>'.
                //             await Task.Factory.StartNew(() => { });
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, "await Task.Factory.StartNew(() => { })").WithArguments("System.Threading.Tasks.Task").WithLocation(20, 13),
                // (24,13): error CS4032: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task<System.Threading.Tasks.Task>'.
                //             await Task.Factory.StartNew(() => { });
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, "await Task.Factory.StartNew(() => { })").WithArguments("System.Threading.Tasks.Task").WithLocation(24, 13),
                // (6,17): error CS0161: 'Test.M2()': not all code paths return a value
                //     static Task M2()
                Diagnostic(ErrorCode.ERR_ReturnExpected, "M2").WithArguments("Test.M2()").WithLocation(6, 17));
        }

        [Fact]
        public void AsyncExplicitInterfaceImplementation()
        {
            var source = @"
using System.Threading.Tasks;

interface IInterface
{
    void F(); 
}

class C : IInterface
{
    async void IInterface.F()
    {
        await Task.Factory.StartNew(() => { });
    }

    static void Main()
    {
        
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }

        [Fact]
        public void AsyncInterfaceMember()
        {
            var source = @"
interface IInterface
{
    async void F(); 
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (4,16): error CS0106: The modifier 'async' is not valid for this item
                //     async void F(); 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "F").WithArguments("async"));
        }

        [Fact]
        public void MainCantBeAsync()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static void Main()
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (4,23): error CS4009: 'A.Main()': an entry point cannot be marked with the 'async' modifier
                //     async static void Main()
                Diagnostic(ErrorCode.ERR_MainCantBeAsync, "Main").WithArguments("A.Main()"));
        }

        [Fact]
        public void MainCantBeAsync_AndGeneric()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static void Main<T>()
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (4,23): warning CS0402: 'A.Main<T>()': an entry point cannot be generic or in a generic type
                //     async static void Main<T>()
                Diagnostic(ErrorCode.WRN_MainCantBeGeneric, "Main").WithArguments("A.Main<T>()"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void MainCantBeAsync_AndBadSig()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static void Main(bool truth)
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (4,23): warning CS0028: 'A.Main(bool)' has the wrong signature to be an entry point
                //     async static void Main(bool truth)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main(bool)"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void MainCantBeAsync_AndGeneric_AndBadSig()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static void Main<T>(bool truth)
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (4,23): warning CS0028: 'A.Main<T>(bool)' has the wrong signature to be an entry point
                //     async static void Main<T>(bool truth)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main<T>(bool)"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void AwaitInQuery_FirstCollectionExpressionOfInitialFrom()
        {
            var source = @"
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    async static Task<List<int>> F1()
    {
        return await Task.Factory.StartNew(() => new List<int>() { 1, 2, 3 });
    }

    async static void F2()
    {
        await Task.Factory.StartNew(() => { });

        var ls = new List<int>() {1, 2, 3};

        var xs = from l in await F1() where l > 1 select l;
    }
}";
            CreateCompilationWithMscorlib45(source, new MetadataReference[] { SystemRef, LinqAssemblyRef }).VerifyDiagnostics();
        }

        [Fact]
        public void AwaitInQuery_CollectionExpressionOfJoin()
        {
            var source = @"
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    async static Task<List<int>> F1()
    {
        return await Task.Factory.StartNew(() => new List<int>() { 1, 2, 3 });
    }

    async static void F2()
    {
        await Task.Factory.StartNew(() => { });

        var ls = new List<int>() {1, 2, 3};

        var xs = from l in ls
                 join l2 in await F1() on l equals l2
                 where l > 1 select l;
    }
}";
            CreateCompilationWithMscorlib45(source, new MetadataReference[] { SystemRef, LinqAssemblyRef }).VerifyDiagnostics();
        }

        [Fact]
        public void BadAwaitInQuery_QueryBody()
        {
            var source = @"
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    async static Task<List<int>> F1()
    {
        return await Task.Factory.StartNew(() => new List<int>() { 1, 2, 3 });
    }

    async static void F2()
    {
        await Task.Factory.StartNew(() => { });

        var ls = new List<int>() {1, 2, 3};

        var xs = from l in ls
                 where l > 1 select await F1();
    }
}";
            CreateCompilationWithMscorlib45(source, new MetadataReference[] { SystemRef, LinqAssemblyRef }).VerifyDiagnostics(
                // (20,37): error CS1995: The 'await' operator may only be used in a query expression within the first collection expression of the initial 'from' clause or within the collection expression of a 'join' clause
                //                  where l > 1 select await F1();
                Diagnostic(ErrorCode.ERR_BadAwaitInQuery, "await F1()"));
        }

        [Fact]
        public void BadAwaitInQuery_FirstCollectionExpressionOfInitialFrom_InsideQueryBody()
        {
            var source = @"
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    async static Task<List<int>> F1()
    {
        return await Task.Factory.StartNew(() => new List<int>() { 1, 2, 3 });
    }

    async static void F2()
    {
        await Task.Factory.StartNew(() => { });

        var ls = new List<int>() { 1, 2, 3 };

        var xs = from l in ls
                 where l > 1
                 select (from l2 in await F1() where l2 > 1 select l2);
    }
}";
            CreateCompilationWithMscorlib45(source, new MetadataReference[] { SystemRef, LinqAssemblyRef }).VerifyDiagnostics(
                // (21,37): error CS1995: The 'await' operator may only be used in a query expression within the first collection expression of the initial 'from' clause or within the collection expression of a 'join' clause
                //                  select (from l2 in await F1() where l2 > 1 select l2);
                Diagnostic(ErrorCode.ERR_BadAwaitInQuery, "await F1()"));
        }

        [Fact]
        public void BadAwaitInQuery_CollectionExpressionOfJoin_InsideQueryBody()
        {
            var source = @"
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    async static Task<List<int>> F1()
    {
        return await Task.Factory.StartNew(() => new List<int>() { 1, 2, 3 });
    }

    async static void F2()
    {
        await Task.Factory.StartNew(() => { });

        var ls = new List<int>() { 1, 2, 3 };

        var xs = from l in ls
                 where l > 1
                 select (from l2 in ls
                         join l3 in await F1() on l2 equals l3
                         where l2 > 1
                         select l2);
    }
}";
            CreateCompilationWithMscorlib45(source, new MetadataReference[] { SystemRef, LinqAssemblyRef }).VerifyDiagnostics(
                // (22,37): error CS1995: The 'await' operator may only be used in a query expression within the first collection expression of the initial 'from' clause or within the collection expression of a 'join' clause
                //                          join l3 in await F1() on l2 equals l3
                Diagnostic(ErrorCode.ERR_BadAwaitInQuery, "await F1()"));
        }

        [Fact]
        public void BadAwaitWithoutAsyncInUnsafeQuery()
        {
            var source = @"
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    async static Task<List<int>> F1()
    {
        return await Task.Factory.StartNew(() => new List<int>() { 1, 2, 3 });
    }

    static void F2()
    {
        var ls = new List<int>() { 1, 2, 3 };

        unsafe
        {
            var xs = from l in ls
                     where l > 1
                     select (from l2 in ls
                             join l3 in await F1() on l2 equals l3
                             where l2 > 1
                             select l2);
        }
    }
}";
            var c = CreateCompilationWithMscorlib45(
                source,
                new MetadataReference[] { SystemRef, LinqAssemblyRef },
                TestOptions.UnsafeReleaseDll);

            c.VerifyDiagnostics(
                // (22,41): error CS4004: Cannot await in an unsafe context
                //                              join l3 in await F1() on l2 equals l3
                Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await F1()"),
                // (22,41): error CS1995: The 'await' operator may only be used in a query expression within the first collection expression of the initial 'from' clause or within the collection expression of a 'join' clause
                //                              join l3 in await F1() on l2 equals l3
                Diagnostic(ErrorCode.ERR_BadAwaitInQuery, "await F1()"));
        }

        [Fact]
        public void BadAwaitWithoutAsyncInQuery()
        {
            var source = @"
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    async static Task<List<int>> F1()
    {
        return await Task.Factory.StartNew(() => new List<int>() { 1, 2, 3 });
    }

    static void F2()
    {
        var ls = new List<int>() { 1, 2, 3 };

        var xs = from l in ls
                    where l > 1
                    select (from l2 in ls
                            join l3 in await F1() on l2 equals l3
                            where l2 > 1
                            select l2);
    }
}";
            CreateCompilationWithMscorlib45(
                source,
                new MetadataReference[] { SystemRef, LinqAssemblyRef },
                TestOptions.ReleaseDll).VerifyDiagnostics(

                // (22,41): error CS1995: The 'await' operator may only be used in a query expression within the first collection expression of the initial 'from' clause or within the collection expression of a 'join' clause
                //                              join l3 in await F1() on l2 equals l3
                Diagnostic(ErrorCode.ERR_BadAwaitInQuery, "await F1()"));
        }

        [Fact]
        public void BadAwaitWithoutAsyncInLegalQueryRegion()
        {
            var source = @"
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    async static Task<List<int>> F1()
    {
        return await Task.Factory.StartNew(() => new List<int>() { 1, 2, 3 });
    }

    static void F2()
    {
        var ls = new List<int>() { 1, 2, 3 };

        var xs = from l in await F1()
                    where l > 1
                    select l;
    }
}";
            CreateCompilationWithMscorlib45(source, new MetadataReference[] { SystemRef, LinqAssemblyRef }).VerifyDiagnostics(
                // (17,28): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
                //         var xs = from l in await F1()
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, "await F1()"));
        }

        [Fact]
        public void AsyncLacksAwaits()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    async static Task M()
    {
    }

    static void Main()
    {
        Action f1 = async () => new Action(() => { })();
        Action f2 = async () => { };
        Func<Task<int>> f3 = async () => { return 1; };
        Action f4 = async delegate () { };
        Func<Task<int>> f5 = async delegate () { return 1; };
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (7,23): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async static Task M()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M"),
                // (13,21): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Action f1 = async () => new Action(() => { })();
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "async () => new Action(() => { })()"),
                // (14,21): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Action f2 = async () => { };
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "async () => { }"),
                // (15,30): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Func<Task<int>> f3 = async () => { return 1; };
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "async () => { return 1; }"),
                // (16,21): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Action f4 = async delegate () { };
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "async delegate () { }"),
                // (17,30): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Func<Task<int>> f5 = async delegate () { return 1; };
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "async delegate () { return 1; }"));
        }

        [Fact]
        public void UnobservedAwaitableExpression()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    public static async Task F1()
    {
        await Task.Factory.StartNew(() => 1);
    }

    public static async Task<int> F2(bool truth)
    {
        for (F1(); truth; F1()) ;

        for (F1(), F1(); truth; F1(), F1()) ;

        return await Task.Factory.StartNew(() => 1);
    }

    static void Main()
    {
        F2(false);
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         for (F1(); truth; F1()) ;
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "F1()"),
                // (13,27): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         for (F1(); truth; F1()) ;
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "F1()"),
                // (15,14): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         for (F1(), F1(); truth; F1(), F1()) ;
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "F1()"),
                // (15,20): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         for (F1(), F1(); truth; F1(), F1()) ;
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "F1()"),
                // (15,33): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         for (F1(), F1(); truth; F1(), F1()) ;
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "F1()"),
                // (15,39): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         for (F1(), F1(); truth; F1(), F1()) ;
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "F1()"),
                // (22,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         F2(false);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "F2(false)"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait01()
        {
            // invoke a method that returns an awaitable type in an async method
            var source = @"
using System.Threading.Tasks;
class Test
{
    public async void Meth()
    {
        await Task.Delay(10);
        Foo();
    }

    public async Task<int> Foo()
    {
        await Task.Delay(10);
        return 1;
    }

    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (16,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Foo();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Foo()"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait02()
        {
            // invoke a method that returns an awaitable type in an async method

            var source = @"
using System.Threading.Tasks;

class Test
{
    public async Task Meth()
    {
        Foo();
        foreach (var x in new int[] { 1, 2 })
        {
            Foo();
        }

        while (await Foo())
        {
            Foo();
        }
    }

    public async Task<dynamic> Foo()
    {
        return 1;
    }

    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { CSharpRef, SystemCoreRef }).VerifyDiagnostics(
                // (19,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Foo();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Foo()"),
                // (22,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             Foo();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Foo()"),
                // (27,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             Foo();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Foo()"),
                // (31,32): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public async Task<dynamic> Foo()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Foo"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait03()
        {
            // invoke a method that returns an awaitable type in an async method
            var source = @"
using System.Threading.Tasks;

public delegate Task<decimal?> Bar();
class Test
{
    static async Task<object> Meth()
    {
        Foo();

        Bar bar = Foo;
        bar();

        return (object)"""";
    }

    static async Task<decimal?> Foo()
    {
        return null;
    }

    static Task Meth2()
    {
        Foo();

        Bar bar = Foo;
        bar();
        return Task.Run(async () => { await Task.Delay(10); });
    }

    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (21,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Foo();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Foo()"),
                // (24,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         bar();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "bar()"),
                // (19,31): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async Task<object> Meth()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Meth"),
                // (29,33): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async Task<decimal?> Foo()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Foo"),
                // (36,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Foo();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Foo()"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait04()
        {
            // invoke a method that returns an awaitable type in an async method
            var source = @"
using System.Threading.Tasks;

class C1{}

static class Extension
{
    public static Task<C1> MethExt(this int i)
    {
        return Task.Run(async () => new C1());
    }
}

class Test
{
    static async Task<T> Meth<T>(T t)
    {
        int i = 0;
        i.MethExt();
        Foo(1);
        return t;
    }

    static Task<T> Foo<T>(T t)
    {
        return Task.Run(async () => { return t; });
    }
    
    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (22,25): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         return Task.Run(async () => new C1());
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "async () => new C1()"),
                // (28,26): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async Task<T> Meth<T>(T t)
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Meth"),
                // (31,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         i.MethExt();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "i.MethExt()"),
                // (32,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Foo(1);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Foo(1)"),
                // (38,25): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         return Task.Run(async () => { return t; });
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "async () => { return t; }"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait05()
        {
            // invoke a method that returns an awaitable type in an async method
            var source = @"
using System;
using System.Threading.Tasks;
class Test
{
    static async Task<dynamic> Meth1()
    {
        throw new EntryPointNotFoundException();
        Foo();       
        return """";
    }

    static async Task<decimal?> Meth2()
    {
        Foo();
        throw new DuplicateWaitObjectException();
        return null;
    }

    static async Task<decimal?> Foo()
    {
        return null;
    }

    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { CSharpRef, SystemCoreRef }).VerifyDiagnostics(
                // (20,32): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async Task<dynamic> Meth1()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Meth1"),
                // (23,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Foo();       
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Foo()"),
                // (23,9): warning CS0162: Unreachable code detected
                //         Foo();       
                Diagnostic(ErrorCode.WRN_UnreachableCode, "Foo"),
                // (27,33): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async Task<decimal?> Meth2()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Meth2"),
                // (29,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Foo();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Foo()"),
                // (31,9): warning CS0162: Unreachable code detected
                //         return null;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return"),
                // (34,33): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async Task<decimal?> Foo()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Foo"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait05_b()
        {
            // invoke a method that returns an awaitable type in an async method
            var source = @"
using System.Threading.Tasks;

class Test
{
    static async Task<dynamic> Meth1()
    {
        return """";
        Foo();
    }

    static int Meth2()
    {
        return 2;
        Foo();
    }

    static async Task<double?> Foo()
    {
        return null;
    }

    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { CSharpRef, SystemCoreRef }).VerifyDiagnostics(
                // (22,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Foo();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Foo()"),
                // (19,32): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async Task<dynamic> Meth1()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Meth1"),
                // (22,9): warning CS0162: Unreachable code detected
                //         Foo();
                Diagnostic(ErrorCode.WRN_UnreachableCode, "Foo"),
                // (28,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Foo();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Foo()"),
                // (28,9): warning CS0162: Unreachable code detected
                //         Foo();
                Diagnostic(ErrorCode.WRN_UnreachableCode, "Foo"),
                // (31,32): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async Task<double?> Foo()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Foo"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait06()
        {
            // invoke a method that returns an awaitable type in an async method
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<T> Meth<T>(T t)
    {
        await Task.Delay(10);
        return t;
    }

    static async Task<T> Foo<T>(T t)
    {
        Func<Task<int>> f = async () =>
        {
            await Task.Delay(10);
            Meth(1);
            return 2;
        };
        f();

        Func<Task<Func<Task<int>>>> ff = async delegate()
            {
                Meth((dynamic)5.1);
                await Task.Delay(10);
                return (Func<Task<int>>)(async () =>
                    {
                        Meth("""");
                        await Task.Delay(10);
                        return 3;
                    });
            };

        ff();

        (await ff())();

        return t;
    }

    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (30,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             Meth(1);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Meth(1)"),
                // (33,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         f();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "f()"),
                // (41,25): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //                         Meth("");
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, @"Meth("""")"),
                // (47,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         ff();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "ff()"),
                // (49,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         (await ff())();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "(await ff())()"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait07()
        {
            // invoke a method that returns an awaitable type in an async method
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
struct MyDisp : IDisposable
{
    public static int DisposeCalledCount;
    public void Dispose()
    {
        DisposeCalledCount++;
        TestCase.DisposeCalled++;
    }

    public MyDisp(int Count = 0)
    {
        DisposeCalledCount = 0;
    }
}

class TestCase
{
    private int tests;
    public static int DisposeCalled;

    public async Task Meth(MyDisp x)
    {
        await Task.Delay(10);
    }

    public async void Run()
    {
        this.tests = 0;
        TestCase.DisposeCalled = 0;

        //using statement inside async void sub
        tests++;
        using (var x = new MyDisp())
        {
            Meth(x);
        }
        if (MyDisp.DisposeCalledCount == TestCase.DisposeCalled)
            Driver.Count++;

        //using statement inside Task returning lambda
        this.tests++;
        Func<Task> f = async () =>
        {
            await Task.Delay(10);
            using (var x = new MyDisp())
            {
                Meth(x);
            }
        };
        f();
        await f();
        if (MyDisp.DisposeCalledCount == TestCase.DisposeCalled)
            Driver.Count++;

        //using statement inside Task<decimal> returning lambda
        this.tests++;
        Func<Task<decimal>> g = async () =>
        {
            await Task.Delay(10);
            Meth(new MyDisp());
            using (var x = new MyDisp())
            {
                Task.Run(async () =>
                {
                    new Action<MyDisp>((y) => { })(x);
                    await Task.Delay(10);
                });
            }
            return 1;
        };

        var t = await g();
        if (MyDisp.DisposeCalledCount == TestCase.DisposeCalled && t == 1)
            Driver.Count++;

        Driver.Result = Driver.Count - this.tests;
        //When test complete, set the flag.
        Driver.CompletedSignal.Set();
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static int Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        return Driver.Result;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (55,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             Meth(x);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Meth(x)"),
                // (67,17): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //                 Meth(x);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Meth(x)"),
                // (70,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         f();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "f()"),
                // (80,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             Meth(new MyDisp());
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Meth(new MyDisp())"),
                // (83,17): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //                 Task.Run(async () =>
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, @"Task.Run(async () =>
                {
                    new Action<MyDisp>((y) => { })(x);
                    await Task.Delay(10);
                })"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait08()
        {
            // invoke a method that returns an awaitable type in an async method
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    static async Task<T> Meth<T>(T t)
    {
        await Task.Delay(10);
        return t;
    }

    static async Task Foo()
    {
        try
        {
            Meth(1);
        }
        catch (Exception)
        {
            Meth("""");
        }
        finally
        {
            Meth((decimal?)2);
        }
    }

    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (28,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             Meth(1);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Meth(1)"),
                // (32,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             Meth("");
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, @"Meth("""")"),
                // (36,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             Meth((decimal?)2);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Meth((decimal?)2)"),
                // (24,23): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async Task Foo()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Foo"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait09()
        {
            // invoke a method that returns an awaitable type in an async method
            var source = @"
using System;
using System.Threading.Tasks;

static class Extension
{
    public static async Task Meth<T>(this int i, T t)
    {
        await Task.Delay(10);
    }
}

class Test
{
    static async Task<T> Foo<T>(T t)
    {
        var i = 0;
        Func<Task> f = async delegate()
        {
            await Task.Delay(10);
            i.Meth(1);
        };

        Func<Task<Func<Task<int>>>> ff = async () =>
        {
            f();
            await Task.Delay(10);
            return (Func<Task<int>>)(async delegate()
            {
                i.Meth("""");
                await Task.Delay(10);
                return 3;
            });
        };

        ff();

        (await ff())();

        return t;
    }

    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (33,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             i.Meth(1);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "i.Meth(1)"),
                // (38,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             f();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "f()"),
                // (42,17): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //                 i.Meth("");
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, @"i.Meth("""")"),
                // (48,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         ff();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "ff()"),
                // (50,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         (await ff())();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "(await ff())()"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait10()
        {
            // invoke a method that returns an awaitable type in an async method
            var source = @"
using System.Threading.Tasks;

class Test
{
    async Task<T> Meth<T>(T t)
    {
        await Task.Delay(10);
        return t;
    }

    private Task field;
    public Task Prop
    {
        get
        {
            Meth(1);
            return field;
        }
        set
        {
            Bar();
            Meth("""");
            field = value;
        }
    }

    public async Task Foo()
    {
        await Bar();
        Bar(); //the callee return type is dynamic, it will not give warning
        Meth((decimal?)null);
    }

    public dynamic Bar()
    {
        return Task.Run<int>(async () => { await Task.Delay(10); return 2; });
    }

    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { CSharpRef, SystemCoreRef }).VerifyDiagnostics(
                // (30,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             Meth(1);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Meth(1)"),
                // (36,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             Meth("");
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, @"Meth("""")"),
                // (45,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Meth((decimal?)null);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Meth((decimal?)null)"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait11()
        {
            // invoke a method that returns an awaitable type in an async method
            var source = @"
using System.Threading.Tasks;

partial class Test
{
    partial void Bar();
    public async Task<T> Meth<T>(params T[] t)
    {
        await Task.Delay(10);
        return t[0];
    }
}

partial class Test
{
    async partial void Bar()
    {
        await Task.Delay(10);
        Meth("""", null);
    }

    async public Task Foo()
    {
        Bar();
        Meth(Task.Run(async () => 1), Meth(1));
    }

    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (30,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Meth("", null);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, @"Meth("""", null)"),
                // (36,23): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Meth(Task.Run(async () => 1), Meth(1));
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "async () => 1"),
                // (36,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Meth(Task.Run(async () => 1), Meth(1));
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Meth(Task.Run(async () => 1), Meth(1))"),
                // (33,23): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async public Task Foo()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Foo"));
        }

        [WorkItem(611150, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611150")]
        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait12()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    static async Task<T> Meth<T>(T t)
    {
        await Task.Delay(10);
        return t;
    }

    static async Task Foo()
    {
        checked
        {
            Meth(await Meth(int.MaxValue) + 1);
        }
        unchecked
        {
            Meth(long.MinValue - 1);
        }

        var str = """";
        lock (str)
        {
            Meth((int?)null);
        }

        lock (Meth(""""))
        {
            Task.Run(async () => { await Task.Delay(10); return """"; });
        }
    }

    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (16,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             Meth(await Meth(int.MaxValue) + 1);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Meth(await Meth(int.MaxValue) + 1)"),
                // (20,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             Meth(long.MinValue - 1);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Meth(long.MinValue - 1)"),
                // (26,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             Meth((int?)null);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Meth((int?)null)"));
        }

        [WorkItem(611150, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611150")]
        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait12_breaking()
        {
            // The native compiler gives a warning on the un-awaited async invocation. However, awaiting inside the lock
            // would escalate the warning to an error. Roslyn does not give a warning.
            var source = @"
using System.Threading.Tasks;

class Test
{
    static async Task Foo()
    {
        await Task.Factory.StartNew(() => { });
        lock (new object())
        {
            Task.Run(async () => { await Task.Delay(10); return """"; });
        }
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait13()
        {
            // invoke a method that returns an awaitable type in an async method
            //      1. the callee is a property/indexer that returns Task/Task<T>
            //      2. invoke the method in the constructor
            var source = @"
using System.Threading.Tasks;

class Test
{
    public Task Prop
    {
        get { return Task.Run(async () => { await Task.Delay(10); }); }
    }

    public Task<int> this[dynamic index]
    {
        get
        {
            return Task.Run<int>(async () => { await Task.Delay(10); return index; });
        }
    }

    public async Task<T> Meth<T>(T t)
    {
        await Task.Delay(10);
        return t;
    }

    public Test()
    {
        Meth(1); //warning CS4014
    }

    public async Task Foo()
    {
        var test = new Test();
        test.Prop; //error CS0201
        test[1]; //error CS0201
    }

    static int Main()
    {
        return 0;
    }
}";

            CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { CSharpRef, SystemCoreRef }).VerifyDiagnostics(
                // (41,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Meth(1); //warning CS4014
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Meth(1)"),
                // (47,9): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         test.Prop; //error CS0201
                Diagnostic(ErrorCode.ERR_IllegalStatement, "test.Prop"),
                // (48,9): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         test[1]; //error CS0201
                Diagnostic(ErrorCode.ERR_IllegalStatement, "test[1]"),
                // (44,23): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public async Task Foo()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Foo"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait14()
        {
            // invoke a method that returns an awaitable type in async/non-async method
            var source = @"
using System.Threading.Tasks;

class Test
{
    static async Task<T> Meth<T>(T t)
    {
        await Task.Delay(10);
        return t;
    }

    static async Task Foo()
    {
        for (Meth(""""); await Meth(false); Meth((float?)null))
        {
            Meth(1);
        }
    }

    static Task<int> Bar()
    {
        for (Meth(5m); ; Meth(""""))
        {
            Meth((string)null);
        }
    }

    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (27,14): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         for (Meth(""); await Meth(false); Meth((float?)null))
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, @"Meth("""")"),
                // (27,43): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         for (Meth(""); await Meth(false); Meth((float?)null))
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Meth((float?)null)"),
                // (29,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             Meth(1);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Meth(1)"),
                // (35,14): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         for (Meth(5m); ; Meth(""))
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Meth(5m)"),
                // (35,26): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         for (Meth(5m); ; Meth(""))
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, @"Meth("""")"),
                // (37,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             Meth((string)null);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Meth((string)null)"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait15()
        {
            // invoke a method that returns an awaitable type in async/non-async method
            var source = @"
using System.Threading.Tasks;

public class TestB
{
    public async Task<T> Meth<T>(T t = default(T))
    {
        await Task.Delay(10);
        return t;
    }

    public async Task Meth2()
    {
        await Task.Delay(10);
    }
}

public class Test
{
    public async Task Foo()
    {
        var testB = new TestB();
        for (testB.Meth2(); await testB.Meth(false); testB.Meth((float?)null))
        {
            testB.Meth(1);
        }
    }

    public void Bar()
    {
        //if the awaitable method was compiled into a library, the warning will not give
        var testB = new TestB();
        testB.Meth<decimal>();
        testB.Meth2();
    }

    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (33,14): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         for (testB.Meth2(); await testB.Meth(false); testB.Meth((float?)null))
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "testB.Meth2()"),
                // (33,54): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         for (testB.Meth2(); await testB.Meth(false); testB.Meth((float?)null))
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "testB.Meth((float?)null)"),
                // (35,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             testB.Meth(1);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "testB.Meth(1)"),
                // (43,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         testB.Meth<decimal>();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "testB.Meth<decimal>()"),
                // (44,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         testB.Meth2();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "testB.Meth2()"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait16()
        {
            // invoke a method that returns an awaitable type in an non-async method
            var source = @"
using System.Threading.Tasks;
class Test
{
    public void Meth()
    {
        Foo();
    }

    public async Task<int> Foo()
    {
        await Task.Delay(10);
        return 1;
    }
    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (15,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Foo();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Foo()"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait17()
        {
            // invoke a method that returns an awaitable type in an non-async method
            var source = @"
using System;
using System.Threading.Tasks;

static class Extension
{
    public static async Task<int> ExMeth(this string str)
    {
        await Task.Delay(10);
        return str.Length;
    }
}
class Test:IDisposable
{
    public void Dispose() { }
    static int Main()
    {
        using (Test test = new Test())
        {
            string s=""abc"";
             s.ExMeth();
        }
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (29,14): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //              s.ExMeth();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "s.ExMeth()"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait18()
        {
            // invoke a method that returns an awaitable type in an non-async method
            var source = @"
using System.Threading.Tasks;

class Test
{
    public delegate T Del<T>(T item);
    public T Meth<T>(T t)
    {
        Foo();
        Del<int> del = x =>
            {
                Foo();
                return 1;
            };
        return t;
    }


    static async Task<Task> Foo()
    {
        await Task.Delay(10);
        return new Task(() => { Task.Delay(10); });
    }
    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (18,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Foo();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Foo()"),
                // (21,17): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //                 Foo();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Foo()"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait19()
        {
            // invoke a method that returns an awaitable type in an non-async method
            var source = @"
using System.Threading.Tasks;

class Test
{
    public delegate T Del<T>();
    public T Meth<T>(T t)
    {
        Foo();
        Del<Task<string>> del =async delegate()
        {
            await Task.Delay(10);
            Foo();
            Del<int> del2 = () =>
                {
                    Foo();
                    return 1;
                };
            return """";
        };
        del();
        return t;
    }


    public Task Foo()
    {
        return Task.Run(async() =>
            {
                await Task.Delay(10);
            });
    }
    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (21,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             Foo();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Foo()"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait20()
        {
            // invoke a method that returns an awaitable type in an non-async method
            var source = @"
using System;
using System.Threading.Tasks;

class Test { }
class Testcase
{
    static Test test = null;
    public void Meth()
    {
        Foo();
        Func<Task<Test>> fun = () =>
            {
                return Foo();
            };
    }

    public Task<Test> Foo()
    {
        return Task.Run<Test>( () =>
            {
                if (test == null)
                    test = new Test();
                return test;
            }
            );
    }
    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait21()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    delegate Task<dynamic> Del(dynamic d = null);
    public void Meth()
    {
        Del del = async x =>
            {
                await Task.Delay(10);
                return 1;
            };
        del();
        Func<int,Task<Func<dynamic>>> func =async y =>
            {
                await Task.Delay(10);
                del(y);
                return (delegate() { return 1; });
            };
        func(1);
    }

    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { CSharpRef, SystemCoreRef }).VerifyDiagnostics(
                // (27,17): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //                 del(y);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "del(y)"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait22()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public void Meth()
    {
        Func<Task<string>> Foo = async () =>
            {
                await Task.Delay(10);
                return """";
            };
        Foo();
        Task.Run(() =>
            {
                Foo();
                Func<Task<Task>> fun = async () =>
                    {
                        await Task.Delay(10);
                        return Task.Run(() =>
                            {
                                Foo();
                            });
                    };
            });      
    }

    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait23()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    delegate Task<string> Del();
    delegate void Del2();
    public void Meth()
    {
        Del del = async delegate()
        {
            await Task.Delay(10);
            return """";
        };
        del();
        Del2 del2 = delegate()
        {
            del();
            Del del3 = async () =>
                {
                    await Task.Delay(10);
                    Del2 del4 = delegate()
                    {
                        del();
                    };
                    return """";
                };
            del3();
            
        };
    }

    static int Main()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait24()
        {
            var source = @"
using System;
using System.Threading.Tasks;

static class Extension
{
    public static Task<int> ExMeth(this int i)
    {
        Func<Task<int>> Foo = async () =>
            {
                await Task.Delay(10);
                return ++i;
            };
        return (Task<int>) Foo();
    }
}
class Test 
{
    public static int amount=0;
    static int Main()
    {
        lock (amount.ExMeth())
        {
            amount.ExMeth();
        }
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait25()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    public async Task<int> Meth()
    {
        await Task.Delay(10);
        return int.MaxValue;
    }
    public Task<int> Foo()
    {
        int i = int.MaxValue;
        return Task.Run(async () => { return i; });
    }
    static int Main()
    {
        Test test = new Test();
        checked
        {
            test.Meth();
            test.Foo();
        }
        unchecked
        {
            test.Meth();
            test.Foo();
        }
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (24,25): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         return Task.Run(async () => { return i; });
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "async () => { return i; }"),
                // (31,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             test.Meth();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "test.Meth()"),
                // (36,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             test.Meth();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "test.Meth()"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait26()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Testcase
{
    ~Testcase()
    {
        Foo();
        Foo2();
    }
    public async Task<string> Foo()
    {
        await Task.Delay(10);
        return ""Foo"";
    }
    public Task Foo2()
    {
        return Task.Run(() => { });
    }
}
class Test 
{
    static int Main()
    {
        Object obj = new Testcase();
        obj = null;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        return 0;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (17,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Foo();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Foo()"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_ForgetAwait27()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test : IDisposable
{
    public async Task Foo()
    {
        await Task.Delay(10);
    }
    public void Dispose() 
    {
        Foo();
    }
    static int Main()
    {
        using (Test test = new Test())
        {
            test.Foo();
        }
        return 0;
    }
}
";

            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (22,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Foo();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Foo()"),
                // (28,13): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //             test.Foo();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "test.Foo()"));
        }

        [Fact]
        public void UnobservedAwaitableExpression_Script()
        {
            var source =
@"using System.Threading.Tasks;
Task.FromResult(1);
Task.FromResult(2);";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script);
            compilation.VerifyDiagnostics(
                // (2,1): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                // Task.FromResult(1);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Task.FromResult(1)").WithLocation(2, 1),
                // (3,1): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                // Task.FromResult(2);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Task.FromResult(2)").WithLocation(3, 1));
        }

        [Fact]
        public void UnobservedAwaitableExpression_Submission()
        {
            var source0 =
@"using System.Threading.Tasks;
Task.FromResult(1);
Task.FromResult(2)";
            var submission = CSharpCompilation.CreateScriptCompilation(
                "s0.dll",
                syntaxTree: SyntaxFactory.ParseSyntaxTree(source0, options: TestOptions.Script),
                references: new[] { MscorlibRef_v4_0_30316_17626 });
            submission.VerifyDiagnostics(
                // (2,1): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                // Task.FromResult(1);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Task.FromResult(1)").WithLocation(2, 1),
                // (3,1): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                // Task.FromResult(2);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "Task.FromResult(2)").WithLocation(3, 1));
        }

        [Fact]
        public void BadAsyncMethodWithNoStatementBody()
        {
            var source = @"
using System.Threading.Tasks;

static class Test
{
    static async Task M1();
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (6,23): error CS1994: The 'async' modifier can only be used in methods that have a body
                //     static async Task M1();
                Diagnostic(ErrorCode.ERR_BadAsyncLacksBody, "M1"));
        }

        [Fact]
        public void BadAsyncMethodWithVarargsAndNoStatementBody()
        {
            var source = @"
using System.Threading.Tasks;

static class Test
{
    static async Task M1(__arglist);
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (6,23): error CS1994: The 'async' modifier can only be used in methods that have a body
                //     static async Task M1(__arglist);
                Diagnostic(ErrorCode.ERR_BadAsyncLacksBody, "M1"));
        }

        [Fact]
        public void BadSpecialByRefParameter()
        {
            var source = @"
using System.Threading.Tasks;
using System;

class Test
{
    async Task M1(TypedReference tr)
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (7,34): error CS4012: Parameters or locals of type 'System.TypedReference' cannot be declared in async methods or lambda expressions
                //     async Task M1(TypedReference tr)
                Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "tr").WithArguments("System.TypedReference"));
        }

        [Fact]
        public void BadSpecialByRefLocal()
        {
            var source = @"
using System.Threading.Tasks;
using System;

class Test
{
    async Task M1()
    {
        TypedReference tr;
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (9,9): error CS4012: Parameters or locals of type 'System.TypedReference' cannot be declared in async methods or lambda expressions
                //         TypedReference tr;
                Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "TypedReference").WithArguments("System.TypedReference"),
                // (9,24): warning CS0168: The variable 'tr' is declared but never used
                //         TypedReference tr;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "tr").WithArguments("tr"));
        }

        [Fact]
        public void BadSpecialByRefVarDeclLocal()
        {
            var source = @"
using System.Threading.Tasks;
using System;

class Test
{
    async Task M1(bool truth)
    {
        var tr = new TypedReference();
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (9,9): error CS4012: Parameters or locals of type 'System.TypedReference' cannot be declared in async methods or lambda expressions
                //         var tr = new TypedReference();
                Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "var").WithArguments("System.TypedReference"),
                // (9,13): warning CS0219: The variable 'tr' is assigned but its value is never used
                //         var tr = new TypedReference();
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "tr").WithArguments("tr"));
        }

        [Fact]
        public void BadFixedSpecialByRefLocal()
        {
            var source = @"
using System;

public class MyClass
{
    unsafe async public static void F()
    {
        fixed (TypedReference tr) { }
    }
}";
            CreateCompilationWithMscorlib45(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,31): error CS0209: The type of a local declared in a fixed statement must be a pointer type
                //         fixed (TypedReference tr) { }
                Diagnostic(ErrorCode.ERR_BadFixedInitType, "tr"),
                // (8,16): error CS4012: Parameters or locals of type 'System.TypedReference' cannot be declared in async methods or lambda expressions.
                //         fixed (TypedReference tr) { }
                Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "TypedReference").WithArguments("System.TypedReference"),
                // (8,31): error CS0210: You must provide an initializer in a fixed or using statement declaration
                //         fixed (TypedReference tr) { }
                Diagnostic(ErrorCode.ERR_FixedMustInit, "tr"),
                // (6,37): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     unsafe async public static void F()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "F"));
        }

        [Fact]
        public void AsyncInSecurityCriticalClass()
        {
            var source = @"
using System.Security;
using System.Threading.Tasks;

[SecurityCritical]
public class C
{
    public async void M()
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (8,23): error CS4031: Async methods are not allowed in an Interface, Class, or Structure which has the 'SecurityCritical' or 'SecuritySafeCritical' attribute.
                //     public async void M()
                Diagnostic(ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsyncInClassOrStruct, "M"));
        }

        [Fact]
        public void AsyncInSecuritySafeCriticalClass()
        {
            var source = @"
using System.Security;
using System.Threading.Tasks;

[SecuritySafeCritical]
public class C
{
    public async void M()
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (8,23): error CS4031: Async methods are not allowed in an Interface, Class, or Structure which has the 'SecurityCritical' or 'SecuritySafeCritical' attribute.
                //     public async void M()
                Diagnostic(ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsyncInClassOrStruct, "M"));
        }

        [Fact]
        public void AsyncInSecuritySafeCriticalAndSecurityCriticalClass()
        {
            var source = @"
using System.Security;
using System.Threading.Tasks;

[SecurityCritical]
[SecuritySafeCritical]
public class C
{
    public async void M()
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (9,23): error CS4031: Async methods are not allowed in an Interface, Class, or Structure which has the 'SecurityCritical' or 'SecuritySafeCritical' attribute.
                //     public async void M()
                Diagnostic(ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsyncInClassOrStruct, "M"));
        }

        [Fact]
        public void AsyncInDoublyNestedSecuritySafeCriticalAndSecurityCriticalClasses()
        {
            var source = @"
using System.Security;
using System.Threading.Tasks;

[SecurityCritical]
[SecuritySafeCritical]
public class C
{
    [SecuritySafeCritical]
    public class D
    {
        public async void M()
        {
            await Task.Factory.StartNew(() => { });
        }
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (12,27): error CS4031: Async methods are not allowed in an Interface, Class, or Structure which has the 'SecurityCritical' or 'SecuritySafeCritical' attribute.
                //         public async void M()
                Diagnostic(ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsyncInClassOrStruct, "M"));
        }

        [Fact]
        public void SecurityCriticalOnAsync()
        {
            var source = @"
using System.Security;
using System.Threading.Tasks;

public class D
{
    [SecurityCritical]
    public async void M()
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (7,6): error CS4030: Security attribute 'SecurityCritical' cannot be applied to an Async method.
                //     [SecurityCritical]
                Diagnostic(ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsync, "SecurityCritical").WithArguments("SecurityCritical"));
        }

        [Fact]
        public void SecuritySafeCriticalOnAsync()
        {
            var source = @"
using System.Security;
using System.Threading.Tasks;

public class D
{
    [SecuritySafeCritical]
    public async void M()
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (7,6): error CS4030: Security attribute 'SecuritySafeCritical' cannot be applied to an Async method.
                //     [SecuritySafeCritical]
                Diagnostic(ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsync, "SecuritySafeCritical").WithArguments("SecuritySafeCritical"));
        }


        [Fact]
        public void SecuritySafeCriticalAndSecurityCriticalOnAsync()
        {
            var source = @"
using System.Security;
using System.Threading.Tasks;

public class D
{
    [SecurityCritical]
    [SecuritySafeCritical]
    public async void M()
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (7,6): error CS4030: Security attribute 'SecurityCritical' cannot be applied to an Async method.
                //     [SecurityCritical]
                Diagnostic(ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsync, "SecurityCritical").WithArguments("SecurityCritical"),
                // (8,6): error CS4030: Security attribute 'SecuritySafeCritical' cannot be applied to an Async method.
                //     [SecuritySafeCritical]
                Diagnostic(ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsync, "SecuritySafeCritical").WithArguments("SecuritySafeCritical"));
        }

        [Fact, WorkItem(547077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547077")]
        public void Repro_17880()
        {
            var source = @"
class Program
{
    async void Meth()
    {
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (4,16): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async void Meth()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Meth"));
        }

        [Fact, WorkItem(547079, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547079")]
        public void Repro_17883()
        {
            var source = @"
abstract class Base
{
    public async abstract void M1();
}

class Test
{
    public static int Main()
    {
        return 1;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (4,32): error CS1994: The 'async' modifier can only be used in methods that have a body.
                //     public async abstract void M1();
                Diagnostic(ErrorCode.ERR_BadAsyncLacksBody, "M1"));
        }

        [Fact, WorkItem(547081, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547081")]
        public void Repro_17885()
        {
            var source = @"
class Test
{
    async public static void Main()
    {
    }
}";
            CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (4,30): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async public static void Main()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Main"),
                // (4,30): error CS4009: 'Test.Main()': an entry point cannot be marked with the 'async' modifier
                //     async public static void Main()
                Diagnostic(ErrorCode.ERR_MainCantBeAsync, "Main").WithArguments("Test.Main()"));
        }

        [Fact, WorkItem(547088, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547088")]
        public void Repro_17914()
        {
            var source = @"
class Driver
{
    static int Main()
    {
        return 1;
    }
 
    public async void Foo(ref int x)
    { }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (9,35): error CS1988: Async methods cannot have ref or out parameters
                //     public async void Foo(ref int x)
                Diagnostic(ErrorCode.ERR_BadAsyncArgType, "x"),
                // (9,23): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public async void Foo(ref int x)
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Foo"));
        }


        [Fact]
        public void BadAsync_MethodImpl_Synchronized()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class C
{
    [MethodImpl(MethodImplOptions.Synchronized)]
    async Task<int> F1()
    {
        return await Task.Factory.StartNew(() => 1);
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (9,21): error CS4015: 'MethodImplOptions.Synchronized' cannot be applied to an Async method.
                //     async Task<int> F1()
                Diagnostic(ErrorCode.ERR_SynchronizedAsyncMethod, "F1"));
        }

        [Fact]
        public void Async_MethodImplSynchronized_BadReturn_SecurityCritical()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading.Tasks;

[SecurityCritical]
public class C
{
    [MethodImpl(MethodImplOptions.Synchronized)]
    async int F1()
    {
        return await Task.Factory.StartNew(() => 1);
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,15): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async int F1()
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "F1"),
                // (10,15): error CS4031: Async methods are not allowed in an Interface, Class, or Structure which has the 'SecurityCritical' or 'SecuritySafeCritical' attribute.
                //     async int F1()
                Diagnostic(ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsyncInClassOrStruct, "F1"),
                // (10,15): error CS4015: 'MethodImplOptions.Synchronized' cannot be applied to an Async method.
                //     async int F1()
                Diagnostic(ErrorCode.ERR_SynchronizedAsyncMethod, "F1"));
        }

        [Fact]
        [WorkItem(552382, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552382")]
        public void GetAwaiterIsExtension()
        {
            var source =
@"using System;
using A;
 
namespace A
{
    public class IAS<T>
    {
    }
    public class Awaiter<T> : System.Runtime.CompilerServices.INotifyCompletion
    {
        public bool IsCompleted { get { return true; } }
        public T GetResult() { return default(T); }
        public void OnCompleted(Action continuation) { }
    }
    public static class IAS_Extensions
    {
        public static Awaiter<T> GetAwaiter<T>(this IAS<T> self) { return null; }
    }
}

namespace B
{
    public class Program
    {
        static async void M(IAS<int> i)
        {
            int i1 = await i;
            int i2 = i.GetAwaiter().GetResult();
        }
        public static void Main(string[] args)
        {
        }
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(576311, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576311")]
        public void BadDelegateTypeForAsync()
        {
            var source =
@"using System;
class C
{
    static void Main()
    {
        Func<int> x = async delegate { throw null; };
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (6,23): error CS4010: Cannot convert async anonymous method to delegate type 'System.Func<int>'. An async anonymous method may return void, Task or Task<T>, none of which are convertible to 'System.Func<int>'.
                //         Func<int> x = async delegate { throw null; };
                Diagnostic(ErrorCode.ERR_CantConvAsyncAnonFuncReturns, "async delegate { throw null; }").WithArguments("anonymous method", "System.Func<int>").WithLocation(6, 23),
                // (6,23): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Func<int> x = async delegate { throw null; };
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "async delegate { throw null; }").WithLocation(6, 23)
           );
        }

        [WorkItem(588706, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/588706")]
        [Fact]
        public void AsyncAsLambdaParameter()
        {
            var source =
@"using System;
using System.Threading.Tasks;

class C
{
    async void Foo()
    {
        Action<int> x = (await) => { }; // should be a syntax error
        await Task.Delay(1);
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (8,26): error CS4003: 'await' cannot be used as an identifier within an async method or lambda expression
                //         Action<int> x = (await) => { }; // should be a syntax error
                Diagnostic(ErrorCode.ERR_BadAwaitAsIdentifier, "await").WithLocation(8, 26)
                );
        }

        [Fact]
        [WorkItem(629368, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629368")]
        public void GetAwaiterFieldUsedLikeMethod()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class C
{
    public Func<TaskAwaiter<int>> GetAwaiter;

    public async Task<int> F(C x)
    {
        return await x;
    }
}
";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (12,17): error CS0118: 'GetAwaiter' is a field but is used like a method
                Diagnostic(ErrorCode.ERR_BadSKknown, "await x").WithArguments("GetAwaiter", "field", "method"));
        }

        [Fact]
        [WorkItem(629368, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629368")]
        public void GetAwaiterPropertyUsedLikeMethod()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class C
{
    public Func<TaskAwaiter<int>> GetAwaiter { get; set; }

    public async Task<int> F(C x)
    {
        return await x;
    }
}
";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (12,16): error CS0118: 'GetAwaiter' is a property but is used like a method
                Diagnostic(ErrorCode.ERR_BadSKknown, "await x").WithArguments("GetAwaiter", "property", "method"));
        }

        [Fact]
        [WorkItem(628619, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/628619")]
        public void ReturnExpressionNotConvertible()
        {
            string source = @"
using System.Threading.Tasks;
 
class Program
{
    static async Task<T> Foo<T>()
    {
        await Task.Delay(1);
        return 1;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (9,16): error CS0029: Cannot implicitly convert type 'int' to 'T'
                //         return 1;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "T")
                );
        }

        [Fact]
        [WorkItem(632824, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632824")]
        public void RefParameterOnAsyncLambda()
        {
            string source = @"
using System;
using System.Threading.Tasks;
 
delegate Task D(ref int x);
 
class C
{
    static void Main()
    {
        D d = async delegate(ref int i)
        {
            await Task.Delay(500);
            Console.WriteLine(i++);
        };
 
        int x = 5;
        d(ref x).Wait();
        Console.WriteLine(x);
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (11,38): error CS1988: Async methods cannot have ref or out parameters
                //         D d = async delegate(ref int i)
                Diagnostic(ErrorCode.ERR_BadAsyncArgType, "i")
                );
        }

        [Fact]
        [WorkItem(858059, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858059")]
        public void UnawaitedVersusLambda()
        {
            string source =
@"using System;
using System.Threading.Tasks;

public class Program
{
    public static void Main(string[] args)
    {
    }

    void X()
    {
        Action x1 = () => XAsync(); // warn
        Action y1 = () => YAsync(); // ok
        Action x2 = async () => XAsync(); // warn
        Action y2 = async () => YAsync(); // warn

        XAsync(); // warn
        YAsync(); // ok
    }
    async Task XAsync()
    {
        await Task.Delay(1);
    }
    Task YAsync()
    {
        return Task.Delay(1);
    }
}";
            // The rules for when we give a warning may seem quirky, but we aim to precisely replicate
            // the diagnostics produced by the native compiler.
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (12,27): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Action x1 = () => XAsync(); // warn
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "XAsync()").WithLocation(12, 27),
                // (14,33): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Action x2 = async () => XAsync(); // warn
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "XAsync()").WithLocation(14, 33),
                // (15,33): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         Action y2 = async () => YAsync(); // warn
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "YAsync()").WithLocation(15, 33),
                // (17,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         XAsync(); // warn
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "XAsync()").WithLocation(17, 9),
                // (14,21): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Action x2 = async () => XAsync(); // warn
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "async () => XAsync()").WithLocation(14, 21),
                // (15,21): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Action y2 = async () => YAsync(); // warn
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "async () => YAsync()").WithLocation(15, 21)
                );
        }
    }
}
