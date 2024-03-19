// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenAsyncLocalsTests : EmitMetadataTestBase
    {
        private static readonly MetadataReference[] s_asyncRefs = new[] { MscorlibRef_v4_0_30316_17626, SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929 };

        public CodeGenAsyncLocalsTests()
        {
        }

        private CompilationVerifier CompileAndVerify(string source, string expectedOutput = null, IEnumerable<MetadataReference> references = null, CSharpCompilationOptions options = null, Verification verify = default)
        {
            references = (references != null) ? references.Concat(s_asyncRefs) : s_asyncRefs;
            return base.CompileAndVerify(source, targetFramework: TargetFramework.Empty, expectedOutput: expectedOutput, references: references, options: options, verify: verify);
        }

        private string GetFieldLoadsAndStores(CompilationVerifier c, string qualifiedMethodName)
        {
            var actualLines = c.VisualizeIL(qualifiedMethodName).Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            return string.Join(Environment.NewLine,
                from pair in actualLines.Zip(actualLines.Skip(1), (line1, line2) => new { line1, line2 })
                where pair.line2.Contains("ldfld") || pair.line2.Contains("stfld")
                select pair.line1.Trim() + Environment.NewLine + pair.line2.Trim());
        }

        [Fact]
        public void AsyncWithLocals()
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
        await F(x);
        c += x;
        await F(x);
        c += x;
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
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        [WorkItem(13867, "https://github.com/dotnet/roslyn/issues/13867")]
        public void AsyncWithLotsLocals()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main()
        {
            DoItAsync().Wait();
        }

        public static async Task DoItAsync()
        {
            var var1 = 0;
            var var2 = 0;
            var var3 = 0;
            var var4 = 0;
            var var5 = 0;
            var var6 = 0;
            var var7 = 0;
            var var8 = 0;
            var var9 = 0;
            var var10 = 0;
            var var11 = 0;
            var var12 = 0;
            var var13 = 0;
            var var14 = 0;
            var var15 = 0;
            var var16 = 0;
            var var17 = 0;
            var var18 = 0;
            var var19 = 0;
            var var20 = 0;
            var var21 = 0;
            var var22 = 0;
            var var23 = 0;
            var var24 = 0;
            var var25 = 0;
            var var26 = 0;
            var var27 = 0;
            var var28 = 0;
            var var29 = 0;
            var var30 = 0;
            var var31 = 0;

            string s;
            if (true)
            {
                s = ""a"";
                await Task.Yield();
            }
            else
            {
                s = ""b"";
            }

            Console.WriteLine(s ?? ""null"");  // should be ""a"" always, somehow is ""null""
        }
    }
}";
            var expected = @"
a
";
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: expected);
            CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: expected);
        }

        [Fact]
        public void AsyncWithParam()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int> G(int x)
    {
        await Task.Factory.StartNew(() => { return x; });
        x += 21;
        await Task.Factory.StartNew(() => { return x; });
        x += 21;
        return x;
    }

    public static void Main()
    {
        Task<int> t = G(0);
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
        public void AsyncWithParamsAndLocals_Unhoisted()
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
        c = await F(x);
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
21
";
            CompileAndVerify(source, expectedOutput: expected);
        }

        [Fact]
        public void HoistedParameters()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    public static Task<int> G() => null;

    public static async Task M(int x, int y, int z)
    {
        x = z;
        await G();
        y = 1;
    }
}";
            CompileAndVerify(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                AssertEx.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "x",
                    "z",
                    "y",
                    "<>u__1",
                }, module.GetFieldNames("C.<M>d__1"));
            });

            CompileAndVerify(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                AssertEx.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "x",
                    "y",
                    "z",
                    "<>u__1",
                }, module.GetFieldNames("C.<M>d__1"));
            });
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void SynthesizedVariables1()
        {
            var source =
@"
using System;
using System.Threading.Tasks;

class C
{
    public static Task<int> H() => null;
    public static Task<int> G(int a, int b, int c) => null;
    public static int F(int a) => 1;
    
    public async Task M(IDisposable disposable)
    {
        foreach (var item in new[] { 1, 2, 3 }) { using (disposable) { await H(); } }
        foreach (var item in new[] { 1, 2, 3 }) { }
        using (disposable) { await H(); }
        if (disposable != null) { using (disposable) { await G(F(1), F(2), await G(F(3), F(4), await H())); } }
        using (disposable) { await H(); }
        if (disposable != null) { using (disposable) { } }
        lock (this) { }
    }
}";
            CompileAndVerify(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                AssertEx.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "disposable",
                    "<>4__this",
                    "<>7__wrap1",
                    "<>7__wrap2",
                    "<>7__wrap3",
                    "<>u__1",
                    "<>7__wrap4",
                    "<>7__wrap5",
                    "<>7__wrap6",
                }, module.GetFieldNames("C.<M>d__3"));
            });

            var vd = CompileAndVerify(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                AssertEx.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "disposable",
                    "<>4__this",
                    "<>s__1",
                    "<>s__2",
                    "<item>5__3",
                    "<>s__4",
                    "<>s__5",
                    "<>s__6",
                    "<item>5__7",
                    "<>s__8",
                    "<>s__9",
                    "<>s__10",
                    "<>s__11",
                    "<>s__12",
                    "<>s__13",
                    "<>s__14",
                    "<>s__15",
                    "<>s__16",
                    "<>s__17",
                    "<>s__18",
                    "<>s__19",
                    "<>u__1",
                }, module.GetFieldNames("C.<M>d__3"));
            });

            vd.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames=""disposable"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__3"" />
        <encLocalSlotMap>
          <slot kind=""6"" offset=""11"" />
          <slot kind=""8"" offset=""11"" />
          <slot kind=""0"" offset=""11"" />
          <slot kind=""4"" offset=""53"" />
          <slot kind=""6"" offset=""98"" />
          <slot kind=""8"" offset=""98"" />
          <slot kind=""0"" offset=""98"" />
          <slot kind=""4"" offset=""151"" />
          <slot kind=""4"" offset=""220"" />
          <slot kind=""28"" offset=""261"" />
          <slot kind=""28"" offset=""261"" ordinal=""1"" />
          <slot kind=""28"" offset=""261"" ordinal=""2"" />
          <slot kind=""28"" offset=""281"" />
          <slot kind=""28"" offset=""281"" ordinal=""1"" />
          <slot kind=""28"" offset=""281"" ordinal=""2"" />
          <slot kind=""4"" offset=""307"" />
          <slot kind=""4"" offset=""376"" />
          <slot kind=""3"" offset=""410"" />
          <slot kind=""2"" offset=""410"" />
        </encLocalSlotMap>
        <encStateMachineStateMap>
          <state number=""0"" offset=""74"" />
          <state number=""1"" offset=""172"" />
          <state number=""4"" offset=""241"" />
          <state number=""3"" offset=""261"" />
          <state number=""2"" offset=""281"" />
          <state number=""5"" offset=""328"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: PdbValidationOptions.ExcludeDocuments);
        }

        [Fact]
        public void CaptureThis()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System;

struct TestCase
{
    public async Task<int> Run()
    {
        return await Goo();
    }

    public async Task<int> Goo()
    {
        return await Task.Factory.StartNew(() => 42);
    }
}

