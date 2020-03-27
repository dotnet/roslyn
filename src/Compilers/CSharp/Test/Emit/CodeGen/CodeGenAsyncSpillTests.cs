// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
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

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void KeepLtrSemantics_ClassFieldAccessOnProperty()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var a = new A();
        try
        {
            a.Invalid.x = await Write(""Invalid"");
        }
        catch 
        {
            a.Valid.x = await Write(""Valid"");
        }                       
                                      
        async Task<int> Write(string s)
        {
            await Task.Yield();
            Console.WriteLine(s);
            return 5;
        }
    }
}

class A
{
    public B Valid => new B();
    public B Invalid => throw new Exception();
}

class B
{
    public int x;
}";
            var comp = CSharpTestBase.CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "Valid")
                .VerifyIL("Program.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      385 (0x181)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0023
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_010f
    IL_0011:  ldarg.0
    IL_0012:  newobj     ""A..ctor()""
    IL_0017:  stfld      ""A Program.<Main>d__0.<a>5__2""
    IL_001c:  ldarg.0
    IL_001d:  ldc.i4.0
    IL_001e:  stfld      ""int Program.<Main>d__0.<>7__wrap2""
    IL_0023:  nop
    .try
    {
      IL_0024:  ldloc.0
      IL_0025:  brfalse.s  IL_0074
      IL_0027:  ldarg.0
      IL_0028:  ldarg.0
      IL_0029:  ldfld      ""A Program.<Main>d__0.<a>5__2""
      IL_002e:  callvirt   ""B A.Invalid.get""
      IL_0033:  stfld      ""B Program.<Main>d__0.<>7__wrap3""
      IL_0038:  ldstr      ""Invalid""
      IL_003d:  call       ""System.Threading.Tasks.Task<int> Program.<Main>g__Write|0_0(string)""
      IL_0042:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
      IL_0047:  stloc.2
      IL_0048:  ldloca.s   V_2
      IL_004a:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
      IL_004f:  brtrue.s   IL_0090
      IL_0051:  ldarg.0
      IL_0052:  ldc.i4.0
      IL_0053:  dup
      IL_0054:  stloc.0
      IL_0055:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_005a:  ldarg.0
      IL_005b:  ldloc.2
      IL_005c:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_0061:  ldarg.0
      IL_0062:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
      IL_0067:  ldloca.s   V_2
      IL_0069:  ldarg.0
      IL_006a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
      IL_006f:  leave      IL_0180
      IL_0074:  ldarg.0
      IL_0075:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_007a:  stloc.2
      IL_007b:  ldarg.0
      IL_007c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_0081:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_0087:  ldarg.0
      IL_0088:  ldc.i4.m1
      IL_0089:  dup
      IL_008a:  stloc.0
      IL_008b:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_0090:  ldloca.s   V_2
      IL_0092:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
      IL_0097:  stloc.1
      IL_0098:  ldarg.0
      IL_0099:  ldfld      ""B Program.<Main>d__0.<>7__wrap3""
      IL_009e:  ldloc.1
      IL_009f:  stfld      ""int B.x""
      IL_00a4:  ldarg.0
      IL_00a5:  ldnull
      IL_00a6:  stfld      ""B Program.<Main>d__0.<>7__wrap3""
      IL_00ab:  leave.s    IL_00b7
    }
    catch object
    {
      IL_00ad:  pop
      IL_00ae:  ldarg.0
      IL_00af:  ldc.i4.1
      IL_00b0:  stfld      ""int Program.<Main>d__0.<>7__wrap2""
      IL_00b5:  leave.s    IL_00b7
    }
    IL_00b7:  ldarg.0
    IL_00b8:  ldfld      ""int Program.<Main>d__0.<>7__wrap2""
    IL_00bd:  stloc.1
    IL_00be:  ldloc.1
    IL_00bf:  ldc.i4.1
    IL_00c0:  bne.un     IL_0146
    IL_00c5:  ldarg.0
    IL_00c6:  ldarg.0
    IL_00c7:  ldfld      ""A Program.<Main>d__0.<a>5__2""
    IL_00cc:  callvirt   ""B A.Valid.get""
    IL_00d1:  stfld      ""B Program.<Main>d__0.<>7__wrap3""
    IL_00d6:  ldstr      ""Valid""
    IL_00db:  call       ""System.Threading.Tasks.Task<int> Program.<Main>g__Write|0_0(string)""
    IL_00e0:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_00e5:  stloc.2
    IL_00e6:  ldloca.s   V_2
    IL_00e8:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00ed:  brtrue.s   IL_012b
    IL_00ef:  ldarg.0
    IL_00f0:  ldc.i4.1
    IL_00f1:  dup
    IL_00f2:  stloc.0
    IL_00f3:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_00f8:  ldarg.0
    IL_00f9:  ldloc.2
    IL_00fa:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_00ff:  ldarg.0
    IL_0100:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_0105:  ldloca.s   V_2
    IL_0107:  ldarg.0
    IL_0108:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
    IL_010d:  leave.s    IL_0180
    IL_010f:  ldarg.0
    IL_0110:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_0115:  stloc.2
    IL_0116:  ldarg.0
    IL_0117:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_011c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0122:  ldarg.0
    IL_0123:  ldc.i4.m1
    IL_0124:  dup
    IL_0125:  stloc.0
    IL_0126:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_012b:  ldloca.s   V_2
    IL_012d:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0132:  stloc.1
    IL_0133:  ldarg.0
    IL_0134:  ldfld      ""B Program.<Main>d__0.<>7__wrap3""
    IL_0139:  ldloc.1
    IL_013a:  stfld      ""int B.x""
    IL_013f:  ldarg.0
    IL_0140:  ldnull
    IL_0141:  stfld      ""B Program.<Main>d__0.<>7__wrap3""
    IL_0146:  leave.s    IL_0166
  }
  catch System.Exception
  {
    IL_0148:  stloc.3
    IL_0149:  ldarg.0
    IL_014a:  ldc.i4.s   -2
    IL_014c:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_0151:  ldarg.0
    IL_0152:  ldnull
    IL_0153:  stfld      ""A Program.<Main>d__0.<a>5__2""
    IL_0158:  ldarg.0
    IL_0159:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_015e:  ldloc.3
    IL_015f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0164:  leave.s    IL_0180
  }
  IL_0166:  ldarg.0
  IL_0167:  ldc.i4.s   -2
  IL_0169:  stfld      ""int Program.<Main>d__0.<>1__state""
  IL_016e:  ldarg.0
  IL_016f:  ldnull
  IL_0170:  stfld      ""A Program.<Main>d__0.<a>5__2""
  IL_0175:  ldarg.0
  IL_0176:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
  IL_017b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0180:  ret
}");
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void KeepLtrSemantics_ClassFieldAccessOnArray()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var a = new A[1];
        try
        {
            a[1].x = await Write(""1"");
        }
        catch 
        {
            try
            {
                a[0].x = await WriteAndSet(""0"");
            }
            catch
            {
                
                a[0].x = await Write(""0"");
            }
        }                       
                                      
        async Task<int> Write(string s)
        {
            await Task.Yield();
            Console.WriteLine(s);
            return 5;
        }

        async Task<int> WriteAndSet(string s)
        {
            a[0] = new A();
            await Task.Yield();
            Console.WriteLine(s);
            return 5;
        }
    }
}

class A
{
    public int x;
}";
            var comp = CSharpTestBase.CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"0
