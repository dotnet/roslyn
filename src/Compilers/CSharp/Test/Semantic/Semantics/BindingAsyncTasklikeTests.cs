// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class BindingAsyncTasklikeTests : CompilingTestBase
    {
        [Fact]
        public void AsyncTasklikeFromBuilderMethod()
        {
            var source = @"
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
class C {
    async ValueTask f() { await (Task)null; }
    async ValueTask<int> g() { await (Task)null; return 1; }
}
[AsyncMethodBuilder(typeof(string))]
struct ValueTask { }
[AsyncMethodBuilder(typeof(Task<>))]
struct ValueTask<T> { }

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";

            var compilation = CreateCompilationWithMscorlib461(source).VerifyDiagnostics();
            var methodf = compilation.GetMember<MethodSymbol>("C.f");
            var methodg = compilation.GetMember<MethodSymbol>("C.g");
            Assert.True(methodf.IsAsync);
            Assert.True(methodg.IsAsync);
        }

        [Fact]
        public void AsyncTasklikeNotFromDelegate()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
    }

    static async Task f()
    {
        await new Awaitable();
        await new Unawaitable(); // error: GetAwaiter must be a field not a delegate
    }

    static async Tasklike g()
    {
        await (Task)null;
    }
}

class Awaitable
{
    public TaskAwaiter GetAwaiter() => (Task.FromResult(1) as Task).GetAwaiter();
}

class Unawaitable
{
    public Func<TaskAwaiter> GetAwaiter = () => (Task.FromResult(1) as Task).GetAwaiter();
}

[AsyncMethodBuilder(typeof(TasklikeMethodBuilder))]
public class Tasklike { }

public class TasklikeMethodBuilder
{
    public static TasklikeMethodBuilder Create() => null;
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void SetResult() { }
    public void SetException(Exception exception) { }
    private void EnsureTaskBuilder() { }
    public Tasklike Task => null;
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            CreateCompilationWithMscorlib461(source).VerifyDiagnostics(
                // (15,9): error CS0118: 'GetAwaiter' is a field but is used like a method
                //         await new Unawaitable(); // error: GetAwaiter must be a field not a delegate
                Diagnostic(ErrorCode.ERR_BadSKknown, "await new Unawaitable()").WithArguments("GetAwaiter", "field", "method").WithLocation(15, 9)
            );
        }

        private bool VerifyTaskOverloads(string arg, string betterOverload, string worseOverload, bool implicitConversionToTask = false, bool isError = false)
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class Program
{
    #pragma warning disable CS1998
    static void Main()
    {
        var s = (<<arg>>);
        Console.Write(s);
    }
    <<betterOverload>>
    <<worseOverload>>
}
[AsyncMethodBuilder(typeof(ValueTaskMethodBuilder))]
struct ValueTask
{
    <<implicitConversionToTask>>
    internal Awaiter GetAwaiter() => new Awaiter();
    internal class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action a) { }
        internal bool IsCompleted => true;
        internal void GetResult() { }
    }
}
[AsyncMethodBuilder(typeof(ValueTaskMethodBuilder<>))]
struct ValueTask<T>
{
    internal T _result;
    <<implicitConversionToTaskT>>
    public T Result => _result;
    internal Awaiter GetAwaiter() => new Awaiter(this);
    internal class Awaiter : INotifyCompletion
    {
        private ValueTask<T> _task;
        internal Awaiter(ValueTask<T> task) { _task = task; }
        public void OnCompleted(Action a) { }
        internal bool IsCompleted => true;
        internal T GetResult() => _task.Result;
    }
}
sealed class ValueTaskMethodBuilder
{
    private ValueTask _task = new ValueTask();
    public static ValueTaskMethodBuilder Create() => new ValueTaskMethodBuilder();
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
    {
        stateMachine.MoveNext();
    }
    public void SetException(Exception e) { }
    public void SetResult() { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public ValueTask Task => _task;
}
sealed class ValueTaskMethodBuilder<T>
{
    private ValueTask<T> _task = new ValueTask<T>();
    public static ValueTaskMethodBuilder<T> Create() => null;
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
    {
        stateMachine.MoveNext();
    }
    public void SetException(Exception e) { }
    public void SetResult(T t) { _task._result = t; }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public ValueTask<T> Task => _task;
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            source = source.Replace("<<arg>>", arg);
            source = source.Replace("<<betterOverload>>", (betterOverload != null) ? "static string " + betterOverload + " => \"better\";" : "");
            source = source.Replace("<<worseOverload>>", (worseOverload != null) ? "static string " + worseOverload + " => \"worse\";" : "");
            source = source.Replace("<<implicitConversionToTask>>", implicitConversionToTask ? "public static implicit operator Task(ValueTask t) => Task.FromResult(0);" : "");
            source = source.Replace("<<implicitConversionToTaskT>>", implicitConversionToTask ? "public static implicit operator Task<T>(ValueTask<T> t) => Task.FromResult<T>(t._result);" : "");
            if (isError)
            {
                var compilation = CreateCompilationWithMscorlib461(source);
                var diagnostics = compilation.GetDiagnostics();
                Assert.True(diagnostics.Length == 1);
                Assert.True(diagnostics.First().Code == (int)ErrorCode.ERR_AmbigCall);
            }
            else
            {
                CompileAndVerify(source, targetFramework: TargetFramework.Empty, references: new[] { MscorlibRef_v4_0_30316_17626 }, expectedOutput: "better");
            }
            return true;
        }

