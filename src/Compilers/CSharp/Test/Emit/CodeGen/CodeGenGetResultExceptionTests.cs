// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.TestMetadata;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen;

public class CodeGenGetResultExceptionTests : EmitMetadataTestBase
{
    const string ExtensionsSource = """
#nullable enable

using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

internal static class TaskAwaiterExceptionExtensions
{
    // !!!WARNING!!!: These depend on the exact layout of awaiters having the stored task
    // as the first field of the struct.  That is the case in both .NET Framework and .NET Core.

    public static void GetResult(this TaskAwaiter awaiter, out Exception? exception)
    {
        Task? t = Unsafe.As<TaskAwaiter, Task?>(ref awaiter)!;
        exception = null;
        if (t is not null)
        {
            if (t.IsFaulted)
            {
                exception = t.Exception!.InnerException;
            }
            else if (t.IsCanceled)
            {
                exception = new TaskCanceledException(t);
            }
        }
    }

    public static TResult GetResult<TResult>(this TaskAwaiter<TResult> awaiter, out Exception? exception)
    {
        Task<TResult>? t = Unsafe.As<TaskAwaiter<TResult>, Task<TResult>?>(ref awaiter)!;
        exception = null;

        if (t is not null)
        {
            if (!t.IsFaulted && !t.IsCanceled)
            {
                return t.Result;
            }

            exception = t.Exception?.InnerException ?? new TaskCanceledException(t);
        }

        return default!;
    }

    public static void GetResult(this ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter, out Exception? exception)
    {
        Task? t = Unsafe.As<ConfiguredTaskAwaitable.ConfiguredTaskAwaiter, Task?>(ref awaiter)!;
        exception = null;
        if (t is not null)
        {
            if (t.IsFaulted)
            {
                exception = t.Exception!.InnerException;
            }
            else if (t.IsCanceled)
            {
                exception = new TaskCanceledException(t);
            }
        }
    }

    public static TResult GetResult<TResult>(this ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter awaiter, out Exception? exception)
    {
        Task<TResult>? t = Unsafe.As<ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter, Task<TResult>?>(ref awaiter)!;
        exception = null;

        if (t is not null)
        {
            if (!t.IsFaulted && !t.IsCanceled)
            {
                return t.Result;
            }

            exception = t.Exception?.InnerException ?? new TaskCanceledException(t);
        }

        return default!;
    }
}

internal static class ValueTaskAwaiterExceptionExtensions
{
    // !!!WARNING!!!: These depend on the exact layout of awaiters having the stored task
    // as the first field of the struct.  That _should_ be the case in both .NET Framework
    // and .NET Core, but it's technically not guaranteed, as the structs have AutoLayout
    // and the runtime _could_ reorder the fields, though it doesn't have a good reason to.

    public static void GetResult(this ValueTaskAwaiter awaiter, out Exception? exception)
    {
        exception = null;

        if (Unsafe.As<ValueTaskAwaiter, object?>(ref awaiter) is not Task t)
        {
            awaiter.GetResult();
            return;
        }

        if (t.IsFaulted | t.IsCanceled)
        {
            exception = t.Exception?.InnerException ?? new TaskCanceledException(t);
        }
    }

    public static TResult GetResult<TResult>(this ValueTaskAwaiter<TResult> awaiter, out Exception? exception)
    {
        exception = null;

        if (Unsafe.As<ValueTaskAwaiter<TResult>, object?>(ref awaiter) is not Task<TResult> t)
        {
            return awaiter.GetResult();
        }

        if (!(t.IsFaulted | t.IsCanceled))
        {
            return t.Result;
        }

        exception = t.Exception?.InnerException ?? new TaskCanceledException(t);
        return default!;
    }

    public static void GetResult(this ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter, out Exception? exception)
    {
        exception = null;

        if (Unsafe.As<ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter, object?>(ref awaiter) is not Task t)
        {
            awaiter.GetResult();
            return;
        }

        if (t.IsFaulted | t.IsCanceled)
        {
            exception = t.Exception?.InnerException ?? new TaskCanceledException(t);
        }
    }

    public static TResult GetResult<TResult>(this ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter awaiter, out Exception? exception)
    {
        exception = null;

        if (Unsafe.As<ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter, object?>(ref awaiter) is not Task<TResult> t)
        {
            return awaiter.GetResult();
        }

        if (!(t.IsFaulted | t.IsCanceled))
        {
            return t.Result;
        }

        exception = t.Exception?.InnerException ?? new TaskCanceledException(t);
        return default!;
    }
}
""";

    [Fact]
    public void MyTask_InsideTryWithoutCatch_WithGetResultExceptionMethod()
    {
        var source = @"
using System;
using System.Threading;

//Implementation of you own async pattern
public class MyTask
{
    public async void Run()
    {
        int tests = 0;

        try
        {
            tests++;
            var myTask = new MyTask();
            var x = await myTask;
            if (x == 123) Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}
public class MyTaskAwaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action continuationAction)
    {
    }

    public int GetResult(out Exception e)
    {
        e = null;
        return 123;
    }

    public bool IsCompleted { get { return true; } }
}

public static class Extension
{
    public static MyTaskAwaiter GetAwaiter(this MyTask my)
    {
        return new MyTaskAwaiter();
    }
}
//-------------------------------------

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        new MyTask().Run();

        CompletedSignal.WaitOne();

        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";

        var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe,
            references: new[] { Net451.System, Net451.SystemCore, Net451.MicrosoftCSharp });
        compilation.VerifyDiagnostics();

