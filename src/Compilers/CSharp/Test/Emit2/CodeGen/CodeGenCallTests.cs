// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Resources.Proprietary;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenCallTests : CSharpTestBase
    {
        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Call_Class()
        {
            var source = @"
using System;

interface IMoveable
{
    void GetName(int x);
}

class Item : IMoveable
{
    public string Name {get; set;}

    public void GetName(int x)
    {
        Console.WriteLine(""Position GetName for item '{0}'"", Name);
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Call1(item1);

        var item2 = new Item {Name = ""2""};
        Call2(item2);
    }

    static void Call1<T>(T item) where T : class, IMoveable
    {
        item.GetName(GetOffset(ref item));
    }

    static void Call2<T>(T item) where T : IMoveable
    {
        item.GetName(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            // Wrong output on some frameworks
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe/*, expectedOutput: @"
Position GetName for item '1'
Position GetName for item '2'
"*/).VerifyDiagnostics();

            verifier.VerifyIL("Program.Call1<T>",
@"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  ldarga.s   V_0
  IL_0008:  call       ""int Program.GetOffset<T>(ref T)""
  IL_000d:  callvirt   ""void IMoveable.GetName(int)""
  IL_0012:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Call2<T>",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0009:  constrained. ""T""
  IL_000f:  callvirt   ""void IMoveable.GetName(int)""
  IL_0014:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Call_Struct()
        {
            var source = @"
using System;

interface IMoveable
{
    void GetName(int x);
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public void GetName(int x)
    {
        Console.WriteLine(""Position GetName for item '{0}'"", Name);
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Call1(item1);

        var item2 = new Item {Name = ""2""};
        Call2(item2);
    }

    static void Call1<T>(T item) where T : struct, IMoveable
    {
        item.GetName(GetOffset(ref item));
    }

    static void Call2<T>(T item) where T : IMoveable
    {
        item.GetName(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position GetName for item '-1'
Position GetName for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Call1<T>",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0009:  constrained. ""T""
  IL_000f:  callvirt   ""void IMoveable.GetName(int)""
  IL_0014:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Call_Class_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    void GetName(int x);
}

class Item : IMoveable
{
    public string Name {get; set;}

    public void GetName(int x)
    {
        Console.WriteLine(""Position GetName for item '{0}'"", Name);
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Call1(ref item1);

        var item2 = new Item {Name = ""2""};
        Call2(ref item2);
    }

    static void Call1<T>(ref T item) where T : class, IMoveable
    {
        item.GetName(GetOffset(ref item));
    }

    static void Call2<T>(ref T item) where T : IMoveable
    {
        item.GetName(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            // Wrong output on some frameworks
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe/*, expectedOutput: @"
Position GetName for item '1'
Position GetName for item '2'
"*/).VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.Call1<T>",
@"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""void IMoveable.GetName(int)""
  IL_0012:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Call2<T>",
@"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""void IMoveable.GetName(int)""
  IL_0012:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Call_Struct_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    void GetName(int x);
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public void GetName(int x)
    {
        Console.WriteLine(""Position GetName for item '{0}'"", Name);
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Call1(ref item1);

        var item2 = new Item {Name = ""2""};
        Call2(ref item2);
    }

    static void Call1<T>(ref T item) where T : struct, IMoveable
    {
        item.GetName(GetOffset(ref item));
    }

    static void Call2<T>(ref T item) where T : IMoveable
    {
        item.GetName(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position GetName for item '-1'
Position GetName for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Call1<T>",
@"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""void IMoveable.GetName(int)""
  IL_0012:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Call_Class_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    void GetName(int x);
}

class Item : IMoveable
{
    public string Name {get; set;}

    public void GetName(int x)
    {
        Console.WriteLine(""Position GetName for item '{0}'"", Name);
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Call1(item1);

        var item2 = new Item {Name = ""2""};
        await Call2(item2);
    }

    static async Task Call1<T>(T item) where T : class, IMoveable
    {
        item.GetName(await GetOffsetAsync(GetOffset(ref item)));
    }

    static async Task Call2<T>(T item) where T : IMoveable
    {
        item.GetName(await GetOffsetAsync(GetOffset(ref item)));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position GetName for item '1'
Position GetName for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Call1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      195 (0xc3)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Call1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0055
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""T Program.<Call1>d__1<T>.item""
    IL_0011:  stfld      ""T Program.<Call1>d__1<T>.<>7__wrap1""
    IL_0016:  ldarg.0
    IL_0017:  ldflda     ""T Program.<Call1>d__1<T>.item""
    IL_001c:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0021:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0026:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_002b:  stloc.2
    IL_002c:  ldloca.s   V_2
    IL_002e:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0033:  brtrue.s   IL_0071
    IL_0035:  ldarg.0
    IL_0036:  ldc.i4.0
    IL_0037:  dup
    IL_0038:  stloc.0
    IL_0039:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_003e:  ldarg.0
    IL_003f:  ldloc.2
    IL_0040:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call1>d__1<T>.<>u__1""
    IL_0045:  ldarg.0
    IL_0046:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
    IL_004b:  ldloca.s   V_2
    IL_004d:  ldarg.0
    IL_004e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Call1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Call1>d__1<T>)""
    IL_0053:  leave.s    IL_00c2
    IL_0055:  ldarg.0
    IL_0056:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call1>d__1<T>.<>u__1""
    IL_005b:  stloc.2
    IL_005c:  ldarg.0
    IL_005d:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call1>d__1<T>.<>u__1""
    IL_0062:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0068:  ldarg.0
    IL_0069:  ldc.i4.m1
    IL_006a:  dup
    IL_006b:  stloc.0
    IL_006c:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_0071:  ldloca.s   V_2
    IL_0073:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0078:  stloc.1
    IL_0079:  ldarg.0
    IL_007a:  ldfld      ""T Program.<Call1>d__1<T>.<>7__wrap1""
    IL_007f:  box        ""T""
    IL_0084:  ldloc.1
    IL_0085:  callvirt   ""void IMoveable.GetName(int)""
    IL_008a:  ldarg.0
    IL_008b:  ldflda     ""T Program.<Call1>d__1<T>.<>7__wrap1""
    IL_0090:  initobj    ""T""
    IL_0096:  leave.s    IL_00af
  }
  catch System.Exception
  {
    IL_0098:  stloc.3
    IL_0099:  ldarg.0
    IL_009a:  ldc.i4.s   -2
    IL_009c:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_00a1:  ldarg.0
    IL_00a2:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
    IL_00a7:  ldloc.3
    IL_00a8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00ad:  leave.s    IL_00c2
  }
  IL_00af:  ldarg.0
  IL_00b0:  ldc.i4.s   -2
  IL_00b2:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
  IL_00b7:  ldarg.0
  IL_00b8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
  IL_00bd:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c2:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Call2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      172 (0xac)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Call2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0049
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Call2>d__2<T>.item""
    IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0015:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_001a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0027:  brtrue.s   IL_0065
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call2>d__2<T>.<>u__1""
    IL_0039:  ldarg.0
    IL_003a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call2>d__2<T>.<>t__builder""
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Call2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Call2>d__2<T>)""
    IL_0047:  leave.s    IL_00ab
    IL_0049:  ldarg.0
    IL_004a:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call2>d__2<T>.<>u__1""
    IL_004f:  stloc.2
    IL_0050:  ldarg.0
    IL_0051:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call2>d__2<T>.<>u__1""
    IL_0056:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.m1
    IL_005e:  dup
    IL_005f:  stloc.0
    IL_0060:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
    IL_0065:  ldloca.s   V_2
    IL_0067:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006c:  stloc.1
    IL_006d:  ldarg.0
    IL_006e:  ldflda     ""T Program.<Call2>d__2<T>.item""
    IL_0073:  ldloc.1
    IL_0074:  constrained. ""T""
    IL_007a:  callvirt   ""void IMoveable.GetName(int)""
    IL_007f:  leave.s    IL_0098
  }
  catch System.Exception
  {
    IL_0081:  stloc.3
    IL_0082:  ldarg.0
    IL_0083:  ldc.i4.s   -2
    IL_0085:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
    IL_008a:  ldarg.0
    IL_008b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call2>d__2<T>.<>t__builder""
    IL_0090:  ldloc.3
    IL_0091:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0096:  leave.s    IL_00ab
  }
  IL_0098:  ldarg.0
  IL_0099:  ldc.i4.s   -2
  IL_009b:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
  IL_00a0:  ldarg.0
  IL_00a1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call2>d__2<T>.<>t__builder""
  IL_00a6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00ab:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Call_Struct_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    void GetName(int x);
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public void GetName(int x)
    {
        Console.WriteLine(""Position GetName for item '{0}'"", Name);
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Call1(item1);

        var item2 = new Item {Name = ""2""};
        await Call2(item2);
    }

    static async Task Call1<T>(T item) where T : struct, IMoveable
    {
        item.GetName(await GetOffsetAsync(GetOffset(ref item)));
    }

    static async Task Call2<T>(T item) where T : IMoveable
    {
        item.GetName(await GetOffsetAsync(GetOffset(ref item)));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position GetName for item '-1'
Position GetName for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Call1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      172 (0xac)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Call1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0049
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Call1>d__1<T>.item""
    IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0015:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_001a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0027:  brtrue.s   IL_0065
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call1>d__1<T>.<>u__1""
    IL_0039:  ldarg.0
    IL_003a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Call1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Call1>d__1<T>)""
    IL_0047:  leave.s    IL_00ab
    IL_0049:  ldarg.0
    IL_004a:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call1>d__1<T>.<>u__1""
    IL_004f:  stloc.2
    IL_0050:  ldarg.0
    IL_0051:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call1>d__1<T>.<>u__1""
    IL_0056:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.m1
    IL_005e:  dup
    IL_005f:  stloc.0
    IL_0060:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_0065:  ldloca.s   V_2
    IL_0067:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006c:  stloc.1
    IL_006d:  ldarg.0
    IL_006e:  ldflda     ""T Program.<Call1>d__1<T>.item""
    IL_0073:  ldloc.1
    IL_0074:  constrained. ""T""
    IL_007a:  callvirt   ""void IMoveable.GetName(int)""
    IL_007f:  leave.s    IL_0098
  }
  catch System.Exception
  {
    IL_0081:  stloc.3
    IL_0082:  ldarg.0
    IL_0083:  ldc.i4.s   -2
    IL_0085:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_008a:  ldarg.0
    IL_008b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
    IL_0090:  ldloc.3
    IL_0091:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0096:  leave.s    IL_00ab
  }
  IL_0098:  ldarg.0
  IL_0099:  ldc.i4.s   -2
  IL_009b:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
  IL_00a0:  ldarg.0
  IL_00a1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
  IL_00a6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00ab:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Call_Class_Async_02()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    void GetName(int x);
}

class Item : IMoveable
{
    public string Name {get; set;}

    public void GetName(int x)
    {
        Console.WriteLine(""Position GetName for item '{0}'"", Name);
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Call1(item1);

        var item2 = new Item {Name = ""2""};
        await Call2(item2);
    }

    static async Task Call1<T>(T item) where T : class, IMoveable
    {
        await Task.Yield();
        item.GetName(await GetOffsetAsync(GetOffset(ref item)));
    }

    static async Task Call2<T>(T item) where T : IMoveable
    {
        await Task.Yield();
        item.GetName(await GetOffsetAsync(GetOffset(ref item)));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position GetName for item '1'
Position GetName for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Call1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      300 (0x12c)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Call1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00bb
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Call1>d__1<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Call1>d__1<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Call1>d__1<T>)""
    IL_0046:  leave      IL_012b
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Call1>d__1<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Call1>d__1<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldarg.0
    IL_006f:  ldarg.0
    IL_0070:  ldfld      ""T Program.<Call1>d__1<T>.item""
    IL_0075:  stfld      ""T Program.<Call1>d__1<T>.<>7__wrap1""
    IL_007a:  ldarg.0
    IL_007b:  ldflda     ""T Program.<Call1>d__1<T>.item""
    IL_0080:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0085:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_008a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_008f:  stloc.s    V_4
    IL_0091:  ldloca.s   V_4
    IL_0093:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0098:  brtrue.s   IL_00d8
    IL_009a:  ldarg.0
    IL_009b:  ldc.i4.1
    IL_009c:  dup
    IL_009d:  stloc.0
    IL_009e:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_00a3:  ldarg.0
    IL_00a4:  ldloc.s    V_4
    IL_00a6:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call1>d__1<T>.<>u__2""
    IL_00ab:  ldarg.0
    IL_00ac:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
    IL_00b1:  ldloca.s   V_4
    IL_00b3:  ldarg.0
    IL_00b4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Call1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Call1>d__1<T>)""
    IL_00b9:  leave.s    IL_012b
    IL_00bb:  ldarg.0
    IL_00bc:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call1>d__1<T>.<>u__2""
    IL_00c1:  stloc.s    V_4
    IL_00c3:  ldarg.0
    IL_00c4:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call1>d__1<T>.<>u__2""
    IL_00c9:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00cf:  ldarg.0
    IL_00d0:  ldc.i4.m1
    IL_00d1:  dup
    IL_00d2:  stloc.0
    IL_00d3:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_00d8:  ldloca.s   V_4
    IL_00da:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00df:  stloc.3
    IL_00e0:  ldarg.0
    IL_00e1:  ldfld      ""T Program.<Call1>d__1<T>.<>7__wrap1""
    IL_00e6:  box        ""T""
    IL_00eb:  ldloc.3
    IL_00ec:  callvirt   ""void IMoveable.GetName(int)""
    IL_00f1:  ldarg.0
    IL_00f2:  ldflda     ""T Program.<Call1>d__1<T>.<>7__wrap1""
    IL_00f7:  initobj    ""T""
    IL_00fd:  leave.s    IL_0118
  }
  catch System.Exception
  {
    IL_00ff:  stloc.s    V_5
    IL_0101:  ldarg.0
    IL_0102:  ldc.i4.s   -2
    IL_0104:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_0109:  ldarg.0
    IL_010a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
    IL_010f:  ldloc.s    V_5
    IL_0111:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0116:  leave.s    IL_012b
  }
  IL_0118:  ldarg.0
  IL_0119:  ldc.i4.s   -2
  IL_011b:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
  IL_0120:  ldarg.0
  IL_0121:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
  IL_0126:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_012b:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Call2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      277 (0x115)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Call2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00af
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Call2>d__2<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call2>d__2<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Call2>d__2<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Call2>d__2<T>)""
    IL_0046:  leave      IL_0114
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Call2>d__2<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Call2>d__2<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldarg.0
    IL_006f:  ldflda     ""T Program.<Call2>d__2<T>.item""
    IL_0074:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0079:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_007e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0083:  stloc.s    V_4
    IL_0085:  ldloca.s   V_4
    IL_0087:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_008c:  brtrue.s   IL_00cc
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.1
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
    IL_0097:  ldarg.0
    IL_0098:  ldloc.s    V_4
    IL_009a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call2>d__2<T>.<>u__2""
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call2>d__2<T>.<>t__builder""
    IL_00a5:  ldloca.s   V_4
    IL_00a7:  ldarg.0
    IL_00a8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Call2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Call2>d__2<T>)""
    IL_00ad:  leave.s    IL_0114
    IL_00af:  ldarg.0
    IL_00b0:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call2>d__2<T>.<>u__2""
    IL_00b5:  stloc.s    V_4
    IL_00b7:  ldarg.0
    IL_00b8:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call2>d__2<T>.<>u__2""
    IL_00bd:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00c3:  ldarg.0
    IL_00c4:  ldc.i4.m1
    IL_00c5:  dup
    IL_00c6:  stloc.0
    IL_00c7:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
    IL_00cc:  ldloca.s   V_4
    IL_00ce:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00d3:  stloc.3
    IL_00d4:  ldarg.0
    IL_00d5:  ldflda     ""T Program.<Call2>d__2<T>.item""
    IL_00da:  ldloc.3
    IL_00db:  constrained. ""T""
    IL_00e1:  callvirt   ""void IMoveable.GetName(int)""
    IL_00e6:  leave.s    IL_0101
  }
  catch System.Exception
  {
    IL_00e8:  stloc.s    V_5
    IL_00ea:  ldarg.0
    IL_00eb:  ldc.i4.s   -2
    IL_00ed:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
    IL_00f2:  ldarg.0
    IL_00f3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call2>d__2<T>.<>t__builder""
    IL_00f8:  ldloc.s    V_5
    IL_00fa:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00ff:  leave.s    IL_0114
  }
  IL_0101:  ldarg.0
  IL_0102:  ldc.i4.s   -2
  IL_0104:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
  IL_0109:  ldarg.0
  IL_010a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call2>d__2<T>.<>t__builder""
  IL_010f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0114:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Call_Struct_Async_02()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    void GetName(int x);
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public void GetName(int x)
    {
        Console.WriteLine(""Position GetName for item '{0}'"", Name);
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Call1(item1);

        var item2 = new Item {Name = ""2""};
        await Call2(item2);
    }

    static async Task Call1<T>(T item) where T : struct, IMoveable
    {
        await Task.Yield();
        item.GetName(await GetOffsetAsync(GetOffset(ref item)));
    }

    static async Task Call2<T>(T item) where T : IMoveable
    {
        await Task.Yield();
        item.GetName(await GetOffsetAsync(GetOffset(ref item)));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position GetName for item '-1'
Position GetName for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Call1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      277 (0x115)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Call1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00af
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Call1>d__1<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Call1>d__1<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Call1>d__1<T>)""
    IL_0046:  leave      IL_0114
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Call1>d__1<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Call1>d__1<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldarg.0
    IL_006f:  ldflda     ""T Program.<Call1>d__1<T>.item""
    IL_0074:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0079:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_007e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0083:  stloc.s    V_4
    IL_0085:  ldloca.s   V_4
    IL_0087:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_008c:  brtrue.s   IL_00cc
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.1
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_0097:  ldarg.0
    IL_0098:  ldloc.s    V_4
    IL_009a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call1>d__1<T>.<>u__2""
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
    IL_00a5:  ldloca.s   V_4
    IL_00a7:  ldarg.0
    IL_00a8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Call1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Call1>d__1<T>)""
    IL_00ad:  leave.s    IL_0114
    IL_00af:  ldarg.0
    IL_00b0:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call1>d__1<T>.<>u__2""
    IL_00b5:  stloc.s    V_4
    IL_00b7:  ldarg.0
    IL_00b8:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call1>d__1<T>.<>u__2""
    IL_00bd:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00c3:  ldarg.0
    IL_00c4:  ldc.i4.m1
    IL_00c5:  dup
    IL_00c6:  stloc.0
    IL_00c7:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_00cc:  ldloca.s   V_4
    IL_00ce:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00d3:  stloc.3
    IL_00d4:  ldarg.0
    IL_00d5:  ldflda     ""T Program.<Call1>d__1<T>.item""
    IL_00da:  ldloc.3
    IL_00db:  constrained. ""T""
    IL_00e1:  callvirt   ""void IMoveable.GetName(int)""
    IL_00e6:  leave.s    IL_0101
  }
  catch System.Exception
  {
    IL_00e8:  stloc.s    V_5
    IL_00ea:  ldarg.0
    IL_00eb:  ldc.i4.s   -2
    IL_00ed:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_00f2:  ldarg.0
    IL_00f3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
    IL_00f8:  ldloc.s    V_5
    IL_00fa:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00ff:  leave.s    IL_0114
  }
  IL_0101:  ldarg.0
  IL_0102:  ldc.i4.s   -2
  IL_0104:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
  IL_0109:  ldarg.0
  IL_010a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
  IL_010f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0114:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Call_Conditional_Class()
        {
            var source = @"
using System;

interface IMoveable
{
    void GetName(int x);
}

class Item : IMoveable
{
    public string Name {get; set;}

    public void GetName(int x)
    {
        Console.WriteLine(""Position GetName for item '{0}'"", Name);
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Call1(item1);

        var item2 = new Item {Name = ""2""};
        Call2(item2);
    }

    static void Call1<T>(T item) where T : class, IMoveable
    {
        item?.GetName(GetOffset(ref item));
    }

    static void Call2<T>(T item) where T : IMoveable
    {
        item?.GetName(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            // Wrong output on some frameworks
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe/*, expectedOutput: @"
Position GetName for item '1'
Position GetName for item '2'
"*/).VerifyDiagnostics();

            verifier.VerifyIL("Program.Call1<T>",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_000b
  IL_0009:  pop
  IL_000a:  ret
  IL_000b:  ldarga.s   V_0
  IL_000d:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0012:  callvirt   ""void IMoveable.GetName(int)""
  IL_0017:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Call2<T>",
@"
{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  brfalse.s  IL_001c
  IL_0008:  ldarga.s   V_0
  IL_000a:  ldarga.s   V_0
  IL_000c:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0011:  constrained. ""T""
  IL_0017:  callvirt   ""void IMoveable.GetName(int)""
  IL_001c:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Call_Conditional_Struct()
        {
            var source = @"
using System;

interface IMoveable
{
    void GetName(int x);
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public void GetName(int x)
    {
        Console.WriteLine(""Position GetName for item '{0}'"", Name);
    }
}

class Program
{
    static void Main()
    {
        var item2 = new Item {Name = ""2""};
        Call2(item2);
    }

    static void Call2<T>(T item) where T : IMoveable
    {
        item?.GetName(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position GetName for item '-1'
").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Call_Conditional_Class_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    void GetName(int x);
}

class Item : IMoveable
{
    public string Name {get; set;}

    public void GetName(int x)
    {
        Console.WriteLine(""Position GetName for item '{0}'"", Name);
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Call1(ref item1);

        var item2 = new Item {Name = ""2""};
        Call2(ref item2);
    }

    static void Call1<T>(ref T item) where T : class, IMoveable
    {
        item?.GetName(GetOffset(ref item));
    }

    static void Call2<T>(ref T item) where T : IMoveable
    {
        item?.GetName(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position GetName for item '1'
Position GetName for item '2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Call1<T>",
@"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldobj      ""T""
  IL_0006:  box        ""T""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0010
  IL_000e:  pop
  IL_000f:  ret
  IL_0010:  ldarg.0
  IL_0011:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0016:  callvirt   ""void IMoveable.GetName(int)""
  IL_001b:  ret
}
");

            verifier.VerifyIL("Program.Call2<T>",
@"
{
  // Code size       53 (0x35)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    ""T""
  IL_0009:  ldloc.0
  IL_000a:  box        ""T""
  IL_000f:  brtrue.s   IL_0023
  IL_0011:  ldobj      ""T""
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldloc.0
  IL_001a:  box        ""T""
  IL_001f:  brtrue.s   IL_0023
  IL_0021:  pop
  IL_0022:  ret
  IL_0023:  ldarg.0
  IL_0024:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0029:  constrained. ""T""
  IL_002f:  callvirt   ""void IMoveable.GetName(int)""
  IL_0034:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Call_Conditional_Struct_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    void GetName(int x);
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public void GetName(int x)
    {
        Console.WriteLine(""Position GetName for item '{0}'"", Name);
    }
}

class Program
{
    static void Main()
    {
        var item2 = new Item {Name = ""2""};
        Call2(ref item2);
    }

    static void Call2<T>(ref T item) where T : IMoveable
    {
        item?.GetName(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position GetName for item '-1'
").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Call_Conditional_Class_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    void GetName(int x);
}

class Item : IMoveable
{
    public string Name {get; set;}

    public void GetName(int x)
    {
        Console.WriteLine(""Position GetName for item '{0}'"", Name);
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Call1(item1);

        var item2 = new Item {Name = ""2""};
        await Call2(item2);
    }

    static async Task Call1<T>(T item) where T : class, IMoveable
    {
        item?.GetName(await GetOffsetAsync(GetOffset(ref item)));
    }

    static async Task Call2<T>(T item) where T : IMoveable
    {
        item?.GetName(await GetOffsetAsync(GetOffset(ref item)));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position GetName for item '1'
Position GetName for item '2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Call1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      208 (0xd0)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Call1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0062
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""T Program.<Call1>d__1<T>.item""
    IL_0011:  stfld      ""T Program.<Call1>d__1<T>.<>7__wrap1""
    IL_0016:  ldarg.0
    IL_0017:  ldfld      ""T Program.<Call1>d__1<T>.<>7__wrap1""
    IL_001c:  box        ""T""
    IL_0021:  brfalse.s  IL_0097
    IL_0023:  ldarg.0
    IL_0024:  ldflda     ""T Program.<Call1>d__1<T>.item""
    IL_0029:  call       ""int Program.GetOffset<T>(ref T)""
    IL_002e:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0033:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0038:  stloc.2
    IL_0039:  ldloca.s   V_2
    IL_003b:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0040:  brtrue.s   IL_007e
    IL_0042:  ldarg.0
    IL_0043:  ldc.i4.0
    IL_0044:  dup
    IL_0045:  stloc.0
    IL_0046:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_004b:  ldarg.0
    IL_004c:  ldloc.2
    IL_004d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call1>d__1<T>.<>u__1""
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
    IL_0058:  ldloca.s   V_2
    IL_005a:  ldarg.0
    IL_005b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Call1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Call1>d__1<T>)""
    IL_0060:  leave.s    IL_00cf
    IL_0062:  ldarg.0
    IL_0063:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call1>d__1<T>.<>u__1""
    IL_0068:  stloc.2
    IL_0069:  ldarg.0
    IL_006a:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call1>d__1<T>.<>u__1""
    IL_006f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0075:  ldarg.0
    IL_0076:  ldc.i4.m1
    IL_0077:  dup
    IL_0078:  stloc.0
    IL_0079:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_007e:  ldloca.s   V_2
    IL_0080:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0085:  stloc.1
    IL_0086:  ldarg.0
    IL_0087:  ldfld      ""T Program.<Call1>d__1<T>.<>7__wrap1""
    IL_008c:  box        ""T""
    IL_0091:  ldloc.1
    IL_0092:  callvirt   ""void IMoveable.GetName(int)""
    IL_0097:  ldarg.0
    IL_0098:  ldflda     ""T Program.<Call1>d__1<T>.<>7__wrap1""
    IL_009d:  initobj    ""T""
    IL_00a3:  leave.s    IL_00bc
  }
  catch System.Exception
  {
    IL_00a5:  stloc.3
    IL_00a6:  ldarg.0
    IL_00a7:  ldc.i4.s   -2
    IL_00a9:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_00ae:  ldarg.0
    IL_00af:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
    IL_00b4:  ldloc.3
    IL_00b5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00ba:  leave.s    IL_00cf
  }
  IL_00bc:  ldarg.0
  IL_00bd:  ldc.i4.s   -2
  IL_00bf:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
  IL_00c4:  ldarg.0
  IL_00c5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
  IL_00ca:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00cf:  ret
}
");

            // Wrong IL ?
            verifier.VerifyIL("Program.<Call2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      257 (0x101)
  .maxstack  3
  .locals init (int V_0,
                T V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Call2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0078
    IL_000a:  ldloca.s   V_1
    IL_000c:  initobj    ""T""
    IL_0012:  ldloc.1
    IL_0013:  box        ""T""
    IL_0018:  brtrue.s   IL_0036
    IL_001a:  ldarg.0
    IL_001b:  ldarg.0
    IL_001c:  ldfld      ""T Program.<Call2>d__2<T>.item""
    IL_0021:  stfld      ""T Program.<Call2>d__2<T>.<>7__wrap1""
    IL_0026:  ldarg.0
    IL_0027:  ldfld      ""T Program.<Call2>d__2<T>.<>7__wrap1""
    IL_002c:  box        ""T""
    IL_0031:  brfalse    IL_00c6
    IL_0036:  ldarg.0
    IL_0037:  ldflda     ""T Program.<Call2>d__2<T>.item""
    IL_003c:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0041:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0046:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_004b:  stloc.3
    IL_004c:  ldloca.s   V_3
    IL_004e:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0053:  brtrue.s   IL_0094
    IL_0055:  ldarg.0
    IL_0056:  ldc.i4.0
    IL_0057:  dup
    IL_0058:  stloc.0
    IL_0059:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
    IL_005e:  ldarg.0
    IL_005f:  ldloc.3
    IL_0060:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call2>d__2<T>.<>u__1""
    IL_0065:  ldarg.0
    IL_0066:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call2>d__2<T>.<>t__builder""
    IL_006b:  ldloca.s   V_3
    IL_006d:  ldarg.0
    IL_006e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Call2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Call2>d__2<T>)""
    IL_0073:  leave      IL_0100
    IL_0078:  ldarg.0
    IL_0079:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call2>d__2<T>.<>u__1""
    IL_007e:  stloc.3
    IL_007f:  ldarg.0
    IL_0080:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call2>d__2<T>.<>u__1""
    IL_0085:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_008b:  ldarg.0
    IL_008c:  ldc.i4.m1
    IL_008d:  dup
    IL_008e:  stloc.0
    IL_008f:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
    IL_0094:  ldloca.s   V_3
    IL_0096:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_009b:  stloc.2
    IL_009c:  ldloca.s   V_1
    IL_009e:  initobj    ""T""
    IL_00a4:  ldloc.1
    IL_00a5:  box        ""T""
    IL_00aa:  brtrue.s   IL_00b4
    IL_00ac:  ldarg.0
    IL_00ad:  ldflda     ""T Program.<Call2>d__2<T>.<>7__wrap1""
    IL_00b2:  br.s       IL_00ba
    IL_00b4:  ldarg.0
    IL_00b5:  ldflda     ""T Program.<Call2>d__2<T>.item""
    IL_00ba:  ldloc.2
    IL_00bb:  constrained. ""T""
    IL_00c1:  callvirt   ""void IMoveable.GetName(int)""
    IL_00c6:  ldarg.0
    IL_00c7:  ldflda     ""T Program.<Call2>d__2<T>.<>7__wrap1""
    IL_00cc:  initobj    ""T""
    IL_00d2:  leave.s    IL_00ed
  }
  catch System.Exception
  {
    IL_00d4:  stloc.s    V_4
    IL_00d6:  ldarg.0
    IL_00d7:  ldc.i4.s   -2
    IL_00d9:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
    IL_00de:  ldarg.0
    IL_00df:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call2>d__2<T>.<>t__builder""
    IL_00e4:  ldloc.s    V_4
    IL_00e6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00eb:  leave.s    IL_0100
  }
  IL_00ed:  ldarg.0
  IL_00ee:  ldc.i4.s   -2
  IL_00f0:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
  IL_00f5:  ldarg.0
  IL_00f6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call2>d__2<T>.<>t__builder""
  IL_00fb:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0100:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Call_Conditional_Struct_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    void GetName(int x);
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public void GetName(int x)
    {
        Console.WriteLine(""Position GetName for item '{0}'"", Name);
    }
}

class Program
{
    static async Task Main()
    {
        var item2 = new Item {Name = ""2""};
        await Call2(item2);
    }

    static async Task Call2<T>(T item) where T : IMoveable
    {
        item?.GetName(await GetOffsetAsync(GetOffset(ref item)));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position GetName for item '-1'
").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Call_Conditional_Class_Async_02()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    void GetName(int x);
}

class Item : IMoveable
{
    public string Name {get; set;}

    public void GetName(int x)
    {
        Console.WriteLine(""Position GetName for item '{0}'"", Name);
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Call1(item1);

        var item2 = new Item {Name = ""2""};
        await Call2(item2);
    }

    static async Task Call1<T>(T item) where T : class, IMoveable
    {
        await Task.Yield();
        item?.GetName(await GetOffsetAsync(GetOffset(ref item)));
    }

    static async Task Call2<T>(T item) where T : IMoveable
    {
        await Task.Yield();
        item?.GetName(await GetOffsetAsync(GetOffset(ref item)));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position GetName for item '1'
Position GetName for item '2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Call1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      313 (0x139)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Call1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00c8
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Call1>d__1<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Call1>d__1<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Call1>d__1<T>)""
    IL_0046:  leave      IL_0138
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Call1>d__1<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Call1>d__1<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldarg.0
    IL_006f:  ldarg.0
    IL_0070:  ldfld      ""T Program.<Call1>d__1<T>.item""
    IL_0075:  stfld      ""T Program.<Call1>d__1<T>.<>7__wrap1""
    IL_007a:  ldarg.0
    IL_007b:  ldfld      ""T Program.<Call1>d__1<T>.<>7__wrap1""
    IL_0080:  box        ""T""
    IL_0085:  brfalse.s  IL_00fe
    IL_0087:  ldarg.0
    IL_0088:  ldflda     ""T Program.<Call1>d__1<T>.item""
    IL_008d:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0092:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0097:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_009c:  stloc.s    V_4
    IL_009e:  ldloca.s   V_4
    IL_00a0:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00a5:  brtrue.s   IL_00e5
    IL_00a7:  ldarg.0
    IL_00a8:  ldc.i4.1
    IL_00a9:  dup
    IL_00aa:  stloc.0
    IL_00ab:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_00b0:  ldarg.0
    IL_00b1:  ldloc.s    V_4
    IL_00b3:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call1>d__1<T>.<>u__2""
    IL_00b8:  ldarg.0
    IL_00b9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
    IL_00be:  ldloca.s   V_4
    IL_00c0:  ldarg.0
    IL_00c1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Call1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Call1>d__1<T>)""
    IL_00c6:  leave.s    IL_0138
    IL_00c8:  ldarg.0
    IL_00c9:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call1>d__1<T>.<>u__2""
    IL_00ce:  stloc.s    V_4
    IL_00d0:  ldarg.0
    IL_00d1:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call1>d__1<T>.<>u__2""
    IL_00d6:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00dc:  ldarg.0
    IL_00dd:  ldc.i4.m1
    IL_00de:  dup
    IL_00df:  stloc.0
    IL_00e0:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_00e5:  ldloca.s   V_4
    IL_00e7:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00ec:  stloc.3
    IL_00ed:  ldarg.0
    IL_00ee:  ldfld      ""T Program.<Call1>d__1<T>.<>7__wrap1""
    IL_00f3:  box        ""T""
    IL_00f8:  ldloc.3
    IL_00f9:  callvirt   ""void IMoveable.GetName(int)""
    IL_00fe:  ldarg.0
    IL_00ff:  ldflda     ""T Program.<Call1>d__1<T>.<>7__wrap1""
    IL_0104:  initobj    ""T""
    IL_010a:  leave.s    IL_0125
  }
  catch System.Exception
  {
    IL_010c:  stloc.s    V_5
    IL_010e:  ldarg.0
    IL_010f:  ldc.i4.s   -2
    IL_0111:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
    IL_0116:  ldarg.0
    IL_0117:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
    IL_011c:  ldloc.s    V_5
    IL_011e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0123:  leave.s    IL_0138
  }
  IL_0125:  ldarg.0
  IL_0126:  ldc.i4.s   -2
  IL_0128:  stfld      ""int Program.<Call1>d__1<T>.<>1__state""
  IL_012d:  ldarg.0
  IL_012e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call1>d__1<T>.<>t__builder""
  IL_0133:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0138:  ret
}
");

            // Wrong IL ?
            verifier.VerifyIL("Program.<Call2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      362 (0x16a)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                T V_3,
                int V_4,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Call2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00de
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Call2>d__2<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call2>d__2<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Call2>d__2<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Call2>d__2<T>)""
    IL_0046:  leave      IL_0169
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Call2>d__2<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Call2>d__2<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldloca.s   V_3
    IL_0070:  initobj    ""T""
    IL_0076:  ldloc.3
    IL_0077:  box        ""T""
    IL_007c:  brtrue.s   IL_009a
    IL_007e:  ldarg.0
    IL_007f:  ldarg.0
    IL_0080:  ldfld      ""T Program.<Call2>d__2<T>.item""
    IL_0085:  stfld      ""T Program.<Call2>d__2<T>.<>7__wrap1""
    IL_008a:  ldarg.0
    IL_008b:  ldfld      ""T Program.<Call2>d__2<T>.<>7__wrap1""
    IL_0090:  box        ""T""
    IL_0095:  brfalse    IL_012f
    IL_009a:  ldarg.0
    IL_009b:  ldflda     ""T Program.<Call2>d__2<T>.item""
    IL_00a0:  call       ""int Program.GetOffset<T>(ref T)""
    IL_00a5:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_00aa:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_00af:  stloc.s    V_5
    IL_00b1:  ldloca.s   V_5
    IL_00b3:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00b8:  brtrue.s   IL_00fb
    IL_00ba:  ldarg.0
    IL_00bb:  ldc.i4.1
    IL_00bc:  dup
    IL_00bd:  stloc.0
    IL_00be:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
    IL_00c3:  ldarg.0
    IL_00c4:  ldloc.s    V_5
    IL_00c6:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call2>d__2<T>.<>u__2""
    IL_00cb:  ldarg.0
    IL_00cc:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call2>d__2<T>.<>t__builder""
    IL_00d1:  ldloca.s   V_5
    IL_00d3:  ldarg.0
    IL_00d4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Call2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Call2>d__2<T>)""
    IL_00d9:  leave      IL_0169
    IL_00de:  ldarg.0
    IL_00df:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call2>d__2<T>.<>u__2""
    IL_00e4:  stloc.s    V_5
    IL_00e6:  ldarg.0
    IL_00e7:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Call2>d__2<T>.<>u__2""
    IL_00ec:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00f2:  ldarg.0
    IL_00f3:  ldc.i4.m1
    IL_00f4:  dup
    IL_00f5:  stloc.0
    IL_00f6:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
    IL_00fb:  ldloca.s   V_5
    IL_00fd:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0102:  stloc.s    V_4
    IL_0104:  ldloca.s   V_3
    IL_0106:  initobj    ""T""
    IL_010c:  ldloc.3
    IL_010d:  box        ""T""
    IL_0112:  brtrue.s   IL_011c
    IL_0114:  ldarg.0
    IL_0115:  ldflda     ""T Program.<Call2>d__2<T>.<>7__wrap1""
    IL_011a:  br.s       IL_0122
    IL_011c:  ldarg.0
    IL_011d:  ldflda     ""T Program.<Call2>d__2<T>.item""
    IL_0122:  ldloc.s    V_4
    IL_0124:  constrained. ""T""
    IL_012a:  callvirt   ""void IMoveable.GetName(int)""
    IL_012f:  ldarg.0
    IL_0130:  ldflda     ""T Program.<Call2>d__2<T>.<>7__wrap1""
    IL_0135:  initobj    ""T""
    IL_013b:  leave.s    IL_0156
  }
  catch System.Exception
  {
    IL_013d:  stloc.s    V_6
    IL_013f:  ldarg.0
    IL_0140:  ldc.i4.s   -2
    IL_0142:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
    IL_0147:  ldarg.0
    IL_0148:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call2>d__2<T>.<>t__builder""
    IL_014d:  ldloc.s    V_6
    IL_014f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0154:  leave.s    IL_0169
  }
  IL_0156:  ldarg.0
  IL_0157:  ldc.i4.s   -2
  IL_0159:  stfld      ""int Program.<Call2>d__2<T>.<>1__state""
  IL_015e:  ldarg.0
  IL_015f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Call2>d__2<T>.<>t__builder""
  IL_0164:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0169:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Call_Conditional_Struct_Async_02()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    void GetName(int x);
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public void GetName(int x)
    {
        Console.WriteLine(""Position GetName for item '{0}'"", Name);
    }
}

class Program
{
    static async Task Main()
    {
        var item2 = new Item {Name = ""2""};
        await Call2(item2);
    }

    static async Task Call2<T>(T item) where T : IMoveable
    {
        await Task.Yield();
        item?.GetName(await GetOffsetAsync(GetOffset(ref item)));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position GetName for item '-1'
").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Property_Class()
        {
            var source = @"
using System;

interface IMoveable
{
    int Position {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int Position
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(item1);

        var item2 = new Item {Name = ""2""};
        Shift2(item2);
    }

    static void Shift1<T>(T item) where T : class, IMoveable
    {
        item.Position += GetOffset(ref item);
    }

    static void Shift2<T>(T item) where T : IMoveable
    {
        item.Position += GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";
            // Wrong output on some frameworks
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe/*, expectedOutput: @"
Position get for item '1'
Position set for item '1'
Position get for item '2'
Position set for item '2'
"*/).VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       36 (0x24)
  .maxstack  3
  .locals init (T& V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  ldloc.0
  IL_0005:  constrained. ""T""
  IL_000b:  callvirt   ""int IMoveable.Position.get""
  IL_0010:  ldarga.s   V_0
  IL_0012:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0017:  add
  IL_0018:  constrained. ""T""
  IL_001e:  callvirt   ""void IMoveable.Position.set""
  IL_0023:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Shift2<T>",
@"
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarga.s   V_0
  IL_0002:  dup
  IL_0003:  constrained. ""T""
  IL_0009:  callvirt   ""int IMoveable.Position.get""
  IL_000e:  ldarga.s   V_0
  IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0015:  add
  IL_0016:  constrained. ""T""
  IL_001c:  callvirt   ""void IMoveable.Position.set""
  IL_0021:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Property_Struct()
        {
            var source = @"
using System;

interface IMoveable
{
    int Position {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int Position
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(item1);

        var item2 = new Item {Name = ""2""};
        Shift2(item2);
    }

    static void Shift1<T>(T item) where T : struct, IMoveable
    {
        item.Position += GetOffset(ref item);
    }

    static void Shift2<T>(T item) where T : IMoveable
    {
        item.Position += GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarga.s   V_0
  IL_0002:  dup
  IL_0003:  constrained. ""T""
  IL_0009:  callvirt   ""int IMoveable.Position.get""
  IL_000e:  ldarga.s   V_0
  IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0015:  add
  IL_0016:  constrained. ""T""
  IL_001c:  callvirt   ""void IMoveable.Position.set""
  IL_0021:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Property_Class_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    int Position {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int Position
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(ref item1);

        var item2 = new Item {Name = ""2""};
        Shift2(ref item2);
    }

    static void Shift1<T>(ref T item) where T : class, IMoveable
    {
        item.Position += GetOffset(ref item);
    }

    static void Shift2<T>(ref T item) where T : IMoveable
    {
        item.Position += GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            // Wrong output on some frameworks
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe/*, expectedOutput: @"
Position get for item '1'
Position set for item '1'
Position get for item '2'
Position set for item '2'
"*/).VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       34 (0x22)
  .maxstack  3
  .locals init (T& V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldloc.0
  IL_0004:  constrained. ""T""
  IL_000a:  callvirt   ""int IMoveable.Position.get""
  IL_000f:  ldarg.0
  IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0015:  add
  IL_0016:  constrained. ""T""
  IL_001c:  callvirt   ""void IMoveable.Position.set""
  IL_0021:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Shift2<T>",
@"
{
  // Code size       32 (0x20)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  constrained. ""T""
  IL_0008:  callvirt   ""int IMoveable.Position.get""
  IL_000d:  ldarg.0
  IL_000e:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0013:  add
  IL_0014:  constrained. ""T""
  IL_001a:  callvirt   ""void IMoveable.Position.set""
  IL_001f:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Property_Struct_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    int Position {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int Position
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(ref item1);

        var item2 = new Item {Name = ""2""};
        Shift2(ref item2);
    }

    static void Shift1<T>(ref T item) where T : struct, IMoveable
    {
        item.Position += GetOffset(ref item);
    }

    static void Shift2<T>(ref T item) where T : IMoveable
    {
        item.Position += GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       32 (0x20)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  constrained. ""T""
  IL_0008:  callvirt   ""int IMoveable.Position.get""
  IL_000d:  ldarg.0
  IL_000e:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0013:  add
  IL_0014:  constrained. ""T""
  IL_001a:  callvirt   ""void IMoveable.Position.set""
  IL_001f:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Property_Class_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int Position {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int Position
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : class, IMoveable
    {
        item.Position += await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item.Position += await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      229 (0xe5)
  .maxstack  3
  .locals init (int V_0,
                T& V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_006e
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0010:  stloc.1
    IL_0011:  ldarg.0
    IL_0012:  ldloc.1
    IL_0013:  ldobj      ""T""
    IL_0018:  stfld      ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_001d:  ldarg.0
    IL_001e:  ldloc.1
    IL_001f:  constrained. ""T""
    IL_0025:  callvirt   ""int IMoveable.Position.get""
    IL_002a:  stfld      ""int Program.<Shift1>d__1<T>.<>7__wrap2""
    IL_002f:  ldarg.0
    IL_0030:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0035:  call       ""int Program.GetOffset<T>(ref T)""
    IL_003a:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_003f:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0044:  stloc.3
    IL_0045:  ldloca.s   V_3
    IL_0047:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_004c:  brtrue.s   IL_008a
    IL_004e:  ldarg.0
    IL_004f:  ldc.i4.0
    IL_0050:  dup
    IL_0051:  stloc.0
    IL_0052:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0057:  ldarg.0
    IL_0058:  ldloc.3
    IL_0059:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_005e:  ldarg.0
    IL_005f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0064:  ldloca.s   V_3
    IL_0066:  ldarg.0
    IL_0067:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_006c:  leave.s    IL_00e4
    IL_006e:  ldarg.0
    IL_006f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0074:  stloc.3
    IL_0075:  ldarg.0
    IL_0076:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_007b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0081:  ldarg.0
    IL_0082:  ldc.i4.m1
    IL_0083:  dup
    IL_0084:  stloc.0
    IL_0085:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_008a:  ldloca.s   V_3
    IL_008c:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0091:  stloc.2
    IL_0092:  ldarg.0
    IL_0093:  ldfld      ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0098:  box        ""T""
    IL_009d:  ldarg.0
    IL_009e:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap2""
    IL_00a3:  ldloc.2
    IL_00a4:  add
    IL_00a5:  callvirt   ""void IMoveable.Position.set""
    IL_00aa:  ldarg.0
    IL_00ab:  ldflda     ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_00b0:  initobj    ""T""
    IL_00b6:  leave.s    IL_00d1
  }
  catch System.Exception
  {
    IL_00b8:  stloc.s    V_4
    IL_00ba:  ldarg.0
    IL_00bb:  ldc.i4.s   -2
    IL_00bd:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00c2:  ldarg.0
    IL_00c3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00c8:  ldloc.s    V_4
    IL_00ca:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00cf:  leave.s    IL_00e4
  }
  IL_00d1:  ldarg.0
  IL_00d2:  ldc.i4.s   -2
  IL_00d4:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00d9:  ldarg.0
  IL_00da:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00df:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00e4:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Shift2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      202 (0xca)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0060
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0011:  constrained. ""T""
    IL_0017:  callvirt   ""int IMoveable.Position.get""
    IL_001c:  stfld      ""int Program.<Shift2>d__2<T>.<>7__wrap1""
    IL_0021:  ldarg.0
    IL_0022:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0027:  call       ""int Program.GetOffset<T>(ref T)""
    IL_002c:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0031:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0036:  stloc.2
    IL_0037:  ldloca.s   V_2
    IL_0039:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_003e:  brtrue.s   IL_007c
    IL_0040:  ldarg.0
    IL_0041:  ldc.i4.0
    IL_0042:  dup
    IL_0043:  stloc.0
    IL_0044:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0049:  ldarg.0
    IL_004a:  ldloc.2
    IL_004b:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0050:  ldarg.0
    IL_0051:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_0056:  ldloca.s   V_2
    IL_0058:  ldarg.0
    IL_0059:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift2>d__2<T>)""
    IL_005e:  leave.s    IL_00c9
    IL_0060:  ldarg.0
    IL_0061:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0066:  stloc.2
    IL_0067:  ldarg.0
    IL_0068:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_006d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0073:  ldarg.0
    IL_0074:  ldc.i4.m1
    IL_0075:  dup
    IL_0076:  stloc.0
    IL_0077:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_007c:  ldloca.s   V_2
    IL_007e:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0083:  stloc.1
    IL_0084:  ldarg.0
    IL_0085:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_008a:  ldarg.0
    IL_008b:  ldfld      ""int Program.<Shift2>d__2<T>.<>7__wrap1""
    IL_0090:  ldloc.1
    IL_0091:  add
    IL_0092:  constrained. ""T""
    IL_0098:  callvirt   ""void IMoveable.Position.set""
    IL_009d:  leave.s    IL_00b6
  }
  catch System.Exception
  {
    IL_009f:  stloc.3
    IL_00a0:  ldarg.0
    IL_00a1:  ldc.i4.s   -2
    IL_00a3:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00a8:  ldarg.0
    IL_00a9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_00ae:  ldloc.3
    IL_00af:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00b4:  leave.s    IL_00c9
  }
  IL_00b6:  ldarg.0
  IL_00b7:  ldc.i4.s   -2
  IL_00b9:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_00be:  ldarg.0
  IL_00bf:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
  IL_00c4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c9:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Property_Struct_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int Position {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int Position
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : struct, IMoveable
    {
        item.Position += await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item.Position += await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      202 (0xca)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0060
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0011:  constrained. ""T""
    IL_0017:  callvirt   ""int IMoveable.Position.get""
    IL_001c:  stfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0021:  ldarg.0
    IL_0022:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0027:  call       ""int Program.GetOffset<T>(ref T)""
    IL_002c:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0031:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0036:  stloc.2
    IL_0037:  ldloca.s   V_2
    IL_0039:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_003e:  brtrue.s   IL_007c
    IL_0040:  ldarg.0
    IL_0041:  ldc.i4.0
    IL_0042:  dup
    IL_0043:  stloc.0
    IL_0044:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0049:  ldarg.0
    IL_004a:  ldloc.2
    IL_004b:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0050:  ldarg.0
    IL_0051:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0056:  ldloca.s   V_2
    IL_0058:  ldarg.0
    IL_0059:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_005e:  leave.s    IL_00c9
    IL_0060:  ldarg.0
    IL_0061:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0066:  stloc.2
    IL_0067:  ldarg.0
    IL_0068:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_006d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0073:  ldarg.0
    IL_0074:  ldc.i4.m1
    IL_0075:  dup
    IL_0076:  stloc.0
    IL_0077:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_007c:  ldloca.s   V_2
    IL_007e:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0083:  stloc.1
    IL_0084:  ldarg.0
    IL_0085:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_008a:  ldarg.0
    IL_008b:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0090:  ldloc.1
    IL_0091:  add
    IL_0092:  constrained. ""T""
    IL_0098:  callvirt   ""void IMoveable.Position.set""
    IL_009d:  leave.s    IL_00b6
  }
  catch System.Exception
  {
    IL_009f:  stloc.3
    IL_00a0:  ldarg.0
    IL_00a1:  ldc.i4.s   -2
    IL_00a3:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00a8:  ldarg.0
    IL_00a9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00ae:  ldloc.3
    IL_00af:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00b4:  leave.s    IL_00c9
  }
  IL_00b6:  ldarg.0
  IL_00b7:  ldc.i4.s   -2
  IL_00b9:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00be:  ldarg.0
  IL_00bf:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00c4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c9:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Property_Class_Async_02()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int Position {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int Position
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : class, IMoveable
    {
        await Task.Yield();
        item.Position += await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        await Task.Yield();
        item.Position += await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      334 (0x14e)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                T& V_3,
                int V_4,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00d4
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Shift1>d__1<T>)""
    IL_0046:  leave      IL_014d
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldarg.0
    IL_006f:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0074:  stloc.3
    IL_0075:  ldarg.0
    IL_0076:  ldloc.3
    IL_0077:  ldobj      ""T""
    IL_007c:  stfld      ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0081:  ldarg.0
    IL_0082:  ldloc.3
    IL_0083:  constrained. ""T""
    IL_0089:  callvirt   ""int IMoveable.Position.get""
    IL_008e:  stfld      ""int Program.<Shift1>d__1<T>.<>7__wrap2""
    IL_0093:  ldarg.0
    IL_0094:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0099:  call       ""int Program.GetOffset<T>(ref T)""
    IL_009e:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_00a3:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_00a8:  stloc.s    V_5
    IL_00aa:  ldloca.s   V_5
    IL_00ac:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00b1:  brtrue.s   IL_00f1
    IL_00b3:  ldarg.0
    IL_00b4:  ldc.i4.1
    IL_00b5:  dup
    IL_00b6:  stloc.0
    IL_00b7:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00bc:  ldarg.0
    IL_00bd:  ldloc.s    V_5
    IL_00bf:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_00c4:  ldarg.0
    IL_00c5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00ca:  ldloca.s   V_5
    IL_00cc:  ldarg.0
    IL_00cd:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_00d2:  leave.s    IL_014d
    IL_00d4:  ldarg.0
    IL_00d5:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_00da:  stloc.s    V_5
    IL_00dc:  ldarg.0
    IL_00dd:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_00e2:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00e8:  ldarg.0
    IL_00e9:  ldc.i4.m1
    IL_00ea:  dup
    IL_00eb:  stloc.0
    IL_00ec:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00f1:  ldloca.s   V_5
    IL_00f3:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00f8:  stloc.s    V_4
    IL_00fa:  ldarg.0
    IL_00fb:  ldfld      ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0100:  box        ""T""
    IL_0105:  ldarg.0
    IL_0106:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap2""
    IL_010b:  ldloc.s    V_4
    IL_010d:  add
    IL_010e:  callvirt   ""void IMoveable.Position.set""
    IL_0113:  ldarg.0
    IL_0114:  ldflda     ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0119:  initobj    ""T""
    IL_011f:  leave.s    IL_013a
  }
  catch System.Exception
  {
    IL_0121:  stloc.s    V_6
    IL_0123:  ldarg.0
    IL_0124:  ldc.i4.s   -2
    IL_0126:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_012b:  ldarg.0
    IL_012c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0131:  ldloc.s    V_6
    IL_0133:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0138:  leave.s    IL_014d
  }
  IL_013a:  ldarg.0
  IL_013b:  ldc.i4.s   -2
  IL_013d:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0142:  ldarg.0
  IL_0143:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_0148:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_014d:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Shift2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      307 (0x133)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00c6
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift2>d__2<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Shift2>d__2<T>)""
    IL_0046:  leave      IL_0132
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift2>d__2<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift2>d__2<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldarg.0
    IL_006f:  ldarg.0
    IL_0070:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0075:  constrained. ""T""
    IL_007b:  callvirt   ""int IMoveable.Position.get""
    IL_0080:  stfld      ""int Program.<Shift2>d__2<T>.<>7__wrap1""
    IL_0085:  ldarg.0
    IL_0086:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_008b:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0090:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0095:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_009a:  stloc.s    V_4
    IL_009c:  ldloca.s   V_4
    IL_009e:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00a3:  brtrue.s   IL_00e3
    IL_00a5:  ldarg.0
    IL_00a6:  ldc.i4.1
    IL_00a7:  dup
    IL_00a8:  stloc.0
    IL_00a9:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00ae:  ldarg.0
    IL_00af:  ldloc.s    V_4
    IL_00b1:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__2""
    IL_00b6:  ldarg.0
    IL_00b7:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_00bc:  ldloca.s   V_4
    IL_00be:  ldarg.0
    IL_00bf:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift2>d__2<T>)""
    IL_00c4:  leave.s    IL_0132
    IL_00c6:  ldarg.0
    IL_00c7:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__2""
    IL_00cc:  stloc.s    V_4
    IL_00ce:  ldarg.0
    IL_00cf:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__2""
    IL_00d4:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00da:  ldarg.0
    IL_00db:  ldc.i4.m1
    IL_00dc:  dup
    IL_00dd:  stloc.0
    IL_00de:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00e3:  ldloca.s   V_4
    IL_00e5:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00ea:  stloc.3
    IL_00eb:  ldarg.0
    IL_00ec:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_00f1:  ldarg.0
    IL_00f2:  ldfld      ""int Program.<Shift2>d__2<T>.<>7__wrap1""
    IL_00f7:  ldloc.3
    IL_00f8:  add
    IL_00f9:  constrained. ""T""
    IL_00ff:  callvirt   ""void IMoveable.Position.set""
    IL_0104:  leave.s    IL_011f
  }
  catch System.Exception
  {
    IL_0106:  stloc.s    V_5
    IL_0108:  ldarg.0
    IL_0109:  ldc.i4.s   -2
    IL_010b:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0110:  ldarg.0
    IL_0111:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_0116:  ldloc.s    V_5
    IL_0118:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_011d:  leave.s    IL_0132
  }
  IL_011f:  ldarg.0
  IL_0120:  ldc.i4.s   -2
  IL_0122:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0127:  ldarg.0
  IL_0128:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
  IL_012d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0132:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Property_Struct_Async_02()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int Position {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int Position
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : struct, IMoveable
    {
        await Task.Yield();
        item.Position += await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        await Task.Yield();
        item.Position += await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      307 (0x133)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00c6
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Shift1>d__1<T>)""
    IL_0046:  leave      IL_0132
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldarg.0
    IL_006f:  ldarg.0
    IL_0070:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0075:  constrained. ""T""
    IL_007b:  callvirt   ""int IMoveable.Position.get""
    IL_0080:  stfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0085:  ldarg.0
    IL_0086:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_008b:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0090:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0095:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_009a:  stloc.s    V_4
    IL_009c:  ldloca.s   V_4
    IL_009e:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00a3:  brtrue.s   IL_00e3
    IL_00a5:  ldarg.0
    IL_00a6:  ldc.i4.1
    IL_00a7:  dup
    IL_00a8:  stloc.0
    IL_00a9:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00ae:  ldarg.0
    IL_00af:  ldloc.s    V_4
    IL_00b1:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_00b6:  ldarg.0
    IL_00b7:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00bc:  ldloca.s   V_4
    IL_00be:  ldarg.0
    IL_00bf:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_00c4:  leave.s    IL_0132
    IL_00c6:  ldarg.0
    IL_00c7:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_00cc:  stloc.s    V_4
    IL_00ce:  ldarg.0
    IL_00cf:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_00d4:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00da:  ldarg.0
    IL_00db:  ldc.i4.m1
    IL_00dc:  dup
    IL_00dd:  stloc.0
    IL_00de:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00e3:  ldloca.s   V_4
    IL_00e5:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00ea:  stloc.3
    IL_00eb:  ldarg.0
    IL_00ec:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_00f1:  ldarg.0
    IL_00f2:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_00f7:  ldloc.3
    IL_00f8:  add
    IL_00f9:  constrained. ""T""
    IL_00ff:  callvirt   ""void IMoveable.Position.set""
    IL_0104:  leave.s    IL_011f
  }
  catch System.Exception
  {
    IL_0106:  stloc.s    V_5
    IL_0108:  ldarg.0
    IL_0109:  ldc.i4.s   -2
    IL_010b:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0110:  ldarg.0
    IL_0111:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0116:  ldloc.s    V_5
    IL_0118:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_011d:  leave.s    IL_0132
  }
  IL_011f:  ldarg.0
  IL_0120:  ldc.i4.s   -2
  IL_0122:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0127:  ldarg.0
  IL_0128:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_012d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0132:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Property_Class()
        {
            var source = @"
using System;

interface IMoveable
{
    int? Position {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int? Position
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(item1);

        var item2 = new Item {Name = ""2""};
        Shift2(item2);
    }

    static void Shift1<T>(T item) where T : class, IMoveable
    {
        item.Position ??= GetOffset(ref item);
    }

    static void Shift2<T>(T item) where T : IMoveable
    {
        item.Position ??= GetOffset(ref item);
    }
    
    static int value = 0;
    static int? GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       47 (0x2f)
  .maxstack  3
  .locals init (T& V_0,
                int? V_1,
                int? V_2)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  constrained. ""T""
  IL_000a:  callvirt   ""int? IMoveable.Position.get""
  IL_000f:  stloc.1
  IL_0010:  ldloca.s   V_1
  IL_0012:  call       ""bool int?.HasValue.get""
  IL_0017:  brtrue.s   IL_002e
  IL_0019:  ldloc.0
  IL_001a:  ldarga.s   V_0
  IL_001c:  call       ""int? Program.GetOffset<T>(ref T)""
  IL_0021:  dup
  IL_0022:  stloc.2
  IL_0023:  constrained. ""T""
  IL_0029:  callvirt   ""void IMoveable.Position.set""
  IL_002e:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Shift2<T>",
@"
{
  // Code size       47 (0x2f)
  .maxstack  3
  .locals init (T& V_0,
                int? V_1,
                int? V_2)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  constrained. ""T""
  IL_000a:  callvirt   ""int? IMoveable.Position.get""
  IL_000f:  stloc.1
  IL_0010:  ldloca.s   V_1
  IL_0012:  call       ""bool int?.HasValue.get""
  IL_0017:  brtrue.s   IL_002e
  IL_0019:  ldloc.0
  IL_001a:  ldarga.s   V_0
  IL_001c:  call       ""int? Program.GetOffset<T>(ref T)""
  IL_0021:  dup
  IL_0022:  stloc.2
  IL_0023:  constrained. ""T""
  IL_0029:  callvirt   ""void IMoveable.Position.set""
  IL_002e:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Property_Struct()
        {
            var source = @"
using System;

interface IMoveable
{
    int? Position {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int? Position
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(item1);

        var item2 = new Item {Name = ""2""};
        Shift2(item2);
    }

    static void Shift1<T>(T item) where T : struct, IMoveable
    {
        item.Position ??= GetOffset(ref item);
    }

    static void Shift2<T>(T item) where T : IMoveable
    {
        item.Position ??= GetOffset(ref item);
    }
    
    static int value = 0;
    static int? GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       47 (0x2f)
  .maxstack  3
  .locals init (T& V_0,
                int? V_1,
                int? V_2)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  constrained. ""T""
  IL_000a:  callvirt   ""int? IMoveable.Position.get""
  IL_000f:  stloc.1
  IL_0010:  ldloca.s   V_1
  IL_0012:  call       ""bool int?.HasValue.get""
  IL_0017:  brtrue.s   IL_002e
  IL_0019:  ldloc.0
  IL_001a:  ldarga.s   V_0
  IL_001c:  call       ""int? Program.GetOffset<T>(ref T)""
  IL_0021:  dup
  IL_0022:  stloc.2
  IL_0023:  constrained. ""T""
  IL_0029:  callvirt   ""void IMoveable.Position.set""
  IL_002e:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Property_Class_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    int? Position {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int? Position
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(ref item1);

        var item2 = new Item {Name = ""2""};
        Shift2(ref item2);
    }

    static void Shift1<T>(ref T item) where T : class, IMoveable
    {
        item.Position ??= GetOffset(ref item);
    }

    static void Shift2<T>(ref T item) where T : IMoveable
    {
        item.Position ??= GetOffset(ref item);
    }
    
    static int value = 0;
    static int? GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       45 (0x2d)
  .maxstack  3
  .locals init (T& V_0,
                int? V_1,
                int? V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  constrained. ""T""
  IL_0009:  callvirt   ""int? IMoveable.Position.get""
  IL_000e:  stloc.1
  IL_000f:  ldloca.s   V_1
  IL_0011:  call       ""bool int?.HasValue.get""
  IL_0016:  brtrue.s   IL_002c
  IL_0018:  ldloc.0
  IL_0019:  ldarg.0
  IL_001a:  call       ""int? Program.GetOffset<T>(ref T)""
  IL_001f:  dup
  IL_0020:  stloc.2
  IL_0021:  constrained. ""T""
  IL_0027:  callvirt   ""void IMoveable.Position.set""
  IL_002c:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Shift2<T>",
@"
{
  // Code size       45 (0x2d)
  .maxstack  3
  .locals init (T& V_0,
                int? V_1,
                int? V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  constrained. ""T""
  IL_0009:  callvirt   ""int? IMoveable.Position.get""
  IL_000e:  stloc.1
  IL_000f:  ldloca.s   V_1
  IL_0011:  call       ""bool int?.HasValue.get""
  IL_0016:  brtrue.s   IL_002c
  IL_0018:  ldloc.0
  IL_0019:  ldarg.0
  IL_001a:  call       ""int? Program.GetOffset<T>(ref T)""
  IL_001f:  dup
  IL_0020:  stloc.2
  IL_0021:  constrained. ""T""
  IL_0027:  callvirt   ""void IMoveable.Position.set""
  IL_002c:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Property_Struct_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    int? Position {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int? Position
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(ref item1);

        var item2 = new Item {Name = ""2""};
        Shift2(ref item2);
    }

    static void Shift1<T>(ref T item) where T : struct, IMoveable
    {
        item.Position ??= GetOffset(ref item);
    }

    static void Shift2<T>(ref T item) where T : IMoveable
    {
        item.Position ??= GetOffset(ref item);
    }
    
    static int value = 0;
    static int? GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       45 (0x2d)
  .maxstack  3
  .locals init (T& V_0,
                int? V_1,
                int? V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  constrained. ""T""
  IL_0009:  callvirt   ""int? IMoveable.Position.get""
  IL_000e:  stloc.1
  IL_000f:  ldloca.s   V_1
  IL_0011:  call       ""bool int?.HasValue.get""
  IL_0016:  brtrue.s   IL_002c
  IL_0018:  ldloc.0
  IL_0019:  ldarg.0
  IL_001a:  call       ""int? Program.GetOffset<T>(ref T)""
  IL_001f:  dup
  IL_0020:  stloc.2
  IL_0021:  constrained. ""T""
  IL_0027:  callvirt   ""void IMoveable.Position.set""
  IL_002c:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Property_Class_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int? Position {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int? Position
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : class, IMoveable
    {
        item.Position ??= await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item.Position ??= await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int? GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int?> GetOffsetAsync(int? i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      235 (0xeb)
  .maxstack  3
  .locals init (int V_0,
                T& V_1,
                int? V_2,
                int? V_3,
                int? V_4,
                System.Runtime.CompilerServices.TaskAwaiter<int?> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0077
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0010:  stloc.1
    IL_0011:  ldloc.1
    IL_0012:  constrained. ""T""
    IL_0018:  callvirt   ""int? IMoveable.Position.get""
    IL_001d:  stloc.2
    IL_001e:  ldloca.s   V_2
    IL_0020:  call       ""bool int?.HasValue.get""
    IL_0025:  brtrue     IL_00bc
    IL_002a:  ldarg.0
    IL_002b:  ldloc.1
    IL_002c:  ldobj      ""T""
    IL_0031:  stfld      ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0036:  ldarg.0
    IL_0037:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_003c:  call       ""int? Program.GetOffset<T>(ref T)""
    IL_0041:  call       ""System.Threading.Tasks.Task<int?> Program.GetOffsetAsync(int?)""
    IL_0046:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int?> System.Threading.Tasks.Task<int?>.GetAwaiter()""
    IL_004b:  stloc.s    V_5
    IL_004d:  ldloca.s   V_5
    IL_004f:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int?>.IsCompleted.get""
    IL_0054:  brtrue.s   IL_0094
    IL_0056:  ldarg.0
    IL_0057:  ldc.i4.0
    IL_0058:  dup
    IL_0059:  stloc.0
    IL_005a:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_005f:  ldarg.0
    IL_0060:  ldloc.s    V_5
    IL_0062:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int?> Program.<Shift1>d__1<T>.<>u__1""
    IL_0067:  ldarg.0
    IL_0068:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_006d:  ldloca.s   V_5
    IL_006f:  ldarg.0
    IL_0070:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int?>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int?>, ref Program.<Shift1>d__1<T>)""
    IL_0075:  leave.s    IL_00ea
    IL_0077:  ldarg.0
    IL_0078:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int?> Program.<Shift1>d__1<T>.<>u__1""
    IL_007d:  stloc.s    V_5
    IL_007f:  ldarg.0
    IL_0080:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int?> Program.<Shift1>d__1<T>.<>u__1""
    IL_0085:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int?>""
    IL_008b:  ldarg.0
    IL_008c:  ldc.i4.m1
    IL_008d:  dup
    IL_008e:  stloc.0
    IL_008f:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0094:  ldloca.s   V_5
    IL_0096:  call       ""int? System.Runtime.CompilerServices.TaskAwaiter<int?>.GetResult()""
    IL_009b:  stloc.3
    IL_009c:  ldarg.0
    IL_009d:  ldfld      ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_00a2:  box        ""T""
    IL_00a7:  ldloc.3
    IL_00a8:  dup
    IL_00a9:  stloc.s    V_4
    IL_00ab:  callvirt   ""void IMoveable.Position.set""
    IL_00b0:  ldarg.0
    IL_00b1:  ldflda     ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_00b6:  initobj    ""T""
    IL_00bc:  leave.s    IL_00d7
  }
  catch System.Exception
  {
    IL_00be:  stloc.s    V_6
    IL_00c0:  ldarg.0
    IL_00c1:  ldc.i4.s   -2
    IL_00c3:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00c8:  ldarg.0
    IL_00c9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00ce:  ldloc.s    V_6
    IL_00d0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00d5:  leave.s    IL_00ea
  }
  IL_00d7:  ldarg.0
  IL_00d8:  ldc.i4.s   -2
  IL_00da:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00df:  ldarg.0
  IL_00e0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00e5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00ea:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Shift2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      206 (0xce)
  .maxstack  3
  .locals init (int V_0,
                int? V_1,
                int? V_2,
                int? V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int?> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0066
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0010:  constrained. ""T""
    IL_0016:  callvirt   ""int? IMoveable.Position.get""
    IL_001b:  stloc.1
    IL_001c:  ldloca.s   V_1
    IL_001e:  call       ""bool int?.HasValue.get""
    IL_0023:  brtrue.s   IL_009f
    IL_0025:  ldarg.0
    IL_0026:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_002b:  call       ""int? Program.GetOffset<T>(ref T)""
    IL_0030:  call       ""System.Threading.Tasks.Task<int?> Program.GetOffsetAsync(int?)""
    IL_0035:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int?> System.Threading.Tasks.Task<int?>.GetAwaiter()""
    IL_003a:  stloc.s    V_4
    IL_003c:  ldloca.s   V_4
    IL_003e:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int?>.IsCompleted.get""
    IL_0043:  brtrue.s   IL_0083
    IL_0045:  ldarg.0
    IL_0046:  ldc.i4.0
    IL_0047:  dup
    IL_0048:  stloc.0
    IL_0049:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_004e:  ldarg.0
    IL_004f:  ldloc.s    V_4
    IL_0051:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int?> Program.<Shift2>d__2<T>.<>u__1""
    IL_0056:  ldarg.0
    IL_0057:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_005c:  ldloca.s   V_4
    IL_005e:  ldarg.0
    IL_005f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int?>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int?>, ref Program.<Shift2>d__2<T>)""
    IL_0064:  leave.s    IL_00cd
    IL_0066:  ldarg.0
    IL_0067:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int?> Program.<Shift2>d__2<T>.<>u__1""
    IL_006c:  stloc.s    V_4
    IL_006e:  ldarg.0
    IL_006f:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int?> Program.<Shift2>d__2<T>.<>u__1""
    IL_0074:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int?>""
    IL_007a:  ldarg.0
    IL_007b:  ldc.i4.m1
    IL_007c:  dup
    IL_007d:  stloc.0
    IL_007e:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0083:  ldloca.s   V_4
    IL_0085:  call       ""int? System.Runtime.CompilerServices.TaskAwaiter<int?>.GetResult()""
    IL_008a:  stloc.2
    IL_008b:  ldarg.0
    IL_008c:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0091:  ldloc.2
    IL_0092:  dup
    IL_0093:  stloc.3
    IL_0094:  constrained. ""T""
    IL_009a:  callvirt   ""void IMoveable.Position.set""
    IL_009f:  leave.s    IL_00ba
  }
  catch System.Exception
  {
    IL_00a1:  stloc.s    V_5
    IL_00a3:  ldarg.0
    IL_00a4:  ldc.i4.s   -2
    IL_00a6:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00ab:  ldarg.0
    IL_00ac:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_00b1:  ldloc.s    V_5
    IL_00b3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00b8:  leave.s    IL_00cd
  }
  IL_00ba:  ldarg.0
  IL_00bb:  ldc.i4.s   -2
  IL_00bd:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_00c2:  ldarg.0
  IL_00c3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
  IL_00c8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00cd:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Property_Struct_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int? Position {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int? Position
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : struct, IMoveable
    {
        item.Position ??= await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item.Position ??= await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int? GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int?> GetOffsetAsync(int? i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      206 (0xce)
  .maxstack  3
  .locals init (int V_0,
                int? V_1,
                int? V_2,
                int? V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int?> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0066
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0010:  constrained. ""T""
    IL_0016:  callvirt   ""int? IMoveable.Position.get""
    IL_001b:  stloc.1
    IL_001c:  ldloca.s   V_1
    IL_001e:  call       ""bool int?.HasValue.get""
    IL_0023:  brtrue.s   IL_009f
    IL_0025:  ldarg.0
    IL_0026:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_002b:  call       ""int? Program.GetOffset<T>(ref T)""
    IL_0030:  call       ""System.Threading.Tasks.Task<int?> Program.GetOffsetAsync(int?)""
    IL_0035:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int?> System.Threading.Tasks.Task<int?>.GetAwaiter()""
    IL_003a:  stloc.s    V_4
    IL_003c:  ldloca.s   V_4
    IL_003e:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int?>.IsCompleted.get""
    IL_0043:  brtrue.s   IL_0083
    IL_0045:  ldarg.0
    IL_0046:  ldc.i4.0
    IL_0047:  dup
    IL_0048:  stloc.0
    IL_0049:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_004e:  ldarg.0
    IL_004f:  ldloc.s    V_4
    IL_0051:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int?> Program.<Shift1>d__1<T>.<>u__1""
    IL_0056:  ldarg.0
    IL_0057:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_005c:  ldloca.s   V_4
    IL_005e:  ldarg.0
    IL_005f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int?>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int?>, ref Program.<Shift1>d__1<T>)""
    IL_0064:  leave.s    IL_00cd
    IL_0066:  ldarg.0
    IL_0067:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int?> Program.<Shift1>d__1<T>.<>u__1""
    IL_006c:  stloc.s    V_4
    IL_006e:  ldarg.0
    IL_006f:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int?> Program.<Shift1>d__1<T>.<>u__1""
    IL_0074:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int?>""
    IL_007a:  ldarg.0
    IL_007b:  ldc.i4.m1
    IL_007c:  dup
    IL_007d:  stloc.0
    IL_007e:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0083:  ldloca.s   V_4
    IL_0085:  call       ""int? System.Runtime.CompilerServices.TaskAwaiter<int?>.GetResult()""
    IL_008a:  stloc.2
    IL_008b:  ldarg.0
    IL_008c:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0091:  ldloc.2
    IL_0092:  dup
    IL_0093:  stloc.3
    IL_0094:  constrained. ""T""
    IL_009a:  callvirt   ""void IMoveable.Position.set""
    IL_009f:  leave.s    IL_00ba
  }
  catch System.Exception
  {
    IL_00a1:  stloc.s    V_5
    IL_00a3:  ldarg.0
    IL_00a4:  ldc.i4.s   -2
    IL_00a6:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00ab:  ldarg.0
    IL_00ac:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00b1:  ldloc.s    V_5
    IL_00b3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00b8:  leave.s    IL_00cd
  }
  IL_00ba:  ldarg.0
  IL_00bb:  ldc.i4.s   -2
  IL_00bd:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00c2:  ldarg.0
  IL_00c3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00c8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00cd:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Property_Class_Async_02()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int? Position {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int? Position
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : class, IMoveable
    {
        await Task.Yield();
        item.Position ??= await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        await Task.Yield();
        item.Position ??= await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int? GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int?> GetOffsetAsync(int? i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      338 (0x152)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                T& V_3,
                int? V_4,
                int? V_5,
                int? V_6,
                System.Runtime.CompilerServices.TaskAwaiter<int?> V_7,
                System.Exception V_8)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00dc
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Shift1>d__1<T>)""
    IL_0046:  leave      IL_0151
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldarg.0
    IL_006f:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0074:  stloc.3
    IL_0075:  ldloc.3
    IL_0076:  constrained. ""T""
    IL_007c:  callvirt   ""int? IMoveable.Position.get""
    IL_0081:  stloc.s    V_4
    IL_0083:  ldloca.s   V_4
    IL_0085:  call       ""bool int?.HasValue.get""
    IL_008a:  brtrue     IL_0123
    IL_008f:  ldarg.0
    IL_0090:  ldloc.3
    IL_0091:  ldobj      ""T""
    IL_0096:  stfld      ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_009b:  ldarg.0
    IL_009c:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_00a1:  call       ""int? Program.GetOffset<T>(ref T)""
    IL_00a6:  call       ""System.Threading.Tasks.Task<int?> Program.GetOffsetAsync(int?)""
    IL_00ab:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int?> System.Threading.Tasks.Task<int?>.GetAwaiter()""
    IL_00b0:  stloc.s    V_7
    IL_00b2:  ldloca.s   V_7
    IL_00b4:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int?>.IsCompleted.get""
    IL_00b9:  brtrue.s   IL_00f9
    IL_00bb:  ldarg.0
    IL_00bc:  ldc.i4.1
    IL_00bd:  dup
    IL_00be:  stloc.0
    IL_00bf:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00c4:  ldarg.0
    IL_00c5:  ldloc.s    V_7
    IL_00c7:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int?> Program.<Shift1>d__1<T>.<>u__2""
    IL_00cc:  ldarg.0
    IL_00cd:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00d2:  ldloca.s   V_7
    IL_00d4:  ldarg.0
    IL_00d5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int?>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int?>, ref Program.<Shift1>d__1<T>)""
    IL_00da:  leave.s    IL_0151
    IL_00dc:  ldarg.0
    IL_00dd:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int?> Program.<Shift1>d__1<T>.<>u__2""
    IL_00e2:  stloc.s    V_7
    IL_00e4:  ldarg.0
    IL_00e5:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int?> Program.<Shift1>d__1<T>.<>u__2""
    IL_00ea:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int?>""
    IL_00f0:  ldarg.0
    IL_00f1:  ldc.i4.m1
    IL_00f2:  dup
    IL_00f3:  stloc.0
    IL_00f4:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00f9:  ldloca.s   V_7
    IL_00fb:  call       ""int? System.Runtime.CompilerServices.TaskAwaiter<int?>.GetResult()""
    IL_0100:  stloc.s    V_5
    IL_0102:  ldarg.0
    IL_0103:  ldfld      ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0108:  box        ""T""
    IL_010d:  ldloc.s    V_5
    IL_010f:  dup
    IL_0110:  stloc.s    V_6
    IL_0112:  callvirt   ""void IMoveable.Position.set""
    IL_0117:  ldarg.0
    IL_0118:  ldflda     ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_011d:  initobj    ""T""
    IL_0123:  leave.s    IL_013e
  }
  catch System.Exception
  {
    IL_0125:  stloc.s    V_8
    IL_0127:  ldarg.0
    IL_0128:  ldc.i4.s   -2
    IL_012a:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_012f:  ldarg.0
    IL_0130:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0135:  ldloc.s    V_8
    IL_0137:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_013c:  leave.s    IL_0151
  }
  IL_013e:  ldarg.0
  IL_013f:  ldc.i4.s   -2
  IL_0141:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0146:  ldarg.0
  IL_0147:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_014c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0151:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Shift2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      309 (0x135)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                int? V_3,
                int? V_4,
                int? V_5,
                System.Runtime.CompilerServices.TaskAwaiter<int?> V_6,
                System.Exception V_7)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ca
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift2>d__2<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Shift2>d__2<T>)""
    IL_0046:  leave      IL_0134
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift2>d__2<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift2>d__2<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldarg.0
    IL_006f:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0074:  constrained. ""T""
    IL_007a:  callvirt   ""int? IMoveable.Position.get""
    IL_007f:  stloc.3
    IL_0080:  ldloca.s   V_3
    IL_0082:  call       ""bool int?.HasValue.get""
    IL_0087:  brtrue.s   IL_0106
    IL_0089:  ldarg.0
    IL_008a:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_008f:  call       ""int? Program.GetOffset<T>(ref T)""
    IL_0094:  call       ""System.Threading.Tasks.Task<int?> Program.GetOffsetAsync(int?)""
    IL_0099:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int?> System.Threading.Tasks.Task<int?>.GetAwaiter()""
    IL_009e:  stloc.s    V_6
    IL_00a0:  ldloca.s   V_6
    IL_00a2:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int?>.IsCompleted.get""
    IL_00a7:  brtrue.s   IL_00e7
    IL_00a9:  ldarg.0
    IL_00aa:  ldc.i4.1
    IL_00ab:  dup
    IL_00ac:  stloc.0
    IL_00ad:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00b2:  ldarg.0
    IL_00b3:  ldloc.s    V_6
    IL_00b5:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int?> Program.<Shift2>d__2<T>.<>u__2""
    IL_00ba:  ldarg.0
    IL_00bb:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_00c0:  ldloca.s   V_6
    IL_00c2:  ldarg.0
    IL_00c3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int?>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int?>, ref Program.<Shift2>d__2<T>)""
    IL_00c8:  leave.s    IL_0134
    IL_00ca:  ldarg.0
    IL_00cb:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int?> Program.<Shift2>d__2<T>.<>u__2""
    IL_00d0:  stloc.s    V_6
    IL_00d2:  ldarg.0
    IL_00d3:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int?> Program.<Shift2>d__2<T>.<>u__2""
    IL_00d8:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int?>""
    IL_00de:  ldarg.0
    IL_00df:  ldc.i4.m1
    IL_00e0:  dup
    IL_00e1:  stloc.0
    IL_00e2:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00e7:  ldloca.s   V_6
    IL_00e9:  call       ""int? System.Runtime.CompilerServices.TaskAwaiter<int?>.GetResult()""
    IL_00ee:  stloc.s    V_4
    IL_00f0:  ldarg.0
    IL_00f1:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_00f6:  ldloc.s    V_4
    IL_00f8:  dup
    IL_00f9:  stloc.s    V_5
    IL_00fb:  constrained. ""T""
    IL_0101:  callvirt   ""void IMoveable.Position.set""
    IL_0106:  leave.s    IL_0121
  }
  catch System.Exception
  {
    IL_0108:  stloc.s    V_7
    IL_010a:  ldarg.0
    IL_010b:  ldc.i4.s   -2
    IL_010d:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0112:  ldarg.0
    IL_0113:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_0118:  ldloc.s    V_7
    IL_011a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_011f:  leave.s    IL_0134
  }
  IL_0121:  ldarg.0
  IL_0122:  ldc.i4.s   -2
  IL_0124:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0129:  ldarg.0
  IL_012a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
  IL_012f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0134:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Property_Struct_Async_02()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int? Position {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int? Position
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : struct, IMoveable
    {
        await Task.Yield();
        item.Position ??= await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        await Task.Yield();
        item.Position ??= await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int? GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int?> GetOffsetAsync(int? i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      309 (0x135)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                int? V_3,
                int? V_4,
                int? V_5,
                System.Runtime.CompilerServices.TaskAwaiter<int?> V_6,
                System.Exception V_7)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ca
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Shift1>d__1<T>)""
    IL_0046:  leave      IL_0134
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldarg.0
    IL_006f:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0074:  constrained. ""T""
    IL_007a:  callvirt   ""int? IMoveable.Position.get""
    IL_007f:  stloc.3
    IL_0080:  ldloca.s   V_3
    IL_0082:  call       ""bool int?.HasValue.get""
    IL_0087:  brtrue.s   IL_0106
    IL_0089:  ldarg.0
    IL_008a:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_008f:  call       ""int? Program.GetOffset<T>(ref T)""
    IL_0094:  call       ""System.Threading.Tasks.Task<int?> Program.GetOffsetAsync(int?)""
    IL_0099:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int?> System.Threading.Tasks.Task<int?>.GetAwaiter()""
    IL_009e:  stloc.s    V_6
    IL_00a0:  ldloca.s   V_6
    IL_00a2:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int?>.IsCompleted.get""
    IL_00a7:  brtrue.s   IL_00e7
    IL_00a9:  ldarg.0
    IL_00aa:  ldc.i4.1
    IL_00ab:  dup
    IL_00ac:  stloc.0
    IL_00ad:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00b2:  ldarg.0
    IL_00b3:  ldloc.s    V_6
    IL_00b5:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int?> Program.<Shift1>d__1<T>.<>u__2""
    IL_00ba:  ldarg.0
    IL_00bb:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00c0:  ldloca.s   V_6
    IL_00c2:  ldarg.0
    IL_00c3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int?>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int?>, ref Program.<Shift1>d__1<T>)""
    IL_00c8:  leave.s    IL_0134
    IL_00ca:  ldarg.0
    IL_00cb:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int?> Program.<Shift1>d__1<T>.<>u__2""
    IL_00d0:  stloc.s    V_6
    IL_00d2:  ldarg.0
    IL_00d3:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int?> Program.<Shift1>d__1<T>.<>u__2""
    IL_00d8:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int?>""
    IL_00de:  ldarg.0
    IL_00df:  ldc.i4.m1
    IL_00e0:  dup
    IL_00e1:  stloc.0
    IL_00e2:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00e7:  ldloca.s   V_6
    IL_00e9:  call       ""int? System.Runtime.CompilerServices.TaskAwaiter<int?>.GetResult()""
    IL_00ee:  stloc.s    V_4
    IL_00f0:  ldarg.0
    IL_00f1:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_00f6:  ldloc.s    V_4
    IL_00f8:  dup
    IL_00f9:  stloc.s    V_5
    IL_00fb:  constrained. ""T""
    IL_0101:  callvirt   ""void IMoveable.Position.set""
    IL_0106:  leave.s    IL_0121
  }
  catch System.Exception
  {
    IL_0108:  stloc.s    V_7
    IL_010a:  ldarg.0
    IL_010b:  ldc.i4.s   -2
    IL_010d:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0112:  ldarg.0
    IL_0113:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0118:  ldloc.s    V_7
    IL_011a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_011f:  leave.s    IL_0134
  }
  IL_0121:  ldarg.0
  IL_0122:  ldc.i4.s   -2
  IL_0124:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0129:  ldarg.0
  IL_012a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_012f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0134:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Index()
        {
            var source = @"
using System;

interface IMoveable
{
    int this[int i] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(item1);

        var item2 = new Item {Name = ""2""};
        Shift2(item2);
    }

    static void Shift1<T>(T item) where T : class, IMoveable
    {
        item[GetOffset(ref item)] += 1;
    }

    static void Shift2<T>(T item) where T : IMoveable
    {
        item[GetOffset(ref item)] += 1;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";
            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       40 (0x28)
  .maxstack  4
  .locals init (T& V_0,
                int V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldarga.s   V_0
  IL_0005:  call       ""int Program.GetOffset<T>(ref T)""
  IL_000a:  stloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  ldloc.0
  IL_000e:  ldloc.1
  IL_000f:  constrained. ""T""
  IL_0015:  callvirt   ""int IMoveable.this[int].get""
  IL_001a:  ldc.i4.1
  IL_001b:  add
  IL_001c:  constrained. ""T""
  IL_0022:  callvirt   ""void IMoveable.this[int].set""
  IL_0027:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Shift2<T>",
@"
{
  // Code size       40 (0x28)
  .maxstack  4
  .locals init (T& V_0,
                int V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldarga.s   V_0
  IL_0005:  call       ""int Program.GetOffset<T>(ref T)""
  IL_000a:  stloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  ldloc.0
  IL_000e:  ldloc.1
  IL_000f:  constrained. ""T""
  IL_0015:  callvirt   ""int IMoveable.this[int].get""
  IL_001a:  ldc.i4.1
  IL_001b:  add
  IL_001c:  constrained. ""T""
  IL_0022:  callvirt   ""void IMoveable.this[int].set""
  IL_0027:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Index()
        {
            var source = @"
using System;

interface IMoveable
{
    int this[int i] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(item1);

        var item2 = new Item {Name = ""2""};
        Shift2(item2);
    }

    static void Shift1<T>(T item) where T : struct, IMoveable
    {
        item[GetOffset(ref item)] += 1;
    }

    static void Shift2<T>(T item) where T : IMoveable
    {
        item[GetOffset(ref item)] += 1;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       40 (0x28)
  .maxstack  4
  .locals init (T& V_0,
                int V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldarga.s   V_0
  IL_0005:  call       ""int Program.GetOffset<T>(ref T)""
  IL_000a:  stloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  ldloc.0
  IL_000e:  ldloc.1
  IL_000f:  constrained. ""T""
  IL_0015:  callvirt   ""int IMoveable.this[int].get""
  IL_001a:  ldc.i4.1
  IL_001b:  add
  IL_001c:  constrained. ""T""
  IL_0022:  callvirt   ""void IMoveable.this[int].set""
  IL_0027:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Index_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    int this[int i] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(ref item1);

        var item2 = new Item {Name = ""2""};
        Shift2(ref item2);
    }

    static void Shift1<T>(ref T item) where T : class, IMoveable
    {
        item[GetOffset(ref item)] += 1;
    }

    static void Shift2<T>(ref T item) where T : IMoveable
    {
        item[GetOffset(ref item)] += 1;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";
            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       38 (0x26)
  .maxstack  4
  .locals init (T& V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0008:  stloc.1
  IL_0009:  ldloc.0
  IL_000a:  ldloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  constrained. ""T""
  IL_0013:  callvirt   ""int IMoveable.this[int].get""
  IL_0018:  ldc.i4.1
  IL_0019:  add
  IL_001a:  constrained. ""T""
  IL_0020:  callvirt   ""void IMoveable.this[int].set""
  IL_0025:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Shift2<T>",
@"
{
  // Code size       38 (0x26)
  .maxstack  4
  .locals init (T& V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0008:  stloc.1
  IL_0009:  ldloc.0
  IL_000a:  ldloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  constrained. ""T""
  IL_0013:  callvirt   ""int IMoveable.this[int].get""
  IL_0018:  ldc.i4.1
  IL_0019:  add
  IL_001a:  constrained. ""T""
  IL_0020:  callvirt   ""void IMoveable.this[int].set""
  IL_0025:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Index_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    int this[int i] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(ref item1);

        var item2 = new Item {Name = ""2""};
        Shift2(ref item2);
    }

    static void Shift1<T>(ref T item) where T : struct, IMoveable
    {
        item[GetOffset(ref item)] += 1;
    }

    static void Shift2<T>(ref T item) where T : IMoveable
    {
        item[GetOffset(ref item)] += 1;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       38 (0x26)
  .maxstack  4
  .locals init (T& V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0008:  stloc.1
  IL_0009:  ldloc.0
  IL_000a:  ldloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  constrained. ""T""
  IL_0013:  callvirt   ""int IMoveable.this[int].get""
  IL_0018:  ldc.i4.1
  IL_0019:  add
  IL_001a:  constrained. ""T""
  IL_0020:  callvirt   ""void IMoveable.this[int].set""
  IL_0025:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Index_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int this[int i] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : class, IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] += 1;
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] += 1;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      190 (0xbe)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0049
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0015:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_001a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0027:  brtrue.s   IL_0065
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0039:  ldarg.0
    IL_003a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_0047:  leave.s    IL_00bd
    IL_0049:  ldarg.0
    IL_004a:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_004f:  stloc.2
    IL_0050:  ldarg.0
    IL_0051:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0056:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.m1
    IL_005e:  dup
    IL_005f:  stloc.0
    IL_0060:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0065:  ldloca.s   V_2
    IL_0067:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006c:  stloc.1
    IL_006d:  ldarg.0
    IL_006e:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_0073:  box        ""T""
    IL_0078:  ldloc.1
    IL_0079:  ldarg.0
    IL_007a:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_007f:  box        ""T""
    IL_0084:  ldloc.1
    IL_0085:  callvirt   ""int IMoveable.this[int].get""
    IL_008a:  ldc.i4.1
    IL_008b:  add
    IL_008c:  callvirt   ""void IMoveable.this[int].set""
    IL_0091:  leave.s    IL_00aa
  }
  catch System.Exception
  {
    IL_0093:  stloc.3
    IL_0094:  ldarg.0
    IL_0095:  ldc.i4.s   -2
    IL_0097:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_009c:  ldarg.0
    IL_009d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00a2:  ldloc.3
    IL_00a3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00a8:  leave.s    IL_00bd
  }
  IL_00aa:  ldarg.0
  IL_00ab:  ldc.i4.s   -2
  IL_00ad:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00b2:  ldarg.0
  IL_00b3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00b8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00bd:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Shift2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      192 (0xc0)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0049
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0015:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_001a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0027:  brtrue.s   IL_0065
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0039:  ldarg.0
    IL_003a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift2>d__2<T>)""
    IL_0047:  leave.s    IL_00bf
    IL_0049:  ldarg.0
    IL_004a:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_004f:  stloc.2
    IL_0050:  ldarg.0
    IL_0051:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0056:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.m1
    IL_005e:  dup
    IL_005f:  stloc.0
    IL_0060:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0065:  ldloca.s   V_2
    IL_0067:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006c:  stloc.1
    IL_006d:  ldarg.0
    IL_006e:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0073:  ldloc.1
    IL_0074:  ldarg.0
    IL_0075:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_007a:  ldloc.1
    IL_007b:  constrained. ""T""
    IL_0081:  callvirt   ""int IMoveable.this[int].get""
    IL_0086:  ldc.i4.1
    IL_0087:  add
    IL_0088:  constrained. ""T""
    IL_008e:  callvirt   ""void IMoveable.this[int].set""
    IL_0093:  leave.s    IL_00ac
  }
  catch System.Exception
  {
    IL_0095:  stloc.3
    IL_0096:  ldarg.0
    IL_0097:  ldc.i4.s   -2
    IL_0099:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_009e:  ldarg.0
    IL_009f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_00a4:  ldloc.3
    IL_00a5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00aa:  leave.s    IL_00bf
  }
  IL_00ac:  ldarg.0
  IL_00ad:  ldc.i4.s   -2
  IL_00af:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_00b4:  ldarg.0
  IL_00b5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
  IL_00ba:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00bf:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Index_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int this[int i] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : struct, IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] += 1;
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] += 1;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      192 (0xc0)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0049
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0015:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_001a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0027:  brtrue.s   IL_0065
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0039:  ldarg.0
    IL_003a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_0047:  leave.s    IL_00bf
    IL_0049:  ldarg.0
    IL_004a:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_004f:  stloc.2
    IL_0050:  ldarg.0
    IL_0051:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0056:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.m1
    IL_005e:  dup
    IL_005f:  stloc.0
    IL_0060:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0065:  ldloca.s   V_2
    IL_0067:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006c:  stloc.1
    IL_006d:  ldarg.0
    IL_006e:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0073:  ldloc.1
    IL_0074:  ldarg.0
    IL_0075:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_007a:  ldloc.1
    IL_007b:  constrained. ""T""
    IL_0081:  callvirt   ""int IMoveable.this[int].get""
    IL_0086:  ldc.i4.1
    IL_0087:  add
    IL_0088:  constrained. ""T""
    IL_008e:  callvirt   ""void IMoveable.this[int].set""
    IL_0093:  leave.s    IL_00ac
  }
  catch System.Exception
  {
    IL_0095:  stloc.3
    IL_0096:  ldarg.0
    IL_0097:  ldc.i4.s   -2
    IL_0099:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_009e:  ldarg.0
    IL_009f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00a4:  ldloc.3
    IL_00a5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00aa:  leave.s    IL_00bf
  }
  IL_00ac:  ldarg.0
  IL_00ad:  ldc.i4.s   -2
  IL_00af:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00b4:  ldarg.0
  IL_00b5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00ba:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00bf:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Index_Async_02()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int this[int i] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : class, IMoveable
    {
        await Task.Yield();
        item[await GetOffsetAsync(GetOffset(ref item))] += 1;
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        await Task.Yield();
        item[await GetOffsetAsync(GetOffset(ref item))] += 1;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      295 (0x127)
  .maxstack  4
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00af
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Shift1>d__1<T>)""
    IL_0046:  leave      IL_0126
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldarg.0
    IL_006f:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0074:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0079:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_007e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0083:  stloc.s    V_4
    IL_0085:  ldloca.s   V_4
    IL_0087:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_008c:  brtrue.s   IL_00cc
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.1
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0097:  ldarg.0
    IL_0098:  ldloc.s    V_4
    IL_009a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00a5:  ldloca.s   V_4
    IL_00a7:  ldarg.0
    IL_00a8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_00ad:  leave.s    IL_0126
    IL_00af:  ldarg.0
    IL_00b0:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_00b5:  stloc.s    V_4
    IL_00b7:  ldarg.0
    IL_00b8:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_00bd:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00c3:  ldarg.0
    IL_00c4:  ldc.i4.m1
    IL_00c5:  dup
    IL_00c6:  stloc.0
    IL_00c7:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00cc:  ldloca.s   V_4
    IL_00ce:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00d3:  stloc.3
    IL_00d4:  ldarg.0
    IL_00d5:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_00da:  box        ""T""
    IL_00df:  ldloc.3
    IL_00e0:  ldarg.0
    IL_00e1:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_00e6:  box        ""T""
    IL_00eb:  ldloc.3
    IL_00ec:  callvirt   ""int IMoveable.this[int].get""
    IL_00f1:  ldc.i4.1
    IL_00f2:  add
    IL_00f3:  callvirt   ""void IMoveable.this[int].set""
    IL_00f8:  leave.s    IL_0113
  }
  catch System.Exception
  {
    IL_00fa:  stloc.s    V_5
    IL_00fc:  ldarg.0
    IL_00fd:  ldc.i4.s   -2
    IL_00ff:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0104:  ldarg.0
    IL_0105:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_010a:  ldloc.s    V_5
    IL_010c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0111:  leave.s    IL_0126
  }
  IL_0113:  ldarg.0
  IL_0114:  ldc.i4.s   -2
  IL_0116:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_011b:  ldarg.0
  IL_011c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_0121:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0126:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Shift2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      297 (0x129)
  .maxstack  4
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00af
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift2>d__2<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Shift2>d__2<T>)""
    IL_0046:  leave      IL_0128
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift2>d__2<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift2>d__2<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldarg.0
    IL_006f:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0074:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0079:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_007e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0083:  stloc.s    V_4
    IL_0085:  ldloca.s   V_4
    IL_0087:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_008c:  brtrue.s   IL_00cc
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.1
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0097:  ldarg.0
    IL_0098:  ldloc.s    V_4
    IL_009a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__2""
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_00a5:  ldloca.s   V_4
    IL_00a7:  ldarg.0
    IL_00a8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift2>d__2<T>)""
    IL_00ad:  leave.s    IL_0128
    IL_00af:  ldarg.0
    IL_00b0:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__2""
    IL_00b5:  stloc.s    V_4
    IL_00b7:  ldarg.0
    IL_00b8:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__2""
    IL_00bd:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00c3:  ldarg.0
    IL_00c4:  ldc.i4.m1
    IL_00c5:  dup
    IL_00c6:  stloc.0
    IL_00c7:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00cc:  ldloca.s   V_4
    IL_00ce:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00d3:  stloc.3
    IL_00d4:  ldarg.0
    IL_00d5:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_00da:  ldloc.3
    IL_00db:  ldarg.0
    IL_00dc:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_00e1:  ldloc.3
    IL_00e2:  constrained. ""T""
    IL_00e8:  callvirt   ""int IMoveable.this[int].get""
    IL_00ed:  ldc.i4.1
    IL_00ee:  add
    IL_00ef:  constrained. ""T""
    IL_00f5:  callvirt   ""void IMoveable.this[int].set""
    IL_00fa:  leave.s    IL_0115
  }
  catch System.Exception
  {
    IL_00fc:  stloc.s    V_5
    IL_00fe:  ldarg.0
    IL_00ff:  ldc.i4.s   -2
    IL_0101:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0106:  ldarg.0
    IL_0107:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_010c:  ldloc.s    V_5
    IL_010e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0113:  leave.s    IL_0128
  }
  IL_0115:  ldarg.0
  IL_0116:  ldc.i4.s   -2
  IL_0118:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_011d:  ldarg.0
  IL_011e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
  IL_0123:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0128:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Index_Async_02()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int this[int i] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : struct, IMoveable
    {
        await Task.Yield();
        item[await GetOffsetAsync(GetOffset(ref item))] += 1;
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        await Task.Yield();
        item[await GetOffsetAsync(GetOffset(ref item))] += 1;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      297 (0x129)
  .maxstack  4
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00af
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Shift1>d__1<T>)""
    IL_0046:  leave      IL_0128
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldarg.0
    IL_006f:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0074:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0079:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_007e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0083:  stloc.s    V_4
    IL_0085:  ldloca.s   V_4
    IL_0087:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_008c:  brtrue.s   IL_00cc
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.1
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0097:  ldarg.0
    IL_0098:  ldloc.s    V_4
    IL_009a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00a5:  ldloca.s   V_4
    IL_00a7:  ldarg.0
    IL_00a8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_00ad:  leave.s    IL_0128
    IL_00af:  ldarg.0
    IL_00b0:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_00b5:  stloc.s    V_4
    IL_00b7:  ldarg.0
    IL_00b8:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_00bd:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00c3:  ldarg.0
    IL_00c4:  ldc.i4.m1
    IL_00c5:  dup
    IL_00c6:  stloc.0
    IL_00c7:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00cc:  ldloca.s   V_4
    IL_00ce:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00d3:  stloc.3
    IL_00d4:  ldarg.0
    IL_00d5:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_00da:  ldloc.3
    IL_00db:  ldarg.0
    IL_00dc:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_00e1:  ldloc.3
    IL_00e2:  constrained. ""T""
    IL_00e8:  callvirt   ""int IMoveable.this[int].get""
    IL_00ed:  ldc.i4.1
    IL_00ee:  add
    IL_00ef:  constrained. ""T""
    IL_00f5:  callvirt   ""void IMoveable.this[int].set""
    IL_00fa:  leave.s    IL_0115
  }
  catch System.Exception
  {
    IL_00fc:  stloc.s    V_5
    IL_00fe:  ldarg.0
    IL_00ff:  ldc.i4.s   -2
    IL_0101:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0106:  ldarg.0
    IL_0107:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_010c:  ldloc.s    V_5
    IL_010e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0113:  leave.s    IL_0128
  }
  IL_0115:  ldarg.0
  IL_0116:  ldc.i4.s   -2
  IL_0118:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_011d:  ldarg.0
  IL_011e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_0123:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0128:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Increment_Indexer_Class()
        {
            var source = @"
using System;

interface IMoveable
{
    int this[int i] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(item1);

        var item2 = new Item {Name = ""2""};
        Shift2(item2);
    }

    static void Shift1<T>(T item) where T : class, IMoveable
    {
        item[GetOffset(ref item)] ++;
    }

    static void Shift2<T>(T item) where T : IMoveable
    {
        item[GetOffset(ref item)] ++;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";
            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       40 (0x28)
  .maxstack  4
  .locals init (int V_0,
                int V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0009:  stloc.0
  IL_000a:  dup
  IL_000b:  ldloc.0
  IL_000c:  constrained. ""T""
  IL_0012:  callvirt   ""int IMoveable.this[int].get""
  IL_0017:  stloc.1
  IL_0018:  ldloc.0
  IL_0019:  ldloc.1
  IL_001a:  ldc.i4.1
  IL_001b:  add
  IL_001c:  constrained. ""T""
  IL_0022:  callvirt   ""void IMoveable.this[int].set""
  IL_0027:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Shift2<T>",
@"
{
  // Code size       40 (0x28)
  .maxstack  4
  .locals init (int V_0,
                int V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0009:  stloc.0
  IL_000a:  dup
  IL_000b:  ldloc.0
  IL_000c:  constrained. ""T""
  IL_0012:  callvirt   ""int IMoveable.this[int].get""
  IL_0017:  stloc.1
  IL_0018:  ldloc.0
  IL_0019:  ldloc.1
  IL_001a:  ldc.i4.1
  IL_001b:  add
  IL_001c:  constrained. ""T""
  IL_0022:  callvirt   ""void IMoveable.this[int].set""
  IL_0027:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Increment_Indexer_Struct()
        {
            var source = @"
using System;

interface IMoveable
{
    int this[int i] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(item1);

        var item2 = new Item {Name = ""2""};
        Shift2(item2);
    }

    static void Shift1<T>(T item) where T : struct, IMoveable
    {
        item[GetOffset(ref item)] ++;
    }

    static void Shift2<T>(T item) where T : IMoveable
    {
        item[GetOffset(ref item)] ++;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       40 (0x28)
  .maxstack  4
  .locals init (int V_0,
                int V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0009:  stloc.0
  IL_000a:  dup
  IL_000b:  ldloc.0
  IL_000c:  constrained. ""T""
  IL_0012:  callvirt   ""int IMoveable.this[int].get""
  IL_0017:  stloc.1
  IL_0018:  ldloc.0
  IL_0019:  ldloc.1
  IL_001a:  ldc.i4.1
  IL_001b:  add
  IL_001c:  constrained. ""T""
  IL_0022:  callvirt   ""void IMoveable.this[int].set""
  IL_0027:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Increment_Indexer_Class_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    int this[int i] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(ref item1);

        var item2 = new Item {Name = ""2""};
        Shift2(ref item2);
    }

    static void Shift1<T>(ref T item) where T : class, IMoveable
    {
        item[GetOffset(ref item)] ++;
    }

    static void Shift2<T>(ref T item) where T : IMoveable
    {
        item[GetOffset(ref item)] ++;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";
            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       38 (0x26)
  .maxstack  4
  .locals init (int V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0007:  stloc.0
  IL_0008:  dup
  IL_0009:  ldloc.0
  IL_000a:  constrained. ""T""
  IL_0010:  callvirt   ""int IMoveable.this[int].get""
  IL_0015:  stloc.1
  IL_0016:  ldloc.0
  IL_0017:  ldloc.1
  IL_0018:  ldc.i4.1
  IL_0019:  add
  IL_001a:  constrained. ""T""
  IL_0020:  callvirt   ""void IMoveable.this[int].set""
  IL_0025:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Shift2<T>",
@"
{
  // Code size       38 (0x26)
  .maxstack  4
  .locals init (int V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0007:  stloc.0
  IL_0008:  dup
  IL_0009:  ldloc.0
  IL_000a:  constrained. ""T""
  IL_0010:  callvirt   ""int IMoveable.this[int].get""
  IL_0015:  stloc.1
  IL_0016:  ldloc.0
  IL_0017:  ldloc.1
  IL_0018:  ldc.i4.1
  IL_0019:  add
  IL_001a:  constrained. ""T""
  IL_0020:  callvirt   ""void IMoveable.this[int].set""
  IL_0025:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Increment_Indexer_Struct_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    int this[int i] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(ref item1);

        var item2 = new Item {Name = ""2""};
        Shift2(ref item2);
    }

    static void Shift1<T>(ref T item) where T : struct, IMoveable
    {
        item[GetOffset(ref item)] ++;
    }

    static void Shift2<T>(ref T item) where T : IMoveable
    {
        item[GetOffset(ref item)] ++;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       38 (0x26)
  .maxstack  4
  .locals init (int V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0007:  stloc.0
  IL_0008:  dup
  IL_0009:  ldloc.0
  IL_000a:  constrained. ""T""
  IL_0010:  callvirt   ""int IMoveable.this[int].get""
  IL_0015:  stloc.1
  IL_0016:  ldloc.0
  IL_0017:  ldloc.1
  IL_0018:  ldc.i4.1
  IL_0019:  add
  IL_001a:  constrained. ""T""
  IL_0020:  callvirt   ""void IMoveable.this[int].set""
  IL_0025:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Increment_Indexer_Class_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int this[int i] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : class, IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] ++;
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] ++;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      194 (0xc2)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0049
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0015:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_001a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_001f:  stloc.3
    IL_0020:  ldloca.s   V_3
    IL_0022:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0027:  brtrue.s   IL_0065
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0032:  ldarg.0
    IL_0033:  ldloc.3
    IL_0034:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0039:  ldarg.0
    IL_003a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_003f:  ldloca.s   V_3
    IL_0041:  ldarg.0
    IL_0042:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_0047:  leave.s    IL_00c1
    IL_0049:  ldarg.0
    IL_004a:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_004f:  stloc.3
    IL_0050:  ldarg.0
    IL_0051:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0056:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.m1
    IL_005e:  dup
    IL_005f:  stloc.0
    IL_0060:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0065:  ldloca.s   V_3
    IL_0067:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006c:  stloc.1
    IL_006d:  ldarg.0
    IL_006e:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_0073:  box        ""T""
    IL_0078:  ldloc.1
    IL_0079:  callvirt   ""int IMoveable.this[int].get""
    IL_007e:  stloc.2
    IL_007f:  ldarg.0
    IL_0080:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_0085:  box        ""T""
    IL_008a:  ldloc.1
    IL_008b:  ldloc.2
    IL_008c:  ldc.i4.1
    IL_008d:  add
    IL_008e:  callvirt   ""void IMoveable.this[int].set""
    IL_0093:  leave.s    IL_00ae
  }
  catch System.Exception
  {
    IL_0095:  stloc.s    V_4
    IL_0097:  ldarg.0
    IL_0098:  ldc.i4.s   -2
    IL_009a:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00a5:  ldloc.s    V_4
    IL_00a7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00ac:  leave.s    IL_00c1
  }
  IL_00ae:  ldarg.0
  IL_00af:  ldc.i4.s   -2
  IL_00b1:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00b6:  ldarg.0
  IL_00b7:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00bc:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c1:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Shift2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      196 (0xc4)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0049
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0015:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_001a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_001f:  stloc.3
    IL_0020:  ldloca.s   V_3
    IL_0022:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0027:  brtrue.s   IL_0065
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0032:  ldarg.0
    IL_0033:  ldloc.3
    IL_0034:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0039:  ldarg.0
    IL_003a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_003f:  ldloca.s   V_3
    IL_0041:  ldarg.0
    IL_0042:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift2>d__2<T>)""
    IL_0047:  leave.s    IL_00c3
    IL_0049:  ldarg.0
    IL_004a:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_004f:  stloc.3
    IL_0050:  ldarg.0
    IL_0051:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0056:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.m1
    IL_005e:  dup
    IL_005f:  stloc.0
    IL_0060:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0065:  ldloca.s   V_3
    IL_0067:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006c:  stloc.1
    IL_006d:  ldarg.0
    IL_006e:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0073:  ldloc.1
    IL_0074:  constrained. ""T""
    IL_007a:  callvirt   ""int IMoveable.this[int].get""
    IL_007f:  stloc.2
    IL_0080:  ldarg.0
    IL_0081:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0086:  ldloc.1
    IL_0087:  ldloc.2
    IL_0088:  ldc.i4.1
    IL_0089:  add
    IL_008a:  constrained. ""T""
    IL_0090:  callvirt   ""void IMoveable.this[int].set""
    IL_0095:  leave.s    IL_00b0
  }
  catch System.Exception
  {
    IL_0097:  stloc.s    V_4
    IL_0099:  ldarg.0
    IL_009a:  ldc.i4.s   -2
    IL_009c:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00a1:  ldarg.0
    IL_00a2:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_00a7:  ldloc.s    V_4
    IL_00a9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00ae:  leave.s    IL_00c3
  }
  IL_00b0:  ldarg.0
  IL_00b1:  ldc.i4.s   -2
  IL_00b3:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_00b8:  ldarg.0
  IL_00b9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
  IL_00be:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c3:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Increment_Indexer_Struct_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int this[int i] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : struct, IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] ++;
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] ++;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      196 (0xc4)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0049
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0015:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_001a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_001f:  stloc.3
    IL_0020:  ldloca.s   V_3
    IL_0022:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0027:  brtrue.s   IL_0065
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0032:  ldarg.0
    IL_0033:  ldloc.3
    IL_0034:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0039:  ldarg.0
    IL_003a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_003f:  ldloca.s   V_3
    IL_0041:  ldarg.0
    IL_0042:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_0047:  leave.s    IL_00c3
    IL_0049:  ldarg.0
    IL_004a:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_004f:  stloc.3
    IL_0050:  ldarg.0
    IL_0051:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0056:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.m1
    IL_005e:  dup
    IL_005f:  stloc.0
    IL_0060:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0065:  ldloca.s   V_3
    IL_0067:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006c:  stloc.1
    IL_006d:  ldarg.0
    IL_006e:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0073:  ldloc.1
    IL_0074:  constrained. ""T""
    IL_007a:  callvirt   ""int IMoveable.this[int].get""
    IL_007f:  stloc.2
    IL_0080:  ldarg.0
    IL_0081:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0086:  ldloc.1
    IL_0087:  ldloc.2
    IL_0088:  ldc.i4.1
    IL_0089:  add
    IL_008a:  constrained. ""T""
    IL_0090:  callvirt   ""void IMoveable.this[int].set""
    IL_0095:  leave.s    IL_00b0
  }
  catch System.Exception
  {
    IL_0097:  stloc.s    V_4
    IL_0099:  ldarg.0
    IL_009a:  ldc.i4.s   -2
    IL_009c:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00a1:  ldarg.0
    IL_00a2:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00a7:  ldloc.s    V_4
    IL_00a9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00ae:  leave.s    IL_00c3
  }
  IL_00b0:  ldarg.0
  IL_00b1:  ldc.i4.s   -2
  IL_00b3:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00b8:  ldarg.0
  IL_00b9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00be:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c3:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Increment_Indexer_Class_Async_02()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int this[int i] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : class, IMoveable
    {
        await Task.Yield();
        item[await GetOffsetAsync(GetOffset(ref item))] ++;
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        await Task.Yield();
        item[await GetOffsetAsync(GetOffset(ref item))] ++;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      299 (0x12b)
  .maxstack  4
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                int V_3,
                int V_4,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00af
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Shift1>d__1<T>)""
    IL_0046:  leave      IL_012a
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldarg.0
    IL_006f:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0074:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0079:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_007e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0083:  stloc.s    V_5
    IL_0085:  ldloca.s   V_5
    IL_0087:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_008c:  brtrue.s   IL_00cc
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.1
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0097:  ldarg.0
    IL_0098:  ldloc.s    V_5
    IL_009a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00a5:  ldloca.s   V_5
    IL_00a7:  ldarg.0
    IL_00a8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_00ad:  leave.s    IL_012a
    IL_00af:  ldarg.0
    IL_00b0:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_00b5:  stloc.s    V_5
    IL_00b7:  ldarg.0
    IL_00b8:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_00bd:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00c3:  ldarg.0
    IL_00c4:  ldc.i4.m1
    IL_00c5:  dup
    IL_00c6:  stloc.0
    IL_00c7:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00cc:  ldloca.s   V_5
    IL_00ce:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00d3:  stloc.3
    IL_00d4:  ldarg.0
    IL_00d5:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_00da:  box        ""T""
    IL_00df:  ldloc.3
    IL_00e0:  callvirt   ""int IMoveable.this[int].get""
    IL_00e5:  stloc.s    V_4
    IL_00e7:  ldarg.0
    IL_00e8:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_00ed:  box        ""T""
    IL_00f2:  ldloc.3
    IL_00f3:  ldloc.s    V_4
    IL_00f5:  ldc.i4.1
    IL_00f6:  add
    IL_00f7:  callvirt   ""void IMoveable.this[int].set""
    IL_00fc:  leave.s    IL_0117
  }
  catch System.Exception
  {
    IL_00fe:  stloc.s    V_6
    IL_0100:  ldarg.0
    IL_0101:  ldc.i4.s   -2
    IL_0103:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0108:  ldarg.0
    IL_0109:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_010e:  ldloc.s    V_6
    IL_0110:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0115:  leave.s    IL_012a
  }
  IL_0117:  ldarg.0
  IL_0118:  ldc.i4.s   -2
  IL_011a:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_011f:  ldarg.0
  IL_0120:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_0125:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_012a:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Shift2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      301 (0x12d)
  .maxstack  4
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                int V_3,
                int V_4,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00af
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift2>d__2<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Shift2>d__2<T>)""
    IL_0046:  leave      IL_012c
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift2>d__2<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift2>d__2<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldarg.0
    IL_006f:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0074:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0079:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_007e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0083:  stloc.s    V_5
    IL_0085:  ldloca.s   V_5
    IL_0087:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_008c:  brtrue.s   IL_00cc
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.1
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0097:  ldarg.0
    IL_0098:  ldloc.s    V_5
    IL_009a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__2""
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_00a5:  ldloca.s   V_5
    IL_00a7:  ldarg.0
    IL_00a8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift2>d__2<T>)""
    IL_00ad:  leave.s    IL_012c
    IL_00af:  ldarg.0
    IL_00b0:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__2""
    IL_00b5:  stloc.s    V_5
    IL_00b7:  ldarg.0
    IL_00b8:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__2""
    IL_00bd:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00c3:  ldarg.0
    IL_00c4:  ldc.i4.m1
    IL_00c5:  dup
    IL_00c6:  stloc.0
    IL_00c7:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00cc:  ldloca.s   V_5
    IL_00ce:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00d3:  stloc.3
    IL_00d4:  ldarg.0
    IL_00d5:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_00da:  ldloc.3
    IL_00db:  constrained. ""T""
    IL_00e1:  callvirt   ""int IMoveable.this[int].get""
    IL_00e6:  stloc.s    V_4
    IL_00e8:  ldarg.0
    IL_00e9:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_00ee:  ldloc.3
    IL_00ef:  ldloc.s    V_4
    IL_00f1:  ldc.i4.1
    IL_00f2:  add
    IL_00f3:  constrained. ""T""
    IL_00f9:  callvirt   ""void IMoveable.this[int].set""
    IL_00fe:  leave.s    IL_0119
  }
  catch System.Exception
  {
    IL_0100:  stloc.s    V_6
    IL_0102:  ldarg.0
    IL_0103:  ldc.i4.s   -2
    IL_0105:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_010a:  ldarg.0
    IL_010b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_0110:  ldloc.s    V_6
    IL_0112:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0117:  leave.s    IL_012c
  }
  IL_0119:  ldarg.0
  IL_011a:  ldc.i4.s   -2
  IL_011c:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0121:  ldarg.0
  IL_0122:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
  IL_0127:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_012c:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Increment_Indexer_Struct_Async_02()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int this[int i] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : struct, IMoveable
    {
        await Task.Yield();
        item[await GetOffsetAsync(GetOffset(ref item))] ++;
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        await Task.Yield();
        item[await GetOffsetAsync(GetOffset(ref item))] ++;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      301 (0x12d)
  .maxstack  4
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                int V_3,
                int V_4,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00af
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Shift1>d__1<T>)""
    IL_0046:  leave      IL_012c
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldarg.0
    IL_006f:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0074:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0079:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_007e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0083:  stloc.s    V_5
    IL_0085:  ldloca.s   V_5
    IL_0087:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_008c:  brtrue.s   IL_00cc
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.1
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0097:  ldarg.0
    IL_0098:  ldloc.s    V_5
    IL_009a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00a5:  ldloca.s   V_5
    IL_00a7:  ldarg.0
    IL_00a8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_00ad:  leave.s    IL_012c
    IL_00af:  ldarg.0
    IL_00b0:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_00b5:  stloc.s    V_5
    IL_00b7:  ldarg.0
    IL_00b8:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_00bd:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00c3:  ldarg.0
    IL_00c4:  ldc.i4.m1
    IL_00c5:  dup
    IL_00c6:  stloc.0
    IL_00c7:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00cc:  ldloca.s   V_5
    IL_00ce:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00d3:  stloc.3
    IL_00d4:  ldarg.0
    IL_00d5:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_00da:  ldloc.3
    IL_00db:  constrained. ""T""
    IL_00e1:  callvirt   ""int IMoveable.this[int].get""
    IL_00e6:  stloc.s    V_4
    IL_00e8:  ldarg.0
    IL_00e9:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_00ee:  ldloc.3
    IL_00ef:  ldloc.s    V_4
    IL_00f1:  ldc.i4.1
    IL_00f2:  add
    IL_00f3:  constrained. ""T""
    IL_00f9:  callvirt   ""void IMoveable.this[int].set""
    IL_00fe:  leave.s    IL_0119
  }
  catch System.Exception
  {
    IL_0100:  stloc.s    V_6
    IL_0102:  ldarg.0
    IL_0103:  ldc.i4.s   -2
    IL_0105:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_010a:  ldarg.0
    IL_010b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0110:  ldloc.s    V_6
    IL_0112:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0117:  leave.s    IL_012c
  }
  IL_0119:  ldarg.0
  IL_011a:  ldc.i4.s   -2
  IL_011c:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0121:  ldarg.0
  IL_0122:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_0127:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_012c:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Class_Index()
        {
            var source = @"
using System;

interface IMoveable
{
    int? this[int x] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(item1);

        var item2 = new Item {Name = ""2""};
        Shift2(item2);
    }

    static void Shift1<T>(T item) where T : class, IMoveable
    {
        item[GetOffset(ref item)] ??= 1;
    }

    static void Shift2<T>(T item) where T : IMoveable
    {
        item[GetOffset(ref item)] ??= 1;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       68 (0x44)
  .maxstack  4
  .locals init (T& V_0,
                int V_1,
                int? V_2,
                int V_3,
                int? V_4)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldarga.s   V_0
  IL_0005:  call       ""int Program.GetOffset<T>(ref T)""
  IL_000a:  stloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  constrained. ""T""
  IL_0013:  callvirt   ""int? IMoveable.this[int].get""
  IL_0018:  stloc.2
  IL_0019:  ldloca.s   V_2
  IL_001b:  call       ""int int?.GetValueOrDefault()""
  IL_0020:  stloc.3
  IL_0021:  ldloca.s   V_2
  IL_0023:  call       ""bool int?.HasValue.get""
  IL_0028:  brtrue.s   IL_0043
  IL_002a:  ldc.i4.1
  IL_002b:  stloc.3
  IL_002c:  ldloc.0
  IL_002d:  ldloc.1
  IL_002e:  ldloca.s   V_4
  IL_0030:  ldloc.3
  IL_0031:  call       ""int?..ctor(int)""
  IL_0036:  ldloc.s    V_4
  IL_0038:  constrained. ""T""
  IL_003e:  callvirt   ""void IMoveable.this[int].set""
  IL_0043:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Shift2<T>",
@"
{
  // Code size       68 (0x44)
  .maxstack  4
  .locals init (T& V_0,
                int V_1,
                int? V_2,
                int V_3,
                int? V_4)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldarga.s   V_0
  IL_0005:  call       ""int Program.GetOffset<T>(ref T)""
  IL_000a:  stloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  constrained. ""T""
  IL_0013:  callvirt   ""int? IMoveable.this[int].get""
  IL_0018:  stloc.2
  IL_0019:  ldloca.s   V_2
  IL_001b:  call       ""int int?.GetValueOrDefault()""
  IL_0020:  stloc.3
  IL_0021:  ldloca.s   V_2
  IL_0023:  call       ""bool int?.HasValue.get""
  IL_0028:  brtrue.s   IL_0043
  IL_002a:  ldc.i4.1
  IL_002b:  stloc.3
  IL_002c:  ldloc.0
  IL_002d:  ldloc.1
  IL_002e:  ldloca.s   V_4
  IL_0030:  ldloc.3
  IL_0031:  call       ""int?..ctor(int)""
  IL_0036:  ldloc.s    V_4
  IL_0038:  constrained. ""T""
  IL_003e:  callvirt   ""void IMoveable.this[int].set""
  IL_0043:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Struct_Index()
        {
            var source = @"
using System;

interface IMoveable
{
    int? this[int x] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(item1);

        var item2 = new Item {Name = ""2""};
        Shift2(item2);
    }

    static void Shift1<T>(T item) where T : struct, IMoveable
    {
        item[GetOffset(ref item)] ??= 1;
    }

    static void Shift2<T>(T item) where T : IMoveable
    {
        item[GetOffset(ref item)] ??= 1;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       68 (0x44)
  .maxstack  4
  .locals init (T& V_0,
                int V_1,
                int? V_2,
                int V_3,
                int? V_4)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldarga.s   V_0
  IL_0005:  call       ""int Program.GetOffset<T>(ref T)""
  IL_000a:  stloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  constrained. ""T""
  IL_0013:  callvirt   ""int? IMoveable.this[int].get""
  IL_0018:  stloc.2
  IL_0019:  ldloca.s   V_2
  IL_001b:  call       ""int int?.GetValueOrDefault()""
  IL_0020:  stloc.3
  IL_0021:  ldloca.s   V_2
  IL_0023:  call       ""bool int?.HasValue.get""
  IL_0028:  brtrue.s   IL_0043
  IL_002a:  ldc.i4.1
  IL_002b:  stloc.3
  IL_002c:  ldloc.0
  IL_002d:  ldloc.1
  IL_002e:  ldloca.s   V_4
  IL_0030:  ldloc.3
  IL_0031:  call       ""int?..ctor(int)""
  IL_0036:  ldloc.s    V_4
  IL_0038:  constrained. ""T""
  IL_003e:  callvirt   ""void IMoveable.this[int].set""
  IL_0043:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Class_Index_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    int? this[int x] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(ref item1);

        var item2 = new Item {Name = ""2""};
        Shift2(ref item2);
    }

    static void Shift1<T>(ref T item) where T : class, IMoveable
    {
        item[GetOffset(ref item)] ??= 1;
    }

    static void Shift2<T>(ref T item) where T : IMoveable
    {
        item[GetOffset(ref item)] ??= 1;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       66 (0x42)
  .maxstack  4
  .locals init (T& V_0,
                int V_1,
                int? V_2,
                int V_3,
                int? V_4)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0008:  stloc.1
  IL_0009:  ldloc.0
  IL_000a:  ldloc.1
  IL_000b:  constrained. ""T""
  IL_0011:  callvirt   ""int? IMoveable.this[int].get""
  IL_0016:  stloc.2
  IL_0017:  ldloca.s   V_2
  IL_0019:  call       ""int int?.GetValueOrDefault()""
  IL_001e:  stloc.3
  IL_001f:  ldloca.s   V_2
  IL_0021:  call       ""bool int?.HasValue.get""
  IL_0026:  brtrue.s   IL_0041
  IL_0028:  ldc.i4.1
  IL_0029:  stloc.3
  IL_002a:  ldloc.0
  IL_002b:  ldloc.1
  IL_002c:  ldloca.s   V_4
  IL_002e:  ldloc.3
  IL_002f:  call       ""int?..ctor(int)""
  IL_0034:  ldloc.s    V_4
  IL_0036:  constrained. ""T""
  IL_003c:  callvirt   ""void IMoveable.this[int].set""
  IL_0041:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Shift2<T>",
@"
{
  // Code size       66 (0x42)
  .maxstack  4
  .locals init (T& V_0,
                int V_1,
                int? V_2,
                int V_3,
                int? V_4)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0008:  stloc.1
  IL_0009:  ldloc.0
  IL_000a:  ldloc.1
  IL_000b:  constrained. ""T""
  IL_0011:  callvirt   ""int? IMoveable.this[int].get""
  IL_0016:  stloc.2
  IL_0017:  ldloca.s   V_2
  IL_0019:  call       ""int int?.GetValueOrDefault()""
  IL_001e:  stloc.3
  IL_001f:  ldloca.s   V_2
  IL_0021:  call       ""bool int?.HasValue.get""
  IL_0026:  brtrue.s   IL_0041
  IL_0028:  ldc.i4.1
  IL_0029:  stloc.3
  IL_002a:  ldloc.0
  IL_002b:  ldloc.1
  IL_002c:  ldloca.s   V_4
  IL_002e:  ldloc.3
  IL_002f:  call       ""int?..ctor(int)""
  IL_0034:  ldloc.s    V_4
  IL_0036:  constrained. ""T""
  IL_003c:  callvirt   ""void IMoveable.this[int].set""
  IL_0041:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Struct_Index_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    int? this[int x] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(ref item1);

        var item2 = new Item {Name = ""2""};
        Shift2(ref item2);
    }

    static void Shift1<T>(ref T item) where T : struct, IMoveable
    {
        item[GetOffset(ref item)] ??= 1;
    }

    static void Shift2<T>(ref T item) where T : IMoveable
    {
        item[GetOffset(ref item)] ??= 1;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       66 (0x42)
  .maxstack  4
  .locals init (T& V_0,
                int V_1,
                int? V_2,
                int V_3,
                int? V_4)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0008:  stloc.1
  IL_0009:  ldloc.0
  IL_000a:  ldloc.1
  IL_000b:  constrained. ""T""
  IL_0011:  callvirt   ""int? IMoveable.this[int].get""
  IL_0016:  stloc.2
  IL_0017:  ldloca.s   V_2
  IL_0019:  call       ""int int?.GetValueOrDefault()""
  IL_001e:  stloc.3
  IL_001f:  ldloca.s   V_2
  IL_0021:  call       ""bool int?.HasValue.get""
  IL_0026:  brtrue.s   IL_0041
  IL_0028:  ldc.i4.1
  IL_0029:  stloc.3
  IL_002a:  ldloc.0
  IL_002b:  ldloc.1
  IL_002c:  ldloca.s   V_4
  IL_002e:  ldloc.3
  IL_002f:  call       ""int?..ctor(int)""
  IL_0034:  ldloc.s    V_4
  IL_0036:  constrained. ""T""
  IL_003c:  callvirt   ""void IMoveable.this[int].set""
  IL_0041:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Class_Index_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int? this[int x] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : class, IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] ??= 1;
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] ??= 1;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      225 (0xe1)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                int? V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                int? V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004e
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0015:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_001a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_001f:  stloc.s    V_4
    IL_0021:  ldloca.s   V_4
    IL_0023:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0028:  brtrue.s   IL_006b
    IL_002a:  ldarg.0
    IL_002b:  ldc.i4.0
    IL_002c:  dup
    IL_002d:  stloc.0
    IL_002e:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0033:  ldarg.0
    IL_0034:  ldloc.s    V_4
    IL_0036:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_003b:  ldarg.0
    IL_003c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0041:  ldloca.s   V_4
    IL_0043:  ldarg.0
    IL_0044:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_0049:  leave      IL_00e0
    IL_004e:  ldarg.0
    IL_004f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0054:  stloc.s    V_4
    IL_0056:  ldarg.0
    IL_0057:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_005c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0062:  ldarg.0
    IL_0063:  ldc.i4.m1
    IL_0064:  dup
    IL_0065:  stloc.0
    IL_0066:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_006b:  ldloca.s   V_4
    IL_006d:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0072:  stloc.1
    IL_0073:  ldarg.0
    IL_0074:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_0079:  box        ""T""
    IL_007e:  ldloc.1
    IL_007f:  callvirt   ""int? IMoveable.this[int].get""
    IL_0084:  stloc.2
    IL_0085:  ldloca.s   V_2
    IL_0087:  call       ""int int?.GetValueOrDefault()""
    IL_008c:  stloc.3
    IL_008d:  ldloca.s   V_2
    IL_008f:  call       ""bool int?.HasValue.get""
    IL_0094:  brtrue.s   IL_00b2
    IL_0096:  ldc.i4.1
    IL_0097:  stloc.3
    IL_0098:  ldarg.0
    IL_0099:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_009e:  box        ""T""
    IL_00a3:  ldloc.1
    IL_00a4:  ldloc.3
    IL_00a5:  newobj     ""int?..ctor(int)""
    IL_00aa:  dup
    IL_00ab:  stloc.s    V_5
    IL_00ad:  callvirt   ""void IMoveable.this[int].set""
    IL_00b2:  leave.s    IL_00cd
  }
  catch System.Exception
  {
    IL_00b4:  stloc.s    V_6
    IL_00b6:  ldarg.0
    IL_00b7:  ldc.i4.s   -2
    IL_00b9:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00be:  ldarg.0
    IL_00bf:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00c4:  ldloc.s    V_6
    IL_00c6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00cb:  leave.s    IL_00e0
  }
  IL_00cd:  ldarg.0
  IL_00ce:  ldc.i4.s   -2
  IL_00d0:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00d5:  ldarg.0
  IL_00d6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00db:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00e0:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Shift2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      227 (0xe3)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                int? V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                int? V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004e
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0015:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_001a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_001f:  stloc.s    V_4
    IL_0021:  ldloca.s   V_4
    IL_0023:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0028:  brtrue.s   IL_006b
    IL_002a:  ldarg.0
    IL_002b:  ldc.i4.0
    IL_002c:  dup
    IL_002d:  stloc.0
    IL_002e:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0033:  ldarg.0
    IL_0034:  ldloc.s    V_4
    IL_0036:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_003b:  ldarg.0
    IL_003c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_0041:  ldloca.s   V_4
    IL_0043:  ldarg.0
    IL_0044:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift2>d__2<T>)""
    IL_0049:  leave      IL_00e2
    IL_004e:  ldarg.0
    IL_004f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0054:  stloc.s    V_4
    IL_0056:  ldarg.0
    IL_0057:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_005c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0062:  ldarg.0
    IL_0063:  ldc.i4.m1
    IL_0064:  dup
    IL_0065:  stloc.0
    IL_0066:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_006b:  ldloca.s   V_4
    IL_006d:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0072:  stloc.1
    IL_0073:  ldarg.0
    IL_0074:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0079:  ldloc.1
    IL_007a:  constrained. ""T""
    IL_0080:  callvirt   ""int? IMoveable.this[int].get""
    IL_0085:  stloc.2
    IL_0086:  ldloca.s   V_2
    IL_0088:  call       ""int int?.GetValueOrDefault()""
    IL_008d:  stloc.3
    IL_008e:  ldloca.s   V_2
    IL_0090:  call       ""bool int?.HasValue.get""
    IL_0095:  brtrue.s   IL_00b4
    IL_0097:  ldc.i4.1
    IL_0098:  stloc.3
    IL_0099:  ldarg.0
    IL_009a:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_009f:  ldloc.1
    IL_00a0:  ldloc.3
    IL_00a1:  newobj     ""int?..ctor(int)""
    IL_00a6:  dup
    IL_00a7:  stloc.s    V_5
    IL_00a9:  constrained. ""T""
    IL_00af:  callvirt   ""void IMoveable.this[int].set""
    IL_00b4:  leave.s    IL_00cf
  }
  catch System.Exception
  {
    IL_00b6:  stloc.s    V_6
    IL_00b8:  ldarg.0
    IL_00b9:  ldc.i4.s   -2
    IL_00bb:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00c0:  ldarg.0
    IL_00c1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_00c6:  ldloc.s    V_6
    IL_00c8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00cd:  leave.s    IL_00e2
  }
  IL_00cf:  ldarg.0
  IL_00d0:  ldc.i4.s   -2
  IL_00d2:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_00d7:  ldarg.0
  IL_00d8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
  IL_00dd:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00e2:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Struct_Index_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int? this[int x] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : struct, IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] ??= 1;
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] ??= 1;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      227 (0xe3)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                int? V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                int? V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004e
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0015:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_001a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_001f:  stloc.s    V_4
    IL_0021:  ldloca.s   V_4
    IL_0023:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0028:  brtrue.s   IL_006b
    IL_002a:  ldarg.0
    IL_002b:  ldc.i4.0
    IL_002c:  dup
    IL_002d:  stloc.0
    IL_002e:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0033:  ldarg.0
    IL_0034:  ldloc.s    V_4
    IL_0036:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_003b:  ldarg.0
    IL_003c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0041:  ldloca.s   V_4
    IL_0043:  ldarg.0
    IL_0044:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_0049:  leave      IL_00e2
    IL_004e:  ldarg.0
    IL_004f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0054:  stloc.s    V_4
    IL_0056:  ldarg.0
    IL_0057:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_005c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0062:  ldarg.0
    IL_0063:  ldc.i4.m1
    IL_0064:  dup
    IL_0065:  stloc.0
    IL_0066:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_006b:  ldloca.s   V_4
    IL_006d:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0072:  stloc.1
    IL_0073:  ldarg.0
    IL_0074:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0079:  ldloc.1
    IL_007a:  constrained. ""T""
    IL_0080:  callvirt   ""int? IMoveable.this[int].get""
    IL_0085:  stloc.2
    IL_0086:  ldloca.s   V_2
    IL_0088:  call       ""int int?.GetValueOrDefault()""
    IL_008d:  stloc.3
    IL_008e:  ldloca.s   V_2
    IL_0090:  call       ""bool int?.HasValue.get""
    IL_0095:  brtrue.s   IL_00b4
    IL_0097:  ldc.i4.1
    IL_0098:  stloc.3
    IL_0099:  ldarg.0
    IL_009a:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_009f:  ldloc.1
    IL_00a0:  ldloc.3
    IL_00a1:  newobj     ""int?..ctor(int)""
    IL_00a6:  dup
    IL_00a7:  stloc.s    V_5
    IL_00a9:  constrained. ""T""
    IL_00af:  callvirt   ""void IMoveable.this[int].set""
    IL_00b4:  leave.s    IL_00cf
  }
  catch System.Exception
  {
    IL_00b6:  stloc.s    V_6
    IL_00b8:  ldarg.0
    IL_00b9:  ldc.i4.s   -2
    IL_00bb:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00c0:  ldarg.0
    IL_00c1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00c6:  ldloc.s    V_6
    IL_00c8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00cd:  leave.s    IL_00e2
  }
  IL_00cf:  ldarg.0
  IL_00d0:  ldc.i4.s   -2
  IL_00d2:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00d7:  ldarg.0
  IL_00d8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00dd:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00e2:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Class_Index_Async_02()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int? this[int x] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : class, IMoveable
    {
        await Task.Yield();
        item[await GetOffsetAsync(GetOffset(ref item))] ??= 1;
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        await Task.Yield();
        item[await GetOffsetAsync(GetOffset(ref item))] ??= 1;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      329 (0x149)
  .maxstack  4
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                int V_3,
                int? V_4,
                int V_5,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_6,
                int? V_7,
                System.Exception V_8)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00b2
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Shift1>d__1<T>)""
    IL_0046:  leave      IL_0148
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldarg.0
    IL_006f:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0074:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0079:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_007e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0083:  stloc.s    V_6
    IL_0085:  ldloca.s   V_6
    IL_0087:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_008c:  brtrue.s   IL_00cf
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.1
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0097:  ldarg.0
    IL_0098:  ldloc.s    V_6
    IL_009a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00a5:  ldloca.s   V_6
    IL_00a7:  ldarg.0
    IL_00a8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_00ad:  leave      IL_0148
    IL_00b2:  ldarg.0
    IL_00b3:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_00b8:  stloc.s    V_6
    IL_00ba:  ldarg.0
    IL_00bb:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_00c0:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00c6:  ldarg.0
    IL_00c7:  ldc.i4.m1
    IL_00c8:  dup
    IL_00c9:  stloc.0
    IL_00ca:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00cf:  ldloca.s   V_6
    IL_00d1:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00d6:  stloc.3
    IL_00d7:  ldarg.0
    IL_00d8:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_00dd:  box        ""T""
    IL_00e2:  ldloc.3
    IL_00e3:  callvirt   ""int? IMoveable.this[int].get""
    IL_00e8:  stloc.s    V_4
    IL_00ea:  ldloca.s   V_4
    IL_00ec:  call       ""int int?.GetValueOrDefault()""
    IL_00f1:  stloc.s    V_5
    IL_00f3:  ldloca.s   V_4
    IL_00f5:  call       ""bool int?.HasValue.get""
    IL_00fa:  brtrue.s   IL_011a
    IL_00fc:  ldc.i4.1
    IL_00fd:  stloc.s    V_5
    IL_00ff:  ldarg.0
    IL_0100:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_0105:  box        ""T""
    IL_010a:  ldloc.3
    IL_010b:  ldloc.s    V_5
    IL_010d:  newobj     ""int?..ctor(int)""
    IL_0112:  dup
    IL_0113:  stloc.s    V_7
    IL_0115:  callvirt   ""void IMoveable.this[int].set""
    IL_011a:  leave.s    IL_0135
  }
  catch System.Exception
  {
    IL_011c:  stloc.s    V_8
    IL_011e:  ldarg.0
    IL_011f:  ldc.i4.s   -2
    IL_0121:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0126:  ldarg.0
    IL_0127:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_012c:  ldloc.s    V_8
    IL_012e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0133:  leave.s    IL_0148
  }
  IL_0135:  ldarg.0
  IL_0136:  ldc.i4.s   -2
  IL_0138:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_013d:  ldarg.0
  IL_013e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_0143:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0148:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Shift2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      331 (0x14b)
  .maxstack  4
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                int V_3,
                int? V_4,
                int V_5,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_6,
                int? V_7,
                System.Exception V_8)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00b2
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift2>d__2<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Shift2>d__2<T>)""
    IL_0046:  leave      IL_014a
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift2>d__2<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift2>d__2<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldarg.0
    IL_006f:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0074:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0079:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_007e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0083:  stloc.s    V_6
    IL_0085:  ldloca.s   V_6
    IL_0087:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_008c:  brtrue.s   IL_00cf
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.1
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0097:  ldarg.0
    IL_0098:  ldloc.s    V_6
    IL_009a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__2""
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_00a5:  ldloca.s   V_6
    IL_00a7:  ldarg.0
    IL_00a8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift2>d__2<T>)""
    IL_00ad:  leave      IL_014a
    IL_00b2:  ldarg.0
    IL_00b3:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__2""
    IL_00b8:  stloc.s    V_6
    IL_00ba:  ldarg.0
    IL_00bb:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__2""
    IL_00c0:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00c6:  ldarg.0
    IL_00c7:  ldc.i4.m1
    IL_00c8:  dup
    IL_00c9:  stloc.0
    IL_00ca:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00cf:  ldloca.s   V_6
    IL_00d1:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00d6:  stloc.3
    IL_00d7:  ldarg.0
    IL_00d8:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_00dd:  ldloc.3
    IL_00de:  constrained. ""T""
    IL_00e4:  callvirt   ""int? IMoveable.this[int].get""
    IL_00e9:  stloc.s    V_4
    IL_00eb:  ldloca.s   V_4
    IL_00ed:  call       ""int int?.GetValueOrDefault()""
    IL_00f2:  stloc.s    V_5
    IL_00f4:  ldloca.s   V_4
    IL_00f6:  call       ""bool int?.HasValue.get""
    IL_00fb:  brtrue.s   IL_011c
    IL_00fd:  ldc.i4.1
    IL_00fe:  stloc.s    V_5
    IL_0100:  ldarg.0
    IL_0101:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0106:  ldloc.3
    IL_0107:  ldloc.s    V_5
    IL_0109:  newobj     ""int?..ctor(int)""
    IL_010e:  dup
    IL_010f:  stloc.s    V_7
    IL_0111:  constrained. ""T""
    IL_0117:  callvirt   ""void IMoveable.this[int].set""
    IL_011c:  leave.s    IL_0137
  }
  catch System.Exception
  {
    IL_011e:  stloc.s    V_8
    IL_0120:  ldarg.0
    IL_0121:  ldc.i4.s   -2
    IL_0123:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0128:  ldarg.0
    IL_0129:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_012e:  ldloc.s    V_8
    IL_0130:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0135:  leave.s    IL_014a
  }
  IL_0137:  ldarg.0
  IL_0138:  ldc.i4.s   -2
  IL_013a:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_013f:  ldarg.0
  IL_0140:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
  IL_0145:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_014a:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Struct_Index_Async_02()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int? this[int x] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : struct, IMoveable
    {
        await Task.Yield();
        item[await GetOffsetAsync(GetOffset(ref item))] ??= 1;
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        await Task.Yield();
        item[await GetOffsetAsync(GetOffset(ref item))] ??= 1;
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      331 (0x14b)
  .maxstack  4
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                int V_3,
                int? V_4,
                int V_5,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_6,
                int? V_7,
                System.Exception V_8)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00b2
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Shift1>d__1<T>)""
    IL_0046:  leave      IL_014a
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Shift1>d__1<T>.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  ldarg.0
    IL_006f:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0074:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0079:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_007e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0083:  stloc.s    V_6
    IL_0085:  ldloca.s   V_6
    IL_0087:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_008c:  brtrue.s   IL_00cf
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.1
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0097:  ldarg.0
    IL_0098:  ldloc.s    V_6
    IL_009a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00a5:  ldloca.s   V_6
    IL_00a7:  ldarg.0
    IL_00a8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_00ad:  leave      IL_014a
    IL_00b2:  ldarg.0
    IL_00b3:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_00b8:  stloc.s    V_6
    IL_00ba:  ldarg.0
    IL_00bb:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__2""
    IL_00c0:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00c6:  ldarg.0
    IL_00c7:  ldc.i4.m1
    IL_00c8:  dup
    IL_00c9:  stloc.0
    IL_00ca:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00cf:  ldloca.s   V_6
    IL_00d1:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00d6:  stloc.3
    IL_00d7:  ldarg.0
    IL_00d8:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_00dd:  ldloc.3
    IL_00de:  constrained. ""T""
    IL_00e4:  callvirt   ""int? IMoveable.this[int].get""
    IL_00e9:  stloc.s    V_4
    IL_00eb:  ldloca.s   V_4
    IL_00ed:  call       ""int int?.GetValueOrDefault()""
    IL_00f2:  stloc.s    V_5
    IL_00f4:  ldloca.s   V_4
    IL_00f6:  call       ""bool int?.HasValue.get""
    IL_00fb:  brtrue.s   IL_011c
    IL_00fd:  ldc.i4.1
    IL_00fe:  stloc.s    V_5
    IL_0100:  ldarg.0
    IL_0101:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0106:  ldloc.3
    IL_0107:  ldloc.s    V_5
    IL_0109:  newobj     ""int?..ctor(int)""
    IL_010e:  dup
    IL_010f:  stloc.s    V_7
    IL_0111:  constrained. ""T""
    IL_0117:  callvirt   ""void IMoveable.this[int].set""
    IL_011c:  leave.s    IL_0137
  }
  catch System.Exception
  {
    IL_011e:  stloc.s    V_8
    IL_0120:  ldarg.0
    IL_0121:  ldc.i4.s   -2
    IL_0123:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0128:  ldarg.0
    IL_0129:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_012e:  ldloc.s    V_8
    IL_0130:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0135:  leave.s    IL_014a
  }
  IL_0137:  ldarg.0
  IL_0138:  ldc.i4.s   -2
  IL_013a:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_013f:  ldarg.0
  IL_0140:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_0145:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_014a:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Value()
        {
            var source = @"
using System;

interface IMoveable
{
    int this[int i] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(item1);

        var item2 = new Item {Name = ""2""};
        Shift2(item2);
    }

    static void Shift1<T>(T item) where T : class, IMoveable
    {
        item[1] += GetOffset(ref item);
    }

    static void Shift2<T>(T item) where T : IMoveable
    {
        item[1] += GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            // Wrong output on some frameworks
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe/*, expectedOutput: @"
Position get for item '1'
Position set for item '1'
Position get for item '2'
Position set for item '2'
"*/).VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       38 (0x26)
  .maxstack  4
  .locals init (T& V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  ldc.i4.1
  IL_0005:  ldloc.0
  IL_0006:  ldc.i4.1
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""int IMoveable.this[int].get""
  IL_0012:  ldarga.s   V_0
  IL_0014:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0019:  add
  IL_001a:  constrained. ""T""
  IL_0020:  callvirt   ""void IMoveable.this[int].set""
  IL_0025:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Shift2<T>",
@"
{
  // Code size       38 (0x26)
  .maxstack  4
  .locals init (T& V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  ldc.i4.1
  IL_0005:  ldloc.0
  IL_0006:  ldc.i4.1
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""int IMoveable.this[int].get""
  IL_0012:  ldarga.s   V_0
  IL_0014:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0019:  add
  IL_001a:  constrained. ""T""
  IL_0020:  callvirt   ""void IMoveable.this[int].set""
  IL_0025:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Value()
        {
            var source = @"
using System;

interface IMoveable
{
    int this[int i] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(item1);

        var item2 = new Item {Name = ""2""};
        Shift2(item2);
    }

    static void Shift1<T>(T item) where T : struct, IMoveable
    {
        item[1] += GetOffset(ref item);
    }

    static void Shift2<T>(T item) where T : IMoveable
    {
        item[1] += GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       38 (0x26)
  .maxstack  4
  .locals init (T& V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  ldc.i4.1
  IL_0005:  ldloc.0
  IL_0006:  ldc.i4.1
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""int IMoveable.this[int].get""
  IL_0012:  ldarga.s   V_0
  IL_0014:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0019:  add
  IL_001a:  constrained. ""T""
  IL_0020:  callvirt   ""void IMoveable.this[int].set""
  IL_0025:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Value_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    int this[int i] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(ref item1);

        var item2 = new Item {Name = ""2""};
        Shift2(ref item2);
    }

    static void Shift1<T>(ref T item) where T : class, IMoveable
    {
        item[1] += GetOffset(ref item);
    }

    static void Shift2<T>(ref T item) where T : IMoveable
    {
        item[1] += GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            // Wrong output on some frameworks
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe/*, expectedOutput: @"
Position get for item '1'
Position set for item '1'
Position get for item '2'
Position set for item '2'
"*/).VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       36 (0x24)
  .maxstack  4
  .locals init (T& V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  ldloc.0
  IL_0005:  ldc.i4.1
  IL_0006:  constrained. ""T""
  IL_000c:  callvirt   ""int IMoveable.this[int].get""
  IL_0011:  ldarg.0
  IL_0012:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0017:  add
  IL_0018:  constrained. ""T""
  IL_001e:  callvirt   ""void IMoveable.this[int].set""
  IL_0023:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Shift2<T>",
@"
{
  // Code size       36 (0x24)
  .maxstack  4
  .locals init (T& V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  ldloc.0
  IL_0005:  ldc.i4.1
  IL_0006:  constrained. ""T""
  IL_000c:  callvirt   ""int IMoveable.this[int].get""
  IL_0011:  ldarg.0
  IL_0012:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0017:  add
  IL_0018:  constrained. ""T""
  IL_001e:  callvirt   ""void IMoveable.this[int].set""
  IL_0023:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Value_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    int this[int i] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(ref item1);

        var item2 = new Item {Name = ""2""};
        Shift2(ref item2);
    }

    static void Shift1<T>(ref T item) where T : struct, IMoveable
    {
        item[1] += GetOffset(ref item);
    }

    static void Shift2<T>(ref T item) where T : IMoveable
    {
        item[1] += GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       36 (0x24)
  .maxstack  4
  .locals init (T& V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  ldloc.0
  IL_0005:  ldc.i4.1
  IL_0006:  constrained. ""T""
  IL_000c:  callvirt   ""int IMoveable.this[int].get""
  IL_0011:  ldarg.0
  IL_0012:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0017:  add
  IL_0018:  constrained. ""T""
  IL_001e:  callvirt   ""void IMoveable.this[int].set""
  IL_0023:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Value_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int this[int i] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : class, IMoveable
    {
        item[1] += await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[1] += await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      231 (0xe7)
  .maxstack  4
  .locals init (int V_0,
                T& V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_006f
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0010:  stloc.1
    IL_0011:  ldarg.0
    IL_0012:  ldloc.1
    IL_0013:  ldobj      ""T""
    IL_0018:  stfld      ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_001d:  ldarg.0
    IL_001e:  ldloc.1
    IL_001f:  ldc.i4.1
    IL_0020:  constrained. ""T""
    IL_0026:  callvirt   ""int IMoveable.this[int].get""
    IL_002b:  stfld      ""int Program.<Shift1>d__1<T>.<>7__wrap2""
    IL_0030:  ldarg.0
    IL_0031:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0036:  call       ""int Program.GetOffset<T>(ref T)""
    IL_003b:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0040:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0045:  stloc.3
    IL_0046:  ldloca.s   V_3
    IL_0048:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_004d:  brtrue.s   IL_008b
    IL_004f:  ldarg.0
    IL_0050:  ldc.i4.0
    IL_0051:  dup
    IL_0052:  stloc.0
    IL_0053:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0058:  ldarg.0
    IL_0059:  ldloc.3
    IL_005a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_005f:  ldarg.0
    IL_0060:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0065:  ldloca.s   V_3
    IL_0067:  ldarg.0
    IL_0068:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_006d:  leave.s    IL_00e6
    IL_006f:  ldarg.0
    IL_0070:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0075:  stloc.3
    IL_0076:  ldarg.0
    IL_0077:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_007c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0082:  ldarg.0
    IL_0083:  ldc.i4.m1
    IL_0084:  dup
    IL_0085:  stloc.0
    IL_0086:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_008b:  ldloca.s   V_3
    IL_008d:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0092:  stloc.2
    IL_0093:  ldarg.0
    IL_0094:  ldfld      ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0099:  box        ""T""
    IL_009e:  ldc.i4.1
    IL_009f:  ldarg.0
    IL_00a0:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap2""
    IL_00a5:  ldloc.2
    IL_00a6:  add
    IL_00a7:  callvirt   ""void IMoveable.this[int].set""
    IL_00ac:  ldarg.0
    IL_00ad:  ldflda     ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_00b2:  initobj    ""T""
    IL_00b8:  leave.s    IL_00d3
  }
  catch System.Exception
  {
    IL_00ba:  stloc.s    V_4
    IL_00bc:  ldarg.0
    IL_00bd:  ldc.i4.s   -2
    IL_00bf:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00c4:  ldarg.0
    IL_00c5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00ca:  ldloc.s    V_4
    IL_00cc:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00d1:  leave.s    IL_00e6
  }
  IL_00d3:  ldarg.0
  IL_00d4:  ldc.i4.s   -2
  IL_00d6:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00db:  ldarg.0
  IL_00dc:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00e1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00e6:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Shift2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      204 (0xcc)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0061
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0011:  ldc.i4.1
    IL_0012:  constrained. ""T""
    IL_0018:  callvirt   ""int IMoveable.this[int].get""
    IL_001d:  stfld      ""int Program.<Shift2>d__2<T>.<>7__wrap1""
    IL_0022:  ldarg.0
    IL_0023:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0028:  call       ""int Program.GetOffset<T>(ref T)""
    IL_002d:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0032:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0037:  stloc.2
    IL_0038:  ldloca.s   V_2
    IL_003a:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_003f:  brtrue.s   IL_007d
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.0
    IL_0043:  dup
    IL_0044:  stloc.0
    IL_0045:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_004a:  ldarg.0
    IL_004b:  ldloc.2
    IL_004c:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0051:  ldarg.0
    IL_0052:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_0057:  ldloca.s   V_2
    IL_0059:  ldarg.0
    IL_005a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift2>d__2<T>)""
    IL_005f:  leave.s    IL_00cb
    IL_0061:  ldarg.0
    IL_0062:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0067:  stloc.2
    IL_0068:  ldarg.0
    IL_0069:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_006e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0074:  ldarg.0
    IL_0075:  ldc.i4.m1
    IL_0076:  dup
    IL_0077:  stloc.0
    IL_0078:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_007d:  ldloca.s   V_2
    IL_007f:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0084:  stloc.1
    IL_0085:  ldarg.0
    IL_0086:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_008b:  ldc.i4.1
    IL_008c:  ldarg.0
    IL_008d:  ldfld      ""int Program.<Shift2>d__2<T>.<>7__wrap1""
    IL_0092:  ldloc.1
    IL_0093:  add
    IL_0094:  constrained. ""T""
    IL_009a:  callvirt   ""void IMoveable.this[int].set""
    IL_009f:  leave.s    IL_00b8
  }
  catch System.Exception
  {
    IL_00a1:  stloc.3
    IL_00a2:  ldarg.0
    IL_00a3:  ldc.i4.s   -2
    IL_00a5:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00aa:  ldarg.0
    IL_00ab:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_00b0:  ldloc.3
    IL_00b1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00b6:  leave.s    IL_00cb
  }
  IL_00b8:  ldarg.0
  IL_00b9:  ldc.i4.s   -2
  IL_00bb:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_00c0:  ldarg.0
  IL_00c1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
  IL_00c6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00cb:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Value_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int this[int i] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : struct, IMoveable
    {
        item[1] += await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[1] += await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      204 (0xcc)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0061
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0011:  ldc.i4.1
    IL_0012:  constrained. ""T""
    IL_0018:  callvirt   ""int IMoveable.this[int].get""
    IL_001d:  stfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0022:  ldarg.0
    IL_0023:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0028:  call       ""int Program.GetOffset<T>(ref T)""
    IL_002d:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0032:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0037:  stloc.2
    IL_0038:  ldloca.s   V_2
    IL_003a:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_003f:  brtrue.s   IL_007d
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.0
    IL_0043:  dup
    IL_0044:  stloc.0
    IL_0045:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_004a:  ldarg.0
    IL_004b:  ldloc.2
    IL_004c:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0051:  ldarg.0
    IL_0052:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0057:  ldloca.s   V_2
    IL_0059:  ldarg.0
    IL_005a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_005f:  leave.s    IL_00cb
    IL_0061:  ldarg.0
    IL_0062:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0067:  stloc.2
    IL_0068:  ldarg.0
    IL_0069:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_006e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0074:  ldarg.0
    IL_0075:  ldc.i4.m1
    IL_0076:  dup
    IL_0077:  stloc.0
    IL_0078:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_007d:  ldloca.s   V_2
    IL_007f:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0084:  stloc.1
    IL_0085:  ldarg.0
    IL_0086:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_008b:  ldc.i4.1
    IL_008c:  ldarg.0
    IL_008d:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0092:  ldloc.1
    IL_0093:  add
    IL_0094:  constrained. ""T""
    IL_009a:  callvirt   ""void IMoveable.this[int].set""
    IL_009f:  leave.s    IL_00b8
  }
  catch System.Exception
  {
    IL_00a1:  stloc.3
    IL_00a2:  ldarg.0
    IL_00a3:  ldc.i4.s   -2
    IL_00a5:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00aa:  ldarg.0
    IL_00ab:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00b0:  ldloc.3
    IL_00b1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00b6:  leave.s    IL_00cb
  }
  IL_00b8:  ldarg.0
  IL_00b9:  ldc.i4.s   -2
  IL_00bb:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00c0:  ldarg.0
  IL_00c1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00c6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00cb:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_IndexAndValue()
        {
            var source = @"
using System;

interface IMoveable
{
    int this[int i] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(item1);

        var item2 = new Item {Name = ""2""};
        Shift2(item2);
    }

    static void Shift1<T>(T item) where T : class, IMoveable
    {
        item[GetOffset(ref item)] += GetOffset(ref item);
    }

    static void Shift2<T>(T item) where T : IMoveable
    {
        item[GetOffset(ref item)] += GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";
            // Wrong output and framework dependent
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe/*, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-3'
Position set for item '-3'
"*/).VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       46 (0x2e)
  .maxstack  4
  .locals init (T& V_0,
                int V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldarga.s   V_0
  IL_0005:  call       ""int Program.GetOffset<T>(ref T)""
  IL_000a:  stloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  ldloc.0
  IL_000e:  ldloc.1
  IL_000f:  constrained. ""T""
  IL_0015:  callvirt   ""int IMoveable.this[int].get""
  IL_001a:  ldarga.s   V_0
  IL_001c:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0021:  add
  IL_0022:  constrained. ""T""
  IL_0028:  callvirt   ""void IMoveable.this[int].set""
  IL_002d:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Shift2<T>",
@"
{
  // Code size       46 (0x2e)
  .maxstack  4
  .locals init (T& V_0,
                int V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldarga.s   V_0
  IL_0005:  call       ""int Program.GetOffset<T>(ref T)""
  IL_000a:  stloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  ldloc.0
  IL_000e:  ldloc.1
  IL_000f:  constrained. ""T""
  IL_0015:  callvirt   ""int IMoveable.this[int].get""
  IL_001a:  ldarga.s   V_0
  IL_001c:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0021:  add
  IL_0022:  constrained. ""T""
  IL_0028:  callvirt   ""void IMoveable.this[int].set""
  IL_002d:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_IndexAndValue()
        {
            var source = @"
using System;

interface IMoveable
{
    int this[int i] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(item1);

        var item2 = new Item {Name = ""2""};
        Shift2(item2);
    }

    static void Shift1<T>(T item) where T : struct, IMoveable
    {
        item[GetOffset(ref item)] += GetOffset(ref item);
    }

    static void Shift2<T>(T item) where T : IMoveable
    {
        item[GetOffset(ref item)] += GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       46 (0x2e)
  .maxstack  4
  .locals init (T& V_0,
                int V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldarga.s   V_0
  IL_0005:  call       ""int Program.GetOffset<T>(ref T)""
  IL_000a:  stloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  ldloc.0
  IL_000e:  ldloc.1
  IL_000f:  constrained. ""T""
  IL_0015:  callvirt   ""int IMoveable.this[int].get""
  IL_001a:  ldarga.s   V_0
  IL_001c:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0021:  add
  IL_0022:  constrained. ""T""
  IL_0028:  callvirt   ""void IMoveable.this[int].set""
  IL_002d:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_IndexAndValue_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    int this[int i] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(ref item1);

        var item2 = new Item {Name = ""2""};
        Shift2(ref item2);
    }

    static void Shift1<T>(ref T item) where T : class, IMoveable
    {
        item[GetOffset(ref item)] += GetOffset(ref item);
    }

    static void Shift2<T>(ref T item) where T : IMoveable
    {
        item[GetOffset(ref item)] += GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";
            // Wrong output and framework dependent
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe/*, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-3'
Position set for item '-3'
"*/).VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       43 (0x2b)
  .maxstack  4
  .locals init (T& V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0008:  stloc.1
  IL_0009:  ldloc.0
  IL_000a:  ldloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  constrained. ""T""
  IL_0013:  callvirt   ""int IMoveable.this[int].get""
  IL_0018:  ldarg.0
  IL_0019:  call       ""int Program.GetOffset<T>(ref T)""
  IL_001e:  add
  IL_001f:  constrained. ""T""
  IL_0025:  callvirt   ""void IMoveable.this[int].set""
  IL_002a:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Shift2<T>",
@"
{
  // Code size       43 (0x2b)
  .maxstack  4
  .locals init (T& V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0008:  stloc.1
  IL_0009:  ldloc.0
  IL_000a:  ldloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  constrained. ""T""
  IL_0013:  callvirt   ""int IMoveable.this[int].get""
  IL_0018:  ldarg.0
  IL_0019:  call       ""int Program.GetOffset<T>(ref T)""
  IL_001e:  add
  IL_001f:  constrained. ""T""
  IL_0025:  callvirt   ""void IMoveable.this[int].set""
  IL_002a:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_IndexAndValue_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    int this[int i] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(ref item1);

        var item2 = new Item {Name = ""2""};
        Shift2(ref item2);
    }

    static void Shift1<T>(ref T item) where T : struct, IMoveable
    {
        item[GetOffset(ref item)] += GetOffset(ref item);
    }

    static void Shift2<T>(ref T item) where T : IMoveable
    {
        item[GetOffset(ref item)] += GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       43 (0x2b)
  .maxstack  4
  .locals init (T& V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0008:  stloc.1
  IL_0009:  ldloc.0
  IL_000a:  ldloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  constrained. ""T""
  IL_0013:  callvirt   ""int IMoveable.this[int].get""
  IL_0018:  ldarg.0
  IL_0019:  call       ""int Program.GetOffset<T>(ref T)""
  IL_001e:  add
  IL_001f:  constrained. ""T""
  IL_0025:  callvirt   ""void IMoveable.this[int].set""
  IL_002a:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_IndexAndValue_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int this[int i] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : class, IMoveable
    {
        item[GetOffset(ref item)] += await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[GetOffset(ref item)] += await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      258 (0x102)
  .maxstack  4
  .locals init (int V_0,
                T& V_1,
                int V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0084
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0010:  stloc.1
    IL_0011:  ldarg.0
    IL_0012:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0017:  call       ""int Program.GetOffset<T>(ref T)""
    IL_001c:  stloc.2
    IL_001d:  ldarg.0
    IL_001e:  ldloc.1
    IL_001f:  ldobj      ""T""
    IL_0024:  stfld      ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0029:  ldarg.0
    IL_002a:  ldloc.2
    IL_002b:  stfld      ""int Program.<Shift1>d__1<T>.<>7__wrap2""
    IL_0030:  ldarg.0
    IL_0031:  ldloc.1
    IL_0032:  ldloc.2
    IL_0033:  constrained. ""T""
    IL_0039:  callvirt   ""int IMoveable.this[int].get""
    IL_003e:  stfld      ""int Program.<Shift1>d__1<T>.<>7__wrap3""
    IL_0043:  ldarg.0
    IL_0044:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0049:  call       ""int Program.GetOffset<T>(ref T)""
    IL_004e:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0053:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0058:  stloc.s    V_4
    IL_005a:  ldloca.s   V_4
    IL_005c:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0061:  brtrue.s   IL_00a1
    IL_0063:  ldarg.0
    IL_0064:  ldc.i4.0
    IL_0065:  dup
    IL_0066:  stloc.0
    IL_0067:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_006c:  ldarg.0
    IL_006d:  ldloc.s    V_4
    IL_006f:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0074:  ldarg.0
    IL_0075:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_007a:  ldloca.s   V_4
    IL_007c:  ldarg.0
    IL_007d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_0082:  leave.s    IL_0101
    IL_0084:  ldarg.0
    IL_0085:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_008a:  stloc.s    V_4
    IL_008c:  ldarg.0
    IL_008d:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0092:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0098:  ldarg.0
    IL_0099:  ldc.i4.m1
    IL_009a:  dup
    IL_009b:  stloc.0
    IL_009c:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00a1:  ldloca.s   V_4
    IL_00a3:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00a8:  stloc.3
    IL_00a9:  ldarg.0
    IL_00aa:  ldfld      ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_00af:  box        ""T""
    IL_00b4:  ldarg.0
    IL_00b5:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap2""
    IL_00ba:  ldarg.0
    IL_00bb:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap3""
    IL_00c0:  ldloc.3
    IL_00c1:  add
    IL_00c2:  callvirt   ""void IMoveable.this[int].set""
    IL_00c7:  ldarg.0
    IL_00c8:  ldflda     ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_00cd:  initobj    ""T""
    IL_00d3:  leave.s    IL_00ee
  }
  catch System.Exception
  {
    IL_00d5:  stloc.s    V_5
    IL_00d7:  ldarg.0
    IL_00d8:  ldc.i4.s   -2
    IL_00da:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00df:  ldarg.0
    IL_00e0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00e5:  ldloc.s    V_5
    IL_00e7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00ec:  leave.s    IL_0101
  }
  IL_00ee:  ldarg.0
  IL_00ef:  ldc.i4.s   -2
  IL_00f1:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00f6:  ldarg.0
  IL_00f7:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00fc:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0101:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Shift2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      230 (0xe6)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0074
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0015:  stloc.1
    IL_0016:  ldarg.0
    IL_0017:  ldloc.1
    IL_0018:  stfld      ""int Program.<Shift2>d__2<T>.<>7__wrap1""
    IL_001d:  ldarg.0
    IL_001e:  ldarg.0
    IL_001f:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0024:  ldloc.1
    IL_0025:  constrained. ""T""
    IL_002b:  callvirt   ""int IMoveable.this[int].get""
    IL_0030:  stfld      ""int Program.<Shift2>d__2<T>.<>7__wrap2""
    IL_0035:  ldarg.0
    IL_0036:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_003b:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0040:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0045:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_004a:  stloc.3
    IL_004b:  ldloca.s   V_3
    IL_004d:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0052:  brtrue.s   IL_0090
    IL_0054:  ldarg.0
    IL_0055:  ldc.i4.0
    IL_0056:  dup
    IL_0057:  stloc.0
    IL_0058:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_005d:  ldarg.0
    IL_005e:  ldloc.3
    IL_005f:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0064:  ldarg.0
    IL_0065:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_006a:  ldloca.s   V_3
    IL_006c:  ldarg.0
    IL_006d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift2>d__2<T>)""
    IL_0072:  leave.s    IL_00e5
    IL_0074:  ldarg.0
    IL_0075:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_007a:  stloc.3
    IL_007b:  ldarg.0
    IL_007c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0081:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0087:  ldarg.0
    IL_0088:  ldc.i4.m1
    IL_0089:  dup
    IL_008a:  stloc.0
    IL_008b:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0090:  ldloca.s   V_3
    IL_0092:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0097:  stloc.2
    IL_0098:  ldarg.0
    IL_0099:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_009e:  ldarg.0
    IL_009f:  ldfld      ""int Program.<Shift2>d__2<T>.<>7__wrap1""
    IL_00a4:  ldarg.0
    IL_00a5:  ldfld      ""int Program.<Shift2>d__2<T>.<>7__wrap2""
    IL_00aa:  ldloc.2
    IL_00ab:  add
    IL_00ac:  constrained. ""T""
    IL_00b2:  callvirt   ""void IMoveable.this[int].set""
    IL_00b7:  leave.s    IL_00d2
  }
  catch System.Exception
  {
    IL_00b9:  stloc.s    V_4
    IL_00bb:  ldarg.0
    IL_00bc:  ldc.i4.s   -2
    IL_00be:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00c3:  ldarg.0
    IL_00c4:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_00c9:  ldloc.s    V_4
    IL_00cb:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00d0:  leave.s    IL_00e5
  }
  IL_00d2:  ldarg.0
  IL_00d3:  ldc.i4.s   -2
  IL_00d5:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_00da:  ldarg.0
  IL_00db:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
  IL_00e0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00e5:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_IndexAndValue_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int this[int i] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : struct, IMoveable
    {
        item[GetOffset(ref item)] += await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[GetOffset(ref item)] += await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      230 (0xe6)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0074
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0015:  stloc.1
    IL_0016:  ldarg.0
    IL_0017:  ldloc.1
    IL_0018:  stfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_001d:  ldarg.0
    IL_001e:  ldarg.0
    IL_001f:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0024:  ldloc.1
    IL_0025:  constrained. ""T""
    IL_002b:  callvirt   ""int IMoveable.this[int].get""
    IL_0030:  stfld      ""int Program.<Shift1>d__1<T>.<>7__wrap2""
    IL_0035:  ldarg.0
    IL_0036:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_003b:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0040:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0045:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_004a:  stloc.3
    IL_004b:  ldloca.s   V_3
    IL_004d:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0052:  brtrue.s   IL_0090
    IL_0054:  ldarg.0
    IL_0055:  ldc.i4.0
    IL_0056:  dup
    IL_0057:  stloc.0
    IL_0058:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_005d:  ldarg.0
    IL_005e:  ldloc.3
    IL_005f:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0064:  ldarg.0
    IL_0065:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_006a:  ldloca.s   V_3
    IL_006c:  ldarg.0
    IL_006d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_0072:  leave.s    IL_00e5
    IL_0074:  ldarg.0
    IL_0075:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_007a:  stloc.3
    IL_007b:  ldarg.0
    IL_007c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0081:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0087:  ldarg.0
    IL_0088:  ldc.i4.m1
    IL_0089:  dup
    IL_008a:  stloc.0
    IL_008b:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0090:  ldloca.s   V_3
    IL_0092:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0097:  stloc.2
    IL_0098:  ldarg.0
    IL_0099:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_009e:  ldarg.0
    IL_009f:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_00a4:  ldarg.0
    IL_00a5:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap2""
    IL_00aa:  ldloc.2
    IL_00ab:  add
    IL_00ac:  constrained. ""T""
    IL_00b2:  callvirt   ""void IMoveable.this[int].set""
    IL_00b7:  leave.s    IL_00d2
  }
  catch System.Exception
  {
    IL_00b9:  stloc.s    V_4
    IL_00bb:  ldarg.0
    IL_00bc:  ldc.i4.s   -2
    IL_00be:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00c3:  ldarg.0
    IL_00c4:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00c9:  ldloc.s    V_4
    IL_00cb:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00d0:  leave.s    IL_00e5
  }
  IL_00d2:  ldarg.0
  IL_00d3:  ldc.i4.s   -2
  IL_00d5:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00da:  ldarg.0
  IL_00db:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00e0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00e5:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_IndexAndValue_Async_03()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int this[int i] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : class, IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] += GetOffset(ref item);
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] += GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output and framework dependent
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe/*, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-3'
Position set for item '-3'
"*/).VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      200 (0xc8)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0049
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0015:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_001a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0027:  brtrue.s   IL_0065
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0039:  ldarg.0
    IL_003a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_0047:  leave.s    IL_00c7
    IL_0049:  ldarg.0
    IL_004a:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_004f:  stloc.2
    IL_0050:  ldarg.0
    IL_0051:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0056:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.m1
    IL_005e:  dup
    IL_005f:  stloc.0
    IL_0060:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0065:  ldloca.s   V_2
    IL_0067:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006c:  stloc.1
    IL_006d:  ldarg.0
    IL_006e:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_0073:  box        ""T""
    IL_0078:  ldloc.1
    IL_0079:  ldarg.0
    IL_007a:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_007f:  box        ""T""
    IL_0084:  ldloc.1
    IL_0085:  callvirt   ""int IMoveable.this[int].get""
    IL_008a:  ldarg.0
    IL_008b:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0090:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0095:  add
    IL_0096:  callvirt   ""void IMoveable.this[int].set""
    IL_009b:  leave.s    IL_00b4
  }
  catch System.Exception
  {
    IL_009d:  stloc.3
    IL_009e:  ldarg.0
    IL_009f:  ldc.i4.s   -2
    IL_00a1:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00a6:  ldarg.0
    IL_00a7:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00ac:  ldloc.3
    IL_00ad:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00b2:  leave.s    IL_00c7
  }
  IL_00b4:  ldarg.0
  IL_00b5:  ldc.i4.s   -2
  IL_00b7:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00bc:  ldarg.0
  IL_00bd:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00c2:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c7:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Shift2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      205 (0xcd)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0015:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_001a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0039:  ldarg.0
    IL_003a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift2>d__2<T>)""
    IL_0047:  leave      IL_00cc
    IL_004c:  ldarg.0
    IL_004d:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0052:  stloc.2
    IL_0053:  ldarg.0
    IL_0054:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0059:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005f:  ldarg.0
    IL_0060:  ldc.i4.m1
    IL_0061:  dup
    IL_0062:  stloc.0
    IL_0063:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0068:  ldloca.s   V_2
    IL_006a:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006f:  stloc.1
    IL_0070:  ldarg.0
    IL_0071:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0076:  ldloc.1
    IL_0077:  ldarg.0
    IL_0078:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_007d:  ldloc.1
    IL_007e:  constrained. ""T""
    IL_0084:  callvirt   ""int IMoveable.this[int].get""
    IL_0089:  ldarg.0
    IL_008a:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_008f:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0094:  add
    IL_0095:  constrained. ""T""
    IL_009b:  callvirt   ""void IMoveable.this[int].set""
    IL_00a0:  leave.s    IL_00b9
  }
  catch System.Exception
  {
    IL_00a2:  stloc.3
    IL_00a3:  ldarg.0
    IL_00a4:  ldc.i4.s   -2
    IL_00a6:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00ab:  ldarg.0
    IL_00ac:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_00b1:  ldloc.3
    IL_00b2:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00b7:  leave.s    IL_00cc
  }
  IL_00b9:  ldarg.0
  IL_00ba:  ldc.i4.s   -2
  IL_00bc:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_00c1:  ldarg.0
  IL_00c2:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
  IL_00c7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00cc:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_IndexAndValue_Async_03()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int this[int i] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : struct, IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] += GetOffset(ref item);
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] += GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      205 (0xcd)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0015:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_001a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0039:  ldarg.0
    IL_003a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_0047:  leave      IL_00cc
    IL_004c:  ldarg.0
    IL_004d:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0052:  stloc.2
    IL_0053:  ldarg.0
    IL_0054:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0059:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005f:  ldarg.0
    IL_0060:  ldc.i4.m1
    IL_0061:  dup
    IL_0062:  stloc.0
    IL_0063:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0068:  ldloca.s   V_2
    IL_006a:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006f:  stloc.1
    IL_0070:  ldarg.0
    IL_0071:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0076:  ldloc.1
    IL_0077:  ldarg.0
    IL_0078:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_007d:  ldloc.1
    IL_007e:  constrained. ""T""
    IL_0084:  callvirt   ""int IMoveable.this[int].get""
    IL_0089:  ldarg.0
    IL_008a:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_008f:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0094:  add
    IL_0095:  constrained. ""T""
    IL_009b:  callvirt   ""void IMoveable.this[int].set""
    IL_00a0:  leave.s    IL_00b9
  }
  catch System.Exception
  {
    IL_00a2:  stloc.3
    IL_00a3:  ldarg.0
    IL_00a4:  ldc.i4.s   -2
    IL_00a6:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00ab:  ldarg.0
    IL_00ac:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00b1:  ldloc.3
    IL_00b2:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00b7:  leave.s    IL_00cc
  }
  IL_00b9:  ldarg.0
  IL_00ba:  ldc.i4.s   -2
  IL_00bc:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00c1:  ldarg.0
  IL_00c2:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00c7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00cc:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_IndexAndValue_Async_04()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int this[int i] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : class, IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] += await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] += await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-1'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      349 (0x15d)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0053
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00e0
    IL_0011:  ldarg.0
    IL_0012:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0017:  call       ""int Program.GetOffset<T>(ref T)""
    IL_001c:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0021:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0026:  stloc.3
    IL_0027:  ldloca.s   V_3
    IL_0029:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_002e:  brtrue.s   IL_006f
    IL_0030:  ldarg.0
    IL_0031:  ldc.i4.0
    IL_0032:  dup
    IL_0033:  stloc.0
    IL_0034:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0039:  ldarg.0
    IL_003a:  ldloc.3
    IL_003b:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0040:  ldarg.0
    IL_0041:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0046:  ldloca.s   V_3
    IL_0048:  ldarg.0
    IL_0049:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_004e:  leave      IL_015c
    IL_0053:  ldarg.0
    IL_0054:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0059:  stloc.3
    IL_005a:  ldarg.0
    IL_005b:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0060:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0066:  ldarg.0
    IL_0067:  ldc.i4.m1
    IL_0068:  dup
    IL_0069:  stloc.0
    IL_006a:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_006f:  ldloca.s   V_3
    IL_0071:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0076:  stloc.1
    IL_0077:  ldarg.0
    IL_0078:  ldarg.0
    IL_0079:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_007e:  stfld      ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0083:  ldarg.0
    IL_0084:  ldloc.1
    IL_0085:  stfld      ""int Program.<Shift1>d__1<T>.<>7__wrap2""
    IL_008a:  ldarg.0
    IL_008b:  ldarg.0
    IL_008c:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_0091:  box        ""T""
    IL_0096:  ldloc.1
    IL_0097:  callvirt   ""int IMoveable.this[int].get""
    IL_009c:  stfld      ""int Program.<Shift1>d__1<T>.<>7__wrap3""
    IL_00a1:  ldarg.0
    IL_00a2:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_00a7:  call       ""int Program.GetOffset<T>(ref T)""
    IL_00ac:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_00b1:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_00b6:  stloc.3
    IL_00b7:  ldloca.s   V_3
    IL_00b9:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00be:  brtrue.s   IL_00fc
    IL_00c0:  ldarg.0
    IL_00c1:  ldc.i4.1
    IL_00c2:  dup
    IL_00c3:  stloc.0
    IL_00c4:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00c9:  ldarg.0
    IL_00ca:  ldloc.3
    IL_00cb:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_00d0:  ldarg.0
    IL_00d1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00d6:  ldloca.s   V_3
    IL_00d8:  ldarg.0
    IL_00d9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_00de:  leave.s    IL_015c
    IL_00e0:  ldarg.0
    IL_00e1:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_00e6:  stloc.3
    IL_00e7:  ldarg.0
    IL_00e8:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_00ed:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00f3:  ldarg.0
    IL_00f4:  ldc.i4.m1
    IL_00f5:  dup
    IL_00f6:  stloc.0
    IL_00f7:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00fc:  ldloca.s   V_3
    IL_00fe:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0103:  stloc.2
    IL_0104:  ldarg.0
    IL_0105:  ldfld      ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_010a:  box        ""T""
    IL_010f:  ldarg.0
    IL_0110:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap2""
    IL_0115:  ldarg.0
    IL_0116:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap3""
    IL_011b:  ldloc.2
    IL_011c:  add
    IL_011d:  callvirt   ""void IMoveable.this[int].set""
    IL_0122:  ldarg.0
    IL_0123:  ldflda     ""T Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0128:  initobj    ""T""
    IL_012e:  leave.s    IL_0149
  }
  catch System.Exception
  {
    IL_0130:  stloc.s    V_4
    IL_0132:  ldarg.0
    IL_0133:  ldc.i4.s   -2
    IL_0135:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_013a:  ldarg.0
    IL_013b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0140:  ldloc.s    V_4
    IL_0142:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0147:  leave.s    IL_015c
  }
  IL_0149:  ldarg.0
  IL_014a:  ldc.i4.s   -2
  IL_014c:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0151:  ldarg.0
  IL_0152:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_0157:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_015c:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Shift2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      327 (0x147)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0053
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00d5
    IL_0011:  ldarg.0
    IL_0012:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0017:  call       ""int Program.GetOffset<T>(ref T)""
    IL_001c:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0021:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0026:  stloc.3
    IL_0027:  ldloca.s   V_3
    IL_0029:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_002e:  brtrue.s   IL_006f
    IL_0030:  ldarg.0
    IL_0031:  ldc.i4.0
    IL_0032:  dup
    IL_0033:  stloc.0
    IL_0034:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0039:  ldarg.0
    IL_003a:  ldloc.3
    IL_003b:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0040:  ldarg.0
    IL_0041:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_0046:  ldloca.s   V_3
    IL_0048:  ldarg.0
    IL_0049:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift2>d__2<T>)""
    IL_004e:  leave      IL_0146
    IL_0053:  ldarg.0
    IL_0054:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0059:  stloc.3
    IL_005a:  ldarg.0
    IL_005b:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0060:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0066:  ldarg.0
    IL_0067:  ldc.i4.m1
    IL_0068:  dup
    IL_0069:  stloc.0
    IL_006a:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_006f:  ldloca.s   V_3
    IL_0071:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0076:  stloc.1
    IL_0077:  ldarg.0
    IL_0078:  ldloc.1
    IL_0079:  stfld      ""int Program.<Shift2>d__2<T>.<>7__wrap1""
    IL_007e:  ldarg.0
    IL_007f:  ldarg.0
    IL_0080:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0085:  ldloc.1
    IL_0086:  constrained. ""T""
    IL_008c:  callvirt   ""int IMoveable.this[int].get""
    IL_0091:  stfld      ""int Program.<Shift2>d__2<T>.<>7__wrap2""
    IL_0096:  ldarg.0
    IL_0097:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_009c:  call       ""int Program.GetOffset<T>(ref T)""
    IL_00a1:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_00a6:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_00ab:  stloc.3
    IL_00ac:  ldloca.s   V_3
    IL_00ae:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00b3:  brtrue.s   IL_00f1
    IL_00b5:  ldarg.0
    IL_00b6:  ldc.i4.1
    IL_00b7:  dup
    IL_00b8:  stloc.0
    IL_00b9:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00be:  ldarg.0
    IL_00bf:  ldloc.3
    IL_00c0:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_00c5:  ldarg.0
    IL_00c6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_00cb:  ldloca.s   V_3
    IL_00cd:  ldarg.0
    IL_00ce:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift2>d__2<T>)""
    IL_00d3:  leave.s    IL_0146
    IL_00d5:  ldarg.0
    IL_00d6:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_00db:  stloc.3
    IL_00dc:  ldarg.0
    IL_00dd:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_00e2:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00e8:  ldarg.0
    IL_00e9:  ldc.i4.m1
    IL_00ea:  dup
    IL_00eb:  stloc.0
    IL_00ec:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00f1:  ldloca.s   V_3
    IL_00f3:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00f8:  stloc.2
    IL_00f9:  ldarg.0
    IL_00fa:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_00ff:  ldarg.0
    IL_0100:  ldfld      ""int Program.<Shift2>d__2<T>.<>7__wrap1""
    IL_0105:  ldarg.0
    IL_0106:  ldfld      ""int Program.<Shift2>d__2<T>.<>7__wrap2""
    IL_010b:  ldloc.2
    IL_010c:  add
    IL_010d:  constrained. ""T""
    IL_0113:  callvirt   ""void IMoveable.this[int].set""
    IL_0118:  leave.s    IL_0133
  }
  catch System.Exception
  {
    IL_011a:  stloc.s    V_4
    IL_011c:  ldarg.0
    IL_011d:  ldc.i4.s   -2
    IL_011f:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0124:  ldarg.0
    IL_0125:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_012a:  ldloc.s    V_4
    IL_012c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0131:  leave.s    IL_0146
  }
  IL_0133:  ldarg.0
  IL_0134:  ldc.i4.s   -2
  IL_0136:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_013b:  ldarg.0
  IL_013c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
  IL_0141:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0146:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_IndexAndValue_Async_04()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int this[int i] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int this[int i]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return 0;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : struct, IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] += await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] += await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      327 (0x147)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0053
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00d5
    IL_0011:  ldarg.0
    IL_0012:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0017:  call       ""int Program.GetOffset<T>(ref T)""
    IL_001c:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0021:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0026:  stloc.3
    IL_0027:  ldloca.s   V_3
    IL_0029:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_002e:  brtrue.s   IL_006f
    IL_0030:  ldarg.0
    IL_0031:  ldc.i4.0
    IL_0032:  dup
    IL_0033:  stloc.0
    IL_0034:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0039:  ldarg.0
    IL_003a:  ldloc.3
    IL_003b:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0040:  ldarg.0
    IL_0041:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0046:  ldloca.s   V_3
    IL_0048:  ldarg.0
    IL_0049:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_004e:  leave      IL_0146
    IL_0053:  ldarg.0
    IL_0054:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0059:  stloc.3
    IL_005a:  ldarg.0
    IL_005b:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0060:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0066:  ldarg.0
    IL_0067:  ldc.i4.m1
    IL_0068:  dup
    IL_0069:  stloc.0
    IL_006a:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_006f:  ldloca.s   V_3
    IL_0071:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0076:  stloc.1
    IL_0077:  ldarg.0
    IL_0078:  ldloc.1
    IL_0079:  stfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_007e:  ldarg.0
    IL_007f:  ldarg.0
    IL_0080:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0085:  ldloc.1
    IL_0086:  constrained. ""T""
    IL_008c:  callvirt   ""int IMoveable.this[int].get""
    IL_0091:  stfld      ""int Program.<Shift1>d__1<T>.<>7__wrap2""
    IL_0096:  ldarg.0
    IL_0097:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_009c:  call       ""int Program.GetOffset<T>(ref T)""
    IL_00a1:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_00a6:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_00ab:  stloc.3
    IL_00ac:  ldloca.s   V_3
    IL_00ae:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00b3:  brtrue.s   IL_00f1
    IL_00b5:  ldarg.0
    IL_00b6:  ldc.i4.1
    IL_00b7:  dup
    IL_00b8:  stloc.0
    IL_00b9:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00be:  ldarg.0
    IL_00bf:  ldloc.3
    IL_00c0:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_00c5:  ldarg.0
    IL_00c6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00cb:  ldloca.s   V_3
    IL_00cd:  ldarg.0
    IL_00ce:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_00d3:  leave.s    IL_0146
    IL_00d5:  ldarg.0
    IL_00d6:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_00db:  stloc.3
    IL_00dc:  ldarg.0
    IL_00dd:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_00e2:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00e8:  ldarg.0
    IL_00e9:  ldc.i4.m1
    IL_00ea:  dup
    IL_00eb:  stloc.0
    IL_00ec:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00f1:  ldloca.s   V_3
    IL_00f3:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00f8:  stloc.2
    IL_00f9:  ldarg.0
    IL_00fa:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_00ff:  ldarg.0
    IL_0100:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0105:  ldarg.0
    IL_0106:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap2""
    IL_010b:  ldloc.2
    IL_010c:  add
    IL_010d:  constrained. ""T""
    IL_0113:  callvirt   ""void IMoveable.this[int].set""
    IL_0118:  leave.s    IL_0133
  }
  catch System.Exception
  {
    IL_011a:  stloc.s    V_4
    IL_011c:  ldarg.0
    IL_011d:  ldc.i4.s   -2
    IL_011f:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0124:  ldarg.0
    IL_0125:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_012a:  ldloc.s    V_4
    IL_012c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0131:  leave.s    IL_0146
  }
  IL_0133:  ldarg.0
  IL_0134:  ldc.i4.s   -2
  IL_0136:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_013b:  ldarg.0
  IL_013c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_0141:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0146:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Class_Value()
        {
            var source = @"
using System;

interface IMoveable
{
    int? this[int x] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(item1);

        var item2 = new Item {Name = ""2""};
        Shift2(item2);
    }

    static void Shift1<T>(T item) where T : class, IMoveable
    {
        item[1] ??= GetOffset(ref item);
    }

    static void Shift2<T>(T item) where T : IMoveable
    {
        item[1] ??= GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       65 (0x41)
  .maxstack  4
  .locals init (T& V_0,
                int? V_1,
                int V_2,
                int? V_3)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  ldc.i4.1
  IL_0005:  constrained. ""T""
  IL_000b:  callvirt   ""int? IMoveable.this[int].get""
  IL_0010:  stloc.1
  IL_0011:  ldloca.s   V_1
  IL_0013:  call       ""int int?.GetValueOrDefault()""
  IL_0018:  stloc.2
  IL_0019:  ldloca.s   V_1
  IL_001b:  call       ""bool int?.HasValue.get""
  IL_0020:  brtrue.s   IL_0040
  IL_0022:  ldarga.s   V_0
  IL_0024:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0029:  stloc.2
  IL_002a:  ldloc.0
  IL_002b:  ldc.i4.1
  IL_002c:  ldloca.s   V_3
  IL_002e:  ldloc.2
  IL_002f:  call       ""int?..ctor(int)""
  IL_0034:  ldloc.3
  IL_0035:  constrained. ""T""
  IL_003b:  callvirt   ""void IMoveable.this[int].set""
  IL_0040:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Shift2<T>",
@"
{
  // Code size       65 (0x41)
  .maxstack  4
  .locals init (T& V_0,
                int? V_1,
                int V_2,
                int? V_3)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  ldc.i4.1
  IL_0005:  constrained. ""T""
  IL_000b:  callvirt   ""int? IMoveable.this[int].get""
  IL_0010:  stloc.1
  IL_0011:  ldloca.s   V_1
  IL_0013:  call       ""int int?.GetValueOrDefault()""
  IL_0018:  stloc.2
  IL_0019:  ldloca.s   V_1
  IL_001b:  call       ""bool int?.HasValue.get""
  IL_0020:  brtrue.s   IL_0040
  IL_0022:  ldarga.s   V_0
  IL_0024:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0029:  stloc.2
  IL_002a:  ldloc.0
  IL_002b:  ldc.i4.1
  IL_002c:  ldloca.s   V_3
  IL_002e:  ldloc.2
  IL_002f:  call       ""int?..ctor(int)""
  IL_0034:  ldloc.3
  IL_0035:  constrained. ""T""
  IL_003b:  callvirt   ""void IMoveable.this[int].set""
  IL_0040:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Struct_Value()
        {
            var source = @"
using System;

interface IMoveable
{
    int? this[int x] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(item1);

        var item2 = new Item {Name = ""2""};
        Shift2(item2);
    }

    static void Shift1<T>(T item) where T : struct, IMoveable
    {
        item[1] ??= GetOffset(ref item);
    }

    static void Shift2<T>(T item) where T : IMoveable
    {
        item[1] ??= GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       65 (0x41)
  .maxstack  4
  .locals init (T& V_0,
                int? V_1,
                int V_2,
                int? V_3)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  ldc.i4.1
  IL_0005:  constrained. ""T""
  IL_000b:  callvirt   ""int? IMoveable.this[int].get""
  IL_0010:  stloc.1
  IL_0011:  ldloca.s   V_1
  IL_0013:  call       ""int int?.GetValueOrDefault()""
  IL_0018:  stloc.2
  IL_0019:  ldloca.s   V_1
  IL_001b:  call       ""bool int?.HasValue.get""
  IL_0020:  brtrue.s   IL_0040
  IL_0022:  ldarga.s   V_0
  IL_0024:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0029:  stloc.2
  IL_002a:  ldloc.0
  IL_002b:  ldc.i4.1
  IL_002c:  ldloca.s   V_3
  IL_002e:  ldloc.2
  IL_002f:  call       ""int?..ctor(int)""
  IL_0034:  ldloc.3
  IL_0035:  constrained. ""T""
  IL_003b:  callvirt   ""void IMoveable.this[int].set""
  IL_0040:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Class_Value_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    int? this[int x] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(ref item1);

        var item2 = new Item {Name = ""2""};
        Shift2(ref item2);
    }

    static void Shift1<T>(ref T item) where T : class, IMoveable
    {
        item[1] ??= GetOffset(ref item);
    }

    static void Shift2<T>(ref T item) where T : IMoveable
    {
        item[1] ??= GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       63 (0x3f)
  .maxstack  4
  .locals init (T& V_0,
                int? V_1,
                int V_2,
                int? V_3)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  constrained. ""T""
  IL_000a:  callvirt   ""int? IMoveable.this[int].get""
  IL_000f:  stloc.1
  IL_0010:  ldloca.s   V_1
  IL_0012:  call       ""int int?.GetValueOrDefault()""
  IL_0017:  stloc.2
  IL_0018:  ldloca.s   V_1
  IL_001a:  call       ""bool int?.HasValue.get""
  IL_001f:  brtrue.s   IL_003e
  IL_0021:  ldarg.0
  IL_0022:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0027:  stloc.2
  IL_0028:  ldloc.0
  IL_0029:  ldc.i4.1
  IL_002a:  ldloca.s   V_3
  IL_002c:  ldloc.2
  IL_002d:  call       ""int?..ctor(int)""
  IL_0032:  ldloc.3
  IL_0033:  constrained. ""T""
  IL_0039:  callvirt   ""void IMoveable.this[int].set""
  IL_003e:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Shift2<T>",
@"
{
  // Code size       63 (0x3f)
  .maxstack  4
  .locals init (T& V_0,
                int? V_1,
                int V_2,
                int? V_3)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  constrained. ""T""
  IL_000a:  callvirt   ""int? IMoveable.this[int].get""
  IL_000f:  stloc.1
  IL_0010:  ldloca.s   V_1
  IL_0012:  call       ""int int?.GetValueOrDefault()""
  IL_0017:  stloc.2
  IL_0018:  ldloca.s   V_1
  IL_001a:  call       ""bool int?.HasValue.get""
  IL_001f:  brtrue.s   IL_003e
  IL_0021:  ldarg.0
  IL_0022:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0027:  stloc.2
  IL_0028:  ldloc.0
  IL_0029:  ldc.i4.1
  IL_002a:  ldloca.s   V_3
  IL_002c:  ldloc.2
  IL_002d:  call       ""int?..ctor(int)""
  IL_0032:  ldloc.3
  IL_0033:  constrained. ""T""
  IL_0039:  callvirt   ""void IMoveable.this[int].set""
  IL_003e:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Struct_Value_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    int? this[int x] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(ref item1);

        var item2 = new Item {Name = ""2""};
        Shift2(ref item2);
    }

    static void Shift1<T>(ref T item) where T : struct, IMoveable
    {
        item[1] ??= GetOffset(ref item);
    }

    static void Shift2<T>(ref T item) where T : IMoveable
    {
        item[1] ??= GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       63 (0x3f)
  .maxstack  4
  .locals init (T& V_0,
                int? V_1,
                int V_2,
                int? V_3)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  constrained. ""T""
  IL_000a:  callvirt   ""int? IMoveable.this[int].get""
  IL_000f:  stloc.1
  IL_0010:  ldloca.s   V_1
  IL_0012:  call       ""int int?.GetValueOrDefault()""
  IL_0017:  stloc.2
  IL_0018:  ldloca.s   V_1
  IL_001a:  call       ""bool int?.HasValue.get""
  IL_001f:  brtrue.s   IL_003e
  IL_0021:  ldarg.0
  IL_0022:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0027:  stloc.2
  IL_0028:  ldloc.0
  IL_0029:  ldc.i4.1
  IL_002a:  ldloca.s   V_3
  IL_002c:  ldloc.2
  IL_002d:  call       ""int?..ctor(int)""
  IL_0032:  ldloc.3
  IL_0033:  constrained. ""T""
  IL_0039:  callvirt   ""void IMoveable.this[int].set""
  IL_003e:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Class_Value_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int? this[int x] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : class, IMoveable
    {
        item[1] ??= await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[1] ??= await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      217 (0xd9)
  .maxstack  4
  .locals init (int V_0,
                int? V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                int? V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_006c
    IL_000a:  ldarg.0
    IL_000b:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_0010:  box        ""T""
    IL_0015:  ldc.i4.1
    IL_0016:  callvirt   ""int? IMoveable.this[int].get""
    IL_001b:  stloc.1
    IL_001c:  ldloca.s   V_1
    IL_001e:  call       ""int int?.GetValueOrDefault()""
    IL_0023:  stloc.2
    IL_0024:  ldloca.s   V_1
    IL_0026:  call       ""bool int?.HasValue.get""
    IL_002b:  brtrue.s   IL_00aa
    IL_002d:  ldarg.0
    IL_002e:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0033:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0038:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_003d:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0042:  stloc.3
    IL_0043:  ldloca.s   V_3
    IL_0045:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_004a:  brtrue.s   IL_0088
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.0
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0055:  ldarg.0
    IL_0056:  ldloc.3
    IL_0057:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_005c:  ldarg.0
    IL_005d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0062:  ldloca.s   V_3
    IL_0064:  ldarg.0
    IL_0065:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_006a:  leave.s    IL_00d8
    IL_006c:  ldarg.0
    IL_006d:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0072:  stloc.3
    IL_0073:  ldarg.0
    IL_0074:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0079:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_007f:  ldarg.0
    IL_0080:  ldc.i4.m1
    IL_0081:  dup
    IL_0082:  stloc.0
    IL_0083:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0088:  ldloca.s   V_3
    IL_008a:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_008f:  stloc.2
    IL_0090:  ldarg.0
    IL_0091:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_0096:  box        ""T""
    IL_009b:  ldc.i4.1
    IL_009c:  ldloc.2
    IL_009d:  newobj     ""int?..ctor(int)""
    IL_00a2:  dup
    IL_00a3:  stloc.s    V_4
    IL_00a5:  callvirt   ""void IMoveable.this[int].set""
    IL_00aa:  leave.s    IL_00c5
  }
  catch System.Exception
  {
    IL_00ac:  stloc.s    V_5
    IL_00ae:  ldarg.0
    IL_00af:  ldc.i4.s   -2
    IL_00b1:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00b6:  ldarg.0
    IL_00b7:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00bc:  ldloc.s    V_5
    IL_00be:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00c3:  leave.s    IL_00d8
  }
  IL_00c5:  ldarg.0
  IL_00c6:  ldc.i4.s   -2
  IL_00c8:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00cd:  ldarg.0
  IL_00ce:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00d3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00d8:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Shift2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      219 (0xdb)
  .maxstack  4
  .locals init (int V_0,
                int? V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                int? V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_006d
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0010:  ldc.i4.1
    IL_0011:  constrained. ""T""
    IL_0017:  callvirt   ""int? IMoveable.this[int].get""
    IL_001c:  stloc.1
    IL_001d:  ldloca.s   V_1
    IL_001f:  call       ""int int?.GetValueOrDefault()""
    IL_0024:  stloc.2
    IL_0025:  ldloca.s   V_1
    IL_0027:  call       ""bool int?.HasValue.get""
    IL_002c:  brtrue.s   IL_00ac
    IL_002e:  ldarg.0
    IL_002f:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0034:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0039:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_003e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0043:  stloc.3
    IL_0044:  ldloca.s   V_3
    IL_0046:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_004b:  brtrue.s   IL_0089
    IL_004d:  ldarg.0
    IL_004e:  ldc.i4.0
    IL_004f:  dup
    IL_0050:  stloc.0
    IL_0051:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0056:  ldarg.0
    IL_0057:  ldloc.3
    IL_0058:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_005d:  ldarg.0
    IL_005e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_0063:  ldloca.s   V_3
    IL_0065:  ldarg.0
    IL_0066:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift2>d__2<T>)""
    IL_006b:  leave.s    IL_00da
    IL_006d:  ldarg.0
    IL_006e:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0073:  stloc.3
    IL_0074:  ldarg.0
    IL_0075:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_007a:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0080:  ldarg.0
    IL_0081:  ldc.i4.m1
    IL_0082:  dup
    IL_0083:  stloc.0
    IL_0084:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0089:  ldloca.s   V_3
    IL_008b:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0090:  stloc.2
    IL_0091:  ldarg.0
    IL_0092:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0097:  ldc.i4.1
    IL_0098:  ldloc.2
    IL_0099:  newobj     ""int?..ctor(int)""
    IL_009e:  dup
    IL_009f:  stloc.s    V_4
    IL_00a1:  constrained. ""T""
    IL_00a7:  callvirt   ""void IMoveable.this[int].set""
    IL_00ac:  leave.s    IL_00c7
  }
  catch System.Exception
  {
    IL_00ae:  stloc.s    V_5
    IL_00b0:  ldarg.0
    IL_00b1:  ldc.i4.s   -2
    IL_00b3:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00b8:  ldarg.0
    IL_00b9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_00be:  ldloc.s    V_5
    IL_00c0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00c5:  leave.s    IL_00da
  }
  IL_00c7:  ldarg.0
  IL_00c8:  ldc.i4.s   -2
  IL_00ca:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_00cf:  ldarg.0
  IL_00d0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
  IL_00d5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00da:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Struct_Value_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int? this[int x] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : struct, IMoveable
    {
        item[1] ??= await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[1] ??= await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      219 (0xdb)
  .maxstack  4
  .locals init (int V_0,
                int? V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                int? V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_006d
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0010:  ldc.i4.1
    IL_0011:  constrained. ""T""
    IL_0017:  callvirt   ""int? IMoveable.this[int].get""
    IL_001c:  stloc.1
    IL_001d:  ldloca.s   V_1
    IL_001f:  call       ""int int?.GetValueOrDefault()""
    IL_0024:  stloc.2
    IL_0025:  ldloca.s   V_1
    IL_0027:  call       ""bool int?.HasValue.get""
    IL_002c:  brtrue.s   IL_00ac
    IL_002e:  ldarg.0
    IL_002f:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0034:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0039:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_003e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0043:  stloc.3
    IL_0044:  ldloca.s   V_3
    IL_0046:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_004b:  brtrue.s   IL_0089
    IL_004d:  ldarg.0
    IL_004e:  ldc.i4.0
    IL_004f:  dup
    IL_0050:  stloc.0
    IL_0051:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0056:  ldarg.0
    IL_0057:  ldloc.3
    IL_0058:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_005d:  ldarg.0
    IL_005e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0063:  ldloca.s   V_3
    IL_0065:  ldarg.0
    IL_0066:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_006b:  leave.s    IL_00da
    IL_006d:  ldarg.0
    IL_006e:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0073:  stloc.3
    IL_0074:  ldarg.0
    IL_0075:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_007a:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0080:  ldarg.0
    IL_0081:  ldc.i4.m1
    IL_0082:  dup
    IL_0083:  stloc.0
    IL_0084:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0089:  ldloca.s   V_3
    IL_008b:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0090:  stloc.2
    IL_0091:  ldarg.0
    IL_0092:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0097:  ldc.i4.1
    IL_0098:  ldloc.2
    IL_0099:  newobj     ""int?..ctor(int)""
    IL_009e:  dup
    IL_009f:  stloc.s    V_4
    IL_00a1:  constrained. ""T""
    IL_00a7:  callvirt   ""void IMoveable.this[int].set""
    IL_00ac:  leave.s    IL_00c7
  }
  catch System.Exception
  {
    IL_00ae:  stloc.s    V_5
    IL_00b0:  ldarg.0
    IL_00b1:  ldc.i4.s   -2
    IL_00b3:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00b8:  ldarg.0
    IL_00b9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00be:  ldloc.s    V_5
    IL_00c0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00c5:  leave.s    IL_00da
  }
  IL_00c7:  ldarg.0
  IL_00c8:  ldc.i4.s   -2
  IL_00ca:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00cf:  ldarg.0
  IL_00d0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00d5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00da:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Class_IndexAndValue()
        {
            var source = @"
using System;

interface IMoveable
{
    int? this[int x] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(item1);

        var item2 = new Item {Name = ""2""};
        Shift2(item2);
    }

    static void Shift1<T>(T item) where T : class, IMoveable
    {
        item[GetOffset(ref item)] ??= GetOffset(ref item);
    }

    static void Shift2<T>(T item) where T : IMoveable
    {
        item[GetOffset(ref item)] ??= GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       74 (0x4a)
  .maxstack  4
  .locals init (T& V_0,
                int V_1,
                int? V_2,
                int V_3,
                int? V_4)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldarga.s   V_0
  IL_0005:  call       ""int Program.GetOffset<T>(ref T)""
  IL_000a:  stloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  constrained. ""T""
  IL_0013:  callvirt   ""int? IMoveable.this[int].get""
  IL_0018:  stloc.2
  IL_0019:  ldloca.s   V_2
  IL_001b:  call       ""int int?.GetValueOrDefault()""
  IL_0020:  stloc.3
  IL_0021:  ldloca.s   V_2
  IL_0023:  call       ""bool int?.HasValue.get""
  IL_0028:  brtrue.s   IL_0049
  IL_002a:  ldarga.s   V_0
  IL_002c:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0031:  stloc.3
  IL_0032:  ldloc.0
  IL_0033:  ldloc.1
  IL_0034:  ldloca.s   V_4
  IL_0036:  ldloc.3
  IL_0037:  call       ""int?..ctor(int)""
  IL_003c:  ldloc.s    V_4
  IL_003e:  constrained. ""T""
  IL_0044:  callvirt   ""void IMoveable.this[int].set""
  IL_0049:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Shift2<T>",
@"
{
  // Code size       74 (0x4a)
  .maxstack  4
  .locals init (T& V_0,
                int V_1,
                int? V_2,
                int V_3,
                int? V_4)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldarga.s   V_0
  IL_0005:  call       ""int Program.GetOffset<T>(ref T)""
  IL_000a:  stloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  constrained. ""T""
  IL_0013:  callvirt   ""int? IMoveable.this[int].get""
  IL_0018:  stloc.2
  IL_0019:  ldloca.s   V_2
  IL_001b:  call       ""int int?.GetValueOrDefault()""
  IL_0020:  stloc.3
  IL_0021:  ldloca.s   V_2
  IL_0023:  call       ""bool int?.HasValue.get""
  IL_0028:  brtrue.s   IL_0049
  IL_002a:  ldarga.s   V_0
  IL_002c:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0031:  stloc.3
  IL_0032:  ldloc.0
  IL_0033:  ldloc.1
  IL_0034:  ldloca.s   V_4
  IL_0036:  ldloc.3
  IL_0037:  call       ""int?..ctor(int)""
  IL_003c:  ldloc.s    V_4
  IL_003e:  constrained. ""T""
  IL_0044:  callvirt   ""void IMoveable.this[int].set""
  IL_0049:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Struct_IndexAndValue()
        {
            var source = @"
using System;

interface IMoveable
{
    int? this[int x] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(item1);

        var item2 = new Item {Name = ""2""};
        Shift2(item2);
    }

    static void Shift1<T>(T item) where T : struct, IMoveable
    {
        item[GetOffset(ref item)] ??= GetOffset(ref item);
    }

    static void Shift2<T>(T item) where T : IMoveable
    {
        item[GetOffset(ref item)] ??= GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       74 (0x4a)
  .maxstack  4
  .locals init (T& V_0,
                int V_1,
                int? V_2,
                int V_3,
                int? V_4)
  IL_0000:  ldarga.s   V_0
  IL_0002:  stloc.0
  IL_0003:  ldarga.s   V_0
  IL_0005:  call       ""int Program.GetOffset<T>(ref T)""
  IL_000a:  stloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  constrained. ""T""
  IL_0013:  callvirt   ""int? IMoveable.this[int].get""
  IL_0018:  stloc.2
  IL_0019:  ldloca.s   V_2
  IL_001b:  call       ""int int?.GetValueOrDefault()""
  IL_0020:  stloc.3
  IL_0021:  ldloca.s   V_2
  IL_0023:  call       ""bool int?.HasValue.get""
  IL_0028:  brtrue.s   IL_0049
  IL_002a:  ldarga.s   V_0
  IL_002c:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0031:  stloc.3
  IL_0032:  ldloc.0
  IL_0033:  ldloc.1
  IL_0034:  ldloca.s   V_4
  IL_0036:  ldloc.3
  IL_0037:  call       ""int?..ctor(int)""
  IL_003c:  ldloc.s    V_4
  IL_003e:  constrained. ""T""
  IL_0044:  callvirt   ""void IMoveable.this[int].set""
  IL_0049:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Class_IndexAndValue_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    int? this[int x] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(ref item1);

        var item2 = new Item {Name = ""2""};
        Shift2(ref item2);
    }

    static void Shift1<T>(ref T item) where T : class, IMoveable
    {
        item[GetOffset(ref item)] ??= GetOffset(ref item);
    }

    static void Shift2<T>(ref T item) where T : IMoveable
    {
        item[GetOffset(ref item)] ??= GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics();

            // Wrong IL
            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       71 (0x47)
  .maxstack  4
  .locals init (T& V_0,
                int V_1,
                int? V_2,
                int V_3,
                int? V_4)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0008:  stloc.1
  IL_0009:  ldloc.0
  IL_000a:  ldloc.1
  IL_000b:  constrained. ""T""
  IL_0011:  callvirt   ""int? IMoveable.this[int].get""
  IL_0016:  stloc.2
  IL_0017:  ldloca.s   V_2
  IL_0019:  call       ""int int?.GetValueOrDefault()""
  IL_001e:  stloc.3
  IL_001f:  ldloca.s   V_2
  IL_0021:  call       ""bool int?.HasValue.get""
  IL_0026:  brtrue.s   IL_0046
  IL_0028:  ldarg.0
  IL_0029:  call       ""int Program.GetOffset<T>(ref T)""
  IL_002e:  stloc.3
  IL_002f:  ldloc.0
  IL_0030:  ldloc.1
  IL_0031:  ldloca.s   V_4
  IL_0033:  ldloc.3
  IL_0034:  call       ""int?..ctor(int)""
  IL_0039:  ldloc.s    V_4
  IL_003b:  constrained. ""T""
  IL_0041:  callvirt   ""void IMoveable.this[int].set""
  IL_0046:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.Shift2<T>",
@"
{
  // Code size       71 (0x47)
  .maxstack  4
  .locals init (T& V_0,
                int V_1,
                int? V_2,
                int V_3,
                int? V_4)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0008:  stloc.1
  IL_0009:  ldloc.0
  IL_000a:  ldloc.1
  IL_000b:  constrained. ""T""
  IL_0011:  callvirt   ""int? IMoveable.this[int].get""
  IL_0016:  stloc.2
  IL_0017:  ldloca.s   V_2
  IL_0019:  call       ""int int?.GetValueOrDefault()""
  IL_001e:  stloc.3
  IL_001f:  ldloca.s   V_2
  IL_0021:  call       ""bool int?.HasValue.get""
  IL_0026:  brtrue.s   IL_0046
  IL_0028:  ldarg.0
  IL_0029:  call       ""int Program.GetOffset<T>(ref T)""
  IL_002e:  stloc.3
  IL_002f:  ldloc.0
  IL_0030:  ldloc.1
  IL_0031:  ldloca.s   V_4
  IL_0033:  ldloc.3
  IL_0034:  call       ""int?..ctor(int)""
  IL_0039:  ldloc.s    V_4
  IL_003b:  constrained. ""T""
  IL_0041:  callvirt   ""void IMoveable.this[int].set""
  IL_0046:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Struct_IndexAndValue_Ref()
        {
            var source = @"
using System;

interface IMoveable
{
    int? this[int x] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static void Main()
    {
        var item1 = new Item {Name = ""1""};
        Shift1(ref item1);

        var item2 = new Item {Name = ""2""};
        Shift2(ref item2);
    }

    static void Shift1<T>(ref T item) where T : struct, IMoveable
    {
        item[GetOffset(ref item)] ??= GetOffset(ref item);
    }

    static void Shift2<T>(ref T item) where T : IMoveable
    {
        item[GetOffset(ref item)] ??= GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Shift1<T>",
@"
{
  // Code size       71 (0x47)
  .maxstack  4
  .locals init (T& V_0,
                int V_1,
                int? V_2,
                int V_3,
                int? V_4)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  call       ""int Program.GetOffset<T>(ref T)""
  IL_0008:  stloc.1
  IL_0009:  ldloc.0
  IL_000a:  ldloc.1
  IL_000b:  constrained. ""T""
  IL_0011:  callvirt   ""int? IMoveable.this[int].get""
  IL_0016:  stloc.2
  IL_0017:  ldloca.s   V_2
  IL_0019:  call       ""int int?.GetValueOrDefault()""
  IL_001e:  stloc.3
  IL_001f:  ldloca.s   V_2
  IL_0021:  call       ""bool int?.HasValue.get""
  IL_0026:  brtrue.s   IL_0046
  IL_0028:  ldarg.0
  IL_0029:  call       ""int Program.GetOffset<T>(ref T)""
  IL_002e:  stloc.3
  IL_002f:  ldloc.0
  IL_0030:  ldloc.1
  IL_0031:  ldloca.s   V_4
  IL_0033:  ldloc.3
  IL_0034:  call       ""int?..ctor(int)""
  IL_0039:  ldloc.s    V_4
  IL_003b:  constrained. ""T""
  IL_0041:  callvirt   ""void IMoveable.this[int].set""
  IL_0046:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Class_IndexAndValue_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int? this[int x] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : class, IMoveable
    {
        item[GetOffset(ref item)] ??= await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[GetOffset(ref item)] ??= await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      247 (0xf7)
  .maxstack  4
  .locals init (int V_0,
                int? V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                int? V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0085
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0011:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0016:  stfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_001b:  ldarg.0
    IL_001c:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_0021:  box        ""T""
    IL_0026:  ldarg.0
    IL_0027:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_002c:  callvirt   ""int? IMoveable.this[int].get""
    IL_0031:  stloc.1
    IL_0032:  ldloca.s   V_1
    IL_0034:  call       ""int int?.GetValueOrDefault()""
    IL_0039:  stloc.2
    IL_003a:  ldloca.s   V_1
    IL_003c:  call       ""bool int?.HasValue.get""
    IL_0041:  brtrue     IL_00c8
    IL_0046:  ldarg.0
    IL_0047:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_004c:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0051:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0056:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_005b:  stloc.3
    IL_005c:  ldloca.s   V_3
    IL_005e:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0063:  brtrue.s   IL_00a1
    IL_0065:  ldarg.0
    IL_0066:  ldc.i4.0
    IL_0067:  dup
    IL_0068:  stloc.0
    IL_0069:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_006e:  ldarg.0
    IL_006f:  ldloc.3
    IL_0070:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0075:  ldarg.0
    IL_0076:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_007b:  ldloca.s   V_3
    IL_007d:  ldarg.0
    IL_007e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_0083:  leave.s    IL_00f6
    IL_0085:  ldarg.0
    IL_0086:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_008b:  stloc.3
    IL_008c:  ldarg.0
    IL_008d:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0092:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0098:  ldarg.0
    IL_0099:  ldc.i4.m1
    IL_009a:  dup
    IL_009b:  stloc.0
    IL_009c:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00a1:  ldloca.s   V_3
    IL_00a3:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00a8:  stloc.2
    IL_00a9:  ldarg.0
    IL_00aa:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_00af:  box        ""T""
    IL_00b4:  ldarg.0
    IL_00b5:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_00ba:  ldloc.2
    IL_00bb:  newobj     ""int?..ctor(int)""
    IL_00c0:  dup
    IL_00c1:  stloc.s    V_4
    IL_00c3:  callvirt   ""void IMoveable.this[int].set""
    IL_00c8:  leave.s    IL_00e3
  }
  catch System.Exception
  {
    IL_00ca:  stloc.s    V_5
    IL_00cc:  ldarg.0
    IL_00cd:  ldc.i4.s   -2
    IL_00cf:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00d4:  ldarg.0
    IL_00d5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00da:  ldloc.s    V_5
    IL_00dc:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00e1:  leave.s    IL_00f6
  }
  IL_00e3:  ldarg.0
  IL_00e4:  ldc.i4.s   -2
  IL_00e6:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00eb:  ldarg.0
  IL_00ec:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00f1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00f6:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Shift2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      249 (0xf9)
  .maxstack  4
  .locals init (int V_0,
                int? V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                int? V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0086
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0011:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0016:  stfld      ""int Program.<Shift2>d__2<T>.<>7__wrap1""
    IL_001b:  ldarg.0
    IL_001c:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0021:  ldarg.0
    IL_0022:  ldfld      ""int Program.<Shift2>d__2<T>.<>7__wrap1""
    IL_0027:  constrained. ""T""
    IL_002d:  callvirt   ""int? IMoveable.this[int].get""
    IL_0032:  stloc.1
    IL_0033:  ldloca.s   V_1
    IL_0035:  call       ""int int?.GetValueOrDefault()""
    IL_003a:  stloc.2
    IL_003b:  ldloca.s   V_1
    IL_003d:  call       ""bool int?.HasValue.get""
    IL_0042:  brtrue     IL_00ca
    IL_0047:  ldarg.0
    IL_0048:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_004d:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0052:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0057:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_005c:  stloc.3
    IL_005d:  ldloca.s   V_3
    IL_005f:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0064:  brtrue.s   IL_00a2
    IL_0066:  ldarg.0
    IL_0067:  ldc.i4.0
    IL_0068:  dup
    IL_0069:  stloc.0
    IL_006a:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_006f:  ldarg.0
    IL_0070:  ldloc.3
    IL_0071:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0076:  ldarg.0
    IL_0077:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_007c:  ldloca.s   V_3
    IL_007e:  ldarg.0
    IL_007f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift2>d__2<T>)""
    IL_0084:  leave.s    IL_00f8
    IL_0086:  ldarg.0
    IL_0087:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_008c:  stloc.3
    IL_008d:  ldarg.0
    IL_008e:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0093:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0099:  ldarg.0
    IL_009a:  ldc.i4.m1
    IL_009b:  dup
    IL_009c:  stloc.0
    IL_009d:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00a2:  ldloca.s   V_3
    IL_00a4:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00a9:  stloc.2
    IL_00aa:  ldarg.0
    IL_00ab:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_00b0:  ldarg.0
    IL_00b1:  ldfld      ""int Program.<Shift2>d__2<T>.<>7__wrap1""
    IL_00b6:  ldloc.2
    IL_00b7:  newobj     ""int?..ctor(int)""
    IL_00bc:  dup
    IL_00bd:  stloc.s    V_4
    IL_00bf:  constrained. ""T""
    IL_00c5:  callvirt   ""void IMoveable.this[int].set""
    IL_00ca:  leave.s    IL_00e5
  }
  catch System.Exception
  {
    IL_00cc:  stloc.s    V_5
    IL_00ce:  ldarg.0
    IL_00cf:  ldc.i4.s   -2
    IL_00d1:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00d6:  ldarg.0
    IL_00d7:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_00dc:  ldloc.s    V_5
    IL_00de:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00e3:  leave.s    IL_00f8
  }
  IL_00e5:  ldarg.0
  IL_00e6:  ldc.i4.s   -2
  IL_00e8:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_00ed:  ldarg.0
  IL_00ee:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
  IL_00f3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00f8:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Struct_IndexAndValue_Async_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int? this[int x] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : struct, IMoveable
    {
        item[GetOffset(ref item)] ??= await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[GetOffset(ref item)] ??= await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      249 (0xf9)
  .maxstack  4
  .locals init (int V_0,
                int? V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                int? V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0086
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0011:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0016:  stfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_001b:  ldarg.0
    IL_001c:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0021:  ldarg.0
    IL_0022:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0027:  constrained. ""T""
    IL_002d:  callvirt   ""int? IMoveable.this[int].get""
    IL_0032:  stloc.1
    IL_0033:  ldloca.s   V_1
    IL_0035:  call       ""int int?.GetValueOrDefault()""
    IL_003a:  stloc.2
    IL_003b:  ldloca.s   V_1
    IL_003d:  call       ""bool int?.HasValue.get""
    IL_0042:  brtrue     IL_00ca
    IL_0047:  ldarg.0
    IL_0048:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_004d:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0052:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0057:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_005c:  stloc.3
    IL_005d:  ldloca.s   V_3
    IL_005f:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0064:  brtrue.s   IL_00a2
    IL_0066:  ldarg.0
    IL_0067:  ldc.i4.0
    IL_0068:  dup
    IL_0069:  stloc.0
    IL_006a:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_006f:  ldarg.0
    IL_0070:  ldloc.3
    IL_0071:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0076:  ldarg.0
    IL_0077:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_007c:  ldloca.s   V_3
    IL_007e:  ldarg.0
    IL_007f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_0084:  leave.s    IL_00f8
    IL_0086:  ldarg.0
    IL_0087:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_008c:  stloc.3
    IL_008d:  ldarg.0
    IL_008e:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0093:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0099:  ldarg.0
    IL_009a:  ldc.i4.m1
    IL_009b:  dup
    IL_009c:  stloc.0
    IL_009d:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00a2:  ldloca.s   V_3
    IL_00a4:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00a9:  stloc.2
    IL_00aa:  ldarg.0
    IL_00ab:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_00b0:  ldarg.0
    IL_00b1:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_00b6:  ldloc.2
    IL_00b7:  newobj     ""int?..ctor(int)""
    IL_00bc:  dup
    IL_00bd:  stloc.s    V_4
    IL_00bf:  constrained. ""T""
    IL_00c5:  callvirt   ""void IMoveable.this[int].set""
    IL_00ca:  leave.s    IL_00e5
  }
  catch System.Exception
  {
    IL_00cc:  stloc.s    V_5
    IL_00ce:  ldarg.0
    IL_00cf:  ldc.i4.s   -2
    IL_00d1:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00d6:  ldarg.0
    IL_00d7:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00dc:  ldloc.s    V_5
    IL_00de:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00e3:  leave.s    IL_00f8
  }
  IL_00e5:  ldarg.0
  IL_00e6:  ldc.i4.s   -2
  IL_00e8:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00ed:  ldarg.0
  IL_00ee:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00f3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00f8:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Class_IndexAndValue_Async_03()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int? this[int x] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : class, IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] ??= GetOffset(ref item);
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] ??= GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      235 (0xeb)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                int? V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                int? V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004e
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0015:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_001a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_001f:  stloc.s    V_4
    IL_0021:  ldloca.s   V_4
    IL_0023:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0028:  brtrue.s   IL_006b
    IL_002a:  ldarg.0
    IL_002b:  ldc.i4.0
    IL_002c:  dup
    IL_002d:  stloc.0
    IL_002e:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0033:  ldarg.0
    IL_0034:  ldloc.s    V_4
    IL_0036:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_003b:  ldarg.0
    IL_003c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0041:  ldloca.s   V_4
    IL_0043:  ldarg.0
    IL_0044:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_0049:  leave      IL_00ea
    IL_004e:  ldarg.0
    IL_004f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0054:  stloc.s    V_4
    IL_0056:  ldarg.0
    IL_0057:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_005c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0062:  ldarg.0
    IL_0063:  ldc.i4.m1
    IL_0064:  dup
    IL_0065:  stloc.0
    IL_0066:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_006b:  ldloca.s   V_4
    IL_006d:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0072:  stloc.1
    IL_0073:  ldarg.0
    IL_0074:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_0079:  box        ""T""
    IL_007e:  ldloc.1
    IL_007f:  callvirt   ""int? IMoveable.this[int].get""
    IL_0084:  stloc.2
    IL_0085:  ldloca.s   V_2
    IL_0087:  call       ""int int?.GetValueOrDefault()""
    IL_008c:  stloc.3
    IL_008d:  ldloca.s   V_2
    IL_008f:  call       ""bool int?.HasValue.get""
    IL_0094:  brtrue.s   IL_00bc
    IL_0096:  ldarg.0
    IL_0097:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_009c:  call       ""int Program.GetOffset<T>(ref T)""
    IL_00a1:  stloc.3
    IL_00a2:  ldarg.0
    IL_00a3:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_00a8:  box        ""T""
    IL_00ad:  ldloc.1
    IL_00ae:  ldloc.3
    IL_00af:  newobj     ""int?..ctor(int)""
    IL_00b4:  dup
    IL_00b5:  stloc.s    V_5
    IL_00b7:  callvirt   ""void IMoveable.this[int].set""
    IL_00bc:  leave.s    IL_00d7
  }
  catch System.Exception
  {
    IL_00be:  stloc.s    V_6
    IL_00c0:  ldarg.0
    IL_00c1:  ldc.i4.s   -2
    IL_00c3:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00c8:  ldarg.0
    IL_00c9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00ce:  ldloc.s    V_6
    IL_00d0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00d5:  leave.s    IL_00ea
  }
  IL_00d7:  ldarg.0
  IL_00d8:  ldc.i4.s   -2
  IL_00da:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00df:  ldarg.0
  IL_00e0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00e5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00ea:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Shift2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      237 (0xed)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                int? V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                int? V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004e
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0015:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_001a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_001f:  stloc.s    V_4
    IL_0021:  ldloca.s   V_4
    IL_0023:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0028:  brtrue.s   IL_006b
    IL_002a:  ldarg.0
    IL_002b:  ldc.i4.0
    IL_002c:  dup
    IL_002d:  stloc.0
    IL_002e:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0033:  ldarg.0
    IL_0034:  ldloc.s    V_4
    IL_0036:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_003b:  ldarg.0
    IL_003c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_0041:  ldloca.s   V_4
    IL_0043:  ldarg.0
    IL_0044:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift2>d__2<T>)""
    IL_0049:  leave      IL_00ec
    IL_004e:  ldarg.0
    IL_004f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0054:  stloc.s    V_4
    IL_0056:  ldarg.0
    IL_0057:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_005c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0062:  ldarg.0
    IL_0063:  ldc.i4.m1
    IL_0064:  dup
    IL_0065:  stloc.0
    IL_0066:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_006b:  ldloca.s   V_4
    IL_006d:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0072:  stloc.1
    IL_0073:  ldarg.0
    IL_0074:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0079:  ldloc.1
    IL_007a:  constrained. ""T""
    IL_0080:  callvirt   ""int? IMoveable.this[int].get""
    IL_0085:  stloc.2
    IL_0086:  ldloca.s   V_2
    IL_0088:  call       ""int int?.GetValueOrDefault()""
    IL_008d:  stloc.3
    IL_008e:  ldloca.s   V_2
    IL_0090:  call       ""bool int?.HasValue.get""
    IL_0095:  brtrue.s   IL_00be
    IL_0097:  ldarg.0
    IL_0098:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_009d:  call       ""int Program.GetOffset<T>(ref T)""
    IL_00a2:  stloc.3
    IL_00a3:  ldarg.0
    IL_00a4:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_00a9:  ldloc.1
    IL_00aa:  ldloc.3
    IL_00ab:  newobj     ""int?..ctor(int)""
    IL_00b0:  dup
    IL_00b1:  stloc.s    V_5
    IL_00b3:  constrained. ""T""
    IL_00b9:  callvirt   ""void IMoveable.this[int].set""
    IL_00be:  leave.s    IL_00d9
  }
  catch System.Exception
  {
    IL_00c0:  stloc.s    V_6
    IL_00c2:  ldarg.0
    IL_00c3:  ldc.i4.s   -2
    IL_00c5:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00ca:  ldarg.0
    IL_00cb:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_00d0:  ldloc.s    V_6
    IL_00d2:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00d7:  leave.s    IL_00ec
  }
  IL_00d9:  ldarg.0
  IL_00da:  ldc.i4.s   -2
  IL_00dc:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_00e1:  ldarg.0
  IL_00e2:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
  IL_00e7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00ec:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Struct_IndexAndValue_Async_03()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int? this[int x] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : struct, IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] ??= GetOffset(ref item);
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] ??= GetOffset(ref item);
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      237 (0xed)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                int? V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                int? V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004e
    IL_000a:  ldarg.0
    IL_000b:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0010:  call       ""int Program.GetOffset<T>(ref T)""
    IL_0015:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_001a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_001f:  stloc.s    V_4
    IL_0021:  ldloca.s   V_4
    IL_0023:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0028:  brtrue.s   IL_006b
    IL_002a:  ldarg.0
    IL_002b:  ldc.i4.0
    IL_002c:  dup
    IL_002d:  stloc.0
    IL_002e:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0033:  ldarg.0
    IL_0034:  ldloc.s    V_4
    IL_0036:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_003b:  ldarg.0
    IL_003c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0041:  ldloca.s   V_4
    IL_0043:  ldarg.0
    IL_0044:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_0049:  leave      IL_00ec
    IL_004e:  ldarg.0
    IL_004f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0054:  stloc.s    V_4
    IL_0056:  ldarg.0
    IL_0057:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_005c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0062:  ldarg.0
    IL_0063:  ldc.i4.m1
    IL_0064:  dup
    IL_0065:  stloc.0
    IL_0066:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_006b:  ldloca.s   V_4
    IL_006d:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0072:  stloc.1
    IL_0073:  ldarg.0
    IL_0074:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0079:  ldloc.1
    IL_007a:  constrained. ""T""
    IL_0080:  callvirt   ""int? IMoveable.this[int].get""
    IL_0085:  stloc.2
    IL_0086:  ldloca.s   V_2
    IL_0088:  call       ""int int?.GetValueOrDefault()""
    IL_008d:  stloc.3
    IL_008e:  ldloca.s   V_2
    IL_0090:  call       ""bool int?.HasValue.get""
    IL_0095:  brtrue.s   IL_00be
    IL_0097:  ldarg.0
    IL_0098:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_009d:  call       ""int Program.GetOffset<T>(ref T)""
    IL_00a2:  stloc.3
    IL_00a3:  ldarg.0
    IL_00a4:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_00a9:  ldloc.1
    IL_00aa:  ldloc.3
    IL_00ab:  newobj     ""int?..ctor(int)""
    IL_00b0:  dup
    IL_00b1:  stloc.s    V_5
    IL_00b3:  constrained. ""T""
    IL_00b9:  callvirt   ""void IMoveable.this[int].set""
    IL_00be:  leave.s    IL_00d9
  }
  catch System.Exception
  {
    IL_00c0:  stloc.s    V_6
    IL_00c2:  ldarg.0
    IL_00c3:  ldc.i4.s   -2
    IL_00c5:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00ca:  ldarg.0
    IL_00cb:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00d0:  ldloc.s    V_6
    IL_00d2:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00d7:  leave.s    IL_00ec
  }
  IL_00d9:  ldarg.0
  IL_00da:  ldc.i4.s   -2
  IL_00dc:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_00e1:  ldarg.0
  IL_00e2:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_00e7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00ec:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Class_IndexAndValue_Async_04()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int? this[int x] {get;set;}
}

class Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : class, IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] ??= await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] ??= await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            // Wrong output
            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      352 (0x160)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                int? V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                int? V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0055
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ed
    IL_0011:  ldarg.0
    IL_0012:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0017:  call       ""int Program.GetOffset<T>(ref T)""
    IL_001c:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0021:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0026:  stloc.s    V_4
    IL_0028:  ldloca.s   V_4
    IL_002a:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_002f:  brtrue.s   IL_0072
    IL_0031:  ldarg.0
    IL_0032:  ldc.i4.0
    IL_0033:  dup
    IL_0034:  stloc.0
    IL_0035:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_003a:  ldarg.0
    IL_003b:  ldloc.s    V_4
    IL_003d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0042:  ldarg.0
    IL_0043:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0048:  ldloca.s   V_4
    IL_004a:  ldarg.0
    IL_004b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_0050:  leave      IL_015f
    IL_0055:  ldarg.0
    IL_0056:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_005b:  stloc.s    V_4
    IL_005d:  ldarg.0
    IL_005e:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0063:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0069:  ldarg.0
    IL_006a:  ldc.i4.m1
    IL_006b:  dup
    IL_006c:  stloc.0
    IL_006d:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0072:  ldloca.s   V_4
    IL_0074:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0079:  stloc.1
    IL_007a:  ldarg.0
    IL_007b:  ldloc.1
    IL_007c:  stfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0081:  ldarg.0
    IL_0082:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_0087:  box        ""T""
    IL_008c:  ldarg.0
    IL_008d:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0092:  callvirt   ""int? IMoveable.this[int].get""
    IL_0097:  stloc.2
    IL_0098:  ldloca.s   V_2
    IL_009a:  call       ""int int?.GetValueOrDefault()""
    IL_009f:  stloc.3
    IL_00a0:  ldloca.s   V_2
    IL_00a2:  call       ""bool int?.HasValue.get""
    IL_00a7:  brtrue     IL_0131
    IL_00ac:  ldarg.0
    IL_00ad:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_00b2:  call       ""int Program.GetOffset<T>(ref T)""
    IL_00b7:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_00bc:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_00c1:  stloc.s    V_4
    IL_00c3:  ldloca.s   V_4
    IL_00c5:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00ca:  brtrue.s   IL_010a
    IL_00cc:  ldarg.0
    IL_00cd:  ldc.i4.1
    IL_00ce:  dup
    IL_00cf:  stloc.0
    IL_00d0:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00d5:  ldarg.0
    IL_00d6:  ldloc.s    V_4
    IL_00d8:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_00dd:  ldarg.0
    IL_00de:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00e3:  ldloca.s   V_4
    IL_00e5:  ldarg.0
    IL_00e6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_00eb:  leave.s    IL_015f
    IL_00ed:  ldarg.0
    IL_00ee:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_00f3:  stloc.s    V_4
    IL_00f5:  ldarg.0
    IL_00f6:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_00fb:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0101:  ldarg.0
    IL_0102:  ldc.i4.m1
    IL_0103:  dup
    IL_0104:  stloc.0
    IL_0105:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_010a:  ldloca.s   V_4
    IL_010c:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0111:  stloc.3
    IL_0112:  ldarg.0
    IL_0113:  ldfld      ""T Program.<Shift1>d__1<T>.item""
    IL_0118:  box        ""T""
    IL_011d:  ldarg.0
    IL_011e:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0123:  ldloc.3
    IL_0124:  newobj     ""int?..ctor(int)""
    IL_0129:  dup
    IL_012a:  stloc.s    V_5
    IL_012c:  callvirt   ""void IMoveable.this[int].set""
    IL_0131:  leave.s    IL_014c
  }
  catch System.Exception
  {
    IL_0133:  stloc.s    V_6
    IL_0135:  ldarg.0
    IL_0136:  ldc.i4.s   -2
    IL_0138:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_013d:  ldarg.0
    IL_013e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0143:  ldloc.s    V_6
    IL_0145:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_014a:  leave.s    IL_015f
  }
  IL_014c:  ldarg.0
  IL_014d:  ldc.i4.s   -2
  IL_014f:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0154:  ldarg.0
  IL_0155:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_015a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_015f:  ret
}
");

            // Wrong IL
            verifier.VerifyIL("Program.<Shift2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      354 (0x162)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                int? V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                int? V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0055
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ee
    IL_0011:  ldarg.0
    IL_0012:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0017:  call       ""int Program.GetOffset<T>(ref T)""
    IL_001c:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0021:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0026:  stloc.s    V_4
    IL_0028:  ldloca.s   V_4
    IL_002a:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_002f:  brtrue.s   IL_0072
    IL_0031:  ldarg.0
    IL_0032:  ldc.i4.0
    IL_0033:  dup
    IL_0034:  stloc.0
    IL_0035:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_003a:  ldarg.0
    IL_003b:  ldloc.s    V_4
    IL_003d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0042:  ldarg.0
    IL_0043:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_0048:  ldloca.s   V_4
    IL_004a:  ldarg.0
    IL_004b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift2>d__2<T>)""
    IL_0050:  leave      IL_0161
    IL_0055:  ldarg.0
    IL_0056:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_005b:  stloc.s    V_4
    IL_005d:  ldarg.0
    IL_005e:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_0063:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0069:  ldarg.0
    IL_006a:  ldc.i4.m1
    IL_006b:  dup
    IL_006c:  stloc.0
    IL_006d:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_0072:  ldloca.s   V_4
    IL_0074:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0079:  stloc.1
    IL_007a:  ldarg.0
    IL_007b:  ldloc.1
    IL_007c:  stfld      ""int Program.<Shift2>d__2<T>.<>7__wrap1""
    IL_0081:  ldarg.0
    IL_0082:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0087:  ldarg.0
    IL_0088:  ldfld      ""int Program.<Shift2>d__2<T>.<>7__wrap1""
    IL_008d:  constrained. ""T""
    IL_0093:  callvirt   ""int? IMoveable.this[int].get""
    IL_0098:  stloc.2
    IL_0099:  ldloca.s   V_2
    IL_009b:  call       ""int int?.GetValueOrDefault()""
    IL_00a0:  stloc.3
    IL_00a1:  ldloca.s   V_2
    IL_00a3:  call       ""bool int?.HasValue.get""
    IL_00a8:  brtrue     IL_0133
    IL_00ad:  ldarg.0
    IL_00ae:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_00b3:  call       ""int Program.GetOffset<T>(ref T)""
    IL_00b8:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_00bd:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_00c2:  stloc.s    V_4
    IL_00c4:  ldloca.s   V_4
    IL_00c6:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00cb:  brtrue.s   IL_010b
    IL_00cd:  ldarg.0
    IL_00ce:  ldc.i4.1
    IL_00cf:  dup
    IL_00d0:  stloc.0
    IL_00d1:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_00d6:  ldarg.0
    IL_00d7:  ldloc.s    V_4
    IL_00d9:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_00de:  ldarg.0
    IL_00df:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_00e4:  ldloca.s   V_4
    IL_00e6:  ldarg.0
    IL_00e7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift2>d__2<T>)""
    IL_00ec:  leave.s    IL_0161
    IL_00ee:  ldarg.0
    IL_00ef:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_00f4:  stloc.s    V_4
    IL_00f6:  ldarg.0
    IL_00f7:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift2>d__2<T>.<>u__1""
    IL_00fc:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0102:  ldarg.0
    IL_0103:  ldc.i4.m1
    IL_0104:  dup
    IL_0105:  stloc.0
    IL_0106:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_010b:  ldloca.s   V_4
    IL_010d:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0112:  stloc.3
    IL_0113:  ldarg.0
    IL_0114:  ldflda     ""T Program.<Shift2>d__2<T>.item""
    IL_0119:  ldarg.0
    IL_011a:  ldfld      ""int Program.<Shift2>d__2<T>.<>7__wrap1""
    IL_011f:  ldloc.3
    IL_0120:  newobj     ""int?..ctor(int)""
    IL_0125:  dup
    IL_0126:  stloc.s    V_5
    IL_0128:  constrained. ""T""
    IL_012e:  callvirt   ""void IMoveable.this[int].set""
    IL_0133:  leave.s    IL_014e
  }
  catch System.Exception
  {
    IL_0135:  stloc.s    V_6
    IL_0137:  ldarg.0
    IL_0138:  ldc.i4.s   -2
    IL_013a:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
    IL_013f:  ldarg.0
    IL_0140:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
    IL_0145:  ldloc.s    V_6
    IL_0147:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_014c:  leave.s    IL_0161
  }
  IL_014e:  ldarg.0
  IL_014f:  ldc.i4.s   -2
  IL_0151:  stfld      ""int Program.<Shift2>d__2<T>.<>1__state""
  IL_0156:  ldarg.0
  IL_0157:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift2>d__2<T>.<>t__builder""
  IL_015c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0161:  ret
}
");
        }

        [Fact]
        [WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")]
        public void GenericTypeParameterAsReceiver_Assignment_Conditional_Indexer_Struct_IndexAndValue_Async_04()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface IMoveable
{
    int? this[int x] {get;set;}
}

struct Item : IMoveable
{
    public string Name {get; set;}

    public int? this[int x]
    {
        get
        {
            Console.WriteLine(""Position get for item '{0}'"", Name);
            return null;
        }
        set
        {
            Console.WriteLine(""Position set for item '{0}'"", Name);
        }
    }
}

class Program
{
    static async Task Main()
    {
        var item1 = new Item {Name = ""1""};
        await Shift1(item1);

        var item2 = new Item {Name = ""2""};
        await Shift2(item2);
    }

    static async Task Shift1<T>(T item) where T : struct, IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] ??= await GetOffsetAsync(GetOffset(ref item));
    }

    static async Task Shift2<T>(T item) where T : IMoveable
    {
        item[await GetOffsetAsync(GetOffset(ref item))] ??= await GetOffsetAsync(GetOffset(ref item));
    }
    
    static int value = 0;
    static int GetOffset<T>(ref T item)
    {
        item = (T)(IMoveable)new Item {Name = (--value).ToString()};
        return 0;
    }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
    static async Task<int> GetOffsetAsync(int i)
    {
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Shift1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      354 (0x162)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                int? V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                int? V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0055
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ee
    IL_0011:  ldarg.0
    IL_0012:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0017:  call       ""int Program.GetOffset<T>(ref T)""
    IL_001c:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_0021:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0026:  stloc.s    V_4
    IL_0028:  ldloca.s   V_4
    IL_002a:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_002f:  brtrue.s   IL_0072
    IL_0031:  ldarg.0
    IL_0032:  ldc.i4.0
    IL_0033:  dup
    IL_0034:  stloc.0
    IL_0035:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_003a:  ldarg.0
    IL_003b:  ldloc.s    V_4
    IL_003d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0042:  ldarg.0
    IL_0043:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0048:  ldloca.s   V_4
    IL_004a:  ldarg.0
    IL_004b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_0050:  leave      IL_0161
    IL_0055:  ldarg.0
    IL_0056:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_005b:  stloc.s    V_4
    IL_005d:  ldarg.0
    IL_005e:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_0063:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0069:  ldarg.0
    IL_006a:  ldc.i4.m1
    IL_006b:  dup
    IL_006c:  stloc.0
    IL_006d:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_0072:  ldloca.s   V_4
    IL_0074:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0079:  stloc.1
    IL_007a:  ldarg.0
    IL_007b:  ldloc.1
    IL_007c:  stfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_0081:  ldarg.0
    IL_0082:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0087:  ldarg.0
    IL_0088:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_008d:  constrained. ""T""
    IL_0093:  callvirt   ""int? IMoveable.this[int].get""
    IL_0098:  stloc.2
    IL_0099:  ldloca.s   V_2
    IL_009b:  call       ""int int?.GetValueOrDefault()""
    IL_00a0:  stloc.3
    IL_00a1:  ldloca.s   V_2
    IL_00a3:  call       ""bool int?.HasValue.get""
    IL_00a8:  brtrue     IL_0133
    IL_00ad:  ldarg.0
    IL_00ae:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_00b3:  call       ""int Program.GetOffset<T>(ref T)""
    IL_00b8:  call       ""System.Threading.Tasks.Task<int> Program.GetOffsetAsync(int)""
    IL_00bd:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_00c2:  stloc.s    V_4
    IL_00c4:  ldloca.s   V_4
    IL_00c6:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00cb:  brtrue.s   IL_010b
    IL_00cd:  ldarg.0
    IL_00ce:  ldc.i4.1
    IL_00cf:  dup
    IL_00d0:  stloc.0
    IL_00d1:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_00d6:  ldarg.0
    IL_00d7:  ldloc.s    V_4
    IL_00d9:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_00de:  ldarg.0
    IL_00df:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_00e4:  ldloca.s   V_4
    IL_00e6:  ldarg.0
    IL_00e7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Shift1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Shift1>d__1<T>)""
    IL_00ec:  leave.s    IL_0161
    IL_00ee:  ldarg.0
    IL_00ef:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_00f4:  stloc.s    V_4
    IL_00f6:  ldarg.0
    IL_00f7:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Shift1>d__1<T>.<>u__1""
    IL_00fc:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0102:  ldarg.0
    IL_0103:  ldc.i4.m1
    IL_0104:  dup
    IL_0105:  stloc.0
    IL_0106:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_010b:  ldloca.s   V_4
    IL_010d:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0112:  stloc.3
    IL_0113:  ldarg.0
    IL_0114:  ldflda     ""T Program.<Shift1>d__1<T>.item""
    IL_0119:  ldarg.0
    IL_011a:  ldfld      ""int Program.<Shift1>d__1<T>.<>7__wrap1""
    IL_011f:  ldloc.3
    IL_0120:  newobj     ""int?..ctor(int)""
    IL_0125:  dup
    IL_0126:  stloc.s    V_5
    IL_0128:  constrained. ""T""
    IL_012e:  callvirt   ""void IMoveable.this[int].set""
    IL_0133:  leave.s    IL_014e
  }
  catch System.Exception
  {
    IL_0135:  stloc.s    V_6
    IL_0137:  ldarg.0
    IL_0138:  ldc.i4.s   -2
    IL_013a:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
    IL_013f:  ldarg.0
    IL_0140:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
    IL_0145:  ldloc.s    V_6
    IL_0147:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_014c:  leave.s    IL_0161
  }
  IL_014e:  ldarg.0
  IL_014f:  ldc.i4.s   -2
  IL_0151:  stfld      ""int Program.<Shift1>d__1<T>.<>1__state""
  IL_0156:  ldarg.0
  IL_0157:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Shift1>d__1<T>.<>t__builder""
  IL_015c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0161:  ret
}
");
        }
    }
}