        [Fact]
        public void TasklikeA3() => VerifyTaskOverloads("f(async () => 3)",
                                                        "f(Func<ValueTask<int>> lambda)",
                                                        null);

        [Fact]
        public void TasklikeA3n() => VerifyTaskOverloads("f(async () => {})",
                                                         "f(Func<ValueTask> lambda)",
                                                         null);

        [Fact]
        public void TasklikeA4() => VerifyTaskOverloads("f(async () => 3)",
                                                        "f<T>(Func<ValueTask<T>> labda)",
                                                        null);

        [Fact]
        public void TasklikeA5s() => VerifyTaskOverloads("f(() => 3)",
                                                         "f<T>(Func<T> lambda)",
                                                         "f<T>(Func<ValueTask<T>> lambda)");

        [Fact]
        public void TasklikeA5a() => VerifyTaskOverloads("f(async () => 3)",
                                                         "f<T>(Func<ValueTask<T>> lambda)",
                                                         "f<T>(Func<T> lambda)");

        [Fact]
        public void TasklikeA6() => VerifyTaskOverloads("f(async () => 3)",
                                                        "f(Func<ValueTask<int>> lambda)",
                                                        "f(Func<ValueTask<double>> lambda)");

        [Fact]
        public void TasklikeA7() => VerifyTaskOverloads("f(async () => 3)",
                                                        "f(Func<ValueTask<byte>> lambda)",
                                                        "f(Func<ValueTask<short>> lambda)");

        [Fact]
        public void TasklikeA8() => VerifyTaskOverloads("f(async () => {})",
                                                        "f(Func<ValueTask> lambda)",
                                                        "f(Action lambda)");

        [Fact]
        public void TasklikeB7_ic0() => VerifyTaskOverloads("f(async () => 3)",
                                                           "f(Func<ValueTask<int>> lambda)",
                                                           "f(Func<Task<int>> lambda)",
                                                           isError: true);

        [Fact]
        public void TasklikeB7_ic1() => VerifyTaskOverloads("f(async () => 3)",
                                                            "f(Func<ValueTask<int>> lambda)",
                                                            "f(Func<Task<int>> lambda)",
                                                            implicitConversionToTask: true);

        [Fact]
        public void TasklikeB7g_ic0() => VerifyTaskOverloads("f(async () => 3)",
                                                             "f<T>(Func<ValueTask<T>> lambda)",
                                                             "f<T>(Func<Task<T>> lambda)",
                                                             isError: true);

        [Fact]
        public void TasklikeB7g_ic1() => VerifyTaskOverloads("f(async () => 3)",
                                                             "f<T>(Func<ValueTask<T>> lambda)",
                                                             "f<T>(Func<Task<T>> lambda)",
                                                            implicitConversionToTask: true);

        [Fact]
        public void TasklikeB7n_ic0() => VerifyTaskOverloads("f(async () => {})",
                                                             "f(Func<ValueTask> lambda)",
                                                             "f(Func<Task> lambda)",
                                                             isError: true);

        [Fact]
        public void TasklikeB7n_ic1() => VerifyTaskOverloads("f(async () => {})",
                                                             "f(Func<ValueTask> lambda)",
                                                             "f(Func<Task> lambda)",
                                                            implicitConversionToTask: true);

        [Fact]
        public void TasklikeC1() => VerifyTaskOverloads("f(async () => 3)",
                                                        "f(Func<ValueTask<int>> lambda)",
                                                        "f(Func<Task<double>> lambda)");