        var verifier = CompileAndVerify(compilation);
        verifier.VerifyIL("MyTask.<Run>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      313 (0x139)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                MyTaskAwaiter V_2,
                System.Exception V_3,
                MyTask.<Run>d__0 V_4,
                bool V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int MyTask.<Run>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_0016
    IL_000e:  nop
    IL_000f:  ldarg.0
    IL_0010:  ldc.i4.0
    IL_0011:  stfld      "int MyTask.<Run>d__0.<tests>5__1"
    IL_0016:  nop
    .try
    {
      IL_0017:  ldloc.0
      IL_0018:  brfalse.s  IL_001c
      IL_001a:  br.s       IL_001e
      IL_001c:  br.s       IL_0076
      IL_001e:  nop
      IL_001f:  ldarg.0
      IL_0020:  ldfld      "int MyTask.<Run>d__0.<tests>5__1"
      IL_0025:  stloc.1
      IL_0026:  ldarg.0
      IL_0027:  ldloc.1
      IL_0028:  ldc.i4.1
      IL_0029:  add
      IL_002a:  stfld      "int MyTask.<Run>d__0.<tests>5__1"
      IL_002f:  ldarg.0
      IL_0030:  newobj     "MyTask..ctor()"
      IL_0035:  stfld      "MyTask MyTask.<Run>d__0.<myTask>5__2"
      IL_003a:  ldarg.0
      IL_003b:  ldfld      "MyTask MyTask.<Run>d__0.<myTask>5__2"
      IL_0040:  call       "MyTaskAwaiter Extension.GetAwaiter(MyTask)"
      IL_0045:  stloc.2
      IL_0046:  ldloc.2
      IL_0047:  callvirt   "bool MyTaskAwaiter.IsCompleted.get"
      IL_004c:  brtrue.s   IL_0092
      IL_004e:  ldarg.0
      IL_004f:  ldc.i4.0
      IL_0050:  dup
      IL_0051:  stloc.0
      IL_0052:  stfld      "int MyTask.<Run>d__0.<>1__state"
      IL_0057:  ldarg.0
      IL_0058:  ldloc.2
      IL_0059:  stfld      "object MyTask.<Run>d__0.<>u__1"
      IL_005e:  ldarg.0
      IL_005f:  stloc.s    V_4
      IL_0061:  ldarg.0
      IL_0062:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder MyTask.<Run>d__0.<>t__builder"
      IL_0067:  ldloca.s   V_2
      IL_0069:  ldloca.s   V_4
      IL_006b:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitOnCompleted<MyTaskAwaiter, MyTask.<Run>d__0>(ref MyTaskAwaiter, ref MyTask.<Run>d__0)"
      IL_0070:  nop
      IL_0071:  leave      IL_0138
      IL_0076:  ldarg.0
      IL_0077:  ldfld      "object MyTask.<Run>d__0.<>u__1"
      IL_007c:  castclass  "MyTaskAwaiter"
      IL_0081:  stloc.2
      IL_0082:  ldarg.0
      IL_0083:  ldnull
      IL_0084:  stfld      "object MyTask.<Run>d__0.<>u__1"
      IL_0089:  ldarg.0
      IL_008a:  ldc.i4.m1
      IL_008b:  dup
      IL_008c:  stloc.0
      IL_008d:  stfld      "int MyTask.<Run>d__0.<>1__state"
      IL_0092:  ldarg.0
      IL_0093:  ldloc.2
      IL_0094:  ldloca.s   V_3
      IL_0096:  callvirt   "int MyTaskAwaiter.GetResult(out System.Exception)"
      IL_009b:  stfld      "int MyTask.<Run>d__0.<>s__4"
      IL_00a0:  ldloc.3
      IL_00a1:  brfalse.s  IL_00b5
      IL_00a3:  ldarg.0
      IL_00a4:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder MyTask.<Run>d__0.<>t__builder"
      IL_00a9:  ldloc.3
      IL_00aa:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)"
      IL_00af:  nop
      IL_00b0:  leave      IL_0138
      IL_00b5:  ldarg.0
      IL_00b6:  ldarg.0
      IL_00b7:  ldfld      "int MyTask.<Run>d__0.<>s__4"
      IL_00bc:  stfld      "int MyTask.<Run>d__0.<x>5__3"
      IL_00c1:  ldarg.0
      IL_00c2:  ldfld      "int MyTask.<Run>d__0.<x>5__3"
      IL_00c7:  ldc.i4.s   123
      IL_00c9:  ceq
      IL_00cb:  stloc.s    V_5
      IL_00cd:  ldloc.s    V_5
      IL_00cf:  brfalse.s  IL_00dd
      IL_00d1:  ldsfld     "int Driver.Count"
      IL_00d6:  ldc.i4.1
      IL_00d7:  add
      IL_00d8:  stsfld     "int Driver.Count"
      IL_00dd:  nop
      IL_00de:  ldarg.0
      IL_00df:  ldnull
      IL_00e0:  stfld      "MyTask MyTask.<Run>d__0.<myTask>5__2"
      IL_00e5:  leave.s    IL_010a
    }
    finally
    {
      IL_00e7:  ldloc.0
      IL_00e8:  ldc.i4.0
      IL_00e9:  bge.s      IL_0109
      IL_00eb:  nop
      IL_00ec:  ldsfld     "int Driver.Count"
      IL_00f1:  ldarg.0
      IL_00f2:  ldfld      "int MyTask.<Run>d__0.<tests>5__1"
      IL_00f7:  sub
      IL_00f8:  stsfld     "int Driver.Result"
      IL_00fd:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
      IL_0102:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
      IL_0107:  pop
      IL_0108:  nop
      IL_0109:  endfinally
    }
    IL_010a:  leave.s    IL_0124
  }
  catch System.Exception
  {
    IL_010c:  stloc.3
    IL_010d:  ldarg.0
    IL_010e:  ldc.i4.s   -2
    IL_0110:  stfld      "int MyTask.<Run>d__0.<>1__state"
    IL_0115:  ldarg.0
    IL_0116:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder MyTask.<Run>d__0.<>t__builder"
    IL_011b:  ldloc.3
    IL_011c:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)"
    IL_0121:  nop
    IL_0122:  leave.s    IL_0138
  }
  IL_0124:  ldarg.0
  IL_0125:  ldc.i4.s   -2
  IL_0127:  stfld      "int MyTask.<Run>d__0.<>1__state"
  IL_012c:  ldarg.0
  IL_012d:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder MyTask.<Run>d__0.<>t__builder"
  IL_0132:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()"
  IL_0137:  nop
  IL_0138:  ret
}
""");
    }

    [Fact]
    public void MyTask_InsideTryWithCatch_WithGetResultExceptionMethod()
    {
        var source = @"
using System;
using System.Threading;

//Implementation of you own async pattern
public class MyTask
{
    public async void Run()
    {
        int tests = 0;

        try
        {
            tests++;
            var myTask = new MyTask();
            var x = await myTask;
            if (x == 123) Driver.Count++;
        }
        catch { }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}
public class MyTaskAwaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action continuationAction)
    {
    }

    public int GetResult(out Exception e)
    {
        e = null;
        return 123;
    }

    public bool IsCompleted { get { return true; } }
}

public static class Extension
{
    public static MyTaskAwaiter GetAwaiter(this MyTask my)
    {
        return new MyTaskAwaiter();
    }
}
//-------------------------------------

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        new MyTask().Run();

        CompletedSignal.WaitOne();

        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";

        var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe,
            references: new[] { Net451.System, Net451.SystemCore, Net451.MicrosoftCSharp });
        // Prefer GetResult() when inside the body of a try with catch clause
        compilation.VerifyDiagnostics(
            // (16,21): error CS7036: There is no argument given that corresponds to the required parameter 'e' of 'MyTaskAwaiter.GetResult(out Exception)'
            //             var x = await myTask;
            Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "await myTask").WithArguments("e", "MyTaskAwaiter.GetResult(out System.Exception)").WithLocation(16, 21)
            );
    }

    [Fact]
    public void MyTask_InsideTryWithCatch_WithBothGetResultMethods()
    {
        var source = @"
using System;
using System.Threading;

//Implementation of you own async pattern
public class MyTask
{
    public async void Run()
    {
        int tests = 0;

        try
        {
            tests++;
            var myTask = new MyTask();
            var x = await myTask;
            if (x == 123) Driver.Count++;
        }
        catch { }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}
public class MyTaskAwaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action continuationAction)
    {
    }

    public int GetResult(out Exception e)
        => throw null;

    public int GetResult()
    {
        return 123;
    }

    public bool IsCompleted { get { return true; } }
}

public static class Extension
{
    public static MyTaskAwaiter GetAwaiter(this MyTask my)
    {
        return new MyTaskAwaiter();
    }
}
//-------------------------------------

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        new MyTask().Run();

        CompletedSignal.WaitOne();

        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";

        var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe,
            references: new[] { Net451.System, Net451.SystemCore, Net451.MicrosoftCSharp });
        compilation.VerifyDiagnostics();

        var verifier = CompileAndVerify(compilation);
        // Prefer GetResult() when inside the body of a try with catch clause
        verifier.VerifyIL("MyTask.<Run>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      298 (0x12a)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                MyTaskAwaiter V_2,
                MyTask.<Run>d__0 V_3,
                bool V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int MyTask.<Run>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_0016
    IL_000e:  nop
    IL_000f:  ldarg.0
    IL_0010:  ldc.i4.0
    IL_0011:  stfld      "int MyTask.<Run>d__0.<tests>5__1"
    IL_0016:  nop
    .try
    {
      .try
      {
        IL_0017:  ldloc.0
        IL_0018:  brfalse.s  IL_001c
        IL_001a:  br.s       IL_001e
        IL_001c:  br.s       IL_0075
        IL_001e:  nop
        IL_001f:  ldarg.0
        IL_0020:  ldfld      "int MyTask.<Run>d__0.<tests>5__1"
        IL_0025:  stloc.1
        IL_0026:  ldarg.0
        IL_0027:  ldloc.1
        IL_0028:  ldc.i4.1
        IL_0029:  add
        IL_002a:  stfld      "int MyTask.<Run>d__0.<tests>5__1"
        IL_002f:  ldarg.0
        IL_0030:  newobj     "MyTask..ctor()"
        IL_0035:  stfld      "MyTask MyTask.<Run>d__0.<myTask>5__2"
        IL_003a:  ldarg.0
        IL_003b:  ldfld      "MyTask MyTask.<Run>d__0.<myTask>5__2"
        IL_0040:  call       "MyTaskAwaiter Extension.GetAwaiter(MyTask)"
        IL_0045:  stloc.2
        IL_0046:  ldloc.2
        IL_0047:  callvirt   "bool MyTaskAwaiter.IsCompleted.get"
        IL_004c:  brtrue.s   IL_0091
        IL_004e:  ldarg.0
        IL_004f:  ldc.i4.0
        IL_0050:  dup
        IL_0051:  stloc.0
        IL_0052:  stfld      "int MyTask.<Run>d__0.<>1__state"
        IL_0057:  ldarg.0
        IL_0058:  ldloc.2
        IL_0059:  stfld      "object MyTask.<Run>d__0.<>u__1"
        IL_005e:  ldarg.0
        IL_005f:  stloc.3
        IL_0060:  ldarg.0
        IL_0061:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder MyTask.<Run>d__0.<>t__builder"
        IL_0066:  ldloca.s   V_2
        IL_0068:  ldloca.s   V_3
        IL_006a:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitOnCompleted<MyTaskAwaiter, MyTask.<Run>d__0>(ref MyTaskAwaiter, ref MyTask.<Run>d__0)"
        IL_006f:  nop
        IL_0070:  leave      IL_0129
        IL_0075:  ldarg.0
        IL_0076:  ldfld      "object MyTask.<Run>d__0.<>u__1"
        IL_007b:  castclass  "MyTaskAwaiter"
        IL_0080:  stloc.2
        IL_0081:  ldarg.0
        IL_0082:  ldnull
        IL_0083:  stfld      "object MyTask.<Run>d__0.<>u__1"
        IL_0088:  ldarg.0
        IL_0089:  ldc.i4.m1
        IL_008a:  dup
        IL_008b:  stloc.0
        IL_008c:  stfld      "int MyTask.<Run>d__0.<>1__state"
        IL_0091:  ldarg.0
        IL_0092:  ldloc.2
        IL_0093:  callvirt   "int MyTaskAwaiter.GetResult()"
        IL_0098:  stfld      "int MyTask.<Run>d__0.<>s__4"
        IL_009d:  ldarg.0
        IL_009e:  ldarg.0
        IL_009f:  ldfld      "int MyTask.<Run>d__0.<>s__4"
        IL_00a4:  stfld      "int MyTask.<Run>d__0.<x>5__3"
        IL_00a9:  ldarg.0
        IL_00aa:  ldfld      "int MyTask.<Run>d__0.<x>5__3"
        IL_00af:  ldc.i4.s   123
        IL_00b1:  ceq
        IL_00b3:  stloc.s    V_4
        IL_00b5:  ldloc.s    V_4
        IL_00b7:  brfalse.s  IL_00c5
        IL_00b9:  ldsfld     "int Driver.Count"
        IL_00be:  ldc.i4.1
        IL_00bf:  add
        IL_00c0:  stsfld     "int Driver.Count"
        IL_00c5:  nop
        IL_00c6:  ldarg.0
        IL_00c7:  ldnull
        IL_00c8:  stfld      "MyTask MyTask.<Run>d__0.<myTask>5__2"
        IL_00cd:  leave.s    IL_00d4
      }
      catch object
      {
        IL_00cf:  pop
        IL_00d0:  nop
        IL_00d1:  nop
        IL_00d2:  leave.s    IL_00d4
      }
      IL_00d4:  leave.s    IL_00f9
    }
    finally
    {
      IL_00d6:  ldloc.0
      IL_00d7:  ldc.i4.0
      IL_00d8:  bge.s      IL_00f8
      IL_00da:  nop
      IL_00db:  ldsfld     "int Driver.Count"
      IL_00e0:  ldarg.0
      IL_00e1:  ldfld      "int MyTask.<Run>d__0.<tests>5__1"
      IL_00e6:  sub
      IL_00e7:  stsfld     "int Driver.Result"
      IL_00ec:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
      IL_00f1:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
      IL_00f6:  pop
      IL_00f7:  nop
      IL_00f8:  endfinally
    }
    IL_00f9:  leave.s    IL_0115
  }
  catch System.Exception
  {
    IL_00fb:  stloc.s    V_5
    IL_00fd:  ldarg.0
    IL_00fe:  ldc.i4.s   -2
    IL_0100:  stfld      "int MyTask.<Run>d__0.<>1__state"
    IL_0105:  ldarg.0
    IL_0106:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder MyTask.<Run>d__0.<>t__builder"
    IL_010b:  ldloc.s    V_5
    IL_010d:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)"
    IL_0112:  nop
    IL_0113:  leave.s    IL_0129
  }
  IL_0115:  ldarg.0
  IL_0116:  ldc.i4.s   -2
  IL_0118:  stfld      "int MyTask.<Run>d__0.<>1__state"
  IL_011d:  ldarg.0
  IL_011e:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder MyTask.<Run>d__0.<>t__builder"
  IL_0123:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()"
  IL_0128:  nop
  IL_0129:  ret
}
""");
    }

    [Fact]
    public void ExtensionGetResultExceptionMethod()
    {
        var source = @"
using System;
using System.Threading;

//Implementation of you own async pattern
public class MyTask
{
    public async void Run()
    {
        int tests = 0;

        try
        {
            tests++;
            var myTask = new MyTask();
            var x = await myTask;
            if (x == 123) Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}
public class MyTaskAwaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action continuationAction)
    {
    }

    public bool IsCompleted { get { return true; } }
}

public static class Extension
{
    public static MyTaskAwaiter GetAwaiter(this MyTask my)
    {
        return new MyTaskAwaiter();
    }

    public static int GetResult(this MyTaskAwaiter a, out Exception e)
    {
        e = null;
        return 123;
    }
}
//-------------------------------------

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        new MyTask().Run();

        CompletedSignal.WaitOne();

        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";

        var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe,
            references: new[] { Net451.System, Net451.SystemCore, Net451.MicrosoftCSharp });
        compilation.VerifyDiagnostics();

        var verifier = CompileAndVerify(compilation);
        verifier.VerifyIL("MyTask.<Run>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      313 (0x139)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                MyTaskAwaiter V_2,
                System.Exception V_3,
                MyTask.<Run>d__0 V_4,
                bool V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int MyTask.<Run>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_0016
    IL_000e:  nop
    IL_000f:  ldarg.0
    IL_0010:  ldc.i4.0
    IL_0011:  stfld      "int MyTask.<Run>d__0.<tests>5__1"
    IL_0016:  nop
    .try
    {
      IL_0017:  ldloc.0
      IL_0018:  brfalse.s  IL_001c
      IL_001a:  br.s       IL_001e
      IL_001c:  br.s       IL_0076
      IL_001e:  nop
      IL_001f:  ldarg.0
      IL_0020:  ldfld      "int MyTask.<Run>d__0.<tests>5__1"
      IL_0025:  stloc.1
      IL_0026:  ldarg.0
      IL_0027:  ldloc.1
      IL_0028:  ldc.i4.1
      IL_0029:  add
      IL_002a:  stfld      "int MyTask.<Run>d__0.<tests>5__1"
      IL_002f:  ldarg.0
      IL_0030:  newobj     "MyTask..ctor()"
      IL_0035:  stfld      "MyTask MyTask.<Run>d__0.<myTask>5__2"
      IL_003a:  ldarg.0
      IL_003b:  ldfld      "MyTask MyTask.<Run>d__0.<myTask>5__2"
      IL_0040:  call       "MyTaskAwaiter Extension.GetAwaiter(MyTask)"
      IL_0045:  stloc.2
      IL_0046:  ldloc.2
      IL_0047:  callvirt   "bool MyTaskAwaiter.IsCompleted.get"
      IL_004c:  brtrue.s   IL_0092
      IL_004e:  ldarg.0
      IL_004f:  ldc.i4.0
      IL_0050:  dup
      IL_0051:  stloc.0
      IL_0052:  stfld      "int MyTask.<Run>d__0.<>1__state"
      IL_0057:  ldarg.0
      IL_0058:  ldloc.2
      IL_0059:  stfld      "object MyTask.<Run>d__0.<>u__1"
      IL_005e:  ldarg.0
      IL_005f:  stloc.s    V_4
      IL_0061:  ldarg.0
      IL_0062:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder MyTask.<Run>d__0.<>t__builder"
      IL_0067:  ldloca.s   V_2
      IL_0069:  ldloca.s   V_4
      IL_006b:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitOnCompleted<MyTaskAwaiter, MyTask.<Run>d__0>(ref MyTaskAwaiter, ref MyTask.<Run>d__0)"
      IL_0070:  nop
      IL_0071:  leave      IL_0138
      IL_0076:  ldarg.0
      IL_0077:  ldfld      "object MyTask.<Run>d__0.<>u__1"
      IL_007c:  castclass  "MyTaskAwaiter"
      IL_0081:  stloc.2
      IL_0082:  ldarg.0
      IL_0083:  ldnull
      IL_0084:  stfld      "object MyTask.<Run>d__0.<>u__1"
      IL_0089:  ldarg.0
      IL_008a:  ldc.i4.m1
      IL_008b:  dup
      IL_008c:  stloc.0
      IL_008d:  stfld      "int MyTask.<Run>d__0.<>1__state"
      IL_0092:  ldarg.0
      IL_0093:  ldloc.2
      IL_0094:  ldloca.s   V_3
      IL_0096:  call       "int Extension.GetResult(MyTaskAwaiter, out System.Exception)"
      IL_009b:  stfld      "int MyTask.<Run>d__0.<>s__4"
      IL_00a0:  ldloc.3
      IL_00a1:  brfalse.s  IL_00b5
      IL_00a3:  ldarg.0
      IL_00a4:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder MyTask.<Run>d__0.<>t__builder"
      IL_00a9:  ldloc.3
      IL_00aa:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)"
      IL_00af:  nop
      IL_00b0:  leave      IL_0138
      IL_00b5:  ldarg.0
      IL_00b6:  ldarg.0
      IL_00b7:  ldfld      "int MyTask.<Run>d__0.<>s__4"
      IL_00bc:  stfld      "int MyTask.<Run>d__0.<x>5__3"
      IL_00c1:  ldarg.0
      IL_00c2:  ldfld      "int MyTask.<Run>d__0.<x>5__3"
      IL_00c7:  ldc.i4.s   123
      IL_00c9:  ceq
      IL_00cb:  stloc.s    V_5
      IL_00cd:  ldloc.s    V_5
      IL_00cf:  brfalse.s  IL_00dd
      IL_00d1:  ldsfld     "int Driver.Count"
      IL_00d6:  ldc.i4.1
      IL_00d7:  add
      IL_00d8:  stsfld     "int Driver.Count"
      IL_00dd:  nop
      IL_00de:  ldarg.0
      IL_00df:  ldnull
      IL_00e0:  stfld      "MyTask MyTask.<Run>d__0.<myTask>5__2"
      IL_00e5:  leave.s    IL_010a
    }
    finally
    {
      IL_00e7:  ldloc.0
      IL_00e8:  ldc.i4.0
      IL_00e9:  bge.s      IL_0109
      IL_00eb:  nop
      IL_00ec:  ldsfld     "int Driver.Count"
      IL_00f1:  ldarg.0
      IL_00f2:  ldfld      "int MyTask.<Run>d__0.<tests>5__1"
      IL_00f7:  sub
      IL_00f8:  stsfld     "int Driver.Result"
      IL_00fd:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
      IL_0102:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
      IL_0107:  pop
      IL_0108:  nop
      IL_0109:  endfinally
    }
    IL_010a:  leave.s    IL_0124
  }
  catch System.Exception
  {
    IL_010c:  stloc.3
    IL_010d:  ldarg.0
    IL_010e:  ldc.i4.s   -2
    IL_0110:  stfld      "int MyTask.<Run>d__0.<>1__state"
    IL_0115:  ldarg.0
    IL_0116:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder MyTask.<Run>d__0.<>t__builder"
    IL_011b:  ldloc.3
    IL_011c:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)"
    IL_0121:  nop
    IL_0122:  leave.s    IL_0138
  }
  IL_0124:  ldarg.0
  IL_0125:  ldc.i4.s   -2
  IL_0127:  stfld      "int MyTask.<Run>d__0.<>1__state"
  IL_012c:  ldarg.0
  IL_012d:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder MyTask.<Run>d__0.<>t__builder"
  IL_0132:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()"
  IL_0137:  nop
  IL_0138:  ret
}
""");
    }

    [Fact]
    public void ExtensionGetResultExceptionMethod2()
    {
        var source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class C
{
    public static async Task<int> Main()
    {
        await Task.Delay(1000);
        return 0;
    }
}
static class Extension
{
    public static T GetResult<T>(this TaskAwaiter<T> task, out Exception exception)
    {
        exception = null;
        return task.GetResult();
    }
    public static void GetResult(this TaskAwaiter task, out Exception exception)
    {
        exception = null;
        task.GetResult();
    }
}";
        var compilation = CreateCompilation(source, targetFramework: Roslyn.Test.Utilities.TargetFramework.Net70, options: TestOptions.DebugExe);
        compilation.VerifyDiagnostics();

        var verifier = CompileAndVerify(compilation, verify: CodeAnalysis.Test.Utilities.Verification.Skipped);
        verifier.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      182 (0xb6)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                System.Exception V_3,
                C.<Main>d__0 V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<Main>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_004d
    IL_000e:  nop
    IL_000f:  ldc.i4     0x3e8
    IL_0014:  call       "System.Threading.Tasks.Task System.Threading.Tasks.Task.Delay(int)"
    IL_0019:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()"
    IL_001e:  stloc.2
    IL_001f:  ldloca.s   V_2
    IL_0021:  call       "bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get"
    IL_0026:  brtrue.s   IL_0069
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      "int C.<Main>d__0.<>1__state"
    IL_0031:  ldarg.0
    IL_0032:  ldloc.2
    IL_0033:  stfld      "System.Runtime.CompilerServices.TaskAwaiter C.<Main>d__0.<>u__1"
    IL_0038:  ldarg.0
    IL_0039:  stloc.s    V_4
    IL_003b:  ldarg.0
    IL_003c:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<Main>d__0.<>t__builder"
    IL_0041:  ldloca.s   V_2
    IL_0043:  ldloca.s   V_4
    IL_0045:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<Main>d__0)"
    IL_004a:  nop
    IL_004b:  leave.s    IL_00b5
    IL_004d:  ldarg.0
    IL_004e:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter C.<Main>d__0.<>u__1"
    IL_0053:  stloc.2
    IL_0054:  ldarg.0
    IL_0055:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter C.<Main>d__0.<>u__1"
    IL_005a:  initobj    "System.Runtime.CompilerServices.TaskAwaiter"
    IL_0060:  ldarg.0
    IL_0061:  ldc.i4.m1
    IL_0062:  dup
    IL_0063:  stloc.0
    IL_0064:  stfld      "int C.<Main>d__0.<>1__state"
    IL_0069:  ldloc.2
    IL_006a:  ldloca.s   V_3
    IL_006c:  call       "void Extension.GetResult(System.Runtime.CompilerServices.TaskAwaiter, out System.Exception)"
    IL_0071:  nop
    IL_0072:  ldloc.3
    IL_0073:  brfalse.s  IL_0084
    IL_0075:  ldarg.0
    IL_0076:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<Main>d__0.<>t__builder"
    IL_007b:  ldloc.3
    IL_007c:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)"
    IL_0081:  nop
    IL_0082:  leave.s    IL_00b5
    IL_0084:  ldc.i4.0
    IL_0085:  stloc.1
    IL_0086:  leave.s    IL_00a0
  }
  catch System.Exception
  {
    IL_0088:  stloc.3
    IL_0089:  ldarg.0
    IL_008a:  ldc.i4.s   -2
    IL_008c:  stfld      "int C.<Main>d__0.<>1__state"
    IL_0091:  ldarg.0
    IL_0092:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<Main>d__0.<>t__builder"
    IL_0097:  ldloc.3
    IL_0098:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)"
    IL_009d:  nop
    IL_009e:  leave.s    IL_00b5
  }
  IL_00a0:  ldarg.0
  IL_00a1:  ldc.i4.s   -2
  IL_00a3:  stfld      "int C.<Main>d__0.<>1__state"
  IL_00a8:  ldarg.0
  IL_00a9:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<Main>d__0.<>t__builder"
  IL_00ae:  ldloc.1
  IL_00af:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)"
  IL_00b4:  nop
  IL_00b5:  ret
}
""");

        verifier.VerifyIL("C.<Main>()", """
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (System.Runtime.CompilerServices.TaskAwaiter<int> V_0)
  IL_0000:  call       "System.Threading.Tasks.Task<int> C.Main()"
  IL_0005:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
  IL_000a:  stloc.0
  IL_000b:  ldloca.s   V_0
  IL_000d:  call       "int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()"
  IL_0012:  ret
}
""");
    }

    [Fact]
    public void ExtensionGetResultExceptionMethod3()
    {
        var source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class C
{
    public static async Task<int> M()
    {
        await Task.Delay(1000);
        return 0;
    }
}
static class Extension
{
    public static T GetResult<T>(this TaskAwaiter<T> task, out Exception exception)
    {
        exception = null;
        return task.GetResult();
    }
    public static void GetResult(this TaskAwaiter task, out Exception exception)
    {
        exception = null;
        task.GetResult();
    }
}";
        var compilation = CreateCompilation(source, targetFramework: Roslyn.Test.Utilities.TargetFramework.Net70);
        compilation.VerifyDiagnostics();

        var verifier = CompileAndVerify(compilation, verify: CodeAnalysis.Test.Utilities.Verification.Skipped);
        verifier.VerifyIL("C.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      168 (0xa8)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<M>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0043
    IL_000a:  ldc.i4     0x3e8
    IL_000f:  call       "System.Threading.Tasks.Task System.Threading.Tasks.Task.Delay(int)"
    IL_0014:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()"
    IL_0019:  stloc.2
    IL_001a:  ldloca.s   V_2
    IL_001c:  call       "bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get"
    IL_0021:  brtrue.s   IL_005f
    IL_0023:  ldarg.0
    IL_0024:  ldc.i4.0
    IL_0025:  dup
    IL_0026:  stloc.0
    IL_0027:  stfld      "int C.<M>d__0.<>1__state"
    IL_002c:  ldarg.0
    IL_002d:  ldloc.2
    IL_002e:  stfld      "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1"
    IL_0033:  ldarg.0
    IL_0034:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<M>d__0.<>t__builder"
    IL_0039:  ldloca.s   V_2
    IL_003b:  ldarg.0
    IL_003c:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<M>d__0)"
    IL_0041:  leave.s    IL_00a7
    IL_0043:  ldarg.0
    IL_0044:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1"
    IL_0049:  stloc.2
    IL_004a:  ldarg.0
    IL_004b:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__0.<>u__1"
    IL_0050:  initobj    "System.Runtime.CompilerServices.TaskAwaiter"
    IL_0056:  ldarg.0
    IL_0057:  ldc.i4.m1
    IL_0058:  dup
    IL_0059:  stloc.0
    IL_005a:  stfld      "int C.<M>d__0.<>1__state"
    IL_005f:  ldloc.2
    IL_0060:  ldloca.s   V_3
    IL_0062:  call       "void Extension.GetResult(System.Runtime.CompilerServices.TaskAwaiter, out System.Exception)"
    IL_0067:  ldloc.3
    IL_0068:  brfalse.s  IL_0078
    IL_006a:  ldarg.0
    IL_006b:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<M>d__0.<>t__builder"
    IL_0070:  ldloc.3
    IL_0071:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)"
    IL_0076:  leave.s    IL_00a7
    IL_0078:  ldc.i4.0
    IL_0079:  stloc.1
    IL_007a:  leave.s    IL_0093
  }
  catch System.Exception
  {
    IL_007c:  stloc.3
    IL_007d:  ldarg.0
    IL_007e:  ldc.i4.s   -2
    IL_0080:  stfld      "int C.<M>d__0.<>1__state"
    IL_0085:  ldarg.0
    IL_0086:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<M>d__0.<>t__builder"
    IL_008b:  ldloc.3
    IL_008c:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)"
    IL_0091:  leave.s    IL_00a7
  }
  IL_0093:  ldarg.0
  IL_0094:  ldc.i4.s   -2
  IL_0096:  stfld      "int C.<M>d__0.<>1__state"
  IL_009b:  ldarg.0
  IL_009c:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<M>d__0.<>t__builder"
  IL_00a1:  ldloc.1
  IL_00a2:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)"
  IL_00a7:  ret
}
""");
    }

    [Fact]
    public void MyTask_InAwaitUsing()
    {
        var source = @"
using System;

public class MyTask
{
    public async void Run()
    {
        await using (new MyDisposable())
        {
        }
    }

    public MyTaskAwaiter GetAwaiter()
        => throw null;
}
public class MyDisposable
{
    public MyTask DisposeAsync() => throw null;
}
public class MyTaskAwaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action continuationAction)
    {
    }

    public int GetResult(out Exception e)
        => throw null;

    public bool IsCompleted { get { return true; } }
}
";

        var compilation = CreateCompilationWithMscorlib45(source,
            references: new[] { Net451.System, Net451.SystemCore, Net451.MicrosoftCSharp });
        compilation.VerifyDiagnostics();

        var verifier = CompileAndVerify(compilation);
        verifier.VerifyIL("MyTask.<Run>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      241 (0xf1)
  .maxstack  3
  .locals init (int V_0,
                MyDisposable V_1,
                object V_2,
                MyTaskAwaiter V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int MyTask.<Run>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0064
    IL_000a:  newobj     "MyDisposable..ctor()"
    IL_000f:  stloc.1
    IL_0010:  ldarg.0
    IL_0011:  ldnull
    IL_0012:  stfld      "object MyTask.<Run>d__0.<>7__wrap1"
    IL_0017:  ldarg.0
    IL_0018:  ldc.i4.0
    IL_0019:  stfld      "int MyTask.<Run>d__0.<>7__wrap2"
    .try
    {
      IL_001e:  leave.s    IL_002a
    }
    catch object
    {
      IL_0020:  stloc.2
      IL_0021:  ldarg.0
      IL_0022:  ldloc.2
      IL_0023:  stfld      "object MyTask.<Run>d__0.<>7__wrap1"
      IL_0028:  leave.s    IL_002a
    }
    IL_002a:  ldloc.1
    IL_002b:  brfalse.s  IL_009c
    IL_002d:  ldloc.1
    IL_002e:  callvirt   "MyTask MyDisposable.DisposeAsync()"
    IL_0033:  callvirt   "MyTaskAwaiter MyTask.GetAwaiter()"
    IL_0038:  stloc.3
    IL_0039:  ldloc.3
    IL_003a:  callvirt   "bool MyTaskAwaiter.IsCompleted.get"
    IL_003f:  brtrue.s   IL_0080
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.0
    IL_0043:  dup
    IL_0044:  stloc.0
    IL_0045:  stfld      "int MyTask.<Run>d__0.<>1__state"
    IL_004a:  ldarg.0
    IL_004b:  ldloc.3
    IL_004c:  stfld      "object MyTask.<Run>d__0.<>u__1"
    IL_0051:  ldarg.0
    IL_0052:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder MyTask.<Run>d__0.<>t__builder"
    IL_0057:  ldloca.s   V_3
    IL_0059:  ldarg.0
    IL_005a:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitOnCompleted<MyTaskAwaiter, MyTask.<Run>d__0>(ref MyTaskAwaiter, ref MyTask.<Run>d__0)"
    IL_005f:  leave      IL_00f0
    IL_0064:  ldarg.0
    IL_0065:  ldfld      "object MyTask.<Run>d__0.<>u__1"
    IL_006a:  castclass  "MyTaskAwaiter"
    IL_006f:  stloc.3
    IL_0070:  ldarg.0
    IL_0071:  ldnull
    IL_0072:  stfld      "object MyTask.<Run>d__0.<>u__1"
    IL_0077:  ldarg.0
    IL_0078:  ldc.i4.m1
    IL_0079:  dup
    IL_007a:  stloc.0
    IL_007b:  stfld      "int MyTask.<Run>d__0.<>1__state"
    IL_0080:  ldloc.3
    IL_0081:  ldloca.s   V_4
    IL_0083:  callvirt   "int MyTaskAwaiter.GetResult(out System.Exception)"
    IL_0088:  pop
    IL_0089:  ldloc.s    V_4
    IL_008b:  brfalse.s  IL_009c
    IL_008d:  ldarg.0
    IL_008e:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder MyTask.<Run>d__0.<>t__builder"
    IL_0093:  ldloc.s    V_4
    IL_0095:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)"
    IL_009a:  leave.s    IL_00f0
    IL_009c:  ldarg.0
    IL_009d:  ldfld      "object MyTask.<Run>d__0.<>7__wrap1"
    IL_00a2:  stloc.2
    IL_00a3:  ldloc.2
    IL_00a4:  brfalse.s  IL_00bb
    IL_00a6:  ldloc.2
    IL_00a7:  isinst     "System.Exception"
    IL_00ac:  dup
    IL_00ad:  brtrue.s   IL_00b1
    IL_00af:  ldloc.2
    IL_00b0:  throw
    IL_00b1:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
    IL_00b6:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
    IL_00bb:  ldarg.0
    IL_00bc:  ldnull
    IL_00bd:  stfld      "object MyTask.<Run>d__0.<>7__wrap1"
    IL_00c2:  leave.s    IL_00dd
  }
  catch System.Exception
  {
    IL_00c4:  stloc.s    V_4
    IL_00c6:  ldarg.0
    IL_00c7:  ldc.i4.s   -2
    IL_00c9:  stfld      "int MyTask.<Run>d__0.<>1__state"
    IL_00ce:  ldarg.0
    IL_00cf:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder MyTask.<Run>d__0.<>t__builder"
    IL_00d4:  ldloc.s    V_4
    IL_00d6:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)"
    IL_00db:  leave.s    IL_00f0
  }
  IL_00dd:  ldarg.0
  IL_00de:  ldc.i4.s   -2
  IL_00e0:  stfld      "int MyTask.<Run>d__0.<>1__state"
  IL_00e5:  ldarg.0
  IL_00e6:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder MyTask.<Run>d__0.<>t__builder"
  IL_00eb:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()"
  IL_00f0:  ret
}
""");
    }

    [Fact]
    public void MyTask_InAwaitForeach()
    {
        var source = @"
using System;
public class C
{
    public async void Run()
    {
        await foreach (var i in new MyEnumerable())
        {
        }
    }
}

public class MyTask
{
    public MyTaskAwaiter GetAwaiter() => throw null;
}
public class MyTask<T>
{
    public MyTaskAwaiter<T> GetAwaiter() => throw null;
}
public class MyEnumerable
{
    public MyEnumerator GetAsyncEnumerator() => throw null;
}
public class MyEnumerator
{
    public MyTask<bool> MoveNextAsync() => throw null;
    public int Current => throw null;
    public MyTask DisposeAsync() => throw null;
}
public class MyDisposable
{
    public MyTask DisposeAsync() => throw null;
}
public class MyTaskAwaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action continuationAction) { }
    public int GetResult(out Exception e) => throw null;
    public bool IsCompleted { get { return true; } }
}
public class MyTaskAwaiter<T> : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action continuationAction) { }
    public T GetResult(out Exception e) => throw null;
    public bool IsCompleted { get { return true; } }
}
";

        var compilation = CreateCompilationWithMscorlib45(source,
            references: new[] { Net451.System, Net451.SystemCore, Net451.MicrosoftCSharp });
        compilation.VerifyDiagnostics();

        var verifier = CompileAndVerify(compilation);
        verifier.VerifyIL("C.<Run>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      423 (0x1a7)
  .maxstack  3
  .locals init (int V_0,
                bool V_1,
                MyTaskAwaiter<bool> V_2,
                System.Exception V_3,
                object V_4,
                MyTaskAwaiter V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<Run>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_002f
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_0111
    IL_0011:  ldarg.0
    IL_0012:  newobj     "MyEnumerable..ctor()"
    IL_0017:  call       "MyEnumerator MyEnumerable.GetAsyncEnumerator()"
    IL_001c:  stfld      "MyEnumerator C.<Run>d__0.<>7__wrap1"
    IL_0021:  ldarg.0
    IL_0022:  ldnull
    IL_0023:  stfld      "object C.<Run>d__0.<>7__wrap2"
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  stfld      "int C.<Run>d__0.<>7__wrap3"
    IL_002f:  nop
    .try
    {
      IL_0030:  ldloc.0
      IL_0031:  brfalse.s  IL_007d
      IL_0033:  br.s       IL_0041
      IL_0035:  ldarg.0
      IL_0036:  ldfld      "MyEnumerator C.<Run>d__0.<>7__wrap1"
      IL_003b:  callvirt   "int MyEnumerator.Current.get"
      IL_0040:  pop
      IL_0041:  ldarg.0
      IL_0042:  ldfld      "MyEnumerator C.<Run>d__0.<>7__wrap1"
      IL_0047:  callvirt   "MyTask<bool> MyEnumerator.MoveNextAsync()"
      IL_004c:  callvirt   "MyTaskAwaiter<bool> MyTask<bool>.GetAwaiter()"
      IL_0051:  stloc.2
      IL_0052:  ldloc.2
      IL_0053:  callvirt   "bool MyTaskAwaiter<bool>.IsCompleted.get"
      IL_0058:  brtrue.s   IL_0099
      IL_005a:  ldarg.0
      IL_005b:  ldc.i4.0
      IL_005c:  dup
      IL_005d:  stloc.0
      IL_005e:  stfld      "int C.<Run>d__0.<>1__state"
      IL_0063:  ldarg.0
      IL_0064:  ldloc.2
      IL_0065:  stfld      "object C.<Run>d__0.<>u__1"
      IL_006a:  ldarg.0
      IL_006b:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder C.<Run>d__0.<>t__builder"
      IL_0070:  ldloca.s   V_2
      IL_0072:  ldarg.0
      IL_0073:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitOnCompleted<MyTaskAwaiter<bool>, C.<Run>d__0>(ref MyTaskAwaiter<bool>, ref C.<Run>d__0)"
      IL_0078:  leave      IL_01a6
      IL_007d:  ldarg.0
      IL_007e:  ldfld      "object C.<Run>d__0.<>u__1"
      IL_0083:  castclass  "MyTaskAwaiter<bool>"
      IL_0088:  stloc.2
      IL_0089:  ldarg.0
      IL_008a:  ldnull
      IL_008b:  stfld      "object C.<Run>d__0.<>u__1"
      IL_0090:  ldarg.0
      IL_0091:  ldc.i4.m1
      IL_0092:  dup
      IL_0093:  stloc.0
      IL_0094:  stfld      "int C.<Run>d__0.<>1__state"
      IL_0099:  ldloc.2
      IL_009a:  ldloca.s   V_3
      IL_009c:  callvirt   "bool MyTaskAwaiter<bool>.GetResult(out System.Exception)"
      IL_00a1:  stloc.1
      IL_00a2:  ldloc.3
      IL_00a3:  brfalse.s  IL_00b6
      IL_00a5:  ldarg.0
      IL_00a6:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder C.<Run>d__0.<>t__builder"
      IL_00ab:  ldloc.3
      IL_00ac:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)"
      IL_00b1:  leave      IL_01a6
      IL_00b6:  ldloc.1
      IL_00b7:  brtrue     IL_0035
      IL_00bc:  leave.s    IL_00ca
    }
    catch object
    {
      IL_00be:  stloc.s    V_4
      IL_00c0:  ldarg.0
      IL_00c1:  ldloc.s    V_4
      IL_00c3:  stfld      "object C.<Run>d__0.<>7__wrap2"
      IL_00c8:  leave.s    IL_00ca
    }
    IL_00ca:  ldarg.0
    IL_00cb:  ldfld      "MyEnumerator C.<Run>d__0.<>7__wrap1"
    IL_00d0:  brfalse.s  IL_0149
    IL_00d2:  ldarg.0
    IL_00d3:  ldfld      "MyEnumerator C.<Run>d__0.<>7__wrap1"
    IL_00d8:  callvirt   "MyTask MyEnumerator.DisposeAsync()"
    IL_00dd:  callvirt   "MyTaskAwaiter MyTask.GetAwaiter()"
    IL_00e2:  stloc.s    V_5
    IL_00e4:  ldloc.s    V_5
    IL_00e6:  callvirt   "bool MyTaskAwaiter.IsCompleted.get"
    IL_00eb:  brtrue.s   IL_012e
    IL_00ed:  ldarg.0
    IL_00ee:  ldc.i4.1
    IL_00ef:  dup
    IL_00f0:  stloc.0
    IL_00f1:  stfld      "int C.<Run>d__0.<>1__state"
    IL_00f6:  ldarg.0
    IL_00f7:  ldloc.s    V_5
    IL_00f9:  stfld      "object C.<Run>d__0.<>u__1"
    IL_00fe:  ldarg.0
    IL_00ff:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder C.<Run>d__0.<>t__builder"
    IL_0104:  ldloca.s   V_5
    IL_0106:  ldarg.0
    IL_0107:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitOnCompleted<MyTaskAwaiter, C.<Run>d__0>(ref MyTaskAwaiter, ref C.<Run>d__0)"
    IL_010c:  leave      IL_01a6
    IL_0111:  ldarg.0
    IL_0112:  ldfld      "object C.<Run>d__0.<>u__1"
    IL_0117:  castclass  "MyTaskAwaiter"
    IL_011c:  stloc.s    V_5
    IL_011e:  ldarg.0
    IL_011f:  ldnull
    IL_0120:  stfld      "object C.<Run>d__0.<>u__1"
    IL_0125:  ldarg.0
    IL_0126:  ldc.i4.m1
    IL_0127:  dup
    IL_0128:  stloc.0
    IL_0129:  stfld      "int C.<Run>d__0.<>1__state"
    IL_012e:  ldloc.s    V_5
    IL_0130:  ldloca.s   V_3
    IL_0132:  callvirt   "int MyTaskAwaiter.GetResult(out System.Exception)"
    IL_0137:  pop
    IL_0138:  ldloc.3
    IL_0139:  brfalse.s  IL_0149
    IL_013b:  ldarg.0
    IL_013c:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder C.<Run>d__0.<>t__builder"
    IL_0141:  ldloc.3
    IL_0142:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)"
    IL_0147:  leave.s    IL_01a6
    IL_0149:  ldarg.0
    IL_014a:  ldfld      "object C.<Run>d__0.<>7__wrap2"
    IL_014f:  stloc.s    V_4
    IL_0151:  ldloc.s    V_4
    IL_0153:  brfalse.s  IL_016c
    IL_0155:  ldloc.s    V_4
    IL_0157:  isinst     "System.Exception"
    IL_015c:  dup
    IL_015d:  brtrue.s   IL_0162
    IL_015f:  ldloc.s    V_4
    IL_0161:  throw
    IL_0162:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
    IL_0167:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
    IL_016c:  ldarg.0
    IL_016d:  ldnull
    IL_016e:  stfld      "object C.<Run>d__0.<>7__wrap2"
    IL_0173:  ldarg.0
    IL_0174:  ldnull
    IL_0175:  stfld      "MyEnumerator C.<Run>d__0.<>7__wrap1"
    IL_017a:  leave.s    IL_0193
  }
  catch System.Exception
  {
    IL_017c:  stloc.3
    IL_017d:  ldarg.0
    IL_017e:  ldc.i4.s   -2
    IL_0180:  stfld      "int C.<Run>d__0.<>1__state"
    IL_0185:  ldarg.0
    IL_0186:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder C.<Run>d__0.<>t__builder"
    IL_018b:  ldloc.3
    IL_018c:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)"
    IL_0191:  leave.s    IL_01a6
  }
  IL_0193:  ldarg.0
  IL_0194:  ldc.i4.s   -2
  IL_0196:  stfld      "int C.<Run>d__0.<>1__state"
  IL_019b:  ldarg.0
  IL_019c:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder C.<Run>d__0.<>t__builder"
  IL_01a1:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()"
  IL_01a6:  ret
}
""");
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void Execution_Returning_TaskOfInt()
    {
        var source = """