0")
                .VerifyIL("Program.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      592 (0x250)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  switch    (
        IL_003c,
        IL_00e7,
        IL_01de)
    IL_0019:  ldarg.0
    IL_001a:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
    IL_001f:  stfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_0024:  ldarg.0
    IL_0025:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_002a:  ldc.i4.1
    IL_002b:  newarr     ""A""
    IL_0030:  stfld      ""A[] Program.<>c__DisplayClass0_0.a""
    IL_0035:  ldarg.0
    IL_0036:  ldc.i4.0
    IL_0037:  stfld      ""int Program.<Main>d__0.<>7__wrap1""
    IL_003c:  nop
    .try
    {
      IL_003d:  ldloc.0
      IL_003e:  brfalse.s  IL_008f
      IL_0040:  ldarg.0
      IL_0041:  ldarg.0
      IL_0042:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
      IL_0047:  ldfld      ""A[] Program.<>c__DisplayClass0_0.a""
      IL_004c:  ldc.i4.1
      IL_004d:  ldelem.ref
      IL_004e:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
      IL_0053:  ldstr      ""1""
      IL_0058:  call       ""System.Threading.Tasks.Task<int> Program.<Main>g__Write|0_0(string)""
      IL_005d:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
      IL_0062:  stloc.2
      IL_0063:  ldloca.s   V_2
      IL_0065:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
      IL_006a:  brtrue.s   IL_00ab
      IL_006c:  ldarg.0
      IL_006d:  ldc.i4.0
      IL_006e:  dup
      IL_006f:  stloc.0
      IL_0070:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_0075:  ldarg.0
      IL_0076:  ldloc.2
      IL_0077:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_007c:  ldarg.0
      IL_007d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
      IL_0082:  ldloca.s   V_2
      IL_0084:  ldarg.0
      IL_0085:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
      IL_008a:  leave      IL_024f
      IL_008f:  ldarg.0
      IL_0090:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_0095:  stloc.2
      IL_0096:  ldarg.0
      IL_0097:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_009c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_00a2:  ldarg.0
      IL_00a3:  ldc.i4.m1
      IL_00a4:  dup
      IL_00a5:  stloc.0
      IL_00a6:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_00ab:  ldloca.s   V_2
      IL_00ad:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
      IL_00b2:  stloc.1
      IL_00b3:  ldarg.0
      IL_00b4:  ldfld      ""A Program.<Main>d__0.<>7__wrap2""
      IL_00b9:  ldloc.1
      IL_00ba:  stfld      ""int A.x""
      IL_00bf:  ldarg.0
      IL_00c0:  ldnull
      IL_00c1:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
      IL_00c6:  leave.s    IL_00d2
    }
    catch object
    {
      IL_00c8:  pop
      IL_00c9:  ldarg.0
      IL_00ca:  ldc.i4.1
      IL_00cb:  stfld      ""int Program.<Main>d__0.<>7__wrap1""
      IL_00d0:  leave.s    IL_00d2
    }
    IL_00d2:  ldarg.0
    IL_00d3:  ldfld      ""int Program.<Main>d__0.<>7__wrap1""
    IL_00d8:  stloc.1
    IL_00d9:  ldloc.1
    IL_00da:  ldc.i4.1
    IL_00db:  bne.un     IL_0215
    IL_00e0:  ldarg.0
    IL_00e1:  ldc.i4.0
    IL_00e2:  stfld      ""int Program.<Main>d__0.<>7__wrap3""
    IL_00e7:  nop
    .try
    {
      IL_00e8:  ldloc.0
      IL_00e9:  ldc.i4.1
      IL_00ea:  beq.s      IL_0141
      IL_00ec:  ldarg.0
      IL_00ed:  ldarg.0
      IL_00ee:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
      IL_00f3:  ldfld      ""A[] Program.<>c__DisplayClass0_0.a""
      IL_00f8:  ldc.i4.0
      IL_00f9:  ldelem.ref
      IL_00fa:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
      IL_00ff:  ldarg.0
      IL_0100:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
      IL_0105:  ldstr      ""0""
      IL_010a:  callvirt   ""System.Threading.Tasks.Task<int> Program.<>c__DisplayClass0_0.<Main>g__WriteAndSet|1(string)""
      IL_010f:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
      IL_0114:  stloc.2
      IL_0115:  ldloca.s   V_2
      IL_0117:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
      IL_011c:  brtrue.s   IL_015d
      IL_011e:  ldarg.0
      IL_011f:  ldc.i4.1
      IL_0120:  dup
      IL_0121:  stloc.0
      IL_0122:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_0127:  ldarg.0
      IL_0128:  ldloc.2
      IL_0129:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_012e:  ldarg.0
      IL_012f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
      IL_0134:  ldloca.s   V_2
      IL_0136:  ldarg.0
      IL_0137:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
      IL_013c:  leave      IL_024f
      IL_0141:  ldarg.0
      IL_0142:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_0147:  stloc.2
      IL_0148:  ldarg.0
      IL_0149:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_014e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_0154:  ldarg.0
      IL_0155:  ldc.i4.m1
      IL_0156:  dup
      IL_0157:  stloc.0
      IL_0158:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_015d:  ldloca.s   V_2
      IL_015f:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
      IL_0164:  stloc.1
      IL_0165:  ldarg.0
      IL_0166:  ldfld      ""A Program.<Main>d__0.<>7__wrap2""
      IL_016b:  ldloc.1
      IL_016c:  stfld      ""int A.x""
      IL_0171:  ldarg.0
      IL_0172:  ldnull
      IL_0173:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
      IL_0178:  leave.s    IL_0184
    }
    catch object
    {
      IL_017a:  pop
      IL_017b:  ldarg.0
      IL_017c:  ldc.i4.1
      IL_017d:  stfld      ""int Program.<Main>d__0.<>7__wrap3""
      IL_0182:  leave.s    IL_0184
    }
    IL_0184:  ldarg.0
    IL_0185:  ldfld      ""int Program.<Main>d__0.<>7__wrap3""
    IL_018a:  stloc.1
    IL_018b:  ldloc.1
    IL_018c:  ldc.i4.1
    IL_018d:  bne.un     IL_0215
    IL_0192:  ldarg.0
    IL_0193:  ldarg.0
    IL_0194:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_0199:  ldfld      ""A[] Program.<>c__DisplayClass0_0.a""
    IL_019e:  ldc.i4.0
    IL_019f:  ldelem.ref
    IL_01a0:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
    IL_01a5:  ldstr      ""0""
    IL_01aa:  call       ""System.Threading.Tasks.Task<int> Program.<Main>g__Write|0_0(string)""
    IL_01af:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_01b4:  stloc.2
    IL_01b5:  ldloca.s   V_2
    IL_01b7:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_01bc:  brtrue.s   IL_01fa
    IL_01be:  ldarg.0
    IL_01bf:  ldc.i4.2
    IL_01c0:  dup
    IL_01c1:  stloc.0
    IL_01c2:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_01c7:  ldarg.0
    IL_01c8:  ldloc.2
    IL_01c9:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_01ce:  ldarg.0
    IL_01cf:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_01d4:  ldloca.s   V_2
    IL_01d6:  ldarg.0
    IL_01d7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
    IL_01dc:  leave.s    IL_024f
    IL_01de:  ldarg.0
    IL_01df:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_01e4:  stloc.2
    IL_01e5:  ldarg.0
    IL_01e6:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_01eb:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_01f1:  ldarg.0
    IL_01f2:  ldc.i4.m1
    IL_01f3:  dup
    IL_01f4:  stloc.0
    IL_01f5:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_01fa:  ldloca.s   V_2
    IL_01fc:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0201:  stloc.1
    IL_0202:  ldarg.0
    IL_0203:  ldfld      ""A Program.<Main>d__0.<>7__wrap2""
    IL_0208:  ldloc.1
    IL_0209:  stfld      ""int A.x""
    IL_020e:  ldarg.0
    IL_020f:  ldnull
    IL_0210:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
    IL_0215:  leave.s    IL_0235
  }
  catch System.Exception
  {
    IL_0217:  stloc.3
    IL_0218:  ldarg.0
    IL_0219:  ldc.i4.s   -2
    IL_021b:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_0220:  ldarg.0
    IL_0221:  ldnull
    IL_0222:  stfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_0227:  ldarg.0
    IL_0228:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_022d:  ldloc.3
    IL_022e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0233:  leave.s    IL_024f
  }
  IL_0235:  ldarg.0
  IL_0236:  ldc.i4.s   -2
  IL_0238:  stfld      ""int Program.<Main>d__0.<>1__state""
  IL_023d:  ldarg.0
  IL_023e:  ldnull
  IL_023f:  stfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
  IL_0244:  ldarg.0
  IL_0245:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
  IL_024a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_024f:  ret
}");
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void KeepLtrSemantics_StructFieldAccessOnStructFieldAccessOnClassField()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        A a = null;
        try
        {
            a.b.c.x = await WriteAndSetA(""First"");
        }
        catch 
        {
            a = new A();
            a.b.c.x = await WriteAndSetA(""Second"");
            Console.WriteLine(a.b.c.x);

            a.b.c.x = await Write(""Third"");
            Console.WriteLine(a.b.c.x);
        }                       
                                      
        async Task<int> WriteAndSetA(string s)
        {
            a = new A();
            await Task.Yield();
            Console.WriteLine(s);
            return 5;
        }

        async Task<int> Write(string s)
        {
            await Task.Yield();
            Console.WriteLine(s);
            return 5;
        }
    }
}

class A
{
    public B b;
}

struct B
{
    public C c;
}

struct C
{
    public int x;
}";
            var comp = CSharpTestBase.CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"Second
