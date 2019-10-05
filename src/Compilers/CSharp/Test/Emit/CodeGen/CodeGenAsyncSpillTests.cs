// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenAsyncSpillTests : EmitMetadataTestBase
    {
        public CodeGenAsyncSpillTests()
        {
        }

        private CompilationVerifier CompileAndVerify(string source, string expectedOutput = null, IEnumerable<MetadataReference> references = null, CSharpCompilationOptions options = null)
        {
            return base.CompileAndVerify(source, expectedOutput: expectedOutput, references: references, options: options);
        }

        [Fact]
        public void AsyncWithTernary()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static Task<T> F<T>(T x)
    {
        Console.WriteLine(""F("" + x + "")"");
        return Task.Factory.StartNew(() => { return x; });
    }

    public static async Task<int> G(bool b1, bool b2)
    {
        int c = 0;
        c = c + (b1 ? 1 : await F(2));
        c = c + (b2 ? await F(4) : 8);
        return await F(c);
    }

    public static int H(bool b1, bool b2)
    {
        Task<int> t = G(b1, b2);
        t.Wait(1000 * 60);
        return t.Result;
    }

    public static void Main()
    {
        Console.WriteLine(H(false, false));
        Console.WriteLine(H(false, true));
        Console.WriteLine(H(true, false));
        Console.WriteLine(H(true, true));
    }
}";
            var expected = @"
F(2)
F(10)
10
F(2)
F(4)
F(6)
6
F(9)
9
F(4)
F(5)
5
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncWithAnd()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static Task<T> F<T>(T x)
    {
        Console.WriteLine(""F("" + x + "")"");
        return Task.Factory.StartNew(() => { return x; });
    }

    public static async Task<int> G(bool b1, bool b2)
    {
        bool x1 = b1 && await F(true);
        bool x2 = b1 && await F(false);
        bool x3 = b2 && await F(true);
        bool x4 = b2 && await F(false);
        int c = 0;
        if (x1) c += 1;
        if (x2) c += 2;
        if (x3) c += 4;
        if (x4) c += 8;
        return await F(c);
    }

    public static int H(bool b1, bool b2)
    {
        Task<int> t = G(b1, b2);
        t.Wait(1000 * 60);
        return t.Result;
    }

    public static void Main()
    {
        Console.WriteLine(H(false, true));
    }
}";
            var expected = @"
F(True)
F(False)
F(4)
4
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncWithOr()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static Task<T> F<T>(T x)
    {
        Console.WriteLine(""F("" + x + "")"");
        return Task.Factory.StartNew(() => { return x; });
    }

    public static async Task<int> G(bool b1, bool b2)
    {
        bool x1 = b1 || await F(true);
        bool x2 = b1 || await F(false);
        bool x3 = b2 || await F(true);
        bool x4 = b2 || await F(false);
        int c = 0;
        if (x1) c += 1;
        if (x2) c += 2;
        if (x3) c += 4;
        if (x4) c += 8;
        return await F(c);
    }

    public static int H(bool b1, bool b2)
    {
        Task<int> t = G(b1, b2);
        t.Wait(1000 * 60);
        return t.Result;
    }

    public static void Main()
    {
        Console.WriteLine(H(false, true));
    }
}";
            var expected = @"
F(True)
F(False)
F(13)
13
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AsyncWithCoalesce()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static Task<string> F(string x)
    {
        Console.WriteLine(""F("" + (x ?? ""null"") + "")"");
        return Task.Factory.StartNew(() => { return x; });
    }

    public static async Task<string> G(string s1, string s2)
    {
        var result = await F(s1) ?? await F(s2);
        Console.WriteLine("" "" + (result ?? ""null""));
        return result;
    }

    public static string H(string s1, string s2)
    {
        Task<string> t = G(s1, s2);
        t.Wait(1000 * 60);
        return t.Result;
    }

    public static void Main()
    {
        H(null, null);
        H(null, ""a"");
        H(""b"", null);
        H(""c"", ""d"");
    }
}";
            var expected = @"
F(null)
F(null)
 null
F(null)
F(a)
 a
F(b)
 b
F(c)
 c
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void AwaitInExpr()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int> F()
    {
        return await Task.Factory.StartNew(() => 21);
    }

    public static async Task<int> G()
    {
        int c = 0;
        c = (await F()) + 21;
        return c;
    }

    public static void Main()
    {
        Task<int> t = G();
        t.Wait(1000 * 60);
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void SpillNestedUnary()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int> F()
    {
        return 1;
    }

    public static async Task<int> G1()
    {
        return -(await F());
    }

    public static async Task<int> G2()
    {
        return -(-(await F()));
    }

    public static async Task<int> G3()
    {
        return -(-(-(await F())));
    }

    public static void WaitAndPrint(Task<int> t)
    {
        t.Wait();
        Console.WriteLine(t.Result);
    }

    public static void Main()
    {
        WaitAndPrint(G1());
        WaitAndPrint(G2());
        WaitAndPrint(G3());
    }
}";
            var expected = @"
-1
1
-1
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void AsyncWithParamsAndLocals_DoubleAwait_Spilling()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => { return x; });
    }

    public static async Task<int> G(int x)
    {
        int c = 0;
        c = (await F(x)) + c;
        c = (await F(x)) + c;
        return c;
    }

    public static void Main()
    {
        Task<int> t = G(21);
        t.Wait(1000 * 60);
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
42
";
            // When the local 'c' gets hoisted, the statement:
            //   c = (await F(x)) + c;
            // Gets rewritten to:
            //   this.c_field = (await F(x)) + this.c_field;
            //
            // The code-gen for the assignment is something like this:
            //   ldarg0  // load the 'this' reference to the stack
            //   <emitted await expression>
            //   stfld
            //
            // What we really want is to evaluate any parts of the lvalue that have side-effects (which is this case is
            // nothing), and then defer loading the address for the field reference until after the await expression:
            //   <emitted await expression>
            //   <store to tmp>
            //   ldarg0
            //   <load tmp>
            //   stfld
            //
            // So this case actually requires stack spilling, which is not yet implemented. This has the unfortunate
            // consequence of preventing await expressions from being assigned to hoisted locals.
            //
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void SpillCall()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    public static void Printer(int a, int b, int c, int d, int e)
    {
        foreach (var x in new List<int>() { a, b, c, d, e })
        {
            Console.WriteLine(x);
        }
    }

    public static int Get(int x)
    {
        Console.WriteLine(""> "" + x);
        return x;
    }

    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => x);
    }

    public static async Task G()
    {
        Printer(Get(111), Get(222), Get(333), await F(Get(444)), Get(555));
    }

    public static void Main()
    {
        Task t = G();
        t.Wait();
    }
}";
            var expected = @"
> 111
> 222
> 333
> 444
> 555
111
222
333
444
555
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillCall2()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    public static void Printer(int a, int b, int c, int d, int e)
    {
        foreach (var x in new List<int>() { a, b, c, d, e })
        {
            Console.WriteLine(x);
        }
    }

    public static int Get(int x)
    {
        Console.WriteLine(""> "" + x);
        return x;
    }

    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => x);
    }

    public static async Task G()
    {
        Printer(Get(111), await F(Get(222)), Get(333), await F(Get(444)), Get(555));
    }

    public static void Main()
    {
        Task t = G();
        t.Wait();
    }
}";
            var expected = @"
> 111
> 222
> 333
> 444
> 555
111
222
333
444
555
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillCall3()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    public static void Printer(int a, int b, int c, int d, int e, int f)
    {
        foreach (var x in new List<int>(){a, b, c, d, e, f})
        {
            Console.WriteLine(x);
        }
    }

    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => x);
    }

    public static async Task G()
    {
        Printer(1, await F(2), 3, await F(await F(await F(await F(4)))), await F(5), 6);
    }

    public static void Main()
    {
        Task t = G();
        t.Wait();
    }
}";
            var expected = @"