class Driver
{
    static void Main()
    {
        var t = new TestCase();
        var task = t.Run();
        task.Wait();
        Console.WriteLine(task.Result);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void CaptureThis2()
        {
            var source = @"
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;

struct TestCase
{
    public IEnumerable<int> Run()
    {
        yield return Goo();
    }

    public int Goo()
    {
        return 42;
    }
}

class Driver
{
    static void Main()
    {
        var t = new TestCase();
        foreach (var x in t.Run())
        {
            Console.WriteLine(x);
        }
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expected);
        }

        [Fact]
        public void AsyncWithDynamic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int> F(dynamic t)
    {
        return await t;
    }

    public static void Main()
    {
        Task<int> t = F(Task.Factory.StartNew(() => { return 42; }));
        t.Wait(1000 * 60);
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
42
";
            CompileAndVerify(source, expectedOutput: expected, references: new[] { CSharpRef });
        }

        [Fact]
        public void AsyncWithThisRef()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    int x = 42;

    public async Task<int> F()
    {
        int c = this.x;
        return await Task.Factory.StartNew(() => c);
    }
}

class Test
{
    public static void Main()
    {
        Task<int> t = new C().F();
        t.Wait(1000 * 60);
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
42
";
            var verifier = CompileAndVerify(source, expectedOutput: expected);

            verifier.VerifyIL("C.<F>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      191 (0xbf)
  .maxstack  3
  .locals init (int V_0,
                C V_1,
                int V_2,
                C.<>c__DisplayClass1_0 V_3, //CS$<>8__locals0
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__1.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""C C.<F>d__1.<>4__this""
  IL_000d:  stloc.1
  .try
  {
    IL_000e:  ldloc.0
    IL_000f:  brfalse.s  IL_006a
    IL_0011:  newobj     ""C.<>c__DisplayClass1_0..ctor()""
    IL_0016:  stloc.3
    IL_0017:  ldloc.3
    IL_0018:  ldloc.1
    IL_0019:  ldfld      ""int C.x""
    IL_001e:  stfld      ""int C.<>c__DisplayClass1_0.c""
    IL_0023:  call       ""System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get""
    IL_0028:  ldloc.3
    IL_0029:  ldftn      ""int C.<>c__DisplayClass1_0.<F>b__0()""
    IL_002f:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
    IL_0034:  callvirt   ""System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)""
    IL_0039:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_003e:  stloc.s    V_4
    IL_0040:  ldloca.s   V_4
    IL_0042:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0047:  brtrue.s   IL_0087
    IL_0049:  ldarg.0
    IL_004a:  ldc.i4.0
    IL_004b:  dup
    IL_004c:  stloc.0
    IL_004d:  stfld      ""int C.<F>d__1.<>1__state""
    IL_0052:  ldarg.0
    IL_0053:  ldloc.s    V_4
    IL_0055:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__1.<>u__1""
    IL_005a:  ldarg.0
    IL_005b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__1.<>t__builder""
    IL_0060:  ldloca.s   V_4
    IL_0062:  ldarg.0
    IL_0063:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__1)""
    IL_0068:  leave.s    IL_00be
    IL_006a:  ldarg.0
    IL_006b:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__1.<>u__1""
    IL_0070:  stloc.s    V_4
    IL_0072:  ldarg.0
    IL_0073:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__1.<>u__1""
    IL_0078:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_007e:  ldarg.0
    IL_007f:  ldc.i4.m1
    IL_0080:  dup
    IL_0081:  stloc.0
    IL_0082:  stfld      ""int C.<F>d__1.<>1__state""
    IL_0087:  ldloca.s   V_4
    IL_0089:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_008e:  stloc.2
    IL_008f:  leave.s    IL_00aa
  }
  catch System.Exception
  {
    IL_0091:  stloc.s    V_5
    IL_0093:  ldarg.0
    IL_0094:  ldc.i4.s   -2
    IL_0096:  stfld      ""int C.<F>d__1.<>1__state""
    IL_009b:  ldarg.0
    IL_009c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__1.<>t__builder""
    IL_00a1:  ldloc.s    V_5
    IL_00a3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00a8:  leave.s    IL_00be
  }
  IL_00aa:  ldarg.0
  IL_00ab:  ldc.i4.s   -2
  IL_00ad:  stfld      ""int C.<F>d__1.<>1__state""
  IL_00b2:  ldarg.0
  IL_00b3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__1.<>t__builder""
  IL_00b8:  ldloc.2
  IL_00b9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00be:  ret
}
");
        }

        [Fact]
        public void AsyncWithThisRef01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    int x => 14;

    public async Task<int> F()
    {
        int c = this.x;
        await Task.Yield();
        c += this.x;
        await Task.Yield();
        c += this.x;
        await Task.Yield();
        await Task.Yield();
        await Task.Yield();

        return c;
    }
}

class Test
{
    public static void Main()
    {
        Task<int> t = new C().F();
        t.Wait(1000 * 60);
        Console.WriteLine(t.Result);
    }
}";
            var expected = @"
42
";
            var verifier = CompileAndVerify(source, expectedOutput: expected);