0
Third
5")
                .VerifyIL("Program.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      696 (0x2b8)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  switch    (
        IL_0037,
        IL_0164,
        IL_021d)
    IL_0019:  ldarg.0
    IL_001a:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
    IL_001f:  stfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_0024:  ldarg.0
    IL_0025:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_002a:  ldnull
    IL_002b:  stfld      ""A Program.<>c__DisplayClass0_0.a""
    IL_0030:  ldarg.0
    IL_0031:  ldc.i4.0
    IL_0032:  stfld      ""int Program.<Main>d__0.<>7__wrap1""
    IL_0037:  nop
    .try
    {
      IL_0038:  ldloc.0
      IL_0039:  brfalse.s  IL_009a
      IL_003b:  ldarg.0
      IL_003c:  ldarg.0
      IL_003d:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
      IL_0042:  ldfld      ""A Program.<>c__DisplayClass0_0.a""
      IL_0047:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
      IL_004c:  ldarg.0
      IL_004d:  ldfld      ""A Program.<Main>d__0.<>7__wrap2""
      IL_0052:  ldfld      ""B A.b""
      IL_0057:  pop
      IL_0058:  ldarg.0
      IL_0059:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
      IL_005e:  ldstr      ""First""
      IL_0063:  callvirt   ""System.Threading.Tasks.Task<int> Program.<>c__DisplayClass0_0.<Main>g__WriteAndSetA|0(string)""
      IL_0068:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
      IL_006d:  stloc.2
      IL_006e:  ldloca.s   V_2
      IL_0070:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
      IL_0075:  brtrue.s   IL_00b6
      IL_0077:  ldarg.0
      IL_0078:  ldc.i4.0
      IL_0079:  dup
      IL_007a:  stloc.0
      IL_007b:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_0080:  ldarg.0
      IL_0081:  ldloc.2
      IL_0082:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_0087:  ldarg.0
      IL_0088:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
      IL_008d:  ldloca.s   V_2
      IL_008f:  ldarg.0
      IL_0090:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
      IL_0095:  leave      IL_02b7
      IL_009a:  ldarg.0
      IL_009b:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_00a0:  stloc.2
      IL_00a1:  ldarg.0
      IL_00a2:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_00a7:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_00ad:  ldarg.0
      IL_00ae:  ldc.i4.m1
      IL_00af:  dup
      IL_00b0:  stloc.0
      IL_00b1:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_00b6:  ldloca.s   V_2
      IL_00b8:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
      IL_00bd:  stloc.1
      IL_00be:  ldarg.0
      IL_00bf:  ldfld      ""A Program.<Main>d__0.<>7__wrap2""
      IL_00c4:  ldflda     ""B A.b""
      IL_00c9:  ldflda     ""C B.c""
      IL_00ce:  ldloc.1
      IL_00cf:  stfld      ""int C.x""
      IL_00d4:  ldarg.0
      IL_00d5:  ldnull
      IL_00d6:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
      IL_00db:  leave.s    IL_00e7
    }
    catch object
    {
      IL_00dd:  pop
      IL_00de:  ldarg.0
      IL_00df:  ldc.i4.1
      IL_00e0:  stfld      ""int Program.<Main>d__0.<>7__wrap1""
      IL_00e5:  leave.s    IL_00e7
    }
    IL_00e7:  ldarg.0
    IL_00e8:  ldfld      ""int Program.<Main>d__0.<>7__wrap1""
    IL_00ed:  stloc.1
    IL_00ee:  ldloc.1
    IL_00ef:  ldc.i4.1
    IL_00f0:  bne.un     IL_027d
    IL_00f5:  ldarg.0
    IL_00f6:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_00fb:  newobj     ""A..ctor()""
    IL_0100:  stfld      ""A Program.<>c__DisplayClass0_0.a""
    IL_0105:  ldarg.0
    IL_0106:  ldarg.0
    IL_0107:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_010c:  ldfld      ""A Program.<>c__DisplayClass0_0.a""
    IL_0111:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
    IL_0116:  ldarg.0
    IL_0117:  ldfld      ""A Program.<Main>d__0.<>7__wrap2""
    IL_011c:  ldfld      ""B A.b""
    IL_0121:  pop
    IL_0122:  ldarg.0
    IL_0123:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_0128:  ldstr      ""Second""
    IL_012d:  callvirt   ""System.Threading.Tasks.Task<int> Program.<>c__DisplayClass0_0.<Main>g__WriteAndSetA|0(string)""
    IL_0132:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0137:  stloc.2
    IL_0138:  ldloca.s   V_2
    IL_013a:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_013f:  brtrue.s   IL_0180
    IL_0141:  ldarg.0
    IL_0142:  ldc.i4.1
    IL_0143:  dup
    IL_0144:  stloc.0
    IL_0145:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_014a:  ldarg.0
    IL_014b:  ldloc.2
    IL_014c:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_0151:  ldarg.0
    IL_0152:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_0157:  ldloca.s   V_2
    IL_0159:  ldarg.0
    IL_015a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
    IL_015f:  leave      IL_02b7
    IL_0164:  ldarg.0
    IL_0165:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_016a:  stloc.2
    IL_016b:  ldarg.0
    IL_016c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_0171:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0177:  ldarg.0
    IL_0178:  ldc.i4.m1
    IL_0179:  dup
    IL_017a:  stloc.0
    IL_017b:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_0180:  ldloca.s   V_2
    IL_0182:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0187:  stloc.1
    IL_0188:  ldarg.0
    IL_0189:  ldfld      ""A Program.<Main>d__0.<>7__wrap2""
    IL_018e:  ldflda     ""B A.b""
    IL_0193:  ldflda     ""C B.c""
    IL_0198:  ldloc.1
    IL_0199:  stfld      ""int C.x""
    IL_019e:  ldarg.0
    IL_019f:  ldnull
    IL_01a0:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
    IL_01a5:  ldarg.0
    IL_01a6:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_01ab:  ldfld      ""A Program.<>c__DisplayClass0_0.a""
    IL_01b0:  ldflda     ""B A.b""
    IL_01b5:  ldflda     ""C B.c""
    IL_01ba:  ldfld      ""int C.x""
    IL_01bf:  call       ""void System.Console.WriteLine(int)""
    IL_01c4:  ldarg.0
    IL_01c5:  ldarg.0
    IL_01c6:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_01cb:  ldfld      ""A Program.<>c__DisplayClass0_0.a""
    IL_01d0:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
    IL_01d5:  ldarg.0
    IL_01d6:  ldfld      ""A Program.<Main>d__0.<>7__wrap2""
    IL_01db:  ldfld      ""B A.b""
    IL_01e0:  pop
    IL_01e1:  ldstr      ""Third""
    IL_01e6:  call       ""System.Threading.Tasks.Task<int> Program.<Main>g__Write|0_1(string)""
    IL_01eb:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_01f0:  stloc.2
    IL_01f1:  ldloca.s   V_2
    IL_01f3:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_01f8:  brtrue.s   IL_0239
    IL_01fa:  ldarg.0
    IL_01fb:  ldc.i4.2
    IL_01fc:  dup
    IL_01fd:  stloc.0
    IL_01fe:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_0203:  ldarg.0
    IL_0204:  ldloc.2
    IL_0205:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_020a:  ldarg.0
    IL_020b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_0210:  ldloca.s   V_2
    IL_0212:  ldarg.0
    IL_0213:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
    IL_0218:  leave      IL_02b7
    IL_021d:  ldarg.0
    IL_021e:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_0223:  stloc.2
    IL_0224:  ldarg.0
    IL_0225:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_022a:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0230:  ldarg.0
    IL_0231:  ldc.i4.m1
    IL_0232:  dup
    IL_0233:  stloc.0
    IL_0234:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_0239:  ldloca.s   V_2
    IL_023b:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0240:  stloc.1
    IL_0241:  ldarg.0
    IL_0242:  ldfld      ""A Program.<Main>d__0.<>7__wrap2""
    IL_0247:  ldflda     ""B A.b""
    IL_024c:  ldflda     ""C B.c""
    IL_0251:  ldloc.1
    IL_0252:  stfld      ""int C.x""
    IL_0257:  ldarg.0
    IL_0258:  ldnull
    IL_0259:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
    IL_025e:  ldarg.0
    IL_025f:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_0264:  ldfld      ""A Program.<>c__DisplayClass0_0.a""
    IL_0269:  ldflda     ""B A.b""
    IL_026e:  ldflda     ""C B.c""
    IL_0273:  ldfld      ""int C.x""
    IL_0278:  call       ""void System.Console.WriteLine(int)""
    IL_027d:  leave.s    IL_029d
  }
  catch System.Exception
  {
    IL_027f:  stloc.3
    IL_0280:  ldarg.0
    IL_0281:  ldc.i4.s   -2
    IL_0283:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_0288:  ldarg.0
    IL_0289:  ldnull
    IL_028a:  stfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_028f:  ldarg.0
    IL_0290:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_0295:  ldloc.3
    IL_0296:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_029b:  leave.s    IL_02b7
  }
  IL_029d:  ldarg.0
  IL_029e:  ldc.i4.s   -2
  IL_02a0:  stfld      ""int Program.<Main>d__0.<>1__state""
  IL_02a5:  ldarg.0
  IL_02a6:  ldnull
  IL_02a7:  stfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
  IL_02ac:  ldarg.0
  IL_02ad:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
  IL_02b2:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_02b7:  ret
}");
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42464")]
        public void KeepLtrSemantics_StructFieldAccessOnArray()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var a = new A[1];
        try
        {
            a[1].x = await Write(""1"");
        }
        catch 
        {
            a[0].x = await Write(""0"");
        }                       
        
        var index = 0;

        async Task<int> Write(string s)
        {
            await Task.Yield();
            Console.WriteLine(s);
            return 5;
        }

        a[index].x = await WriteAndSetIndexAndArray(""0"");

        async Task<int> WriteAndSetIndexAndArray(string s)
        {
            a = new A[0];
            index = 1;
            await Task.Yield();
            Console.WriteLine(s);
            return 5;
        }
    }
}

struct A
{
    public int x;
}";
            var comp = CSharpTestBase.CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"0
