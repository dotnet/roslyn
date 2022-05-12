// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
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
            Assert.True(method.IsAsyncEffectivelyReturningTask(compilation));
            method = (MethodSymbol)testData.GetMethodData("C.G<T>(T)").Method;
            Assert.True(method.IsAsync);
            Assert.True(method.IsAsyncEffectivelyReturningGenericTask(compilation));
            verifier.VerifyIL("C.F()",
@"{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (C.<F>d__0 V_0)
  IL_0000:  newobj     ""C.<F>d__0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       ""MyTaskMethodBuilder MyTaskMethodBuilder.Create()""
  IL_000c:  stfld      ""MyTaskMethodBuilder C.<F>d__0.<>t__builder""
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4.m1
  IL_0013:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0018:  ldloc.0
  IL_0019:  ldflda     ""MyTaskMethodBuilder C.<F>d__0.<>t__builder""
  IL_001e:  ldloca.s   V_0
  IL_0020:  call       ""void MyTaskMethodBuilder.Start<C.<F>d__0>(ref C.<F>d__0)""
  IL_0025:  ldloc.0
  IL_0026:  ldflda     ""MyTaskMethodBuilder C.<F>d__0.<>t__builder""
  IL_002b:  call       ""MyTask MyTaskMethodBuilder.Task.get""
  IL_0030:  ret
}");
            verifier.VerifyIL("C.G<T>(T)",
@"{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (C.<G>d__1<T> V_0)
  IL_0000:  newobj     ""C.<G>d__1<T>..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       ""MyTaskMethodBuilder<T> MyTaskMethodBuilder<T>.Create()""
  IL_000c:  stfld      ""MyTaskMethodBuilder<T> C.<G>d__1<T>.<>t__builder""
  IL_0011:  ldloc.0
  IL_0012:  ldarg.0
  IL_0013:  stfld      ""T C.<G>d__1<T>.t""
  IL_0018:  ldloc.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int C.<G>d__1<T>.<>1__state""
  IL_001f:  ldloc.0
  IL_0020:  ldflda     ""MyTaskMethodBuilder<T> C.<G>d__1<T>.<>t__builder""
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       ""void MyTaskMethodBuilder<T>.Start<C.<G>d__1<T>>(ref C.<G>d__1<T>)""
  IL_002c:  ldloc.0
  IL_002d:  ldflda     ""MyTaskMethodBuilder<T> C.<G>d__1<T>.<>t__builder""
  IL_0032:  call       ""MyTask<T> MyTaskMethodBuilder<T>.Task.get""
  IL_0037:  ret
}");
        }

        [Fact]
        public void AsyncMethod_CreateHasRefReturn()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{
    static async MyTask F() { await Task.Delay(0); }
    static async MyTask<T> G<T>(T t) { await Task.Delay(0); return t; }
    static async MyTask<int> M() { await F(); return await G(3); }
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
    public static ref MyTaskMethodBuilder Create() => throw null;
    internal MyTaskMethodBuilder(MyTask task) { _task = task; }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { stateMachine.MoveNext(); }
    public void SetException(Exception e) { }
    public void SetResult() { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask Task => _task;
}
struct MyTaskMethodBuilder<T>
{
    private MyTask<T> _task;
    public static ref MyTaskMethodBuilder<T> Create() => throw null;
    internal MyTaskMethodBuilder(MyTask<T> task) { _task = task; }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { stateMachine.MoveNext(); }
    public void SetException(Exception e) { }
    public void SetResult(T t) { _task._result = t; }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask<T> Task => _task;
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            var compilation = CreateCompilationWithMscorlib45(source);
            compilation.VerifyEmitDiagnostics(
                // (6,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.Create'
                //     static async MyTask F() { await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "Create").WithLocation(6, 29),
                // (7,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.Create'
                //     static async MyTask<T> G<T>(T t) { await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "Create").WithLocation(7, 38),
                // (8,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.Create'
                //     static async MyTask<int> M() { await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await F(); return await G(3); }").WithArguments("MyTaskMethodBuilder<int>", "Create").WithLocation(8, 34)
                );
        }

        [Fact]
        public void AsyncMethod_BuilderFactoryDisallowed()
        {
            // Only method-level builder overrides allow having Create() return a different builder
            var source =
@"using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C
{
    static async MyTask F() { await Task.Delay(0); }
    static async MyTask<T> G<T>(T t) { await Task.Delay(0); return t; }
    static async MyTask<int> M() { await F(); return await G(3); }
}
[AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory))]
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
[AsyncMethodBuilder(typeof(MyTaskMethodBuilderFactory<>))]
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
struct MyTaskMethodBuilderFactory
{
    public static MyTaskMethodBuilder Create() => throw null;
}
struct MyTaskMethodBuilder
{
    private MyTask _task;
    internal MyTaskMethodBuilder(MyTask task) { _task = task; }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { stateMachine.MoveNext(); }
    public void SetException(Exception e) { }
    public void SetResult() { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask Task => _task;
}
struct MyTaskMethodBuilderFactory<T>
{
    public static MyTaskMethodBuilder<T> Create() => throw null;
}
struct MyTaskMethodBuilder<T>
{
    private MyTask<T> _task;
    internal MyTaskMethodBuilder(MyTask<T> task) { _task = task; }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { stateMachine.MoveNext(); }
    public void SetException(Exception e) { }
    public void SetResult(T t) { _task._result = t; }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask<T> Task => _task;
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            var compilation = CreateCompilationWithMscorlib45(source);
            compilation.VerifyEmitDiagnostics(
                // (6,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory.Task'
                //     static async MyTask F() { await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.Delay(0); }").WithArguments("MyTaskMethodBuilderFactory", "Task").WithLocation(6, 29),
                // (6,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory.Create'
                //     static async MyTask F() { await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.Delay(0); }").WithArguments("MyTaskMethodBuilderFactory", "Create").WithLocation(6, 29),
                // (7,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<T>.Task'
                //     static async MyTask<T> G<T>(T t) { await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilderFactory<T>", "Task").WithLocation(7, 38),
                // (7,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<T>.Create'
                //     static async MyTask<T> G<T>(T t) { await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilderFactory<T>", "Create").WithLocation(7, 38),
                // (8,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<int>.Task'
                //     static async MyTask<int> M() { await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await F(); return await G(3); }").WithArguments("MyTaskMethodBuilderFactory<int>", "Task").WithLocation(8, 34),
                // (8,34): error CS0656: Missing compiler required member 'MyTaskMethodBuilderFactory<int>.Create'
                //     static async MyTask<int> M() { await F(); return await G(3); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await F(); return await G(3); }").WithArguments("MyTaskMethodBuilderFactory<int>", "Create").WithLocation(8, 34)
                );
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
            Assert.True(method.IsAsyncEffectivelyReturningTask(compilation));
            Assert.Equal("C.MyTask", method.ReturnTypeWithAnnotations.ToDisplayString());
            method = (MethodSymbol)testData.GetMethodData("C.G()").Method;
            Assert.True(method.IsAsync);
            Assert.True(method.IsAsyncEffectivelyReturningGenericTask(compilation));
            Assert.Equal("C.MyTask<int>", method.ReturnTypeWithAnnotations.ToDisplayString());
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
            Assert.True(method.IsAsyncEffectivelyReturningTask(compilation));
            Assert.Equal("MyTask", method.ReturnTypeWithAnnotations.ToDisplayString());
            method = (MethodSymbol)testData.GetMethodData("C.<>c.<M>b__3_1()").Method;
            Assert.True(method.IsAsync);
            Assert.True(method.IsAsyncEffectivelyReturningGenericTask(compilation));
            Assert.Equal("MyTask<int>", method.ReturnTypeWithAnnotations.ToDisplayString());
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
            var method = (MethodSymbol)testData.GetMethodData("C.<M>g__F|0_0()").Method;
            Assert.True(method.IsAsync);
            Assert.True(method.IsAsyncEffectivelyReturningTask(compilation));
            Assert.Equal("MyTask", method.ReturnTypeWithAnnotations.ToDisplayString());
            method = (MethodSymbol)testData.GetMethodData("C.<M>g__G|0_1<T>(T)").Method;
            Assert.True(method.IsAsync);
            Assert.True(method.IsAsyncEffectivelyReturningGenericTask(compilation));
            Assert.Equal("MyTask<T>", method.ReturnTypeWithAnnotations.ToDisplayString());
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
        public static void AsyncMethodBuilderAttributeMultipleParameters()
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
        public static void AsyncMethodBuilderAttributeSingleParameterWrong()
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