1
2
3
4
5
6
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillCall4()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    public static void Printer(int a, int b)
    {
        foreach (var x in new List<int>(){a, b})
        {
            Console.WriteLine(x);
        }
    }

    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => x);
    }

    public static async Task G()
    {
        Printer(1, await F(await F(2)));
    }

    public static void Main()
    {
        Task t = G();
        t.Wait();
    }
}";
            var expected = @"
1
2
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillSequences1()
        {
            var source = @"
using System.Threading.Tasks;

public class Test
{
    public static int H(int a, int b, int c)
    {
        return a;
    }

    public static Task<int> G()
    {
        return null;
    }

    public static async Task<int> F(int[] array)
    {
        H(array[1] += 2, array[3] += await G(), 4);
        return 1;
    }
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("Test.<F>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"{
  // Code size      273 (0x111)
  .maxstack  5
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                Test.<F>d__2 V_4,
                System.Exception V_5)
 ~IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<F>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
   ~IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_0088
   -IL_000e:  nop
   -IL_000f:  ldarg.0
    IL_0010:  ldarg.0
    IL_0011:  ldfld      ""int[] Test.<F>d__2.array""
    IL_0016:  ldc.i4.1
    IL_0017:  ldelema    ""int""
    IL_001c:  dup
    IL_001d:  ldind.i4
    IL_001e:  ldc.i4.2
    IL_001f:  add
    IL_0020:  dup
    IL_0021:  stloc.2
    IL_0022:  stind.i4
    IL_0023:  ldloc.2
    IL_0024:  stfld      ""int Test.<F>d__2.<>s__1""
    IL_0029:  ldarg.0
    IL_002a:  ldarg.0
    IL_002b:  ldfld      ""int[] Test.<F>d__2.array""
    IL_0030:  stfld      ""int[] Test.<F>d__2.<>s__4""
    IL_0035:  ldarg.0
    IL_0036:  ldfld      ""int[] Test.<F>d__2.<>s__4""
    IL_003b:  ldc.i4.3
    IL_003c:  ldelem.i4
    IL_003d:  pop
    IL_003e:  ldarg.0
    IL_003f:  ldarg.0
    IL_0040:  ldfld      ""int[] Test.<F>d__2.<>s__4""
    IL_0045:  ldc.i4.3
    IL_0046:  ldelem.i4
    IL_0047:  stfld      ""int Test.<F>d__2.<>s__2""
    IL_004c:  call       ""System.Threading.Tasks.Task<int> Test.G()""
    IL_0051:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0056:  stloc.3
   ~IL_0057:  ldloca.s   V_3
    IL_0059:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_005e:  brtrue.s   IL_00a4
    IL_0060:  ldarg.0
    IL_0061:  ldc.i4.0
    IL_0062:  dup
    IL_0063:  stloc.0
    IL_0064:  stfld      ""int Test.<F>d__2.<>1__state""
   <IL_0069:  ldarg.0
    IL_006a:  ldloc.3
    IL_006b:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__2.<>u__1""
    IL_0070:  ldarg.0
    IL_0071:  stloc.s    V_4
    IL_0073:  ldarg.0
    IL_0074:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__2.<>t__builder""
    IL_0079:  ldloca.s   V_3
    IL_007b:  ldloca.s   V_4
    IL_007d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<F>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<F>d__2)""
    IL_0082:  nop
    IL_0083:  leave      IL_0110
   >IL_0088:  ldarg.0
    IL_0089:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__2.<>u__1""
    IL_008e:  stloc.3
    IL_008f:  ldarg.0
    IL_0090:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__2.<>u__1""
    IL_0095:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_009b:  ldarg.0
    IL_009c:  ldc.i4.m1
    IL_009d:  dup
    IL_009e:  stloc.0
    IL_009f:  stfld      ""int Test.<F>d__2.<>1__state""
    IL_00a4:  ldarg.0
    IL_00a5:  ldloca.s   V_3
    IL_00a7:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00ac:  stfld      ""int Test.<F>d__2.<>s__3""
    IL_00b1:  ldarg.0
    IL_00b2:  ldfld      ""int Test.<F>d__2.<>s__1""
    IL_00b7:  ldarg.0
    IL_00b8:  ldfld      ""int[] Test.<F>d__2.<>s__4""
    IL_00bd:  ldc.i4.3
    IL_00be:  ldarg.0
    IL_00bf:  ldfld      ""int Test.<F>d__2.<>s__2""
    IL_00c4:  ldarg.0
    IL_00c5:  ldfld      ""int Test.<F>d__2.<>s__3""
    IL_00ca:  add
    IL_00cb:  dup
    IL_00cc:  stloc.2
    IL_00cd:  stelem.i4
    IL_00ce:  ldloc.2
    IL_00cf:  ldc.i4.4
    IL_00d0:  call       ""int Test.H(int, int, int)""
    IL_00d5:  pop
    IL_00d6:  ldarg.0
    IL_00d7:  ldnull
    IL_00d8:  stfld      ""int[] Test.<F>d__2.<>s__4""
   -IL_00dd:  ldc.i4.1
    IL_00de:  stloc.1
    IL_00df:  leave.s    IL_00fb
  }
  catch System.Exception
  {
   ~IL_00e1:  stloc.s    V_5
    IL_00e3:  ldarg.0
    IL_00e4:  ldc.i4.s   -2
    IL_00e6:  stfld      ""int Test.<F>d__2.<>1__state""
    IL_00eb:  ldarg.0
    IL_00ec:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__2.<>t__builder""
    IL_00f1:  ldloc.s    V_5
    IL_00f3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00f8:  nop
    IL_00f9:  leave.s    IL_0110
  }
 -IL_00fb:  ldarg.0
  IL_00fc:  ldc.i4.s   -2
  IL_00fe:  stfld      ""int Test.<F>d__2.<>1__state""
 ~IL_0103:  ldarg.0
  IL_0104:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__2.<>t__builder""
  IL_0109:  ldloc.1
  IL_010a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_010f:  nop
  IL_0110:  ret
}", sequencePoints: "Test+<F>d__2.MoveNext");
        }

        [Fact]
        public void SpillSequencesRelease()
        {
            var source = @"
using System.Threading.Tasks;

public class Test
{
    public static int H(int a, int b, int c)
    {
        return a;
    }

    public static Task<int> G()
    {
        return null;
    }

    public static async Task<int> F(int[] array)
    {
        H(array[1] += 2, array[3] += await G(), 4);
        return 1;
    }
}
";
            var v = CompileAndVerify(source, options: TestOptions.ReleaseDll);

            v.VerifyIL("Test.<F>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      251 (0xfb)
  .maxstack  5
  .locals init (int V_0,
                int V_1,
                int V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                System.Exception V_5)
 ~IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<F>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
   ~IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_007d
   -IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""int[] Test.<F>d__2.array""
    IL_0011:  ldc.i4.1
    IL_0012:  ldelema    ""int""
    IL_0017:  dup
    IL_0018:  ldind.i4
    IL_0019:  ldc.i4.2
    IL_001a:  add
    IL_001b:  dup
    IL_001c:  stloc.3
    IL_001d:  stind.i4
    IL_001e:  ldloc.3
    IL_001f:  stfld      ""int Test.<F>d__2.<>7__wrap1""
    IL_0024:  ldarg.0
    IL_0025:  ldarg.0
    IL_0026:  ldfld      ""int[] Test.<F>d__2.array""
    IL_002b:  stfld      ""int[] Test.<F>d__2.<>7__wrap3""
    IL_0030:  ldarg.0
    IL_0031:  ldfld      ""int[] Test.<F>d__2.<>7__wrap3""
    IL_0036:  ldc.i4.3
    IL_0037:  ldelem.i4
    IL_0038:  pop
    IL_0039:  ldarg.0
    IL_003a:  ldarg.0
    IL_003b:  ldfld      ""int[] Test.<F>d__2.<>7__wrap3""
    IL_0040:  ldc.i4.3
    IL_0041:  ldelem.i4
    IL_0042:  stfld      ""int Test.<F>d__2.<>7__wrap2""
    IL_0047:  call       ""System.Threading.Tasks.Task<int> Test.G()""
    IL_004c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0051:  stloc.s    V_4
   ~IL_0053:  ldloca.s   V_4
    IL_0055:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_005a:  brtrue.s   IL_009a
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.0
    IL_005e:  dup
    IL_005f:  stloc.0
    IL_0060:  stfld      ""int Test.<F>d__2.<>1__state""
   <IL_0065:  ldarg.0
    IL_0066:  ldloc.s    V_4
    IL_0068:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__2.<>u__1""
    IL_006d:  ldarg.0
    IL_006e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__2.<>t__builder""
    IL_0073:  ldloca.s   V_4
    IL_0075:  ldarg.0
    IL_0076:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<F>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<F>d__2)""
    IL_007b:  leave.s    IL_00fa
   >IL_007d:  ldarg.0
    IL_007e:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__2.<>u__1""
    IL_0083:  stloc.s    V_4
    IL_0085:  ldarg.0
    IL_0086:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__2.<>u__1""
    IL_008b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0091:  ldarg.0
    IL_0092:  ldc.i4.m1
    IL_0093:  dup
    IL_0094:  stloc.0
    IL_0095:  stfld      ""int Test.<F>d__2.<>1__state""
    IL_009a:  ldloca.s   V_4
    IL_009c:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00a1:  stloc.2
    IL_00a2:  ldarg.0
    IL_00a3:  ldfld      ""int Test.<F>d__2.<>7__wrap1""
    IL_00a8:  ldarg.0
    IL_00a9:  ldfld      ""int[] Test.<F>d__2.<>7__wrap3""
    IL_00ae:  ldc.i4.3
    IL_00af:  ldarg.0
    IL_00b0:  ldfld      ""int Test.<F>d__2.<>7__wrap2""
    IL_00b5:  ldloc.2
    IL_00b6:  add
    IL_00b7:  dup
    IL_00b8:  stloc.3
    IL_00b9:  stelem.i4
    IL_00ba:  ldloc.3
    IL_00bb:  ldc.i4.4
    IL_00bc:  call       ""int Test.H(int, int, int)""
    IL_00c1:  pop
    IL_00c2:  ldarg.0
    IL_00c3:  ldnull
    IL_00c4:  stfld      ""int[] Test.<F>d__2.<>7__wrap3""
   -IL_00c9:  ldc.i4.1
    IL_00ca:  stloc.1
    IL_00cb:  leave.s    IL_00e6
  }
  catch System.Exception
  {
   ~IL_00cd:  stloc.s    V_5
    IL_00cf:  ldarg.0
    IL_00d0:  ldc.i4.s   -2
    IL_00d2:  stfld      ""int Test.<F>d__2.<>1__state""
    IL_00d7:  ldarg.0
    IL_00d8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__2.<>t__builder""
    IL_00dd:  ldloc.s    V_5
    IL_00df:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00e4:  leave.s    IL_00fa
  }
 -IL_00e6:  ldarg.0
  IL_00e7:  ldc.i4.s   -2
  IL_00e9:  stfld      ""int Test.<F>d__2.<>1__state""
 ~IL_00ee:  ldarg.0
  IL_00ef:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__2.<>t__builder""
  IL_00f4:  ldloc.1
  IL_00f5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00fa:  ret
}", sequencePoints: "Test+<F>d__2.MoveNext");
        }

        [Fact]
        public void SpillSequencesInConditionalExpression1()
        {
            var source = @"
using System.Threading.Tasks;

public class Test
{
    public static int H(int a, int b, int c)
    {
        return a;
    }

    public static Task<int> G()
    {
        return null;
    }

    public static async Task<int> F(int[] array)
    {
        H(0, (1 == await G()) ? array[3] += await G() : 1, 4);
        return 1;
    }
}
";
            CompileAndVerify(source, options: TestOptions.DebugDll);
        }

        [Fact]
        public void SpillSequencesInNullCoalescingOperator1()
        {
            var source = @"
using System.Threading.Tasks;

public class C
{
    public static int H(int a, object b, int c)
    {
        return a;
    }

    public static object O(int a)
    {
        return null;
    }

    public static Task<int> G()
    {
        return null;
    }

    public static async Task<int> F(int[] array)
    {
        H(0, O(array[0] += await G()) ?? (array[1] += await G()), 4);
        return 1;
    }
}
";
            CompileAndVerify(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                AssertEx.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "array",
                    "<>7__wrap1",
                    "<>7__wrap2",
                    "<>u__1",
                    "<>7__wrap3",
                    "<>7__wrap4",
                }, module.GetFieldNames("C.<F>d__3"));
            });

            CompileAndVerify(source, verify: Verification.Passes, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
             {
                 AssertEx.Equal(new[]
                 {
                    "<>1__state",
                    "<>t__builder",
                    "array",
                    "<>s__1",
                    "<>s__2",
                    "<>s__3",
                    "<>s__4",
                    "<>s__5",
                    "<>s__6",
                    "<>s__7",
                    "<>u__1",
                    "<>s__8"
                 }, module.GetFieldNames("C.<F>d__3"));
             });
        }

        [WorkItem(4628, "https://github.com/dotnet/roslyn/issues/4628")]
        [Fact]
        public void AsyncWithShortCircuiting001()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace AsyncConditionalBug {
  class Program {
    private readonly bool b=true;

    private async Task AsyncMethod() {
      Console.WriteLine(b && await Task.FromResult(false));
      Console.WriteLine(b); 
    }

    static void Main(string[] args) {
      new Program().AsyncMethod().Wait();
    }
  }
}";
            var expected = @"