0")
                .VerifyIL("Program.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      644 (0x284)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  switch    (
        IL_003c,
        IL_014b,
        IL_0207)
    IL_0019:  ldarg.0
    IL_001a:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
    IL_001f:  stfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_0024:  ldarg.0
    IL_0025:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_002a:  ldc.i4.1
    IL_002b:  newarr     ""A""
    IL_0030:  stfld      ""A[] Program.<>c__DisplayClass0_0.a""
    IL_0035:  ldarg.0
    IL_0036:  ldc.i4.0
    IL_0037:  stfld      ""int Program.<Main>d__0.<>7__wrap1""
    IL_003c:  nop
    .try
    {
      IL_003d:  ldloc.0
      IL_003e:  brfalse.s  IL_009a
      IL_0040:  ldarg.0
      IL_0041:  ldarg.0
      IL_0042:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
      IL_0047:  ldfld      ""A[] Program.<>c__DisplayClass0_0.a""
      IL_004c:  stfld      ""A[] Program.<Main>d__0.<>7__wrap2""
      IL_0051:  ldarg.0
      IL_0052:  ldfld      ""A[] Program.<Main>d__0.<>7__wrap2""
      IL_0057:  ldc.i4.1
      IL_0058:  ldelema    ""A""
      IL_005d:  pop
      IL_005e:  ldstr      ""1""
      IL_0063:  call       ""System.Threading.Tasks.Task<int> Program.<Main>g__Write|0_0(string)""
      IL_0068:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
      IL_006d:  stloc.2
      IL_006e:  ldloca.s   V_2
      IL_0070:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
      IL_0075:  brtrue.s   IL_00b6
      IL_0077:  ldarg.0
      IL_0078:  ldc.i4.0
      IL_0079:  dup
      IL_007a:  stloc.0
      IL_007b:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_0080:  ldarg.0
      IL_0081:  ldloc.2
      IL_0082:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_0087:  ldarg.0
      IL_0088:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
      IL_008d:  ldloca.s   V_2
      IL_008f:  ldarg.0
      IL_0090:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
      IL_0095:  leave      IL_0283
      IL_009a:  ldarg.0
      IL_009b:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_00a0:  stloc.2
      IL_00a1:  ldarg.0
      IL_00a2:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_00a7:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_00ad:  ldarg.0
      IL_00ae:  ldc.i4.m1
      IL_00af:  dup
      IL_00b0:  stloc.0
      IL_00b1:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_00b6:  ldloca.s   V_2
      IL_00b8:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
      IL_00bd:  stloc.1
      IL_00be:  ldarg.0
      IL_00bf:  ldfld      ""A[] Program.<Main>d__0.<>7__wrap2""
      IL_00c4:  ldc.i4.1
      IL_00c5:  ldelema    ""A""
      IL_00ca:  ldloc.1
      IL_00cb:  stfld      ""int A.x""
      IL_00d0:  ldarg.0
      IL_00d1:  ldnull
      IL_00d2:  stfld      ""A[] Program.<Main>d__0.<>7__wrap2""
      IL_00d7:  leave.s    IL_00e3
    }
    catch object
    {
      IL_00d9:  pop
      IL_00da:  ldarg.0
      IL_00db:  ldc.i4.1
      IL_00dc:  stfld      ""int Program.<Main>d__0.<>7__wrap1""
      IL_00e1:  leave.s    IL_00e3
    }
    IL_00e3:  ldarg.0
    IL_00e4:  ldfld      ""int Program.<Main>d__0.<>7__wrap1""
    IL_00e9:  stloc.1
    IL_00ea:  ldloc.1
    IL_00eb:  ldc.i4.1
    IL_00ec:  bne.un     IL_0188
    IL_00f1:  ldarg.0
    IL_00f2:  ldarg.0
    IL_00f3:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_00f8:  ldfld      ""A[] Program.<>c__DisplayClass0_0.a""
    IL_00fd:  stfld      ""A[] Program.<Main>d__0.<>7__wrap2""
    IL_0102:  ldarg.0
    IL_0103:  ldfld      ""A[] Program.<Main>d__0.<>7__wrap2""
    IL_0108:  ldc.i4.0
    IL_0109:  ldelema    ""A""
    IL_010e:  pop
    IL_010f:  ldstr      ""0""
    IL_0114:  call       ""System.Threading.Tasks.Task<int> Program.<Main>g__Write|0_0(string)""
    IL_0119:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_011e:  stloc.2
    IL_011f:  ldloca.s   V_2
    IL_0121:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0126:  brtrue.s   IL_0167
    IL_0128:  ldarg.0
    IL_0129:  ldc.i4.1
    IL_012a:  dup
    IL_012b:  stloc.0
    IL_012c:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_0131:  ldarg.0
    IL_0132:  ldloc.2
    IL_0133:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_0138:  ldarg.0
    IL_0139:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_013e:  ldloca.s   V_2
    IL_0140:  ldarg.0
    IL_0141:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
    IL_0146:  leave      IL_0283
    IL_014b:  ldarg.0
    IL_014c:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_0151:  stloc.2
    IL_0152:  ldarg.0
    IL_0153:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_0158:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_015e:  ldarg.0
    IL_015f:  ldc.i4.m1
    IL_0160:  dup
    IL_0161:  stloc.0
    IL_0162:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_0167:  ldloca.s   V_2
    IL_0169:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_016e:  stloc.1
    IL_016f:  ldarg.0
    IL_0170:  ldfld      ""A[] Program.<Main>d__0.<>7__wrap2""
    IL_0175:  ldc.i4.0
    IL_0176:  ldelema    ""A""
    IL_017b:  ldloc.1
    IL_017c:  stfld      ""int A.x""
    IL_0181:  ldarg.0
    IL_0182:  ldnull
    IL_0183:  stfld      ""A[] Program.<Main>d__0.<>7__wrap2""
    IL_0188:  ldarg.0
    IL_0189:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_018e:  ldc.i4.0
    IL_018f:  stfld      ""int Program.<>c__DisplayClass0_0.index""
    IL_0194:  ldarg.0
    IL_0195:  ldarg.0
    IL_0196:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_019b:  ldfld      ""A[] Program.<>c__DisplayClass0_0.a""
    IL_01a0:  stfld      ""A[] Program.<Main>d__0.<>7__wrap2""
    IL_01a5:  ldarg.0
    IL_01a6:  ldarg.0
    IL_01a7:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_01ac:  ldfld      ""int Program.<>c__DisplayClass0_0.index""
    IL_01b1:  stfld      ""int Program.<Main>d__0.<>7__wrap1""
    IL_01b6:  ldarg.0
    IL_01b7:  ldfld      ""A[] Program.<Main>d__0.<>7__wrap2""
    IL_01bc:  ldarg.0
    IL_01bd:  ldfld      ""int Program.<Main>d__0.<>7__wrap1""
    IL_01c2:  ldelema    ""A""
    IL_01c7:  pop
    IL_01c8:  ldarg.0
    IL_01c9:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_01ce:  ldstr      ""0""
    IL_01d3:  callvirt   ""System.Threading.Tasks.Task<int> Program.<>c__DisplayClass0_0.<Main>g__WriteAndSetIndexAndArray|1(string)""
    IL_01d8:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_01dd:  stloc.2
    IL_01de:  ldloca.s   V_2
    IL_01e0:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_01e5:  brtrue.s   IL_0223
    IL_01e7:  ldarg.0
    IL_01e8:  ldc.i4.2
    IL_01e9:  dup
    IL_01ea:  stloc.0
    IL_01eb:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_01f0:  ldarg.0
    IL_01f1:  ldloc.2
    IL_01f2:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_01f7:  ldarg.0
    IL_01f8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_01fd:  ldloca.s   V_2
    IL_01ff:  ldarg.0
    IL_0200:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
    IL_0205:  leave.s    IL_0283
    IL_0207:  ldarg.0
    IL_0208:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_020d:  stloc.2
    IL_020e:  ldarg.0
    IL_020f:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_0214:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_021a:  ldarg.0
    IL_021b:  ldc.i4.m1
    IL_021c:  dup
    IL_021d:  stloc.0
    IL_021e:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_0223:  ldloca.s   V_2
    IL_0225:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_022a:  stloc.1
    IL_022b:  ldarg.0
    IL_022c:  ldfld      ""A[] Program.<Main>d__0.<>7__wrap2""
    IL_0231:  ldarg.0
    IL_0232:  ldfld      ""int Program.<Main>d__0.<>7__wrap1""
    IL_0237:  ldelema    ""A""
    IL_023c:  ldloc.1
    IL_023d:  stfld      ""int A.x""
    IL_0242:  ldarg.0
    IL_0243:  ldnull
    IL_0244:  stfld      ""A[] Program.<Main>d__0.<>7__wrap2""
    IL_0249:  leave.s    IL_0269
  }
  catch System.Exception
  {
    IL_024b:  stloc.3
    IL_024c:  ldarg.0
    IL_024d:  ldc.i4.s   -2
    IL_024f:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_0254:  ldarg.0
    IL_0255:  ldnull
    IL_0256:  stfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_025b:  ldarg.0
    IL_025c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_0261:  ldloc.3
    IL_0262:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0267:  leave.s    IL_0283
  }
  IL_0269:  ldarg.0
  IL_026a:  ldc.i4.s   -2
  IL_026c:  stfld      ""int Program.<Main>d__0.<>1__state""
  IL_0271:  ldarg.0
  IL_0272:  ldnull
  IL_0273:  stfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
  IL_0278:  ldarg.0
  IL_0279:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
  IL_027e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0283:  ret
}");
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42464")]
        public void KeepLtrSemantics_ArrayAccess()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var a = new int[1];
        try
        {
            a[1] = await Write(""1"");
        }
        catch 
        {
            a[0] = await Write(""0"");
        }                       
        
        var index = 0;

        async Task<int> Write(string s)
        {
            await Task.Yield();
            Console.WriteLine(s);
            return 5;
        }

        a[index] = await WriteAndSetIndexAndArray(""0"");

        async Task<int> WriteAndSetIndexAndArray(string s)
        {
            a = new int[0];
            index = 1;
            await Task.Yield();
            Console.WriteLine(s);
            return 5;
        }
    }
}";
            var comp = CSharpTestBase.CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"1