using System.Threading.Tasks;

System.Console.Write(await m());

static async Task<int> m()
{
    return await m2();
}

static async Task<int> m2()
{
    await Task.Yield();
    return 42;
}
""";

        var comp = CreateCompilation(new[] { source, ExtensionsSource },
            targetFramework: TargetFramework.Net70);

        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "42", verify: Verification.Skipped);
        verifier.VerifyIL("Program.<<<Main>$>g__m|0_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      168 (0xa8)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_003e
    IL_000a:  call       "System.Threading.Tasks.Task<int> Program.<<Main>$>g__m2|0_1()"
    IL_000f:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
    IL_0014:  stloc.3
    IL_0015:  ldloca.s   V_3
    IL_0017:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get"
    IL_001c:  brtrue.s   IL_005a
    IL_001e:  ldarg.0
    IL_001f:  ldc.i4.0
    IL_0020:  dup
    IL_0021:  stloc.0
    IL_0022:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
    IL_0027:  ldarg.0
    IL_0028:  ldloc.3
    IL_0029:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Program.<<<Main>$>g__m|0_0>d.<>u__1"
    IL_002e:  ldarg.0
    IL_002f:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<<<Main>$>g__m|0_0>d.<>t__builder"
    IL_0034:  ldloca.s   V_3
    IL_0036:  ldarg.0
    IL_0037:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<<<Main>$>g__m|0_0>d>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<<<Main>$>g__m|0_0>d)"
    IL_003c:  leave.s    IL_00a7
    IL_003e:  ldarg.0
    IL_003f:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Program.<<<Main>$>g__m|0_0>d.<>u__1"
    IL_0044:  stloc.3
    IL_0045:  ldarg.0
    IL_0046:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<int> Program.<<<Main>$>g__m|0_0>d.<>u__1"
    IL_004b:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<int>"
    IL_0051:  ldarg.0
    IL_0052:  ldc.i4.m1
    IL_0053:  dup
    IL_0054:  stloc.0
    IL_0055:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
    IL_005a:  ldloc.3
    IL_005b:  ldloca.s   V_4
    IL_005d:  call       "int TaskAwaiterExceptionExtensions.GetResult<int>(System.Runtime.CompilerServices.TaskAwaiter<int>, out System.Exception)"
    IL_0062:  stloc.2
    IL_0063:  ldloc.s    V_4
    IL_0065:  brfalse.s  IL_0076
    IL_0067:  ldarg.0
    IL_0068:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<<<Main>$>g__m|0_0>d.<>t__builder"
    IL_006d:  ldloc.s    V_4
    IL_006f:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)"
    IL_0074:  leave.s    IL_00a7
    IL_0076:  ldloc.2
    IL_0077:  stloc.1
    IL_0078:  leave.s    IL_0093
  }
  catch System.Exception
  {
    IL_007a:  stloc.s    V_4
    IL_007c:  ldarg.0
    IL_007d:  ldc.i4.s   -2
    IL_007f:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
    IL_0084:  ldarg.0
    IL_0085:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<<<Main>$>g__m|0_0>d.<>t__builder"
    IL_008a:  ldloc.s    V_4
    IL_008c:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)"
    IL_0091:  leave.s    IL_00a7
  }
  IL_0093:  ldarg.0
  IL_0094:  ldc.i4.s   -2
  IL_0096:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
  IL_009b:  ldarg.0
  IL_009c:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<<<Main>$>g__m|0_0>d.<>t__builder"
  IL_00a1:  ldloc.1
  IL_00a2:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)"
  IL_00a7:  ret
}
""");
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void Execution_Returning_Task()
    {
        var source = """
using System.Threading.Tasks;

await m();

static async Task m()
{
    await m2();
}

static async Task m2()
{
    await Task.Yield();
    System.Console.Write(42);
}
""";

        var comp = CreateCompilation(new[] { source, ExtensionsSource },
            targetFramework: TargetFramework.Net70);

        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "42", verify: Verification.Skipped);
        verifier.VerifyIL("Program.<<<Main>$>g__m|0_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      160 (0xa0)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_003e
    IL_000a:  call       "System.Threading.Tasks.Task Program.<<Main>$>g__m2|0_1()"
    IL_000f:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()"
    IL_0014:  stloc.1
    IL_0015:  ldloca.s   V_1
    IL_0017:  call       "bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get"
    IL_001c:  brtrue.s   IL_005a
    IL_001e:  ldarg.0
    IL_001f:  ldc.i4.0
    IL_0020:  dup
    IL_0021:  stloc.0
    IL_0022:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
    IL_0027:  ldarg.0
    IL_0028:  ldloc.1
    IL_0029:  stfld      "System.Runtime.CompilerServices.TaskAwaiter Program.<<<Main>$>g__m|0_0>d.<>u__1"
    IL_002e:  ldarg.0
    IL_002f:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<<Main>$>g__m|0_0>d.<>t__builder"
    IL_0034:  ldloca.s   V_1
    IL_0036:  ldarg.0
    IL_0037:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, Program.<<<Main>$>g__m|0_0>d>(ref System.Runtime.CompilerServices.TaskAwaiter, ref Program.<<<Main>$>g__m|0_0>d)"
    IL_003c:  leave.s    IL_009f
    IL_003e:  ldarg.0
    IL_003f:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter Program.<<<Main>$>g__m|0_0>d.<>u__1"
    IL_0044:  stloc.1
    IL_0045:  ldarg.0
    IL_0046:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter Program.<<<Main>$>g__m|0_0>d.<>u__1"
    IL_004b:  initobj    "System.Runtime.CompilerServices.TaskAwaiter"
    IL_0051:  ldarg.0
    IL_0052:  ldc.i4.m1
    IL_0053:  dup
    IL_0054:  stloc.0
    IL_0055:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
    IL_005a:  ldloc.1
    IL_005b:  ldloca.s   V_2
    IL_005d:  call       "void TaskAwaiterExceptionExtensions.GetResult(System.Runtime.CompilerServices.TaskAwaiter, out System.Exception)"
    IL_0062:  ldloc.2
    IL_0063:  brfalse.s  IL_0073
    IL_0065:  ldarg.0
    IL_0066:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<<Main>$>g__m|0_0>d.<>t__builder"
    IL_006b:  ldloc.2
    IL_006c:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0071:  leave.s    IL_009f
    IL_0073:  leave.s    IL_008c
  }
  catch System.Exception
  {
    IL_0075:  stloc.2
    IL_0076:  ldarg.0
    IL_0077:  ldc.i4.s   -2
    IL_0079:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
    IL_007e:  ldarg.0
    IL_007f:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<<Main>$>g__m|0_0>d.<>t__builder"
    IL_0084:  ldloc.2
    IL_0085:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_008a:  leave.s    IL_009f
  }
  IL_008c:  ldarg.0
  IL_008d:  ldc.i4.s   -2
  IL_008f:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
  IL_0094:  ldarg.0
  IL_0095:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<<Main>$>g__m|0_0>d.<>t__builder"
  IL_009a:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_009f:  ret
}
""");
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void Execution_Throwing_TaskOfInt()
    {
        var source = """