            verifier.VerifyIL("C.<F>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      612 (0x264)
  .maxstack  3
  .locals init (int V_0,
                C V_1,
                int V_2,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_3,
                System.Runtime.CompilerServices.YieldAwaitable V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__2.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""C C.<F>d__2.<>4__this""
  IL_000d:  stloc.1
  .try
  {
    IL_000e:  ldloc.0
    IL_000f:  switch    (
        IL_006f,
        IL_00e0,
        IL_0151,
        IL_01af,
        IL_020a)
    IL_0028:  ldarg.0
    IL_0029:  ldloc.1
    IL_002a:  call       ""int C.x.get""
    IL_002f:  stfld      ""int C.<F>d__2.<c>5__2""
    IL_0034:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0039:  stloc.s    V_4
    IL_003b:  ldloca.s   V_4
    IL_003d:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_0042:  stloc.3
    IL_0043:  ldloca.s   V_3
    IL_0045:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_004a:  brtrue.s   IL_008b
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.0
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      ""int C.<F>d__2.<>1__state""
    IL_0055:  ldarg.0
    IL_0056:  ldloc.3
    IL_0057:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__2.<>u__1""
    IL_005c:  ldarg.0
    IL_005d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__2.<>t__builder""
    IL_0062:  ldloca.s   V_3
    IL_0064:  ldarg.0
    IL_0065:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, C.<F>d__2>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref C.<F>d__2)""
    IL_006a:  leave      IL_0263
    IL_006f:  ldarg.0
    IL_0070:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__2.<>u__1""
    IL_0075:  stloc.3
    IL_0076:  ldarg.0
    IL_0077:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__2.<>u__1""
    IL_007c:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_0082:  ldarg.0
    IL_0083:  ldc.i4.m1
    IL_0084:  dup
    IL_0085:  stloc.0
    IL_0086:  stfld      ""int C.<F>d__2.<>1__state""
    IL_008b:  ldloca.s   V_3
    IL_008d:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_0092:  ldarg.0
    IL_0093:  ldarg.0
    IL_0094:  ldfld      ""int C.<F>d__2.<c>5__2""
    IL_0099:  ldloc.1
    IL_009a:  call       ""int C.x.get""
    IL_009f:  add
    IL_00a0:  stfld      ""int C.<F>d__2.<c>5__2""
    IL_00a5:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_00aa:  stloc.s    V_4
    IL_00ac:  ldloca.s   V_4
    IL_00ae:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_00b3:  stloc.3
    IL_00b4:  ldloca.s   V_3
    IL_00b6:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_00bb:  brtrue.s   IL_00fc
    IL_00bd:  ldarg.0
    IL_00be:  ldc.i4.1
    IL_00bf:  dup
    IL_00c0:  stloc.0
    IL_00c1:  stfld      ""int C.<F>d__2.<>1__state""
    IL_00c6:  ldarg.0
    IL_00c7:  ldloc.3
    IL_00c8:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__2.<>u__1""
    IL_00cd:  ldarg.0
    IL_00ce:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__2.<>t__builder""
    IL_00d3:  ldloca.s   V_3
    IL_00d5:  ldarg.0
    IL_00d6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, C.<F>d__2>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref C.<F>d__2)""
    IL_00db:  leave      IL_0263
    IL_00e0:  ldarg.0
    IL_00e1:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__2.<>u__1""
    IL_00e6:  stloc.3
    IL_00e7:  ldarg.0
    IL_00e8:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__2.<>u__1""
    IL_00ed:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_00f3:  ldarg.0
    IL_00f4:  ldc.i4.m1
    IL_00f5:  dup
    IL_00f6:  stloc.0
    IL_00f7:  stfld      ""int C.<F>d__2.<>1__state""
    IL_00fc:  ldloca.s   V_3
    IL_00fe:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_0103:  ldarg.0
    IL_0104:  ldarg.0
    IL_0105:  ldfld      ""int C.<F>d__2.<c>5__2""
    IL_010a:  ldloc.1
    IL_010b:  call       ""int C.x.get""
    IL_0110:  add
    IL_0111:  stfld      ""int C.<F>d__2.<c>5__2""
    IL_0116:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_011b:  stloc.s    V_4
    IL_011d:  ldloca.s   V_4
    IL_011f:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_0124:  stloc.3
    IL_0125:  ldloca.s   V_3
    IL_0127:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_012c:  brtrue.s   IL_016d
    IL_012e:  ldarg.0
    IL_012f:  ldc.i4.2
    IL_0130:  dup
    IL_0131:  stloc.0
    IL_0132:  stfld      ""int C.<F>d__2.<>1__state""
    IL_0137:  ldarg.0
    IL_0138:  ldloc.3
    IL_0139:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__2.<>u__1""
    IL_013e:  ldarg.0
    IL_013f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__2.<>t__builder""
    IL_0144:  ldloca.s   V_3
    IL_0146:  ldarg.0
    IL_0147:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, C.<F>d__2>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref C.<F>d__2)""
    IL_014c:  leave      IL_0263
    IL_0151:  ldarg.0
    IL_0152:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__2.<>u__1""
    IL_0157:  stloc.3
    IL_0158:  ldarg.0
    IL_0159:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__2.<>u__1""
    IL_015e:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_0164:  ldarg.0
    IL_0165:  ldc.i4.m1
    IL_0166:  dup
    IL_0167:  stloc.0
    IL_0168:  stfld      ""int C.<F>d__2.<>1__state""
    IL_016d:  ldloca.s   V_3
    IL_016f:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_0174:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0179:  stloc.s    V_4
    IL_017b:  ldloca.s   V_4
    IL_017d:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_0182:  stloc.3
    IL_0183:  ldloca.s   V_3
    IL_0185:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_018a:  brtrue.s   IL_01cb
    IL_018c:  ldarg.0
    IL_018d:  ldc.i4.3
    IL_018e:  dup
    IL_018f:  stloc.0
    IL_0190:  stfld      ""int C.<F>d__2.<>1__state""
    IL_0195:  ldarg.0
    IL_0196:  ldloc.3
    IL_0197:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__2.<>u__1""
    IL_019c:  ldarg.0
    IL_019d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__2.<>t__builder""
    IL_01a2:  ldloca.s   V_3
    IL_01a4:  ldarg.0
    IL_01a5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, C.<F>d__2>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref C.<F>d__2)""
    IL_01aa:  leave      IL_0263
    IL_01af:  ldarg.0
    IL_01b0:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__2.<>u__1""
    IL_01b5:  stloc.3
    IL_01b6:  ldarg.0
    IL_01b7:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__2.<>u__1""
    IL_01bc:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_01c2:  ldarg.0
    IL_01c3:  ldc.i4.m1
    IL_01c4:  dup
    IL_01c5:  stloc.0
    IL_01c6:  stfld      ""int C.<F>d__2.<>1__state""
    IL_01cb:  ldloca.s   V_3
    IL_01cd:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_01d2:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_01d7:  stloc.s    V_4
    IL_01d9:  ldloca.s   V_4
    IL_01db:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_01e0:  stloc.3
    IL_01e1:  ldloca.s   V_3
    IL_01e3:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_01e8:  brtrue.s   IL_0226
    IL_01ea:  ldarg.0
    IL_01eb:  ldc.i4.4
    IL_01ec:  dup
    IL_01ed:  stloc.0
    IL_01ee:  stfld      ""int C.<F>d__2.<>1__state""
    IL_01f3:  ldarg.0
    IL_01f4:  ldloc.3
    IL_01f5:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__2.<>u__1""
    IL_01fa:  ldarg.0
    IL_01fb:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__2.<>t__builder""
    IL_0200:  ldloca.s   V_3
    IL_0202:  ldarg.0
    IL_0203:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, C.<F>d__2>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref C.<F>d__2)""
    IL_0208:  leave.s    IL_0263
    IL_020a:  ldarg.0
    IL_020b:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__2.<>u__1""
    IL_0210:  stloc.3
    IL_0211:  ldarg.0
    IL_0212:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__2.<>u__1""
    IL_0217:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_021d:  ldarg.0
    IL_021e:  ldc.i4.m1
    IL_021f:  dup
    IL_0220:  stloc.0
    IL_0221:  stfld      ""int C.<F>d__2.<>1__state""
    IL_0226:  ldloca.s   V_3
    IL_0228:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_022d:  ldarg.0
    IL_022e:  ldfld      ""int C.<F>d__2.<c>5__2""
    IL_0233:  stloc.2
    IL_0234:  leave.s    IL_024f
  }
  catch System.Exception
  {
    IL_0236:  stloc.s    V_5
    IL_0238:  ldarg.0
    IL_0239:  ldc.i4.s   -2
    IL_023b:  stfld      ""int C.<F>d__2.<>1__state""
    IL_0240:  ldarg.0
    IL_0241:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__2.<>t__builder""
    IL_0246:  ldloc.s    V_5
    IL_0248:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_024d:  leave.s    IL_0263
  }
  IL_024f:  ldarg.0
  IL_0250:  ldc.i4.s   -2
  IL_0252:  stfld      ""int C.<F>d__2.<>1__state""
  IL_0257:  ldarg.0
  IL_0258:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__2.<>t__builder""
  IL_025d:  ldloc.2
  IL_025e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_0263:  ret
}
");
        }

        [Fact]
        public void AsyncWithBaseRef()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class B
{
    protected int x = 42;   
}

class C : B
{
    public async Task<int> F()
    {
        int c = base.x;
        return await Task.Factory.StartNew(() => c);
    }
}