0
0")
                .VerifyIL("Program.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      573 (0x23d)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  switch    (
        IL_003c,
        IL_0128,
        IL_01c9)
    IL_0019:  ldarg.0
    IL_001a:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
    IL_001f:  stfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_0024:  ldarg.0
    IL_0025:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_002a:  ldc.i4.1
    IL_002b:  newarr     ""int""
    IL_0030:  stfld      ""int[] Program.<>c__DisplayClass0_0.a""
    IL_0035:  ldarg.0
    IL_0036:  ldc.i4.0
    IL_0037:  stfld      ""int Program.<Main>d__0.<>7__wrap1""
    IL_003c:  nop
    .try
    {
      IL_003d:  ldloc.0
      IL_003e:  brfalse.s  IL_008d
      IL_0040:  ldarg.0
      IL_0041:  ldarg.0
      IL_0042:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
      IL_0047:  ldfld      ""int[] Program.<>c__DisplayClass0_0.a""
      IL_004c:  stfld      ""int[] Program.<Main>d__0.<>7__wrap2""
      IL_0051:  ldstr      ""1""
      IL_0056:  call       ""System.Threading.Tasks.Task<int> Program.<Main>g__Write|0_0(string)""
      IL_005b:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
      IL_0060:  stloc.2
      IL_0061:  ldloca.s   V_2
      IL_0063:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
      IL_0068:  brtrue.s   IL_00a9
      IL_006a:  ldarg.0
      IL_006b:  ldc.i4.0
      IL_006c:  dup
      IL_006d:  stloc.0
      IL_006e:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_0073:  ldarg.0
      IL_0074:  ldloc.2
      IL_0075:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_007a:  ldarg.0
      IL_007b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
      IL_0080:  ldloca.s   V_2
      IL_0082:  ldarg.0
      IL_0083:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
      IL_0088:  leave      IL_023c
      IL_008d:  ldarg.0
      IL_008e:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_0093:  stloc.2
      IL_0094:  ldarg.0
      IL_0095:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_009a:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_00a0:  ldarg.0
      IL_00a1:  ldc.i4.m1
      IL_00a2:  dup
      IL_00a3:  stloc.0
      IL_00a4:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_00a9:  ldloca.s   V_2
      IL_00ab:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
      IL_00b0:  stloc.1
      IL_00b1:  ldarg.0
      IL_00b2:  ldfld      ""int[] Program.<Main>d__0.<>7__wrap2""
      IL_00b7:  ldc.i4.1
      IL_00b8:  ldloc.1
      IL_00b9:  stelem.i4
      IL_00ba:  ldarg.0
      IL_00bb:  ldnull
      IL_00bc:  stfld      ""int[] Program.<Main>d__0.<>7__wrap2""
      IL_00c1:  leave.s    IL_00cd
    }
    catch object
    {
      IL_00c3:  pop
      IL_00c4:  ldarg.0
      IL_00c5:  ldc.i4.1
      IL_00c6:  stfld      ""int Program.<Main>d__0.<>7__wrap1""
      IL_00cb:  leave.s    IL_00cd
    }
    IL_00cd:  ldarg.0
    IL_00ce:  ldfld      ""int Program.<Main>d__0.<>7__wrap1""
    IL_00d3:  stloc.1
    IL_00d4:  ldloc.1
    IL_00d5:  ldc.i4.1
    IL_00d6:  bne.un     IL_015c
    IL_00db:  ldarg.0
    IL_00dc:  ldarg.0
    IL_00dd:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_00e2:  ldfld      ""int[] Program.<>c__DisplayClass0_0.a""
    IL_00e7:  stfld      ""int[] Program.<Main>d__0.<>7__wrap2""
    IL_00ec:  ldstr      ""0""
    IL_00f1:  call       ""System.Threading.Tasks.Task<int> Program.<Main>g__Write|0_0(string)""
    IL_00f6:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_00fb:  stloc.2
    IL_00fc:  ldloca.s   V_2
    IL_00fe:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0103:  brtrue.s   IL_0144
    IL_0105:  ldarg.0
    IL_0106:  ldc.i4.1
    IL_0107:  dup
    IL_0108:  stloc.0
    IL_0109:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_010e:  ldarg.0
    IL_010f:  ldloc.2
    IL_0110:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_0115:  ldarg.0
    IL_0116:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_011b:  ldloca.s   V_2
    IL_011d:  ldarg.0
    IL_011e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
    IL_0123:  leave      IL_023c
    IL_0128:  ldarg.0
    IL_0129:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_012e:  stloc.2
    IL_012f:  ldarg.0
    IL_0130:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_0135:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_013b:  ldarg.0
    IL_013c:  ldc.i4.m1
    IL_013d:  dup
    IL_013e:  stloc.0
    IL_013f:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_0144:  ldloca.s   V_2
    IL_0146:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_014b:  stloc.1
    IL_014c:  ldarg.0
    IL_014d:  ldfld      ""int[] Program.<Main>d__0.<>7__wrap2""
    IL_0152:  ldc.i4.0
    IL_0153:  ldloc.1
    IL_0154:  stelem.i4
    IL_0155:  ldarg.0
    IL_0156:  ldnull
    IL_0157:  stfld      ""int[] Program.<Main>d__0.<>7__wrap2""
    IL_015c:  ldarg.0
    IL_015d:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_0162:  ldc.i4.0
    IL_0163:  stfld      ""int Program.<>c__DisplayClass0_0.index""
    IL_0168:  ldarg.0
    IL_0169:  ldarg.0
    IL_016a:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_016f:  ldfld      ""int[] Program.<>c__DisplayClass0_0.a""
    IL_0174:  stfld      ""int[] Program.<Main>d__0.<>7__wrap2""
    IL_0179:  ldarg.0
    IL_017a:  ldarg.0
    IL_017b:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_0180:  ldfld      ""int Program.<>c__DisplayClass0_0.index""
    IL_0185:  stfld      ""int Program.<Main>d__0.<>7__wrap1""
    IL_018a:  ldarg.0
    IL_018b:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_0190:  ldstr      ""0""
    IL_0195:  callvirt   ""System.Threading.Tasks.Task<int> Program.<>c__DisplayClass0_0.<Main>g__WriteAndSetIndexAndArray|1(string)""
    IL_019a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_019f:  stloc.2
    IL_01a0:  ldloca.s   V_2
    IL_01a2:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_01a7:  brtrue.s   IL_01e5
    IL_01a9:  ldarg.0
    IL_01aa:  ldc.i4.2
    IL_01ab:  dup
    IL_01ac:  stloc.0
    IL_01ad:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_01b2:  ldarg.0
    IL_01b3:  ldloc.2
    IL_01b4:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_01b9:  ldarg.0
    IL_01ba:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_01bf:  ldloca.s   V_2
    IL_01c1:  ldarg.0
    IL_01c2:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
    IL_01c7:  leave.s    IL_023c
    IL_01c9:  ldarg.0
    IL_01ca:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_01cf:  stloc.2
    IL_01d0:  ldarg.0
    IL_01d1:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_01d6:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_01dc:  ldarg.0
    IL_01dd:  ldc.i4.m1
    IL_01de:  dup
    IL_01df:  stloc.0
    IL_01e0:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_01e5:  ldloca.s   V_2
    IL_01e7:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_01ec:  stloc.1
    IL_01ed:  ldarg.0
    IL_01ee:  ldfld      ""int[] Program.<Main>d__0.<>7__wrap2""
    IL_01f3:  ldarg.0
    IL_01f4:  ldfld      ""int Program.<Main>d__0.<>7__wrap1""
    IL_01f9:  ldloc.1
    IL_01fa:  stelem.i4
    IL_01fb:  ldarg.0
    IL_01fc:  ldnull
    IL_01fd:  stfld      ""int[] Program.<Main>d__0.<>7__wrap2""
    IL_0202:  leave.s    IL_0222
  }
  catch System.Exception
  {
    IL_0204:  stloc.3
    IL_0205:  ldarg.0
    IL_0206:  ldc.i4.s   -2
    IL_0208:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_020d:  ldarg.0
    IL_020e:  ldnull
    IL_020f:  stfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_0214:  ldarg.0
    IL_0215:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_021a:  ldloc.3
    IL_021b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0220:  leave.s    IL_023c
  }
  IL_0222:  ldarg.0
  IL_0223:  ldc.i4.s   -2
  IL_0225:  stfld      ""int Program.<Main>d__0.<>1__state""
  IL_022a:  ldarg.0
  IL_022b:  ldnull
  IL_022c:  stfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
  IL_0231:  ldarg.0
  IL_0232:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
  IL_0237:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_023c:  ret
}");
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void KeepNonAsyncSemantics_FieldAccessOnClass()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        A a = null;
        try
        {
            a.x = await WriteAndSet(""First"");
        }
        catch 
        {
            a.x = await Write(""Second"");
        }                       
                                      
        async Task<int> Write(string s)
        {
            await Task.Yield();
            Console.WriteLine(s);
            return 5;
        }

        async Task<int> WriteAndSet(string s)
        {
            a = new A();
            await Task.Yield();
            Console.WriteLine(s);
            return 5;
        }
    }
}

class A
{
    public int x;
}";
            var comp = CSharpTestBase.CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"First