        [Fact]
        public void AsyncMethodBuilder_IncorrectMethodArity()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
namespace System.Runtime.CompilerServices
{
    class AsyncMethodBuilderAttribute : System.Attribute
   {
       public AsyncMethodBuilderAttribute(System.Type t) { }
    }
}
class C
{
    static async MyTask F() { await Task.Delay(0); }
    static async MyTask<T> G<T>(T t) { await Task.Delay(0); return t; }
    static async MyTask<int> M()
    {
        await F();
        return await G(3);
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
    internal MyTaskMethodBuilder(MyTask task) { }
    public void SetStateMachine<T>(IAsyncStateMachine stateMachine) { }
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
    internal MyTaskMethodBuilder(MyTask<T> task) { }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TAwaiter, TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
    public void SetException(Exception e) { }
    public void SetResult(T t) { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask<T> Task => default(MyTask<T>);
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll);
            compilation.VerifyEmitDiagnostics(
                // (13,29): error CS0656: Missing compiler required member 'MyTaskMethodBuilder.SetStateMachine'
                //     static async MyTask F() { await Task.Delay(0); }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.Delay(0); }").WithArguments("MyTaskMethodBuilder", "SetStateMachine").WithLocation(13, 29),
                // (14,38): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<T>.Start'
                //     static async MyTask<T> G<T>(T t) { await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ await Task.Delay(0); return t; }").WithArguments("MyTaskMethodBuilder<T>", "Start").WithLocation(14, 38),
                // (16,5): error CS0656: Missing compiler required member 'MyTaskMethodBuilder<int>.Start'
                //     {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{
        await F();
        return await G(3);
    }").WithArguments("MyTaskMethodBuilder<int>", "Start").WithLocation(16, 5));
        }

