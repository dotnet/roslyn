// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
                // (18,28): error CS8178: A reference returned by a call to 'C.P.get' cannot be preserved across 'await' or 'yield' boundary.
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

            v.VerifyMethodBody("Program.<Serialize>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      184 (0xb8)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<byte[]> V_1,
                Program.<Serialize>d__1 V_2,
                System.Exception V_3)
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Serialize>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_0047
    // sequence point: {
    IL_000e:  nop
    // sequence point: System.Text.Json.Serialization.JsonSerializer.Parse<string>(await TestAsync());
    IL_000f:  call       ""System.Threading.Tasks.Task<byte[]> Program.TestAsync()""
    IL_0014:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<byte[]> System.Threading.Tasks.Task<byte[]>.GetAwaiter()""
    IL_0019:  stloc.1
    // sequence point: <hidden>
    IL_001a:  ldloca.s   V_1
    IL_001c:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<byte[]>.IsCompleted.get""
    IL_0021:  brtrue.s   IL_0063
    IL_0023:  ldarg.0
    IL_0024:  ldc.i4.0
    IL_0025:  dup
    IL_0026:  stloc.0
    IL_0027:  stfld      ""int Program.<Serialize>d__1.<>1__state""
    // async: yield
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
    // async: resume
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
    // sequence point: <hidden>
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
  // sequence point: }
  IL_00a3:  ldarg.0
  IL_00a4:  ldc.i4.s   -2
  IL_00a6:  stfld      ""int Program.<Serialize>d__1.<>1__state""
  // sequence point: <hidden>
  IL_00ab:  ldarg.0
  IL_00ac:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Serialize>d__1.<>t__builder""
  IL_00b1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00b6:  nop
  IL_00b7:  ret
}
");
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
                // (9,17): error CS4013: Instance of type 'S' cannot be used inside a nested function, query expression, iterator block or async method
                //             Q { F: { P1: true } } when await c => r, // error: cached Q.F is alive
                Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "F").WithArguments("S").WithLocation(9, 17)
                );
            CreateCompilation(source, options: TestOptions.ReleaseDll).VerifyDiagnostics().VerifyEmitDiagnostics(
                // (9,17): error CS4013: Instance of type 'S' cannot be used inside a nested function, query expression, iterator block or async method
                //             Q { F: { P1: true } } when await c => r, // error: cached Q.F is alive
                Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "F").WithArguments("S").WithLocation(9, 17)
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
    static async Task Assign(A a)
    {
        a.B.x = await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestPropertyAccessThrows();
        await TestFieldAccessThrows();
        await TestPropertyAccessSucceeds();
    }

    static async Task TestPropertyAccessThrows()
    {
        Console.WriteLine(nameof(TestPropertyAccessThrows));
        
        A a = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestFieldAccessThrows()
    {
        Console.WriteLine(nameof(TestFieldAccessThrows));
        
        var a = new A();
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestPropertyAccessSucceeds()
    {
        Console.WriteLine(nameof(TestPropertyAccessSucceeds));

        var a = new A{ B = new B() };
        Console.WriteLine(""Before Assignment a.B.x is: "" + a.B.x);
        await Assign(a);
        Console.WriteLine(""After Assignment a.B.x is: "" + a.B.x);
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}

class A
{
    public B B { get; set; }
}

class B
{
    public int x;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"TestPropertyAccessThrows
Before Assignment
Caught NullReferenceException
TestFieldAccessThrows
Before Assignment
RHS
Caught NullReferenceException
TestPropertyAccessSucceeds
Before Assignment a.B.x is: 0
RHS
After Assignment a.B.x is: 42")
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      184 (0xb8)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0054
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""A Program.<Assign>d__0.a""
    IL_0011:  callvirt   ""B A.B.get""
    IL_0016:  stfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_001b:  ldstr      ""RHS""
    IL_0020:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_0025:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_002a:  stloc.2
    IL_002b:  ldloca.s   V_2
    IL_002d:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0032:  brtrue.s   IL_0070
    IL_0034:  ldarg.0
    IL_0035:  ldc.i4.0
    IL_0036:  dup
    IL_0037:  stloc.0
    IL_0038:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_003d:  ldarg.0
    IL_003e:  ldloc.2
    IL_003f:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0044:  ldarg.0
    IL_0045:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_004a:  ldloca.s   V_2
    IL_004c:  ldarg.0
    IL_004d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_0052:  leave.s    IL_00b7
    IL_0054:  ldarg.0
    IL_0055:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_005a:  stloc.2
    IL_005b:  ldarg.0
    IL_005c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0061:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0067:  ldarg.0
    IL_0068:  ldc.i4.m1
    IL_0069:  dup
    IL_006a:  stloc.0
    IL_006b:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0070:  ldloca.s   V_2
    IL_0072:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0077:  stloc.1
    IL_0078:  ldarg.0
    IL_0079:  ldfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_007e:  ldloc.1
    IL_007f:  stfld      ""int B.x""
    IL_0084:  ldarg.0
    IL_0085:  ldnull
    IL_0086:  stfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_008b:  leave.s    IL_00a4
  }
  catch System.Exception
  {
    IL_008d:  stloc.3
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.s   -2
    IL_0091:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0096:  ldarg.0
    IL_0097:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_009c:  ldloc.3
    IL_009d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00a2:  leave.s    IL_00b7
  }
  IL_00a4:  ldarg.0
  IL_00a5:  ldc.i4.s   -2
  IL_00a7:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00ac:  ldarg.0
  IL_00ad:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00b2:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00b7:  ret
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
    static async Task Assign(A[] arr)
    {
        arr[0].x = await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestIndexerThrows();
        await TestAssignmentThrows();
        await TestIndexerSucceeds();
        await TestReassignsArrayAndIndexerDuringAwait();
        await TestReassignsTargetDuringAwait();
    }

    static async Task TestIndexerThrows()
    {
        Console.WriteLine(nameof(TestIndexerThrows));
        
        var arr = new A[0];
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(arr);
        }
        catch (IndexOutOfRangeException)
        {
            Console.WriteLine(""Caught IndexOutOfRangeException"");
        }
    }

    static async Task TestAssignmentThrows()
    {
        Console.WriteLine(nameof(TestAssignmentThrows));
        
        var arr = new A[1];
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(arr);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestIndexerSucceeds()
    {
        Console.WriteLine(nameof(TestIndexerSucceeds));

        var arr = new A[1]{ new A() };
        Console.WriteLine(""Before Assignment arr[0].x is: "" + arr[0].x);
        await Assign(arr);
        Console.WriteLine(""After Assignment arr[0].x is: "" + arr[0].x);
    }

    static async Task TestReassignsArrayAndIndexerDuringAwait()
    {
        Console.WriteLine(nameof(TestReassignsArrayAndIndexerDuringAwait));

        var a = new A();
        var arr = new A[1]{ a };
        var index = 0;
        Console.WriteLine(""Before Assignment arr.Length is: "" + arr.Length);
        Console.WriteLine(""Before Assignment a.x is: "" + a.x);
        arr[index].x = await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment arr.Length is: "" + arr.Length);
        Console.WriteLine(""After Assignment a.x is: "" + a.x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            arr = new A[0];
            index = 1;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task TestReassignsTargetDuringAwait()
    {
        Console.WriteLine(nameof(TestReassignsTargetDuringAwait));

        var a = new A();
        var arr = new A[1]{ a };
        Console.WriteLine(""Before Assignment arr[0].x is: "" + arr[0].x);
        Console.WriteLine(""Before Assignment arr[0].y is: "" + arr[0].y);
        Console.WriteLine(""Before Assignment a.x is: "" + a.x);
        arr[0].x = await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment arr[0].x is: "" + arr[0].x);
        Console.WriteLine(""After Assignment arr[0].y is: "" + arr[0].y);
        Console.WriteLine(""After Assignment a.x is: "" + a.x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            arr[0] = new A{ y = true };
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}

class A
{
    public int x;

    public bool y;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"TestIndexerThrows
Before Assignment
Caught IndexOutOfRangeException
TestAssignmentThrows
Before Assignment
RHS
Caught NullReferenceException
TestIndexerSucceeds
Before Assignment arr[0].x is: 0
RHS
After Assignment arr[0].x is: 42
TestReassignsArrayAndIndexerDuringAwait
Before Assignment arr.Length is: 1
Before Assignment a.x is: 0
RHS
After Assignment arr.Length is: 0
After Assignment a.x is: 42
TestReassignsTargetDuringAwait
Before Assignment arr[0].x is: 0
Before Assignment arr[0].y is: False
Before Assignment a.x is: 0
RHS
After Assignment arr[0].x is: 0
After Assignment arr[0].y is: True
After Assignment a.x is: 42")
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      181 (0xb5)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0051
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""A[] Program.<Assign>d__0.arr""
    IL_0011:  ldc.i4.0
    IL_0012:  ldelem.ref
    IL_0013:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0018:  ldstr      ""RHS""
    IL_001d:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_0022:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0027:  stloc.2
    IL_0028:  ldloca.s   V_2
    IL_002a:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_002f:  brtrue.s   IL_006d
    IL_0031:  ldarg.0
    IL_0032:  ldc.i4.0
    IL_0033:  dup
    IL_0034:  stloc.0
    IL_0035:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_003a:  ldarg.0
    IL_003b:  ldloc.2
    IL_003c:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0041:  ldarg.0
    IL_0042:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0047:  ldloca.s   V_2
    IL_0049:  ldarg.0
    IL_004a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_004f:  leave.s    IL_00b4
    IL_0051:  ldarg.0
    IL_0052:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0057:  stloc.2
    IL_0058:  ldarg.0
    IL_0059:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_005e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0064:  ldarg.0
    IL_0065:  ldc.i4.m1
    IL_0066:  dup
    IL_0067:  stloc.0
    IL_0068:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_006d:  ldloca.s   V_2
    IL_006f:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0074:  stloc.1
    IL_0075:  ldarg.0
    IL_0076:  ldfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_007b:  ldloc.1
    IL_007c:  stfld      ""int A.x""
    IL_0081:  ldarg.0
    IL_0082:  ldnull
    IL_0083:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0088:  leave.s    IL_00a1
  }
  catch System.Exception
  {
    IL_008a:  stloc.3
    IL_008b:  ldarg.0
    IL_008c:  ldc.i4.s   -2
    IL_008e:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0093:  ldarg.0
    IL_0094:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0099:  ldloc.3
    IL_009a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_009f:  leave.s    IL_00b4
  }
  IL_00a1:  ldarg.0
  IL_00a2:  ldc.i4.s   -2
  IL_00a4:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00a9:  ldarg.0
  IL_00aa:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00af:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00b4:  ret
}");
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void KeepLtrSemantics_StructFieldAccessOnArray()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign(A[] arr)
    {
        arr[0].x = await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestIndexerThrows();
        await TestIndexerSucceeds();
        await TestReassignsArrayAndIndexerDuringAwait();
        await TestReassignsTargetDuringAwait();
    }

    static async Task TestIndexerThrows()
    {
        Console.WriteLine(nameof(TestIndexerThrows));
        
        var arr = new A[0];
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(arr);
        }
        catch (IndexOutOfRangeException)
        {
            Console.WriteLine(""Caught IndexOutOfRangeException"");
        }
    }

    static async Task TestIndexerSucceeds()
    {
        Console.WriteLine(nameof(TestIndexerSucceeds));

        var arr = new A[1];
        Console.WriteLine(""Before Assignment arr[0].x is: "" + arr[0].x);
        await Assign(arr);
        Console.WriteLine(""After Assignment arr[0].x is: "" + arr[0].x);
    }

    static async Task TestReassignsArrayAndIndexerDuringAwait()
    {
        Console.WriteLine(nameof(TestReassignsArrayAndIndexerDuringAwait));

        var arr = new A[1];
        var arrCopy = arr;
        var index = 0;
        Console.WriteLine(""Before Assignment arr.Length is: "" + arr.Length);
        Console.WriteLine(""Before Assignment arrCopy[0].x is: "" + arrCopy[0].x);
        arr[index].x = await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment arr.Length is: "" + arr.Length);
        Console.WriteLine(""After Assignment arrCopy[0].x is: "" + arrCopy[0].x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            arr = new A[0];
            index = 1;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task TestReassignsTargetDuringAwait()
    {
        Console.WriteLine(nameof(TestReassignsTargetDuringAwait));

        var arr = new A[1];
        Console.WriteLine(""Before Assignment arr[0].x is: "" + arr[0].x);
        Console.WriteLine(""Before Assignment arr[0].y is: "" + arr[0].y);
        arr[0].x = await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment arr[0].x is: "" + arr[0].x);
        Console.WriteLine(""Before Assignment arr[0].y is: "" + arr[0].y);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            arr[0] = new A{y = true };
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}

struct A
{
    public int x;

    public bool y;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"TestIndexerThrows
Before Assignment
Caught IndexOutOfRangeException
TestIndexerSucceeds
Before Assignment arr[0].x is: 0
RHS
After Assignment arr[0].x is: 42
TestReassignsArrayAndIndexerDuringAwait
Before Assignment arr.Length is: 1
Before Assignment arrCopy[0].x is: 0
RHS
After Assignment arr.Length is: 0
After Assignment arrCopy[0].x is: 42
TestReassignsTargetDuringAwait
Before Assignment arr[0].x is: 0
Before Assignment arr[0].y is: False
RHS
After Assignment arr[0].x is: 42
Before Assignment arr[0].y is: True")
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      198 (0xc6)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_005c
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""A[] Program.<Assign>d__0.arr""
    IL_0011:  stfld      ""A[] Program.<Assign>d__0.<>7__wrap1""
    IL_0016:  ldarg.0
    IL_0017:  ldfld      ""A[] Program.<Assign>d__0.<>7__wrap1""
    IL_001c:  ldc.i4.0
    IL_001d:  ldelema    ""A""
    IL_0022:  pop
    IL_0023:  ldstr      ""RHS""
    IL_0028:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_002d:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0032:  stloc.2
    IL_0033:  ldloca.s   V_2
    IL_0035:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_003a:  brtrue.s   IL_0078
    IL_003c:  ldarg.0
    IL_003d:  ldc.i4.0
    IL_003e:  dup
    IL_003f:  stloc.0
    IL_0040:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0045:  ldarg.0
    IL_0046:  ldloc.2
    IL_0047:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_004c:  ldarg.0
    IL_004d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0052:  ldloca.s   V_2
    IL_0054:  ldarg.0
    IL_0055:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_005a:  leave.s    IL_00c5
    IL_005c:  ldarg.0
    IL_005d:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0062:  stloc.2
    IL_0063:  ldarg.0
    IL_0064:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0069:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_006f:  ldarg.0
    IL_0070:  ldc.i4.m1
    IL_0071:  dup
    IL_0072:  stloc.0
    IL_0073:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0078:  ldloca.s   V_2
    IL_007a:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_007f:  stloc.1
    IL_0080:  ldarg.0
    IL_0081:  ldfld      ""A[] Program.<Assign>d__0.<>7__wrap1""
    IL_0086:  ldc.i4.0
    IL_0087:  ldelema    ""A""
    IL_008c:  ldloc.1
    IL_008d:  stfld      ""int A.x""
    IL_0092:  ldarg.0
    IL_0093:  ldnull
    IL_0094:  stfld      ""A[] Program.<Assign>d__0.<>7__wrap1""
    IL_0099:  leave.s    IL_00b2
  }
  catch System.Exception
  {
    IL_009b:  stloc.3
    IL_009c:  ldarg.0
    IL_009d:  ldc.i4.s   -2
    IL_009f:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_00a4:  ldarg.0
    IL_00a5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_00aa:  ldloc.3
    IL_00ab:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00b0:  leave.s    IL_00c5
  }
  IL_00b2:  ldarg.0
  IL_00b3:  ldc.i4.s   -2
  IL_00b5:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00ba:  ldarg.0
  IL_00bb:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00c0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c5:  ret
}");
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void KeepLtrSemantics_AssignmentToArray()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign(int[] arr)
    {
        arr[0] = await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestIndexerThrows();
        await TestIndexerSucceeds();
        await TestReassignsArrayAndIndexerDuringAwait();
    }

    static async Task TestIndexerThrows()
    {
        Console.WriteLine(nameof(TestIndexerThrows));
        
        var arr = new int[0];
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(arr);
        }
        catch (IndexOutOfRangeException)
        {
            Console.WriteLine(""Caught IndexOutOfRangeException"");
        }
    }

    static async Task TestIndexerSucceeds()
    {
        Console.WriteLine(nameof(TestIndexerSucceeds));

        var arr = new int[1];
        Console.WriteLine(""Before Assignment arr[0] is: "" + arr[0]);
        await Assign(arr);
        Console.WriteLine(""After Assignment arr[0] is: "" + arr[0]);
    }

    static async Task TestReassignsArrayAndIndexerDuringAwait()
    {
        Console.WriteLine(nameof(TestReassignsArrayAndIndexerDuringAwait));

        var arr = new int[1];
        var arrCopy = arr;
        var index = 0;
        Console.WriteLine(""Before Assignment arr.Length is: "" + arr.Length);
        Console.WriteLine(""Before Assignment arrCopy[0] is: "" + arrCopy[0]);
        arr[index] = await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment arr.Length is: "" + arr.Length);
        Console.WriteLine(""After Assignment arrCopy[0] is: "" + arrCopy[0]);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            arr = new int[0];
            index = 1;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"TestIndexerThrows