Second")
                .VerifyIL("Program.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      403 (0x193)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_002f
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_0121
    IL_0011:  ldarg.0
    IL_0012:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
    IL_0017:  stfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_001c:  ldarg.0
    IL_001d:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_0022:  ldnull
    IL_0023:  stfld      ""A Program.<>c__DisplayClass0_0.a""
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  stfld      ""int Program.<Main>d__0.<>7__wrap1""
    IL_002f:  nop
    .try
    {
      IL_0030:  ldloc.0
      IL_0031:  brfalse.s  IL_0086
      IL_0033:  ldarg.0
      IL_0034:  ldarg.0
      IL_0035:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
      IL_003a:  ldfld      ""A Program.<>c__DisplayClass0_0.a""
      IL_003f:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
      IL_0044:  ldarg.0
      IL_0045:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
      IL_004a:  ldstr      ""First""
      IL_004f:  callvirt   ""System.Threading.Tasks.Task<int> Program.<>c__DisplayClass0_0.<Main>g__WriteAndSet|1(string)""
      IL_0054:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
      IL_0059:  stloc.2
      IL_005a:  ldloca.s   V_2
      IL_005c:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
      IL_0061:  brtrue.s   IL_00a2
      IL_0063:  ldarg.0
      IL_0064:  ldc.i4.0
      IL_0065:  dup
      IL_0066:  stloc.0
      IL_0067:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_006c:  ldarg.0
      IL_006d:  ldloc.2
      IL_006e:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_0073:  ldarg.0
      IL_0074:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
      IL_0079:  ldloca.s   V_2
      IL_007b:  ldarg.0
      IL_007c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
      IL_0081:  leave      IL_0192
      IL_0086:  ldarg.0
      IL_0087:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_008c:  stloc.2
      IL_008d:  ldarg.0
      IL_008e:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_0093:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_0099:  ldarg.0
      IL_009a:  ldc.i4.m1
      IL_009b:  dup
      IL_009c:  stloc.0
      IL_009d:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_00a2:  ldloca.s   V_2
      IL_00a4:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
      IL_00a9:  stloc.1
      IL_00aa:  ldarg.0
      IL_00ab:  ldfld      ""A Program.<Main>d__0.<>7__wrap2""
      IL_00b0:  ldloc.1
      IL_00b1:  stfld      ""int A.x""
      IL_00b6:  ldarg.0
      IL_00b7:  ldnull
      IL_00b8:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
      IL_00bd:  leave.s    IL_00c9
    }
    catch object
    {
      IL_00bf:  pop
      IL_00c0:  ldarg.0
      IL_00c1:  ldc.i4.1
      IL_00c2:  stfld      ""int Program.<Main>d__0.<>7__wrap1""
      IL_00c7:  leave.s    IL_00c9
    }
    IL_00c9:  ldarg.0
    IL_00ca:  ldfld      ""int Program.<Main>d__0.<>7__wrap1""
    IL_00cf:  stloc.1
    IL_00d0:  ldloc.1
    IL_00d1:  ldc.i4.1
    IL_00d2:  bne.un     IL_0158
    IL_00d7:  ldarg.0
    IL_00d8:  ldarg.0
    IL_00d9:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_00de:  ldfld      ""A Program.<>c__DisplayClass0_0.a""
    IL_00e3:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
    IL_00e8:  ldstr      ""Second""
    IL_00ed:  call       ""System.Threading.Tasks.Task<int> Program.<Main>g__Write|0_0(string)""
    IL_00f2:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_00f7:  stloc.2
    IL_00f8:  ldloca.s   V_2
    IL_00fa:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00ff:  brtrue.s   IL_013d
    IL_0101:  ldarg.0
    IL_0102:  ldc.i4.1
    IL_0103:  dup
    IL_0104:  stloc.0
    IL_0105:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_010a:  ldarg.0
    IL_010b:  ldloc.2
    IL_010c:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_0111:  ldarg.0
    IL_0112:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_0117:  ldloca.s   V_2
    IL_0119:  ldarg.0
    IL_011a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
    IL_011f:  leave.s    IL_0192
    IL_0121:  ldarg.0
    IL_0122:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_0127:  stloc.2
    IL_0128:  ldarg.0
    IL_0129:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_012e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0134:  ldarg.0
    IL_0135:  ldc.i4.m1
    IL_0136:  dup
    IL_0137:  stloc.0
    IL_0138:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_013d:  ldloca.s   V_2
    IL_013f:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0144:  stloc.1
    IL_0145:  ldarg.0
    IL_0146:  ldfld      ""A Program.<Main>d__0.<>7__wrap2""
    IL_014b:  ldloc.1
    IL_014c:  stfld      ""int A.x""
    IL_0151:  ldarg.0
    IL_0152:  ldnull
    IL_0153:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
    IL_0158:  leave.s    IL_0178
  }
  catch System.Exception
  {
    IL_015a:  stloc.3
    IL_015b:  ldarg.0
    IL_015c:  ldc.i4.s   -2
    IL_015e:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_0163:  ldarg.0
    IL_0164:  ldnull
    IL_0165:  stfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_016a:  ldarg.0
    IL_016b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_0170:  ldloc.3
    IL_0171:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0176:  leave.s    IL_0192
  }
  IL_0178:  ldarg.0
  IL_0179:  ldc.i4.s   -2
  IL_017b:  stfld      ""int Program.<Main>d__0.<>1__state""
  IL_0180:  ldarg.0
  IL_0181:  ldnull
  IL_0182:  stfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
  IL_0187:  ldarg.0
  IL_0188:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
  IL_018d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0192:  ret
}");
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void CompoundAssignment()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        A a = null;
        try
        {
            a.x += await WriteAndSet(""First"");
        }
        catch 
        {
            Console.WriteLine(a is null);
            a = new A{ x = 10 };
            var aCopy = a;
            a.x += await WriteAndSet(""Second"");
            Console.WriteLine(a.x);
            Console.WriteLine(aCopy.x);
        }

        async Task<int> WriteAndSet(string s)
        {
            a = new A();
            await Task.Yield();
            Console.WriteLine(s);
            return 5;
        }
    }
}

class A
{
    public int x;
}";
            var comp = CSharpTestBase.CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"True
Second
0
15")
                .VerifyIL("Program.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      560 (0x230)
  .maxstack  4
  .locals init (int V_0,
                A V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_002f
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_0189
    IL_0011:  ldarg.0
    IL_0012:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
    IL_0017:  stfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_001c:  ldarg.0
    IL_001d:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_0022:  ldnull
    IL_0023:  stfld      ""A Program.<>c__DisplayClass0_0.a""
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  stfld      ""int Program.<Main>d__0.<>7__wrap1""
    IL_002f:  nop
    .try
    {
      IL_0030:  ldloc.0
      IL_0031:  brfalse.s  IL_0094
      IL_0033:  ldarg.0
      IL_0034:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
      IL_0039:  ldfld      ""A Program.<>c__DisplayClass0_0.a""
      IL_003e:  stloc.1
      IL_003f:  ldarg.0
      IL_0040:  ldloc.1
      IL_0041:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
      IL_0046:  ldarg.0
      IL_0047:  ldloc.1
      IL_0048:  ldfld      ""int A.x""
      IL_004d:  stfld      ""int Program.<Main>d__0.<>7__wrap3""
      IL_0052:  ldarg.0
      IL_0053:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
      IL_0058:  ldstr      ""First""
      IL_005d:  callvirt   ""System.Threading.Tasks.Task<int> Program.<>c__DisplayClass0_0.<Main>g__WriteAndSet|0(string)""
      IL_0062:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
      IL_0067:  stloc.3
      IL_0068:  ldloca.s   V_3
      IL_006a:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
      IL_006f:  brtrue.s   IL_00b0
      IL_0071:  ldarg.0
      IL_0072:  ldc.i4.0
      IL_0073:  dup
      IL_0074:  stloc.0
      IL_0075:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_007a:  ldarg.0
      IL_007b:  ldloc.3
      IL_007c:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_0081:  ldarg.0
      IL_0082:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
      IL_0087:  ldloca.s   V_3
      IL_0089:  ldarg.0
      IL_008a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
      IL_008f:  leave      IL_022f
      IL_0094:  ldarg.0
      IL_0095:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_009a:  stloc.3
      IL_009b:  ldarg.0
      IL_009c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_00a1:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_00a7:  ldarg.0
      IL_00a8:  ldc.i4.m1
      IL_00a9:  dup
      IL_00aa:  stloc.0
      IL_00ab:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_00b0:  ldloca.s   V_3
      IL_00b2:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
      IL_00b7:  stloc.2
      IL_00b8:  ldarg.0
      IL_00b9:  ldfld      ""A Program.<Main>d__0.<>7__wrap2""
      IL_00be:  ldarg.0
      IL_00bf:  ldfld      ""int Program.<Main>d__0.<>7__wrap3""
      IL_00c4:  ldloc.2
      IL_00c5:  add
      IL_00c6:  stfld      ""int A.x""
      IL_00cb:  ldarg.0
      IL_00cc:  ldnull
      IL_00cd:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
      IL_00d2:  leave.s    IL_00de
    }
    catch object
    {
      IL_00d4:  pop
      IL_00d5:  ldarg.0
      IL_00d6:  ldc.i4.1
      IL_00d7:  stfld      ""int Program.<Main>d__0.<>7__wrap1""
      IL_00dc:  leave.s    IL_00de
    }
    IL_00de:  ldarg.0
    IL_00df:  ldfld      ""int Program.<Main>d__0.<>7__wrap1""
    IL_00e4:  stloc.2
    IL_00e5:  ldloc.2
    IL_00e6:  ldc.i4.1
    IL_00e7:  bne.un     IL_01f3
    IL_00ec:  ldarg.0
    IL_00ed:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_00f2:  ldfld      ""A Program.<>c__DisplayClass0_0.a""
    IL_00f7:  ldnull
    IL_00f8:  ceq
    IL_00fa:  call       ""void System.Console.WriteLine(bool)""
    IL_00ff:  ldarg.0
    IL_0100:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_0105:  newobj     ""A..ctor()""
    IL_010a:  dup
    IL_010b:  ldc.i4.s   10
    IL_010d:  stfld      ""int A.x""
    IL_0112:  stfld      ""A Program.<>c__DisplayClass0_0.a""
    IL_0117:  ldarg.0
    IL_0118:  ldarg.0
    IL_0119:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_011e:  ldfld      ""A Program.<>c__DisplayClass0_0.a""
    IL_0123:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
    IL_0128:  ldarg.0
    IL_0129:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_012e:  ldfld      ""A Program.<>c__DisplayClass0_0.a""
    IL_0133:  stloc.1
    IL_0134:  ldarg.0
    IL_0135:  ldloc.1
    IL_0136:  stfld      ""A Program.<Main>d__0.<>7__wrap4""
    IL_013b:  ldarg.0
    IL_013c:  ldloc.1
    IL_013d:  ldfld      ""int A.x""
    IL_0142:  stfld      ""int Program.<Main>d__0.<>7__wrap3""
    IL_0147:  ldarg.0
    IL_0148:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_014d:  ldstr      ""Second""
    IL_0152:  callvirt   ""System.Threading.Tasks.Task<int> Program.<>c__DisplayClass0_0.<Main>g__WriteAndSet|0(string)""
    IL_0157:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_015c:  stloc.3
    IL_015d:  ldloca.s   V_3
    IL_015f:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0164:  brtrue.s   IL_01a5
    IL_0166:  ldarg.0
    IL_0167:  ldc.i4.1
    IL_0168:  dup
    IL_0169:  stloc.0
    IL_016a:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_016f:  ldarg.0
    IL_0170:  ldloc.3
    IL_0171:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_0176:  ldarg.0
    IL_0177:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_017c:  ldloca.s   V_3
    IL_017e:  ldarg.0
    IL_017f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
    IL_0184:  leave      IL_022f
    IL_0189:  ldarg.0
    IL_018a:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_018f:  stloc.3
    IL_0190:  ldarg.0
    IL_0191:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_0196:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_019c:  ldarg.0
    IL_019d:  ldc.i4.m1
    IL_019e:  dup
    IL_019f:  stloc.0
    IL_01a0:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_01a5:  ldloca.s   V_3
    IL_01a7:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_01ac:  stloc.2
    IL_01ad:  ldarg.0
    IL_01ae:  ldfld      ""A Program.<Main>d__0.<>7__wrap4""
    IL_01b3:  ldarg.0
    IL_01b4:  ldfld      ""int Program.<Main>d__0.<>7__wrap3""
    IL_01b9:  ldloc.2
    IL_01ba:  add
    IL_01bb:  stfld      ""int A.x""
    IL_01c0:  ldarg.0
    IL_01c1:  ldnull
    IL_01c2:  stfld      ""A Program.<Main>d__0.<>7__wrap4""
    IL_01c7:  ldarg.0
    IL_01c8:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_01cd:  ldfld      ""A Program.<>c__DisplayClass0_0.a""
    IL_01d2:  ldfld      ""int A.x""
    IL_01d7:  call       ""void System.Console.WriteLine(int)""
    IL_01dc:  ldarg.0
    IL_01dd:  ldfld      ""A Program.<Main>d__0.<>7__wrap2""
    IL_01e2:  ldfld      ""int A.x""
    IL_01e7:  call       ""void System.Console.WriteLine(int)""
    IL_01ec:  ldarg.0
    IL_01ed:  ldnull
    IL_01ee:  stfld      ""A Program.<Main>d__0.<>7__wrap2""
    IL_01f3:  leave.s    IL_0215
  }
  catch System.Exception
  {
    IL_01f5:  stloc.s    V_4
    IL_01f7:  ldarg.0
    IL_01f8:  ldc.i4.s   -2
    IL_01fa:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_01ff:  ldarg.0
    IL_0200:  ldnull
    IL_0201:  stfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
    IL_0206:  ldarg.0
    IL_0207:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_020c:  ldloc.s    V_4
    IL_020e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0213:  leave.s    IL_022f
  }
  IL_0215:  ldarg.0
  IL_0216:  ldc.i4.s   -2
  IL_0218:  stfld      ""int Program.<Main>d__0.<>1__state""
  IL_021d:  ldarg.0
  IL_021e:  ldnull
  IL_021f:  stfld      ""Program.<>c__DisplayClass0_0 Program.<Main>d__0.<>8__1""
  IL_0224:  ldarg.0
  IL_0225:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
  IL_022a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_022f:  ret
}");
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void AssignmentToAssignment()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        
        A a = null;
        B b = null;
        try
        {
            b.x = b.x = await Write(""First"");
        }
        catch 
        {
            try
            {
                a.b.x = b.x = await Write(""Second"");
            }
            catch
            {
                try
                {
                    b.x = a.b.x = await Write(""Third"");
                }
                catch
                {
                    a = new A();
                    a.b = new B();
                    b = new B();
                    a.b.x = b.x = await Write(""Fourth"");
                }
            }
        }

        async Task<int> Write(string s)
        {
            await Task.Yield();
            Console.WriteLine(s);
            return 5;
        }
    }
}