        [WorkItem(12616, "https://github.com/dotnet/roslyn/issues/12616")]
        [Fact]
        public void AsyncMethodBuilder_MissingConstraints()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
namespace System.Runtime.CompilerServices
{
    class AsyncMethodBuilderAttribute : System.Attribute
   {
       public AsyncMethodBuilderAttribute(System.Type t) { }
    }
}
class C
{
    static async MyTask F() { await Task.Delay(0); }
    static async MyTask<T> G<T>(T t) { await Task.Delay(0); return t; }
    static async MyTask<int> M()
    {
        await F();
        return await G(3);
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
    internal MyTaskMethodBuilder(MyTask task) { }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    // Missing constraint: where TStateMachine : IAsyncStateMachine
    public void Start<TStateMachine>(ref TStateMachine stateMachine) { }
    public void SetException(Exception e) { }
    public void SetResult() { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask Task => default(MyTask);
}
class MyTaskMethodBuilder<T>
{
    public static MyTaskMethodBuilder<T> Create() => null;
    internal MyTaskMethodBuilder(MyTask<T> task) { }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
    public void SetException(Exception e) { }
    public void SetResult(T t) { }
    // Incorrect constraint: where TAwaiter : IAsyncStateMachine
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : IAsyncStateMachine where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask<T> Task => default(MyTask<T>);
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll);
            compilation.VerifyEmitDiagnostics(
                // (17,9): error CS0311: The type 'MyTask.Awaiter' cannot be used as type parameter 'TAwaiter' in the generic type or method 'MyTaskMethodBuilder<int>.AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter, ref TStateMachine)'. There is no implicit reference conversion from 'MyTask.Awaiter' to 'System.Runtime.CompilerServices.IAsyncStateMachine'.
                //         await F();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "await F();").WithArguments("MyTaskMethodBuilder<int>.AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter, ref TStateMachine)", "System.Runtime.CompilerServices.IAsyncStateMachine", "TAwaiter", "MyTask.Awaiter").WithLocation(17, 9),
                // (18,16): error CS0311: The type 'MyTask<int>.Awaiter' cannot be used as type parameter 'TAwaiter' in the generic type or method 'MyTaskMethodBuilder<int>.AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter, ref TStateMachine)'. There is no implicit reference conversion from 'MyTask<int>.Awaiter' to 'System.Runtime.CompilerServices.IAsyncStateMachine'.
                //         return await G(3);
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "await G(3)").WithArguments("MyTaskMethodBuilder<int>.AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter, ref TStateMachine)", "System.Runtime.CompilerServices.IAsyncStateMachine", "TAwaiter", "MyTask<int>.Awaiter").WithLocation(18, 16)

                );
        }