        [Fact]
        public void TasklikeC2() => VerifyTaskOverloads("f(async () => 3)",
                                                        "f(Func<ValueTask<byte>> lambda)",
                                                        "f(Func<Task<short>> lambda)");

        [Fact]
        public void TasklikeC5() => VerifyTaskOverloads("f(async () => 3)",
                                                        "f(Func<ValueTask<int>> lambda)",
                                                        "f<T>(Func<Task<T>> lambda)");

        [Fact]
        public void AsyncTasklikeMethod()
        {
            var source = @"
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
class C {
    async ValueTask f() { await Task.Delay(0); }
    async ValueTask<int> g() { await Task.Delay(0); return 1; }
}
[AsyncMethodBuilder(typeof(ValueTaskMethodBuilder))]
struct ValueTask { }
[AsyncMethodBuilder(typeof(ValueTaskMethodBuilder<>))]
struct ValueTask<T> { }
class ValueTaskMethodBuilder {}
class ValueTaskMethodBuilder<T> {}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            var compilation = CreateCompilationWithMscorlib461(source).VerifyDiagnostics();
            var methodf = compilation.GetMember<MethodSymbol>("C.f");
            var methodg = compilation.GetMember<MethodSymbol>("C.g");
            Assert.True(methodf.IsAsync);
            Assert.True(methodg.IsAsync);
        }

        [Fact]
        public void NotTasklike()
        {
            var source1 = @"
using System.Threading.Tasks;
class C
{
    static void Main() { }
    async MyTask f() { await (Task)null; }
}
public class MyTask { }
";
            CreateCompilationWithMscorlib461(source1).VerifyDiagnostics(
                // (6,18): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async MyTask f() { await (Task)null; }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "f").WithLocation(6, 18),
                // (6,18): error CS0161: 'C.f()': not all code paths return a value
                //     async MyTask f() { await (Task)null; }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "f").WithArguments("C.f()").WithLocation(6, 18)
                );

            var source2 = @"
using System.Threading.Tasks;
class C
{
    static void Main() { }
    async MyTask f() { await (Task)null; }
}
public class MyTask { }
";
            CreateCompilationWithMscorlib461(source2).VerifyDiagnostics(
                // (6,18): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async MyTask f() { await (Task)null; }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "f").WithLocation(6, 18),
                // (6,18): error CS0161: 'C.f()': not all code paths return a value
                //     async MyTask f() { await (Task)null; }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "f").WithArguments("C.f()").WithLocation(6, 18)
                );

            var source3 = @"
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
class C
{
    static void Main() { }
    async MyTask f() { await (Task)null; }
}
[AsyncMethodBuilder(typeof(MyTaskBuilder))]
public class MyTask { }
public class MyTaskBuilder
{
    public static MyTaskBuilder Create() => null;
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            CreateCompilationWithMscorlib461(source3).VerifyDiagnostics();
        }

        [Fact]
        public void AsyncTasklikeOverloadLambdas()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C {
    static void Main() {
        f(async () => { await (Task)null; return 1; });
        h(async () => { await (Task)null; });
    }
    static void f<T>(Func<MyTask<T>> lambda) { }
    static void f<T>(Func<T> lambda) { }
    static void f<T>(T arg) { }

    static void h(Func<MyTask> lambda) { }
    static void h(Func<Task> lambda) { }
}

[AsyncMethodBuilder(typeof(MyTaskBuilder<>))]
public class MyTask<T> { }
public class MyTaskBuilder<T> {
    public static MyTaskBuilder<T> Create() => null;
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void SetResult(T result) { }
    public void SetException(Exception exception) { }
    public MyTask<T> Task => default(MyTask<T>);
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
}

[AsyncMethodBuilder(typeof(MyTaskBuilder))]
public class MyTask { }
public class MyTaskBuilder
{
    public static MyTaskBuilder Create() => null;
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void SetResult() { }
    public void SetException(Exception exception) { }
    public MyTask Task => default(MyTask);
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            CreateCompilationWithMscorlib461(source).VerifyDiagnostics(
                // (8,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.h(Func<MyTask>)' and 'C.h(Func<Task>)'
                //         h(async () => { await (Task)null; });
                Diagnostic(ErrorCode.ERR_AmbigCall, "h").WithArguments("C.h(System.Func<MyTask>)", "C.h(System.Func<System.Threading.Tasks.Task>)").WithLocation(8, 9)
                );
        }

        [Fact]
        public void AsyncTasklikeInadmissibleArity()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
class C {
    async Mismatch2<int,int> g() { await Task.Delay(0); return 1; }
}
[AsyncMethodBuilder(typeof(Mismatch2MethodBuilder<>))]
struct Mismatch2<T,U> { }
class Mismatch2MethodBuilder<T> {}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            var comp = CreateCompilationWithMscorlib461(source);
            comp.VerifyEmitDiagnostics(
                // (5,30): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async Mismatch2<int,int> g() { await Task.Delay(0); return 1; }
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "g").WithLocation(5, 30)
                );
        }

        [Fact]
        public void AsyncTasklikeOverloadInvestigations()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class Program
{
    static Task<int> arg1 = null;
    static int arg2 = 0;

    static int f1(MyTask<int> t) => 0;
    static int f1(Task<int> t) => 1;
    static int r1 = f1(arg1); // 1

    static void Main()
    {
        Console.Write(r1);
    }
}