class A
{
    public B b;
}

class B
{
    public int x;
}";
            var comp = CSharpTestBase.CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"First
Fourth")
                .VerifyIL("Program.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      924 (0x39c)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                int V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  switch    (
        IL_0032,
        IL_00f7,
        IL_01c2,
        IL_0305)
    IL_001d:  ldarg.0
    IL_001e:  ldnull
    IL_001f:  stfld      ""A Program.<Main>d__0.<a>5__2""
    IL_0024:  ldarg.0
    IL_0025:  ldnull
    IL_0026:  stfld      ""B Program.<Main>d__0.<b>5__3""
    IL_002b:  ldarg.0
    IL_002c:  ldc.i4.0
    IL_002d:  stfld      ""int Program.<Main>d__0.<>7__wrap3""
    IL_0032:  nop
    .try
    {
      IL_0033:  ldloc.0
      IL_0034:  brfalse.s  IL_008a
      IL_0036:  ldarg.0
      IL_0037:  ldarg.0
      IL_0038:  ldfld      ""B Program.<Main>d__0.<b>5__3""
      IL_003d:  stfld      ""B Program.<Main>d__0.<>7__wrap4""
      IL_0042:  ldarg.0
      IL_0043:  ldarg.0
      IL_0044:  ldfld      ""B Program.<Main>d__0.<b>5__3""
      IL_0049:  stfld      ""B Program.<Main>d__0.<>7__wrap5""
      IL_004e:  ldstr      ""First""
      IL_0053:  call       ""System.Threading.Tasks.Task<int> Program.<Main>g__Write|0_0(string)""
      IL_0058:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
      IL_005d:  stloc.2
      IL_005e:  ldloca.s   V_2
      IL_0060:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
      IL_0065:  brtrue.s   IL_00a6
      IL_0067:  ldarg.0
      IL_0068:  ldc.i4.0
      IL_0069:  dup
      IL_006a:  stloc.0
      IL_006b:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_0070:  ldarg.0
      IL_0071:  ldloc.2
      IL_0072:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_0077:  ldarg.0
      IL_0078:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
      IL_007d:  ldloca.s   V_2
      IL_007f:  ldarg.0
      IL_0080:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
      IL_0085:  leave      IL_039b
      IL_008a:  ldarg.0
      IL_008b:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_0090:  stloc.2
      IL_0091:  ldarg.0
      IL_0092:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_0097:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_009d:  ldarg.0
      IL_009e:  ldc.i4.m1
      IL_009f:  dup
      IL_00a0:  stloc.0
      IL_00a1:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_00a6:  ldloca.s   V_2
      IL_00a8:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
      IL_00ad:  stloc.1
      IL_00ae:  ldarg.0
      IL_00af:  ldfld      ""B Program.<Main>d__0.<>7__wrap4""
      IL_00b4:  ldarg.0
      IL_00b5:  ldfld      ""B Program.<Main>d__0.<>7__wrap5""
      IL_00ba:  ldloc.1
      IL_00bb:  dup
      IL_00bc:  stloc.3
      IL_00bd:  stfld      ""int B.x""
      IL_00c2:  ldloc.3
      IL_00c3:  stfld      ""int B.x""
      IL_00c8:  ldarg.0
      IL_00c9:  ldnull
      IL_00ca:  stfld      ""B Program.<Main>d__0.<>7__wrap4""
      IL_00cf:  ldarg.0
      IL_00d0:  ldnull
      IL_00d1:  stfld      ""B Program.<Main>d__0.<>7__wrap5""
      IL_00d6:  leave.s    IL_00e2
    }
    catch object
    {
      IL_00d8:  pop
      IL_00d9:  ldarg.0
      IL_00da:  ldc.i4.1
      IL_00db:  stfld      ""int Program.<Main>d__0.<>7__wrap3""
      IL_00e0:  leave.s    IL_00e2
    }
    IL_00e2:  ldarg.0
    IL_00e3:  ldfld      ""int Program.<Main>d__0.<>7__wrap3""
    IL_00e8:  stloc.1
    IL_00e9:  ldloc.1
    IL_00ea:  ldc.i4.1
    IL_00eb:  bne.un     IL_0351
    IL_00f0:  ldarg.0
    IL_00f1:  ldc.i4.0
    IL_00f2:  stfld      ""int Program.<Main>d__0.<>7__wrap6""
    IL_00f7:  nop
    .try
    {
      IL_00f8:  ldloc.0
      IL_00f9:  ldc.i4.1
      IL_00fa:  beq.s      IL_0155
      IL_00fc:  ldarg.0
      IL_00fd:  ldarg.0
      IL_00fe:  ldfld      ""A Program.<Main>d__0.<a>5__2""
      IL_0103:  ldfld      ""B A.b""
      IL_0108:  stfld      ""B Program.<Main>d__0.<>7__wrap5""
      IL_010d:  ldarg.0
      IL_010e:  ldarg.0
      IL_010f:  ldfld      ""B Program.<Main>d__0.<b>5__3""
      IL_0114:  stfld      ""B Program.<Main>d__0.<>7__wrap4""
      IL_0119:  ldstr      ""Second""
      IL_011e:  call       ""System.Threading.Tasks.Task<int> Program.<Main>g__Write|0_0(string)""
      IL_0123:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
      IL_0128:  stloc.2
      IL_0129:  ldloca.s   V_2
      IL_012b:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
      IL_0130:  brtrue.s   IL_0171
      IL_0132:  ldarg.0
      IL_0133:  ldc.i4.1
      IL_0134:  dup
      IL_0135:  stloc.0
      IL_0136:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_013b:  ldarg.0
      IL_013c:  ldloc.2
      IL_013d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_0142:  ldarg.0
      IL_0143:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
      IL_0148:  ldloca.s   V_2
      IL_014a:  ldarg.0
      IL_014b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
      IL_0150:  leave      IL_039b
      IL_0155:  ldarg.0
      IL_0156:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_015b:  stloc.2
      IL_015c:  ldarg.0
      IL_015d:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_0162:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_0168:  ldarg.0
      IL_0169:  ldc.i4.m1
      IL_016a:  dup
      IL_016b:  stloc.0
      IL_016c:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_0171:  ldloca.s   V_2
      IL_0173:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
      IL_0178:  stloc.1
      IL_0179:  ldarg.0
      IL_017a:  ldfld      ""B Program.<Main>d__0.<>7__wrap5""
      IL_017f:  ldarg.0
      IL_0180:  ldfld      ""B Program.<Main>d__0.<>7__wrap4""
      IL_0185:  ldloc.1
      IL_0186:  dup
      IL_0187:  stloc.3
      IL_0188:  stfld      ""int B.x""
      IL_018d:  ldloc.3
      IL_018e:  stfld      ""int B.x""
      IL_0193:  ldarg.0
      IL_0194:  ldnull
      IL_0195:  stfld      ""B Program.<Main>d__0.<>7__wrap5""
      IL_019a:  ldarg.0
      IL_019b:  ldnull
      IL_019c:  stfld      ""B Program.<Main>d__0.<>7__wrap4""
      IL_01a1:  leave.s    IL_01ad
    }
    catch object
    {
      IL_01a3:  pop
      IL_01a4:  ldarg.0
      IL_01a5:  ldc.i4.1
      IL_01a6:  stfld      ""int Program.<Main>d__0.<>7__wrap6""
      IL_01ab:  leave.s    IL_01ad
    }
    IL_01ad:  ldarg.0
    IL_01ae:  ldfld      ""int Program.<Main>d__0.<>7__wrap6""
    IL_01b3:  stloc.1
    IL_01b4:  ldloc.1
    IL_01b5:  ldc.i4.1
    IL_01b6:  bne.un     IL_0351
    IL_01bb:  ldarg.0
    IL_01bc:  ldc.i4.0
    IL_01bd:  stfld      ""int Program.<Main>d__0.<>7__wrap7""
    IL_01c2:  nop
    .try
    {
      IL_01c3:  ldloc.0
      IL_01c4:  ldc.i4.2
      IL_01c5:  beq.s      IL_0220
      IL_01c7:  ldarg.0
      IL_01c8:  ldarg.0
      IL_01c9:  ldfld      ""B Program.<Main>d__0.<b>5__3""
      IL_01ce:  stfld      ""B Program.<Main>d__0.<>7__wrap4""
      IL_01d3:  ldarg.0
      IL_01d4:  ldarg.0
      IL_01d5:  ldfld      ""A Program.<Main>d__0.<a>5__2""
      IL_01da:  ldfld      ""B A.b""
      IL_01df:  stfld      ""B Program.<Main>d__0.<>7__wrap5""
      IL_01e4:  ldstr      ""Third""
      IL_01e9:  call       ""System.Threading.Tasks.Task<int> Program.<Main>g__Write|0_0(string)""
      IL_01ee:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
      IL_01f3:  stloc.2
      IL_01f4:  ldloca.s   V_2
      IL_01f6:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
      IL_01fb:  brtrue.s   IL_023c
      IL_01fd:  ldarg.0
      IL_01fe:  ldc.i4.2
      IL_01ff:  dup
      IL_0200:  stloc.0
      IL_0201:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_0206:  ldarg.0
      IL_0207:  ldloc.2
      IL_0208:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_020d:  ldarg.0
      IL_020e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
      IL_0213:  ldloca.s   V_2
      IL_0215:  ldarg.0
      IL_0216:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
      IL_021b:  leave      IL_039b
      IL_0220:  ldarg.0
      IL_0221:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_0226:  stloc.2
      IL_0227:  ldarg.0
      IL_0228:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
      IL_022d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_0233:  ldarg.0
      IL_0234:  ldc.i4.m1
      IL_0235:  dup
      IL_0236:  stloc.0
      IL_0237:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_023c:  ldloca.s   V_2
      IL_023e:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
      IL_0243:  stloc.1
      IL_0244:  ldarg.0
      IL_0245:  ldfld      ""B Program.<Main>d__0.<>7__wrap4""
      IL_024a:  ldarg.0
      IL_024b:  ldfld      ""B Program.<Main>d__0.<>7__wrap5""
      IL_0250:  ldloc.1
      IL_0251:  dup
      IL_0252:  stloc.3
      IL_0253:  stfld      ""int B.x""
      IL_0258:  ldloc.3
      IL_0259:  stfld      ""int B.x""
      IL_025e:  ldarg.0
      IL_025f:  ldnull
      IL_0260:  stfld      ""B Program.<Main>d__0.<>7__wrap4""
      IL_0265:  ldarg.0
      IL_0266:  ldnull
      IL_0267:  stfld      ""B Program.<Main>d__0.<>7__wrap5""
      IL_026c:  leave.s    IL_0278
    }
    catch object
    {
      IL_026e:  pop
      IL_026f:  ldarg.0
      IL_0270:  ldc.i4.1
      IL_0271:  stfld      ""int Program.<Main>d__0.<>7__wrap7""
      IL_0276:  leave.s    IL_0278
    }
    IL_0278:  ldarg.0
    IL_0279:  ldfld      ""int Program.<Main>d__0.<>7__wrap7""
    IL_027e:  stloc.1
    IL_027f:  ldloc.1
    IL_0280:  ldc.i4.1
    IL_0281:  bne.un     IL_0351
    IL_0286:  ldarg.0
    IL_0287:  newobj     ""A..ctor()""
    IL_028c:  stfld      ""A Program.<Main>d__0.<a>5__2""
    IL_0291:  ldarg.0
    IL_0292:  ldfld      ""A Program.<Main>d__0.<a>5__2""
    IL_0297:  newobj     ""B..ctor()""
    IL_029c:  stfld      ""B A.b""
    IL_02a1:  ldarg.0
    IL_02a2:  newobj     ""B..ctor()""
    IL_02a7:  stfld      ""B Program.<Main>d__0.<b>5__3""
    IL_02ac:  ldarg.0
    IL_02ad:  ldarg.0
    IL_02ae:  ldfld      ""A Program.<Main>d__0.<a>5__2""
    IL_02b3:  ldfld      ""B A.b""
    IL_02b8:  stfld      ""B Program.<Main>d__0.<>7__wrap5""
    IL_02bd:  ldarg.0
    IL_02be:  ldarg.0
    IL_02bf:  ldfld      ""B Program.<Main>d__0.<b>5__3""
    IL_02c4:  stfld      ""B Program.<Main>d__0.<>7__wrap4""
    IL_02c9:  ldstr      ""Fourth""
    IL_02ce:  call       ""System.Threading.Tasks.Task<int> Program.<Main>g__Write|0_0(string)""
    IL_02d3:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_02d8:  stloc.2
    IL_02d9:  ldloca.s   V_2
    IL_02db:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_02e0:  brtrue.s   IL_0321
    IL_02e2:  ldarg.0
    IL_02e3:  ldc.i4.3
    IL_02e4:  dup
    IL_02e5:  stloc.0
    IL_02e6:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_02eb:  ldarg.0
    IL_02ec:  ldloc.2
    IL_02ed:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_02f2:  ldarg.0
    IL_02f3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_02f8:  ldloca.s   V_2
    IL_02fa:  ldarg.0
    IL_02fb:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Main>d__0)""
    IL_0300:  leave      IL_039b
    IL_0305:  ldarg.0
    IL_0306:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_030b:  stloc.2
    IL_030c:  ldarg.0
    IL_030d:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Main>d__0.<>u__1""
    IL_0312:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0318:  ldarg.0
    IL_0319:  ldc.i4.m1
    IL_031a:  dup
    IL_031b:  stloc.0
    IL_031c:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_0321:  ldloca.s   V_2
    IL_0323:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0328:  stloc.1
    IL_0329:  ldarg.0
    IL_032a:  ldfld      ""B Program.<Main>d__0.<>7__wrap5""
    IL_032f:  ldarg.0
    IL_0330:  ldfld      ""B Program.<Main>d__0.<>7__wrap4""
    IL_0335:  ldloc.1
    IL_0336:  dup
    IL_0337:  stloc.3
    IL_0338:  stfld      ""int B.x""
    IL_033d:  ldloc.3
    IL_033e:  stfld      ""int B.x""
    IL_0343:  ldarg.0
    IL_0344:  ldnull
    IL_0345:  stfld      ""B Program.<Main>d__0.<>7__wrap5""
    IL_034a:  ldarg.0
    IL_034b:  ldnull
    IL_034c:  stfld      ""B Program.<Main>d__0.<>7__wrap4""
    IL_0351:  leave.s    IL_037a
  }
  catch System.Exception
  {
    IL_0353:  stloc.s    V_4
    IL_0355:  ldarg.0
    IL_0356:  ldc.i4.s   -2
    IL_0358:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_035d:  ldarg.0
    IL_035e:  ldnull
    IL_035f:  stfld      ""A Program.<Main>d__0.<a>5__2""
    IL_0364:  ldarg.0
    IL_0365:  ldnull
    IL_0366:  stfld      ""B Program.<Main>d__0.<b>5__3""
    IL_036b:  ldarg.0
    IL_036c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_0371:  ldloc.s    V_4
    IL_0373:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0378:  leave.s    IL_039b
  }
  IL_037a:  ldarg.0
  IL_037b:  ldc.i4.s   -2
  IL_037d:  stfld      ""int Program.<Main>d__0.<>1__state""
  IL_0382:  ldarg.0
  IL_0383:  ldnull
  IL_0384:  stfld      ""A Program.<Main>d__0.<a>5__2""
  IL_0389:  ldarg.0
  IL_038a:  ldnull
  IL_038b:  stfld      ""B Program.<Main>d__0.<b>5__3""
  IL_0390:  ldarg.0
  IL_0391:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
  IL_0396:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_039b:  ret
}");
        }
    }
}