using System.Threading.Tasks;

try
{
    await m();
}
catch (System.Exception e)
{
    System.Console.Write(e.Message);
}

static async Task<int> m()
{
    return await m2();
}

static async Task<int> m2()
{
    await Task.Yield();
    throw new System.Exception("hello");
}
""";

        var comp = CreateCompilation(new[] { source, ExtensionsSource },
            targetFramework: TargetFramework.Net70);

        comp.VerifyDiagnostics();
        CompileAndVerify(comp, expectedOutput: "hello", verify: Verification.Skipped);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void Execution_Throwing_Task()
    {
        var source = """
using System.Threading.Tasks;

try
{
    await m();
}
catch (System.Exception e)
{
    System.Console.Write(e.Message);
}

static async Task m()
{
    await m2();
}

static async Task m2()
{
    await Task.Yield();
    throw new System.Exception("hello");
}
""";

        var comp = CreateCompilation(new[] { source, ExtensionsSource },
            targetFramework: TargetFramework.Net70);

        comp.VerifyDiagnostics();
        CompileAndVerify(comp, expectedOutput: "hello", verify: Verification.Skipped);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void Execution_AwaitForeach_Returning()
    {
        var source = """
using System.Threading.Tasks;
using System.Collections.Generic;

await foreach (var i in m())
{
    System.Console.Write(i);
}