        [WorkItem(12616, "https://github.com/dotnet/roslyn/issues/12616")]
        [Fact]
        public void AsyncMethodBuilder_AdditionalConstraints()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
namespace System.Runtime.CompilerServices
{
    class AsyncMethodBuilderAttribute : System.Attribute
   {
       public AsyncMethodBuilderAttribute(System.Type t) { }
    }
}
class C
{
    static async MyTask F() { await Task.Delay(0); }
    static async MyTask<T> G<T>(T t) { await Task.Delay(0); return t; }
    static async MyTask<int> M()
    {
        await F();
        return await G(3);
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
    internal MyTaskMethodBuilder(MyTask task) { }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    // Additional constraint: class
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : class, IAsyncStateMachine { }
    public void SetException(Exception e) { }
    public void SetResult() { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask Task => default(MyTask);
}
class MyTaskMethodBuilder<T>
{
    public static MyTaskMethodBuilder<T> Create() => null;
    internal MyTaskMethodBuilder(MyTask<T> task) { }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
    public void SetException(Exception e) { }
    public void SetResult(T t) { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    // Additional constraint: ICriticalNotifyCompletion
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine, ICriticalNotifyCompletion { }
    public MyTask<T> Task => default(MyTask<T>);
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll);
            compilation.VerifyEmitDiagnostics(
                // (14,40): error CS0311: The type 'C.<G>d__1<T>' cannot be used as type parameter 'TStateMachine' in the generic type or method 'MyTaskMethodBuilder<T>.AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter, ref TStateMachine)'. There is no implicit reference conversion from 'C.<G>d__1<T>' to 'System.Runtime.CompilerServices.ICriticalNotifyCompletion'.
                //     static async MyTask<T> G<T>(T t) { await Task.Delay(0); return t; }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "await Task.Delay(0);").WithArguments("MyTaskMethodBuilder<T>.AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter, ref TStateMachine)", "System.Runtime.CompilerServices.ICriticalNotifyCompletion", "TStateMachine", "C.<G>d__1<T>").WithLocation(14, 40));
        }

        [WorkItem(15955, "https://github.com/dotnet/roslyn/issues/15955")]
        [Fact]
        public void OverloadWithVoidPointer()
        {
            var source =
@"class A
{
    unsafe public static void F(void* p) { }
    unsafe public static void F(int* p) { }
}
class B
{
    static void Main()
    {
        unsafe
        {
            A.F(null);
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.UnsafeDebugExe);
            compilation.VerifyEmitDiagnostics();
        }

        [Fact]
        public void AwaiterMissingINotifyCompletion()
        {
            var source0 =
@"using System;
using System.Runtime.CompilerServices;
namespace System.Runtime.CompilerServices
{
    class AsyncMethodBuilderAttribute : Attribute
    {
        public AsyncMethodBuilderAttribute(Type t) { }
    }
}
[AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
public sealed class MyTask
{
    public Awaiter GetAwaiter() => null;
    public class Awaiter
    {
        public void OnCompleted(Action a) { }
        public bool IsCompleted => true;
        public void GetResult() { }
    }
}
public sealed class MyTaskMethodBuilder
{
    public static MyTaskMethodBuilder Create() => new MyTaskMethodBuilder();
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
    }
    public void SetException(Exception e) { }
    public void SetResult() { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
    }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
    }
    public MyTask Task => new MyTask();
}";
            var compilation0 = CreateCompilationWithMscorlib45(source0);
            var ref0 = compilation0.EmitToImageReference();
            var source =
@"class Program
{
    static async MyTask F()
    {
        await new MyTask();
    }
    static void Main()
    {
        var t = F();
        t.GetAwaiter().GetResult();
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { ref0 });
            compilation.VerifyEmitDiagnostics(
                // (5,9): error CS4027: 'MyTask.Awaiter' does not implement 'INotifyCompletion'
                //         await new MyTask();
                Diagnostic(ErrorCode.ERR_DoesntImplementAwaitInterface, "await new MyTask()").WithArguments("MyTask.Awaiter", "System.Runtime.CompilerServices.INotifyCompletion").WithLocation(5, 9));
        }

        /// <summary>
        /// Avoid checking constraints in generic methods in actual AsyncTaskMethodBuilder
        /// to avoid breaking change.
        /// </summary>
        [WorkItem(21500, "https://github.com/dotnet/roslyn/issues/21500")]
        [Fact]
        public void AdditionalConstraintMissingFromStateMachine_AsyncTaskMethodBuilder()
        {
            var source0 =
@"using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
namespace System
{
    public delegate void Action();
}
namespace System.Runtime.CompilerServices
{
    public interface INotifyCompletion
    {
        void OnCompleted(Action a);
    }
    public interface ICriticalNotifyCompletion : INotifyCompletion
    {
        void UnsafeOnCompleted(Action a);
    }
    public interface IAsyncStateMachine
    {
        void MoveNext();
        void SetStateMachine(IAsyncStateMachine stateMachine);
    }
    public interface IMyStateMachine
    {
    }
    public struct AsyncTaskMethodBuilder
    {
        public static AsyncTaskMethodBuilder Create() => new AsyncTaskMethodBuilder();
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IMyStateMachine, IAsyncStateMachine
        {
        }
        public void SetException(Exception e) { }
        public void SetResult() { }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IMyStateMachine, IAsyncStateMachine
        {
        }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IMyStateMachine, IAsyncStateMachine
        {
        }
        public Task Task => new Task();
    }
}
namespace System.Threading.Tasks
{
    public class Task
    {
        public Awaiter GetAwaiter() => new Awaiter();
        public class Awaiter : INotifyCompletion
        {
            public void OnCompleted(Action a) { }
            public bool IsCompleted => true;
            public void GetResult() { }
        }
    }
}";
            var compilation0 = CreateEmptyCompilation(source0, references: new[] { MscorlibRef_v20 });
            var ref0 = compilation0.EmitToImageReference();
            var source =
@"using System.Threading.Tasks;
class Program
{
    static async Task F()
    {
        await new Task();
    }
    static void Main()
    {
        var t = F();
        t.GetAwaiter().GetResult();
    }
}";
            var compilation = CreateEmptyCompilation(source, references: new[] { MscorlibRef_v20, ref0 });
            compilation.VerifyEmitDiagnostics();
        }

        /// <summary>
        /// Verify constraints at the call-site for generic methods of async method build.
        /// </summary>
        [WorkItem(21500, "https://github.com/dotnet/roslyn/issues/21500")]
        [Fact]
        public void Start_AdditionalConstraintMissingFromStateMachine()
        {
            var source0 =
@"using System;
using System.Runtime.CompilerServices;
namespace System.Runtime.CompilerServices
{
    class AsyncMethodBuilderAttribute : Attribute
    {
        public AsyncMethodBuilderAttribute(Type t) { }
    }
}
[AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
public sealed class MyTask
{
    public Awaiter GetAwaiter() => new Awaiter();
    public class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action a) { }
        public bool IsCompleted => true;
        public void GetResult() { }
    }
}
public interface IMyStateMachine { }
public sealed class MyTaskMethodBuilder
{
    public static MyTaskMethodBuilder Create() => new MyTaskMethodBuilder();
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IMyStateMachine, IAsyncStateMachine
    {
    }
    public void SetException(Exception e) { }
    public void SetResult() { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
    }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
    }
    public MyTask Task => new MyTask();
}";
            var compilation0 = CreateCompilationWithMscorlib45(source0);
            var ref0 = compilation0.EmitToImageReference();
            var source =
@"class Program
{
    static async MyTask F()
    {
        await new MyTask();
    }
    static void Main()
    {
        var t = F();
        t.GetAwaiter().GetResult();
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { ref0 });
            compilation.VerifyEmitDiagnostics(
                // (4,5): error CS0315: The type 'Program.<F>d__0' cannot be used as type parameter 'TStateMachine' in the generic type or method 'MyTaskMethodBuilder.Start<TStateMachine>(ref TStateMachine)'. There is no boxing conversion from 'Program.<F>d__0' to 'IMyStateMachine'.
                //     {
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, @"{
        await new MyTask();
    }").WithArguments("MyTaskMethodBuilder.Start<TStateMachine>(ref TStateMachine)", "IMyStateMachine", "TStateMachine", "Program.<F>d__0").WithLocation(4, 5));
        }

        /// <summary>
        /// Verify constraints at the call-site for generic methods of async method build.
        /// </summary>
        [WorkItem(21500, "https://github.com/dotnet/roslyn/issues/21500")]
        [Fact]
        public void AwaitOnCompleted_AdditionalConstraintMissingFromAwaiter()
        {
            var source0 =
@"using System;
using System.Runtime.CompilerServices;
namespace System.Runtime.CompilerServices
{
    class AsyncMethodBuilderAttribute : Attribute
    {
        public AsyncMethodBuilderAttribute(Type t) { }
    }
}
[AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
public sealed class MyTask
{
    public Awaiter GetAwaiter() => null;
    public abstract class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action a) { }
        public bool IsCompleted => true;
        public void GetResult() { }
    }
}
public sealed class MyTaskMethodBuilder
{
    public static MyTaskMethodBuilder Create() => new MyTaskMethodBuilder();
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
    }
    public void SetException(Exception e) { }
    public void SetResult() { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion, new()
        where TStateMachine : IAsyncStateMachine
    {
    }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
    }
    public MyTask Task => new MyTask();
}";
            var compilation0 = CreateCompilationWithMscorlib45(source0);
            var ref0 = compilation0.EmitToImageReference();
            var source =
@"class Program
{
    static async MyTask F()
    {
        await new MyTask();
    }
    static void Main()
    {
        var t = F();
        t.GetAwaiter().GetResult();
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { ref0 });
            compilation.VerifyEmitDiagnostics(
                // (5,9): error CS0310: 'MyTask.Awaiter' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'TAwaiter' in the generic type or method 'MyTaskMethodBuilder.AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter, ref TStateMachine)'
                //         await new MyTask();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "await new MyTask();").WithArguments("MyTaskMethodBuilder.AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter, ref TStateMachine)", "TAwaiter", "MyTask.Awaiter").WithLocation(5, 9));
        }

        /// <summary>
        /// Verify constraints at the call-site for generic methods of async method build.
        /// </summary>
        [WorkItem(21500, "https://github.com/dotnet/roslyn/issues/21500")]
        [Fact]
        public void AwaitUnsafeOnCompleted_AdditionalConstraintMissingFromAwaiter()
        {
            var source0 =
@"using System;
using System.Runtime.CompilerServices;
namespace System.Runtime.CompilerServices
{
    class AsyncMethodBuilderAttribute : Attribute
    {
        public AsyncMethodBuilderAttribute(Type t) { }
    }
}
public interface IMyAwaiter { }
[AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
public sealed class MyTask
{
    public Awaiter GetAwaiter() => new Awaiter();
    public class Awaiter : ICriticalNotifyCompletion
    {
        public void OnCompleted(Action a) { }
        public void UnsafeOnCompleted(Action a) { }
        public bool IsCompleted => true;
        public void GetResult() { }
    }
}
public sealed class MyTaskMethodBuilder
{
    public static MyTaskMethodBuilder Create() => new MyTaskMethodBuilder();
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
    }
    public void SetException(Exception e) { }
    public void SetResult() { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
    }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : IMyAwaiter, ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
    }
    public MyTask Task => new MyTask();
}";
            var compilation0 = CreateCompilationWithMscorlib45(source0);
            var ref0 = compilation0.EmitToImageReference();
            var source =
@"class Program
{
    static async MyTask F()
    {
        await new MyTask();
    }
    static void Main()
    {
        var t = F();
        t.GetAwaiter().GetResult();
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { ref0 });
            compilation.VerifyEmitDiagnostics(
                // (5,9): error CS0311: The type 'MyTask.Awaiter' cannot be used as type parameter 'TAwaiter' in the generic type or method 'MyTaskMethodBuilder.AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter, ref TStateMachine)'. There is no implicit reference conversion from 'MyTask.Awaiter' to 'IMyAwaiter'.
                //         await new MyTask();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "await new MyTask();").WithArguments("MyTaskMethodBuilder.AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter, ref TStateMachine)", "IMyAwaiter", "TAwaiter", "MyTask.Awaiter").WithLocation(5, 9));
        }

        [Fact, WorkItem(33388, "https://github.com/dotnet/roslyn/pull/33388")]
        public void AttributeArgument_TaskLikeOverloadResolution()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class A : Attribute
{
    public A(int i) { }
}
class B
{
    public static int F(Func<MyTask<C>> t) => 1;
    public static int F(Func<Task<object>> t) => 2;
}
[A(B.F(async () => null))]
class C
{
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

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (15,4): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(B.F(async () => null))]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "B.F(async () => null)").WithLocation(15, 4));
        }

        [Fact, WorkItem(37712, "https://github.com/dotnet/roslyn/issues/37712")]
        public void TaskLikeWithRefStructValue()
        {
            var source = @"
using System;
using System.Threading.Tasks;
ref struct MyAwaitable
{
    public MyAwaiter GetAwaiter() => new MyAwaiter();
}
struct MyAwaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public bool IsCompleted => true;
    public MyResult GetResult() => new MyResult();
    public void OnCompleted(Action continuation) { }
}
ref struct MyResult
{
}
class Program
{
    public static async Task Main()
    {
        M(await new MyAwaitable());
    }
    public static void M(MyResult r)
    {
        Console.WriteLine(3);
    }
}
";

            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            // ILVerify: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator.
            CompileAndVerify(compilation, verify: Verification.FailsILVerify, expectedOutput: "3");

            compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, verify: Verification.FailsILVerify, expectedOutput: "3");
        }
    }
}
