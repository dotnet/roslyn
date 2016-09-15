﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class BindingAsyncTasklikeMoreTests : CompilingTestBase
    {
        [Fact]
        public void AsyncMethod()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{
    static async MyTask F() { await Task.Delay(0); }
    static async MyTask<T> G<T>(T t) { await Task.Delay(0); return t; }
    static async MyTask<int> M()
    {
        await F();
        return await G(3);
    }
    static void Main()
    {
        var i = M().Result;
        Console.WriteLine(i);
    }
}
[AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
struct MyTask
{
    internal Awaiter GetAwaiter() => new Awaiter();
    internal class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action a) { }
        internal bool IsCompleted => true;
        internal void GetResult() { }
    }
}
[AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
struct MyTask<T>
{
    internal T _result;
    public T Result => _result;
    internal Awaiter GetAwaiter() => new Awaiter(this);
    internal class Awaiter : INotifyCompletion
    {
        private readonly MyTask<T> _task;
        internal Awaiter(MyTask<T> task) { _task = task; }
        public void OnCompleted(Action a) { }
        internal bool IsCompleted => true;
        internal T GetResult() => _task.Result;
    }
}
struct MyTaskMethodBuilder
{
    private MyTask _task;
    public static MyTaskMethodBuilder Create() => new MyTaskMethodBuilder(new MyTask());
    internal MyTaskMethodBuilder(MyTask task)
    {
        _task = task;
    }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
    {
        stateMachine.MoveNext();
    }
    public void SetException(Exception e) { }
    public void SetResult() { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask Task => _task;
}
struct MyTaskMethodBuilder<T>
{
    private MyTask<T> _task;
    public static MyTaskMethodBuilder<T> Create() => new MyTaskMethodBuilder<T>(new MyTask<T>());
    internal MyTaskMethodBuilder(MyTask<T> task)
    {
        _task = task;
    }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
    {
        stateMachine.MoveNext();
    }
    public void SetException(Exception e) { }
    public void SetResult(T t) { _task._result = t; }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask<T> Task => _task;
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(compilation, expectedOutput: "3");
            verifier.VerifyDiagnostics();
            var testData = verifier.TestData;
            var method = (MethodSymbol)testData.GetMethodData("C.F()").Method;
            Assert.True(method.IsAsync);
            Assert.True(method.IsTaskReturningAsync(compilation));
            method = (MethodSymbol)testData.GetMethodData("C.G<T>(T)").Method;
            Assert.True(method.IsAsync);
            Assert.True(method.IsGenericTaskReturningAsync(compilation));
            verifier.VerifyIL("C.F()",
@"{
  // Code size       52 (0x34)
  .maxstack  2
  .locals init (C.<F>d__0 V_0,
                MyTaskMethodBuilder V_1)
  IL_0000:  newobj     ""C.<F>d__0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       ""MyTaskMethodBuilder MyTaskMethodBuilder.Create()""
  IL_000c:  stfld      ""MyTaskMethodBuilder C.<F>d__0.<>t__builder""
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4.m1
  IL_0013:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0018:  ldloc.0
  IL_0019:  ldfld      ""MyTaskMethodBuilder C.<F>d__0.<>t__builder""
  IL_001e:  stloc.1
  IL_001f:  ldloca.s   V_1
  IL_0021:  ldloca.s   V_0
  IL_0023:  call       ""void MyTaskMethodBuilder.Start<C.<F>d__0>(ref C.<F>d__0)""
  IL_0028:  ldloc.0
  IL_0029:  ldflda     ""MyTaskMethodBuilder C.<F>d__0.<>t__builder""
  IL_002e:  call       ""MyTask MyTaskMethodBuilder.Task.get""
  IL_0033:  ret
}");
            verifier.VerifyIL("C.G<T>(T)",
@"{
  // Code size       59 (0x3b)
  .maxstack  2
  .locals init (C.<G>d__1<T> V_0,
                MyTaskMethodBuilder<T> V_1)
  IL_0000:  newobj     ""C.<G>d__1<T>..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.0
  IL_0008:  stfld      ""T C.<G>d__1<T>.t""
  IL_000d:  ldloc.0
  IL_000e:  call       ""MyTaskMethodBuilder<T> MyTaskMethodBuilder<T>.Create()""
  IL_0013:  stfld      ""MyTaskMethodBuilder<T> C.<G>d__1<T>.<>t__builder""
  IL_0018:  ldloc.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int C.<G>d__1<T>.<>1__state""
  IL_001f:  ldloc.0
  IL_0020:  ldfld      ""MyTaskMethodBuilder<T> C.<G>d__1<T>.<>t__builder""
  IL_0025:  stloc.1
  IL_0026:  ldloca.s   V_1
  IL_0028:  ldloca.s   V_0
  IL_002a:  call       ""void MyTaskMethodBuilder<T>.Start<C.<G>d__1<T>>(ref C.<G>d__1<T>)""
  IL_002f:  ldloc.0
  IL_0030:  ldflda     ""MyTaskMethodBuilder<T> C.<G>d__1<T>.<>t__builder""
  IL_0035:  call       ""MyTask<T> MyTaskMethodBuilder<T>.Task.get""
  IL_003a:  ret
}");
        }

        [Fact]
        public void AsyncMethodBuilder_MissingMethods()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{
    static async MyTask F() { await (Task)null; }
    static async MyTask<T> G<T>(T t) { await (Task)null; return t; }
}
[AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
struct MyTask { }
[AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
struct MyTask<T> { }
struct MyTaskMethodBuilder
{
    public static MyTaskMethodBuilder Create() => new MyTaskMethodBuilder();
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
    public void SetException(Exception e) { }
    public void SetResult() { }
}
struct MyTaskMethodBuilder<T>
{
    public static MyTaskMethodBuilder<T> Create() => new MyTaskMethodBuilder<T>();
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask<T> Task { get { return default(MyTask<T>); } }
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            var compilation = CreateCompilationWithMscorlib45(source);
            compilation.VerifyEmitDiagnostics(
                // (6,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.Task'
                //     static async MyTask F() { await (Task)null; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await (Task)null; }").WithArguments("MyTaskMethodBuilder", "Task").WithLocation(6, 29),
                // (7,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.SetException'
                //     static async MyTask<T> G<T>(T t) { await (Task)null; return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await (Task)null; return t; }").WithArguments("MyTaskMethodBuilder<T>", "SetException").WithLocation(7, 38));
        }

        [Fact]
        public void Private()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
class C
{
#pragma warning disable CS1998
    static async MyTask F() { }
    static async MyTask<int> G() { return 3; }
#pragma warning restore CS1998
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
    private class MyTask { }
    [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
    private class MyTask<T> { }
    private class MyTaskMethodBuilder
    {
        public static MyTaskMethodBuilder Create() => null;
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
        public void SetException(Exception e) { }
        public void SetResult() { }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
        public MyTask Task { get { return null; } }
    }
    private class MyTaskMethodBuilder<T>
    {
        public static MyTaskMethodBuilder<T> Create() => null;
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
        public void SetException(Exception e) { }
        public void SetResult(T t) { }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
        public MyTask<T> Task { get { return null; } }
    }
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            var compilation = CreateCompilationWithMscorlib45(source);
            var verifier = CompileAndVerify(compilation);
            verifier.VerifyDiagnostics();
            var testData = verifier.TestData;
            var method = (MethodSymbol)testData.GetMethodData("C.F()").Method;
            Assert.True(method.IsAsync);
            Assert.True(method.IsTaskReturningAsync(compilation));
            Assert.Equal("C.MyTask", method.ReturnType.ToDisplayString());
            method = (MethodSymbol)testData.GetMethodData("C.G()").Method;
            Assert.True(method.IsAsync);
            Assert.True(method.IsGenericTaskReturningAsync(compilation));
            Assert.Equal("C.MyTask<int>", method.ReturnType.ToDisplayString());
        }

        [Fact]
        public void AsyncLambda_InferReturnType()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
class C
{
    static void F(Func<MyTask> f) { }
    static void F<T>(Func<MyTask<T>> f) { }
    static void F(Func<MyTask<string>> f) { }
    static void M()
    {
#pragma warning disable CS1998
        F(async () => { });
        F(async () => { return 3; });
        F(async () => { return string.Empty; });
#pragma warning restore CS1998
    }
}
[AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
class MyTask
{
    internal Awaiter GetAwaiter() => null;
    internal class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action a) { }
        internal bool IsCompleted => true;
        internal void GetResult() { }
    }
}
[AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
class MyTask<T>
{
    internal Awaiter GetAwaiter() => null;
    internal class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action a) { }
        internal bool IsCompleted => true;
        internal T GetResult() => default(T);
    }
}
class MyTaskMethodBuilder
{
    public static MyTaskMethodBuilder Create() => null;
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
    public void SetException(Exception e) { }
    public void SetResult() { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask Task => default(MyTask);
}
class MyTaskMethodBuilder<T>
{
    public static MyTaskMethodBuilder<T> Create() => null;
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
    public void SetException(Exception e) { }
    public void SetResult(T t) { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask<T> Task => default(MyTask<T>);
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            var compilation = CreateCompilationWithMscorlib45(source);
            var verifier = CompileAndVerify(compilation);
            verifier.VerifyDiagnostics();
            var testData = verifier.TestData;
            var method = (MethodSymbol)testData.GetMethodData("C.<>c.<M>b__3_0()").Method;
            Assert.True(method.IsAsync);
            Assert.True(method.IsTaskReturningAsync(compilation));
            Assert.Equal("MyTask", method.ReturnType.ToDisplayString());
            method = (MethodSymbol)testData.GetMethodData("C.<>c.<M>b__3_1()").Method;
            Assert.True(method.IsAsync);
            Assert.True(method.IsGenericTaskReturningAsync(compilation));
            Assert.Equal("MyTask<int>", method.ReturnType.ToDisplayString());
        }

        [Fact]
        public void AsyncLocalFunction()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
class C
{
    static async void M()
    {
#pragma warning disable CS1998
        async MyTask F() { }
        async MyTask<T> G<T>(T t) => t;
        await F();
        await G(3);
#pragma warning restore CS1998
    }
}
[AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
struct MyTask
{
    internal Awaiter GetAwaiter() => null;
    internal class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action a) { }
        internal bool IsCompleted => true;
        internal void GetResult() { }
    }
}
[AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
struct MyTask<T>
{
    internal Awaiter GetAwaiter() => null;
    internal class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action a) { }
        internal bool IsCompleted => true;
        internal T GetResult() => default(T);
    }
}
class MyTaskMethodBuilder
{
    public static MyTaskMethodBuilder Create() => null;
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
    public void SetException(Exception e) { }
    public void SetResult() { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask Task => default(MyTask);
}
class MyTaskMethodBuilder<T>
{
    public static MyTaskMethodBuilder<T> Create() => null;
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
    public void SetException(Exception e) { }
    public void SetResult(T t) { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask<T> Task => default(MyTask<T>);
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            var compilation = CreateCompilationWithMscorlib45(source);
            var verifier = CompileAndVerify(compilation);
            verifier.VerifyDiagnostics();
            var testData = verifier.TestData;
            var method = (MethodSymbol)testData.GetMethodData("C.<M>g__F0_0()").Method;
            Assert.True(method.IsAsync);
            Assert.True(method.IsTaskReturningAsync(compilation));
            Assert.Equal("MyTask", method.ReturnType.ToDisplayString());
            method = (MethodSymbol)testData.GetMethodData("C.<M>g__G0_1<T>(T)").Method;
            Assert.True(method.IsAsync);
            Assert.True(method.IsGenericTaskReturningAsync(compilation));
            Assert.Equal("MyTask<T>", method.ReturnType.ToDisplayString());
        }

        [Fact]
        public void Dynamic()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
class C
{
    static void F<T>(Func<MyTask<T>> f) { }
    static void G(Func<MyTask<dynamic>> f) { }
    static void M(object o)
    {
#pragma warning disable CS1998
        F(async () => (dynamic)o);
        F(async () => new[] { (dynamic)o });
        G(async () => o);
#pragma warning restore CS1998
    }
}
[AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
class MyTask<T>
{
    internal Awaiter GetAwaiter() => null;
    internal class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action a) { }
        internal bool IsCompleted => true;
        internal T GetResult() => default(T);
    }
}
class MyTaskMethodBuilder<T>
{
    public static MyTaskMethodBuilder<T> Create() => null;
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
    public void SetException(Exception e) { }
    public void SetResult(T t) { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask<T> Task => default(MyTask<T>);
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            var compilation = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { CSharpRef, SystemCoreRef });
            var verifier = CompileAndVerify(
                compilation,
                expectedSignatures: new[]
                {
                    Signature(
                        "C+<>c__DisplayClass2_0",
                        "<M>b__0",
                        ".method [System.Runtime.CompilerServices.AsyncStateMachineAttribute(C+<>c__DisplayClass2_0+<<M>b__0>d)] assembly hidebysig instance [System.Runtime.CompilerServices.DynamicAttribute(System.Collections.ObjectModel.ReadOnlyCollection`1[System.Reflection.CustomAttributeTypedArgument])] MyTask`1[System.Object] <M>b__0() cil managed"),
                    Signature(
                        "C+<>c__DisplayClass2_0",
                        "<M>b__1",
                        ".method [System.Runtime.CompilerServices.AsyncStateMachineAttribute(C+<>c__DisplayClass2_0+<<M>b__1>d)] assembly hidebysig instance [System.Runtime.CompilerServices.DynamicAttribute(System.Collections.ObjectModel.ReadOnlyCollection`1[System.Reflection.CustomAttributeTypedArgument])] MyTask`1[System.Object[]] <M>b__1() cil managed"),
                    Signature(
                        "C+<>c__DisplayClass2_0",
                        "<M>b__2",
                        ".method [System.Runtime.CompilerServices.AsyncStateMachineAttribute(C+<>c__DisplayClass2_0+<<M>b__2>d)] assembly hidebysig instance [System.Runtime.CompilerServices.DynamicAttribute(System.Collections.ObjectModel.ReadOnlyCollection`1[System.Reflection.CustomAttributeTypedArgument])] MyTask`1[System.Object] <M>b__2() cil managed"),
                });
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void NonTaskBuilder()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
class C
{
    static async void M()
    {
#pragma warning disable CS1998
        async MyTask F() { };
        await F();
#pragma warning restore CS1998
    }
}
[AsyncMethodBuilder(typeof(string))]
public struct MyTask
{
    internal Awaiter GetAwaiter() => null;
    internal class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action a) { }
        internal bool IsCompleted => true;
        internal void GetResult() { }
    }
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            var compilation = CreateCompilationWithMscorlib45(source);
            compilation.VerifyEmitDiagnostics(
                // (8,26): error CS0656: Missing compiler required member 'string.Task'
                //         async MyTask F() { };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ }").WithArguments("string", "Task").WithLocation(8, 26),
                // (8,26): error CS0656: Missing compiler required member 'string.Create'
                //         async MyTask F() { };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ }").WithArguments("string", "Create").WithLocation(8, 26));
        }

        [Fact]
        public void NonTaskBuilderOfT()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
class C
{
    static async void M()
    {
#pragma warning disable CS1998
        async MyTask<T> F<T>(T t) => t;
        await F(3);
#pragma warning restore CS1998
    }
}
[AsyncMethodBuilder(typeof(IEquatable<>))]
public struct MyTask<T>
{
    internal Awaiter GetAwaiter() => null;
    internal class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action a) { }
        internal bool IsCompleted => true;
        internal T GetResult() => default(T);
    }
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            var compilation = CreateCompilationWithMscorlib45(source);
            compilation.VerifyEmitDiagnostics(
                // (8,35): error CS0656: Missing compiler required member 'IEquatable<T>.Task'
                //         async MyTask<T> F<T>(T t) => t;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> t").WithArguments("System.IEquatable<T>", "Task").WithLocation(8, 35),
                // (8,35): error CS0656: Missing compiler required member 'IEquatable<T>.Create'
                //         async MyTask<T> F<T>(T t) => t;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> t").WithArguments("System.IEquatable<T>", "Create").WithLocation(8, 35));
        }

        [Fact]
        public void NonTaskBuilder_Array()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
class C
{
    static async void M()
    {
#pragma warning disable CS1998
        async MyTask F() { };
        await F();
#pragma warning restore CS1998
    }
}
[AsyncMethodBuilder(typeof(object[]))]
struct MyTask
{
    internal Awaiter GetAwaiter() => null;
    internal class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action a) { }
        internal bool IsCompleted => true;
        internal void GetResult() { }
    }
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            var compilation = CreateCompilationWithMscorlib45(source);
            compilation.VerifyEmitDiagnostics(
                // (8,22): error CS1983: The return type of an async method must be void, Task or Task<T>
                //         async MyTask F() { };
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "{ }").WithLocation(8, 26));
        }

        [Fact]
        static void AsyncMethodBuilderAttributeMultipleParameters()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[AsyncMethodBuilder(typeof(B1),1)] class T1 { }
class B1 { }

[AsyncMethodBuilder(typeof(B2))] class T2 { }
class B2 { }

class Program {
    static void Main() { }
    async T1 f1() => await Task.Delay(1);
    async T2 f2() => await Task.Delay(1);
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t, int i) { } } }
";
            var compilation = CreateCompilationWithMscorlib45(source);
            compilation.VerifyEmitDiagnostics(
                // (8,2): error CS7036: There is no argument given that corresponds to the required formal parameter 'i' of 'AsyncMethodBuilderAttribute.AsyncMethodBuilderAttribute(Type, int)'
                // [AsyncMethodBuilder(typeof(B2))] class T2 { }
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "AsyncMethodBuilder(typeof(B2))").WithArguments("i", "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute.AsyncMethodBuilderAttribute(System.Type, int)").WithLocation(8, 2),
                // (13,14): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T1 f1() => await Task.Delay(1);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "f1").WithLocation(13, 14),
                // (14,14): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T2 f2() => await Task.Delay(1);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "f2").WithLocation(14, 14)
                );
        }


        [Fact]
        static void AsyncMethodBuilderAttributeSingleParameterWrong()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[AsyncMethodBuilder(1)] class T { }

class Program {
    static void Main() { }
    async T f() => await Task.Delay(1);
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(int i) { } } }
";
            var compilation = CreateCompilationWithMscorlib45(source);
            compilation.VerifyEmitDiagnostics(
                // (9,13): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async T f() => await Task.Delay(1);
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "f").WithLocation(9, 13)
                );
        }

    }
}
