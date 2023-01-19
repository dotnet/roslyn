// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class ManagedAddressOfTests : ExpressionCompilerTestBase
    {
        [Fact]
        public void AddressOfParameter()
        {
            var source =
@"class C
{
    void M(string s)
    {
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                string error;
                context.CompileExpression("&s", out error, testData);
                Assert.Null(error);

                var methodData = testData.GetMethodData("<>x.<>m0");
                AssertIsStringPointer(((MethodSymbol)methodData.Method).ReturnType);
                methodData.VerifyIL(@"
{
  // Code size        4 (0x4)
  .maxstack  1
  IL_0000:  ldarga.s   V_1
  IL_0002:  conv.u
  IL_0003:  ret
}
");
            });
        }

        [Fact]
        public void AddressOfLocal()
        {
            var source =
@"class C
{
    void M()
    {
        string s = ""hello"";
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                string error;
                context.CompileExpression("&s", out error, testData);
                Assert.Null(error);

                var methodData = testData.GetMethodData("<>x.<>m0");
                AssertIsStringPointer(((MethodSymbol)methodData.Method).ReturnType);
                methodData.VerifyIL(@"
{
  // Code size        4 (0x4)
  .maxstack  1
  .locals init (string V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  conv.u
  IL_0003:  ret
}
");
            });
        }

        [Fact]
        public void AddressOfField()
        {
            var source =
@"class C
{
    string s = ""hello"";

    void M()
    {
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                string error;
                context.CompileExpression("&s", out error, testData);
                Assert.Null(error);

                var methodData = testData.GetMethodData("<>x.<>m0");
                AssertIsStringPointer(((MethodSymbol)methodData.Method).ReturnType);
                methodData.VerifyIL(@"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""string C.s""
  IL_0006:  conv.u
  IL_0007:  ret
}
");
            });
        }

        [Fact]
        public void Sizeof()
        {
            var source = @"
class C
{
    void M<T>()
    {
    }
}

delegate void D();

interface I
{
}

enum E
{
    A
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var types = new[]
                {
                    "C", // class
                    "D", // delegate
                    "I", // interface
                    "T", // type parameter
                    "int[]",
                    "dynamic",
                };

                foreach (var type in types)
                {
                    CompilationTestData testData = new CompilationTestData();
                    context.CompileExpression(string.Format("sizeof({0})", type), out var error, testData);
                    Assert.Null(error);

                    var expectedType = type switch
                    {
                        "dynamic" => "object",
                        _ => type
                    };

                    testData.GetMethodData("<>x.<>m0<T>").VerifyIL($$"""
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  sizeof     "{{expectedType}}"
  IL_0006:  ret
}
""");
                }
            });
        }

        [Fact]
        public void Stackalloc()
        {
            var source =
@"class C
{
    void M()
    {
        System.Action a;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                context.CompileAssignment("a", "() => { var s = stackalloc string[1]; }", out var error, testData);
                Assert.Equal("error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('string')", error);
            });
        }

        [Fact]
        public void PointerTypeOfManagedType()
        {
            var source =
@"class C
{
    void M()
    {
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                string error;
                context.CompileExpression("(string*)null", out error, testData);
                Assert.Null(error);

                var methodData = testData.GetMethodData("<>x.<>m0");
                AssertIsStringPointer(((MethodSymbol)methodData.Method).ReturnType);
                methodData.VerifyIL(@"
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  conv.u
  IL_0002:  ret
}
");
            });
        }

        [Fact]
        public void FixedArray()
        {
            var source =
@"class C
{
    void M(string[] args)
    {
        System.Action a;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                context.CompileAssignment("a", "() => { fixed (void* p = args) { } }", out var error, testData);
                Assert.Null(error);

                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       25 (0x19)
  .maxstack  3
  .locals init (System.Action V_0) //a
  IL_0000:  newobj     ""<>x.<>c__DisplayClass0_0..ctor()""
  IL_0005:  dup
  IL_0006:  ldarg.1
  IL_0007:  stfld      ""string[] <>x.<>c__DisplayClass0_0.args""
  IL_000c:  ldftn      ""void <>x.<>c__DisplayClass0_0.<<>m0>b__0()""
  IL_0012:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0017:  stloc.0
  IL_0018:  ret
}
");

                testData.GetMethodData("<>x.<>c__DisplayClass0_0.<<>m0>b__0").VerifyIL(@"
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (void* V_0, //p
                pinned string[] V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""string[] <>x.<>c__DisplayClass0_0.args""
  IL_0006:  dup
  IL_0007:  stloc.1
  IL_0008:  brfalse.s  IL_000f
  IL_000a:  ldloc.1
  IL_000b:  ldlen
  IL_000c:  conv.i4
  IL_000d:  brtrue.s   IL_0014
  IL_000f:  ldc.i4.0
  IL_0010:  conv.u
  IL_0011:  stloc.0
  IL_0012:  br.s       IL_001f
  IL_0014:  ldloc.1
  IL_0015:  ldc.i4.0
  IL_0016:  readonly.
  IL_0018:  ldelema    ""string""
  IL_001d:  conv.u
  IL_001e:  stloc.0
  IL_001f:  ldnull
  IL_0020:  stloc.1
  IL_0021:  ret
}
");
            });
        }

        private static void AssertIsStringPointer(TypeSymbol returnType)
        {
            Assert.Equal(TypeKind.Pointer, returnType.TypeKind);
            Assert.Equal(SpecialType.System_String, ((PointerTypeSymbol)returnType).PointedAtType.SpecialType);
        }
    }
}