Before Assignment
RHS
Caught IndexOutOfRangeException
TestIndexerSucceeds
Before Assignment arr[0] is: 0
RHS
After Assignment arr[0] is: 42
TestReassignsArrayAndIndexerDuringAwait
Before Assignment arr.Length is: 1
Before Assignment arrCopy[0] is: 0
RHS
After Assignment arr.Length is: 0
After Assignment arrCopy[0] is: 42")
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      176 (0xb0)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004f
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""int[] Program.<Assign>d__0.arr""
    IL_0011:  stfld      ""int[] Program.<Assign>d__0.<>7__wrap1""
    IL_0016:  ldstr      ""RHS""
    IL_001b:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_0020:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0025:  stloc.2
    IL_0026:  ldloca.s   V_2
    IL_0028:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_002d:  brtrue.s   IL_006b
    IL_002f:  ldarg.0
    IL_0030:  ldc.i4.0
    IL_0031:  dup
    IL_0032:  stloc.0
    IL_0033:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0038:  ldarg.0
    IL_0039:  ldloc.2
    IL_003a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_003f:  ldarg.0
    IL_0040:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0045:  ldloca.s   V_2
    IL_0047:  ldarg.0
    IL_0048:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_004d:  leave.s    IL_00af
    IL_004f:  ldarg.0
    IL_0050:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0055:  stloc.2
    IL_0056:  ldarg.0
    IL_0057:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_005c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0062:  ldarg.0
    IL_0063:  ldc.i4.m1
    IL_0064:  dup
    IL_0065:  stloc.0
    IL_0066:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_006b:  ldloca.s   V_2
    IL_006d:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0072:  stloc.1
    IL_0073:  ldarg.0
    IL_0074:  ldfld      ""int[] Program.<Assign>d__0.<>7__wrap1""
    IL_0079:  ldc.i4.0
    IL_007a:  ldloc.1
    IL_007b:  stelem.i4
    IL_007c:  ldarg.0
    IL_007d:  ldnull
    IL_007e:  stfld      ""int[] Program.<Assign>d__0.<>7__wrap1""
    IL_0083:  leave.s    IL_009c
  }
  catch System.Exception
  {
    IL_0085:  stloc.3
    IL_0086:  ldarg.0
    IL_0087:  ldc.i4.s   -2
    IL_0089:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_008e:  ldarg.0
    IL_008f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0094:  ldloc.3
    IL_0095:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_009a:  leave.s    IL_00af
  }
  IL_009c:  ldarg.0
  IL_009d:  ldc.i4.s   -2
  IL_009f:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00a4:  ldarg.0
  IL_00a5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00aa:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00af:  ret
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
    static async Task Assign(A a)
    {
        a.b.c.x = await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestAIsNull();
        await TestAIsNotNull();
        await ReassignADuringAssignment();
    }

    static async Task TestAIsNull()
    {
        Console.WriteLine(nameof(TestAIsNull));
        
        A a = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestAIsNotNull()
    {
        Console.WriteLine(nameof(TestAIsNotNull));

        var a = new A();
        Console.WriteLine(""Before Assignment a.b.c.x is: "" + a.b.c.x);
        await Assign(a);
        Console.WriteLine(""After Assignment a.b.c.x is: "" + a.b.c.x);
    }

    static async Task ReassignADuringAssignment()
    {
        Console.WriteLine(nameof(ReassignADuringAssignment));

        var a = new A();
        var aCopy = a;
        Console.WriteLine(""Before Assignment a is null == "" + (a is null));
        Console.WriteLine(""Before Assignment aCopy.b.c.x is: "" + aCopy.b.c.x);
        a.b.c.x = await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment a is null == "" + (a is null));
        Console.WriteLine(""After Assignment aCopy.b.c.x is: "" + aCopy.b.c.x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            a = null;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
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
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"TestAIsNull
Before Assignment
Caught NullReferenceException
TestAIsNotNull
Before Assignment a.b.c.x is: 0
RHS
After Assignment a.b.c.x is: 42
ReassignADuringAssignment
Before Assignment a is null == False
Before Assignment aCopy.b.c.x is: 0
RHS
After Assignment a is null == True
After Assignment aCopy.b.c.x is: 42")
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      201 (0xc9)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_005b
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""A Program.<Assign>d__0.a""
    IL_0011:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0016:  ldarg.0
    IL_0017:  ldfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_001c:  ldfld      ""B A.b""
    IL_0021:  pop
    IL_0022:  ldstr      ""RHS""
    IL_0027:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_002c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0031:  stloc.2
    IL_0032:  ldloca.s   V_2
    IL_0034:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0039:  brtrue.s   IL_0077
    IL_003b:  ldarg.0
    IL_003c:  ldc.i4.0
    IL_003d:  dup
    IL_003e:  stloc.0
    IL_003f:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0044:  ldarg.0
    IL_0045:  ldloc.2
    IL_0046:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_004b:  ldarg.0
    IL_004c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0051:  ldloca.s   V_2
    IL_0053:  ldarg.0
    IL_0054:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_0059:  leave.s    IL_00c8
    IL_005b:  ldarg.0
    IL_005c:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0061:  stloc.2
    IL_0062:  ldarg.0
    IL_0063:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0068:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_006e:  ldarg.0
    IL_006f:  ldc.i4.m1
    IL_0070:  dup
    IL_0071:  stloc.0
    IL_0072:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0077:  ldloca.s   V_2
    IL_0079:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_007e:  stloc.1
    IL_007f:  ldarg.0
    IL_0080:  ldfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0085:  ldflda     ""B A.b""
    IL_008a:  ldflda     ""C B.c""
    IL_008f:  ldloc.1
    IL_0090:  stfld      ""int C.x""
    IL_0095:  ldarg.0
    IL_0096:  ldnull
    IL_0097:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_009c:  leave.s    IL_00b5
  }
  catch System.Exception
  {
    IL_009e:  stloc.3
    IL_009f:  ldarg.0
    IL_00a0:  ldc.i4.s   -2
    IL_00a2:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_00ad:  ldloc.3
    IL_00ae:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00b3:  leave.s    IL_00c8
  }
  IL_00b5:  ldarg.0
  IL_00b6:  ldc.i4.s   -2
  IL_00b8:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00bd:  ldarg.0
  IL_00be:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00c3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c8:  ret
}");
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void KeepLtrSemantics_ClassPropertyAssignmentOnClassProperty()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign(A a)
    {
        a.b.x = await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestAIsNull();
        await TestAIsNotNull();
        await ReassignADuringAssignment();
    }

    static async Task TestAIsNull()
    {
        Console.WriteLine(nameof(TestAIsNull));
        
        A a = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestAIsNotNull()
    {
        Console.WriteLine(nameof(TestAIsNotNull));

        var a = new A{ _b = new B() };
        Console.WriteLine(""Before Assignment a._b._x is: "" + a._b._x);
        await Assign(a);
        Console.WriteLine(""After Assignment a._b._x is: "" + a._b._x);
    }

    static async Task ReassignADuringAssignment()
    {
        Console.WriteLine(nameof(ReassignADuringAssignment));

        var a = new A{ _b = new B() };
        var aCopy = a;
        Console.WriteLine(""Before Assignment a is null == "" + (a is null));
        Console.WriteLine(""Before Assignment aCopy._b._x is: "" + aCopy._b._x);
        a.b.x = await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment a is null == "" + (a is null));
        Console.WriteLine(""After Assignment aCopy._b._x is: "" + aCopy._b._x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            a = null;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}

class A
{
    public B _b;
    public B b { get { Console.WriteLine(""GetB""); return _b; } set { Console.WriteLine(""SetB""); _b = value; }}
}

class B
{
    public int _x;
    public int x { get { Console.WriteLine(""GetX""); return _x; } set { Console.WriteLine(""SetX""); _x = value; } }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"TestAIsNull
Before Assignment
Caught NullReferenceException
TestAIsNotNull
Before Assignment a._b._x is: 0
GetB
RHS
SetX
After Assignment a._b._x is: 42
ReassignADuringAssignment
Before Assignment a is null == False
Before Assignment aCopy._b._x is: 0
GetB
RHS
SetX
After Assignment a is null == True
After Assignment aCopy._b._x is: 42")
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      184 (0xb8)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0054
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""A Program.<Assign>d__0.a""
    IL_0011:  callvirt   ""B A.b.get""
    IL_0016:  stfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_001b:  ldstr      ""RHS""
    IL_0020:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_0025:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_002a:  stloc.2
    IL_002b:  ldloca.s   V_2
    IL_002d:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0032:  brtrue.s   IL_0070
    IL_0034:  ldarg.0
    IL_0035:  ldc.i4.0
    IL_0036:  dup
    IL_0037:  stloc.0
    IL_0038:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_003d:  ldarg.0
    IL_003e:  ldloc.2
    IL_003f:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0044:  ldarg.0
    IL_0045:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_004a:  ldloca.s   V_2
    IL_004c:  ldarg.0
    IL_004d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_0052:  leave.s    IL_00b7
    IL_0054:  ldarg.0
    IL_0055:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_005a:  stloc.2
    IL_005b:  ldarg.0
    IL_005c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0061:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0067:  ldarg.0
    IL_0068:  ldc.i4.m1
    IL_0069:  dup
    IL_006a:  stloc.0
    IL_006b:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0070:  ldloca.s   V_2
    IL_0072:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0077:  stloc.1
    IL_0078:  ldarg.0
    IL_0079:  ldfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_007e:  ldloc.1
    IL_007f:  callvirt   ""void B.x.set""
    IL_0084:  ldarg.0
    IL_0085:  ldnull
    IL_0086:  stfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_008b:  leave.s    IL_00a4
  }
  catch System.Exception
  {
    IL_008d:  stloc.3
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.s   -2
    IL_0091:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0096:  ldarg.0
    IL_0097:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_009c:  ldloc.3
    IL_009d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00a2:  leave.s    IL_00b7
  }
  IL_00a4:  ldarg.0
  IL_00a5:  ldc.i4.s   -2
  IL_00a7:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00ac:  ldarg.0
  IL_00ad:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00b2:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00b7:  ret
}");
        }

        [WorkItem(19609, "https://github.com/dotnet/roslyn/issues/19609")]
        [Fact]
        public void KeepLtrSemantics_FieldAccessOnClass()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign(A a)
    {
        a.x = await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestAIsNull();
        await TestAIsNotNull();
        await ReassignADuringAssignment();
    }

    static async Task TestAIsNull()
    {
        Console.WriteLine(nameof(TestAIsNull));
        
        A a = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestAIsNotNull()
    {
        Console.WriteLine(nameof(TestAIsNotNull));

        var a = new A();
        Console.WriteLine(""Before Assignment a.x is: "" + a.x);
        await Assign(a);
        Console.WriteLine(""After Assignment a.x is: "" + a.x);
    }

    static async Task ReassignADuringAssignment()
    {
        Console.WriteLine(nameof(ReassignADuringAssignment));

        var a = new A();
        var aCopy = a;
        Console.WriteLine(""Before Assignment a is null == "" + (a is null));
        Console.WriteLine(""Before Assignment aCopy.x is: "" + aCopy.x);
        a.x = await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment a is null == "" + (a is null));
        Console.WriteLine(""After Assignment aCopy.x is: "" + aCopy.x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            a = null;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}

class A
{
    public int x;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"TestAIsNull
Before Assignment
RHS
Caught NullReferenceException
TestAIsNotNull
Before Assignment a.x is: 0
RHS
After Assignment a.x is: 42
ReassignADuringAssignment
Before Assignment a is null == False
Before Assignment aCopy.x is: 0
RHS
After Assignment a is null == True
After Assignment aCopy.x is: 42")
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      179 (0xb3)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004f
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""A Program.<Assign>d__0.a""
    IL_0011:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0016:  ldstr      ""RHS""
    IL_001b:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_0020:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0025:  stloc.2
    IL_0026:  ldloca.s   V_2
    IL_0028:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_002d:  brtrue.s   IL_006b
    IL_002f:  ldarg.0
    IL_0030:  ldc.i4.0
    IL_0031:  dup
    IL_0032:  stloc.0
    IL_0033:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0038:  ldarg.0
    IL_0039:  ldloc.2
    IL_003a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_003f:  ldarg.0
    IL_0040:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0045:  ldloca.s   V_2
    IL_0047:  ldarg.0
    IL_0048:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_004d:  leave.s    IL_00b2
    IL_004f:  ldarg.0
    IL_0050:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0055:  stloc.2
    IL_0056:  ldarg.0
    IL_0057:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_005c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0062:  ldarg.0
    IL_0063:  ldc.i4.m1
    IL_0064:  dup
    IL_0065:  stloc.0
    IL_0066:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_006b:  ldloca.s   V_2
    IL_006d:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0072:  stloc.1
    IL_0073:  ldarg.0
    IL_0074:  ldfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0079:  ldloc.1
    IL_007a:  stfld      ""int A.x""
    IL_007f:  ldarg.0
    IL_0080:  ldnull
    IL_0081:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0086:  leave.s    IL_009f
  }
  catch System.Exception
  {
    IL_0088:  stloc.3
    IL_0089:  ldarg.0
    IL_008a:  ldc.i4.s   -2
    IL_008c:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0091:  ldarg.0
    IL_0092:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0097:  ldloc.3
    IL_0098:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_009d:  leave.s    IL_00b2
  }
  IL_009f:  ldarg.0
  IL_00a0:  ldc.i4.s   -2
  IL_00a2:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00a7:  ldarg.0
  IL_00a8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00ad:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00b2:  ret
}");
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void KeepLtrSemantics_CompoundAssignment()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign(A a)
    {
        a.x += await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestAIsNull();
        await TestAIsNotNull();
        await ReassignADuringAssignment();
        await ReassignXDuringAssignment();
    }

    static async Task TestAIsNull()
    {
        Console.WriteLine(nameof(TestAIsNull));
        
        A a = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestAIsNotNull()
    {
        Console.WriteLine(nameof(TestAIsNotNull));

        var a = new A(){ x = 1 };
        Console.WriteLine(""Before Assignment a.x is: "" + a.x);
        await Assign(a);
        Console.WriteLine(""After Assignment a.x is: "" + a.x);
    }

    static async Task ReassignADuringAssignment()
    {
        Console.WriteLine(nameof(ReassignADuringAssignment));

        var a = new A(){ x = 1 };
        var aCopy = a;
        Console.WriteLine(""Before Assignment a is null == "" + (a is null));
        Console.WriteLine(""Before Assignment aCopy.x is: "" + aCopy.x);
        a.x += await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment a is null == "" + (a is null));
        Console.WriteLine(""After Assignment aCopy.x is: "" + aCopy.x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            a = null;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task ReassignXDuringAssignment()
    {
        Console.WriteLine(nameof(ReassignXDuringAssignment));

        var a = new A(){ x = 1 };
        Console.WriteLine(""Before Assignment a.x is: "" + a.x);
        a.x += await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment a.x is: "" + a.x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            a.x = 100;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}

class A
{
    public int x;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"TestAIsNull
Before Assignment
Caught NullReferenceException
TestAIsNotNull
Before Assignment a.x is: 1
RHS
After Assignment a.x is: 43
ReassignADuringAssignment
Before Assignment a is null == False
Before Assignment aCopy.x is: 1
RHS
After Assignment a is null == True
After Assignment aCopy.x is: 43
ReassignXDuringAssignment
Before Assignment a.x is: 1
RHS
After Assignment a.x is: 43")
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      202 (0xca)
  .maxstack  3
  .locals init (int V_0,
                A V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_005d
    IL_000a:  ldarg.0
    IL_000b:  ldfld      ""A Program.<Assign>d__0.a""
    IL_0010:  stloc.1
    IL_0011:  ldarg.0
    IL_0012:  ldloc.1
    IL_0013:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0018:  ldarg.0
    IL_0019:  ldloc.1
    IL_001a:  ldfld      ""int A.x""
    IL_001f:  stfld      ""int Program.<Assign>d__0.<>7__wrap2""
    IL_0024:  ldstr      ""RHS""
    IL_0029:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_002e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0033:  stloc.3
    IL_0034:  ldloca.s   V_3
    IL_0036:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_003b:  brtrue.s   IL_0079
    IL_003d:  ldarg.0
    IL_003e:  ldc.i4.0
    IL_003f:  dup
    IL_0040:  stloc.0
    IL_0041:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0046:  ldarg.0
    IL_0047:  ldloc.3
    IL_0048:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_004d:  ldarg.0
    IL_004e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0053:  ldloca.s   V_3
    IL_0055:  ldarg.0
    IL_0056:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_005b:  leave.s    IL_00c9
    IL_005d:  ldarg.0
    IL_005e:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0063:  stloc.3
    IL_0064:  ldarg.0
    IL_0065:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_006a:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0070:  ldarg.0
    IL_0071:  ldc.i4.m1
    IL_0072:  dup
    IL_0073:  stloc.0
    IL_0074:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0079:  ldloca.s   V_3
    IL_007b:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0080:  stloc.2
    IL_0081:  ldarg.0
    IL_0082:  ldfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0087:  ldarg.0
    IL_0088:  ldfld      ""int Program.<Assign>d__0.<>7__wrap2""
    IL_008d:  ldloc.2
    IL_008e:  add
    IL_008f:  stfld      ""int A.x""
    IL_0094:  ldarg.0
    IL_0095:  ldnull
    IL_0096:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_009b:  leave.s    IL_00b6
  }
  catch System.Exception
  {
    IL_009d:  stloc.s    V_4
    IL_009f:  ldarg.0
    IL_00a0:  ldc.i4.s   -2
    IL_00a2:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_00ad:  ldloc.s    V_4
    IL_00af:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00b4:  leave.s    IL_00c9
  }
  IL_00b6:  ldarg.0
  IL_00b7:  ldc.i4.s   -2
  IL_00b9:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00be:  ldarg.0
  IL_00bf:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00c4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c9:  ret
}");
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void KeepLtrSemantics_CompoundAssignmentProperties()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign(A a)
    {
        a.x += await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestAIsNull();
        await TestAIsNotNull();
        await ReassignADuringAssignment();
        await ReassignXDuringAssignment();
    }

    static async Task TestAIsNull()
    {
        Console.WriteLine(nameof(TestAIsNull));
        
        A a = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestAIsNotNull()
    {
        Console.WriteLine(nameof(TestAIsNotNull));

        var a = new A(){ _x = 1 };
        Console.WriteLine(""Before Assignment a._x is: "" + a._x);
        await Assign(a);
        Console.WriteLine(""After Assignment a._x is: "" + a._x);
    }

    static async Task ReassignADuringAssignment()
    {
        Console.WriteLine(nameof(ReassignADuringAssignment));

        var a = new A(){ _x = 1 };
        var aCopy = a;
        Console.WriteLine(""Before Assignment a is null == "" + (a is null));
        Console.WriteLine(""Before Assignment aCopy._x is: "" + aCopy._x);
        a.x += await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment a is null == "" + (a is null));
        Console.WriteLine(""After Assignment aCopy._x is: "" + aCopy._x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            a = null;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task ReassignXDuringAssignment()
    {
        Console.WriteLine(nameof(ReassignXDuringAssignment));

        var a = new A(){ _x = 1 };
        Console.WriteLine(""Before Assignment a._x is: "" + a._x);
        a.x += await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment a._x is: "" + a._x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            a._x = 100;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}

class A
{
    public int _x;
    public int x { get { Console.WriteLine(""GetX""); return _x; } set { Console.WriteLine(""SetX""); _x = value; } }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"TestAIsNull
Before Assignment
Caught NullReferenceException
TestAIsNotNull
Before Assignment a._x is: 1
GetX
RHS
SetX
After Assignment a._x is: 43
ReassignADuringAssignment
Before Assignment a is null == False
Before Assignment aCopy._x is: 1
GetX
RHS
SetX
After Assignment a is null == True
After Assignment aCopy._x is: 43
ReassignXDuringAssignment
Before Assignment a._x is: 1
GetX
RHS
SetX
After Assignment a._x is: 43")
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      202 (0xca)
  .maxstack  3
  .locals init (int V_0,
                A V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_005d
    IL_000a:  ldarg.0
    IL_000b:  ldfld      ""A Program.<Assign>d__0.a""
    IL_0010:  stloc.1
    IL_0011:  ldarg.0
    IL_0012:  ldloc.1
    IL_0013:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0018:  ldarg.0
    IL_0019:  ldloc.1
    IL_001a:  callvirt   ""int A.x.get""
    IL_001f:  stfld      ""int Program.<Assign>d__0.<>7__wrap2""
    IL_0024:  ldstr      ""RHS""
    IL_0029:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_002e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0033:  stloc.3
    IL_0034:  ldloca.s   V_3
    IL_0036:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_003b:  brtrue.s   IL_0079
    IL_003d:  ldarg.0
    IL_003e:  ldc.i4.0
    IL_003f:  dup
    IL_0040:  stloc.0
    IL_0041:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0046:  ldarg.0
    IL_0047:  ldloc.3
    IL_0048:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_004d:  ldarg.0
    IL_004e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0053:  ldloca.s   V_3
    IL_0055:  ldarg.0
    IL_0056:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_005b:  leave.s    IL_00c9
    IL_005d:  ldarg.0
    IL_005e:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0063:  stloc.3
    IL_0064:  ldarg.0
    IL_0065:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_006a:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0070:  ldarg.0
    IL_0071:  ldc.i4.m1
    IL_0072:  dup
    IL_0073:  stloc.0
    IL_0074:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0079:  ldloca.s   V_3
    IL_007b:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0080:  stloc.2
    IL_0081:  ldarg.0
    IL_0082:  ldfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0087:  ldarg.0
    IL_0088:  ldfld      ""int Program.<Assign>d__0.<>7__wrap2""
    IL_008d:  ldloc.2
    IL_008e:  add
    IL_008f:  callvirt   ""void A.x.set""
    IL_0094:  ldarg.0
    IL_0095:  ldnull
    IL_0096:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_009b:  leave.s    IL_00b6
  }
  catch System.Exception
  {
    IL_009d:  stloc.s    V_4
    IL_009f:  ldarg.0
    IL_00a0:  ldc.i4.s   -2
    IL_00a2:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_00ad:  ldloc.s    V_4
    IL_00af:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00b4:  leave.s    IL_00c9
  }
  IL_00b6:  ldarg.0
  IL_00b7:  ldc.i4.s   -2
  IL_00b9:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00be:  ldarg.0
  IL_00bf:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00c4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c9:  ret
}");
        }

        [WorkItem(19609, "https://github.com/dotnet/roslyn/issues/19609")]
        [Fact]
        public void KeepLtrSemantics_AssignmentToAssignment()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign(A a, B b)
    {
        a.b.x = b.x = await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestAIsNullBIsNull();
        await TestAIsNullBIsNotNull();
        await TestAIsNotNullBIsNull();
        await TestADotBIsNullBIsNotNull();
        await TestADotBIsNotNullBIsNotNull();
    }

    static async Task TestAIsNullBIsNull()
    {
        Console.WriteLine(nameof(TestAIsNullBIsNull));
        
        A a = null;
        B b = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a, b);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestAIsNullBIsNotNull()
    {
        Console.WriteLine(nameof(TestAIsNullBIsNotNull));
        
        A a = null;
        B b = new B();
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a, b);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestAIsNotNullBIsNull()
    {
        Console.WriteLine(nameof(TestAIsNotNullBIsNull));
        
        A a = new A{ b = new B() };
        B b = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a, b);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestADotBIsNullBIsNotNull()
    {
        Console.WriteLine(nameof(TestADotBIsNullBIsNotNull));
        
        A a = new A();
        B b = new B();
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a, b);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestADotBIsNotNullBIsNotNull()
    {
        Console.WriteLine(nameof(TestADotBIsNotNullBIsNotNull));

        A a = new A{ b = new B() };
        B b = new B();
        Console.WriteLine(""Before Assignment a.b.x is: "" + a.b.x);
        Console.WriteLine(""Before Assignment b.x is: "" + b.x);
        await Assign(a, b);
        Console.WriteLine(""After Assignment a.b.x is: "" + a.b.x);
        Console.WriteLine(""After Assignment b.x is: "" + b.x);
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
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
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"TestAIsNullBIsNull
Before Assignment
Caught NullReferenceException
TestAIsNullBIsNotNull
Before Assignment
Caught NullReferenceException
TestAIsNotNullBIsNull
Before Assignment
RHS
Caught NullReferenceException
TestADotBIsNullBIsNotNull
Before Assignment
RHS
Caught NullReferenceException
TestADotBIsNotNullBIsNotNull
Before Assignment a.b.x is: 0
Before Assignment b.x is: 0
RHS
After Assignment a.b.x is: 42
After Assignment b.x is: 42")
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      219 (0xdb)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                int V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0060
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""A Program.<Assign>d__0.a""
    IL_0011:  ldfld      ""B A.b""
    IL_0016:  stfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_001b:  ldarg.0
    IL_001c:  ldarg.0
    IL_001d:  ldfld      ""B Program.<Assign>d__0.b""
    IL_0022:  stfld      ""B Program.<Assign>d__0.<>7__wrap2""
    IL_0027:  ldstr      ""RHS""
    IL_002c:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_0031:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0036:  stloc.2
    IL_0037:  ldloca.s   V_2
    IL_0039:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_003e:  brtrue.s   IL_007c
    IL_0040:  ldarg.0
    IL_0041:  ldc.i4.0
    IL_0042:  dup
    IL_0043:  stloc.0
    IL_0044:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0049:  ldarg.0
    IL_004a:  ldloc.2
    IL_004b:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0050:  ldarg.0
    IL_0051:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0056:  ldloca.s   V_2
    IL_0058:  ldarg.0
    IL_0059:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_005e:  leave.s    IL_00da
    IL_0060:  ldarg.0
    IL_0061:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0066:  stloc.2
    IL_0067:  ldarg.0
    IL_0068:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_006d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0073:  ldarg.0
    IL_0074:  ldc.i4.m1
    IL_0075:  dup
    IL_0076:  stloc.0
    IL_0077:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_007c:  ldloca.s   V_2
    IL_007e:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0083:  stloc.1
    IL_0084:  ldarg.0
    IL_0085:  ldfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_008a:  ldarg.0
    IL_008b:  ldfld      ""B Program.<Assign>d__0.<>7__wrap2""
    IL_0090:  ldloc.1
    IL_0091:  dup
    IL_0092:  stloc.3
    IL_0093:  stfld      ""int B.x""
    IL_0098:  ldloc.3
    IL_0099:  stfld      ""int B.x""
    IL_009e:  ldarg.0
    IL_009f:  ldnull
    IL_00a0:  stfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_00a5:  ldarg.0
    IL_00a6:  ldnull
    IL_00a7:  stfld      ""B Program.<Assign>d__0.<>7__wrap2""
    IL_00ac:  leave.s    IL_00c7
  }
  catch System.Exception
  {
    IL_00ae:  stloc.s    V_4
    IL_00b0:  ldarg.0
    IL_00b1:  ldc.i4.s   -2
    IL_00b3:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_00b8:  ldarg.0
    IL_00b9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_00be:  ldloc.s    V_4
    IL_00c0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00c5:  leave.s    IL_00da
  }
  IL_00c7:  ldarg.0
  IL_00c8:  ldc.i4.s   -2
  IL_00ca:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00cf:  ldarg.0
  IL_00d0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00d5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00da:  ret
}");
        }

        [WorkItem(19609, "https://github.com/dotnet/roslyn/issues/19609")]
        [Fact]
        public void KeepLtrSemantics_AssignmentToAssignmentProperties()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign(A a, B b)
    {
        a.b.x = b.x = await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestAIsNullBIsNull();
        await TestAIsNullBIsNotNull();
        await TestAIsNotNullBIsNull();
        await TestADotBIsNullBIsNotNull();
        await TestADotBIsNotNullBIsNotNull();
    }

    static async Task TestAIsNullBIsNull()
    {
        Console.WriteLine(nameof(TestAIsNullBIsNull));
        
        A a = null;
        B b = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a, b);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestAIsNullBIsNotNull()
    {
        Console.WriteLine(nameof(TestAIsNullBIsNotNull));
        
        A a = null;
        B b = new B();
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a, b);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestAIsNotNullBIsNull()
    {
        Console.WriteLine(nameof(TestAIsNotNullBIsNull));
        
        A a = new A{ _b = new B() };
        B b = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a, b);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestADotBIsNullBIsNotNull()
    {
        Console.WriteLine(nameof(TestADotBIsNullBIsNotNull));
        
        A a = new A();
        B b = new B();
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a, b);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestADotBIsNotNullBIsNotNull()
    {
        Console.WriteLine(nameof(TestADotBIsNotNullBIsNotNull));

        A a = new A{ _b = new B() };
        B b = new B();
        Console.WriteLine(""Before Assignment a._b._x is: "" + a._b._x);
        Console.WriteLine(""Before Assignment b._x is: "" + b._x);
        await Assign(a, b);
        Console.WriteLine(""After Assignment a._b._x is: "" + a._b._x);
        Console.WriteLine(""After Assignment b._x is: "" + b._x);
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}

class A
{
    public B _b;
    public B b { get { Console.WriteLine(""GetB""); return _b; } set { Console.WriteLine(""SetB""); _b = value; }}
}

class B
{
    public int _x;
    public int x {  get { Console.WriteLine(""GetX""); return _x; } set { Console.WriteLine(""SetX""); _x = value; } }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"TestAIsNullBIsNull
Before Assignment
Caught NullReferenceException
TestAIsNullBIsNotNull
Before Assignment
Caught NullReferenceException
TestAIsNotNullBIsNull
Before Assignment
GetB
RHS
Caught NullReferenceException
TestADotBIsNullBIsNotNull
Before Assignment
GetB
RHS
SetX
Caught NullReferenceException
TestADotBIsNotNullBIsNotNull
Before Assignment a._b._x is: 0
Before Assignment b._x is: 0
GetB
RHS
SetX
SetX
After Assignment a._b._x is: 42
After Assignment b._x is: 42")
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      219 (0xdb)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0060
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""A Program.<Assign>d__0.a""
    IL_0011:  callvirt   ""B A.b.get""
    IL_0016:  stfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_001b:  ldarg.0
    IL_001c:  ldarg.0
    IL_001d:  ldfld      ""B Program.<Assign>d__0.b""
    IL_0022:  stfld      ""B Program.<Assign>d__0.<>7__wrap2""
    IL_0027:  ldstr      ""RHS""
    IL_002c:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_0031:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0036:  stloc.3
    IL_0037:  ldloca.s   V_3
    IL_0039:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_003e:  brtrue.s   IL_007c
    IL_0040:  ldarg.0
    IL_0041:  ldc.i4.0
    IL_0042:  dup
    IL_0043:  stloc.0
    IL_0044:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0049:  ldarg.0
    IL_004a:  ldloc.3
    IL_004b:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0050:  ldarg.0
    IL_0051:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0056:  ldloca.s   V_3
    IL_0058:  ldarg.0
    IL_0059:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_005e:  leave.s    IL_00da
    IL_0060:  ldarg.0
    IL_0061:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0066:  stloc.3
    IL_0067:  ldarg.0
    IL_0068:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_006d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0073:  ldarg.0
    IL_0074:  ldc.i4.m1
    IL_0075:  dup
    IL_0076:  stloc.0
    IL_0077:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_007c:  ldloca.s   V_3
    IL_007e:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0083:  stloc.1
    IL_0084:  ldarg.0
    IL_0085:  ldfld      ""B Program.<Assign>d__0.<>7__wrap2""
    IL_008a:  ldloc.1
    IL_008b:  dup
    IL_008c:  stloc.2
    IL_008d:  callvirt   ""void B.x.set""
    IL_0092:  ldarg.0
    IL_0093:  ldfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_0098:  ldloc.2
    IL_0099:  callvirt   ""void B.x.set""
    IL_009e:  ldarg.0
    IL_009f:  ldnull
    IL_00a0:  stfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_00a5:  ldarg.0
    IL_00a6:  ldnull
    IL_00a7:  stfld      ""B Program.<Assign>d__0.<>7__wrap2""
    IL_00ac:  leave.s    IL_00c7
  }
  catch System.Exception
  {
    IL_00ae:  stloc.s    V_4
    IL_00b0:  ldarg.0
    IL_00b1:  ldc.i4.s   -2
    IL_00b3:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_00b8:  ldarg.0
    IL_00b9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_00be:  ldloc.s    V_4
    IL_00c0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00c5:  leave.s    IL_00da
  }
  IL_00c7:  ldarg.0
  IL_00c8:  ldc.i4.s   -2
  IL_00ca:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00cf:  ldarg.0
  IL_00d0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00d5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00da:  ret
}");
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void AssignmentToFieldOfStaticFieldOfStruct()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign()
    {
        A.b.x = await Write(""RHS"");
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }

    static async Task Main(string[] args)
    {
        Console.WriteLine(""Before Assignment A.b.x is: "" + A.b.x);
        await Assign();
        Console.WriteLine(""After Assignment A.b.x is: "" + A.b.x);
    }
}