False
True
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [WorkItem(4628, "https://github.com/dotnet/roslyn/issues/4628")]
        [Fact]
        public void AsyncWithShortCircuiting002()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace AsyncConditionalBug {
  class Program {
    private static readonly bool b=true;

    private async Task AsyncMethod() {
      Console.WriteLine(b && await Task.FromResult(false));
      Console.WriteLine(b); 
    }

    static void Main(string[] args) {
      new Program().AsyncMethod().Wait();
    }
  }
}";
            var expected = @"
False
True
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [WorkItem(4628, "https://github.com/dotnet/roslyn/issues/4628")]
        [Fact]
        public void AsyncWithShortCircuiting003()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace AsyncConditionalBug
{
    class Program
    {
        private readonly string NULL = null;

        private async Task AsyncMethod()
        {
            Console.WriteLine(NULL ?? await Task.FromResult(""hello""));
            Console.WriteLine(NULL);
        }

        static void Main(string[] args)
        {
            new Program().AsyncMethod().Wait();
        }
    }
}";
            var expected = @"
hello
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [WorkItem(4638, "https://github.com/dotnet/roslyn/issues/4638")]
        [Fact]
        public void AsyncWithShortCircuiting004()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace AsyncConditionalBug
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                DoSomething(Tuple.Create(1.ToString(), Guid.NewGuid())).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                System.Console.Write(ex.Message);
            }
        }

        public static async Task DoSomething(Tuple<string, Guid> item)
        {
            if (item.Item2 != null || await IsValid(item.Item2))
            {
                throw new Exception(""Not Valid!"");
            };
        }

        private static async Task<bool> IsValid(Guid id)
        {
            return false;
        }
    }
}";
            var expected = @"
