// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                AssertIsIntPtrPointer(methodData.Method.ReturnType);
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
                AssertIsIntPtrPointer(methodData.Method.ReturnType);
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
                AssertIsIntPtrPointer(methodData.Method.ReturnType);
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

        /// <remarks>
        /// It seems like we could make this work, but there are special cases for arrays
        /// and interfaces and some of the checks would have to be recursive.  Furthermore,
        /// dev12 didn't support it.
        /// </remarks>
        [Fact]
        public void DisallowSizeof()
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
                    string error;
                    CompilationTestData testData = new CompilationTestData();
                    context.CompileExpression(string.Format("sizeof({0})", type), out error, testData);
                    // CONSIDER: change error code to make text less confusing?
                    Assert.Equal(string.Format("error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('{0}')", type), error);
                }
            });
        }

        [Fact]
        public void DisallowStackalloc()
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
                string error;
                context.CompileAssignment("a", "() => { var s = stackalloc string[1]; }", out error, testData);
                // CONSIDER: change error code to make text less confusing?
                Assert.Equal("error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('string')", error);
            });
        }

        [Fact]
        public void DisallowPointerType()
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
                // CONSIDER: change error code to make text less confusing?
                Assert.Equal("error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('string')", error);
            });
        }

        /// <remarks>
        /// This is not so much disallowed as not specifically enabled.
        /// </remarks>
        [Fact]
        public void DisallowFixedArray()
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
                string error;
                context.CompileAssignment("a", "() => { fixed (void* p = args) { } }", out error, testData);
                // CONSIDER: change error code to make text less confusing?
                Assert.Equal("error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('string')", error);
            });
        }

        private static void AssertIsIntPtrPointer(ITypeSymbol returnType)
        {
            Assert.Equal(TypeKind.Pointer, returnType.TypeKind);
            Assert.Equal(SpecialType.System_IntPtr, ((PointerTypeSymbol)returnType).PointedAtType.SpecialType);
        }
    }
}