struct A
{
    public static B b;
}

struct B
{
    public int x;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"Before Assignment A.b.x is: 0
RHS
After Assignment A.b.x is: 42")
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      159 (0x9f)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0043
    IL_000a:  ldstr      ""RHS""
    IL_000f:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_0014:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0019:  stloc.2
    IL_001a:  ldloca.s   V_2
    IL_001c:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0021:  brtrue.s   IL_005f
    IL_0023:  ldarg.0
    IL_0024:  ldc.i4.0
    IL_0025:  dup
    IL_0026:  stloc.0
    IL_0027:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_002c:  ldarg.0
    IL_002d:  ldloc.2
    IL_002e:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0033:  ldarg.0
    IL_0034:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0039:  ldloca.s   V_2
    IL_003b:  ldarg.0
    IL_003c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_0041:  leave.s    IL_009e
    IL_0043:  ldarg.0
    IL_0044:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0049:  stloc.2
    IL_004a:  ldarg.0
    IL_004b:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0050:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0056:  ldarg.0
    IL_0057:  ldc.i4.m1
    IL_0058:  dup
    IL_0059:  stloc.0
    IL_005a:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_005f:  ldloca.s   V_2
    IL_0061:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0066:  stloc.1
    IL_0067:  ldsflda    ""B A.b""
    IL_006c:  ldloc.1
    IL_006d:  stfld      ""int B.x""
    IL_0072:  leave.s    IL_008b
  }
  catch System.Exception
  {
    IL_0074:  stloc.3
    IL_0075:  ldarg.0
    IL_0076:  ldc.i4.s   -2
    IL_0078:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_007d:  ldarg.0
    IL_007e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0083:  ldloc.3
    IL_0084:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0089:  leave.s    IL_009e
  }
  IL_008b:  ldarg.0
  IL_008c:  ldc.i4.s   -2
  IL_008e:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0093:  ldarg.0
  IL_0094:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_0099:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_009e:  ret
}");
        }

        [Fact, WorkItem(47191, "https://github.com/dotnet/roslyn/issues/47191")]
        public void AssignStaticStructField()
        {
            var source = @"
using System;
using System.Threading.Tasks;

public struct S1
{
    public int Field;
}

public class C
{
    public static S1 s1;
    static async Task M(Task<int> t)
    {
        s1.Field = await t;
    }

    static async Task Main()
    {
        await M(Task.FromResult(1));
        Console.Write(s1.Field);
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyDiagnostics();
        }

        [Fact, WorkItem(47191, "https://github.com/dotnet/roslyn/issues/47191")]
        public void AssignStaticStructField_ViaUsingStatic()
        {
            var source = @"
using System;
using System.Threading.Tasks;
using static C;

public struct S1
{
    public int Field;
}

public class C
{
    public static S1 s1;
}

public class Program
{
    static async Task M(Task<int> t)
    {
        s1.Field = await t;
    }

    static async Task Main()
    {
        await M(Task.FromResult(1));
        Console.Write(s1.Field);
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyDiagnostics();
        }

        [Fact, WorkItem(47191, "https://github.com/dotnet/roslyn/issues/47191")]
        public void AssignInstanceStructField()
        {
            var source = @"
using System;
using System.Threading.Tasks;

public struct S1
{
    public int Field;
}

public class C
{
    public S1 s1;
    async Task M(Task<int> t)
    {
        s1.Field = await t;
    }

    static async Task Main()
    {
        var c = new C();
        await c.M(Task.FromResult(1));
        Console.Write(c.s1.Field);
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyDiagnostics();
        }
    }
}