static async IAsyncEnumerable<int> m()
{
    await foreach (var i in m2())
    {
        yield return i;
    }
}

static async IAsyncEnumerable<int> m2()
{
    await Task.Yield();
    yield return 1;
    yield return 2;
    yield return 3;
}
""";

        var comp = CreateCompilation(new[] { source, ExtensionsSource },
            targetFramework: TargetFramework.Net70);

        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "123", verify: Verification.Skipped);
        verifier.VerifyIL("Program.<<<Main>$>g__m|0_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      664 (0x298)
  .maxstack  3
  .locals init (int V_0,
                System.Threading.CancellationToken V_1,
                int V_2, //i
                bool V_3,
                System.Runtime.CompilerServices.ValueTaskAwaiter<bool> V_4,
                System.Exception V_5,
                System.Threading.Tasks.ValueTask<bool> V_6,
                Program.<<<Main>$>g__m|0_0>d V_7,
                object V_8,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_9,
                System.Threading.Tasks.ValueTask V_10)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -4
    IL_000a:  sub
    IL_000b:  switch    (
        IL_0065,
        IL_0028,
        IL_0028,
        IL_0028,
        IL_0065,
        IL_01a7)
    IL_0028:  ldarg.0
    IL_0029:  ldfld      "bool Program.<<<Main>$>g__m|0_0>d.<>w__disposeMode"
    IL_002e:  brfalse.s  IL_0035
    IL_0030:  leave      IL_0264
    IL_0035:  ldarg.0
    IL_0036:  ldc.i4.m1
    IL_0037:  dup
    IL_0038:  stloc.0
    IL_0039:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
    IL_003e:  ldarg.0
    IL_003f:  call       "System.Collections.Generic.IAsyncEnumerable<int> Program.<<Main>$>g__m2|0_1()"
    IL_0044:  ldloca.s   V_1
    IL_0046:  initobj    "System.Threading.CancellationToken"
    IL_004c:  ldloc.1
    IL_004d:  callvirt   "System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)"
    IL_0052:  stfld      "System.Collections.Generic.IAsyncEnumerator<int> Program.<<<Main>$>g__m|0_0>d.<>7__wrap1"
    IL_0057:  ldarg.0
    IL_0058:  ldnull
    IL_0059:  stfld      "object Program.<<<Main>$>g__m|0_0>d.<>7__wrap2"
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.0
    IL_0060:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>7__wrap3"
    IL_0065:  nop
    .try
    {
      IL_0066:  ldloc.0
      IL_0067:  ldc.i4.s   -4
      IL_0069:  beq.s      IL_0095
      IL_006b:  ldloc.0
      IL_006c:  brfalse    IL_00f2
      IL_0071:  br.s       IL_00ab
      IL_0073:  ldarg.0
      IL_0074:  ldfld      "System.Collections.Generic.IAsyncEnumerator<int> Program.<<<Main>$>g__m|0_0>d.<>7__wrap1"
      IL_0079:  callvirt   "int System.Collections.Generic.IAsyncEnumerator<int>.Current.get"
      IL_007e:  stloc.2
      IL_007f:  ldarg.0
      IL_0080:  ldloc.2
      IL_0081:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>2__current"
      IL_0086:  ldarg.0
      IL_0087:  ldc.i4.s   -4
      IL_0089:  dup
      IL_008a:  stloc.0
      IL_008b:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
      IL_0090:  leave      IL_028b
      IL_0095:  ldarg.0
      IL_0096:  ldc.i4.m1
      IL_0097:  dup
      IL_0098:  stloc.0
      IL_0099:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
      IL_009e:  ldarg.0
      IL_009f:  ldfld      "bool Program.<<<Main>$>g__m|0_0>d.<>w__disposeMode"
      IL_00a4:  brfalse.s  IL_00ab
      IL_00a6:  leave      IL_0155
      IL_00ab:  ldarg.0
      IL_00ac:  ldfld      "System.Collections.Generic.IAsyncEnumerator<int> Program.<<<Main>$>g__m|0_0>d.<>7__wrap1"
      IL_00b1:  callvirt   "System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()"
      IL_00b6:  stloc.s    V_6
      IL_00b8:  ldloca.s   V_6
      IL_00ba:  call       "System.Runtime.CompilerServices.ValueTaskAwaiter<bool> System.Threading.Tasks.ValueTask<bool>.GetAwaiter()"
      IL_00bf:  stloc.s    V_4
      IL_00c1:  ldloca.s   V_4
      IL_00c3:  call       "bool System.Runtime.CompilerServices.ValueTaskAwaiter<bool>.IsCompleted.get"
      IL_00c8:  brtrue.s   IL_010f
      IL_00ca:  ldarg.0
      IL_00cb:  ldc.i4.0
      IL_00cc:  dup
      IL_00cd:  stloc.0
      IL_00ce:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
      IL_00d3:  ldarg.0
      IL_00d4:  ldloc.s    V_4
      IL_00d6:  stfld      "System.Runtime.CompilerServices.ValueTaskAwaiter<bool> Program.<<<Main>$>g__m|0_0>d.<>u__1"
      IL_00db:  ldarg.0
      IL_00dc:  stloc.s    V_7
      IL_00de:  ldarg.0
      IL_00df:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder Program.<<<Main>$>g__m|0_0>d.<>t__builder"
      IL_00e4:  ldloca.s   V_4
      IL_00e6:  ldloca.s   V_7
      IL_00e8:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter<bool>, Program.<<<Main>$>g__m|0_0>d>(ref System.Runtime.CompilerServices.ValueTaskAwaiter<bool>, ref Program.<<<Main>$>g__m|0_0>d)"
      IL_00ed:  leave      IL_0297
      IL_00f2:  ldarg.0
      IL_00f3:  ldfld      "System.Runtime.CompilerServices.ValueTaskAwaiter<bool> Program.<<<Main>$>g__m|0_0>d.<>u__1"
      IL_00f8:  stloc.s    V_4
      IL_00fa:  ldarg.0
      IL_00fb:  ldflda     "System.Runtime.CompilerServices.ValueTaskAwaiter<bool> Program.<<<Main>$>g__m|0_0>d.<>u__1"
      IL_0100:  initobj    "System.Runtime.CompilerServices.ValueTaskAwaiter<bool>"
      IL_0106:  ldarg.0
      IL_0107:  ldc.i4.m1
      IL_0108:  dup
      IL_0109:  stloc.0
      IL_010a:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
      IL_010f:  ldloc.s    V_4
      IL_0111:  ldloca.s   V_5
      IL_0113:  call       "bool ValueTaskAwaiterExceptionExtensions.GetResult<bool>(System.Runtime.CompilerServices.ValueTaskAwaiter<bool>, out System.Exception)"
      IL_0118:  stloc.3
      IL_0119:  ldloc.s    V_5
      IL_011b:  brfalse.s  IL_0141
      IL_011d:  ldarg.0
      IL_011e:  ldc.i4.0
      IL_011f:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>2__current"
      IL_0124:  ldarg.0
      IL_0125:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder Program.<<<Main>$>g__m|0_0>d.<>t__builder"
      IL_012a:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
      IL_012f:  ldarg.0
      IL_0130:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> Program.<<<Main>$>g__m|0_0>d.<>v__promiseOfValueOrEnd"
      IL_0135:  ldloc.s    V_5
      IL_0137:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)"
      IL_013c:  leave      IL_028a
      IL_0141:  ldloc.3
      IL_0142:  brtrue     IL_0073
      IL_0147:  leave.s    IL_0155
    }
    catch object
    {
      IL_0149:  stloc.s    V_8
      IL_014b:  ldarg.0
      IL_014c:  ldloc.s    V_8
      IL_014e:  stfld      "object Program.<<<Main>$>g__m|0_0>d.<>7__wrap2"
      IL_0153:  leave.s    IL_0155
    }
    IL_0155:  ldarg.0
    IL_0156:  ldfld      "System.Collections.Generic.IAsyncEnumerator<int> Program.<<<Main>$>g__m|0_0>d.<>7__wrap1"
    IL_015b:  brfalse    IL_01f5
    IL_0160:  ldarg.0
    IL_0161:  ldfld      "System.Collections.Generic.IAsyncEnumerator<int> Program.<<<Main>$>g__m|0_0>d.<>7__wrap1"
    IL_0166:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
    IL_016b:  stloc.s    V_10
    IL_016d:  ldloca.s   V_10
    IL_016f:  call       "System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()"
    IL_0174:  stloc.s    V_9
    IL_0176:  ldloca.s   V_9
    IL_0178:  call       "bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get"
    IL_017d:  brtrue.s   IL_01c4
    IL_017f:  ldarg.0
    IL_0180:  ldc.i4.1
    IL_0181:  dup
    IL_0182:  stloc.0
    IL_0183:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
    IL_0188:  ldarg.0
    IL_0189:  ldloc.s    V_9
    IL_018b:  stfld      "System.Runtime.CompilerServices.ValueTaskAwaiter Program.<<<Main>$>g__m|0_0>d.<>u__2"
    IL_0190:  ldarg.0
    IL_0191:  stloc.s    V_7
    IL_0193:  ldarg.0
    IL_0194:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder Program.<<<Main>$>g__m|0_0>d.<>t__builder"
    IL_0199:  ldloca.s   V_9
    IL_019b:  ldloca.s   V_7
    IL_019d:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, Program.<<<Main>$>g__m|0_0>d>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref Program.<<<Main>$>g__m|0_0>d)"
    IL_01a2:  leave      IL_0297
    IL_01a7:  ldarg.0
    IL_01a8:  ldfld      "System.Runtime.CompilerServices.ValueTaskAwaiter Program.<<<Main>$>g__m|0_0>d.<>u__2"
    IL_01ad:  stloc.s    V_9
    IL_01af:  ldarg.0
    IL_01b0:  ldflda     "System.Runtime.CompilerServices.ValueTaskAwaiter Program.<<<Main>$>g__m|0_0>d.<>u__2"
    IL_01b5:  initobj    "System.Runtime.CompilerServices.ValueTaskAwaiter"
    IL_01bb:  ldarg.0
    IL_01bc:  ldc.i4.m1
    IL_01bd:  dup
    IL_01be:  stloc.0
    IL_01bf:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
    IL_01c4:  ldloc.s    V_9
    IL_01c6:  ldloca.s   V_5
    IL_01c8:  call       "void ValueTaskAwaiterExceptionExtensions.GetResult(System.Runtime.CompilerServices.ValueTaskAwaiter, out System.Exception)"
    IL_01cd:  ldloc.s    V_5
    IL_01cf:  brfalse.s  IL_01f5
    IL_01d1:  ldarg.0
    IL_01d2:  ldc.i4.0
    IL_01d3:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>2__current"
    IL_01d8:  ldarg.0
    IL_01d9:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder Program.<<<Main>$>g__m|0_0>d.<>t__builder"
    IL_01de:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
    IL_01e3:  ldarg.0
    IL_01e4:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> Program.<<<Main>$>g__m|0_0>d.<>v__promiseOfValueOrEnd"
    IL_01e9:  ldloc.s    V_5
    IL_01eb:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)"
    IL_01f0:  leave      IL_028a
    IL_01f5:  ldarg.0
    IL_01f6:  ldfld      "object Program.<<<Main>$>g__m|0_0>d.<>7__wrap2"
    IL_01fb:  stloc.s    V_8
    IL_01fd:  ldloc.s    V_8
    IL_01ff:  brfalse.s  IL_0218
    IL_0201:  ldloc.s    V_8
    IL_0203:  isinst     "System.Exception"
    IL_0208:  dup
    IL_0209:  brtrue.s   IL_020e
    IL_020b:  ldloc.s    V_8
    IL_020d:  throw
    IL_020e:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
    IL_0213:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
    IL_0218:  ldarg.0
    IL_0219:  ldfld      "int Program.<<<Main>$>g__m|0_0>d.<>7__wrap3"
    IL_021e:  pop
    IL_021f:  ldarg.0
    IL_0220:  ldfld      "bool Program.<<<Main>$>g__m|0_0>d.<>w__disposeMode"
    IL_0225:  brfalse.s  IL_0229
    IL_0227:  leave.s    IL_0264
    IL_0229:  ldarg.0
    IL_022a:  ldnull
    IL_022b:  stfld      "object Program.<<<Main>$>g__m|0_0>d.<>7__wrap2"
    IL_0230:  ldarg.0
    IL_0231:  ldnull
    IL_0232:  stfld      "System.Collections.Generic.IAsyncEnumerator<int> Program.<<<Main>$>g__m|0_0>d.<>7__wrap1"
    IL_0237:  leave.s    IL_0264
  }
  catch System.Exception
  {
    IL_0239:  stloc.s    V_5
    IL_023b:  ldarg.0
    IL_023c:  ldc.i4.s   -2
    IL_023e:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
    IL_0243:  ldarg.0
    IL_0244:  ldc.i4.0
    IL_0245:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>2__current"
    IL_024a:  ldarg.0
    IL_024b:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder Program.<<<Main>$>g__m|0_0>d.<>t__builder"
    IL_0250:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
    IL_0255:  ldarg.0
    IL_0256:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> Program.<<<Main>$>g__m|0_0>d.<>v__promiseOfValueOrEnd"
    IL_025b:  ldloc.s    V_5
    IL_025d:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)"
    IL_0262:  leave.s    IL_0297
  }
  IL_0264:  ldarg.0
  IL_0265:  ldc.i4.s   -2
  IL_0267:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
  IL_026c:  ldarg.0
  IL_026d:  ldc.i4.0
  IL_026e:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>2__current"
  IL_0273:  ldarg.0
  IL_0274:  ldflda     "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder Program.<<<Main>$>g__m|0_0>d.<>t__builder"
  IL_0279:  call       "void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()"
  IL_027e:  ldarg.0
  IL_027f:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> Program.<<<Main>$>g__m|0_0>d.<>v__promiseOfValueOrEnd"
  IL_0284:  ldc.i4.0
  IL_0285:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
  IL_028a:  ret
  IL_028b:  ldarg.0
  IL_028c:  ldflda     "System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> Program.<<<Main>$>g__m|0_0>d.<>v__promiseOfValueOrEnd"
  IL_0291:  ldc.i4.1
  IL_0292:  call       "void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)"
  IL_0297:  ret
}
""");
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void Execution_AwaitValueTaskOfInt_Returning()
    {
        var source = """
using System.Threading.Tasks;

System.Console.Write(await m());

static async ValueTask<int> m()
{
    return await m2();
}

static async ValueTask<int> m2()
{
    await Task.Yield();
    return 42;
}
""";

        var comp = CreateCompilation(new[] { source, ExtensionsSource },
            targetFramework: TargetFramework.Net70);

        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "42", verify: Verification.Skipped);
        verifier.VerifyIL("Program.<<<Main>$>g__m|0_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      172 (0xac)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.ValueTaskAwaiter<int> V_3,
                System.Exception V_4,
                System.Threading.Tasks.ValueTask<int> V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0042
    IL_000a:  call       "System.Threading.Tasks.ValueTask<int> Program.<<Main>$>g__m2|0_1()"
    IL_000f:  stloc.s    V_5
    IL_0011:  ldloca.s   V_5
    IL_0013:  call       "System.Runtime.CompilerServices.ValueTaskAwaiter<int> System.Threading.Tasks.ValueTask<int>.GetAwaiter()"
    IL_0018:  stloc.3
    IL_0019:  ldloca.s   V_3
    IL_001b:  call       "bool System.Runtime.CompilerServices.ValueTaskAwaiter<int>.IsCompleted.get"
    IL_0020:  brtrue.s   IL_005e
    IL_0022:  ldarg.0
    IL_0023:  ldc.i4.0
    IL_0024:  dup
    IL_0025:  stloc.0
    IL_0026:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
    IL_002b:  ldarg.0
    IL_002c:  ldloc.3
    IL_002d:  stfld      "System.Runtime.CompilerServices.ValueTaskAwaiter<int> Program.<<<Main>$>g__m|0_0>d.<>u__1"
    IL_0032:  ldarg.0
    IL_0033:  ldflda     "System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<int> Program.<<<Main>$>g__m|0_0>d.<>t__builder"
    IL_0038:  ldloca.s   V_3
    IL_003a:  ldarg.0
    IL_003b:  call       "void System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter<int>, Program.<<<Main>$>g__m|0_0>d>(ref System.Runtime.CompilerServices.ValueTaskAwaiter<int>, ref Program.<<<Main>$>g__m|0_0>d)"
    IL_0040:  leave.s    IL_00ab
    IL_0042:  ldarg.0
    IL_0043:  ldfld      "System.Runtime.CompilerServices.ValueTaskAwaiter<int> Program.<<<Main>$>g__m|0_0>d.<>u__1"
    IL_0048:  stloc.3
    IL_0049:  ldarg.0
    IL_004a:  ldflda     "System.Runtime.CompilerServices.ValueTaskAwaiter<int> Program.<<<Main>$>g__m|0_0>d.<>u__1"
    IL_004f:  initobj    "System.Runtime.CompilerServices.ValueTaskAwaiter<int>"
    IL_0055:  ldarg.0
    IL_0056:  ldc.i4.m1
    IL_0057:  dup
    IL_0058:  stloc.0
    IL_0059:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
    IL_005e:  ldloc.3
    IL_005f:  ldloca.s   V_4
    IL_0061:  call       "int ValueTaskAwaiterExceptionExtensions.GetResult<int>(System.Runtime.CompilerServices.ValueTaskAwaiter<int>, out System.Exception)"
    IL_0066:  stloc.2
    IL_0067:  ldloc.s    V_4
    IL_0069:  brfalse.s  IL_007a
    IL_006b:  ldarg.0
    IL_006c:  ldflda     "System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<int> Program.<<<Main>$>g__m|0_0>d.<>t__builder"
    IL_0071:  ldloc.s    V_4
    IL_0073:  call       "void System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<int>.SetException(System.Exception)"
    IL_0078:  leave.s    IL_00ab
    IL_007a:  ldloc.2
    IL_007b:  stloc.1
    IL_007c:  leave.s    IL_0097
  }
  catch System.Exception
  {
    IL_007e:  stloc.s    V_4
    IL_0080:  ldarg.0
    IL_0081:  ldc.i4.s   -2
    IL_0083:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
    IL_0088:  ldarg.0
    IL_0089:  ldflda     "System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<int> Program.<<<Main>$>g__m|0_0>d.<>t__builder"
    IL_008e:  ldloc.s    V_4
    IL_0090:  call       "void System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<int>.SetException(System.Exception)"
    IL_0095:  leave.s    IL_00ab
  }
  IL_0097:  ldarg.0
  IL_0098:  ldc.i4.s   -2
  IL_009a:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
  IL_009f:  ldarg.0
  IL_00a0:  ldflda     "System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<int> Program.<<<Main>$>g__m|0_0>d.<>t__builder"
  IL_00a5:  ldloc.1
  IL_00a6:  call       "void System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<int>.SetResult(int)"
  IL_00ab:  ret
}
""");
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void Execution_AwaitValueTaskOfInt_Throwing()
    {
        var source = """
