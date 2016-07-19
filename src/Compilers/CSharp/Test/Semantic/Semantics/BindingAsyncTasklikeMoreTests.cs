// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
struct MyTask
{
    public static MyTaskMethodBuilder CreateAsyncMethodBuilder() => new MyTaskMethodBuilder(new MyTask());
    internal Awaiter GetAwaiter() => new Awaiter();
    internal class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action a) { }
        internal bool IsCompleted => true;
        internal void GetResult() { }
    }
}
struct MyTask<T>
{
    internal T _result;
    public static MyTaskMethodBuilder<T> CreateAsyncMethodBuilder() => new MyTaskMethodBuilder<T>(new MyTask<T>());
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
}";
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
  IL_0007:  call       ""MyTaskMethodBuilder MyTask.CreateAsyncMethodBuilder()""
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
  IL_000e:  call       ""MyTaskMethodBuilder<T> MyTask<T>.CreateAsyncMethodBuilder()""
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
struct MyTask
{
    public static MyTaskMethodBuilder CreateAsyncMethodBuilder() => new MyTaskMethodBuilder();
}
struct MyTask<T>
{
    public static MyTaskMethodBuilder<T> CreateAsyncMethodBuilder() => new MyTaskMethodBuilder<T>();
}
struct MyTaskMethodBuilder
{
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
    public void SetException(Exception e) { }
    public void SetResult() { }
}
struct MyTaskMethodBuilder<T>
{
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask<T> Task { get { return default(MyTask<T>); } }
}";
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
    private class MyTask
    {
        public static MyTaskMethodBuilder CreateAsyncMethodBuilder() => null;
    }
    private class MyTask<T>
    {
        public static MyTaskMethodBuilder<T> CreateAsyncMethodBuilder() => null;
    }
    private class MyTaskMethodBuilder
    {
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
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
        public void SetException(Exception e) { }
        public void SetResult(T t) { }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
        public MyTask<T> Task { get { return null; } }
    }
}";
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
class MyTask
{
    public static MyTaskMethodBuilder CreateAsyncMethodBuilder() => null;
    internal Awaiter GetAwaiter() => null;
    internal class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action a) { }
        internal bool IsCompleted => true;
        internal void GetResult() { }
    }
}
class MyTask<T>
{
    public static MyTaskMethodBuilder<T> CreateAsyncMethodBuilder() => null;
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
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
    public void SetException(Exception e) { }
    public void SetResult(T t) { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask<T> Task => default(MyTask<T>);
}";
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
struct MyTask
{
    public static MyTaskMethodBuilder CreateAsyncMethodBuilder() => null;
    internal Awaiter GetAwaiter() => null;
    internal class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action a) { }
        internal bool IsCompleted => true;
        internal void GetResult() { }
    }
}
struct MyTask<T>
{
    public static MyTaskMethodBuilder<T> CreateAsyncMethodBuilder() => null;
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
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
    public void SetException(Exception e) { }
    public void SetResult(T t) { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask<T> Task => default(MyTask<T>);
}";
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
struct MyTask
{
    public static string CreateAsyncMethodBuilder() => null;
    internal Awaiter GetAwaiter() => null;
    internal class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action a) { }
        internal bool IsCompleted => true;
        internal void GetResult() { }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source);
            compilation.VerifyEmitDiagnostics(
                // (8,26): error CS0656: Missing compiler required member 'string.Task'
                //         async MyTask F() { };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ }").WithArguments("string", "Task").WithLocation(8, 26));
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
struct MyTask<T>
{
    public static IEquatable<T> CreateAsyncMethodBuilder() => null;
    internal Awaiter GetAwaiter() => null;
    internal class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action a) { }
        internal bool IsCompleted => true;
        internal T GetResult() => default(T);
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source);
            compilation.VerifyEmitDiagnostics(
                // (8,35): error CS0656: Missing compiler required member 'IEquatable<T>.Task'
                //         async MyTask<T> F<T>(T t) => t;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=> t").WithArguments("System.IEquatable<T>", "Task").WithLocation(8, 35));
        }
    }
}