class Test
{
    public static void Main()
    {
        Task<int> t = new C().F();
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
        public void ReuseFields_SpillTemps()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    static void F1(int x, int y)
    {
    }

    async static Task<int> F2()
    {
        return await Task.Factory.StartNew(() => 42);
    }

    public static async void Run()
    {
        int x = 1;
        F1(x, await F2());

        int y = 2;
        F1(y, await F2());

        int z = 3;
        F1(z, await F2());
    }

    public static void Main()
    {
        Run();
    }
}";
            var reference = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { SystemRef_v4_0_30319_17929 }).EmitToImageReference();
            var comp = CreateCompilationWithMscorlib45("", new[] { reference }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal));
            var testClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            var stateMachineClass = (NamedTypeSymbol)testClass.GetMembers().Single(s => s.Name.StartsWith("<Run>", StringComparison.Ordinal));
            IEnumerable<IGrouping<TypeSymbol, FieldSymbol>> spillFieldsByType = stateMachineClass.GetMembers().Where(m => m.Kind == SymbolKind.Field && m.Name.StartsWith("<>7__wrap", StringComparison.Ordinal)).Cast<FieldSymbol>().GroupBy(x => x.Type);

            Assert.Equal(1, spillFieldsByType.Count());
            Assert.Equal(1, spillFieldsByType.Single(x => TypeSymbol.Equals(x.Key, comp.GetSpecialType(SpecialType.System_Int32), TypeCompareKind.ConsiderEverything2)).Count());
        }

        [Fact]
        public void ReuseFields_Generic()
        {
            var source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Test<U>
{
    static IEnumerable<T> GetEnum<T>() => null;
    static Task<int> F(int a) => null;

    public static async void M<S, T>()
    {
        foreach (var x in GetEnum<T>()) await F(1);
        foreach (var x in GetEnum<S>()) await F(2);
        foreach (var x in GetEnum<T>()) await F(3);
        foreach (var x in GetEnum<U>()) await F(4);
        foreach (var x in GetEnum<U>()) await F(5);
    }
}";
            var c = CompileAndVerify(source, expectedOutput: null, options: TestOptions.ReleaseDll);

            var actual = GetFieldLoadsAndStores(c, "Test<U>.<M>d__2<S, T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext");

            // make sure we are reusing synthesized iterator locals and that the locals are nulled:
            AssertEx.AssertEqualToleratingWhitespaceDifferences(@"
IL_0000:  ldarg.0
IL_0001:  ldfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_0027:  callvirt   ""System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator()""
IL_002c:  stfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_0037:  ldarg.0
IL_0038:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_005b:  stloc.0
IL_005c:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_0062:  ldloc.1
IL_0063:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_0068:  ldarg.0
IL_0069:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__2<S, T>.<>t__builder""
IL_007b:  ldarg.0
IL_007c:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_0082:  ldarg.0
IL_0083:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_0091:  stloc.0
IL_0092:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_009f:  ldarg.0
IL_00a0:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_00b2:  ldarg.0
IL_00b3:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_00ba:  ldarg.0
IL_00bb:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_00c7:  ldnull
IL_00c8:  stfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_00d3:  callvirt   ""System.Collections.Generic.IEnumerator<S> System.Collections.Generic.IEnumerable<S>.GetEnumerator()""
IL_00d8:  stfld      ""System.Collections.Generic.IEnumerator<S> Test<U>.<M>d__2<S, T>.<>7__wrap2""
IL_00e4:  ldarg.0
IL_00e5:  ldfld      ""System.Collections.Generic.IEnumerator<S> Test<U>.<M>d__2<S, T>.<>7__wrap2""
IL_0108:  stloc.0
IL_0109:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_010f:  ldloc.1
IL_0110:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_0115:  ldarg.0
IL_0116:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__2<S, T>.<>t__builder""
IL_0128:  ldarg.0
IL_0129:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_012f:  ldarg.0
IL_0130:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_013e:  stloc.0
IL_013f:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_014c:  ldarg.0
IL_014d:  ldfld      ""System.Collections.Generic.IEnumerator<S> Test<U>.<M>d__2<S, T>.<>7__wrap2""
IL_015f:  ldarg.0
IL_0160:  ldfld      ""System.Collections.Generic.IEnumerator<S> Test<U>.<M>d__2<S, T>.<>7__wrap2""
IL_0167:  ldarg.0
IL_0168:  ldfld      ""System.Collections.Generic.IEnumerator<S> Test<U>.<M>d__2<S, T>.<>7__wrap2""
IL_0174:  ldnull
IL_0175:  stfld      ""System.Collections.Generic.IEnumerator<S> Test<U>.<M>d__2<S, T>.<>7__wrap2""
IL_0180:  callvirt   ""System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator()""
IL_0185:  stfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_0191:  ldarg.0
IL_0192:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_01b5:  stloc.0
IL_01b6:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_01bc:  ldloc.1
IL_01bd:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_01c2:  ldarg.0
IL_01c3:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__2<S, T>.<>t__builder""
IL_01d5:  ldarg.0
IL_01d6:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_01dc:  ldarg.0
IL_01dd:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_01eb:  stloc.0
IL_01ec:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_01f9:  ldarg.0
IL_01fa:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_020c:  ldarg.0
IL_020d:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_0214:  ldarg.0
IL_0215:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_0221:  ldnull
IL_0222:  stfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_022d:  callvirt   ""System.Collections.Generic.IEnumerator<U> System.Collections.Generic.IEnumerable<U>.GetEnumerator()""
IL_0232:  stfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_023e:  ldarg.0
IL_023f:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_0262:  stloc.0
IL_0263:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_0269:  ldloc.1
IL_026a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_026f:  ldarg.0
IL_0270:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__2<S, T>.<>t__builder""
IL_0282:  ldarg.0
IL_0283:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_0289:  ldarg.0
IL_028a:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_0298:  stloc.0
IL_0299:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_02a6:  ldarg.0
IL_02a7:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_02b9:  ldarg.0
IL_02ba:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_02c1:  ldarg.0
IL_02c2:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_02ce:  ldnull
IL_02cf:  stfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_02da:  callvirt   ""System.Collections.Generic.IEnumerator<U> System.Collections.Generic.IEnumerable<U>.GetEnumerator()""
IL_02df:  stfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_02eb:  ldarg.0
IL_02ec:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_030f:  stloc.0
IL_0310:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_0316:  ldloc.1
IL_0317:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_031c:  ldarg.0
IL_031d:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__2<S, T>.<>t__builder""
IL_032c:  ldarg.0
IL_032d:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_0333:  ldarg.0
IL_0334:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_0342:  stloc.0
IL_0343:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_0350:  ldarg.0
IL_0351:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_0363:  ldarg.0
IL_0364:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_036b:  ldarg.0
IL_036c:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_0378:  ldnull
IL_0379:  stfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_0382:  ldc.i4.s   -2
IL_0384:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_0389:  ldarg.0
IL_038a:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__2<S, T>.<>t__builder""
IL_0398:  ldc.i4.s   -2
IL_039a:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_039f:  ldarg.0
IL_03a0:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__2<S, T>.<>t__builder""
", actual);
        }

        [Fact]
        public void ReuseFields_Dynamic()
        {
            var source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    static IEnumerable<dynamic> GetDynamicEnum() => null;
    static IEnumerable<object> GetObjectEnum() => null;
    static Task<int> F(int a) => null;

    public static async void M()
    {
        foreach (var x in GetDynamicEnum()) await F(1);
        foreach (var x in GetObjectEnum()) await F(2);
    }
}";
            var c = CompileAndVerify(source, expectedOutput: null, options: TestOptions.ReleaseDll);

            var actual = GetFieldLoadsAndStores(c, "Test.<M>d__3.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext");

            // make sure we are reusing synthesized iterator locals and that the locals are nulled:
            AssertEx.AssertEqualToleratingWhitespaceDifferences(@"
IL_0000:  ldarg.0
IL_0001:  ldfld      ""int Test.<M>d__3.<>1__state""
IL_0017:  callvirt   ""System.Collections.Generic.IEnumerator<dynamic> System.Collections.Generic.IEnumerable<dynamic>.GetEnumerator()""
IL_001c:  stfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_0027:  ldarg.0
IL_0028:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_004b:  stloc.0
IL_004c:  stfld      ""int Test.<M>d__3.<>1__state""
IL_0052:  ldloc.1
IL_0053:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<M>d__3.<>u__1""
IL_0058:  ldarg.0
IL_0059:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<M>d__3.<>t__builder""
IL_006b:  ldarg.0
IL_006c:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<M>d__3.<>u__1""
IL_0072:  ldarg.0
IL_0073:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<M>d__3.<>u__1""
IL_0081:  stloc.0
IL_0082:  stfld      ""int Test.<M>d__3.<>1__state""
IL_008f:  ldarg.0
IL_0090:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_00a2:  ldarg.0
IL_00a3:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_00aa:  ldarg.0
IL_00ab:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_00b7:  ldnull
IL_00b8:  stfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_00c3:  callvirt   ""System.Collections.Generic.IEnumerator<object> System.Collections.Generic.IEnumerable<object>.GetEnumerator()""
IL_00c8:  stfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_00d4:  ldarg.0
IL_00d5:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_00f8:  stloc.0
IL_00f9:  stfld      ""int Test.<M>d__3.<>1__state""
IL_00ff:  ldloc.1
IL_0100:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<M>d__3.<>u__1""
IL_0105:  ldarg.0
IL_0106:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<M>d__3.<>t__builder""
IL_0115:  ldarg.0
IL_0116:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<M>d__3.<>u__1""
IL_011c:  ldarg.0
IL_011d:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<M>d__3.<>u__1""
IL_012b:  stloc.0
IL_012c:  stfld      ""int Test.<M>d__3.<>1__state""
IL_0139:  ldarg.0
IL_013a:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_014c:  ldarg.0
IL_014d:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_0154:  ldarg.0
IL_0155:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_0161:  ldnull
IL_0162:  stfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_016b:  ldc.i4.s   -2
IL_016d:  stfld      ""int Test.<M>d__3.<>1__state""
IL_0172:  ldarg.0
IL_0173:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<M>d__3.<>t__builder""
IL_0181:  ldc.i4.s   -2
IL_0183:  stfld      ""int Test.<M>d__3.<>1__state""
IL_0188:  ldarg.0
IL_0189:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<M>d__3.<>t__builder""
", actual);
        }

        [Fact]
        public void ManySynthesizedNames()
        {
            string source = @"
using System;
using System.Threading.Tasks;

public class C
{
    public async Task F()
    {
        var a1 = await Task.FromResult(default(Tuple<char, char, char>));
        var a2 = await Task.FromResult(default(Tuple<char, char, byte>));
        var a3 = await Task.FromResult(default(Tuple<char, byte, char>));
        var a4 = await Task.FromResult(default(Tuple<char, byte, byte>));
        var a5 = await Task.FromResult(default(Tuple<byte, char, char>));
        var a6 = await Task.FromResult(default(Tuple<byte, char, byte>));
        var a7 = await Task.FromResult(default(Tuple<byte, byte, char>));
        var a8 = await Task.FromResult(default(Tuple<byte, byte, byte>));

        var b1 = await Task.FromResult(default(Tuple<int, int, int>));
        var b2 = await Task.FromResult(default(Tuple<int, int, long>));
        var b3 = await Task.FromResult(default(Tuple<int, long, int>));
        var b4 = await Task.FromResult(default(Tuple<int, long, long>));
        var b5 = await Task.FromResult(default(Tuple<long, int, int>));
        var b6 = await Task.FromResult(default(Tuple<long, int, long>));
        var b7 = await Task.FromResult(default(Tuple<long, long, int>));
        var b8 = await Task.FromResult(default(Tuple<long, long, long>));
    }
}";
            CompileAndVerify(source, targetFramework: TargetFramework.Empty, references: s_asyncRefs, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                AssertEx.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "<>u__1",
                    "<>u__2",
                    "<>u__3",
                    "<>u__4",
                    "<>u__5",
                    "<>u__6",
                    "<>u__7",
                    "<>u__8",
                    "<>u__9",
                    "<>u__10",
                    "<>u__11",
                    "<>u__12",
                    "<>u__13",
                    "<>u__14",
                    "<>u__15",
                    "<>u__16",
                }, module.GetFieldNames("C.<F>d__0"));
            });
        }

        [WorkItem(9775, "https://github.com/dotnet/roslyn/issues/9775")]
        [Fact]
        public void Fixed_Debug()
        {
            var text =
@"using System;
using System.Threading.Tasks;
class C
{
    static async Task<int> F(byte[] b)
    {
        int i;
        unsafe
        {
            fixed (byte* p = b)
            {
                i = *p;
            }
        }
        await Task.Yield();
        return i;
    }
    static void Main()
    {
        var i = F(new byte[] { 1, 2, 3 }).Result;
        Console.Write(i);
    }
}";
            var verifier = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"1", verify: Verification.Fails);
            verifier.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()",
