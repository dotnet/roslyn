// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private CSharpCompilation CreateCompilation(string source, IEnumerable<MetadataReference> references = null, CSharpCompilationOptions options = null)
        {
            SynchronizationContext.SetSynchronizationContext(null);

            options = options ?? TestOptions.ReleaseExe;

            IEnumerable<MetadataReference> asyncRefs = new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef };
            references = (references != null) ? references.Concat(asyncRefs) : asyncRefs;

            return CreateCompilationWithMscorlib45(source, options: options, references: references);
        }

        private CompilationVerifier CompileAndVerify(string source, string expectedOutput, IEnumerable<MetadataReference> references = null, EmitOptions emitOptions = EmitOptions.All, CSharpCompilationOptions options = null)
        {
            SynchronizationContext.SetSynchronizationContext(null);

            var compilation = this.CreateCompilation(source, references: references, options: options);
            return base.CompileAndVerify(compilation, expectedOutput: expectedOutput, emitOptions: emitOptions);
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
        public void AsyncWithParamsAndLocals_UnHoisted()
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
        public void AsyncWithParamsAndLocals_Hoisted()
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
            var stateMachineClass = (NamedTypeSymbol)testClass.GetMembers().Single(s => s.Name.StartsWith("<Run>"));
            IEnumerable<IGrouping<TypeSymbol, FieldSymbol>> spillFieldsByType = stateMachineClass.GetMembers().Where(m => m.Kind == SymbolKind.Field && m.Name.StartsWith("<>7__wrap")).Cast<FieldSymbol>().GroupBy(x => x.Type);

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

            var actual = GetFieldLoadsAndStores(c, "Test<U>.<M>d__1<S, T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext");

            // make sure we are reusing synthesized iterator locals and that the locals are nulled:
            AssertEx.AssertEqualToleratingWhitespaceDifferences(@"
IL_0000:  ldarg.0
IL_0001:  ldfld      ""int Test<U>.<M>d__1<S, T>.<>1__state""
IL_002b:  callvirt   ""System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator()""
IL_0030:  stfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__1<S, T>.<>7__wrap1""
IL_003c:  ldarg.0
IL_003d:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__1<S, T>.<>7__wrap1""
IL_0060:  stloc.0
IL_0061:  stfld      ""int Test<U>.<M>d__1<S, T>.<>1__state""
IL_0067:  ldloc.1
IL_0068:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__1<S, T>.<>u__$awaiter0""
IL_006d:  ldarg.0
IL_006e:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__1<S, T>.<>t__builder""
IL_0080:  ldarg.0
IL_0081:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__1<S, T>.<>u__$awaiter0""
IL_0087:  ldarg.0
IL_0088:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__1<S, T>.<>u__$awaiter0""
IL_0096:  stloc.0
IL_0097:  stfld      ""int Test<U>.<M>d__1<S, T>.<>1__state""
IL_00ac:  ldarg.0
IL_00ad:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__1<S, T>.<>7__wrap1""
IL_00bf:  ldarg.0
IL_00c0:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__1<S, T>.<>7__wrap1""
IL_00c7:  ldarg.0
IL_00c8:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__1<S, T>.<>7__wrap1""
IL_00d4:  ldnull
IL_00d5:  stfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__1<S, T>.<>7__wrap1""
IL_00e0:  callvirt   ""System.Collections.Generic.IEnumerator<S> System.Collections.Generic.IEnumerable<S>.GetEnumerator()""
IL_00e5:  stfld      ""System.Collections.Generic.IEnumerator<S> Test<U>.<M>d__1<S, T>.<>7__wrap2""
IL_00f1:  ldarg.0
IL_00f2:  ldfld      ""System.Collections.Generic.IEnumerator<S> Test<U>.<M>d__1<S, T>.<>7__wrap2""
IL_0115:  stloc.0
IL_0116:  stfld      ""int Test<U>.<M>d__1<S, T>.<>1__state""
IL_011c:  ldloc.1
IL_011d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__1<S, T>.<>u__$awaiter0""
IL_0122:  ldarg.0
IL_0123:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__1<S, T>.<>t__builder""
IL_0135:  ldarg.0
IL_0136:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__1<S, T>.<>u__$awaiter0""
IL_013c:  ldarg.0
IL_013d:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__1<S, T>.<>u__$awaiter0""
IL_014b:  stloc.0
IL_014c:  stfld      ""int Test<U>.<M>d__1<S, T>.<>1__state""
IL_0161:  ldarg.0
IL_0162:  ldfld      ""System.Collections.Generic.IEnumerator<S> Test<U>.<M>d__1<S, T>.<>7__wrap2""
IL_0174:  ldarg.0
IL_0175:  ldfld      ""System.Collections.Generic.IEnumerator<S> Test<U>.<M>d__1<S, T>.<>7__wrap2""
IL_017c:  ldarg.0
IL_017d:  ldfld      ""System.Collections.Generic.IEnumerator<S> Test<U>.<M>d__1<S, T>.<>7__wrap2""
IL_0189:  ldnull
IL_018a:  stfld      ""System.Collections.Generic.IEnumerator<S> Test<U>.<M>d__1<S, T>.<>7__wrap2""
IL_0195:  callvirt   ""System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator()""
IL_019a:  stfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__1<S, T>.<>7__wrap1""
IL_01a6:  ldarg.0
IL_01a7:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__1<S, T>.<>7__wrap1""
IL_01ca:  stloc.0
IL_01cb:  stfld      ""int Test<U>.<M>d__1<S, T>.<>1__state""
IL_01d1:  ldloc.1
IL_01d2:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__1<S, T>.<>u__$awaiter0""
IL_01d7:  ldarg.0
IL_01d8:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__1<S, T>.<>t__builder""
IL_01ea:  ldarg.0
IL_01eb:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__1<S, T>.<>u__$awaiter0""
IL_01f1:  ldarg.0
IL_01f2:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__1<S, T>.<>u__$awaiter0""
IL_0200:  stloc.0
IL_0201:  stfld      ""int Test<U>.<M>d__1<S, T>.<>1__state""
IL_0216:  ldarg.0
IL_0217:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__1<S, T>.<>7__wrap1""
IL_0229:  ldarg.0
IL_022a:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__1<S, T>.<>7__wrap1""
IL_0231:  ldarg.0
IL_0232:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__1<S, T>.<>7__wrap1""
IL_023e:  ldnull
IL_023f:  stfld      ""System.Collections.Generic.IEnumerator<T> Test<U>.<M>d__1<S, T>.<>7__wrap1""
IL_024a:  callvirt   ""System.Collections.Generic.IEnumerator<U> System.Collections.Generic.IEnumerable<U>.GetEnumerator()""
IL_024f:  stfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__1<S, T>.<>7__wrap3""
IL_025b:  ldarg.0
IL_025c:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__1<S, T>.<>7__wrap3""
IL_027f:  stloc.0
IL_0280:  stfld      ""int Test<U>.<M>d__1<S, T>.<>1__state""
IL_0286:  ldloc.1
IL_0287:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__1<S, T>.<>u__$awaiter0""
IL_028c:  ldarg.0
IL_028d:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__1<S, T>.<>t__builder""
IL_029f:  ldarg.0
IL_02a0:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__1<S, T>.<>u__$awaiter0""
IL_02a6:  ldarg.0
IL_02a7:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__1<S, T>.<>u__$awaiter0""
IL_02b5:  stloc.0
IL_02b6:  stfld      ""int Test<U>.<M>d__1<S, T>.<>1__state""
IL_02cb:  ldarg.0
IL_02cc:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__1<S, T>.<>7__wrap3""
IL_02de:  ldarg.0
IL_02df:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__1<S, T>.<>7__wrap3""
IL_02e6:  ldarg.0
IL_02e7:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__1<S, T>.<>7__wrap3""
IL_02f3:  ldnull
IL_02f4:  stfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__1<S, T>.<>7__wrap3""
IL_02ff:  callvirt   ""System.Collections.Generic.IEnumerator<U> System.Collections.Generic.IEnumerable<U>.GetEnumerator()""
IL_0304:  stfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__1<S, T>.<>7__wrap3""
IL_0310:  ldarg.0
IL_0311:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__1<S, T>.<>7__wrap3""
IL_0334:  stloc.0
IL_0335:  stfld      ""int Test<U>.<M>d__1<S, T>.<>1__state""
IL_033b:  ldloc.1
IL_033c:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__1<S, T>.<>u__$awaiter0""
IL_0341:  ldarg.0
IL_0342:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__1<S, T>.<>t__builder""
IL_0354:  ldarg.0
IL_0355:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__1<S, T>.<>u__$awaiter0""
IL_035b:  ldarg.0
IL_035c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test<U>.<M>d__1<S, T>.<>u__$awaiter0""
IL_036a:  stloc.0
IL_036b:  stfld      ""int Test<U>.<M>d__1<S, T>.<>1__state""
IL_0380:  ldarg.0
IL_0381:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__1<S, T>.<>7__wrap3""
IL_0393:  ldarg.0
IL_0394:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__1<S, T>.<>7__wrap3""
IL_039b:  ldarg.0
IL_039c:  ldfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__1<S, T>.<>7__wrap3""
IL_03a8:  ldnull
IL_03a9:  stfld      ""System.Collections.Generic.IEnumerator<U> Test<U>.<M>d__1<S, T>.<>7__wrap3""
IL_03b2:  ldc.i4.s   -2
IL_03b4:  stfld      ""int Test<U>.<M>d__1<S, T>.<>1__state""
IL_03b9:  ldarg.0
IL_03ba:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__1<S, T>.<>t__builder""
IL_03c8:  ldc.i4.s   -2
IL_03ca:  stfld      ""int Test<U>.<M>d__1<S, T>.<>1__state""
IL_03cf:  ldarg.0
IL_03d0:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test<U>.<M>d__1<S, T>.<>t__builder""
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

            var actual = GetFieldLoadsAndStores(c, "Test.<M>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext");

            // make sure we are reusing synthesized iterator locals and that the locals are nulled:
            AssertEx.AssertEqualToleratingWhitespaceDifferences(@"
IL_0000:  ldarg.0
IL_0001:  ldfld      ""int Test.<M>d__1.<>1__state""
IL_001f:  callvirt   ""System.Collections.Generic.IEnumerator<dynamic> System.Collections.Generic.IEnumerable<dynamic>.GetEnumerator()""
IL_0024:  stfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__1.<>7__wrap1""
IL_0030:  ldarg.0
IL_0031:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__1.<>7__wrap1""
IL_0054:  stloc.0
IL_0055:  stfld      ""int Test.<M>d__1.<>1__state""
IL_005b:  ldloc.1
IL_005c:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<M>d__1.<>u__$awaiter0""
IL_0061:  ldarg.0
IL_0062:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<M>d__1.<>t__builder""
IL_0074:  ldarg.0
IL_0075:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<M>d__1.<>u__$awaiter0""
IL_007b:  ldarg.0
IL_007c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<M>d__1.<>u__$awaiter0""
IL_008a:  stloc.0
IL_008b:  stfld      ""int Test.<M>d__1.<>1__state""
IL_00a0:  ldarg.0
IL_00a1:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__1.<>7__wrap1""
IL_00b3:  ldarg.0
IL_00b4:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__1.<>7__wrap1""
IL_00bb:  ldarg.0
IL_00bc:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__1.<>7__wrap1""
IL_00c8:  ldnull
IL_00c9:  stfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__1.<>7__wrap1""
IL_00d4:  callvirt   ""System.Collections.Generic.IEnumerator<object> System.Collections.Generic.IEnumerable<object>.GetEnumerator()""
IL_00d9:  stfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__1.<>7__wrap1""
IL_00e5:  ldarg.0
IL_00e6:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__1.<>7__wrap1""
IL_0109:  stloc.0
IL_010a:  stfld      ""int Test.<M>d__1.<>1__state""
IL_0110:  ldloc.1
IL_0111:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<M>d__1.<>u__$awaiter0""
IL_0116:  ldarg.0
IL_0117:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<M>d__1.<>t__builder""
IL_0129:  ldarg.0
IL_012a:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<M>d__1.<>u__$awaiter0""
IL_0130:  ldarg.0
IL_0131:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<M>d__1.<>u__$awaiter0""
IL_013f:  stloc.0
IL_0140:  stfld      ""int Test.<M>d__1.<>1__state""
IL_0155:  ldarg.0
IL_0156:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__1.<>7__wrap1""
IL_0168:  ldarg.0
IL_0169:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__1.<>7__wrap1""
IL_0170:  ldarg.0
IL_0171:  ldfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__1.<>7__wrap1""
IL_017d:  ldnull
IL_017e:  stfld      ""System.Collections.Generic.IEnumerator<dynamic> Test.<M>d__1.<>7__wrap1""
IL_0187:  ldc.i4.s   -2
IL_0189:  stfld      ""int Test.<M>d__1.<>1__state""
IL_018e:  ldarg.0
IL_018f:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<M>d__1.<>t__builder""
IL_019d:  ldc.i4.s   -2
IL_019f:  stfld      ""int Test.<M>d__1.<>1__state""
IL_01a4:  ldarg.0
IL_01a5:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Test.<M>d__1.<>t__builder""
", actual);
        }
    }
}