Not Valid!
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void SpillSequencesInLogicalBinaryOperator1()
        {
            var source = @"
using System.Threading.Tasks;

public class Test
{
    public static int H(int a, bool b, int c)
    {
        return a;
    }

    public static bool B(int a)
    {
        return true;
    }

    public static Task<int> G()
    {
        return null;
    }

    public static async Task<int> F(int[] array)
    {
        H(0, B(array[0] += await G()) || B(array[1] += await G()), 4);
        return 1;
    }
}
";
            CompileAndVerify(source, options: TestOptions.DebugDll);
        }

        [Fact]
        public void SpillArray01()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            tests++;
            int[] arr = new int[await GetVal(4)];
            if (arr.Length == 4)
                Driver.Count++;

            //multidimensional
            tests++;
            decimal[,] arr2 = new decimal[await GetVal(4), await GetVal(4)];
            if (arr2.Rank == 2 && arr2.Length == 16)
                Driver.Count++;

            arr2 = new decimal[4, await GetVal(4)];
            if (arr2.Rank == 2 && arr2.Length == 16)
                Driver.Count++;

            tests++;
            arr2 = new decimal[await GetVal(4), 4];
            if (arr2.Rank == 2 && arr2.Length == 16)
                Driver.Count++;


            //jagged array
            tests++;
            decimal?[][] arr3 = new decimal?[await GetVal(4)][];
            if (arr3.Rank == 2 && arr3.Length == 4)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillArray02_1()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            tests++;
            int[] arr = new int[await GetVal(4)];
            if (arr.Length == 4)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillArray02_2()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            int[] arr = new int[4];    
        
            tests++;
            arr[0] = await GetVal(4);
            if (arr[0] == 4)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillArray02_3()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            int[] arr = new int[4];  
            arr[0] = 4;  
            
            tests++;
            arr[0] += await GetVal(4);
            if (arr[0] == 8)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillArray02_4()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            int[] arr = new int[] { 8, 0, 0, 0 };

            tests++;
            arr[1] += await (GetVal(arr[0]));
            if (arr[1] == 8)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillArray02_5()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            int[] arr = new int[] { 8, 8, 0, 0 };
            
            tests++;
            arr[1] += await (GetVal(arr[await GetVal(0)]));
            if (arr[1] == 16)
                Driver.Count++;

            tests++;
            arr[await GetVal(2)]++;
            if (arr[2] == 1)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillArray02_6()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            int[] arr = new int[4];

            tests++;
            arr[await GetVal(2)]++;
            if (arr[2] == 1)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillArray03()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            int[,] arr = new int[await GetVal(4), await GetVal(4)];

            tests++;
            arr[0, 0] = await GetVal(4);
            if (arr[0, await (GetVal(0))] == 4)
                Driver.Count++;

            tests++;
            arr[0, 0] += await GetVal(4);
            if (arr[0, 0] == 8)
                Driver.Count++;

            tests++;
            arr[1, 1] += await (GetVal(arr[0, 0]));
            if (arr[1, 1] == 8)
                Driver.Count++;

            tests++;
            arr[1, 1] += await (GetVal(arr[0, await GetVal(0)]));
            if (arr[1, 1] == 16)
                Driver.Count++;

            tests++;
            arr[2, await GetVal(2)]++;
            if (arr[2, 2] == 1)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillArray04()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System;

struct MyStruct<T>
{
    T t { get; set; }
    public T this[T index]
    {
        get
        {
            return t;
        }
        set
        {
            t = value;
        }
    }
}

struct TestCase
{
    public async void Run()
    {
        try
        {
            MyStruct<int> ms = new MyStruct<int>();
            var x = ms[index: await Goo()];
        }
        finally
        {
            Driver.CompletedSignal.Set();
        }
    }
    public async Task<int> Goo()
    {
        await Task.Delay(1);
        return 1;
    }
}