using System.Threading.Tasks;

try
{
    _ = await m();
}
catch (System.Exception e)
{
    System.Console.Write(e.Message);
}

static async ValueTask<int> m()
{
    return await m2();
}

static async ValueTask<int> m2()
{
    await Task.Yield();
    throw new System.Exception("hello");
}
""";

        var comp = CreateCompilation(new[] { source, ExtensionsSource },
            targetFramework: TargetFramework.Net70);

        comp.VerifyDiagnostics();
        CompileAndVerify(comp, expectedOutput: "hello", verify: Verification.Skipped);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void Execution_AwaitConfiguredValueTaskOfInt_Returning()
    {
        var source = """
using System.Threading.Tasks;

System.Console.Write(await m().ConfigureAwait(false));

static async ValueTask<int> m()
{
    return await m2().ConfigureAwait(false);
}

static async ValueTask<int> m2()
{
    await Task.Yield();
    return 42;
}
""";

        var comp = CreateCompilation(new[] { source, ExtensionsSource },
            targetFramework: TargetFramework.Net70);

        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "42", verify: Verification.Skipped);

        // We use ValueTaskAwaiterExceptionExtensions.GetResult
        verifier.VerifyIL("Program.<<<Main>$>g__m|0_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      182 (0xb6)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter V_3,
                System.Exception V_4,
                System.Threading.Tasks.ValueTask<int> V_5,
                System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<int> V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  call       "System.Threading.Tasks.ValueTask<int> Program.<<Main>$>g__m2|0_1()"
    IL_000f:  stloc.s    V_5
    IL_0011:  ldloca.s   V_5
    IL_0013:  ldc.i4.0
    IL_0014:  call       "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<int> System.Threading.Tasks.ValueTask<int>.ConfigureAwait(bool)"
    IL_0019:  stloc.s    V_6
    IL_001b:  ldloca.s   V_6
    IL_001d:  call       "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<int>.GetAwaiter()"
    IL_0022:  stloc.3
    IL_0023:  ldloca.s   V_3
    IL_0025:  call       "bool System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter.IsCompleted.get"
    IL_002a:  brtrue.s   IL_0068
    IL_002c:  ldarg.0
    IL_002d:  ldc.i4.0
    IL_002e:  dup
    IL_002f:  stloc.0
    IL_0030:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
    IL_0035:  ldarg.0
    IL_0036:  ldloc.3
    IL_0037:  stfld      "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter Program.<<<Main>$>g__m|0_0>d.<>u__1"
    IL_003c:  ldarg.0
    IL_003d:  ldflda     "System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<int> Program.<<<Main>$>g__m|0_0>d.<>t__builder"
    IL_0042:  ldloca.s   V_3
    IL_0044:  ldarg.0
    IL_0045:  call       "void System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter, Program.<<<Main>$>g__m|0_0>d>(ref System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter, ref Program.<<<Main>$>g__m|0_0>d)"
    IL_004a:  leave.s    IL_00b5
    IL_004c:  ldarg.0
    IL_004d:  ldfld      "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter Program.<<<Main>$>g__m|0_0>d.<>u__1"
    IL_0052:  stloc.3
    IL_0053:  ldarg.0
    IL_0054:  ldflda     "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter Program.<<<Main>$>g__m|0_0>d.<>u__1"
    IL_0059:  initobj    "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter"
    IL_005f:  ldarg.0
    IL_0060:  ldc.i4.m1
    IL_0061:  dup
    IL_0062:  stloc.0
    IL_0063:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
    IL_0068:  ldloc.3
    IL_0069:  ldloca.s   V_4
    IL_006b:  call       "int ValueTaskAwaiterExceptionExtensions.GetResult<int>(System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter, out System.Exception)"
    IL_0070:  stloc.2
    IL_0071:  ldloc.s    V_4
    IL_0073:  brfalse.s  IL_0084
    IL_0075:  ldarg.0
    IL_0076:  ldflda     "System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<int> Program.<<<Main>$>g__m|0_0>d.<>t__builder"
    IL_007b:  ldloc.s    V_4
    IL_007d:  call       "void System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<int>.SetException(System.Exception)"
    IL_0082:  leave.s    IL_00b5
    IL_0084:  ldloc.2
    IL_0085:  stloc.1
    IL_0086:  leave.s    IL_00a1
  }
  catch System.Exception
  {
    IL_0088:  stloc.s    V_4
    IL_008a:  ldarg.0
    IL_008b:  ldc.i4.s   -2
    IL_008d:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
    IL_0092:  ldarg.0
    IL_0093:  ldflda     "System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<int> Program.<<<Main>$>g__m|0_0>d.<>t__builder"
    IL_0098:  ldloc.s    V_4
    IL_009a:  call       "void System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<int>.SetException(System.Exception)"
    IL_009f:  leave.s    IL_00b5
  }
  IL_00a1:  ldarg.0
  IL_00a2:  ldc.i4.s   -2
  IL_00a4:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
  IL_00a9:  ldarg.0
  IL_00aa:  ldflda     "System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<int> Program.<<<Main>$>g__m|0_0>d.<>t__builder"
  IL_00af:  ldloc.1
  IL_00b0:  call       "void System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<int>.SetResult(int)"
  IL_00b5:  ret
}
""");
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void Execution_AwaitValueTask_Returning()
    {
        var source = """
using System.Threading.Tasks;

await m();

static async ValueTask m()
{
    await m2();
}

static async ValueTask m2()
{
    await Task.Yield();
    System.Console.Write(42);
}
""";

        var comp = CreateCompilation(new[] { source, ExtensionsSource },
            targetFramework: TargetFramework.Net70);

        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "42", verify: Verification.Skipped);
        verifier.VerifyIL("Program.<<<Main>$>g__m|0_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      163 (0xa3)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_1,
                System.Exception V_2,
                System.Threading.Tasks.ValueTask V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0041
    IL_000a:  call       "System.Threading.Tasks.ValueTask Program.<<Main>$>g__m2|0_1()"
    IL_000f:  stloc.3
    IL_0010:  ldloca.s   V_3
    IL_0012:  call       "System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()"
    IL_0017:  stloc.1
    IL_0018:  ldloca.s   V_1
    IL_001a:  call       "bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get"
    IL_001f:  brtrue.s   IL_005d
    IL_0021:  ldarg.0
    IL_0022:  ldc.i4.0
    IL_0023:  dup
    IL_0024:  stloc.0
    IL_0025:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
    IL_002a:  ldarg.0
    IL_002b:  ldloc.1
    IL_002c:  stfld      "System.Runtime.CompilerServices.ValueTaskAwaiter Program.<<<Main>$>g__m|0_0>d.<>u__1"
    IL_0031:  ldarg.0
    IL_0032:  ldflda     "System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder Program.<<<Main>$>g__m|0_0>d.<>t__builder"
    IL_0037:  ldloca.s   V_1
    IL_0039:  ldarg.0
    IL_003a:  call       "void System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, Program.<<<Main>$>g__m|0_0>d>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref Program.<<<Main>$>g__m|0_0>d)"
    IL_003f:  leave.s    IL_00a2
    IL_0041:  ldarg.0
    IL_0042:  ldfld      "System.Runtime.CompilerServices.ValueTaskAwaiter Program.<<<Main>$>g__m|0_0>d.<>u__1"
    IL_0047:  stloc.1
    IL_0048:  ldarg.0
    IL_0049:  ldflda     "System.Runtime.CompilerServices.ValueTaskAwaiter Program.<<<Main>$>g__m|0_0>d.<>u__1"
    IL_004e:  initobj    "System.Runtime.CompilerServices.ValueTaskAwaiter"
    IL_0054:  ldarg.0
    IL_0055:  ldc.i4.m1
    IL_0056:  dup
    IL_0057:  stloc.0
    IL_0058:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
    IL_005d:  ldloc.1
    IL_005e:  ldloca.s   V_2
    IL_0060:  call       "void ValueTaskAwaiterExceptionExtensions.GetResult(System.Runtime.CompilerServices.ValueTaskAwaiter, out System.Exception)"
    IL_0065:  ldloc.2
    IL_0066:  brfalse.s  IL_0076
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder Program.<<<Main>$>g__m|0_0>d.<>t__builder"
    IL_006e:  ldloc.2
    IL_006f:  call       "void System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder.SetException(System.Exception)"
    IL_0074:  leave.s    IL_00a2
    IL_0076:  leave.s    IL_008f
  }
  catch System.Exception
  {
    IL_0078:  stloc.2
    IL_0079:  ldarg.0
    IL_007a:  ldc.i4.s   -2
    IL_007c:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
    IL_0081:  ldarg.0
    IL_0082:  ldflda     "System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder Program.<<<Main>$>g__m|0_0>d.<>t__builder"
    IL_0087:  ldloc.2
    IL_0088:  call       "void System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder.SetException(System.Exception)"
    IL_008d:  leave.s    IL_00a2
  }
  IL_008f:  ldarg.0
  IL_0090:  ldc.i4.s   -2
  IL_0092:  stfld      "int Program.<<<Main>$>g__m|0_0>d.<>1__state"
  IL_0097:  ldarg.0
  IL_0098:  ldflda     "System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder Program.<<<Main>$>g__m|0_0>d.<>t__builder"
  IL_009d:  call       "void System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder.SetResult()"
  IL_00a2:  ret
}
""");
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void Execution_AwaitValueTask_Throwing()
    {
        var source = """
