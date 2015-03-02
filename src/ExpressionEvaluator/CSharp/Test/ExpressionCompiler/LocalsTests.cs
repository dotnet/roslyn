// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.NotNull(assembly);
            Assert.Equal(assembly.Count, 0);
            Assert.Equal(locals.Count, 0);
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M",
                atLineNumber: 999);

            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.NotNull(assembly);
            Assert.NotEqual(assembly.Count, 0);

            Assert.Equal(locals.Count, 4);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0, //b
                int& V_1,
                int V_2,
                C V_3,
                bool V_4,
                int V_5) //c
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
            VerifyLocal(testData, typeName, locals[1], "<>m1", "a", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0, //b
                int& V_1,
                int V_2,
                C V_3,
                bool V_4,
                int V_5) //c
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
            VerifyLocal(testData, typeName, locals[2], "<>m2", "b", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (string V_0, //b
                int& V_1,
                int V_2,
                C V_3,
                bool V_4,
                int V_5) //c
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
            VerifyLocal(testData, typeName, locals[3], "<>m3", "c", expectedILOpt:
@"{
  // Code size        3 (0x3)
  .maxstack  1
  .locals init (string V_0, //b
                int& V_1,
                int V_2,
                C V_3,
                bool V_4,
                int V_5) //c
  IL_0000:  ldloc.s    V_5
  IL_0002:  ret
}");
            locals.Free();
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            byte[] exeBytes;
            byte[] pdbBytes;
            ImmutableArray<MetadataReference> references;
            compilation0.EmitAndGetReferences(out exeBytes, out pdbBytes, out references);
            var runtime = CreateRuntimeInstance(
                ExpressionCompilerUtilities.GenerateUniqueName(),
                references,
                exeBytes,
                new SymReader(pdbBytes),
                includeLocalSignatures: false);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M",
                atLineNumber: 999);

            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

            Assert.Equal(locals.Count, 2);
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

            ResultProperties resultProperties;
            string error;
            testData = new CompilationTestData();
            context.CompileExpression("b", out resultProperties, out error, testData);
            Assert.Equal(error, "error CS0103: The name 'b' does not exist in the current context");

            testData = new CompilationTestData();
            context.CompileExpression("a[1]", out resultProperties, out error, testData);
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

            Assert.Equal(locals.Count, 2);
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: true, typeName: out typeName, testData: testData);

            Assert.Equal(locals.Count, 1);
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.F",
                atLineNumber: 999);
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

            Assert.Equal(locals.Count, 2);
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
        }

        [WorkItem(928113)]
        [Fact]
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.F",
                atLineNumber: 888);
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(locals.Count, 3);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "w");
            VerifyLocal(testData, typeName, locals[1], "<>m1", "y", DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
@"{
// Code size        2 (0x2)
.maxstack  1
.locals init (bool V_0,
            string V_1,
            int V_2)
IL_0000:  ldc.i4.3
IL_0001:  ret
}");
            VerifyLocal(testData, typeName, locals[2], "<>m2", "v", DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
@"{
// Code size        2 (0x2)
.maxstack  1
.locals init (bool V_0,
            string V_1,
            int V_2)
IL_0000:  ldc.i4.0
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
            Assert.Equal(locals.Count, 5);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "w");
            VerifyLocal(testData, typeName, locals[1], "<>m1", "u");
            VerifyLocal(testData, typeName, locals[2], "<>m2", "y", DkmClrCompilationResultFlags.ReadOnlyResult);
            VerifyLocal(testData, typeName, locals[3], "<>m3", "v", DkmClrCompilationResultFlags.ReadOnlyResult);
            VerifyLocal(testData, typeName, locals[4], "<>m4", "z", DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe);
            byte[] exeBytes;
            byte[] pdbBytes;
            ImmutableArray<MetadataReference> references;
            compilation0.EmitAndGetReferences(out exeBytes, out pdbBytes, out references);
            var constantSignatures = ImmutableDictionary.CreateRange(
                new Dictionary<string, byte[]>()
                {
                    { "y", new byte[] { 0x11, 0x08 } }
                });
            var runtime = CreateRuntimeInstance(ExpressionCompilerUtilities.GenerateUniqueName(), references, exeBytes, new SymReader(pdbBytes, constantSignatures));
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(locals.Count, 2);

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

            VerifyLocal(testData, "<>x", locals[1], "<>m1", "y", DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}");
            locals.Free();
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe);
            byte[] exeBytes;
            byte[] pdbBytes;
            ImmutableArray<MetadataReference> references;
            compilation0.EmitAndGetReferences(out exeBytes, out pdbBytes, out references);
            var constantSignatures = ImmutableDictionary.CreateRange(
                new Dictionary<string, byte[]>()
                {
                    { "t", new byte[] { 0x15, 0x11, 0x10, 0x01, 0x13, 0x00 } },
                    { "u", new byte[] { 0x15, 0x11, 0x10, 0x01, 0x1e, 0x00 } }
                });
            var runtime = CreateRuntimeInstance(ExpressionCompilerUtilities.GenerateUniqueName(), references, exeBytes, new SymReader(pdbBytes, constantSignatures));
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(locals.Count, 3);

            VerifyLocal(testData, "<>x<T>", locals[0], "<>m0<U>", "t", DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}",
                expectedGeneric: true);

            VerifyLocal(testData, "<>x<T>", locals[1], "<>m1<U>", "u", DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}",
                expectedGeneric: true);

            VerifyLocal(testData, "<>x<T>", locals[2], "<>m2<U>", "<>TypeVariables", DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var runtime = CreateRuntimeInstance(compilation0);
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
            Assert.Equal(locals.Count, 5);
            locals.Free();
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.<>c__DisplayClass1_1.<M>b__0");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

            VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass1_0 V_0, //CS$<>8__locals0
                object V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<>c__DisplayClass1_1.<>4__this""
  IL_0006:  ret
}");
            VerifyLocal(testData, typeName, locals[1], "<>m1", "_1", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C.<>c__DisplayClass1_0 V_0, //CS$<>8__locals0
                object V_1)
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
            VerifyLocal(testData, typeName, locals[2], "<>m2", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass1_0 V_0, //CS$<>8__locals0
  object V_1)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""object C.<>c__DisplayClass1_0.y""
  IL_0006:  ret
}");
            VerifyLocal(testData, typeName, locals[3], "<>m3", "x", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C.<>c__DisplayClass1_0 V_0, //CS$<>8__locals0
  object V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object C.<>c__DisplayClass1_1.x""
  IL_0006:  ret
}");
            Assert.Equal(locals.Count, 4);
            locals.Free();
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.<>c.<Main>b__0_0");

            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

            VerifyLocal(testData, typeName, locals[0], "<>m0", "x2", expectedILOpt:
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
            VerifyLocal(testData, typeName, locals[1], "<>m1", "x3");
            VerifyLocal(testData, typeName, locals[2], "<>m2", "x4");
            VerifyLocal(testData, typeName, locals[3], "<>m3", "x1");
            Assert.Equal(locals.Count, 4);

            locals.Free();

            context = CreateMethodContext(
                runtime,
                methodName: "C.<>c__DisplayClass0_0.<Main>b__1");

            testData = new CompilationTestData();
            locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

            VerifyLocal(testData, typeName, locals[0], "<>m0", "y2", expectedILOpt:
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
            VerifyLocal(testData, typeName, locals[1], "<>m1", "y3");
            VerifyLocal(testData, typeName, locals[2], "<>m2", "y1");
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
            Assert.Equal(locals.Count, 6);
            locals.Free();

            context = CreateMethodContext(
                runtime,
                methodName: "C.<>c__DisplayClass0_1.<Main>b__2");

            testData = new CompilationTestData();
            locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

            VerifyLocal(testData, typeName, locals[0], "<>m0", "z2", expectedILOpt:
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
            VerifyLocal(testData, typeName, locals[1], "<>m1", "z1");
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
            Assert.Equal(locals.Count, 7);
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
            Assert.Equal(locals.Count, 7);
            locals.Free();
        }

        /// <summary>
        /// Should not include "this" inside display class
        /// instance method if "this" is not captured.
        /// </summary>
        [Fact]
        public void NoThisInsideDisplayClassInstanceMethod()
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.<>c__DisplayClass0_0.<M>b__0");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(3, locals.Count);
            VerifyLocal(testData, "<>x<T>", locals[0], "<>m0", "y");
            VerifyLocal(testData, "<>x<T>", locals[1], "<>m1", "x");
            VerifyLocal(testData, "<>x<T>", locals[2], "<>m2", "<>TypeVariables", DkmClrCompilationResultFlags.ReadOnlyResult);
            locals.Free();

            context = CreateMethodContext(
                runtime,
                methodName: "C.<>c__DisplayClass0_1.<M>b__1");
            testData = new CompilationTestData();
            locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(locals.Count, 4);
            VerifyLocal(testData, "<>x<T>", locals[0], "<>m0", "z");
            VerifyLocal(testData, "<>x<T>", locals[1], "<>m1", "y");
            VerifyLocal(testData, "<>x<T>", locals[2], "<>m2", "x");
            VerifyLocal(testData, "<>x<T>", locals[3], "<>m3", "<>TypeVariables", DkmClrCompilationResultFlags.ReadOnlyResult);
            locals.Free();
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "A.B.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

            Assert.Equal(locals.Count, 6);
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
            Assert.Equal(containingType.TypeParameters[1], returnType.TypeArguments[0]);
            Assert.Equal(containingType.TypeParameters[2], returnType.TypeArguments[1]);
            returnType = returnType.ContainingType;
            Assert.Equal(containingType.TypeParameters[0], returnType.TypeArguments[0]);

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
            Assert.Equal(containingType.TypeParameters[2], returnType.TypeArguments[0]);
            returnType = returnType.ContainingType;
            Assert.Equal(containingType.TypeParameters[1], returnType.TypeArguments[0]);

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

            VerifyLocal(testData, "<>x<T, U, V>", locals[5], "<>m5<W>", "<>TypeVariables", DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
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
            Assert.Equal(containingType.TypeParameters[0], returnType.TypeArguments[0]);
            Assert.Equal(containingType.TypeParameters[1], returnType.TypeArguments[1]);
            Assert.Equal(containingType.TypeParameters[2], returnType.TypeArguments[2]);
            Assert.Equal(method.TypeParameters[0], returnType.TypeArguments[3]);

            // Verify <>c__TypeVariables type was emitted (#976772).
            using (var metadata = ModuleMetadata.CreateFromImage(ImmutableArray.CreateRange(assembly)))
            {
                var reader = metadata.MetadataReader;
                var typeDef = reader.GetTypeDef("<>c__TypeVariables");
                reader.CheckTypeParameters(typeDef.GetGenericParameters(), "T", "U", "V", "W");
            }

            locals.Free();
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.<>c__DisplayClass0_0.<M>b__0");

            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);

            Assert.Equal(locals.Count, 3);
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
            VerifyLocal(testData, "<>x<T, U>", locals[2], "<>m2", "<>TypeVariables", DkmClrCompilationResultFlags.ReadOnlyResult);

            var method = (MethodSymbol)testData.GetMethodData("<>x<T, U>.<>m1").Method;
            var containingType = method.ContainingType;
            Assert.Equal(containingType.TypeParameters[1], method.ReturnType);

            locals.Free();
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.<F>d__2.MoveNext",
                atLineNumber: 999);
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(locals.Count, 2);
            VerifyLocal(testData, "<>x", locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                bool V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<F>d__2.<>4__this""
  IL_0006:  ret
}");
            VerifyLocal(testData, "<>x", locals[1], "<>m1", "o", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                bool V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object C.<F>d__2.<o>5__3""
  IL_0006:  ret
}");
            locals.Free();
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var runtime = CreateRuntimeInstance(compilation0);
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
                bool V_1,
                int V_2,
                bool V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""T[] C.<F>d__0<T>.o""
  IL_0006:  ret
}");
            VerifyLocal(testData, "<>x<T>", locals[1], "<>m1", "i", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                bool V_1,
                int V_2,
                bool V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0<T>.<i>5__1""
  IL_0006:  ret
}");
            VerifyLocal(testData, "<>x<T>", locals[2], "<>m2", "t", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                bool V_1,
                int V_2,
                bool V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""T C.<F>d__0<T>.<t>5__2""
  IL_0006:  ret
}");
            VerifyLocal(testData, "<>x<T>", locals[3], "<>m3", "<>TypeVariables", DkmClrCompilationResultFlags.ReadOnlyResult);
            Assert.Equal(locals.Count, 4);
            locals.Free();
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
            var compilation0 = CreateCompilationWithMscorlib45(
                source,
                options: TestOptions.DebugDll,
                references: new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef });

            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "S.<F>d__1.MoveNext");

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
            VerifyLocal(testData, "<>x<T, U>", locals[3], "<>m3", "<>TypeVariables", DkmClrCompilationResultFlags.ReadOnlyResult);

            Assert.Equal(locals.Count, 4);
            locals.Free();
        }

        [Fact]
        public void Async_StaticMethod()
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
            var compilation0 = CreateCompilationWithMscorlib45(
                source,
                options: TestOptions.DebugDll,
                references: new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef });

            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.<M>d__1.MoveNext");

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
                object V_2,
                C.<M>d__1 V_3,
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
                object V_2,
                C.<M>d__1 V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object C.<M>d__1.<y>5__1""
  IL_0006:  ret
}");
            Assert.Equal(locals.Count, 2);
            locals.Free();
        }

        [WorkItem(995976)]
        [Fact]
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
            var compilation0 = CreateCompilationWithMscorlib45(
                source,
                options: TestOptions.DebugDll,
                references: new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef });

            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.<M>d__2.MoveNext");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(locals.Count, 2);
            VerifyLocal(testData, "<>x", locals[0], "<>m0", "x", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                C.<M>d__2 V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__2.x""
  IL_0006:  ret
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
        }

        [WorkItem(996571)]
        [Fact]
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
            var compilation0 = CreateCompilationWithMscorlib(
                source0,
                options: TestOptions.DebugDll);
            var compilation1 = CreateCompilationWithMscorlib(
                source1,
                options: TestOptions.DebugDll,
                references: new[] { compilation0.EmitToImageReference() });
            byte[] exeBytes;
            byte[] pdbBytes;
            ImmutableArray<MetadataReference> references;
            compilation1.EmitAndGetReferences(out exeBytes, out pdbBytes, out references);
            var runtime = CreateRuntimeInstance(
                ExpressionCompilerUtilities.GenerateUniqueName(),
                ImmutableArray.Create(MscorlibRef), // no reference to compilation0
                exeBytes,
                new SymReader(pdbBytes));
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(locals.Count, 0);
            locals.Free();
        }

        [WorkItem(996571)]
        [Fact]
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
            var compilation0 = CreateCompilationWithMscorlib(source0, options: TestOptions.DebugDll);
            var compilation1 = CreateCompilationWithMscorlib(
                source1,
                options: TestOptions.DebugDll,
                references: new[] { compilation0.EmitToImageReference() });
            byte[] exeBytes;
            byte[] pdbBytes;
            ImmutableArray<MetadataReference> references;
            compilation1.EmitAndGetReferences(out exeBytes, out pdbBytes, out references);
            var runtime = CreateRuntimeInstance(
                ExpressionCompilerUtilities.GenerateUniqueName(),
                ImmutableArray.Create(MscorlibRef), // no reference to compilation0
                exeBytes,
                new SymReader(pdbBytes));
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(locals.Count, 0);
            locals.Free();
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
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var runtime = CreateRuntimeInstance(comp);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M",
                atLineNumber: 999);

            ResultProperties resultProperties;
            string error;
            var testData = new CompilationTestData();
            context.CompileExpression("o = null", out resultProperties, out error, testData);
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
        }

        [WorkItem(1015887)]
        [Fact]
        public void LocalDateTimeConstant()
        {
            var source = @"
class C
{
    static void M()
    {
        const double d = 2.74745778612482E-266; // We'll change the signature using a test hook.
        System.DateTime dt = new System.DateTime(2010, 1, 2); // It's easier to figure out the signature to pass if this is here.
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            byte[] exeBytes;
            byte[] pdbBytes;
            ImmutableArray<MetadataReference> references;
            comp.EmitAndGetReferences(out exeBytes, out pdbBytes, out references);

            // We're going to override the true signature (i.e. double) with one indicating
            // that the constant is a System.DateTime.
            var constantSignatures = new Dictionary<string, byte[]>
            {
                { "d", new byte[] {0x11, 0x19} },
            }.ToImmutableDictionary();
            var runtime = CreateRuntimeInstance(ExpressionCompilerUtilities.GenerateUniqueName(), references, exeBytes, new SymReader(pdbBytes, constantSignatures));
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");

            string errorMessage;
            var testData = new CompilationTestData();
            context.CompileAssignment("d", "default(System.DateTime)", out errorMessage, testData);
            Assert.Equal("error CS0131: The left-hand side of an assignment must be a variable, property or indexer", errorMessage);

            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(2, locals.Count);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "dt");
            VerifyLocal(testData, typeName, locals[1], "<>m1", "d", DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
@"{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (System.DateTime V_0) //dt
  IL_0000:  ldc.i8     0x8cc5955a94ec006
  IL_0009:  newobj     ""System.DateTime..ctor(long)""
  IL_000e:  ret
}");
            // NB: We should actually see 0x8cc5955a94ec000, but the double literal is not precise enough to get the last place right.
            // We can't use a more precise representation and still have the compiler accept it as a constant value.
        }

        [WorkItem(1015887)]
        [Fact]
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
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            byte[] exeBytes;
            byte[] pdbBytes;
            ImmutableArray<MetadataReference> references;
            comp.EmitAndGetReferences(out exeBytes, out pdbBytes, out references);

            var constantSignatures = new Dictionary<string, byte[]>
            {
                { "d", new byte[] {0x11, 0x19} },
            }.ToImmutableDictionary();
            var runtime = CreateRuntimeInstance(ExpressionCompilerUtilities.GenerateUniqueName(), references, exeBytes, new SymReader(pdbBytes, constantSignatures));
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");

            string errorMessage;
            var testData = new CompilationTestData();
            context.CompileAssignment("d", "Nothing", out errorMessage, testData);
            Assert.Equal("error CS0131: The left-hand side of an assignment must be a variable, property or indexer", errorMessage);

            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(1, locals.Count);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
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
        }

        [Fact, WorkItem(1022165), WorkItem(1028883), WorkItem(1034204)]
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");

            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.NotNull(assembly);
            Assert.NotEqual(assembly.Count, 0);

            Assert.Equal(locals.Count, 5);
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
  .locals init (int V_0,
                bool V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.x""
  IL_0006:  ret
}";

            var compilation0 = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.<F>d__0.MoveNext");

            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.NotNull(assembly);
            Assert.NotEqual(assembly.Count, 0);

            Assert.Equal(locals.Count, 1);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: expectedIL);
            Assert.Equal(SpecialType.System_Int32, testData.GetMethodData(typeName + ".<>m0").Method.ReturnType.SpecialType);
            locals.Free();

            testData = new CompilationTestData();
            string error;
            context.CompileExpression("x", out error, testData);
            Assert.Null(error);
            var methodData = testData.GetMethodData("<>x.<>m0");
            methodData.VerifyIL(expectedIL);
            Assert.Equal(SpecialType.System_Int32, methodData.Method.ReturnType.SpecialType);
        }

        [Fact, WorkItem(1063254)]
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
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(compilation);
            string displayClassName;
            string typeName;
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            CompilationTestData testData;
            var ilTemplate = @"
{{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                bool V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""{0} C.{1}.{2}""
  IL_0006:  ret
}}";

            // M1(int, int)
            displayClassName = "<M1>d__0";
            GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: String.Format(ilTemplate, "int", displayClassName, "x"));
            VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: String.Format(ilTemplate, "int", displayClassName, "y"));
            locals.Clear();

            // M1(int, float)
            displayClassName = "<M1>d__1";
            GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: String.Format(ilTemplate, "int", displayClassName, "x"));
            VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: String.Format(ilTemplate, "float", displayClassName, "y"));
            locals.Clear();

            // M2(int, float)
            displayClassName = "<M2>d__2";
            GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: String.Format(ilTemplate, "int", displayClassName, "x"));
            VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: String.Format(ilTemplate, "float", displayClassName, "y"));
            locals.Clear();

            // M2(int, T)
            displayClassName = "<M2>d__3";
            GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
            typeName += "<T>";
            displayClassName += "<T>";
            VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: String.Format(ilTemplate, "int", displayClassName, "x"));
            VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: String.Format(ilTemplate, "T", displayClassName, "y"));
            locals.Clear();

            // M2(int, int)
            displayClassName = "<M2>d__4";
            GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: String.Format(ilTemplate, "int", displayClassName, "x"));
            VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: String.Format(ilTemplate, "int", displayClassName, "y"));
            locals.Clear();

            locals.Free();
        }

        [Fact, WorkItem(1063254)]
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(compilation);
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
            VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: String.Format(ilTemplate, "int", "int", displayClassName, "x"));
            locals.Clear();

            // M1(int, float)
            displayClassName = "<M1>d__1";
            GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: String.Format(ilTemplate, "float", "int", displayClassName, "x"));
            VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: String.Format(ilTemplate, "float", "float", displayClassName, "y"));
            locals.Clear();

            // M2(int, float)
            displayClassName = "<M2>d__2";
            GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: String.Format(ilTemplate, "float", "int", displayClassName, "x"));
            VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: String.Format(ilTemplate, "float", "float", displayClassName, "y"));
            locals.Clear();

            // M2(T)
            displayClassName = "<M2>d__3";
            GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
            VerifyLocal(testData, typeName + "<T>", locals[0], "<>m0", "x", expectedILOpt: String.Format(ilTemplate, "T", "T", displayClassName + "<T>", "x"));
            locals.Clear();

            // M2(int)
            displayClassName = "<M2>d__4";
            GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: String.Format(ilTemplate, "int", "int", displayClassName, "x"));
            locals.Clear();

            locals.Free();
        }

        [Fact, WorkItem(1063254)]
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
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(compilation);
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
  .locals init ({0} V_0)
  IL_0000:  ldarg.{1}
  IL_0001:  ret
}}";

            // y => x.ToString()
            displayClassName = "<>c__DisplayClass0_0";
            GetLocals(runtime, "C." + displayClassName + ".<M1>b__0", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "y", expectedILOpt: String.Format(voidRetILTemplate, 1));
            locals.Clear();

            // z => x
            displayClassName = "<>c__DisplayClass0_0";
            GetLocals(runtime, "C." + displayClassName + ".<M1>b__1", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "z", expectedILOpt: String.Format(funcILTemplate, "int", 1));
            locals.Clear();

            // y => y.ToString()
            displayClassName = "<>c__1";
            GetLocals(runtime, "C." + displayClassName + ".<M2>b__1_0", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
            VerifyLocal(testData, typeName + "<T>", locals[0], "<>m0", "y", expectedILOpt: String.Format(voidRetILTemplate, 1));
            locals.Clear();

            // z => z
            displayClassName = "<>c__1";
            GetLocals(runtime, "C." + displayClassName + ".<M2>b__1_1", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
            VerifyLocal(testData, typeName + "<T>", locals[0], "<>m0", "z", expectedILOpt: String.Format(funcILTemplate, "int", 1));
            locals.Clear();

            // t => t
            displayClassName = "<>c__1";
            GetLocals(runtime, "C." + displayClassName + ".<M2>b__1_2", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
            VerifyLocal(testData, typeName + "<T>", locals[0], "<>m0", "t", expectedILOpt: String.Format(funcILTemplate, "T", 1));
            locals.Clear();

            locals.Free();
        }

        [Fact, WorkItem(1063254)]
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
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(compilation);
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
            VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: String.Format(voidRetILTemplate, "int", 1));
            VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: String.Format(voidRetILTemplate, "int", 2));
            locals.Clear();

            // M1(int, string)
            GetLocals(runtime, "C.M1(Int32,String)", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: String.Format(funcILTemplate, "string", 1));
            VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: String.Format(funcILTemplate, "string", 2));
            locals.Clear();

            // M2(int, string)
            GetLocals(runtime, "C.M2(Int32,String)", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: String.Format(voidRetILTemplate, "string", 0));
            VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: String.Format(voidRetILTemplate, "string", 1));
            locals.Clear();

            // M2(int, T)
            GetLocals(runtime, "C.M2(Int32,T)", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
            VerifyLocal(testData, typeName, locals[0], "<>m0<T>", "x", expectedILOpt: String.Format(funcILTemplate, "T", 0), expectedGeneric: true);
            VerifyLocal(testData, typeName, locals[1], "<>m1<T>", "y", expectedILOpt: String.Format(funcILTemplate, "T", 1), expectedGeneric: true);
            locals.Clear();

            // M2(int, int)
            GetLocals(runtime, "C.M2(Int32,Int32)", argumentsOnly: true, locals: locals, count: 2, typeName: out typeName, testData: out testData);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: String.Format(funcILTemplate, "int", 0));
            VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: String.Format(refParamILTemplate, "int", 1));
            locals.Clear();

            locals.Free();
        }

        [Fact, WorkItem(1063254)]
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(compilation);
            string displayClassName;
            string typeName;
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            CompilationTestData testData;
            var iteratorILTemplate = @"
{{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init ({0} V_0,
                bool V_1)
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
            VerifyLocal(testData, typeName + "<T>", locals[0], "<>m0", "x", expectedILOpt: String.Format(iteratorILTemplate, "int", displayClassName, "x"));
            locals.Clear();

            // M2(int)
            displayClassName = "<M2>d__2";
            GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
            VerifyLocal(testData, typeName + "<T>", locals[0], "<>m0", "x", expectedILOpt: String.Format(iteratorILTemplate, "int", displayClassName, "x"));
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
            VerifyLocal(testData, typeName + "<T, T>", locals[0], "<>m0", "x", expectedILOpt: String.Format(asyncILTemplate, "T", displayClassName + "<T>", "x"));
            locals.Clear();

            // M4(int)
            displayClassName = "<M4>d__6";
            GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 1, typeName: out typeName, testData: out testData);
            VerifyLocal(testData, typeName + "<T, T>", locals[0], "<>m0", "x", expectedILOpt: String.Format(asyncILTemplate, "T", displayClassName + "<T>", "x"));
            locals.Clear();

            // M4()
            displayClassName = "<M4>d__7";
            GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly: true, locals: locals, count: 0, typeName: out typeName, testData: out testData);
            locals.Clear();

            locals.Free();
        }

        [WorkItem(1115030)]
        [Fact(Skip = "1115030")]
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
            var compilation0 = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.<M>d__1.MoveNext",
                atLineNumber: 999);
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
        }

        [WorkItem(947)]
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
            var libRef = CreateCompilationWithMscorlib(libSource).EmitToImageReference();
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemRef }, TestOptions.DebugDll);

            byte[] exeBytes;
            byte[] pdbBytes;
            ImmutableArray<MetadataReference> unusedReferences;
            var result = comp.EmitAndGetReferences(out exeBytes, out pdbBytes, out unusedReferences);
            Assert.True(result);

            var runtime = CreateRuntimeInstance(GetUniqueName(), ImmutableArray.Create(MscorlibRef, SystemRef, SystemCoreRef, SystemXmlLinqRef, libRef), exeBytes, new SymReader(pdbBytes));

            string typeName;
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            CompilationTestData testData;
            GetLocals(runtime, "C.M", argumentsOnly: false, locals: locals, count: 1, typeName: out typeName, testData: out testData);
            Assert.Equal("this", locals.Single().LocalName);
            locals.Free();
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
    }
}