class Driver
{
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();
        CompletedSignal.WaitOne();
    }
}";
            CompileAndVerify(source, "");
        }

        [Fact]
        public void SpillArrayAssign()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class TestCase
{
    static int[] arr = new int[4];

    static async Task Run()
    {
        arr[0] = await Task.Factory.StartNew(() => 42);
    }

    static void Main()
    {
        Task task = Run();
        task.Wait();
        Console.WriteLine(arr[0]);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expected);
        }

        [WorkItem(19609, "https://github.com/dotnet/roslyn/issues/19609")]
        [Fact]
        public void SpillArrayAssign2()
        {
            var source = @"
using System.Threading.Tasks;

class Program
{
    static int[] array = new int[5];

    static void Main(string[] args)
    {
        try
        {
            System.Console.WriteLine(""test not awaited"");
            TestNotAwaited().Wait();
        }
        catch
        {
            System.Console.WriteLine(""exception thrown"");
        }

    System.Console.WriteLine();

        try
        {
            System.Console.WriteLine(""test awaited"");
            TestAwaited().Wait();
        }
        catch
        {
            System.Console.WriteLine(""exception thrown"");
        }

    }

    static async Task TestNotAwaited()
    {
        array[6] = Moo1();
    }

    static async Task TestAwaited()
    {
        array[6] = await Moo();
    }

    static int Moo1()
    {
        System.Console.WriteLine(""hello"");
        return 123;
    }

    static async Task<int> Moo()
    {
        System.Console.WriteLine(""hello"");
        return 123;
    }
}";

            var expected = @"
test not awaited
hello
exception thrown

test awaited
hello
exception thrown
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void SpillArrayLocal()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int[] arr = new int[2] { -1, 42 };

        int tests = 0;
        try
        {
            tests++;
            int t1 = arr[await GetVal(1)];
            if (t1 == 42)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillArrayCompoundAssignmentLValue()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Driver
{
    static int[] arr;

    static async Task Run()
    {
        arr = new int[1];
        arr[0] += await Task.Factory.StartNew(() => 42);
    }

    static void Main()
    {
        Run().Wait();
        Console.WriteLine(arr[0]);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillArrayCompoundAssignmentLValueAwait()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Driver
{
    static int[] arr;

    static async Task Run()
    {
        arr = new int[1];
        arr[await Task.Factory.StartNew(() => 0)] += await Task.Factory.StartNew(() => 42);
    }

    static void Main()
    {
        Run().Wait();
        Console.WriteLine(arr[0]);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillArrayCompoundAssignmentLValueAwait2()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

struct S1
{
    public int x;
}

struct S2
{
    public S1 s1;
}

class Driver
{
    static async Task<int> Run()
    {
        var arr = new S2[1];
        arr[await Task.Factory.StartNew(() => 0)].s1.x += await Task.Factory.StartNew(() => 42);
        return arr[await Task.Factory.StartNew(() => 0)].s1.x;
    }

    static void Main()
    {
        var t = Run();
        t.Wait();
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void DoubleSpillArrayCompoundAssignment()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

struct S1
{
    public int x;
}

struct S2
{
    public S1 s1;
}

class Driver
{
    static async Task<int> Run()
    {
        var arr = new S2[1];
        arr[await Task.Factory.StartNew(() => 0)].s1.x += (arr[await Task.Factory.StartNew(() => 0)].s1.x += await Task.Factory.StartNew(() => 42));
        return arr[await Task.Factory.StartNew(() => 0)].s1.x;
    }

    static void Main()
    {
        var t = Run();
        t.Wait();
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillArrayInitializers1()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run()
    {
        int tests = 0;
        try
        {
            //jagged array
            tests++;
            int[][] arr1 = new[]
                {
                    new []{await GetVal(2),await GetVal(3)},
                    new []{4,await GetVal(5),await GetVal(6)}
                };
            if (arr1[0][1] == 3 && arr1[1][1] == 5 && arr1[1][2] == 6)
                Driver.Count++;

            tests++;
            int[][] arr2 = new[]
                {
                    new []{await GetVal(2),await GetVal(3)},
                    await Goo()
                };
            if (arr2[0][1] == 3 && arr2[1][1] == 2)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }

    public async Task<int[]> Goo()
    {
        await Task.Delay(1);
        return new int[] { 1, 2, 3 };
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillArrayInitializers2()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run()
    {
        int tests = 0;
        try
        {
            //jagged array
            tests++;
            int[,] arr1 = 
                {
                    {await GetVal(2),await GetVal(3)},
                    {await GetVal(5),await GetVal(6)}
                };
            if (arr1[0, 1] == 3 && arr1[1, 0] == 5 && arr1[1, 1] == 6)
                Driver.Count++;

            tests++;
            int[,] arr2 = 
                {
                    {await GetVal(2),3},
                    {4,await GetVal(5)}
                };
            if (arr2[0, 1] == 3 && arr2[1, 1] == 5)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillArrayInitializers3()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run()
    {
        int tests = 0;
        try
        {
            //jagged array
            tests++;
            int[][] arr1 = new[]
                {
                    new []{await GetVal(2),await Task.Run<int>(async()=>{await Task.Delay(1);return 3;})},
                    new []{await GetVal(5),4,await Task.Run<int>(async()=>{await Task.Delay(1);return 6;})}
                };
            if (arr1[0][1] == 3 && arr1[1][1] == 4 && arr1[1][2] == 6)
                Driver.Count++;

            tests++;
            dynamic arr2 = new[]
                {
                    new []{await GetVal(2),3},
                    await Goo()
                };
            if (arr2[0][1] == 3 && arr2[1][1] == 2)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }

    public async Task<int[]> Goo()
    {
        await Task.Delay(1);
        return new int[] { 1, 2, 3 };
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expected, references: new[] { CSharpRef });
        }

        [Fact]
        public void SpillNestedExpressionInArrayInitializer()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int[,]> Run()
    {
        return new int[,] {
            {1, 2, 21 + (await Task.Factory.StartNew(() => 21)) },
        };
    }

    public static void Main()
    {
        var t = Run();
        t.Wait();
        foreach (var xs in t.Result)
        {
            Console.WriteLine(xs);
        }
    }
}";
            var expected = @"
1
2
42
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void SpillConditionalAccess()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{

    class C1
    {
        public int M(int x)
        {
            return x;
        }
    }

    public static int Get(int x)
    {
        Console.WriteLine(""> "" + x);
        return x;
    }

    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => x);
    }

    public static async Task<int?> G()
    {
        var c = new C1();
        return c?.M(await F(Get(42)));
    }

    public static void Main()
    {
        var t = G();
        System.Console.WriteLine(t.Result);
    }
}";
            var expected = @"
> 42
42";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void AssignToAwait()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class S
{
    public int x = -1;
}

class Test
{
    static S _s = new S();

    public static async Task<S> GetS()
    {
        return await Task.Factory.StartNew(() => _s);
    }

    public static async Task Run()
    {
        (await GetS()).x = 42;
        Console.WriteLine(_s.x);
    }
}

class Driver
{
    static void Main()
    {
        Test.Run().Wait();
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void AssignAwaitToAwait()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class S
{
    public int x = -1;
}

class Test
{
    static S _s = new S();

    public static async Task<S> GetS()
    {
        return await Task.Factory.StartNew(() => _s);
    }

    public static async Task Run()
    {
        (await GetS()).x = await Task.Factory.StartNew(() => 42);
        Console.WriteLine(_s.x);
    }
}

class Driver
{
    static void Main()
    {
        Test.Run().Wait();
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expected);
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void SpillArglist()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    static StringBuilder sb = new StringBuilder();
    public async Task Run()
    {
        try
        {
            Bar(__arglist(One(), await Two()));
            if (sb.ToString() == ""OneTwo"")
                Driver.Result = 0;
        }
        finally
        {
            Driver.CompleteSignal.Set();
        }
    }
    int One()
    {
        sb.Append(""One"");
        return 1;
    }
    async Task<int> Two()
    {
        await Task.Delay(1);
        sb.Append(""Two"");
        return 2;
    }
    void Bar(__arglist)
    {
        var ai = new ArgIterator(__arglist);
        while (ai.GetRemainingCount() > 0)
            Console.WriteLine( __refvalue(ai.GetNextArg(), int));
    }
}
class Driver
{
    static public AutoResetEvent CompleteSignal = new AutoResetEvent(false);
    public static int Result = -1;
    public static void Main()
    {
        TestCase tc = new TestCase();
        tc.Run();
        CompleteSignal.WaitOne();

        Console.WriteLine(Result);
    }
}";
            var expected = @"
1
2
0
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void SpillObjectInitializer1()
        {
            var source = @"
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;


struct TestCase : IEnumerable
{
    int X;
    public async Task Run()
    {
        int test = 0;
        int count = 0;
        try
        {
            test++;
            var x = new TestCase { X = await Bar() };
            if (x.X == 1)
                count++;
        }
        finally
        {
            Driver.Result = test - count;
            Driver.CompleteSignal.Set();
        }
    }
    async Task<int> Bar()
    {
        await Task.Delay(1);
        return 1;
    }

    public IEnumerator GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}
class Driver
{
    static public AutoResetEvent CompleteSignal = new AutoResetEvent(false);
    public static int Result = -1;
    public static void Main()
    {
        TestCase tc = new TestCase();
        tc.Run();
        CompleteSignal.WaitOne();

        Console.WriteLine(Result);
    }
}";
            var expected = @"
0
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void SpillWithByRefArguments01()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class BaseTestCase
{
    public void GooRef(ref decimal d, int x, out decimal od)
    {
        od = d;
        d++;
    }

    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }
}

class TestCase : BaseTestCase
{
    public async void Run()
    {
        int tests = 0;
        try
        {
            decimal d = 1;
            decimal od;

            tests++;
            base.GooRef(ref d, await base.GetVal(4), out od);
            if (d == 2 && od == 1) Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void SpillOperator_Compound1()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run()
    {
        int tests = 0;
        try
        {
            tests++;
            int[] x = new int[] { 1, 2, 3, 4 };
            x[await GetVal(0)] += await GetVal(4);
            if (x[0] == 5)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void SpillOperator_Compound2()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run()
    {
        int tests = 0;
        try
        {
            tests++;
            int[] x = new int[] { 1, 2, 3, 4 };
            x[await GetVal(0)] += await GetVal(4);
            if (x[0] == 5)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void Async_StackSpill_Argument_Generic04()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class mc<T>
{
    async public System.Threading.Tasks.Task<dynamic> Goo<V>(T t, V u) { await Task.Delay(1); return u; }
}

class Test
{
    static async Task<int> Goo()
    {
        dynamic mc = new mc<string>();
        var rez = await mc.Goo<string>(null, await ((Func<Task<string>>)(async () => { await Task.Delay(1); return ""Test""; }))());
        if (rez == ""Test"")
            return 0;
        return 1;
    }

    static void Main()
    {
        Console.WriteLine(Goo().Result);
    }
}";
            CompileAndVerify(source, "0", references: new[] { CSharpRef });
        }

        [Fact]
        public void AsyncStackSpill_assign01()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

struct TestCase
{
    private int val;
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run()
    {
        int tests = 0;

        try
        {
            tests++;
            int[] x = new int[] { 1, 2, 3, 4 };
            val = x[await GetVal(0)] += await GetVal(4);
            if (x[0] == 5 && val == await GetVal(5))
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void SpillCollectionInitializer()
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

struct PrivateCollection : IEnumerable
{
    public List<int> lst; //public so we can check the values
    public void Add(int x)
    {
        if (lst == null)
            lst = new List<int>();
        lst.Add(x);
    }

    public IEnumerator GetEnumerator()
    {
        return lst as IEnumerator;
    }
}

class TestCase
{
    public async Task<T> GetValue<T>(T x)
    {
        await Task.Delay(1);
        return x;
    }

    public async void Run()
    {
        int tests = 0;

        try
        {
            tests++;
            var myCol = new PrivateCollection() { 
                await GetValue(1),
                await GetValue(2)
            };
            if (myCol.lst[0] == 1 && myCol.lst[1] == 2)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test completes, set the flag.
            Driver.CompletedSignal.Set();
        }
    }

    public int Goo { get; set; }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void SpillRefExpr()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class MyClass
{
    public int Field;
}

class TestCase
{
    public static int Goo(ref int x, int y)
    {
        return x + y;
    }

    public async Task<int> Run()
    {
        return Goo(
            ref (new MyClass() { Field = 21 }.Field),
            await Task.Factory.StartNew(() => 21));
    }
}

static class Driver
{
    static void Main()
    {
        var t = new TestCase().Run();
        t.Wait();
        Console.WriteLine(t.Result);
    }
}";
            CompileAndVerify(source, "42");
        }

        [Fact]
        public void SpillManagedPointerAssign03()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    class PrivClass
    {
        internal struct ValueT
        {
            public int Field;
        }

        internal ValueT[] arr = new ValueT[3];
    }

    private PrivClass myClass;

    public async void Run()
    {
        int tests = 0;
        this.myClass = new PrivClass();

        try
        {
            tests++;
            this.myClass.arr[0].Field = await GetVal(4);
            if (myClass.arr[0].Field == 4)
                Driver.Count++;

            tests++;
            this.myClass.arr[0].Field += await GetVal(4);
            if (myClass.arr[0].Field == 8)
                Driver.Count++;

            tests++;
            this.myClass.arr[await GetVal(1)].Field += await GetVal(4);
            if (myClass.arr[1].Field == 4)
                Driver.Count++;

            tests++;
            this.myClass.arr[await GetVal(1)].Field++;
            if (myClass.arr[1].Field == 5)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact, WorkItem(36443, "https://github.com/dotnet/roslyn/issues/36443")]
        public void SpillCompoundAssignmentToNullableMemberOfLocal_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;
struct S
{
    int? i;

    static async Task Main()
    {
        S s = default;
        Console.WriteLine(s.i += await GetInt());
    }

    static Task<int?> GetInt() => Task.FromResult((int?)1);
}";
            CompileAndVerify(source, expectedOutput: "", options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: "", options: TestOptions.DebugExe);
        }

        [Fact, WorkItem(36443, "https://github.com/dotnet/roslyn/issues/36443")]
        public void SpillCompoundAssignmentToNullableMemberOfLocal_02()
        {
            var source = @"
class C
{
    static async System.Threading.Tasks.Task Main()
    {
        await new C().M();
    }

    int field = 1;
    async System.Threading.Tasks.Task M()
    {
         this.field += await M2();
         System.Console.Write(this.field);
    }

    async System.Threading.Tasks.Task<int> M2()
    {
         await System.Threading.Tasks.Task.Yield();
         return 42;
    }
}
";
            CompileAndVerify(source, expectedOutput: "43", options: TestOptions.DebugExe);
            CompileAndVerify(source, expectedOutput: "43", options: TestOptions.ReleaseExe);
        }

        [Fact, WorkItem(36443, "https://github.com/dotnet/roslyn/issues/36443")]
        public void SpillCompoundAssignmentToNullableMemberOfLocal_03()
        {
            var source = @"
class C
{
    static async System.Threading.Tasks.Task Main()
    {
        await new C().M();
    }

    int? field = 1;
    async System.Threading.Tasks.Task M()
    {
         this.field += await M2();
         System.Console.Write(this.field);
    }

    async System.Threading.Tasks.Task<int?> M2()
    {
         await System.Threading.Tasks.Task.Yield();
         return 42;
    }
}
";
            CompileAndVerify(source, expectedOutput: "43", options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: "43", options: TestOptions.DebugExe);
        }

        [Fact, WorkItem(36443, "https://github.com/dotnet/roslyn/issues/36443")]
        public void SpillCompoundAssignmentToNullableMemberOfLocal_04()
        {
            var source = @"
using System;
using System.Threading.Tasks;
struct S
{
    int? i;

    static async Task M(S s = default)
    {
        s = default;
        Console.WriteLine(s.i += await GetInt());
    }

    static async Task Main()
    {
        M();
    }

    static Task<int?> GetInt() => Task.FromResult((int?)1);
}";
            CompileAndVerify(source, expectedOutput: "", options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: "", options: TestOptions.DebugExe);
        }

        [Fact]
        public void SpillSacrificialRead()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void F1(ref int x, int y, int z)
    {
        x += y + z;
    }

    static int F0()
    {
        Console.WriteLine(-1);
        return 0;
    }

    static async Task<int> F2()
    {
        int[] x = new int[1] { 21 };
        x = null;
        F1(ref x[0], F0(), await Task.Factory.StartNew(() => 21));
        return x[0];
    }

    public static void Main()
    {
        var t = F2();
        try
        {
            t.Wait();   
        }
        catch(Exception e)
        {
            Console.WriteLine(0);
            return;
        }

        Console.WriteLine(-1);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void SpillRefThisStruct()
        {
            var source = @"
using System;
using System.Threading.Tasks;

struct s1
{
    public int X;

    public async void Goo1()
    {
        Bar(ref this, await Task<int>.FromResult(42));
    }

    public void Goo2()
    {
        Bar(ref this, 42);
    }

    public void Bar(ref s1 x, int y)
    {
        x.X = 42;
    }
}

class c1
{
    public int X;

    public async void Goo1()
    {
        Bar(this, await Task<int>.FromResult(42));
    }

    public void Goo2()
    {
        Bar(this, 42);
    }

    public void Bar(c1 x, int y)
    {
        x.X = 42;
    }
}

class C
{
    public static void Main()
    {
        {
            s1 s;
            s.X = -1;
            s.Goo1();
            Console.WriteLine(s.X);
        }

        {
            s1 s;
            s.X = -1;
            s.Goo2();
            Console.WriteLine(s.X);
        }

        {
            c1 c = new c1();
            c.X = -1;
            c.Goo1();
            Console.WriteLine(c.X);
        }

        {
            c1 c = new c1();
            c.X = -1;
            c.Goo2();
            Console.WriteLine(c.X);
        }
    }
}";
            var expected = @"
-1
42
42
42
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        [WorkItem(13734, "https://github.com/dotnet/roslyn/issues/13734")]
        public void MethodGroupConversionNoSpill()
        {
            string source = @"
using System.Threading.Tasks;
using System;

public class AsyncBug {
    public static void Main() 
    {
        Boom().GetAwaiter().GetResult();
    }
    public static async Task Boom()
    {
        Func<Type> f = (await Task.FromResult(1)).GetType;
        Console.WriteLine(f());
    }
}
";

            var v = CompileAndVerify(source, "System.Int32");
        }

        [Fact]
        [WorkItem(13734, "https://github.com/dotnet/roslyn/issues/13734")]
        public void MethodGroupConversionWithSpill()
        {
            string source = @"
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
namespace AsyncBug
{
    class Program
    {
        private class SomeClass
        {
            public bool Method(int value)
            {
                return value % 2 == 0;
            }
        }

        private async Task<SomeClass> Danger()
        {
            await Task.Yield();
            return new SomeClass();
        }

        private async Task<IEnumerable<bool>> Killer()
        {
            return (new int[] {1, 2, 3, 4, 5}).Select((await Danger()).Method);
        }

        static void Main(string[] args)
        {
            foreach (var b in new Program().Killer().GetAwaiter().GetResult()) {
                Console.WriteLine(b);
            }
        }
    }
}
";
            var expected = new bool[] { false, true, false, true, false }.Aggregate("", (str, next) => str += $"{next}{Environment.NewLine}");
            var v = CompileAndVerify(source, expected);
        }

        [Fact]
        [WorkItem(17706, "https://github.com/dotnet/roslyn/issues/17706")]
        public void SpillAwaitBeforeRefReordered()
        {
            string source = @"
using System.Threading.Tasks;

public class C
{
    private static int i;

    static ref int P => ref i;

    static void Assign(ref int first, int second)
    {
        first = second;
    }

    public static async Task M(Task<int> t)
    {
        // OK: await goes before the ref
        Assign(second: await t, first: ref P);
    }

    public static void Main()
    {
        M(Task.FromResult(42)).Wait();

        System.Console.WriteLine(i);
    }
}
";

            var v = CompileAndVerify(source, "42");
        }

        [Fact]
        [WorkItem(17706, "https://github.com/dotnet/roslyn/issues/17706")]
        public void SpillRefBeforeAwaitReordered()
        {
            string source = @"
using System.Threading.Tasks;

public class C
{
    private static int i;

    static ref int P => ref i;

    static void Assign(int first, ref int second)
    {
        second = first;
    }

    public static async Task M(Task<int> t)
    {
        // ERROR: await goes after the ref
        Assign(second: ref P, first: await t);
    }

    public static void Main()
    {
        M(Task.FromResult(42)).Wait();

        System.Console.WriteLine(i);
    }
}
";

            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (18,28): error CS8178: 'await' cannot be used in an expression containing a call to 'C.P.get' because it returns by reference
                //         Assign(second: ref P, first: await t);
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "P").WithArguments("C.P.get").WithLocation(18, 28)
                );
        }

        [Fact]
        [WorkItem(27831, "https://github.com/dotnet/roslyn/issues/27831")]
        public void AwaitWithInParameter_ArgModifier()
        {
            CreateCompilation(@"
using System.Threading.Tasks;
class Foo
{
    async Task A(string s, Task<int> task)
    {
        C(in s, await task);
    }

    void C(in object obj, int length) {}
}").VerifyDiagnostics(
                // (7,14): error CS1503: Argument 1: cannot convert from 'in string' to 'in object'
                //         C(in s, await task);
                Diagnostic(ErrorCode.ERR_BadArgType, "s").WithArguments("1", "in string", "in object").WithLocation(7, 14));
        }

        [Fact]
        [WorkItem(27831, "https://github.com/dotnet/roslyn/issues/27831")]
        public void AwaitWithInParameter_NoArgModifier()
        {
            CompileAndVerify(@"
using System;
using System.Threading.Tasks;
class Foo
{
    static async Task Main()
    {
        await A(""test"", Task.FromResult(4));
    }
    
    static async Task A(string s, Task<int> task)
    {
        B(s, await task);
    }

    static void B(in object obj, int v)
    {
        Console.WriteLine(obj);
        Console.WriteLine(v);
    }
}", expectedOutput: @"
test
4
");
        }

        [Fact, WorkItem(36856, "https://github.com/dotnet/roslyn/issues/36856")]
        public void Crash36856()
        {
            var source = @"
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
    }

    private static async Task Serialize()
    {
        System.Text.Json.Serialization.JsonSerializer.Parse<string>(await TestAsync());
    }

    private static Task<byte[]> TestAsync()
    {
        return null;
    }
}

namespace System
{
    public readonly ref struct ReadOnlySpan<T>
    {
        public static implicit operator ReadOnlySpan<T>(T[] array)
        {
            throw null;
        }
    }
}
namespace System.Text.Json.Serialization
{
    public static class JsonSerializer
    {
        public static TValue Parse<TValue>(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions options = null)
        {
            throw null;
        }
    }
    public sealed class JsonSerializerOptions
    {
    }
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugExe);

            v.VerifyIL("Program.<Serialize>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
    {
      // Code size      184 (0xb8)
      .maxstack  3
      .locals init (int V_0,
                    System.Runtime.CompilerServices.TaskAwaiter<byte[]> V_1,
                    Program.<Serialize>d__1 V_2,
                    System.Exception V_3)
      IL_0000:  ldarg.0
      IL_0001:  ldfld      ""int Program.<Serialize>d__1.<>1__state""
      IL_0006:  stloc.0
      .try
      {
        IL_0007:  ldloc.0
        IL_0008:  brfalse.s  IL_000c
        IL_000a:  br.s       IL_000e
        IL_000c:  br.s       IL_0047
        IL_000e:  nop
        IL_000f:  call       ""System.Threading.Tasks.Task<byte[]> Program.TestAsync()""
        IL_0014:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<byte[]> System.Threading.Tasks.Task<byte[]>.GetAwaiter()""
        IL_0019:  stloc.1
        IL_001a:  ldloca.s   V_1
        IL_001c:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<byte[]>.IsCompleted.get""
        IL_0021:  brtrue.s   IL_0063
        IL_0023:  ldarg.0
        IL_0024:  ldc.i4.0
        IL_0025:  dup
        IL_0026:  stloc.0
        IL_0027:  stfld      ""int Program.<Serialize>d__1.<>1__state""
        IL_002c:  ldarg.0
        IL_002d:  ldloc.1
        IL_002e:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<byte[]> Program.<Serialize>d__1.<>u__1""
        IL_0033:  ldarg.0
        IL_0034:  stloc.2
        IL_0035:  ldarg.0
        IL_0036:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Serialize>d__1.<>t__builder""
        IL_003b:  ldloca.s   V_1
        IL_003d:  ldloca.s   V_2
        IL_003f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<byte[]>, Program.<Serialize>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<byte[]>, ref Program.<Serialize>d__1)""
        IL_0044:  nop
        IL_0045:  leave.s    IL_00b7
        IL_0047:  ldarg.0
        IL_0048:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<byte[]> Program.<Serialize>d__1.<>u__1""
        IL_004d:  stloc.1
        IL_004e:  ldarg.0
        IL_004f:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<byte[]> Program.<Serialize>d__1.<>u__1""
        IL_0054:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<byte[]>""
        IL_005a:  ldarg.0
        IL_005b:  ldc.i4.m1
        IL_005c:  dup
        IL_005d:  stloc.0
        IL_005e:  stfld      ""int Program.<Serialize>d__1.<>1__state""
        IL_0063:  ldarg.0
        IL_0064:  ldloca.s   V_1
        IL_0066:  call       ""byte[] System.Runtime.CompilerServices.TaskAwaiter<byte[]>.GetResult()""
        IL_006b:  stfld      ""byte[] Program.<Serialize>d__1.<>s__1""
        IL_0070:  ldarg.0
        IL_0071:  ldfld      ""byte[] Program.<Serialize>d__1.<>s__1""
        IL_0076:  call       ""System.ReadOnlySpan<byte> System.ReadOnlySpan<byte>.op_Implicit(byte[])""
        IL_007b:  ldnull
        IL_007c:  call       ""string System.Text.Json.Serialization.JsonSerializer.Parse<string>(System.ReadOnlySpan<byte>, System.Text.Json.Serialization.JsonSerializerOptions)""
        IL_0081:  pop
        IL_0082:  ldarg.0
        IL_0083:  ldnull
        IL_0084:  stfld      ""byte[] Program.<Serialize>d__1.<>s__1""
        IL_0089:  leave.s    IL_00a3
      }
      catch System.Exception
      {
        IL_008b:  stloc.3
        IL_008c:  ldarg.0
        IL_008d:  ldc.i4.s   -2
        IL_008f:  stfld      ""int Program.<Serialize>d__1.<>1__state""
        IL_0094:  ldarg.0
        IL_0095:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Serialize>d__1.<>t__builder""
        IL_009a:  ldloc.3
        IL_009b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
        IL_00a0:  nop
        IL_00a1:  leave.s    IL_00b7
      }
      IL_00a3:  ldarg.0
      IL_00a4:  ldc.i4.s   -2
      IL_00a6:  stfld      ""int Program.<Serialize>d__1.<>1__state""
      IL_00ab:  ldarg.0
      IL_00ac:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Serialize>d__1.<>t__builder""
      IL_00b1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
      IL_00b6:  nop
      IL_00b7:  ret
    }
", sequencePoints: "Program.Serialize");
        }

        [Fact, WorkItem(37461, "https://github.com/dotnet/roslyn/issues/37461")]
        public void ShouldNotSpillStackallocToField_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

public class P
{
    static async Task Main()
    {
        await Async1(F1(), G(F2(), stackalloc int[] { 40, 500, 6000 }));
    }

    static int F1() => 70000;
    static int F2() => 800000;
    static int G(int k, Span<int> span) => k + span.Length + span[0] + span[1] + span[2];
    static Task Async1(int k, int i)
    {
        Console.WriteLine(k + i);
        return Task.Delay(1);
    }
}
";
            var expectedOutput = @"876543";

            var comp = CreateCompilationWithMscorlibAndSpan(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var v = CompileAndVerify(
                compilation: comp,
                expectedOutput: expectedOutput,
                verify: Verification.Fails // localloc is not verifiable.
                );
            comp = CreateCompilationWithMscorlibAndSpan(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            v = CompileAndVerify(
                compilation: comp,
                expectedOutput: expectedOutput,
                verify: Verification.Fails // localloc is not verifiable.
                );
        }

        [Fact, WorkItem(37461, "https://github.com/dotnet/roslyn/issues/37461")]
        public void ShouldNotSpillStackallocToField_02()
        {
            var source = @"
using System;
using System.Threading.Tasks;

public class P
{
    static async Task Main()
    {
        await Async1(F1(), G(F2(), stackalloc int[] { 40, await Task.FromResult(500), 6000 }));
    }

    static int F1() => 70000;
    static int F2() => 800000;
    static int G(int k, Span<int> span) => k + span.Length + span[0] + span[1] + span[2];
    static Task Async1(int k, int i)
    {
        Console.WriteLine(k + i);
        return Task.Delay(1);
    }
}
";
            var expectedOutput = @"876543";

            var comp = CreateCompilationWithMscorlibAndSpan(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var v = CompileAndVerify(
                compilation: comp,
                expectedOutput: expectedOutput,
                verify: Verification.Fails // localloc is not verifiable.
                );
            comp = CreateCompilationWithMscorlibAndSpan(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            v = CompileAndVerify(
                compilation: comp,
                expectedOutput: expectedOutput,
                verify: Verification.Fails // localloc is not verifiable.
                );
        }

        [Fact, WorkItem(37461, "https://github.com/dotnet/roslyn/issues/37461")]
        public void ShouldNotSpillStackallocToField_03()
        {
            var source = @"
using System;
using System.Threading.Tasks;

public class P
{
    static async Task Main()
    {
        await Async1(F1(), G(F2(), stackalloc int[] { 1, 2, 3 }, await F3()));
    }

    static object F1() => 1;
    static object F2() => 1;
    static Task<object> F3() => Task.FromResult<object>(1);
    static int G(object obj, Span<int> span, object o2) => span.Length;
    static async Task Async1(Object obj, int i) { await Task.Delay(1); }
}
";
            foreach (var options in new[] { TestOptions.DebugExe, TestOptions.ReleaseExe })
            {
                var comp = CreateCompilationWithMscorlibAndSpan(source, options: options);
                comp.VerifyDiagnostics();
                comp.VerifyEmitDiagnostics(
                    // (9,66): error CS4007: 'await' cannot be used in an expression containing the type 'System.Span<int>'
                    //         await Async1(F1(), G(F2(), stackalloc int[] { 1, 2, 3 }, await F3()));
                    Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "await F3()").WithArguments("System.Span<int>").WithLocation(9, 66)
                    );
            }
        }

        [Fact]
        public void SpillStateMachineTemps()
        {
            var source = @"using System;
using System.Threading.Tasks;

public class C {
    public static void Main()
    {
        Console.WriteLine(M1(new Q(), SF()).Result);
    }
    public static async Task<int> M1(object o, Task<bool> c)
    {
        return o switch
        {
            Q { F: { P1: true } } when await c => 1, // cached Q.F is alive
            Q { F: { P2: true } } => 2,
            _ => 3,
        };
    }
    public static async Task<bool> SF()
    {
        await Task.Delay(10);
        return false;
    }
}

class Q
{
    public F F => new F(true);
}

struct F
{
    bool _result;
    public F(bool result)
    {
        _result = result;
    }
    public bool P1 => _result;
    public bool P2 => _result;
}
";
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: "2");
            CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: "2");
        }

        [Fact]
        [WorkItem(37713, "https://github.com/dotnet/roslyn/issues/37713")]
        public void RefStructInAsyncStateMachineWithWhenClause()
        {
            var source = @"
using System.Threading.Tasks;
class Program
{
    async Task<int> M1(object o, Task<bool> c, int r)
    {
        return o switch
        {
            Q { F: { P1: true } } when await c => r, // error: cached Q.F is alive
            Q { F: { P2: true } } => 2,
            _ => 3,
        };
    }
    async Task<int> M2(object o, Task<bool> c, int r)
    {
        return o switch
        {
            Q { F: { P1: true } } when await c => r, // ok: only Q.P1 is live
            Q { F: { P1: true } } => 2,
            _ => 3,
        };
    }
    async Task<int> M3(object o, bool c, Task<int> r)
    {
        return o switch
        {
            Q { F: { P1: true } } when c => await r, // ok: nothing alive at await
            Q { F: { P2: true } } => 2,
            _ => 3,
        };
    }
    async Task<int> M4(object o, Task<bool> c, int r)
    {
        return o switch
        {
            Q { F: { P1: true } } when await c => r, // ok: no switch state is alive
            _ => 3,
        };
    }
}
public class Q
{
    public S F => throw null!;
}
public ref struct S
{
    public bool P1 => true;
    public bool P2 => true;
}
";
            CreateCompilation(source, options: TestOptions.DebugDll).VerifyDiagnostics().VerifyEmitDiagnostics(
                // (9,20): error CS4013: Instance of type 'S' cannot be used inside a nested function, query expression, iterator block or async method
                //             Q { F: { P1: true } } when await c => r, // error: cached Q.F is alive
                Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "{ P1: true }").WithArguments("S").WithLocation(9, 20)
                );
            CreateCompilation(source, options: TestOptions.ReleaseDll).VerifyDiagnostics().VerifyEmitDiagnostics(
                // (9,20): error CS4013: Instance of type 'S' cannot be used inside a nested function, query expression, iterator block or async method
                //             Q { F: { P1: true } } when await c => r, // error: cached Q.F is alive
                Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "{ P1: true }").WithArguments("S").WithLocation(9, 20)
                );
        }

        [Fact]
        [WorkItem(37783, "https://github.com/dotnet/roslyn/issues/37783")]
        public void ExpressionLambdaWithObjectInitializer()
        {
            var source =
@"using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

class Program
{
    public static async Task Main()
    {
        int value = 42;
        Console.WriteLine(await M(() => new Box<int>() { Value = value }));
    }

    static Task<int> M(Expression<Func<Box<int>>> e)
    {
        return Task.FromResult(e.Compile()().Value);
    }
}

class Box<T>
{
    public T Value;
}
";
            CompileAndVerify(source, expectedOutput: "42", options: TestOptions.DebugExe);
            CompileAndVerify(source, expectedOutput: "42", options: TestOptions.ReleaseExe);
        }

        [Fact]
        [WorkItem(38309, "https://github.com/dotnet/roslyn/issues/38309")]
        public void ExpressionLambdaWithUserDefinedControlFlow()
        {
            var source =
@"using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RoslynFailFastReproduction
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            await MainAsync(args);
        }
        static async Task MainAsync(string[] args)
        {
            Expression<Func<AltBoolean, AltBoolean>> expr = x => x && x;

            var result = await Task.FromResult(true);
            Console.WriteLine(result);
        }

        class AltBoolean
        {
            public static AltBoolean operator &(AltBoolean x, AltBoolean y) => default;
            public static bool operator true(AltBoolean x) => default;
            public static bool operator false(AltBoolean x) => default;
        }
    }
}
";
            CompileAndVerify(source, expectedOutput: "True", options: TestOptions.DebugExe);
            CompileAndVerify(source, expectedOutput: "True", options: TestOptions.ReleaseExe);
        }
    }
}