using System.Threading.Tasks;

try
{
    await m();
}
catch (System.Exception e)
{
    System.Console.Write(e.Message);
}

static async ValueTask m()
{
    await m2();
}

static async ValueTask m2()
{
    await Task.Yield();
    throw new System.Exception("hello");
}
""";

        var comp = CreateCompilation(new[] { source, ExtensionsSource },
            targetFramework: TargetFramework.Net70);

        comp.VerifyDiagnostics();
        CompileAndVerify(comp, expectedOutput: "hello", verify: Verification.Skipped);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void Execution_AwaitUsing()
    {
        var source = """
using System;
using System.Threading.Tasks;

await using (new C())
{
    System.Console.Write("body ");
}

System.Console.Write("disposed");

class C : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await Task.Yield();
        System.Console.Write("disposing ");
        await Task.Yield();
    }
}
""";

        var comp = CreateCompilation(new[] { source, ExtensionsSource },
            targetFramework: TargetFramework.Net70);

        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "body disposing disposed", verify: Verification.Skipped);

        // We use ValueTaskAwaiterExceptionExtensions.GetResult
        verifier.VerifyIL("Program.<<Main>$>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      266 (0x10a)
  .maxstack  3
  .locals init (int V_0,
                C V_1,
                object V_2,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_3,
                System.Exception V_4,
                System.Threading.Tasks.ValueTask V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int Program.<<Main>$>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0073
    IL_000a:  newobj     "C..ctor()"
    IL_000f:  stloc.1
    IL_0010:  ldarg.0
    IL_0011:  ldnull
    IL_0012:  stfld      "object Program.<<Main>$>d__0.<>7__wrap1"
    IL_0017:  ldarg.0
    IL_0018:  ldc.i4.0
    IL_0019:  stfld      "int Program.<<Main>$>d__0.<>7__wrap2"
    .try
    {
      IL_001e:  ldstr      "body "
      IL_0023:  call       "void System.Console.Write(string)"
      IL_0028:  leave.s    IL_0034
    }
    catch object
    {
      IL_002a:  stloc.2
      IL_002b:  ldarg.0
      IL_002c:  ldloc.2
      IL_002d:  stfld      "object Program.<<Main>$>d__0.<>7__wrap1"
      IL_0032:  leave.s    IL_0034
    }
    IL_0034:  ldloc.1
    IL_0035:  brfalse.s  IL_00aa
    IL_0037:  ldloc.1
    IL_0038:  callvirt   "System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()"
    IL_003d:  stloc.s    V_5
    IL_003f:  ldloca.s   V_5
    IL_0041:  call       "System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()"
    IL_0046:  stloc.3
    IL_0047:  ldloca.s   V_3
    IL_0049:  call       "bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get"
    IL_004e:  brtrue.s   IL_008f
    IL_0050:  ldarg.0
    IL_0051:  ldc.i4.0
    IL_0052:  dup
    IL_0053:  stloc.0
    IL_0054:  stfld      "int Program.<<Main>$>d__0.<>1__state"
    IL_0059:  ldarg.0
    IL_005a:  ldloc.3
    IL_005b:  stfld      "System.Runtime.CompilerServices.ValueTaskAwaiter Program.<<Main>$>d__0.<>u__1"
    IL_0060:  ldarg.0
    IL_0061:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<Main>$>d__0.<>t__builder"
    IL_0066:  ldloca.s   V_3
    IL_0068:  ldarg.0
    IL_0069:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, Program.<<Main>$>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref Program.<<Main>$>d__0)"
    IL_006e:  leave      IL_0108
    IL_0073:  ldarg.0
    IL_0074:  ldfld      "System.Runtime.CompilerServices.ValueTaskAwaiter Program.<<Main>$>d__0.<>u__1"
    IL_0079:  stloc.3
    IL_007a:  ldarg.0
    IL_007b:  ldflda     "System.Runtime.CompilerServices.ValueTaskAwaiter Program.<<Main>$>d__0.<>u__1"
    IL_0080:  initobj    "System.Runtime.CompilerServices.ValueTaskAwaiter"
    IL_0086:  ldarg.0
    IL_0087:  ldc.i4.m1
    IL_0088:  dup
    IL_0089:  stloc.0
    IL_008a:  stfld      "int Program.<<Main>$>d__0.<>1__state"
    IL_008f:  ldloc.3
    IL_0090:  ldloca.s   V_4
    IL_0092:  call       "void ValueTaskAwaiterExceptionExtensions.GetResult(System.Runtime.CompilerServices.ValueTaskAwaiter, out System.Exception)"
    IL_0097:  ldloc.s    V_4
    IL_0099:  brfalse.s  IL_00aa
    IL_009b:  ldarg.0
    IL_009c:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<Main>$>d__0.<>t__builder"
    IL_00a1:  ldloc.s    V_4
    IL_00a3:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00a8:  leave.s    IL_0109
    IL_00aa:  ldarg.0
    IL_00ab:  ldfld      "object Program.<<Main>$>d__0.<>7__wrap1"
    IL_00b0:  stloc.2
    IL_00b1:  ldloc.2
    IL_00b2:  brfalse.s  IL_00c9
    IL_00b4:  ldloc.2
    IL_00b5:  isinst     "System.Exception"
    IL_00ba:  dup
    IL_00bb:  brtrue.s   IL_00bf
    IL_00bd:  ldloc.2
    IL_00be:  throw
    IL_00bf:  call       "System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)"
    IL_00c4:  callvirt   "void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()"
    IL_00c9:  ldarg.0
    IL_00ca:  ldnull
    IL_00cb:  stfld      "object Program.<<Main>$>d__0.<>7__wrap1"
    IL_00d0:  ldstr      "disposed"
    IL_00d5:  call       "void System.Console.Write(string)"
    IL_00da:  leave.s    IL_00f5
  }
  catch System.Exception
  {
    IL_00dc:  stloc.s    V_4
    IL_00de:  ldarg.0
    IL_00df:  ldc.i4.s   -2
    IL_00e1:  stfld      "int Program.<<Main>$>d__0.<>1__state"
    IL_00e6:  ldarg.0
    IL_00e7:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<Main>$>d__0.<>t__builder"
    IL_00ec:  ldloc.s    V_4
    IL_00ee:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00f3:  leave.s    IL_0108
  }
  IL_00f5:  ldarg.0
  IL_00f6:  ldc.i4.s   -2
  IL_00f8:  stfld      "int Program.<<Main>$>d__0.<>1__state"
  IL_00fd:  ldarg.0
  IL_00fe:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<Main>$>d__0.<>t__builder"
  IL_0103:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0108:  ret
  IL_0109:  ret
}
""");
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public void Execution_TryWithFinally_Returning()
    {
        var source = """
using System;
using System.Threading.Tasks;

try
{
    await m();
}
finally
{
    Console.Write("finally");
}

static async ValueTask<int> m()
{
    await Task.Yield();
    Console.Write("ran ");
    return 42;
}
""";

        var comp = CreateCompilation(new[] { source, ExtensionsSource }, targetFramework: TargetFramework.Net70);

        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(comp, expectedOutput: "ran finally", verify: Verification.Skipped);

        // We use ValueTaskAwaiterExceptionExtensions.GetResult
        verifier.VerifyIL("Program.<<Main>$>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      185 (0xb9)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.ValueTaskAwaiter<int> V_1,
                System.Exception V_2,
                System.Threading.Tasks.ValueTask<int> V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int Program.<<Main>$>d__0.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  pop
    IL_0009:  nop
    .try
    {
      IL_000a:  ldloc.0
      IL_000b:  brfalse.s  IL_0044
      IL_000d:  call       "System.Threading.Tasks.ValueTask<int> Program.<<Main>$>g__m|0_0()"
      IL_0012:  stloc.3
      IL_0013:  ldloca.s   V_3
      IL_0015:  call       "System.Runtime.CompilerServices.ValueTaskAwaiter<int> System.Threading.Tasks.ValueTask<int>.GetAwaiter()"
      IL_001a:  stloc.1
      IL_001b:  ldloca.s   V_1
      IL_001d:  call       "bool System.Runtime.CompilerServices.ValueTaskAwaiter<int>.IsCompleted.get"
      IL_0022:  brtrue.s   IL_0060
      IL_0024:  ldarg.0
      IL_0025:  ldc.i4.0
      IL_0026:  dup
      IL_0027:  stloc.0
      IL_0028:  stfld      "int Program.<<Main>$>d__0.<>1__state"
      IL_002d:  ldarg.0
      IL_002e:  ldloc.1
      IL_002f:  stfld      "System.Runtime.CompilerServices.ValueTaskAwaiter<int> Program.<<Main>$>d__0.<>u__1"
      IL_0034:  ldarg.0
      IL_0035:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<Main>$>d__0.<>t__builder"
      IL_003a:  ldloca.s   V_1
      IL_003c:  ldarg.0
      IL_003d:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter<int>, Program.<<Main>$>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter<int>, ref Program.<<Main>$>d__0)"
      IL_0042:  leave.s    IL_00b7
      IL_0044:  ldarg.0
      IL_0045:  ldfld      "System.Runtime.CompilerServices.ValueTaskAwaiter<int> Program.<<Main>$>d__0.<>u__1"
      IL_004a:  stloc.1
      IL_004b:  ldarg.0
      IL_004c:  ldflda     "System.Runtime.CompilerServices.ValueTaskAwaiter<int> Program.<<Main>$>d__0.<>u__1"
      IL_0051:  initobj    "System.Runtime.CompilerServices.ValueTaskAwaiter<int>"
      IL_0057:  ldarg.0
      IL_0058:  ldc.i4.m1
      IL_0059:  dup
      IL_005a:  stloc.0
      IL_005b:  stfld      "int Program.<<Main>$>d__0.<>1__state"
      IL_0060:  ldloc.1
      IL_0061:  ldloca.s   V_2
      IL_0063:  call       "int ValueTaskAwaiterExceptionExtensions.GetResult<int>(System.Runtime.CompilerServices.ValueTaskAwaiter<int>, out System.Exception)"
      IL_0068:  pop
      IL_0069:  ldloc.2
      IL_006a:  brfalse.s  IL_007a
      IL_006c:  ldarg.0
      IL_006d:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<Main>$>d__0.<>t__builder"
      IL_0072:  ldloc.2
      IL_0073:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
      IL_0078:  leave.s    IL_00b8
      IL_007a:  leave.s    IL_008b
    }
    finally
    {
      IL_007c:  ldloc.0
      IL_007d:  ldc.i4.0
      IL_007e:  bge.s      IL_008a
      IL_0080:  ldstr      "finally"
      IL_0085:  call       "void System.Console.Write(string)"
      IL_008a:  endfinally
    }
    IL_008b:  leave.s    IL_00a4
  }
  catch System.Exception
  {
    IL_008d:  stloc.2
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.s   -2
    IL_0091:  stfld      "int Program.<<Main>$>d__0.<>1__state"
    IL_0096:  ldarg.0
    IL_0097:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<Main>$>d__0.<>t__builder"
    IL_009c:  ldloc.2
    IL_009d:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00a2:  leave.s    IL_00b7
  }
  IL_00a4:  ldarg.0
  IL_00a5:  ldc.i4.s   -2
  IL_00a7:  stfld      "int Program.<<Main>$>d__0.<>1__state"
  IL_00ac:  ldarg.0
  IL_00ad:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<Main>$>d__0.<>t__builder"
  IL_00b2:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00b7:  ret
  IL_00b8:  ret
}
""");
    }
}