@"
{
  // Code size      198 (0xc6)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                byte* V_2, //p
                pinned byte[] V_3,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_4,
                System.Runtime.CompilerServices.YieldAwaitable V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_006b
    IL_000a:  ldarg.0
    IL_000b:  ldfld      ""byte[] C.<F>d__0.b""
    IL_0010:  dup
    IL_0011:  stloc.3
    IL_0012:  brfalse.s  IL_0019
    IL_0014:  ldloc.3
    IL_0015:  ldlen
    IL_0016:  conv.i4
    IL_0017:  brtrue.s   IL_001e
    IL_0019:  ldc.i4.0
    IL_001a:  conv.u
    IL_001b:  stloc.2
    IL_001c:  br.s       IL_0027
    IL_001e:  ldloc.3
    IL_001f:  ldc.i4.0
    IL_0020:  ldelema    ""byte""
    IL_0025:  conv.u
    IL_0026:  stloc.2
    IL_0027:  ldarg.0
    IL_0028:  ldloc.2
    IL_0029:  ldind.u1
    IL_002a:  stfld      ""int C.<F>d__0.<i>5__2""
    IL_002f:  ldnull
    IL_0030:  stloc.3
    IL_0031:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0036:  stloc.s    V_5
    IL_0038:  ldloca.s   V_5
    IL_003a:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_003f:  stloc.s    V_4
    IL_0041:  ldloca.s   V_4
    IL_0043:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0048:  brtrue.s   IL_0088
    IL_004a:  ldarg.0
    IL_004b:  ldc.i4.0
    IL_004c:  dup
    IL_004d:  stloc.0
    IL_004e:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0053:  ldarg.0
    IL_0054:  ldloc.s    V_4
    IL_0056:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__0.<>u__1""
    IL_005b:  ldarg.0
    IL_005c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_0061:  ldloca.s   V_4
    IL_0063:  ldarg.0
    IL_0064:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, C.<F>d__0>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref C.<F>d__0)""
    IL_0069:  leave.s    IL_00c5
    IL_006b:  ldarg.0
    IL_006c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__0.<>u__1""
    IL_0071:  stloc.s    V_4
    IL_0073:  ldarg.0
    IL_0074:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__0.<>u__1""
    IL_0079:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_007f:  ldarg.0
    IL_0080:  ldc.i4.m1
    IL_0081:  dup
    IL_0082:  stloc.0
    IL_0083:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0088:  ldloca.s   V_4
    IL_008a:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_008f:  ldarg.0
    IL_0090:  ldfld      ""int C.<F>d__0.<i>5__2""
    IL_0095:  stloc.1
    IL_0096:  leave.s    IL_00b1
  }
  catch System.Exception
  {
    IL_0098:  stloc.s    V_6
    IL_009a:  ldarg.0
    IL_009b:  ldc.i4.s   -2
    IL_009d:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00a2:  ldarg.0
    IL_00a3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_00a8:  ldloc.s    V_6
    IL_00aa:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00af:  leave.s    IL_00c5
  }
  IL_00b1:  ldarg.0
  IL_00b2:  ldc.i4.s   -2
  IL_00b4:  stfld      ""int C.<F>d__0.<>1__state""
  IL_00b9:  ldarg.0
  IL_00ba:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
  IL_00bf:  ldloc.1
  IL_00c0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00c5:  ret
}");
            verifier = CompileAndVerify(text, options: TestOptions.UnsafeDebugExe, expectedOutput: @"1", verify: Verification.Fails);
            verifier.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()",
