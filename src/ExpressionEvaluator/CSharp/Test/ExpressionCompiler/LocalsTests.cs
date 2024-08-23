// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.DiaSymReader;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class LocalsTests : ExpressionCompilerTestBase
    {
        [Fact]
        public void NoLocals()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.Equal(0, assembly.Count);
                Assert.Equal(0, locals.Count);
                locals.Free();
            });
        }

        [Fact]
        public void Locals()
        {
            var source =
@"class C
{
    void M(int[] a)
    {
        string b;
        a[1]++;
        lock (new C())
        {
#line 999
            int c = 3;
            b = a[c].ToString();
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M", atLineNumber: 999);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(4, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
    @"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0, //b
                C V_1,
                bool V_2,
                int V_3) //c
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "a", expectedILOpt:
    @"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0, //b
                C V_1,
                bool V_2,
                int V_3) //c
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[2], "<>m2", "b", expectedILOpt:
    @"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0, //b
                C V_1,
                bool V_2,
                int V_3) //c
  IL_0000:  ldloc.0
  IL_0001:  ret
}
");
                VerifyLocal(testData, typeName, locals[3], "<>m3", "c", expectedILOpt:
    @"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0, //b
                C V_1,
                bool V_2,
                int V_3) //c
  IL_0000:  ldloc.3
  IL_0001:  ret
}");
                locals.Free();
            });
        }

        [Fact]
        public void LocalsInSwitch()
        {
            var source =
@"class C
{
    void M(object o)
    {
        switch (o)
        {
            case string s:
                var a = s;
#line 1000
                return;
            case int s:
#line 2000
                return;
            default:
                return;
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(4, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0, //a
                string V_1, //s
                int V_2,
                object V_3,
                object V_4)
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "o", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0, //a
                string V_1, //s
                int V_2,
                object V_3,
                object V_4)
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[2], "<>m2", "a", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0, //a
                string V_1, //s
                int V_2,
                object V_3,
                object V_4)
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[3], "<>m3", "s", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0, //a
                string V_1, //s
                int V_2,
                object V_3,
                object V_4)
  IL_0000:  ldloc.1
  IL_0001:  ret
}");
                locals.Free();

                context = CreateMethodContext(runtime, "C.M", atLineNumber: 2000);

                testData = new CompilationTestData();
                locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(4, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0, //a
                string V_1,
                int V_2, //s
                object V_3,
                object V_4)
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "o", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0, //a
                string V_1,
                int V_2, //s
                object V_3,
                object V_4)
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[2], "<>m2", "a", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0, //a
                string V_1,
                int V_2, //s
                object V_3,
                object V_4)
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[3], "<>m3", "s", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0, //a
                string V_1,
                int V_2, //s
                object V_3,
                object V_4)
  IL_0000:  ldloc.2
  IL_0001:  ret
}");
                locals.Free();
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16594")]
        public void LocalsInSwitchWithLambda()
        {
            var source =
@"class C
{
    System.Action M(object o)
    {
        switch (o)
        {
            case string s:
                var a = s;
#line 1000
                return () =>
                       {
#line 2000
                           System.Console.WriteLine(s + a); 
                       };
            case int s:
#line 3000
                return () =>
                       {
#line 4000
                           System.Console.WriteLine(s); 
                       };
            default:
                return null;
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(3, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                object V_1,
                object V_2,
                System.Action V_3)
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "o", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                object V_1,
                object V_2,
                System.Action V_3)
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[2], "<>m2", "a", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                object V_1,
                object V_2,
                System.Action V_3)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""string C.<>c__DisplayClass0_0.a""
  IL_0006:  ret
}");
                // We should be able to evaluate "s" within this context, https://github.com/dotnet/roslyn/issues/16594.
                //                VerifyLocal(testData, typeName, locals[3], "<>m3", "s", expectedILOpt:
                //@"{
                //  // Code size        8 (0x8)
                //  .maxstack  1
                //  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
                //                object V_1,
                //                string V_2,
                //                int V_3,
                //                object V_4,
                //                object V_5,
                //                int? V_6,
                //                C.<>c__DisplayClass0_0 V_7, //CS$<>8__locals1
                //                System.Action V_8,
                //                C.<>c__DisplayClass0_2 V_9)
                //  IL_0000:  ldloc.s    V_7
                //  IL_0002:  ldfld      ""string C.<>c__DisplayClass0_0.s""
                //  IL_0007:  ret
                //}");
                locals.Free();

                context = CreateMethodContext(runtime, "C.M", atLineNumber: 3000);

                testData = new CompilationTestData();
                locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(3, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                object V_1,
                object V_2,
                System.Action V_3)
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "o", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                object V_1,
                object V_2,
                System.Action V_3)
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[2], "<>m2", "a", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                object V_1,
                object V_2,
                System.Action V_3)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""string C.<>c__DisplayClass0_0.a""
  IL_0006:  ret
}");
                // We should be able to evaluate "s" within this context, https://github.com/dotnet/roslyn/issues/16594.
                //                VerifyLocal(testData, typeName, locals[3], "<>m3", "s", expectedILOpt:
                //@"{
                //  // Code size        8 (0x8)
                //  .maxstack  1
                //  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
                //                object V_1,
                //                string V_2,
                //                int V_3,
                //                object V_4,
                //                object V_5,
                //                int? V_6,
                //                C.<>c__DisplayClass0_0 V_7,
                //                System.Action V_8,
                //                C.<>c__DisplayClass0_2 V_9) //CS$<>8__locals2
                //  IL_0000:  ldloc.s    V_9
                //  IL_0002:  ldfld      ""int C.<>c__DisplayClass0_2.s""
                //  IL_0007:  ret
                //}");
                locals.Free();

                context = CreateMethodContext(runtime, "C.<>c__DisplayClass0_0.<M>b__0", atLineNumber: 2000);

                testData = new CompilationTestData();
                locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(1, locals.Count);
                // We should be able to evaluate "s" within this context, https://github.com/dotnet/roslyn/issues/16594.
                //                VerifyLocal(testData, typeName, locals[0], "<>m0", "s", expectedILOpt:
                //@"{
                //  // Code size        7 (0x7)
                //  .maxstack  1
                //  IL_0000:  ldarg.0
                //  IL_0001:  ldfld      ""string C.<>c__DisplayClass0_0.s""
                //  IL_0006:  ret
                //}");
                VerifyLocal(testData, typeName, locals[0], "<>m0", "a", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""string C.<>c__DisplayClass0_0.a""
  IL_0006:  ret
}");
                locals.Free();

                context = CreateMethodContext(runtime, "C.<>c__DisplayClass0_0.<M>b__1", atLineNumber: 4000);

                testData = new CompilationTestData();
                locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(1, locals.Count);
                // We should be able to evaluate "s" within this context, https://github.com/dotnet/roslyn/issues/16594.
                //                VerifyLocal(testData, typeName, locals[0], "<>m0", "s", expectedILOpt:
                //@"{
                //  // Code size        7 (0x7)
                //  .maxstack  1
                //  IL_0000:  ldarg.0
                //  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_2.s""
                //  IL_0006:  ret
                //}");
                VerifyLocal(testData, typeName, locals[0], "<>m0", "a", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""string C.<>c__DisplayClass0_0.a""
  IL_0006:  ret
}");
                locals.Free();
            });
        }

        [Fact]
        public void LocalsInSwitchWithAwait()
        {
            var source =
@"
using System.Threading.Tasks;

class C
{
    async Task<object> F()
    {
        return new object();
    }

    async Task<object> M(object o)
    {
        switch (o)
        {
            case string s:
                var a = s;
#line 1000
                await F();
                System.Console.WriteLine(s + a); 
                return o;
            case int s:
#line 2000
                await F();
                System.Console.WriteLine(s); 
                return o;
            default:
                return o;
        }
    }
}";
            var compilation0 = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugDll,
                references: new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef });
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>d__1.MoveNext", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(4, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                object V_1,
                object V_2,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_3,
                C.<M>d__1 V_4,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<M>d__1.<>4__this""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "o", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                object V_1,
                object V_2,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_3,
                C.<M>d__1 V_4,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object C.<M>d__1.o""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[2], "<>m2", "a", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                object V_1,
                object V_2,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_3,
                C.<M>d__1 V_4,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""string C.<M>d__1.<a>5__1""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[3], "<>m3", "s", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                object V_1,
                object V_2,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_3,
                C.<M>d__1 V_4,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""string C.<M>d__1.<s>5__2""
  IL_0006:  ret
}");
                locals.Free();

                context = CreateMethodContext(runtime, "C.<M>d__1.MoveNext", atLineNumber: 2000);

                testData = new CompilationTestData();
                locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(4, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                object V_1,
                object V_2,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_3,
                C.<M>d__1 V_4,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<M>d__1.<>4__this""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "o", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                object V_1,
                object V_2,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_3,
                C.<M>d__1 V_4,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object C.<M>d__1.o""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[2], "<>m2", "a", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                object V_1,
                object V_2,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_3,
                C.<M>d__1 V_4,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""string C.<M>d__1.<a>5__1""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[3], "<>m3", "s", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                object V_1,
                object V_2,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_3,
                C.<M>d__1 V_4,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__1.<s>5__3""
  IL_0006:  ret
}");
                locals.Free();
            });
        }

        /// <summary>
        /// No local signature (debugging a .dmp with no heap). Local
        /// names are known but types are not so the locals are dropped.
        /// Expressions that do not involve locals can be evaluated however.
        /// </summary>
        [Fact]
        public void NoLocalSignature()
        {
            var source =
@"class C
{
    void M(int[] a)
    {
        string b;
        a[1]++;
        lock (new C())
        {
#line 999
            int c = 3;
            b = a[c].ToString();
        }
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(comp, references: null, includeLocalSignatures: false, includeIntrinsicAssembly: true, validator: runtime =>
            {
                var context = CreateMethodContext(
                    runtime,
                    methodName: "C.M",
                    atLineNumber: 999);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
    @"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "a", expectedILOpt:
    @"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                locals.Free();

                string error;
                testData = new CompilationTestData();
                context.CompileExpression("b", out error, testData);
                Assert.Equal("error CS0103: The name 'b' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("a[1]", out error, testData);
                string actualIL = testData.GetMethodData("<>x.<>m0").GetMethodIL();
                AssertEx.AssertEqualToleratingWhitespaceDifferences(actualIL,
    @"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  ldelem.i4
  IL_0003:  ret
}");
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        public void LocalsAndPseudoVariables()
        {
            var source =
@"class C
{
    void M(object o)
    {
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var aliases = ImmutableArray.Create(
                    ExceptionAlias(typeof(System.IO.IOException)),
                    ReturnValueAlias(2, typeof(string)),
                    ReturnValueAlias(),
                    ObjectIdAlias(2, typeof(bool)),
                    VariableAlias("o", "C"));
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var diagnostics = DiagnosticBag.GetInstance();

                var testData = new CompilationTestData();
                context.CompileGetLocals(
                    locals,
                    argumentsOnly: true,
                    aliases: aliases,
                    diagnostics: diagnostics,
                    typeName: out typeName,
                    testData: testData);
                Assert.Equal(1, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "o");
                locals.Clear();

                testData = new CompilationTestData();
                context.CompileGetLocals(
                    locals,
                    argumentsOnly: false,
                    aliases: aliases,
                    diagnostics: diagnostics,
                    typeName: out typeName,
                    testData: testData);
                diagnostics.Free();
                Assert.Equal(6, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "$exception", "Error", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
    @"{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  call       ""System.Exception Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException()""
  IL_0005:  castclass  ""System.IO.IOException""
  IL_000a:  ret
}");
                // $ReturnValue is suppressed since it always matches the last $ReturnValueN
                VerifyLocal(testData, typeName, locals[1], "<>m1", "$ReturnValue2", "Method M2 returned", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
    @"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldc.i4.2
  IL_0001:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetReturnValue(int)""
  IL_0006:  castclass  ""string""
  IL_000b:  ret
}");
                VerifyLocal(testData, typeName, locals[2], "<>m2", "$2", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
@"{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldstr      ""$2""
  IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_000a:  unbox.any  ""bool""
  IL_000f:  ret
}");
                VerifyLocal(testData, typeName, locals[3], "<>m3", "o", expectedILOpt:
@"{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldstr      ""o""
  IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_000a:  castclass  ""C""
  IL_000f:  ret
}");
                VerifyLocal(testData, typeName, locals[4], "<>m4", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[5], "<>m5", "o", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                locals.Free();

                // Confirm that the Watch window is unaffected by the filtering in the Locals window.
                string error;
                context.CompileExpression("$ReturnValue", DkmEvaluationFlags.TreatAsExpression, aliases, out error);
                Assert.Null(error);
            });
        }

        [Fact]
        public void This()
        {
            var source =
@"class C
{
    void M(object @this)
    {
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
    @"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "@this", expectedILOpt: // Native EE uses "this" rather than "@this".
    @"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                locals.Free();
            });
        }

        [Fact]
        public void ArgumentsOnly()
        {
            var source =
@"class C
{
    void M<T>(T x)
    {
        object y = x;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: true, typeName: out typeName, testData: testData);

                Assert.Equal(1, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0<T>", "x", expectedILOpt:
    @"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (object V_0) //y
  IL_0000:  ldarg.1
  IL_0001:  ret
}",
                    expectedGeneric: true);
                locals.Free();
            });
        }

        /// <summary>
        /// Compiler-generated locals should be ignored.
        /// </summary>
        [Fact]
        public void CompilerGeneratedLocals()
        {
            var source =
@"class C
{
    static bool F(object[] args)
    {
        if (args == null)
        {
            return true;
        }
        foreach (var o in args)
        {
#line 999
        }
        ((System.Func<object>)(() => args[0]))();
        return false;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.F", atLineNumber: 999);
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "args", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                bool V_1,
                bool V_2,
                object[] V_3,
                int V_4,
                object V_5) //o
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""object[] C.<>c__DisplayClass0_0.args""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "o", expectedILOpt:
    @"{
  // Code size        3 (0x3)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                bool V_1,
                bool V_2,
                object[] V_3,
                int V_4,
                object V_5) //o
  IL_0000:  ldloc.s    V_5
  IL_0002:  ret
}");
                locals.Free();
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/928113")]
        public void Constants()
        {
            var source =
@"class C
{
    const int x = 2;
    static int F(int w)
    {
#line 888
        w.ToString(); // Force a non-hidden sequence point.
        const int y = 3;
        const object v = null;
        if ((v == null) || (w < 2))
        {
            const string z = ""str"";
#line 999
            string u = z;
            w += z.Length;
        }
        return w + x + y;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.F", atLineNumber: 888);
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(3, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "w");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt: @"
{
// Code size        2 (0x2)
.maxstack  1
.locals init (bool V_0,
              string V_1,
              int V_2)
IL_0000:  ldc.i4.3
IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[2], "<>m2", "v", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt: @"
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (bool V_0,
                string V_1,
                int V_2)
  IL_0000:  ldnull
  IL_0001:  ret
}");
                locals.Free();

                context = CreateMethodContext(
                    runtime,
                    methodName: "C.F",
                    atLineNumber: 999);
                testData = new CompilationTestData();
                locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(5, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "w");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "u");
                VerifyLocal(testData, typeName, locals[2], "<>m2", "y", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult);
                VerifyLocal(testData, typeName, locals[3], "<>m3", "v", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult);
                VerifyLocal(testData, typeName, locals[4], "<>m4", "z", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
    @"{
// Code size        6 (0x6)
.maxstack  1
.locals init (bool V_0,
            string V_1, //u
            int V_2)
IL_0000:  ldstr      ""str""
IL_0005:  ret
}");
                locals.Free();
            });
        }

        [Fact]
        public void ConstantEnum()
        {
            var source =
@"enum E { A, B }
class C
{
    static void M(E x)
    {
        const E y = E.B;
    }
    static void Main()
    {
        M(E.A);
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugExe);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(2, locals.Count);

                var method = (MethodSymbol)testData.GetMethodData("<>x.<>m0").Method;
                Assert.Equal(method.Parameters[0].Type, method.ReturnType);

                VerifyLocal(testData, "<>x", locals[0], "<>m0", "x", expectedILOpt:
    @"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");

                method = (MethodSymbol)testData.GetMethodData("<>x.<>m1").Method;
                Assert.Equal(method.Parameters[0].Type, method.ReturnType);

                VerifyLocal(testData, "<>x", locals[1], "<>m1", "y", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
    @"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}");
                locals.Free();
            });
        }

        [Fact]
        public void ConstantEnumAndTypeParameter()
        {
            var source =
@"class C<T>
{
    enum E { A }
    internal static void M<U>() where U : T
    {
        const C<T>.E t = E.A;
        const C<U>.E u = 0;
    }
}
class P
{
    static void Main()
    {
        C<object>.M<string>();
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugExe);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(3, locals.Count);

                VerifyLocal(testData, "<>x<T>", locals[0], "<>m0<U>", "t", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
    @"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}",
                    expectedGeneric: true);

                VerifyLocal(testData, "<>x<T>", locals[1], "<>m1<U>", "u", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
    @"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}",
                    expectedGeneric: true);

                VerifyLocal(testData, "<>x<T>", locals[2], "<>m2<U>", "<>TypeVariables", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
    @"{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  newobj     ""<>c__TypeVariables<T, U>..ctor()""
  IL_0005:  ret
}",
                    expectedGeneric: true);

                testData.GetMethodData("<>c__TypeVariables<T, U>..ctor").VerifyIL(
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ret
}");

                locals.Free();
            });
        }

        [Fact]
        public void CapturedLocalsOutsideLambda()
        {
            var source =
@"class C
{
    static void F(System.Func<object> f)
    {
    }
    void M(C x)
    {
        var y = new C();
        F(() => x ?? y ?? this);
        if (x != null)
        {
#line 999
            var z = 6;
            var w = 7;
            F(() => y ?? (object)w);
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(
                    runtime,
                    methodName: "C.M",
                    atLineNumber: 999);
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass1_0 V_0, //CS$<>8__locals0
                bool V_1,
                C.<>c__DisplayClass1_1 V_2, //CS$<>8__locals1
                int V_3) //z
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""C C.<>c__DisplayClass1_0.<>4__this""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "x", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass1_0 V_0, //CS$<>8__locals0
                bool V_1,
                C.<>c__DisplayClass1_1 V_2, //CS$<>8__locals1
                int V_3) //z
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""C C.<>c__DisplayClass1_0.x""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[2], "<>m2", "z", expectedILOpt:
    @"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass1_0 V_0, //CS$<>8__locals0
                bool V_1,
                C.<>c__DisplayClass1_1 V_2, //CS$<>8__locals1
                int V_3) //z
  IL_0000:  ldloc.3
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[3], "<>m3", "y", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass1_0 V_0, //CS$<>8__locals0
                bool V_1,
                C.<>c__DisplayClass1_1 V_2, //CS$<>8__locals1
                int V_3) //z
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""C C.<>c__DisplayClass1_0.y""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[4], "<>m4", "w", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass1_0 V_0, //CS$<>8__locals0
                bool V_1,
                C.<>c__DisplayClass1_1 V_2, //CS$<>8__locals1
                int V_3) //z
  IL_0000:  ldloc.2
  IL_0001:  ldfld      ""int C.<>c__DisplayClass1_1.w""
  IL_0006:  ret
}");
                Assert.Equal(5, locals.Count);
                locals.Free();
            });
        }

        [Fact]
        public void CapturedLocalsInsideLambda()
        {
            var source =
@"class C
{
    static void F(System.Func<object, object> f)
    {
        f(null);
    }
    void M()
    {
        var x = new object();
        F(_1 =>
        {
            var y = new object();
            F(_2 => y);
            return x ?? this;
        });
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass1_0.<M>b__0");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass1_1 V_0, //CS$<>8__locals0
                object V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<>c__DisplayClass1_0.<>4__this""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "_1", expectedILOpt:
    @"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass1_1 V_0, //CS$<>8__locals0
                object V_1)
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[2], "<>m2", "x", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass1_1 V_0, //CS$<>8__locals0
  object V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object C.<>c__DisplayClass1_0.x""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[3], "<>m3", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass1_1 V_0, //CS$<>8__locals0
  object V_1)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""object C.<>c__DisplayClass1_1.y""
  IL_0006:  ret
}");
                Assert.Equal(4, locals.Count);
                locals.Free();
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18273")]
        public void CapturedLocalInNestedLambda()
        {
            var source = @"
using System;
class C
{
    void M() { }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                context.CompileExpression("new Action(() => { int x; new Func<int>(() => x).Invoke(); }).Invoke()", out var error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action <>x.<>c.<>9__0_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""<>x.<>c <>x.<>c.<>9""
  IL_000e:  ldftn      ""void <>x.<>c.<<>m0>b__0_0()""
  IL_0014:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""System.Action <>x.<>c.<>9__0_0""
  IL_001f:  callvirt   ""void System.Action.Invoke()""
  IL_0024:  ret
}");
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18273")]
        public void CapturedLocalInNestedLocalFunction()
        {
            var source = @"
using System;
class C
{
    void M() { }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                context.CompileExpression(
@"new Action<int>(x =>
{
    int y;
    int F() => x + y;
    F();
}).Invoke(1)",
                    out var error,
                    testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       38 (0x26)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<int> <>x.<>c.<>9__0_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""<>x.<>c <>x.<>c.<>9""
  IL_000e:  ldftn      ""void <>x.<>c.<<>m0>b__0_0(int)""
  IL_0014:  newobj     ""System.Action<int>..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""System.Action<int> <>x.<>c.<>9__0_0""
  IL_001f:  ldc.i4.1
  IL_0020:  callvirt   ""void System.Action<int>.Invoke(int)""
  IL_0025:  ret
}");
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69117")]
        public void CapturedParameters()
        {
            var source = @"
using System;

class C
{
    void F(int a, byte b, bool c)
    {
        new Func<int>(() => a + b).Invoke();
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.F");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out var typeName, testData: testData);

                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt: @"
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  ldarg.0
  IL_0001:  ret
}
");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "a", expectedILOpt: @"
 {
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.a""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[2], "<>m2", "b", expectedILOpt: @"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""byte C.<>c__DisplayClass0_0.b""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[3], "<>m3", "c", expectedILOpt: @"
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  ldarg.3
  IL_0001:  ret
}");
            });
        }

        [Fact]
        public void RefReadOnlyLocalFunction()
        {
            var source = @"
using System;
class C
{
    void M() { }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                context.CompileExpression(
@"new Action(() =>
{
    int y = 0;
    ref readonly int F(ref int x) => ref x;
    F(ref y);
}).Invoke()",
                    out var error,
                    testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action <>x.<>c.<>9__0_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""<>x.<>c <>x.<>c.<>9""
  IL_000e:  ldftn      ""void <>x.<>c.<<>m0>b__0_0()""
  IL_0014:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""System.Action <>x.<>c.<>9__0_0""
  IL_001f:  callvirt   ""void System.Action.Invoke()""
  IL_0024:  ret
}");
            });
        }

        [Fact]
        public void NestedLambdas()
        {
            var source =
@"using System;
class C
{
    static void Main()
    {
        Func<object, object, object, object, Func<object, object, object, Func<object, object, Func<object, object>>>> f = (x1, x2, x3, x4) =>
        {
            if (x1 == null) return null;
            return (y1, y2, y3) =>
            {
                if ((y1 ?? x2) == null) return null;
                return (z1, z2) =>
                {
                    if ((z1 ?? y2 ?? x3) == null) return null;
                    return w1 =>
                    {
                        if ((z2 ?? y3 ?? x4) == null) return null;
                        return w1;
                    };
                };
            };
        };
        f(1, 2, 3, 4)(5, 6, 7)(8, 9)(10);
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c.<Main>b__0_0");

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

                VerifyLocal(testData, typeName, locals[0], "<>m0", "x1");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "x2", expectedILOpt:
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                bool V_1,
                System.Func<object, object, object, System.Func<object, object, System.Func<object, object>>> V_2)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""object C.<>c__DisplayClass0_0.x2""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[2], "<>m2", "x3");
                VerifyLocal(testData, typeName, locals[3], "<>m3", "x4");

                Assert.Equal(4, locals.Count);

                locals.Free();

                context = CreateMethodContext(
                    runtime,
                    methodName: "C.<>c__DisplayClass0_0.<Main>b__1");

                testData = new CompilationTestData();
                locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

                VerifyLocal(testData, typeName, locals[0], "<>m0", "y1");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "y2", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
                bool V_1,
                System.Func<object, object, System.Func<object, object>> V_2)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""object C.<>c__DisplayClass0_1.y2""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[2], "<>m2", "y3");
                VerifyLocal(testData, typeName, locals[3], "<>m3", "x2");
                VerifyLocal(testData, typeName, locals[4], "<>m4", "x3", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
                bool V_1,
                System.Func<object, object, System.Func<object, object>> V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object C.<>c__DisplayClass0_0.x3""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[5], "<>m5", "x4");
                Assert.Equal(6, locals.Count);
                locals.Free();

                context = CreateMethodContext(
                    runtime,
                    methodName: "C.<>c__DisplayClass0_1.<Main>b__2");

                testData = new CompilationTestData();
                locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

                VerifyLocal(testData, typeName, locals[0], "<>m0", "z1");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "z2", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_2 V_0, //CS$<>8__locals0
                bool V_1,
                System.Func<object, object> V_2)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""object C.<>c__DisplayClass0_2.z2""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[2], "<>m2", "y2");
                VerifyLocal(testData, typeName, locals[3], "<>m3", "y3");
                VerifyLocal(testData, typeName, locals[4], "<>m4", "x2");
                VerifyLocal(testData, typeName, locals[5], "<>m5", "x3", expectedILOpt:
@"{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_2 V_0, //CS$<>8__locals0
                bool V_1,
                System.Func<object, object> V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0006:  ldfld      ""object C.<>c__DisplayClass0_0.x3""
  IL_000b:  ret
}");
                VerifyLocal(testData, typeName, locals[6], "<>m6", "x4");
                Assert.Equal(7, locals.Count);
                locals.Free();

                context = CreateMethodContext(
                    runtime,
                    methodName: "C.<>c__DisplayClass0_2.<Main>b__3");

                testData = new CompilationTestData();
                locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

                VerifyLocal(testData, typeName, locals[0], "<>m0", "w1");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "z2", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (bool V_0,
                object V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object C.<>c__DisplayClass0_2.z2""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[2], "<>m2", "y2");
                VerifyLocal(testData, typeName, locals[3], "<>m3", "y3");
                VerifyLocal(testData, typeName, locals[4], "<>m4", "x2");
                VerifyLocal(testData, typeName, locals[5], "<>m5", "x3");
                VerifyLocal(testData, typeName, locals[6], "<>m6", "x4", expectedILOpt:
    @"{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (bool V_0,
                object V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.<>c__DisplayClass0_1 C.<>c__DisplayClass0_2.CS$<>8__locals2""
  IL_0006:  ldfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_000b:  ldfld      ""object C.<>c__DisplayClass0_0.x4""
  IL_0010:  ret
}");
                Assert.Equal(7, locals.Count);
                locals.Free();
            });
        }

        /// <summary>
        /// Should not include "this" inside display class
        /// instance method if "this" is not captured.
        /// </summary>
        [Theory]
        [MemberData(nameof(NonNullTypesTrueAndFalseDebugDll))]
        public void NoThisInsideDisplayClassInstanceMethod(CSharpCompilationOptions options)
        {
            var source =
@"using System;
class C
{
    void M<T>(T x) where T : class
    {
        Func<object, Func<T, object>> f = y =>
        {
            return z =>
            {
                return x ?? (object)y ?? z;
            };
        };
        f(2)(x);
    }
}";
            var compilation0 = CreateCompilation(source, options: options);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass0_0.<M>b__0");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(3, locals.Count);
                VerifyLocal(testData, "<>x<T>", locals[0], "<>m0", "y");
                VerifyLocal(testData, "<>x<T>", locals[1], "<>m1", "x");
                VerifyLocal(testData, "<>x<T>", locals[2], "<>m2", "<>TypeVariables", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult);
                locals.Free();

                context = CreateMethodContext(
                    runtime,
                    methodName: "C.<>c__DisplayClass0_1.<M>b__1");
                testData = new CompilationTestData();
                locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(4, locals.Count);
                VerifyLocal(testData, "<>x<T>", locals[0], "<>m0", "z");
                VerifyLocal(testData, "<>x<T>", locals[1], "<>m1", "y");
                VerifyLocal(testData, "<>x<T>", locals[2], "<>m2", "x");
                VerifyLocal(testData, "<>x<T>", locals[3], "<>m3", "<>TypeVariables", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult);
                locals.Free();
            });
        }

        [Fact]
        public void GenericMethod()
        {
            var source =
@"class A<T>
{
    struct B<U, V>
    {
        void M<W>(A<U>.B<V, object>[] o)
        {
            var t = default(T);
            var u = default(U);
            var w = default(W);
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "A.B.M");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

                Assert.Equal(6, locals.Count);
                VerifyLocal(testData, "<>x<T, U, V>", locals[0], "<>m0<W>", "this", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (T V_0, //t
  U V_1, //u
  W V_2) //w
  IL_0000:  ldarg.0
  IL_0001:  ldobj      ""A<T>.B<U, V>""
  IL_0006:  ret
}",
                        expectedGeneric: true);
                var method = (MethodSymbol)testData.GetMethodData("<>x<T, U, V>.<>m0<W>").Method;
                var containingType = method.ContainingType;
                var returnType = (NamedTypeSymbol)method.ReturnType;
                Assert.Equal(containingType.TypeParameters[1], returnType.TypeArguments()[0]);
                Assert.Equal(containingType.TypeParameters[2], returnType.TypeArguments()[1]);
                returnType = returnType.ContainingType;
                Assert.Equal(containingType.TypeParameters[0], returnType.TypeArguments()[0]);
                VerifyLocal(testData, "<>x<T, U, V>", locals[1], "<>m1<W>", "o", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (T V_0, //t
  U V_1, //u
  W V_2) //w
  IL_0000:  ldarg.1
  IL_0001:  ret
}",
                    expectedGeneric: true);
                method = (MethodSymbol)testData.GetMethodData("<>x<T, U, V>.<>m1<W>").Method;
                // method.ReturnType: A<U>.B<V, object>[]
                returnType = (NamedTypeSymbol)((ArrayTypeSymbol)method.ReturnType).ElementType;
                Assert.Equal(containingType.TypeParameters[2], returnType.TypeArguments()[0]);
                returnType = returnType.ContainingType;
                Assert.Equal(containingType.TypeParameters[1], returnType.TypeArguments()[0]);
                VerifyLocal(testData, "<>x<T, U, V>", locals[2], "<>m2<W>", "t", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (T V_0, //t
  U V_1, //u
  W V_2) //w
  IL_0000:  ldloc.0
  IL_0001:  ret
}",
                expectedGeneric: true);
                method = (MethodSymbol)testData.GetMethodData("<>x<T, U, V>.<>m2<W>").Method;
                containingType = method.ContainingType;
                Assert.Equal(containingType.TypeParameters[0], method.ReturnType);

                VerifyLocal(testData, "<>x<T, U, V>", locals[3], "<>m3<W>", "u", expectedILOpt:
    @"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (T V_0, //t
  U V_1, //u
  W V_2) //w
  IL_0000:  ldloc.1
  IL_0001:  ret
}",
                    expectedGeneric: true);
                method = (MethodSymbol)testData.GetMethodData("<>x<T, U, V>.<>m3<W>").Method;
                containingType = method.ContainingType;
                Assert.Equal(containingType.TypeParameters[1], method.ReturnType);

                VerifyLocal(testData, "<>x<T, U, V>", locals[4], "<>m4<W>", "w", expectedILOpt:
    @"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (T V_0, //t
  U V_1, //u
  W V_2) //w
  IL_0000:  ldloc.2
  IL_0001:  ret
}",
                    expectedGeneric: true);
                method = (MethodSymbol)testData.GetMethodData("<>x<T, U, V>.<>m4<W>").Method;
                Assert.Equal(method.TypeParameters[0], method.ReturnType);

                VerifyLocal(testData, "<>x<T, U, V>", locals[5], "<>m5<W>", "<>TypeVariables", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
    @"{
  // Code size        6 (0x6)
  .maxstack  1
  .locals init (T V_0, //t
  U V_1, //u
  W V_2) //w
  IL_0000:  newobj     ""<>c__TypeVariables<T, U, V, W>..ctor()""
  IL_0005:  ret
}",
                        expectedGeneric: true);
                method = (MethodSymbol)testData.GetMethodData("<>x<T, U, V>.<>m5<W>").Method;
                returnType = (NamedTypeSymbol)method.ReturnType;
                Assert.Equal(containingType.TypeParameters[0], returnType.TypeArguments()[0]);
                Assert.Equal(containingType.TypeParameters[1], returnType.TypeArguments()[1]);
                Assert.Equal(containingType.TypeParameters[2], returnType.TypeArguments()[2]);
                Assert.Equal(method.TypeParameters[0], returnType.TypeArguments()[3]);

                // Verify <>c__TypeVariables type was emitted (#976772).
                using (var metadata = ModuleMetadata.CreateFromImage(ImmutableArray.CreateRange(assembly)))
                {
                    var reader = metadata.MetadataReader;
                    var typeDef = reader.GetTypeDef("<>c__TypeVariables");
                    reader.CheckTypeParameters(typeDef.GetGenericParameters(), "T", "U", "V", "W");
                }
                locals.Free();
            });
        }

        [Fact]
        public void GenericLambda()
        {
            var source =
@"class C<T> where T : class
{
    static void M<U>(T t)
    {
        var u = default(U);
        System.Func<object> f = () => { return t ?? (object)u; };
        f();
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass0_0.<M>b__0");

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

                Assert.Equal(3, locals.Count);
                VerifyLocal(testData, "<>x<T, U>", locals[0], "<>m0", "t");
                VerifyLocal(testData, "<>x<T, U>", locals[1], "<>m1", "u", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (object V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""U C<T>.<>c__DisplayClass0_0<U>.u""
  IL_0006:  ret
}",
                    expectedGeneric: false);
                VerifyLocal(testData, "<>x<T, U>", locals[2], "<>m2", "<>TypeVariables", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult);

                var method = (MethodSymbol)testData.GetMethodData("<>x<T, U>.<>m1").Method;
                var containingType = method.ContainingType;
                Assert.Equal(containingType.TypeParameters[1], method.ReturnType);

                locals.Free();
            });
        }

        [Fact]
        public void Iterator_InstanceMethod()
        {
            var source =
@"using System.Collections;
class C
{
    private readonly object[] c;
    internal C(object[] c)
    {
        this.c = c;
    }
    internal IEnumerable F()
    {
        foreach (var o in c)
        {
#line 999
            yield return o;
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<F>d__2.MoveNext", atLineNumber: 999);
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, "<>x", locals[0], "<>m0", "this", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<F>d__2.<>4__this""
  IL_0006:  ret
}");
                VerifyLocal(testData, "<>x", locals[1], "<>m1", "o", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object C.<F>d__2.<o>5__3""
  IL_0006:  ret
}");
                locals.Free();
            });
        }

        [Fact]
        public void Iterator_StaticMethod_Generic()
        {
            var source =
@"using System.Collections.Generic;
class C
{
    static IEnumerable<T> F<T>(T[] o)
    {
        for (int i = 0; i < o.Length; i++)
        {
#line 999
            T t = default(T);
            yield return t;
            yield return o[i];
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(
                    runtime,
                    methodName: "C.<F>d__0.MoveNext",
                    atLineNumber: 999);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

                VerifyLocal(testData, "<>x<T>", locals[0], "<>m0", "o", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                bool V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""T[] C.<F>d__0<T>.o""
  IL_0006:  ret
}");
                VerifyLocal(testData, "<>x<T>", locals[1], "<>m1", "i", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                bool V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0<T>.<i>5__1""
  IL_0006:  ret
}");
                VerifyLocal(testData, "<>x<T>", locals[2], "<>m2", "t", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                bool V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""T C.<F>d__0<T>.<t>5__2""
  IL_0006:  ret
}");
                VerifyLocal(testData, "<>x<T>", locals[3], "<>m3", "<>TypeVariables", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult);
                Assert.Equal(4, locals.Count);
                locals.Free();
            });
        }

        [Fact]
        public void Async_InstanceMethod_Generic()
        {
            var source =
@"using System.Threading.Tasks;
struct S<T> where T : class
{
    T x;
    internal async Task<object> F<U>(U y) where U : class
    {
        var z = default(T);
        return this.x ?? (object)y ?? z;
    }
}";
            var compilation0 = CreateCompilationWithMscorlib461(
                source,
                options: TestOptions.DebugDll,
                references: new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef });

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "S.<F>d__1.MoveNext");

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

                VerifyLocal(testData, "<>x<T, U>", locals[0], "<>m0", "this", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                object V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""S<T> S<T>.<F>d__1<U>.<>4__this""
  IL_0006:  ret
}");
                VerifyLocal(testData, "<>x<T, U>", locals[1], "<>m1", "y", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                object V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""U S<T>.<F>d__1<U>.y""
  IL_0006:  ret
}");
                VerifyLocal(testData, "<>x<T, U>", locals[2], "<>m2", "z", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                object V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""T S<T>.<F>d__1<U>.<z>5__1""
  IL_0006:  ret
}");
                VerifyLocal(testData, "<>x<T, U>", locals[3], "<>m3", "<>TypeVariables", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult);

                Assert.Equal(4, locals.Count);
                locals.Free();
            });
        }

        [Fact]
        public void Async_StaticMethod_01()
        {
            var source =
@"using System.Threading.Tasks;
class C
{
    static async Task<object> F(object o)
    {
        return o;
    }
    static async Task M(object x)
    {
        var y = await F(x);
        await F(y);
    }
}";
            var compilation0 = CreateCompilationWithMscorlib461(
                source,
                options: TestOptions.DebugDll,
                references: new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef });

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>d__1.MoveNext");

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

                VerifyLocal(testData, "<>x", locals[0], "<>m0", "x", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_1,
                C.<M>d__1 V_2,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object C.<M>d__1.x""
  IL_0006:  ret
}");
                VerifyLocal(testData, "<>x", locals[1], "<>m1", "y", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_1,
                C.<M>d__1 V_2,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object C.<M>d__1.<y>5__1""
  IL_0006:  ret
}");
                Assert.Equal(2, locals.Count);
                locals.Free();
            });
        }

        [Fact]
        public void Async_StaticMethod_02()
        {
            var source =
@"using System.Threading.Tasks;
class C
{
    static async Task<object> F(object o)
    {
        return o;
    }
    static async Task M(object x)
    {
        {
#line 1000
            int y = (int)await F(x);
            await F(y);
        }
        {
#line 2000
            long y = (long)await F(x);
            await F(y);
        }
    }
}";
            var compilation0 = CreateCompilationWithMscorlib461(
                source,
                options: TestOptions.DebugDll,
                references: new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef });

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>d__1.MoveNext", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

                VerifyLocal(testData, "<>x", locals[0], "<>m0", "x", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_1,
                C.<M>d__1 V_2,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_3,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_4,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object C.<M>d__1.x""
  IL_0006:  ret
}");
                VerifyLocal(testData, "<>x", locals[1], "<>m1", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_1,
                C.<M>d__1 V_2,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_3,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_4,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__1.<y>5__1""
  IL_0006:  ret
}");
                Assert.Equal(2, locals.Count);
                locals.Free();

                context = CreateMethodContext(runtime, "C.<M>d__1.MoveNext", atLineNumber: 2000);

                testData = new CompilationTestData();
                locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

                VerifyLocal(testData, "<>x", locals[0], "<>m0", "x", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_1,
                C.<M>d__1 V_2,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_3,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_4,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object C.<M>d__1.x""
  IL_0006:  ret
}
");
                VerifyLocal(testData, "<>x", locals[1], "<>m1", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_1,
                C.<M>d__1 V_2,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_3,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_4,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""long C.<M>d__1.<y>5__3""
  IL_0006:  ret
}");
                Assert.Equal(2, locals.Count);
                locals.Free();
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10649")]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995976")]
        public void AsyncAndLambda()
        {
            var source =
@"using System;
using System.Threading.Tasks;
class C
{
    static async Task F()
    {
    }
    static void G(Action a)
    {
        a();
    }
    async static Task<int> M(int x)
    {
        int y = x + 1;
        await F();
        G(() => { x += 2; y += 2; });
        x += y;
        return x;
    }
}";
            var compilation0 = CreateCompilationWithMscorlib461(
                source,
                options: TestOptions.DebugDll,
                references: new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef });

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>d__2.MoveNext");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, "<>x", locals[0], "<>m0", "x", expectedILOpt:
    @"{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                C.<M>d__2 V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.<>c__DisplayClass2_0 C.<M>d__2.<>8__1""
  IL_0006:  ldfld      ""int C.<>c__DisplayClass2_0.x""
  IL_000b:  ret
}");
                VerifyLocal(testData, "<>x", locals[1], "<>m1", "y", expectedILOpt:
    @"{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                C.<M>d__2 V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.<>c__DisplayClass2_0 C.<M>d__2.<>8__1""
  IL_0006:  ldfld      ""int C.<>c__DisplayClass2_0.y""
  IL_000b:  ret
}");
                locals.Free();
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2240")]
        public void AsyncLambda()
        {
            var source =
@"using System;
using System.Threading.Tasks;
class C
{
    static void M()
    {
        Func<int, Task> f = async (x) =>
        {
            var y = 42;
        };
    }
}";
            var compilation0 = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.<>c.<<M>b__0_0>d.MoveNext");
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var testData = new CompilationTestData();
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, "<>x", locals[0], "<>m0", "x", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                System.Exception V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c.<<M>b__0_0>d.x""
  IL_0006:  ret
}");
                VerifyLocal(testData, "<>x", locals[1], "<>m1", "y", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                System.Exception V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c.<<M>b__0_0>d.<y>5__1""
  IL_0006:  ret
}");
                locals.Free();
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/996571")]
        public void MissingReference()
        {
            var source0 =
@"public class A
{
}
public struct B
{
}";
            var source1 =
@"class C
{
    static void M(A a, B b, C c)
    {
    }
}";
            var compilation0 = CreateCompilation(
                source0,
                options: TestOptions.DebugDll,
                assemblyName: "Comp1");

            var compilation1 = CreateCompilation(
                source1,
                options: TestOptions.DebugDll,
                references: new[] { compilation0.EmitToImageReference() });

            // no reference to compilation0
            WithRuntimeInstance(compilation1, new[] { MscorlibRef }, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData, expectedDiagnostics:
                [
                // error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                Diagnostic(ErrorCode.ERR_NoTypeDef).WithArguments("A", "Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 1)
            ]);

                Assert.Equal(0, locals.Count);
                locals.Free();
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/996571")]
        public void MissingReference_2()
        {
            var source0 =
@"public interface I
{
}";
            var source1 =
@"class C
{
    static void M<T>(object o) where T : I
    {
    }
}";
            var compilation0 = CreateCompilation(
                source0,
                options: TestOptions.DebugDll,
                assemblyName: "Comp1");

            var compilation1 = CreateCompilation(
                source1,
                options: TestOptions.DebugDll,
                references: new[] { compilation0.EmitToImageReference() });

            // no reference to compilation0
            WithRuntimeInstance(compilation1, new[] { MscorlibRef }, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;

                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData, expectedDiagnostics:
                [
                // error CS0012: The type 'I' is defined in an assembly that is not referenced. You must add a reference to assembly 'Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                Diagnostic(ErrorCode.ERR_NoTypeDef).WithArguments("I", "Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 1)
            ]);

                Assert.Equal(0, locals.Count);
                locals.Free();
            });
        }

        [Fact]
        public void AssignmentToLockLocal()
        {
            var source = @"
class C
{
    void M(object o)
    {
        lock(o)
        {
#line 999
            int x = 1;
        }
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(
                    runtime,
                    methodName: "C.M",
                    atLineNumber: 999);

                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("o = null", out error, testData);
                Assert.Null(error); // In regular code, there would be an error about modifying a lock local.

                testData.GetMethodData("<>x.<>m0").VerifyIL(
    @"{
  // Code size        5 (0x5)
  .maxstack  2
  .locals init (object V_0,
                bool V_1,
                int V_2) //x
  IL_0000:  ldnull
  IL_0001:  dup
  IL_0002:  starg.s    V_1
  IL_0004:  ret
}");

                testData = new CompilationTestData();
                context.CompileAssignment("o", "null", out error, testData);
                Assert.Null(error); // In regular code, there would be an error about modifying a lock local.

                testData.GetMethodData("<>x.<>m0").VerifyIL(
    @"{
  // Code size        4 (0x4)
  .maxstack  1
  .locals init (object V_0,
                bool V_1,
                int V_2) //x
  IL_0000:  ldnull
  IL_0001:  starg.s    V_1
  IL_0003:  ret
}");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1015887")]
        public void LocalDoubleConstant()
        {
            var source = @"
class C
{
    static void M()
    {
        const double d = 2.74745778612482E-266;
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(1, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
    @"{
  // Code size       10 (0xa)
  .maxstack  1
  IL_0000:  ldc.r8     2.74745778612482E-266
  IL_0009:  ret
}");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1015887")]
        public void LocalByteConstant()
        {
            var source = @"
class C
{
    static void M()
    {
        const byte b = 254;
        byte c = 0;
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var testData = new CompilationTestData();

                string error;
                context.CompileAssignment("c", "(byte)(b + 3)", out error, testData);
                Assert.Null(error);

                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        3 (0x3)
  .maxstack  1
  .locals init (byte V_0) //c
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ret
}
");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1015887")]
        public void LocalDecimalConstant()
        {
            var source = @"
class C
{
    static void M()
    {
        const decimal d = 1.5M;
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.M");

                string errorMessage;
                var testData = new CompilationTestData();
                context.CompileAssignment("d", "Nothing", out errorMessage, testData);
                Assert.Equal("error CS0131: The left-hand side of an assignment must be a variable, property or indexer", errorMessage);

                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(1, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
    @"{
  // Code size       12 (0xc)
  .maxstack  5
  IL_0000:  ldc.i4.s   15
  IL_0002:  ldc.i4.0
  IL_0003:  ldc.i4.0
  IL_0004:  ldc.i4.0
  IL_0005:  ldc.i4.1
  IL_0006:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
  IL_000b:  ret
}");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1022165"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1028883"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1034204")]
        public void KeywordIdentifiers()
        {
            var source = @"
class C
{
    void M(int @null)
    {
        int @this = 1;
        char @true = 't';
        string @namespace = ""NS"";
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(5, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt: @"
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //this
                char V_1, //true
                string V_2) //namespace
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "@null", expectedILOpt: @"
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //this
                char V_1, //true
                string V_2) //namespace
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[2], "<>m2", "@this", expectedILOpt: @"
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //this
                char V_1, //true
                string V_2) //namespace
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[3], "<>m3", "@true", expectedILOpt: @"
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //this
                char V_1, //true
                string V_2) //namespace
  IL_0000:  ldloc.1
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[4], "<>m4", "@namespace", expectedILOpt: @"
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //this
                char V_1, //true
                string V_2) //namespace
  IL_0000:  ldloc.2
  IL_0001:  ret
}");
                locals.Free();
            });
        }

        [Fact]
        public void ExtensionIterator()
        {
            var source = @"
static class C
{
    static System.Collections.IEnumerable F(this int x)
    {
        yield return x;
    }
        }
";
            var expectedIL = @"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.x""
  IL_0006:  ret
}";

            var compilation0 = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<F>d__0.MoveNext");

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(1, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: expectedIL);
                Assert.Equal(SpecialType.System_Int32, ((MethodSymbol)testData.GetMethodData(typeName + ".<>m0").Method).ReturnType.SpecialType);
                locals.Free();

                testData = new CompilationTestData();
                string error;
                context.CompileExpression("x", out error, testData);
                Assert.Null(error);
                var methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(expectedIL);
                Assert.Equal(SpecialType.System_Int32, ((MethodSymbol)methodData.Method).ReturnType.SpecialType);
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1063254")]
        public void OverloadedIteratorDifferentParameterTypes_ArgumentsOnly()
        {
            var source = @"
using System.Collections.Generic;
class C
{
    IEnumerable<int> M1(int x, int y)
    {
        int local = 0;
        yield return local;
    }
    IEnumerable<float> M1(int x, float y)
    {
        float local = 0.0F;
        yield return local;
    }
    static IEnumerable<float> M2(int x, float y)
    {
        float local = 0;
        yield return local;
    }
    static IEnumerable<T> M2<T>(int x, T y)
    {
        T local = default(T);
        yield return local;
    }
    static IEnumerable<int> M2(int x, int y)
    {
        int local = 0;
        yield return local;
    }
}";

            var compilation = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation, runtime =>
            {
                string displayClassName;
                string typeName;
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                CompilationTestData testData;
                var ilTemplate = @"
{{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""{0} C.{1}.{2}""
  IL_0006:  ret
}}";

                // M1(int, int)
                displayClassName = "<M1>d__0";
                GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: string.Format(ilTemplate, "int", displayClassName, "x"));
                VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: string.Format(ilTemplate, "int", displayClassName, "y"));
                locals.Clear();

                // M1(int, float)
                displayClassName = "<M1>d__1";
                GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: string.Format(ilTemplate, "int", displayClassName, "x"));
                VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: string.Format(ilTemplate, "float", displayClassName, "y"));
                locals.Clear();

                // M2(int, float)
                displayClassName = "<M2>d__2";
                GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: string.Format(ilTemplate, "int", displayClassName, "x"));
                VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: string.Format(ilTemplate, "float", displayClassName, "y"));
                locals.Clear();

                // M2(int, T)
                displayClassName = "<M2>d__3";
                GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
                typeName += "<T>";
                displayClassName += "<T>";
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: string.Format(ilTemplate, "int", displayClassName, "x"));
                VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: string.Format(ilTemplate, "T", displayClassName, "y"));
                locals.Clear();

                // M2(int, int)
                displayClassName = "<M2>d__4";
                GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: string.Format(ilTemplate, "int", displayClassName, "x"));
                VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: string.Format(ilTemplate, "int", displayClassName, "y"));
                locals.Clear();

                locals.Free();
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1063254")]
        public void OverloadedAsyncDifferentParameterTypes_ArgumentsOnly()
        {
            var source = @"
using System.Threading.Tasks;
class C
{
    async Task<int> M1(int x)
    {
        int local = 0;
        return local;
    }
    async Task<float> M1(int x, float y)
    {
        float local = 0.0F;
        return local;
    }
    static async Task<float> M2(int x, float y)
    {
        float local = 0;
        return local;
    }
    static async Task<T> M2<T>(T x)
    {
        T local = default(T);
        return local;
    }
    static async Task<int> M2(int x)
    {
        int local = 0;
        return local;
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation, runtime =>
            {
                string displayClassName;
                string typeName;
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                CompilationTestData testData;
                var ilTemplate = @"
{{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                {0} V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""{1} C.{2}.{3}""
  IL_0006:  ret
}}";

                // M1(int)
                displayClassName = "<M1>d__0";
                GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: string.Format(ilTemplate, "int", "int", displayClassName, "x"));
                locals.Clear();

                // M1(int, float)
                displayClassName = "<M1>d__1";
                GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: string.Format(ilTemplate, "float", "int", displayClassName, "x"));
                VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: string.Format(ilTemplate, "float", "float", displayClassName, "y"));
                locals.Clear();

                // M2(int, float)
                displayClassName = "<M2>d__2";
                GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: string.Format(ilTemplate, "float", "int", displayClassName, "x"));
                VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: string.Format(ilTemplate, "float", "float", displayClassName, "y"));
                locals.Clear();

                // M2(T)
                displayClassName = "<M2>d__3";
                GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName + "<T>", locals[0], "<>m0", "x", expectedILOpt: string.Format(ilTemplate, "T", "T", displayClassName + "<T>", "x"));
                locals.Clear();

                // M2(int)
                displayClassName = "<M2>d__4";
                GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: string.Format(ilTemplate, "int", "int", displayClassName, "x"));
                locals.Clear();

                locals.Free();
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1063254")]
        public void MultipleLambdasDifferentParameterNames_ArgumentsOnly()
        {
            var source = @"
using System;
class C
{
    void M1(int x)
    {
        Action<int> a = y => x.ToString();
        Func<int, int> f = z => x;
    }
    static void M2<T>(int x)
    {
        Action<int> a = y => y.ToString();
        Func<int, int> f = z => z;
        Func<T, T> g = t => t;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation, runtime =>
            {
                string displayClassName;
                string typeName;
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                CompilationTestData testData;
                var voidRetILTemplate = @"
{{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.{0}
  IL_0001:  ret
}}";
                var funcILTemplate = @"
{{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.{0}
  IL_0001:  ret
}}";

                // y => x.ToString()
                displayClassName = "<>c__DisplayClass0_0";
                GetLocals(runtime, "C." + displayClassName + ".<M1>b__0", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "y", expectedILOpt: string.Format(voidRetILTemplate, 1));
                locals.Clear();

                // z => x
                displayClassName = "<>c__DisplayClass0_0";
                GetLocals(runtime, "C." + displayClassName + ".<M1>b__1", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "z", expectedILOpt: string.Format(funcILTemplate, 1));
                locals.Clear();

                // y => y.ToString()
                displayClassName = "<>c__1";
                GetLocals(runtime, "C." + displayClassName + ".<M2>b__1_0", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName + "<T>", locals[0], "<>m0", "y", expectedILOpt: string.Format(voidRetILTemplate, 1));
                locals.Clear();

                // z => z
                displayClassName = "<>c__1";
                GetLocals(runtime, "C." + displayClassName + ".<M2>b__1_1", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName + "<T>", locals[0], "<>m0", "z", expectedILOpt: string.Format(funcILTemplate, 1));
                locals.Clear();

                // t => t
                displayClassName = "<>c__1";
                GetLocals(runtime, "C." + displayClassName + ".<M2>b__1_2", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName + "<T>", locals[0], "<>m0", "t", expectedILOpt: string.Format(funcILTemplate, 1));
                locals.Clear();

                locals.Free();
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1063254")]
        public void OverloadedRegularMethodDifferentParameterTypes_ArgumentsOnly()
        {
            var source = @"
class C
{
    void M1(int x, int y)
    {
        int local = 0;
    }
    string M1(int x, string y)
    {
        string local = null;
        return local;
    }
    static void M2(int x, string y)
    {
        string local = null;
    }
    static T M2<T>(int x, T y)
    {
        T local = default(T);
        return local;
    }
    static int M2(int x, ref int y)
    {
        int local = 0;
        return local;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation, runtime =>
            {
                string typeName;
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                CompilationTestData testData;
                var voidRetILTemplate = @"
{{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init ({0} V_0) //local
  IL_0000:  ldarg.{1}
  IL_0001:  ret
}}";
                var funcILTemplate = @"
{{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init ({0} V_0, //local
                {0} V_1)
  IL_0000:  ldarg.{1}
  IL_0001:  ret
}}";
                var refParamILTemplate = @"
{{
  // Code size        3 (0x3)
  .maxstack  1
  .locals init ({0} V_0, //local
                {0} V_1)
  IL_0000:  ldarg.{1}
  IL_0001:  ldind.i4
  IL_0002:  ret
}}";

                // M1(int, int)
                GetLocals(runtime, "C.M1(Int32,Int32)", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: string.Format(voidRetILTemplate, "int", 1));
                VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: string.Format(voidRetILTemplate, "int", 2));
                locals.Clear();

                // M1(int, string)
                GetLocals(runtime, "C.M1(Int32,String)", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: string.Format(funcILTemplate, "string", 1));
                VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: string.Format(funcILTemplate, "string", 2));
                locals.Clear();

                // M2(int, string)
                GetLocals(runtime, "C.M2(Int32,String)", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: string.Format(voidRetILTemplate, "string", 0));
                VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: string.Format(voidRetILTemplate, "string", 1));
                locals.Clear();

                // M2(int, T)
                GetLocals(runtime, "C.M2(Int32,T)", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName, locals[0], "<>m0<T>", "x", expectedILOpt: string.Format(funcILTemplate, "T", 0), expectedGeneric: true);
                VerifyLocal(testData, typeName, locals[1], "<>m1<T>", "y", expectedILOpt: string.Format(funcILTemplate, "T", 1), expectedGeneric: true);
                locals.Clear();

                // M2(int, int)
                GetLocals(runtime, "C.M2(Int32,Int32)", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: string.Format(funcILTemplate, "int", 0));
                VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: string.Format(refParamILTemplate, "int", 1));
                locals.Clear();

                locals.Free();
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1063254")]
        public void MultipleMethodsLocalConflictsWithParameterName_ArgumentsOnly()
        {
            var source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
class C<T>
{
    IEnumerable<int> M1()
    {
        int x = 0;
        yield return x;
    }
    IEnumerable<int> M1(int x)
    {
        yield return x;
    }
    IEnumerable<int> M2(int x)
    {
        yield return x;
    }
    IEnumerable<int> M2()
    {
        int x = 0;
        yield return x;
    }
    static async Task<T> M3()
    {
        T x = default(T);
        return x;
    }
    static async Task<T> M3<T>(T x)
    {
        return x;
    }
    static async Task<T> M4<T>(T x)
    {
        return x;
    }
    static async Task<T> M4()
    {
        T x = default(T);
        return x;
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation, runtime =>
            {
                string displayClassName;
                string typeName;
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                CompilationTestData testData;
                var iteratorILTemplate = @"
{{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init ({0} V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""{0} C<T>.{1}.{2}""
  IL_0006:  ret
}}";
                var asyncILTemplate = @"
{{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                {0} V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""{0} C<T>.{1}.{2}""
  IL_0006:  ret
}}";

                // M1()
                displayClassName = "<M1>d__0";
                GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 0, typeName: out typeName, testData: out testData);
                locals.Clear();

                // M1(int)
                displayClassName = "<M1>d__1";
                GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName + "<T>", locals[0], "<>m0", "x", expectedILOpt: string.Format(iteratorILTemplate, "int", displayClassName, "x"));
                locals.Clear();

                // M2(int)
                displayClassName = "<M2>d__2";
                GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName + "<T>", locals[0], "<>m0", "x", expectedILOpt: string.Format(iteratorILTemplate, "int", displayClassName, "x"));
                locals.Clear();

                // M2()
                displayClassName = "<M2>d__3";
                GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 0, typeName: out typeName, testData: out testData);
                locals.Clear();

                // M3()
                displayClassName = "<M3>d__4";
                GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 0, typeName: out typeName, testData: out testData);
                locals.Clear();

                // M3(int)
                displayClassName = "<M3>d__5";
                GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName + "<T, T>", locals[0], "<>m0", "x", expectedILOpt: string.Format(asyncILTemplate, "T", displayClassName + "<T>", "x"));
                locals.Clear();

                // M4(int)
                displayClassName = "<M4>d__6";
                GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
                VerifyLocal(testData, typeName + "<T, T>", locals[0], "<>m0", "x", expectedILOpt: string.Format(asyncILTemplate, "T", displayClassName + "<T>", "x"));
                locals.Clear();

                // M4()
                displayClassName = "<M4>d__7";
                GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 0, typeName: out typeName, testData: out testData);
                locals.Clear();

                locals.Free();
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1115030")]
        public void CatchInAsyncStateMachine()
        {
            var source =
@"using System;
using System.Threading.Tasks;
class C
{
    static object F()
    {
        throw new ArgumentException();
    }
    static async Task M()
    {
        object o;
        try
        {
            o = F();
        }
        catch (Exception e)
        {
#line 999
            o = e;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>d__1.MoveNext", atLineNumber: 999);
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "o", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                System.Exception V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object C.<M>d__1.<o>5__1""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "e", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                System.Exception V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""System.Exception C.<M>d__1.<e>5__2""
  IL_0006:  ret
}");
                locals.Free();
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1115030")]
        public void CatchInIteratorStateMachine()
        {
            var source =
@"using System;
using System.Collections;
class C
{
    static object F()
    {
        throw new ArgumentException();
    }
    static IEnumerable M()
    {
        object o;
        try
        {
            o = F();
        }
        catch (Exception e)
        {
#line 999
            o = e;
        }
        yield return o;
    }
}";
            var compilation0 = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>d__1.MoveNext", atLineNumber: 999);
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "o", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                System.Exception V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object C.<M>d__1.<o>5__1""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "e", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                System.Exception V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""System.Exception C.<M>d__1.<e>5__2""
  IL_0006:  ret
}");
                locals.Free();
            });
        }

        [Fact]
        public void DuplicateEditorBrowsableAttributes()
        {
            const string libSource = @"
namespace System.ComponentModel
{
    public enum EditorBrowsableState
    {
        Always = 0,
        Never = 1,
        Advanced = 2
    }

    [AttributeUsage(AttributeTargets.All)]
    internal sealed class EditorBrowsableAttribute : Attribute
    {
        public EditorBrowsableAttribute(EditorBrowsableState state) { }
    }
}
";

            const string source = @"
[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
class C
{
    void M()
    {
    }
}
";
            var libRef = CreateCompilation(libSource).EmitToImageReference();
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(comp, new[] { MscorlibRef, SystemRef, SystemCoreRef, SystemXmlLinqRef, libRef }, runtime =>
            {
                string typeName;
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                CompilationTestData testData;
                GetLocals(runtime, "C.M", argumentsOnly: false, locals: locals, count: 1, typeName: out typeName, testData: out testData);
                Assert.Equal("this", locals.Single().LocalName);
                locals.Free();
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2089")]
        public void MultipleThisFields()
        {
            var source =
@"using System;
using System.Threading.Tasks;
class C
{
    async static Task F(Action a)
    {
        a();
    }
    void G(string s)
    {
    }
    async void M()
    {
        string s = null;
        await F(() => G(s));
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>d__2.MoveNext()");
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var testData = new CompilationTestData();
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, "<>x", locals[0], "<>m0", "this", expectedILOpt:
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<M>d__2 V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<M>d__2.<>4__this""
  IL_0006:  ret
}");
                VerifyLocal(testData, "<>x", locals[1], "<>m1", "s", expectedILOpt:
    @"{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<M>d__2 V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.<>c__DisplayClass2_0 C.<M>d__2.<>8__1""
  IL_0006:  ldfld      ""string C.<>c__DisplayClass2_0.s""
  IL_000b:  ret
}");
                locals.Free();
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2336")]
        public void LocalsOnAsyncMethodClosingBrace()
        {
            var source =
@"using System;
using System.Threading.Tasks;
class C
{
    async void M()
    {
        string s = null;
#line 999
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>d__0.MoveNext()", atLineNumber: 999);
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var testData = new CompilationTestData();
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, "<>x", locals[0], "<>m0", "this");
                VerifyLocal(testData, "<>x", locals[1], "<>m1", "s");
                locals.Free();
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1139013")]
        public void TransparentIdentifiers_FromParameter()
        {
            const string source = @"
using System.Linq;

class C
{
    void M(string[] args)
    {
        var concat = 
            from x in args
            let y = x.ToString()
            let z = x.GetHashCode()
            select x + y + z;
    }
}
";

            const string methodName = "C.<>c.<M>b__0_2";

            const string zIL = @"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.1
  IL_0001:  ldfld      ""int <>f__AnonymousType1<<>f__AnonymousType0<string, string>, int>.<z>i__Field""
  IL_0006:  ret
}
";
            const string xIL = @"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.1
  IL_0001:  ldfld      ""<>f__AnonymousType0<string, string> <>f__AnonymousType1<<>f__AnonymousType0<string, string>, int>.<<>h__TransparentIdentifier0>i__Field""
  IL_0006:  ldfld      ""string <>f__AnonymousType0<string, string>.<x>i__Field""
  IL_000b:  ret
}
";
            const string yIL = @"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.1
  IL_0001:  ldfld      ""<>f__AnonymousType0<string, string> <>f__AnonymousType1<<>f__AnonymousType0<string, string>, int>.<<>h__TransparentIdentifier0>i__Field""
  IL_0006:  ldfld      ""string <>f__AnonymousType0<string, string>.<y>i__Field""
  IL_000b:  ret
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                string typeName;
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                CompilationTestData testData;
                GetLocals(runtime, methodName, argumentsOnly: false, locals: locals, count: 3, typeName: out typeName, testData: out testData);

                VerifyLocal(testData, typeName, locals[0], "<>m0", "z", expectedILOpt: zIL);
                VerifyLocal(testData, typeName, locals[1], "<>m1", "x", expectedILOpt: xIL);
                VerifyLocal(testData, typeName, locals[2], "<>m2", "y", expectedILOpt: yIL);
                locals.Free();

                var context = CreateMethodContext(runtime, methodName);
                string error;

                testData = new CompilationTestData();
                context.CompileExpression("z", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(zIL);

                testData = new CompilationTestData();
                context.CompileExpression("x", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(xIL);

                testData = new CompilationTestData();
                context.CompileExpression("y", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(yIL);
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1139013")]
        public void TransparentIdentifiers_FromDisplayClassField()
        {
            const string source = @"
using System.Linq;

class C
{
    void M(string[] args)
    {
        var concat = 
            from x in args
            let y = x.ToString()
            let z = x.GetHashCode()
            select x.Select(c => y + z);
    }
}
";

            const string methodName = "C.<>c__DisplayClass0_0.<M>b__3";

            const string cIL = @"
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.1
  IL_0001:  ret
}
";
            const string zIL = @"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""<>f__AnonymousType1<<>f__AnonymousType0<string, string>, int> C.<>c__DisplayClass0_0.<>h__TransparentIdentifier1""
  IL_0006:  ldfld      ""int <>f__AnonymousType1<<>f__AnonymousType0<string, string>, int>.<z>i__Field""
  IL_000b:  ret
}
";
            const string xIL = @"
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""<>f__AnonymousType1<<>f__AnonymousType0<string, string>, int> C.<>c__DisplayClass0_0.<>h__TransparentIdentifier1""
  IL_0006:  ldfld      ""<>f__AnonymousType0<string, string> <>f__AnonymousType1<<>f__AnonymousType0<string, string>, int>.<<>h__TransparentIdentifier0>i__Field""
  IL_000b:  ldfld      ""string <>f__AnonymousType0<string, string>.<x>i__Field""
  IL_0010:  ret
}
";
            const string yIL = @"
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""<>f__AnonymousType1<<>f__AnonymousType0<string, string>, int> C.<>c__DisplayClass0_0.<>h__TransparentIdentifier1""
  IL_0006:  ldfld      ""<>f__AnonymousType0<string, string> <>f__AnonymousType1<<>f__AnonymousType0<string, string>, int>.<<>h__TransparentIdentifier0>i__Field""
  IL_000b:  ldfld      ""string <>f__AnonymousType0<string, string>.<y>i__Field""
  IL_0010:  ret
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                string typeName;
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                CompilationTestData testData;
                GetLocals(runtime, methodName, argumentsOnly: false, locals: locals, count: 4, typeName: out typeName, testData: out testData);

                VerifyLocal(testData, typeName, locals[0], "<>m0", "c", expectedILOpt: cIL);
                VerifyLocal(testData, typeName, locals[1], "<>m1", "z", expectedILOpt: zIL);
                VerifyLocal(testData, typeName, locals[2], "<>m2", "x", expectedILOpt: xIL);
                VerifyLocal(testData, typeName, locals[3], "<>m3", "y", expectedILOpt: yIL);

                locals.Free();

                var context = CreateMethodContext(runtime, methodName);
                string error;

                testData = new CompilationTestData();
                context.CompileExpression("c", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(cIL);

                testData = new CompilationTestData();
                context.CompileExpression("z", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(zIL);

                testData = new CompilationTestData();
                context.CompileExpression("x", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(xIL);

                testData = new CompilationTestData();
                context.CompileExpression("y", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(yIL);
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/3236")]
        public void AnonymousTypeParameter()
        {
            const string source = @"
using System.Linq;

class C
{
    static void Main(string[] args)
    {
        var anonymousTypes =
            from a in args
            select new { Value = a, Length = a.Length };
        var values =
            from t in anonymousTypes
            select t.Value;
    }
}
";

            const string methodName = "C.<>c.<Main>b__0_1";

            const string tIL = @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                string typeName;
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                CompilationTestData testData;
                GetLocals(runtime, methodName, argumentsOnly: false, locals: locals, count: 1, typeName: out typeName, testData: out testData);

                VerifyLocal(testData, typeName, locals[0], "<>m0", "t", expectedILOpt: tIL);

                locals.Free();

                var context = CreateMethodContext(runtime, methodName);
                string error;

                testData = new CompilationTestData();
                context.CompileExpression("t", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(tIL);
            });
        }

        [Fact, WorkItem("https://github.com/aspnet/Home/issues/955")]
        public void ConstantWithErrorType()
        {
            const string source = @"
class Program
{
    static void Main()
    {
        const int a = 1;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            WithRuntimeInstance(comp, runtime =>
            {
                var badConst = new MockSymUnmanagedConstant(
                    "a",
                    1,
                    (int bufferLength, out int count, byte[] name) =>
                    {
                        count = 0;
                        return Roslyn.Test.Utilities.HResult.E_NOTIMPL;
                    });
                var debugInfo = new MethodDebugInfoBytes.Builder(constants: new[] { badConst }).Build();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();

                GetLocals(runtime, "Program.Main", debugInfo, locals, count: 0);

                locals.Free();
            });
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=298297")]
        public void OrderOfArguments_ArgumentsOnly()
        {
            var source =
@"using System.Collections.Generic;
class C
{
    static IEnumerable<object> F(object y, object x)
    {
        yield return x;
#line 500
        DummySequencePoint();
        yield return y;
    }
    static void DummySequencePoint()
    {
    }
}";
            var comp = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseDll);
            WithRuntimeInstance(comp, runtime =>
            {
                EvaluationContext context;
                context = CreateMethodContext(runtime, "C.<F>d__0.MoveNext", atLineNumber: 500);
                string unused;
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                context.CompileGetLocals(locals, argumentsOnly: true, typeName: out unused, testData: null);
                var names = locals.Select(l => l.LocalName).ToArray();
                // The order must confirm the order of the arguments in the method signature.
                Assert.Equal(names, ["y", "x"]);
                locals.Free();
            });

            comp = CreateCompilationWithMscorlib40(source, options: TestOptions.ReleaseDll);
            WithRuntimeInstance(comp, runtime =>
            {
                EvaluationContext context;
                context = CreateMethodContext(runtime, "C.<F>d__0.MoveNext", atLineNumber: 500);
                string unused;
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                context.CompileGetLocals(locals, argumentsOnly: true, typeName: out unused, testData: null);
                var names = locals.Select(l => l.LocalName).ToArray();
                // The problem is not fixed in versions before 4.5: the order of arguments can be wrong.
                Assert.Equal(names, ["x", "y"]);
                locals.Free();
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55606")]
        public void OrderOfArguments_ArgumentsOnly_Async()
        {
            var source =
@"using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    static async IAsyncEnumerable<object> F(object y, object x)
    {
        Task.Yield();
        yield return x;
#line 500
        DummySequencePoint();
        yield return y;
    }
    static void DummySequencePoint()
    {
    }
}";
            MetadataReference[] references = [.. Net461.References.All, Net461.ExtraReferences.SystemThreadingTasksExtensions];
            var comp = CreateEmptyCompilation(new[] { source, AsyncStreamsTypes }, references: references);
            WithRuntimeInstance(comp, references, runtime =>
            {
                EvaluationContext context;
                context = CreateMethodContext(runtime, "C.<F>d__0.MoveNext", atLineNumber: 500);
                string unused;
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                context.CompileGetLocals(locals, argumentsOnly: true, typeName: out unused, testData: null);
                var names = locals.Select(l => l.LocalName).ToArray();
                // The order must confirm the order of the arguments in the method signature.
                Assert.Equal(names, ["y", "x"]);
                locals.Free();
            });
        }

        /// <summary>
        /// CompileGetLocals should skip locals with errors.
        /// </summary>
        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=535899")]
        public void SkipPseudoVariablesWithUseSiteErrors()
        {
            var source =
@"class C
{
    static void M(object x)
    {
        object y;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var aliases = ImmutableArray.Create(ReturnValueAlias(1, "UnknownType, UnknownAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"));
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var diagnostics = DiagnosticBag.GetInstance();
                var testData = new CompilationTestData();
                context.CompileGetLocals(
                    locals,
                    argumentsOnly: false,
                    aliases: aliases,
                    diagnostics: diagnostics,
                    typeName: out typeName,
                    testData: testData);
                diagnostics.Verify();
                diagnostics.Free();
                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "y");
                locals.Free();
            });
        }

        [Fact]
        public void LocalsInFieldInitializer_01()
        {
            var source =
@"class C
{
#line 1000
    bool Test1 = TakeOutParam(1, out var x1);

    C() {}     

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0) //x1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "x1", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0) //x1
  IL_0000:  ldloc.0
  IL_0001:  ret
}");

                locals.Free();
            });
        }

        [Fact]
        public void LocalsInFieldInitializer_02()
        {
            var source =
@"class C : Base
{
#line 1000
    bool Test1 = TakeOutParam(1, out var x1), 
         Test2 = TakeOutParam(2, out var x2);

    C()     
#line 2000
    : base(TakeOutParam(0, out var x0))
    {}     

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}

class Base
{
    public Base(bool x) {}
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //x1
                int V_1,
                int V_2)
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "x1", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //x1
                int V_1,
                int V_2)
  IL_0000:  ldloc.0
  IL_0001:  ret
}");

                locals.Free();
            });

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor", atLineNumber: 1001);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0,
                int V_1, //x2
                int V_2)
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "x2", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0,
                int V_1, //x2
                int V_2)
  IL_0000:  ldloc.1
  IL_0001:  ret
}");

                locals.Free();
            });

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor", atLineNumber: 2000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0,
                int V_1, 
                int V_2) //x0
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "x0", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0,
                int V_1, 
                int V_2) //x0
  IL_0000:  ldloc.2
  IL_0001:  ret
}");

                locals.Free();
            });
        }

        [Fact]
        public void LocalsInFieldInitializer_03()
        {
            var source =
@"class C
{
#line 1000
    bool Test1 = TakeOutParam(1, out var x1) && F(() => x1);

    C() {}     

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
    static bool F(System.Func<int> x) 
    {
        throw null;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass1_0 V_0) //CS$<>8__locals0
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "x1", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass1_0 V_0) //CS$<>8__locals0
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass1_0.x1""
  IL_0006:  ret
}");

                locals.Free();
            });

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass1_0.<.ctor>b__0", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(1, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x1", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass1_0.x1""
  IL_0006:  ret
}");

                locals.Free();
            });
        }

        [Fact]
        public void LocalsInFieldInitializer_04()
        {
            var source =
@"class C
{
#line 1000
    bool Test1 = TakeOutParam(1, out var x1) && F(() => x1),
         Test2 = TakeOutParam(2, out var x2) && F(() => x2);

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
    static bool F(System.Func<int> x) 
    {
        throw null;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass4_0 V_0, //CS$<>8__locals0
                C.<>c__DisplayClass4_1 V_1)
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "x1", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass4_0 V_0, //CS$<>8__locals0
                C.<>c__DisplayClass4_1 V_1)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass4_0.x1""
  IL_0006:  ret
}");

                locals.Free();
            });

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass4_0.<.ctor>b__0", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(1, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x1", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass4_0.x1""
  IL_0006:  ret
}");

                locals.Free();
            });

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor", atLineNumber: 1001);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass4_0 V_0,
                C.<>c__DisplayClass4_1 V_1) //CS$<>8__locals1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "x2", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass4_0 V_0,
                C.<>c__DisplayClass4_1 V_1) //CS$<>8__locals1
  IL_0000:  ldloc.1
  IL_0001:  ldfld      ""int C.<>c__DisplayClass4_1.x2""
  IL_0006:  ret
}");

                locals.Free();
            });

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass4_1.<.ctor>b__1", atLineNumber: 1001);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(1, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x2", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass4_1.x2""
  IL_0006:  ret
}");

                locals.Free();
            });
        }

        [Fact]
        public void LocalsInPropertyInitializer_01()
        {
            var source =
@"class C
{
#line 1000
    bool Test1 { get; } = TakeOutParam(1, out var x1);

    C() {}     

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0) //x1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "x1", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0) //x1
  IL_0000:  ldloc.0
  IL_0001:  ret
}");

                locals.Free();
            });
        }

        [Fact]
        public void LocalsInPropertyInitializer_02()
        {
            var source =
@"class C
{
#line 1000
    bool Test1 { get; } = TakeOutParam(1, out var x1) && F(() => x1);

    C() {}     

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
    static bool F(System.Func<int> x) 
    {
        throw null;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass3_0 V_0) //CS$<>8__locals0
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "x1", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass3_0 V_0) //CS$<>8__locals0
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass3_0.x1""
  IL_0006:  ret
}");

                locals.Free();
            });

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass3_0.<.ctor>b__0", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(1, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x1", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass3_0.x1""
  IL_0006:  ret
}");

                locals.Free();
            });
        }

        [Fact]
        public void LocalsInConstructorInitializer_01()
        {
            var source =
@"class C : Base
{
    C() 
#line 1000
    : base(TakeOutParam(1, out var x1))
    {}     

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}

class Base
{
    public Base(bool x) {}
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0) //x1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "x1", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0) //x1
  IL_0000:  ldloc.0
  IL_0001:  ret
}");

                locals.Free();
            });
        }

        [Fact]
        public void LocalsInConstructorInitializer_02()
        {
            var source =
@"class C : Base
{
    C() 
    : base(TakeOutParam(1, out var x1))
    {
#line 1000
        ;
    }     

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}

class Base
{
    public Base(bool x) {}
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0) //x1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "x1", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0) //x1
  IL_0000:  ldloc.0
  IL_0001:  ret
}");

                locals.Free();
            });
        }

        [Fact]
        public void LocalsInConstructorInitializer_03()
        {
            var source =
@"class C : Base
{
    C() 
    : base(TakeOutParam(1, out var x1))
#line 1000
    => System.Console.WriteLine();

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}

class Base
{
    public Base(bool x) {}
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0) //x1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "x1", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0) //x1
  IL_0000:  ldloc.0
  IL_0001:  ret
}");

                locals.Free();
            });
        }

        [Fact]
        public void LocalsInConstructorInitializer_04()
        {
            var source =
@"class C : Base
{
    C() 
    : base(TakeOutParam(1, out var x1))
    {
        int x2 = 1;
#line 1000
        System.Console.WriteLine(x2);
    }     

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}

class Base
{
    public Base(bool x) {}
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(3, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //x1
                int V_1) //x2
  IL_0000:  ldarg.0
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "x1", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //x1
                int V_1) //x2
  IL_0000:  ldloc.0
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[2], "<>m2", "x2", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //x1
                int V_1) //x2
  IL_0000:  ldloc.1
  IL_0001:  ret
}");
                locals.Free();
            });
        }

        [Fact]
        public void LocalsInConstructorInitializer_05()
        {
            var source =
@"class C : Base
{
    C() 
    : base(TakeOutParam(1, out var x1))
#line 1000
    => System.Console.WriteLine(TakeOutParam(2, out var x2));

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}

class Base
{
    public Base(bool x) {}
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(3, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //x1
                int V_1) //x2
  IL_0000:  ldarg.0
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "x1", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //x1
                int V_1) //x2
  IL_0000:  ldloc.0
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[2], "<>m2", "x2", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //x1
                int V_1) //x2
  IL_0000:  ldloc.1
  IL_0001:  ret
}");
                locals.Free();
            });
        }

        [Fact]
        public void LocalsInConstructorInitializer_06()
        {
            var source =
@"class C : Base
{
    C() 
#line 1000
    : base(TakeOutParam(1, out var x1) && F(() => x1))
    {}     

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
    static bool F(System.Func<int> x) 
    {
        throw null;
    }
}

class Base
{
    public Base(bool x) {}
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  ldarg.0
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "x1", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.x1""
  IL_0006:  ret
}");

                locals.Free();
            });

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass0_0.<.ctor>b__0", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(1, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x1", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.x1""
  IL_0006:  ret
}");

                locals.Free();
            });
        }

        [Fact]
        public void LocalsInConstructorInitializer_07()
        {
            var source =
@"class C
{
    C() 
#line 1000
    : this(TakeOutParam(1, out var x1))
    {}     

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }

    C(bool x) {}
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor", atLineNumber: 1000);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0) //x1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "x1", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0) //x1
  IL_0000:  ldloc.0
  IL_0001:  ret
}");

                locals.Free();
            });
        }

        [Fact]
        public void LocalsInQuery_01()
        {
            var source =
@"
using System.Linq;

class C
{
    C() 
    {
#line 1000
        var q = from a in new [] {1} 
                where 
                      TakeOutParam(a, out var x1)   
                select a;
    }     

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c.<.ctor>b__0_0", atLineNumber: 1002);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "a", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0) //x1
  IL_0000:  ldarg.1
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "x1", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0) //x1
  IL_0000:  ldloc.0
  IL_0001:  ret
}");

                locals.Free();
            });
        }

        [Fact]
        public void LocalsInQuery_02()
        {
            var source =
@"
using System.Linq;

class C
{
    C() 
    {
#line 1000
        var q = from a in new [] {1} 
                where 
                      TakeOutParam(a, out var x1) && F(() => x1)   
                select a;
    }     

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
    static bool F(System.Func<int> x) 
    {
        throw null;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c.<.ctor>b__0_0", atLineNumber: 1002);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(2, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "a", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  ldarg.1
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "x1", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.x1""
  IL_0006:  ret
}");

                locals.Free();
            });

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass0_0.<.ctor>b__1", atLineNumber: 1002);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(1, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x1", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.x1""
  IL_0006:  ret
}");

                locals.Free();
            });
        }

        private static void GetLocals(RuntimeInstance runtime, string methodName, bool argumentsOnly, ArrayBuilder<LocalAndMethod> locals, int count, out string typeName, out CompilationTestData testData)
        {
            var context = CreateMethodContext(runtime, methodName);
            testData = new CompilationTestData();
            var assembly = context.CompileGetLocals(locals, argumentsOnly, out typeName, testData);
            Assert.NotNull(assembly);
            if (count == 0)
            {
                Assert.Equal(0, assembly.Count);
            }
            else
            {
                Assert.InRange(assembly.Count, 0, int.MaxValue);
            }
            Assert.Equal(count, locals.Count);
        }

        private static void GetLocals(RuntimeInstance runtime, string methodName, MethodDebugInfoBytes debugInfo, ArrayBuilder<LocalAndMethod> locals, int count)
        {
            ImmutableArray<MetadataBlock> blocks;
            Guid moduleVersionId;
            ISymUnmanagedReader unused;
            int methodToken;
            int localSignatureToken;
            GetContextState(runtime, methodName, out blocks, out moduleVersionId, out unused, out methodToken, out localSignatureToken);

            var symReader = new MockSymUnmanagedReader(
                new Dictionary<int, MethodDebugInfoBytes>()
                {
                    {methodToken, debugInfo}
                }.ToImmutableDictionary());
            var context = CreateMethodContext(
                new AppDomain(),
                blocks,
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion: 1,
                ilOffset: 0,
                localSignatureToken: localSignatureToken,
                kind: MakeAssemblyReferencesKind.AllAssemblies);

            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: null);

            Assert.NotNull(assembly);
            if (count == 0)
            {
                Assert.Equal(0, assembly.Count);
            }
            else
            {
                Assert.InRange(assembly.Count, 0, int.MaxValue);
            }
            Assert.Equal(count, locals.Count);
        }

        [Fact]
        public void EENamedTypeSymbolNeverSkipsLocalsInit()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public void M<T>()
    {
        var t = default(T);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

                Assert.Equal(3, locals.Count);
                VerifyLocal(testData, "<>x", locals[0], "<>m0<T>", "this", expectedILOpt: @"
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (T V_0) //t
  IL_0000:  ldarg.0
  IL_0001:  ret
}",
                    expectedGeneric: true);

                VerifyLocal(testData, "<>x", locals[1], "<>m1<T>", "t", expectedILOpt: @"
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (T V_0) //t
  IL_0000:  ldloc.0
  IL_0001:  ret
}",
                    expectedGeneric: true);

                VerifyLocal(testData, "<>x", locals[2], "<>m2<T>", "<>TypeVariables", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt: @"
{
  // Code size        6 (0x6)
  .maxstack  1
  .locals init (T V_0) //t
  IL_0000:  newobj     ""<>c__TypeVariables<T>..ctor()""
  IL_0005:  ret
}",
                        expectedGeneric: true);
            });
        }

        [Fact, WorkItem(67177, "https://github.com/dotnet/roslyn/issues/67177")]
        public void CapturingAndShadowing_01()
        {
            var source =
@"class C
{
    void Test()
    {
        int x = 0;
        int z = 1;

        var d1 = () =>
        {
            x += z;
        };

        d1();

        var d2 = () =>
        {
            sbyte x = 0;
            int y = x;

            var d3 = () =>
            {
                y += z;
            };

            x = -100;
#line 100
            z += x;
#line 200
            return d3;
        };

        d2()();
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass0_0.<Test>b__1", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(4, locals.Count);

                VerifyLocal(testData, typeName, locals[0], "<>m0", "z", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
            sbyte V_1, //x
            System.Action V_2, //d3
            System.Action V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.z""
  IL_0006:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "x", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
            sbyte V_1, //x
            System.Action V_2, //d3
            System.Action V_3)
  IL_0000:  ldloc.1
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[2], "<>m2", "d3", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
            sbyte V_1, //x
            System.Action V_2, //d3
            System.Action V_3)
  IL_0000:  ldloc.2
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[3], "<>m3", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
            sbyte V_1, //x
            System.Action V_2, //d3
            System.Action V_3)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_1.y""
  IL_0006:  ret
}");

                locals.Free();
            });
        }

        [Fact, WorkItem(67177, "https://github.com/dotnet/roslyn/issues/67177")]
        public void CapturingAndShadowing_02()
        {
            var source =
@"class C
{
    void Test()
    {
        int x = 0;
        int z = 1;

        var d1 = () =>
        {
            x += z;
        };

        d1();

        var d2 = () =>
        {
            sbyte x = 0;
            int y = x;

            var d3 = () =>
            {
#line 100
                y += z;
#line 200
            };

            x = -100;
            z += x;

            return d3;
        };

        d2()();
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass0_1.<Test>b__2", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(3, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_1.y""
  IL_0006:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "x", expectedILOpt:
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0006:  ldfld      ""int C.<>c__DisplayClass0_0.x""
  IL_000b:  ret
}");

                VerifyLocal(testData, typeName, locals[2], "<>m2", "z", expectedILOpt:
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0006:  ldfld      ""int C.<>c__DisplayClass0_0.z""
  IL_000b:  ret
}");

                locals.Free();
            });
        }

        [Fact, WorkItem(67177, "https://github.com/dotnet/roslyn/issues/67177")]
        public void CapturingAndShadowing_03()
        {
            var source =
@"class C
{
    void Test()
    {
        int x = 0;
        int z = 1;

        var d1 = () =>
        {
            x += z;
        };

        d1();

        var d2 = (sbyte x) =>
        {
            int y = x;

            var d3 = (short x) =>
            {
                y += z;
            };

            x = -100;
#line 100
            z += x;
#line 200
            return d3;
        };

        d2(-100)(-200);
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass0_0.<Test>b__1", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(4, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
                System.Action<short> V_1, //d3
                System.Action<short> V_2)
  IL_0000:  ldarg.1
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "z", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
            System.Action<short> V_1, //d3
            System.Action<short> V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.z""
  IL_0006:  ret
}");

                VerifyLocal(testData, typeName, locals[2], "<>m2", "d3", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
            System.Action<short> V_1, //d3
            System.Action<short> V_2)
  IL_0000:  ldloc.1
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[3], "<>m3", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
            System.Action<short> V_1, //d3
            System.Action<short> V_2)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_1.y""
  IL_0006:  ret
}");

                locals.Free();
            });
        }

        [Fact, WorkItem(67177, "https://github.com/dotnet/roslyn/issues/67177")]
        public void CapturingAndShadowing_04()
        {
            var source =
@"class C
{
    void Test()
    {
        int x = 0;
        int z = 1;

        var d1 = () =>
        {
            x += z;
        };

        d1();

        var d2 = (sbyte x) =>
        {
            int y = x;

            var d3 = (short x) =>
            {
#line 100
                y += z;
#line 200
            };

            x = -100;
            z += x;

            return d3;
        };

        d2(-100)(-200);
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass0_1.<Test>b__2", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(3, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_1.y""
  IL_0006:  ret
}");

                VerifyLocal(testData, typeName, locals[2], "<>m2", "z", expectedILOpt:
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0006:  ldfld      ""int C.<>c__DisplayClass0_0.z""
  IL_000b:  ret
}");

                locals.Free();
            });
        }

        [Fact, WorkItem(67177, "https://github.com/dotnet/roslyn/issues/67177")]
        public void CapturingAndShadowing_05()
        {
            var source =
@"class C
{
    static void Test()
    {
        byte x = 0;
#line 100
        byte l1 = 1;
#line 200

        var d1 = () =>
        {
            x += l1;
        };

        var d2 = () =>
        {
            short x = 0;
            short l2 = l1;
            var d3 = () =>
            {
                x += l2;
            };

            var d4 = () =>
            {
                int x = 0;
                int l3 = 3 + l2;
            };
        };
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.Test", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(4, locals.Count);

                VerifyLocal(testData, typeName, locals[0], "<>m0", "d1", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Action V_1, //d1
                System.Action V_2) //d2
  IL_0000:  ldloc.1
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "d2", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Action V_1, //d1
                System.Action V_2) //d2
  IL_0000:  ldloc.2
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[2], "<>m2", "x", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Action V_1, //d1
                System.Action V_2) //d2
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""byte C.<>c__DisplayClass0_0.x""
  IL_0006:  ret
}");

                VerifyLocal(testData, typeName, locals[3], "<>m3", "l1", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Action V_1, //d1
                System.Action V_2) //d2
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""byte C.<>c__DisplayClass0_0.l1""
  IL_0006:  ret
}");

                locals.Free();
            });
        }

        [Fact, WorkItem(67177, "https://github.com/dotnet/roslyn/issues/67177")]
        public void CapturingAndShadowing_06()
        {
            var source =
@"class C
{
    static void Test()
    {
        byte x = 0;
        byte l1 = 1;

        var d1 = () =>
        {
            x += l1;
        };

        var d2 = () =>
        {
            short x = 0;
#line 100
            short l2 = l1;
#line 200
            var d3 = () =>
            {
                x += l2;
            };

            var d4 = () =>
            {
                int x = 0;
                int l3 = 3 + l2;
            };
        };
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass0_0.<Test>b__1", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(5, locals.Count);

                VerifyLocal(testData, typeName, locals[0], "<>m0", "l1", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
                System.Action V_1, //d3
                System.Action V_2) //d4
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""byte C.<>c__DisplayClass0_0.l1""
  IL_0006:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "d3", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
                System.Action V_1, //d3
                System.Action V_2) //d4
  IL_0000:  ldloc.1
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[2], "<>m2", "d4", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
                System.Action V_1, //d3
                System.Action V_2) //d4
  IL_0000:  ldloc.2
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[3], "<>m3", "x", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
                System.Action V_1, //d3
                System.Action V_2) //d4
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""short C.<>c__DisplayClass0_1.x""
  IL_0006:  ret
}");

                VerifyLocal(testData, typeName, locals[4], "<>m4", "l2", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
                System.Action V_1, //d3
                System.Action V_2) //d4
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""short C.<>c__DisplayClass0_1.l2""
  IL_0006:  ret
}");

                locals.Free();
            });
        }

        [Fact, WorkItem(67177, "https://github.com/dotnet/roslyn/issues/67177")]
        public void CapturingAndShadowing_07()
        {
            var source =
@"class C
{
    static void Test()
    {
        byte x = 0;
        byte l1 = 1;

        var d1 = () =>
        {
            x += l1;
        };

        var d2 = () =>
        {
            short x = 0;
            short l2 = l1;

            var d3 = () =>
            {
                x += l2;
            };

            var d4 = () =>
            {
                int x = 0;
#line 100
                int l3 = 3 + l2;
#line 200
            };
        };
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass0_1.<Test>b__3", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(3, locals.Count);

                VerifyLocal(testData, typeName, locals[0], "<>m0", "l2", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0, //x
                int V_1) //l3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""short C.<>c__DisplayClass0_1.l2""
  IL_0006:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "x", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //x
                int V_1) //l3
  IL_0000:  ldloc.0
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[2], "<>m2", "l3", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //x
                int V_1) //l3
  IL_0000:  ldloc.1
  IL_0001:  ret
}");

                locals.Free();
            });
        }

        [Fact, WorkItem(67177, "https://github.com/dotnet/roslyn/issues/67177")]
        public void CapturingAndShadowing_11()
        {
            var source =
@"class C
{
    void Test()
    {
        int x = 0;
        int z = 1;

        void d1()
        {
            x += z;
        };

        void d2()
        {
            sbyte x = 0;
            int y = x;

            void d3()
            {
                y += z;
            };

            x = -100;
#line 100
            z += x;
#line 200
        };
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<Test>g__d2|0_1", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(3, locals.Count);

                VerifyLocal(testData, typeName, locals[0], "<>m0", "z", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
                sbyte V_1) //x
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.z""
  IL_0006:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "x", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
                sbyte V_1) //x
  IL_0000:  ldloc.1
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[2], "<>m2", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0, //CS$<>8__locals0
                sbyte V_1) //x
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_1.y""
  IL_0006:  ret
}");

                locals.Free();
            });
        }

        [Fact, WorkItem(67177, "https://github.com/dotnet/roslyn/issues/67177")]
        public void CapturingAndShadowing_12()
        {
            var source =
@"class C
{
    void Test()
    {
        int x = 0;
        int z = 1;

        void d1()
        {
            x += z;
        };

        void d2()
        {
            sbyte x = 0;
            int y = x;

            void d3()
            {
#line 100
                y += z;
#line 200
            };

            x = -100;
            z += x;
        };
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<Test>g__d3|0_2", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(3, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.x""
  IL_0006:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "z", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.z""
  IL_0006:  ret
}");

                VerifyLocal(testData, typeName, locals[2], "<>m2", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_1.y""
  IL_0006:  ret
}");

                locals.Free();
            });
        }

        [Fact, WorkItem(67177, "https://github.com/dotnet/roslyn/issues/67177")]
        public void CapturingAndShadowing_13()
        {
            var source =
@"class C
{
    void Test()
    {
        int x = 0;
        int z = 1;

        void d1()
        {
            x += z;
        };

        void d2(sbyte x)
        {
            int y = x;

            void d3(short x)
            {
                y += z;
            };

            x = -100;
#line 100
            z += x;
#line 200
        };
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<Test>g__d2|0_1", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(3, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0) //CS$<>8__locals0
  IL_0000:  ldarg.0
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "z", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0) //CS$<>8__locals0
  IL_0000:  ldarg.1
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.z""
  IL_0006:  ret
}");

                VerifyLocal(testData, typeName, locals[2], "<>m2", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0) //CS$<>8__locals0
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_1.y""
  IL_0006:  ret
}");

                locals.Free();
            });
        }

        [Fact, WorkItem(67177, "https://github.com/dotnet/roslyn/issues/67177")]
        public void CapturingAndShadowing_14()
        {
            var source =
@"class C
{
    void Test()
    {
        int x = 0;
        int z = 1;

        void d1()
        {
            x += z;
        };

        void d2(sbyte x)
        {
            int y = x;

            void d3(short x)
            {
#line 100
                y += z;
#line 200
            };

            x = -100;
            z += x;
        };
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<Test>g__d3|0_2", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(3, locals.Count);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "z", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.z""
  IL_0006:  ret
}");

                VerifyLocal(testData, typeName, locals[2], "<>m2", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.2
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_1.y""
  IL_0006:  ret
}");

                locals.Free();
            });
        }

        [Fact, WorkItem(67177, "https://github.com/dotnet/roslyn/issues/67177")]
        public void CapturingAndShadowing_15()
        {
            var source =
@"class C
{
    static void Test()
    {
        byte x = 0;
#line 100
        byte l1 = 1;
#line 200

        void d1()
        {
            x += l1;
        };

        void d2()
        {
            short x = 0;
            short l2 = l1;

            void d3()
            {
                x += l2;
            };

            void d4()
            {
                int x = 0;
                int l3 = 3 + l2;
            };
        };
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.Test", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(2, locals.Count);

                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""byte C.<>c__DisplayClass0_0.x""
  IL_0006:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "l1", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""byte C.<>c__DisplayClass0_0.l1""
  IL_0006:  ret
}");

                locals.Free();
            });
        }

        [Fact, WorkItem(67177, "https://github.com/dotnet/roslyn/issues/67177")]
        public void CapturingAndShadowing_16()
        {
            var source =
@"class C
{
    static void Test()
    {
        byte x = 0;
        byte l1 = 1;

        void d1()
        {
            x += l1;
        };

        void d2()
        {
            short x = 0;
#line 100
            short l2 = l1;
#line 200

            void d3()
            {
                x += l2;
            };

            void d4()
            {
                int x = 0;
                int l3 = 3 + l2;
            };
        };
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<Test>g__d2|0_1", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(3, locals.Count);

                VerifyLocal(testData, typeName, locals[0], "<>m0", "l1", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0) //CS$<>8__locals0
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""byte C.<>c__DisplayClass0_0.l1""
  IL_0006:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "x", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0) //CS$<>8__locals0
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""short C.<>c__DisplayClass0_1.x""
  IL_0006:  ret
}");

                VerifyLocal(testData, typeName, locals[2], "<>m2", "l2", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_1 V_0) //CS$<>8__locals0
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""short C.<>c__DisplayClass0_1.l2""
  IL_0006:  ret
}");

                locals.Free();
            });
        }

        [Fact, WorkItem(67177, "https://github.com/dotnet/roslyn/issues/67177")]
        public void CapturingAndShadowing_17()
        {
            var source =
@"class C
{
    static void Test()
    {
        byte x = 0;
        byte l1 = 1;

        void d1()
        {
            x += l1;
        };

        void d2()
        {
            short x = 0;
            short l2 = l1;

            void d3()
            {
                x += l2;
            };

            void d4()
            {
                int x = 0;
#line 100
                int l3 = 3 + l2;
#line 200
            };
        };
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<Test>g__d4|0_3", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Equal(3, locals.Count);

                VerifyLocal(testData, typeName, locals[0], "<>m0", "l2", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0, //x
                int V_1) //l3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""short C.<>c__DisplayClass0_1.l2""
  IL_0006:  ret
}");

                VerifyLocal(testData, typeName, locals[1], "<>m1", "x", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //x
                int V_1) //l3
  IL_0000:  ldloc.0
  IL_0001:  ret
}");

                VerifyLocal(testData, typeName, locals[2], "<>m2", "l3", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //x
                int V_1) //l3
  IL_0000:  ldloc.1
  IL_0001:  ret
}");

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_00100_CapturedParameterInsideCapturingInstanceMethod(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    int M()
    {
#line 100
        ;
#line 200
        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<y>P""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Int32 <>x.<>m1(" + (isStruct ? "ref " : "") + "C <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_00200_CapturedParameterShadowedByLocalInsideCapturingInstanceMethod(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    int M()
    {
        {
#line 100
            string y = null;
#line 200
        }

        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0, //y
                int V_1)
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m1(" + (isStruct ? "ref " : "") + "C <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_00300_CapturedParameterShadowedByLocalInsideCapturingInstanceMethod()
        {
            var source =
@"class C(int y)
{
    int M()
    {
        {
#line 100
            string y = null;
#line 200
            var d = () => y;
            _ = d().Length;
        }

        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m2", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass2_0 V_0, //CS$<>8__locals0
            System.Func<string> V_1, //d
            int V_2)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""string C.<>c__DisplayClass2_0.y""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m2");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m2(C <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_00400_CapturedParameterInsideLambdaInInstanceMethod_NoDisplayClass()
        {
            var source =
@"class C(int y)
{
    int M()
    {
        var d = () =>
                {
                    this.ToString();
#line 100
                    ;
#line 200
                };

        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>b__2_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<y>P""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Int32 <>x.<>m1(C <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_00500_CapturedParameterInsideLambdaInInstanceMethod_WithDisplayClass()
        {
            var source =
@"class C<T>(T y)
{
    T M(string x)
    {
        var d = () =>
                {
                    this.ToString();
                    x.ToString();
#line 100
                    ;
#line 200
                };

        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass2_0.<M>b__0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName + "<T>", y, "<>m2", "y", expectedILOpt:
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C<T> C<T>.<>c__DisplayClass2_0.<>4__this""
  IL_0006:  ldfld      ""T C<T>.<y>P""
  IL_000b:  ret
}");
                var methodData = testData.GetMethodData("<>x<T>.<>m2");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("T <>x<T>.<>m2(C<T>.<>c__DisplayClass2_0 <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_00600_CapturedParameterInsideStaticLambdaInInstanceMethod(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    int M()
    {
        var d = static (string x) =>
                {
                    x.ToString();
#line 100
                    ;
#line 200
                };

        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c.<M>b__2_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Empty(locals.Where(l => l.LocalName == "y"));

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_00700_CapturedParameterShadowedByParameterInsideLambdaInInstanceMethod()
        {
            var source =
@"class C(int y)
{
    int M()
    {
        var d = (string y) =>
                {
                    y.ToString();
                    this.ToString();
#line 100
                    ;
#line 200
                };

        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>b__2_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m1(C <>4__this, System.String y)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_00800_CapturedParameterShadowedByParameterInsideLambdaInInstanceMethod()
        {
            var source =
@"class C(int y)
{
    int M()
    {
        var d = (string y) =>
                {
                    y.ToString();
                    this.ToString();

                    var d1 = () => y;
                    _ = d1().Length;
#line 100
                    ;
#line 200
                };

        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>b__2_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass2_0 V_0, //CS$<>8__locals0
                System.Func<string> V_1) //d1
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""string C.<>c__DisplayClass2_0.y""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m1(C <>4__this, System.String y)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_00900_CapturedParameterShadowedByLocalInsideLambdaInInstanceMethod()
        {
            var source =
@"class C(int y)
{
    int M()
    {
        var d = (string x) =>
                {
                    string y = x;
                    y.ToString();
                    this.ToString();
#line 100
                    ;
#line 200
                };

        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>b__2_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m2", "y", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0) //y
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m2");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m2(C <>4__this, System.String x)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_01000_CapturedParameterShadowedByLocalInsideLambdaInInstanceMethod()
        {
            var source =
@"class C(int y)
{
    int M()
    {
        var d = (string x) =>
                {
                    string y = x;
                    y.ToString();
                    this.ToString();

                    var d1 = () => y;
                    _ = d1().Length;
#line 100
                    ;
#line 200
                };

        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>b__2_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m3", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass2_0 V_0, //CS$<>8__locals0
                System.Func<string> V_1) //d1
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""string C.<>c__DisplayClass2_0.y""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m3");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m3(C <>4__this, System.String x)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_01100_CapturedParameterInsideLocalFunctionInInstanceMethod_NoDisplayClass()
        {
            var source =
@"class C(int y)
{
    int M()
    {
        void d()
        {
            this.ToString();
#line 100
            ;
#line 200
        }

        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>g__d|2_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<y>P""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Int32 <>x.<>m1(C <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_01200_CapturedParameterInsideLocalFunctionInInstanceMethod_WithDisplayClass()
        {
            var source =
@"class C(int y)
{
    int M(string x)
    {
        void d()
        {
            this.ToString();
            x.ToString();
#line 100
            ;
#line 200
        }

        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>g__d|2_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m2", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<y>P""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m2");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Int32 <>x.<>m2(C <>4__this, ref C.<>c__DisplayClass2_0 value)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_01201_CapturedParameterInsideLocalFunctionInInstanceMethod_WithDisplayClass()
        {
            var source =
@"class C(int value)
{
    int M(string x)
    {
        void d()
        {
            this.ToString();
            x.ToString();
#line 100
            ;
#line 200
        }

        return value;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>g__d|2_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "value").Single();

                VerifyLocal(testData, typeName, y, "<>m2", "value", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<value>P""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m2");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Int32 <>x.<>m2(C <>4__this, ref C.<>c__DisplayClass2_0 value)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_01300_CapturedParameterInsideStaticLocalFunctionInInstanceMethod(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    int M()
    {
        static void d(string x)
        {
            x.ToString();
#line 100
            ;
#line 200
        }

        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>g__d|2_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                Assert.Empty(locals.Where(l => l.LocalName == "y"));

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_01400_CapturedParameterShadowedByParameterInsideLocalFunctionInInstanceMethod()
        {
            var source =
@"class C(int y)
{
    int M()
    {
        void d(string y)
        {
            y.ToString();
            this.ToString();
#line 100
            ;
#line 200
        }

        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>g__d|2_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m1(C <>4__this, System.String y)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_01401_CapturedParameterShadowedByParameterInsideLocalFunctionInInstanceMethod()
        {
            var source =
@"class C(int y)
{
    int M(int x)
    {
        void d(string y)
        {
            y.ToString();
            x.ToString();
            this.ToString();
#line 100
            ;
#line 200
        }

        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>g__d|2_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m1(C <>4__this, System.String y, ref C.<>c__DisplayClass2_0 value)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_01402_CapturedParameterShadowedByParameterInsideLocalFunctionInInstanceMethod()
        {
            var source =
@"class C(int value)
{
    int M(int x)
    {
        void d(string value)
        {
            value.ToString();
            x.ToString();
            this.ToString();
#line 100
            ;
#line 200
        }

        return value;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>g__d|2_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "value").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "value", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m1(C <>4__this, System.String value, ref C.<>c__DisplayClass2_0 value)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_01500_CapturedParameterShadowedByLocalInsideLocalFunctionInInstanceMethod()
        {
            var source =
@"class C(int y)
{
    int M()
    {
        void d(string x)
        {
            string y = x;
            y.ToString();
            this.ToString();
#line 100
            ;
#line 200
        }

        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>g__d|2_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m2", "y", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0) //y
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m2");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m2(C <>4__this, System.String x)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_01501_CapturedParameterShadowedByLocalInsideLocalFunctionInInstanceMethod()
        {
            var source =
@"class C(int y)
{
    int M(int z)
    {
        void d(string x)
        {
            string y = x;
            y.ToString();
            z.ToString();
            this.ToString();
#line 100
            ;
#line 200
        }

        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>g__d|2_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m3", "y", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0) //y
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m3");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m3(C <>4__this, System.String x, ref C.<>c__DisplayClass2_0 value)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_01502_CapturedParameterShadowedByLocalInsideLocalFunctionInInstanceMethod()
        {
            var source =
@"class C(int value)
{
    int M(int z)
    {
        void d(string x)
        {
            string value = x;
            value.ToString();
            z.ToString();
            this.ToString();
#line 100
            ;
#line 200
        }

        return value;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>g__d|2_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "value").Single();

                VerifyLocal(testData, typeName, y, "<>m3", "value", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0) //value
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m3");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m3(C <>4__this, System.String x, ref C.<>c__DisplayClass2_0 value)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_01503_CapturedParameterShadowedByLocalInsideLocalFunctionInInstanceMethod()
        {
            var source =
@"class C(int y)
{
    int M(int z)
    {
        void d(string value)
        {
            string y = value;
            y.ToString();
            z.ToString();
            this.ToString();
#line 100
            ;
#line 200
        }

        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>g__d|2_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m3", "y", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0) //y
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m3");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m3(C <>4__this, System.String value, ref C.<>c__DisplayClass2_0 value)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_01600_CapturedParameterInsideNonCapturingInstanceMethod(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    int M1()
    {
        return y;
    }

    void M2()
    {
#line 100
        ;
#line 200
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M2", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<y>P""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Int32 <>x.<>m1(" + (isStruct ? "ref " : "") + "C <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_01601_CapturedParameterInsideNonCapturingInstanceMethod(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C<T>(T y)
{
    T M1()
    {
        return y;
    }

    void M2()
    {
#line 100
        ;
#line 200
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M2", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName + "<T>", y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""T C<T>.<y>P""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x<T>.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("T <>x<T>.<>m1(" + (isStruct ? "ref " : "") + "C<T> <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_01700_CapturedParameterShadowedByParameterInsideNonCapturingInstanceMethod(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    int M1()
    {
        return y;
    }

    void M2(string y)
    {
#line 100
        ;
#line 200
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M2", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m1(" + (isStruct ? "ref " : "") + "C <>4__this, System.String y)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_01800_CapturedParameterShadowedByParameterInsideNonCapturingInstanceMethod(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    int M1()
    {
        return y;
    }

    void M2(string y)
    {
        var d = () => y;
        _ = d().Length;
#line 100
        ;
#line 200
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M2", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass3_0 V_0, //CS$<>8__locals0
                System.Func<string> V_1) //d
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""string C.<>c__DisplayClass3_0.y""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m1(" + (isStruct ? "ref " : "") + "C <>4__this, System.String y)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_01900_CapturedParameterShadowedByLocalInsideNonCapturingInstanceMethod(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    int M1()
    {
        return y;
    }

    void M2()
    {
        string y = null;
#line 100
        ;
#line 200
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M2", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0) //y
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m1(" + (isStruct ? "ref " : "") + "C <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_02000_CapturedParameterShadowedByLocalInsideNonCapturingInstanceMethod(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    int M1()
    {
        return y;
    }

    void M2()
    {
        string y = null;
        var d = () => y;
        _ = d().Length;
#line 100
        ;
#line 200
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M2", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m2", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass3_0 V_0, //CS$<>8__locals0
                System.Func<string> V_1) //d
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""string C.<>c__DisplayClass3_0.y""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m2");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m2(" + (isStruct ? "ref " : "") + "C <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_02100_CapturedParameterInsideStaticMethod(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    int M1()
    {
        return y;
    }

    static void M2()
    {
#line 100
        ;
#line 200
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M2", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);

                Assert.Empty(locals.Where(l => l.LocalName == "y"));

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_02200_CapturedParameterInsideLambdaInStaticMethod(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    int M1()
    {
        return y;
    }

    static void M2(string x)
    {
        var d = () =>
                {
#line 100
                    ;
#line 200
                    x.ToString();
                };
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass3_0.<M2>b__0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);

                Assert.Empty(locals.Where(l => l.LocalName == "y"));

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_02300_CapturedParameterInsideLocalFunctionInStaticMethod(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    int M1()
    {
        return y;
    }

    static void M2(string x)
    {
        void d()
        {
#line 100
            ;
#line 200
            x.ToString();
        }
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M2>g__d|3_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);

                Assert.Empty(locals.Where(l => l.LocalName == "y"));

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_02400_CapturedParameterInsideInstanceFieldInitializer(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C<T>(T y)
{
#line 100
    T Y = y;
#line 200
    T M() => y;
}
";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor(T)", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName + "<T>", y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""T C<T>.<y>P""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x<T>.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("T <>x<T>.<>m1(" + (isStruct ? "out " : "") + "C<T> <>4__this, T y)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_02700_CapturedParameterInsideLambdaInInstanceFieldInitializer_NoDisplayClass()
        {
            var source =
@"class C(int y)
{
    System.Func<int> Y = () =>
                         {
#line 100
                            return y;
#line 200
                         };

    int M() => y;
}
";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<.ctor>b__0_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<y>P""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Int32 <>x.<>m1(C <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_02800_CapturedParameterInsideLambdaInInstanceFieldInitializer_WithDisplayClass()
        {
            var source =
@"class C(int y, int x)
{
    System.Func<int> Y = () =>
                         {
#line 100
                            return y + x;
#line 200
                         };

    int M() => y;
}
";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass0_0.<.ctor>b__0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m2", "y", expectedILOpt:
@"{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<>c__DisplayClass0_0.<>4__this""
  IL_0006:  ldfld      ""int C.<y>P""
  IL_000b:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m2");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Int32 <>x.<>m2(C.<>c__DisplayClass0_0 <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_02900_CapturedParameterInsideLambdaInInstanceFieldInitializer(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y, int x)
{
    System.Func<int> Y = () =>
                         {
#line 100
                            return x;
#line 200
                         };

    int M() => y;
}
";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass0_0.<.ctor>b__0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);

                Assert.Empty(locals.Where(l => l.LocalName == "y"));

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_03000_CapturedParameterInsideLambdaInInstanceFieldInitializer(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    System.Func<int> Y = () =>
                         {
#line 100
                            return 1;
#line 200
                         };

    int M() => y;
}
";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c.<.ctor>b__0_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);

                Assert.Empty(locals.Where(l => l.LocalName == "y"));

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_03100_CapturedParameterInsideLambdaInInstanceFieldInitializer(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    System.Func<int> Y = static () =>
                         {
#line 100
                            return 1;
#line 200
                         };

    int M() => y;
}
";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c.<.ctor>b__0_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);

                Assert.Empty(locals.Where(l => l.LocalName == "y"));

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_03200_CapturedParameterShadowedByParameterInsideLambdaInInstanceFieldInitializer()
        {
            var source =
@"class C(int y, int x)
{
    System.Func<string, int> Y = (string y) =>
                         {
#line 100
                            return y.Length + x;
#line 200
                         };

    int M() => y + x;
}
";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<.ctor>b__0_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m1(C <>4__this, System.String y)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_03300_CapturedParameterShadowedByParameterInsideLambdaInInstanceFieldInitializer()
        {
            var source =
@"class C(int y, int x)
{
    System.Func<string, int> Y = (string y) =>
                         {
                            var d = () => y;
                            _ = d().Length;
#line 100
                            return y.Length + x;
#line 200
                         };

    int M() => y + x;
}
";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<.ctor>b__0_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Func<string> V_1, //d
                int V_2)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""string C.<>c__DisplayClass0_0.y""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m1(C <>4__this, System.String y)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_03400_CapturedParameterShadowedByLocalInsideLambdaInInstanceFieldInitializer()
        {
            var source =
@"class C(int y, int x)
{
    System.Func<string, int> Y = (string z) =>
                         {
                            string y = z;
#line 100   
                            return y.Length + x;
#line 200
                         };

    int M() => y + x;
}
";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<.ctor>b__0_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m3", "y", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0, //y
                int V_1)
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m3");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m3(C <>4__this, System.String z)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_03500_CapturedParameterShadowedByLocalInsideLambdaInInstanceFieldInitializer()
        {
            var source =
@"class C(int y, int x)
{
    System.Func<string, int> Y = (string z) =>
                         {
                            string y = z;
                            var d = () => y;
                            _ = d().Length;
#line 100   
                            return y.Length + x;
#line 200
                         };

    int M() => y + x;
}
";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<.ctor>b__0_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m4", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Func<string> V_1, //d
                int V_2)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""string C.<>c__DisplayClass0_0.y""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m4");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.String <>x.<>m4(C <>4__this, System.String z)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_03600_CapturedParameterInsideStaticFieldInitializer(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
#line 100
    static int Y = 1;
#line 200
    int M() => y;
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C..cctor", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);

                Assert.Empty(locals.Where(l => l.LocalName == "y"));

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_03700_CapturedParameterInsidePrimaryConstructorInitializer()
        {
            var source =
@"class C(int y) : 
#line 100
                   Base(1)
#line 200
{
    int M() => y;
}

class Base(int x);
";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<y>P""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Int32 <>x.<>m1(C <>4__this, System.Int32 y)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_03800_ThisInLambdaUsingCapturedParameterInsideInstanceFieldInitializer_NoDisplayClass()
        {
            var source =
@"class C(int y)
{
    System.Func<int> Y = () =>
                         {
#line 100
                             return y;
#line 200
                         };

    int M() => y;
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<.ctor>b__0_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "this").Single();

                VerifyLocal(testData, typeName, y, "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m0");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("C <>x.<>m0(C <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_03900_ThisInLambdaUsingCapturedParameterInsideInstanceFieldInitializer_WithDisplayClass()
        {
            var source =
@"class C(int y, int x)
{
    System.Func<int> Y = () =>
                         {
#line 100
                             return y + x;
#line 200
                         };

    int M() => y;
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass0_0.<.ctor>b__0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "this").Single();

                VerifyLocal(testData, typeName, y, "<>m0", "this", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<>c__DisplayClass0_0.<>4__this""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m0");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("C <>x.<>m0(C.<>c__DisplayClass0_0 <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_04000_ThisInLambdaUsingCapturedParameterInsidePrimaryConstructorInitializer_NoDisplayClass()
        {
            var source =
@"class C(int y)
                  : Base(() =>
                         {
#line 100
                             return y;
#line 200
                         })
{
    int M() => y;
}

class Base(System.Func<int> x);
";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<.ctor>b__0_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "this").Single();

                VerifyLocal(testData, typeName, y, "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m0");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("C <>x.<>m0(C <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_04100_ThisInLambdaUsingCapturedParameterInsidePrimaryConstructorInitializer_WithDisplayClass()
        {
            var source =
@"class C(int y, int x)
                  : Base(() =>
                         {
#line 100
                             return y + x;
#line 200
                         })
{
    int M() => y;
}

class Base(System.Func<int> x);
";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass0_0.<.ctor>b__0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "this").Single();

                VerifyLocal(testData, typeName, y, "<>m0", "this", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<>c__DisplayClass0_0.<>4__this""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m0");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("C <>x.<>m0(C.<>c__DisplayClass0_0 <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_04600_ThisInLambdaUsingNotCapturedParameterInsideInstanceFieldInitializer()
        {
            var source =
@"class C(int y)
{
    System.Func<int> Y = () =>
                         {
#line 100
                             return y;
#line 200
                         };
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass0_0.<.ctor>b__0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);

                Assert.Empty(locals.Where(l => l.LocalName == "this"));

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_04700_CapturedParameterInsideRegularInstanceConstructor(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    public C() : this(1)
    {
#line 100
        ;
#line 200
    }

    int M()
    {
        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor()", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<y>P""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Int32 <>x.<>m1(" + (isStruct ? "out " : "") + "C <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_04800_CapturedParameterShadowedByParameterInsideRegularConstructor(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    C(byte y) : this(0)
    {
#line 100
        _ = y;
#line 200
    }

    int M() => y;
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor(Byte)", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Byte <>x.<>m1(" + (isStruct ? "out " : "") + "C <>4__this, System.Byte y)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_04900_CapturedParameterInsideRegularInstanceConstructorInitializer(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    public C() :
#line 100
          this(1)
#line 200
    {
    }

    int M()
    {
        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor()", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<y>P""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Int32 <>x.<>m1(" + (isStruct ? "out " : "") + "C <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_05000_CapturedParameterShadowedByParameterInsideRegularConstructorInitializer(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y) 
{
    C(byte y) :
#line 100
                this((int)y)
#line 200
    {
    }

    int M() => y;
}
";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor(Byte)", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Byte <>x.<>m1(" + (isStruct ? "out " : "") + "C <>4__this, System.Byte y)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_05100_NotCapturedParameterInsideInstanceMethod(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    void M()
    {
#line 100
        ;
#line 200
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);

                Assert.Empty(locals.Where(l => l.LocalName == "y"));

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_05200_NotCapturedParameterInsideStaticMethod(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    static void M()
    {
#line 100
        ;
#line 200
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);

                Assert.Empty(locals.Where(l => l.LocalName == "y"));

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_05300_NotCapturedParameterInsideInstanceFieldInitializer(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
#line 100
    int Y = y;
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor(Int32)", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Int32 <>x.<>m1(" + (isStruct ? "out " : "") + "C <>4__this, System.Int32 y)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_05400_NotCapturedParameterInsideStaticFieldInitializer(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
#line 100
    static int Y = 1;
#line 200
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C..cctor", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);

                Assert.Empty(locals.Where(l => l.LocalName == "y"));

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_05500_NotCapturedParameterInsidePrimaryConstructorInitializer()
        {
            var source =
@"class C(int y) : 
#line 100
                   Base(1)
#line 200
{
}

class Base(int x);
";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Int32 <>x.<>m1(C <>4__this, System.Int32 y)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_05600_NotCapturedParameterInsideRegularInstanceConstructor(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    public C() : this(1)
    {
#line 100
        ;
#line 200
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor()", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);

                Assert.Empty(locals.Where(l => l.LocalName == "y"));

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_05700_NotCapturedParameterInsideRegularInstanceConstructorInitializer(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    public C() :
#line 100
          this(1)
#line 200
    {
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C..ctor()", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);

                Assert.Empty(locals.Where(l => l.LocalName == "y"));

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_05800_NotCapturedParameterShadowedByMemberInsideInstanceMethod(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    int y = y;

    void M()
    {
#line 100
        ;
#line 200
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);

                Assert.Empty(locals.Where(l => l.LocalName == "y"));

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_05900_NotCapturedParameterShadowedByMemberInsideStaticMethod(bool isStruct)
        {
            var source =
(isStruct ? "struct" : "class") + @" C(int y)
{
    static int y = 1;

    static void M()
    {
#line 100
        ;
#line 200
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);

                Assert.Empty(locals.Where(l => l.LocalName == "y"));

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_06001_CapturedParameterInsideCapturingAsyncInstanceMethod(bool isStruct)
        {
            var source =
@"
using System.Threading.Tasks;
" +
(isStruct ? "struct" : "class") + @" C(int y)
{
    async Task<int> M()
    {
#line 100
        ;
#line 200
        return y;
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>d__2.MoveNext", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
                isStruct ?
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""C C.<M>d__2.<>4__this""
  IL_0006:  ldfld      ""int C.<y>P""
  IL_000b:  ret
}" :
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<M>d__2.<>4__this""
  IL_0006:  ldfld      ""int C.<y>P""
  IL_000b:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Int32 <>x.<>m1(C.<M>d__2 <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_06002_CapturedParameterInsideCapturingIteratorInstanceMethod(bool isStruct)
        {
            var source =
@"
using System.Collections.Generic;
" +
(isStruct ? "struct" : "class") + @" C(int y)
{
    public IEnumerable<int> M()
    {
#line 100
        yield return 9;
#line 200
        yield return y;
    }
}
";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>d__2.MoveNext", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
                isStruct ?
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""C C.<M>d__2.<>4__this""
  IL_0006:  ldfld      ""int C.<y>P""
  IL_000b:  ret
}" :
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<M>d__2.<>4__this""
  IL_0006:  ldfld      ""int C.<y>P""
  IL_000b:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Int32 <>x.<>m1(C.<M>d__2 <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_06003_CapturedParameterInsideCapturingInstanceMethodLambda_NoDisplayClass()
        {
            var source =
@"
using System.Collections.Generic;

class C(int y)
{
    public int M()
    {
        System.Func<int> x = ()
#line 100
                                => y;
#line 200
        return x();
    }
}
";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<M>b__2_0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m1", "y", expectedILOpt:
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<y>P""
  IL_0006:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m1");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Int32 <>x.<>m1(C <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_06004_CapturedParameterInsideCapturingInstanceMethodLambda_WithDisplayClass()
        {
            var source =
@"
using System.Collections.Generic;

class C(int y)
{
    public int M(int a)
    {
        System.Func<int> x = ()
#line 100
                                => y + a;
#line 200
        return x();
    }
}
";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass2_0.<M>b__0", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m2", "y", expectedILOpt:
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<>c__DisplayClass2_0.<>4__this""
  IL_0006:  ldfld      ""int C.<y>P""
  IL_000b:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m2");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Int32 <>x.<>m2(C.<>c__DisplayClass2_0 <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_06005_NotCapturedParameterInsideAsyncLambda()
        {
            var source =
@"
using System.Threading.Tasks;

class C(int y)
{
    public System.Func<Task<int>> F = async Task<int> () =>
                                      {
#line 100
                                          await Task.Yield();
#line 200
                                          return y;
                                      };
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass0_0.<<-ctor>b__0>d.MoveNext", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m0", "y", expectedILOpt:
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                System.Runtime.CompilerServices.YieldAwaitable V_3,
                C.<>c__DisplayClass0_0.<<-ctor>b__0>d V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_0.<<-ctor>b__0>d.<>4__this""
  IL_0006:  ldfld      ""int C.<>c__DisplayClass0_0.y""
  IL_000b:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m0");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Int32 <>x.<>m0(C.<>c__DisplayClass0_0.<<-ctor>b__0>d <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }

        [Fact]
        public void PrimaryConstructors_06006_NotCapturedParameterInsideIteratorLocalFunction()
        {
            var source =
@"
using System.Collections.Generic;

class C(int y)
{
    public System.Func<IEnumerable<int>> F = IEnumerable<int>() =>
                                             {
                                                 IEnumerable<int> local()
                                                 {
#line 100
                                                     yield return 9;
#line 200
                                                     yield return y;
                                                 };

                                                 return local();
                                             };
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, targetDebugFormat: DebugInformationFormat.PortablePdb, validator: runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass0_0.<<-ctor>g__local|1>d.MoveNext", atLineNumber: 100);

                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.NotEqual(0, assembly.Count);

                var y = locals.Where(l => l.LocalName == "y").Single();

                VerifyLocal(testData, typeName, y, "<>m0", "y", expectedILOpt:
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_0.<<-ctor>g__local|1>d.<>4__this""
  IL_0006:  ldfld      ""int C.<>c__DisplayClass0_0.y""
  IL_000b:  ret
}");
                var methodData = testData.GetMethodData("<>x.<>m0");
                Assert.True(methodData.Method.IsStatic);
                AssertEx.Equal("System.Int32 <>x.<>m0(C.<>c__DisplayClass0_0.<<-ctor>g__local|1>d <>4__this)", ((MethodSymbol)methodData.Method).ToTestDisplayString());

                locals.Free();
            });
        }
    }
}
