// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            SynchronizationContext.SetSynchronizationContext(null);
        }

        private CompilationVerifier CompileAndVerify(string source, string expectedOutput = null, IEnumerable<MetadataReference> references = null, CSharpCompilationOptions options = null)
        {
            references = (references != null) ? references.Concat(s_asyncRefs) : s_asyncRefs;
            return base.CompileAndVerify(source, expectedOutput: expectedOutput, additionalRefs: references, options: options);
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
            CompileAndVerify(source, additionalRefs: s_asyncRefs, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
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

            CompileAndVerify(source, additionalRefs: s_asyncRefs, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
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

        [Fact]
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
            CompileAndVerify(source, s_asyncRefs, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
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

            var vd = CompileAndVerify(source, s_asyncRefs, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
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
          <slot kind=""28"" offset=""281"" />
          <slot kind=""28"" offset=""281"" ordinal=""1"" />
          <slot kind=""28"" offset=""281"" ordinal=""2"" />
          <slot kind=""28"" offset=""261"" ordinal=""2"" />
          <slot kind=""4"" offset=""307"" />
          <slot kind=""4"" offset=""376"" />
          <slot kind=""3"" offset=""410"" />
          <slot kind=""2"" offset=""410"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
");
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
        return await Foo();
    }

    public async Task<int> Foo()
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
        yield return Foo();
    }

    public int Foo()
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
            CompileAndVerify(source, expectedOutput: expected);
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
            IEnumerable<IGrouping<TypeSymbol, FieldSymbol>> spillFieldsByType = stateMachineClass.GetMembers().Where(m => m.Kind == SymbolKind.Field && m.Name.StartsWith("<>7__wrap", StringComparison.Ordinal)).Cast<FieldSymbol>().GroupBy(x => x.Type.TypeSymbol);

            Assert.Equal(1, spillFieldsByType.Count());
            Assert.Equal(1, spillFieldsByType.Single(x => x.Key == comp.GetSpecialType(SpecialType.System_Int32)).Count());
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
IL_00a7:  ldarg.0
IL_00a8:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_00ba:  ldarg.0
IL_00bb:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_00c2:  ldarg.0
IL_00c3:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_00cf:  ldnull
IL_00d0:  stfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_00db:  callvirt   ""System.Collections.Generic.IEnumerator<S> System.Collections.Generic.IEnumerable<S>.GetEnumerator()""
IL_00e0:  stfld      ""System.Collections.Generic.IEnumerator<S> Test<U>.<M>d__2<S, T>.<>7__wrap2""
IL_00ec:  ldarg.0
IL_00ed:  ldfld      ""System.Collections.Generic.IEnumerator<S> Test<U>.<M>d__2<S, T>.<>7__wrap2""
IL_0110:  stloc.0
IL_0111:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_0117:  ldloc.1
IL_0118:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_011d:  ldarg.0
IL_011e:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__2<S, T>.<>t__builder""
IL_0130:  ldarg.0
IL_0131:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_0137:  ldarg.0
IL_0138:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_0146:  stloc.0
IL_0147:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_015c:  ldarg.0
IL_015d:  ldfld      ""System.Collections.Generic.IEnumerator<S> Test<U>.<M>d__2<S, T>.<>7__wrap2""
IL_016f:  ldarg.0
IL_0170:  ldfld      ""System.Collections.Generic.IEnumerator<S> Test<U>.<M>d__2<S, T>.<>7__wrap2""
IL_0177:  ldarg.0
IL_0178:  ldfld      ""System.Collections.Generic.IEnumerator<S> Test<U>.<M>d__2<S, T>.<>7__wrap2""
IL_0184:  ldnull
IL_0185:  stfld      ""System.Collections.Generic.IEnumerator<S> Test<U>.<M>d__2<S, T>.<>7__wrap2""
IL_0190:  callvirt   ""System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator()""
IL_0195:  stfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_01a1:  ldarg.0
IL_01a2:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_01c5:  stloc.0
IL_01c6:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_01cc:  ldloc.1
IL_01cd:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_01d2:  ldarg.0
IL_01d3:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__2<S, T>.<>t__builder""
IL_01e5:  ldarg.0
IL_01e6:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_01ec:  ldarg.0
IL_01ed:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_01fb:  stloc.0
IL_01fc:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_0211:  ldarg.0
IL_0212:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_0224:  ldarg.0
IL_0225:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_022c:  ldarg.0
IL_022d:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_0239:  ldnull
IL_023a:  stfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__2<S, T>.<>7__wrap1""
IL_0245:  callvirt   ""System.Collections.Generic.IEnumerator<U> System.Collections.Generic.IEnumerable<U>.GetEnumerator()""
IL_024a:  stfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_0256:  ldarg.0
IL_0257:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_027a:  stloc.0
IL_027b:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_0281:  ldloc.1
IL_0282:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_0287:  ldarg.0
IL_0288:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__2<S, T>.<>t__builder""
IL_029a:  ldarg.0
IL_029b:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_02a1:  ldarg.0
IL_02a2:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_02b0:  stloc.0
IL_02b1:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_02c6:  ldarg.0
IL_02c7:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_02d9:  ldarg.0
IL_02da:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_02e1:  ldarg.0
IL_02e2:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_02ee:  ldnull
IL_02ef:  stfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_02fa:  callvirt   ""System.Collections.Generic.IEnumerator<U> System.Collections.Generic.IEnumerable<U>.GetEnumerator()""
IL_02ff:  stfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_030b:  ldarg.0
IL_030c:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_032f:  stloc.0
IL_0330:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_0336:  ldloc.1
IL_0337:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_033c:  ldarg.0
IL_033d:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__2<S, T>.<>t__builder""
IL_034f:  ldarg.0
IL_0350:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_0356:  ldarg.0
IL_0357:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__2<S, T>.<>u__1""
IL_0365:  stloc.0
IL_0366:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_037b:  ldarg.0
IL_037c:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_038e:  ldarg.0
IL_038f:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_0396:  ldarg.0
IL_0397:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_03a3:  ldnull
IL_03a4:  stfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__2<S, T>.<>7__wrap3""
IL_03ad:  ldc.i4.s   -2
IL_03af:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_03b4:  ldarg.0
IL_03b5:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__2<S, T>.<>t__builder""
IL_03c3:  ldc.i4.s   -2
IL_03c5:  stfld      ""int Test<U>.<M>d__2<S, T>.<>1__state""
IL_03ca:  ldarg.0
IL_03cb:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__2<S, T>.<>t__builder""
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
IL_0097:  ldarg.0
IL_0098:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_00aa:  ldarg.0
IL_00ab:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_00b2:  ldarg.0
IL_00b3:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_00bf:  ldnull
IL_00c0:  stfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_00cb:  callvirt   ""System.Collections.Generic.IEnumerator<object> System.Collections.Generic.IEnumerable<object>.GetEnumerator()""
IL_00d0:  stfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_00dc:  ldarg.0
IL_00dd:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_0100:  stloc.0
IL_0101:  stfld      ""int Test.<M>d__3.<>1__state""
IL_0107:  ldloc.1
IL_0108:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<M>d__3.<>u__1""
IL_010d:  ldarg.0
IL_010e:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<M>d__3.<>t__builder""
IL_0120:  ldarg.0
IL_0121:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<M>d__3.<>u__1""
IL_0127:  ldarg.0
IL_0128:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<M>d__3.<>u__1""
IL_0136:  stloc.0
IL_0137:  stfld      ""int Test.<M>d__3.<>1__state""
IL_014c:  ldarg.0
IL_014d:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_015f:  ldarg.0
IL_0160:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_0167:  ldarg.0
IL_0168:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_0174:  ldnull
IL_0175:  stfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__3.<>7__wrap1""
IL_017e:  ldc.i4.s   -2
IL_0180:  stfld      ""int Test.<M>d__3.<>1__state""
IL_0185:  ldarg.0
IL_0186:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<M>d__3.<>t__builder""
IL_0194:  ldc.i4.s   -2
IL_0196:  stfld      ""int Test.<M>d__3.<>1__state""
IL_019b:  ldarg.0
IL_019c:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<M>d__3.<>t__builder""
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
            CompileAndVerify(source, additionalRefs: s_asyncRefs, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
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
    }
}
