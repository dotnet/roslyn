﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class LocalFunctionTests : ExpressionCompilerTestBase
    {
        [Fact]
        public void NoLocals()
        {
            var source =
@"class C
{
    void F(int x)
    {
        int y = x + 1;
        int G()
        {
            return 0;
        };
        int z = G();
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<F>g__G0_0");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.NotNull(assembly);
                Assert.Equal(assembly.Count, 0);
                Assert.Equal(locals.Count, 0);
                locals.Free();
            });
        }

        [Fact]
        public void Locals()
        {
            var source =
@"class C
{
    void F(int x)
    {
        int G(int y)
        {
            int z = y + 1;
            return z;
        };
        G(x + 1);
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<F>g__G0_0");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(locals.Count, 2);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "y", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //z
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "z", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //z
                int V_1)
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
                locals.Free();
                string error;
                context.CompileExpression("this.F(1)", out error, testData);
                Assert.Equal(error, "error CS0027: Keyword 'this' is not available in the current context");
            });
        }

        [Fact]
        public void CapturedVariable()
        {
            var source =
@"class C
{
    int x;
    void F(int y)
    {
        int G()
        {
            return x + y;
        };
        int z = G();
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<F>g__G1_0");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(locals.Count, 2);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<>c__DisplayClass1_0.<>4__this""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass1_0.y""
  IL_0006:  ret
}");
                locals.Free();
                testData = new CompilationTestData();
                string error;
                context.CompileExpression("this.F(1)", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
 @"{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<>c__DisplayClass1_0.<>4__this""
  IL_0006:  ldc.i4.1
  IL_0007:  callvirt   ""void C.F(int)""
  IL_000c:  ret
}");
            });
        }

        [Fact]
        public void MultipleDisplayClasses()
        {
            var source =
@"class C
{
    void F1(int x)
    {
        int F2(int y)
        {
            int F3() => x + y;
            return F3();
        };
        F2(1);
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<F1>g__F30_1");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(locals.Count, 2);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.x""
  IL_0006:  ret
}");
                VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_1.y""
  IL_0006:  ret
}");
                locals.Free();
                testData = new CompilationTestData();
                string error;
                context.CompileExpression("x + y", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
 @"{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.x""
  IL_0006:  ldarg.1
  IL_0007:  ldfld      ""int C.<>c__DisplayClass0_1.y""
  IL_000c:  add
  IL_000d:  ret
}");
            });
        }

        // Should not bind to unnamed display class parameters
        // (unnamed parameters are treated as named "value").
        [Fact]
        public void CapturedVariableNamedValue()
        {
            var source =
@"class C
{
    void F(int value)
    {
        int G()
        {
            return value + 1;
        };
        G();
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<F>g__G0_0");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(locals.Count, 1);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "value", expectedILOpt:
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.value""
  IL_0006:  ret
}");
                locals.Free();
                testData = new CompilationTestData();
                string error;
                context.CompileExpression("value", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
 @"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.value""
  IL_0006:  ret
}");
            });
        }

        // Should not bind to unnamed display class parameters
        // (unnamed parameters are treated as named "value").
        [WorkItem(18426, "https://github.com/dotnet/roslyn/issues/18426")]
        [Fact(Skip = "18426")]
        public void DisplayClassParameter()
        {
            var source =
@"class C
{
    void F(int x)
    {
        int G()
        {
            return x + 1;
        };
        G();
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<F>g__G0_0");
                var testData = new CompilationTestData();
                string error;
                context.CompileExpression("value", out error, testData);
                Assert.Equal(error, "error CS0103: The name 'value' does not exist in the current context");
            });
        }
    }
}