[AsyncMethodBuilder(typeof(ValueTaskBuilder<>))]
class ValueTask<T>
{
    public static implicit operator ValueTask<T>(Task<T> task) => null;
}

[AsyncMethodBuilder(typeof(MyTaskBuilder<>))]
class MyTask<T>
{
    public static implicit operator MyTask<T>(Task<T> task) => null;
    public static implicit operator Task<T>(MyTask<T> mytask) => null;
}

class ValueTaskBuilder<T>
{
    public static ValueTaskBuilder<T> Create() => null;
    public void Start<TSM>(ref TSM sm) where TSM : IAsyncStateMachine { }
    public void SetStateMachine(IAsyncStateMachine sm) { }
    public void SetResult(T r) { }
    public void SetException(Exception ex) { }
    public ValueTask<T> Task => null;
    public void AwaitOnCompleted<TA, TSM>(ref TA a, ref TSM sm) where TA : INotifyCompletion where TSM : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TA, TSM>(ref TA a, ref TSM sm) where TA : ICriticalNotifyCompletion where TSM : IAsyncStateMachine { }
}

class MyTaskBuilder<T>
{
    public static MyTaskBuilder<T> Create() => null;
    public void Start<TSM>(ref TSM sm) where TSM : IAsyncStateMachine { }
    public void SetStateMachine(IAsyncStateMachine sm) { }
    public void SetResult(T r) { }
    public void SetException(Exception ex) { }
    public ValueTask<T> Task => null;
    public void AwaitOnCompleted<TA, TSM>(ref TA a, ref TSM sm) where TA : INotifyCompletion where TSM : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TA, TSM>(ref TA a, ref TSM sm) where TA : ICriticalNotifyCompletion where TSM : IAsyncStateMachine { }
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            CompileAndVerify(source, targetFramework: TargetFramework.Empty, references: new[] { MscorlibRef_v4_0_30316_17626 }, expectedOutput: "1");
        }

        [Fact]
        public void AsyncTasklikeBetterness()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class Program
{
    static char f1(Func<ValueTask<short>> lambda) => 's';
    static char f1(Func<Task<byte>> lambda) => 'b';

    static char f2(Func<Task<short>> lambda) => 's';
    static char f2(Func<ValueTask<byte>> lambda) => 'b';

    static char f3(Func<Task<short>> lambda) => 's';
    static char f3(Func<Task<byte>> lambda) => 'b';

    static char f4(Func<ValueTask<short>> lambda) => 's';
    static char f4(Func<ValueTask<byte>> lambda) => 'b';

    static void Main()
    {
        Console.Write(f1(async () => { await (Task)null; return 9; }));
        Console.Write(f2(async () => { await (Task)null; return 9; }));
        Console.Write(f3(async () => { await (Task)null; return 9; }));
        Console.Write(f4(async () => { await (Task)null; return 9; }));
    }
}

[AsyncMethodBuilder(typeof(ValueTaskBuilder<>))]
public class ValueTask<T> { }

public class ValueTaskBuilder<T>
{
    public static ValueTaskBuilder<T> Create() => null;
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TSM>(ref TSM stateMachine) where TSM : IAsyncStateMachine { }
    public void AwaitOnCompleted<TA, TSM>(ref TA awaiter, ref TSM stateMachine) where TA : INotifyCompletion where TSM : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TA, TSM>(ref TA awaiter, ref TSM stateMachine) where TA : ICriticalNotifyCompletion where TSM : IAsyncStateMachine { }
    public void SetResult(T result) { }
    public void SetException(Exception ex) { }
    public ValueTask<T> Task => null;
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } }
";
            CompileAndVerify(source, targetFramework: TargetFramework.Empty, references: new[] { MscorlibRef_v4_0_30316_17626 }, expectedOutput: "bbbb");
        }
    }
}