@"
{
  // Code size      227 (0xe3)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                pinned byte[] V_2,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_3,
                System.Runtime.CompilerServices.YieldAwaitable V_4,
                C.<F>d__0 V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_0086
    IL_000e:  nop
    IL_000f:  nop
    IL_0010:  ldarg.0
    IL_0011:  ldfld      ""byte[] C.<F>d__0.b""
    IL_0016:  dup
    IL_0017:  stloc.2
    IL_0018:  brfalse.s  IL_001f
    IL_001a:  ldloc.2
    IL_001b:  ldlen
    IL_001c:  conv.i4
    IL_001d:  brtrue.s   IL_0029
    IL_001f:  ldarg.0
    IL_0020:  ldc.i4.0
    IL_0021:  conv.u
    IL_0022:  stfld      ""byte* C.<F>d__0.<p>5__2""
    IL_0027:  br.s       IL_0037
    IL_0029:  ldarg.0
    IL_002a:  ldloc.2
    IL_002b:  ldc.i4.0
    IL_002c:  ldelema    ""byte""
    IL_0031:  conv.u
    IL_0032:  stfld      ""byte* C.<F>d__0.<p>5__2""
    IL_0037:  nop
    IL_0038:  ldarg.0
    IL_0039:  ldarg.0
    IL_003a:  ldfld      ""byte* C.<F>d__0.<p>5__2""
    IL_003f:  ldind.u1
    IL_0040:  stfld      ""int C.<F>d__0.<i>5__1""
    IL_0045:  nop
    IL_0046:  ldnull
    IL_0047:  stloc.2
    IL_0048:  nop
    IL_0049:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_004e:  stloc.s    V_4
    IL_0050:  ldloca.s   V_4
    IL_0052:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_0057:  stloc.3
    IL_0058:  ldloca.s   V_3
    IL_005a:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_005f:  brtrue.s   IL_00a2
    IL_0061:  ldarg.0
    IL_0062:  ldc.i4.0
    IL_0063:  dup
    IL_0064:  stloc.0
    IL_0065:  stfld      ""int C.<F>d__0.<>1__state""
    IL_006a:  ldarg.0
    IL_006b:  ldloc.3
    IL_006c:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__0.<>u__1""
    IL_0071:  ldarg.0
    IL_0072:  stloc.s    V_5
    IL_0074:  ldarg.0
    IL_0075:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_007a:  ldloca.s   V_3
    IL_007c:  ldloca.s   V_5
    IL_007e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, C.<F>d__0>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref C.<F>d__0)""
    IL_0083:  nop
    IL_0084:  leave.s    IL_00e2
    IL_0086:  ldarg.0
    IL_0087:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__0.<>u__1""
    IL_008c:  stloc.3
    IL_008d:  ldarg.0
    IL_008e:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<F>d__0.<>u__1""
    IL_0093:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_0099:  ldarg.0
    IL_009a:  ldc.i4.m1
    IL_009b:  dup
    IL_009c:  stloc.0
    IL_009d:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00a2:  ldloca.s   V_3
    IL_00a4:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_00a9:  nop
    IL_00aa:  ldarg.0
    IL_00ab:  ldfld      ""int C.<F>d__0.<i>5__1""
    IL_00b0:  stloc.1
    IL_00b1:  leave.s    IL_00cd
  }
  catch System.Exception
  {
    IL_00b3:  stloc.s    V_6
    IL_00b5:  ldarg.0
    IL_00b6:  ldc.i4.s   -2
    IL_00b8:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00bd:  ldarg.0
    IL_00be:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_00c3:  ldloc.s    V_6
    IL_00c5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00ca:  nop
    IL_00cb:  leave.s    IL_00e2
  }
  IL_00cd:  ldarg.0
  IL_00ce:  ldc.i4.s   -2
  IL_00d0:  stfld      ""int C.<F>d__0.<>1__state""
  IL_00d5:  ldarg.0
  IL_00d6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
  IL_00db:  ldloc.1
  IL_00dc:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00e1:  nop
  IL_00e2:  ret
}");
        }

        [WorkItem(15290, "https://github.com/dotnet/roslyn/issues/15290")]
        [Fact]
        public void ReuseLocals()
        {
            var text =
@"

using System;
using System.Threading.Tasks;

class Test
{
    public static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();
    private static async Task MainAsync(string[] args)
    {
        if (args.Length > 0)
        {
            int a = 1;
            await Task.Yield();
            Console.WriteLine(a);
        }
        else
        {
            int b = 2;
            await Task.Yield();
            Console.WriteLine(b);
        }
    }
}";
            var verifier = CompileAndVerify(text, options: TestOptions.ReleaseExe, expectedOutput: @"2");

            // NOTE: only one hoisted int local:  
            //       int Test.<MainAsync>d__1.<a>5__2
            verifier.VerifyIL("Test.<MainAsync>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()",
@"
{
  // Code size      292 (0x124)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<MainAsync>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_005b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00c9
    IL_0011:  ldarg.0
    IL_0012:  ldfld      ""string[] Test.<MainAsync>d__1.args""
    IL_0017:  ldlen
    IL_0018:  brfalse.s  IL_008b
    IL_001a:  ldarg.0
    IL_001b:  ldc.i4.1
    IL_001c:  stfld      ""int Test.<MainAsync>d__1.<a>5__2""
    IL_0021:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0026:  stloc.2
    IL_0027:  ldloca.s   V_2
    IL_0029:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_002e:  stloc.1
    IL_002f:  ldloca.s   V_1
    IL_0031:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0036:  brtrue.s   IL_0077
    IL_0038:  ldarg.0
    IL_0039:  ldc.i4.0
    IL_003a:  dup
    IL_003b:  stloc.0
    IL_003c:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_0041:  ldarg.0
    IL_0042:  ldloc.1
    IL_0043:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_0048:  ldarg.0
    IL_0049:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<MainAsync>d__1.<>t__builder""
    IL_004e:  ldloca.s   V_1
    IL_0050:  ldarg.0
    IL_0051:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Test.<MainAsync>d__1>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Test.<MainAsync>d__1)""
    IL_0056:  leave      IL_0123
    IL_005b:  ldarg.0
    IL_005c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_0061:  stloc.1
    IL_0062:  ldarg.0
    IL_0063:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_0068:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_006e:  ldarg.0
    IL_006f:  ldc.i4.m1
    IL_0070:  dup
    IL_0071:  stloc.0
    IL_0072:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_0077:  ldloca.s   V_1
    IL_0079:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_007e:  ldarg.0
    IL_007f:  ldfld      ""int Test.<MainAsync>d__1.<a>5__2""
    IL_0084:  call       ""void System.Console.WriteLine(int)""
    IL_0089:  br.s       IL_00f7
    IL_008b:  ldarg.0
    IL_008c:  ldc.i4.2
    IL_008d:  stfld      ""int Test.<MainAsync>d__1.<a>5__2""
    IL_0092:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0097:  stloc.2
    IL_0098:  ldloca.s   V_2
    IL_009a:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_009f:  stloc.1
    IL_00a0:  ldloca.s   V_1
    IL_00a2:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_00a7:  brtrue.s   IL_00e5
    IL_00a9:  ldarg.0
    IL_00aa:  ldc.i4.1
    IL_00ab:  dup
    IL_00ac:  stloc.0
    IL_00ad:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_00b2:  ldarg.0
    IL_00b3:  ldloc.1
    IL_00b4:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_00b9:  ldarg.0
    IL_00ba:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<MainAsync>d__1.<>t__builder""
    IL_00bf:  ldloca.s   V_1
    IL_00c1:  ldarg.0
    IL_00c2:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Test.<MainAsync>d__1>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Test.<MainAsync>d__1)""
    IL_00c7:  leave.s    IL_0123
    IL_00c9:  ldarg.0
    IL_00ca:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_00cf:  stloc.1
    IL_00d0:  ldarg.0
    IL_00d1:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_00d6:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_00dc:  ldarg.0
    IL_00dd:  ldc.i4.m1
    IL_00de:  dup
    IL_00df:  stloc.0
    IL_00e0:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_00e5:  ldloca.s   V_1
    IL_00e7:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_00ec:  ldarg.0
    IL_00ed:  ldfld      ""int Test.<MainAsync>d__1.<a>5__2""
    IL_00f2:  call       ""void System.Console.WriteLine(int)""
    IL_00f7:  leave.s    IL_0110
  }
  catch System.Exception
  {
    IL_00f9:  stloc.3
    IL_00fa:  ldarg.0
    IL_00fb:  ldc.i4.s   -2
    IL_00fd:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_0102:  ldarg.0
    IL_0103:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<MainAsync>d__1.<>t__builder""
    IL_0108:  ldloc.3
    IL_0109:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_010e:  leave.s    IL_0123
  }
  IL_0110:  ldarg.0
  IL_0111:  ldc.i4.s   -2
  IL_0113:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
  IL_0118:  ldarg.0
  IL_0119:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<MainAsync>d__1.<>t__builder""
  IL_011e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0123:  ret
}");

            verifier = CompileAndVerify(text, options: TestOptions.DebugExe, expectedOutput: @"2");

            // NOTE: two separate hoisted int locals: 
            //       int Test.<MainAsync>d__1.<a>5__1  and  
            //       int Test.<MainAsync>d__1.<b>5__2
            verifier.VerifyIL("Test.<MainAsync>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()",
@"
{
  // Code size      331 (0x14b)
  .maxstack  3
  .locals init (int V_0,
                bool V_1,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                System.Runtime.CompilerServices.YieldAwaitable V_3,
                Test.<MainAsync>d__1 V_4,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<MainAsync>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0019
    IL_0012:  br.s       IL_006f
    IL_0014:  br         IL_00e8
    IL_0019:  nop
    IL_001a:  ldarg.0
    IL_001b:  ldfld      ""string[] Test.<MainAsync>d__1.args""
    IL_0020:  ldlen
    IL_0021:  ldc.i4.0
    IL_0022:  cgt.un
    IL_0024:  stloc.1
    IL_0025:  ldloc.1
    IL_0026:  brfalse.s  IL_00a2
    IL_0028:  nop
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.1
    IL_002b:  stfld      ""int Test.<MainAsync>d__1.<a>5__1""
    IL_0030:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0035:  stloc.3
    IL_0036:  ldloca.s   V_3
    IL_0038:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_003d:  stloc.2
    IL_003e:  ldloca.s   V_2
    IL_0040:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0045:  brtrue.s   IL_008b
    IL_0047:  ldarg.0
    IL_0048:  ldc.i4.0
    IL_0049:  dup
    IL_004a:  stloc.0
    IL_004b:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_0050:  ldarg.0
    IL_0051:  ldloc.2
    IL_0052:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_0057:  ldarg.0
    IL_0058:  stloc.s    V_4
    IL_005a:  ldarg.0
    IL_005b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<MainAsync>d__1.<>t__builder""
    IL_0060:  ldloca.s   V_2
    IL_0062:  ldloca.s   V_4
    IL_0064:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Test.<MainAsync>d__1>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Test.<MainAsync>d__1)""
    IL_0069:  nop
    IL_006a:  leave      IL_014a
    IL_006f:  ldarg.0
    IL_0070:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_0075:  stloc.2
    IL_0076:  ldarg.0
    IL_0077:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_007c:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_0082:  ldarg.0
    IL_0083:  ldc.i4.m1
    IL_0084:  dup
    IL_0085:  stloc.0
    IL_0086:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_008b:  ldloca.s   V_2
    IL_008d:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_0092:  nop
    IL_0093:  ldarg.0
    IL_0094:  ldfld      ""int Test.<MainAsync>d__1.<a>5__1""
    IL_0099:  call       ""void System.Console.WriteLine(int)""
    IL_009e:  nop
    IL_009f:  nop
    IL_00a0:  br.s       IL_011a
    IL_00a2:  nop
    IL_00a3:  ldarg.0
    IL_00a4:  ldc.i4.2
    IL_00a5:  stfld      ""int Test.<MainAsync>d__1.<b>5__2""
    IL_00aa:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_00af:  stloc.3
    IL_00b0:  ldloca.s   V_3
    IL_00b2:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_00b7:  stloc.s    V_5
    IL_00b9:  ldloca.s   V_5
    IL_00bb:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_00c0:  brtrue.s   IL_0105
    IL_00c2:  ldarg.0
    IL_00c3:  ldc.i4.1
    IL_00c4:  dup
    IL_00c5:  stloc.0
    IL_00c6:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_00cb:  ldarg.0
    IL_00cc:  ldloc.s    V_5
    IL_00ce:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_00d3:  ldarg.0
    IL_00d4:  stloc.s    V_4
    IL_00d6:  ldarg.0
    IL_00d7:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<MainAsync>d__1.<>t__builder""
    IL_00dc:  ldloca.s   V_5
    IL_00de:  ldloca.s   V_4
    IL_00e0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Test.<MainAsync>d__1>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Test.<MainAsync>d__1)""
    IL_00e5:  nop
    IL_00e6:  leave.s    IL_014a
    IL_00e8:  ldarg.0
    IL_00e9:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_00ee:  stloc.s    V_5
    IL_00f0:  ldarg.0
    IL_00f1:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_00f6:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_00fc:  ldarg.0
    IL_00fd:  ldc.i4.m1
    IL_00fe:  dup
    IL_00ff:  stloc.0
    IL_0100:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_0105:  ldloca.s   V_5
    IL_0107:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_010c:  nop
    IL_010d:  ldarg.0
    IL_010e:  ldfld      ""int Test.<MainAsync>d__1.<b>5__2""
    IL_0113:  call       ""void System.Console.WriteLine(int)""
    IL_0118:  nop
    IL_0119:  nop
    IL_011a:  leave.s    IL_0136
  }
  catch System.Exception
  {
    IL_011c:  stloc.s    V_6
    IL_011e:  ldarg.0
    IL_011f:  ldc.i4.s   -2
    IL_0121:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_0126:  ldarg.0
    IL_0127:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<MainAsync>d__1.<>t__builder""
    IL_012c:  ldloc.s    V_6
    IL_012e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0133:  nop
    IL_0134:  leave.s    IL_014a
  }
  IL_0136:  ldarg.0
  IL_0137:  ldc.i4.s   -2
  IL_0139:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
  IL_013e:  ldarg.0
  IL_013f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<MainAsync>d__1.<>t__builder""
  IL_0144:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0149:  nop
  IL_014a:  ret
}");
        }

        [WorkItem(15290, "https://github.com/dotnet/roslyn/issues/15290")]
        [Fact]
        public void ReuseLocalsSynthetic()
        {
            var text =
@"

using System;
using System.Threading.Tasks;

class Test
{
    public static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();
    private static async Task MainAsync(string[] args)
    {
        if (args.Length > 0)
        {
            int a = 1;
            await Task.Yield();
            Console.WriteLine(a);
        }
        else
        {
            int b = 2;
            await Task.Yield();
            Console.WriteLine(b);
        }
    }
}";
            var verifier = CompileAndVerify(text, options: TestOptions.ReleaseExe, expectedOutput: @"2");

            // NOTE: only one hoisted int local:  
            //       int Test.<MainAsync>d__1.<a>5__2
            verifier.VerifyIL("Test.<MainAsync>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()",
@"
{
  // Code size      292 (0x124)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<MainAsync>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_005b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00c9
    IL_0011:  ldarg.0
    IL_0012:  ldfld      ""string[] Test.<MainAsync>d__1.args""
    IL_0017:  ldlen
    IL_0018:  brfalse.s  IL_008b
    IL_001a:  ldarg.0
    IL_001b:  ldc.i4.1
    IL_001c:  stfld      ""int Test.<MainAsync>d__1.<a>5__2""
    IL_0021:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0026:  stloc.2
    IL_0027:  ldloca.s   V_2
    IL_0029:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_002e:  stloc.1
    IL_002f:  ldloca.s   V_1
    IL_0031:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0036:  brtrue.s   IL_0077
    IL_0038:  ldarg.0
    IL_0039:  ldc.i4.0
    IL_003a:  dup
    IL_003b:  stloc.0
    IL_003c:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_0041:  ldarg.0
    IL_0042:  ldloc.1
    IL_0043:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_0048:  ldarg.0
    IL_0049:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<MainAsync>d__1.<>t__builder""
    IL_004e:  ldloca.s   V_1
    IL_0050:  ldarg.0
    IL_0051:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Test.<MainAsync>d__1>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Test.<MainAsync>d__1)""
    IL_0056:  leave      IL_0123
    IL_005b:  ldarg.0
    IL_005c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_0061:  stloc.1
    IL_0062:  ldarg.0
    IL_0063:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_0068:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_006e:  ldarg.0
    IL_006f:  ldc.i4.m1
    IL_0070:  dup
    IL_0071:  stloc.0
    IL_0072:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_0077:  ldloca.s   V_1
    IL_0079:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_007e:  ldarg.0
    IL_007f:  ldfld      ""int Test.<MainAsync>d__1.<a>5__2""
    IL_0084:  call       ""void System.Console.WriteLine(int)""
    IL_0089:  br.s       IL_00f7
    IL_008b:  ldarg.0
    IL_008c:  ldc.i4.2
    IL_008d:  stfld      ""int Test.<MainAsync>d__1.<a>5__2""
    IL_0092:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0097:  stloc.2
    IL_0098:  ldloca.s   V_2
    IL_009a:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_009f:  stloc.1
    IL_00a0:  ldloca.s   V_1
    IL_00a2:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_00a7:  brtrue.s   IL_00e5
    IL_00a9:  ldarg.0
    IL_00aa:  ldc.i4.1
    IL_00ab:  dup
    IL_00ac:  stloc.0
    IL_00ad:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_00b2:  ldarg.0
    IL_00b3:  ldloc.1
    IL_00b4:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_00b9:  ldarg.0
    IL_00ba:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<MainAsync>d__1.<>t__builder""
    IL_00bf:  ldloca.s   V_1
    IL_00c1:  ldarg.0
    IL_00c2:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Test.<MainAsync>d__1>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Test.<MainAsync>d__1)""
    IL_00c7:  leave.s    IL_0123
    IL_00c9:  ldarg.0
    IL_00ca:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_00cf:  stloc.1
    IL_00d0:  ldarg.0
    IL_00d1:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_00d6:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_00dc:  ldarg.0
    IL_00dd:  ldc.i4.m1
    IL_00de:  dup
    IL_00df:  stloc.0
    IL_00e0:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_00e5:  ldloca.s   V_1
    IL_00e7:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_00ec:  ldarg.0
    IL_00ed:  ldfld      ""int Test.<MainAsync>d__1.<a>5__2""
    IL_00f2:  call       ""void System.Console.WriteLine(int)""
    IL_00f7:  leave.s    IL_0110
  }
  catch System.Exception
  {
    IL_00f9:  stloc.3
    IL_00fa:  ldarg.0
    IL_00fb:  ldc.i4.s   -2
    IL_00fd:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_0102:  ldarg.0
    IL_0103:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<MainAsync>d__1.<>t__builder""
    IL_0108:  ldloc.3
    IL_0109:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_010e:  leave.s    IL_0123
  }
  IL_0110:  ldarg.0
  IL_0111:  ldc.i4.s   -2
  IL_0113:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
  IL_0118:  ldarg.0
  IL_0119:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<MainAsync>d__1.<>t__builder""
  IL_011e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0123:  ret
}");

            verifier = CompileAndVerify(text, options: TestOptions.DebugExe, expectedOutput: @"2");

            // NOTE: two separate hoisted int locals: 
            //       int Test.<MainAsync>d__1.<a>5__1  and  
            //       int Test.<MainAsync>d__1.<b>5__2
            verifier.VerifyIL("Test.<MainAsync>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()",
@"
{
  // Code size      331 (0x14b)
  .maxstack  3
  .locals init (int V_0,
                bool V_1,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                System.Runtime.CompilerServices.YieldAwaitable V_3,
                Test.<MainAsync>d__1 V_4,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<MainAsync>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0019
    IL_0012:  br.s       IL_006f
    IL_0014:  br         IL_00e8
    IL_0019:  nop
    IL_001a:  ldarg.0
    IL_001b:  ldfld      ""string[] Test.<MainAsync>d__1.args""
    IL_0020:  ldlen
    IL_0021:  ldc.i4.0
    IL_0022:  cgt.un
    IL_0024:  stloc.1
    IL_0025:  ldloc.1
    IL_0026:  brfalse.s  IL_00a2
    IL_0028:  nop
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.1
    IL_002b:  stfld      ""int Test.<MainAsync>d__1.<a>5__1""
    IL_0030:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0035:  stloc.3
    IL_0036:  ldloca.s   V_3
    IL_0038:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_003d:  stloc.2
    IL_003e:  ldloca.s   V_2
    IL_0040:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0045:  brtrue.s   IL_008b
    IL_0047:  ldarg.0
    IL_0048:  ldc.i4.0
    IL_0049:  dup
    IL_004a:  stloc.0
    IL_004b:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_0050:  ldarg.0
    IL_0051:  ldloc.2
    IL_0052:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_0057:  ldarg.0
    IL_0058:  stloc.s    V_4
    IL_005a:  ldarg.0
    IL_005b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<MainAsync>d__1.<>t__builder""
    IL_0060:  ldloca.s   V_2
    IL_0062:  ldloca.s   V_4
    IL_0064:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Test.<MainAsync>d__1>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Test.<MainAsync>d__1)""
    IL_0069:  nop
    IL_006a:  leave      IL_014a
    IL_006f:  ldarg.0
    IL_0070:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_0075:  stloc.2
    IL_0076:  ldarg.0
    IL_0077:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_007c:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_0082:  ldarg.0
    IL_0083:  ldc.i4.m1
    IL_0084:  dup
    IL_0085:  stloc.0
    IL_0086:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_008b:  ldloca.s   V_2
    IL_008d:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_0092:  nop
    IL_0093:  ldarg.0
    IL_0094:  ldfld      ""int Test.<MainAsync>d__1.<a>5__1""
    IL_0099:  call       ""void System.Console.WriteLine(int)""
    IL_009e:  nop
    IL_009f:  nop
    IL_00a0:  br.s       IL_011a
    IL_00a2:  nop
    IL_00a3:  ldarg.0
    IL_00a4:  ldc.i4.2
    IL_00a5:  stfld      ""int Test.<MainAsync>d__1.<b>5__2""
    IL_00aa:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_00af:  stloc.3
    IL_00b0:  ldloca.s   V_3
    IL_00b2:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_00b7:  stloc.s    V_5
    IL_00b9:  ldloca.s   V_5
    IL_00bb:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_00c0:  brtrue.s   IL_0105
    IL_00c2:  ldarg.0
    IL_00c3:  ldc.i4.1
    IL_00c4:  dup
    IL_00c5:  stloc.0
    IL_00c6:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_00cb:  ldarg.0
    IL_00cc:  ldloc.s    V_5
    IL_00ce:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_00d3:  ldarg.0
    IL_00d4:  stloc.s    V_4
    IL_00d6:  ldarg.0
    IL_00d7:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<MainAsync>d__1.<>t__builder""
    IL_00dc:  ldloca.s   V_5
    IL_00de:  ldloca.s   V_4
    IL_00e0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Test.<MainAsync>d__1>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Test.<MainAsync>d__1)""
    IL_00e5:  nop
    IL_00e6:  leave.s    IL_014a
    IL_00e8:  ldarg.0
    IL_00e9:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_00ee:  stloc.s    V_5
    IL_00f0:  ldarg.0
    IL_00f1:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Test.<MainAsync>d__1.<>u__1""
    IL_00f6:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_00fc:  ldarg.0
    IL_00fd:  ldc.i4.m1
    IL_00fe:  dup
    IL_00ff:  stloc.0
    IL_0100:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_0105:  ldloca.s   V_5
    IL_0107:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_010c:  nop
    IL_010d:  ldarg.0
    IL_010e:  ldfld      ""int Test.<MainAsync>d__1.<b>5__2""
    IL_0113:  call       ""void System.Console.WriteLine(int)""
    IL_0118:  nop
    IL_0119:  nop
    IL_011a:  leave.s    IL_0136
  }
  catch System.Exception
  {
    IL_011c:  stloc.s    V_6
    IL_011e:  ldarg.0
    IL_011f:  ldc.i4.s   -2
    IL_0121:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
    IL_0126:  ldarg.0
    IL_0127:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<MainAsync>d__1.<>t__builder""
    IL_012c:  ldloc.s    V_6
    IL_012e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0133:  nop
    IL_0134:  leave.s    IL_014a
  }
  IL_0136:  ldarg.0
  IL_0137:  ldc.i4.s   -2
  IL_0139:  stfld      ""int Test.<MainAsync>d__1.<>1__state""
  IL_013e:  ldarg.0
  IL_013f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Test.<MainAsync>d__1.<>t__builder""
  IL_0144:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0149:  nop
  IL_014a:  ret
}");
        }
    }
}
