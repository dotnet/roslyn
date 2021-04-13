// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using System;
using Xunit;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class AccessibilityTests : ExpressionCompilerTestBase
    {
        /// <summary>
        /// Do not allow calling accessors directly.
        /// (This is consistent with the native EE.)
        /// </summary>
        [Fact]
        public void NotReferencable()
        {
            var source =
@"class C
{
    object P { get { return null; } }
    void M()
    {
    }
}";
            ResultProperties resultProperties;
            string error;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "this.get_P()",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Equal("error CS0571: 'C.P.get': cannot explicitly call operator or accessor", error);
        }

        [Fact]
        public void ParametersAndReturnType_PrivateType()
        {
            var source =
@"class A
{
    private struct S { }
}
class B
{
    static T F<T>(T t)
    {
        return t;
    }
    static void M()
    {
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "B.M",
                expr: "F(new A.S())");
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (A.S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""A.S""
  IL_0008:  ldloc.0
  IL_0009:  call       ""A.S B.F<A.S>(A.S)""
  IL_000e:  ret
}");
        }

        [Fact]
        public void ParametersAndReturnType_DifferentCompilation()
        {
            var source =
@"class C
{
    static T F<T>(T t)
    {
        return t;
    }
    static void M()
    {
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "F(new { P = 1 })");
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_0006:  call       ""<anonymous type: int P> C.F<<anonymous type: int P>>(<anonymous type: int P>)""
  IL_000b:  ret
}");
        }

        /// <summary>
        /// The compiler generates calls to the least derived virtual method while
        /// the EE calls the most derived method. However, the difference will be
        /// observable only in cases where C# and CLR diff in how overrides are
        /// determined (when override differs by ref/out or modopt for instance).
        /// </summary>
        [Fact]
        public void ProtectedAndInternalVirtualCalls()
        {
            var source =
@"internal class A
{
    protected virtual object M(object o) { return o; }
    internal virtual object P { get { return null; } }
}
internal class B : A
{
    protected override object M(object o) { return o; }
}
internal class C : B
{
    internal override object P { get { return null; } }
    object M()
    {
        return this.M(this.P);
    }
}";
            var compilation0 = CreateCompilation(
                source,
                options: TestOptions.DebugDll,
                assemblyName: Guid.NewGuid().ToString("D"));

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("this.M(this.P)", out error, testData);

                testData.GetMethodData("<>x.<>m0").VerifyIL(
    @"
{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (object V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  callvirt   ""object C.P.get""
  IL_0007:  callvirt   ""object B.M(object)""
  IL_000c:  ret
}
");
            });
        }

        [Fact]
        public void InferredTypeArguments_DifferentCompilation()
        {
            var source =
@"class C
{
    static object F<T, U>(T t, U u)
    {
        return t;
    }
    static object x = new { A = 1 };
    static void M()
    {
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "F(new { A = 2 }, new { B = 3 })"); // new and existing types
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldc.i4.2
  IL_0001:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_0006:  ldc.i4.3
  IL_0007:  newobj     ""<>f__AnonymousType1<int>..ctor(int)""
  IL_000c:  call       ""object C.F<<anonymous type: int A>, <anonymous type: int B>>(<anonymous type: int A>, <anonymous type: int B>)""
  IL_0011:  ret
}");
        }
    }
}
