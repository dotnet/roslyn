// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    /// <summary>
    /// Compile expressions at type-scope to support
    /// expressions in DebuggerDisplayAttribute values.
    /// </summary>
    public class DebuggerDisplayAttributeTests : ExpressionCompilerTestBase
    {
        [Fact]
        public void FieldsAndProperties()
        {
            var source =
@"using System.Diagnostics;
[DebuggerDisplay(""{F}, {G}, {P}, {Q}"")]
class C
{
    static object F = 1;
    int G = 2;
    static int P { get { return 3; } }
    object Q { get { return 4; } }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateTypeContext(runtime, "C");
                // Static field.
                AssertEx.AssertEqualToleratingWhitespaceDifferences(
                    CompileExpression(context, "F"),
    @"{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  ldsfld     ""object C.F""
  IL_0005:  ret
}");
                // Instance field.
                AssertEx.AssertEqualToleratingWhitespaceDifferences(
                    CompileExpression(context, "G"),
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.G""
  IL_0006:  ret
}");
                // Static property.
                AssertEx.AssertEqualToleratingWhitespaceDifferences(
                    CompileExpression(context, "P"),
    @"{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  call       ""int C.P.get""
  IL_0005:  ret
}");
                // Instance property.
                AssertEx.AssertEqualToleratingWhitespaceDifferences(
                    CompileExpression(context, "Q"),
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  callvirt   ""object C.Q.get""
  IL_0006:  ret
}");
            });
        }

        [Fact]
        public void Constants()
        {
            var source =
@"using System.Diagnostics;
[DebuggerDisplay(""{F[G]}"")]
class C
{
    const string F = ""str"";
    const int G = 2;
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll, assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateTypeContext(runtime, "C");
                AssertEx.AssertEqualToleratingWhitespaceDifferences(
                    CompileExpression(context, "F[G]"),
    @"{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldstr      ""str""
  IL_0005:  ldc.i4.2
  IL_0006:  call       ""char string.this[int].get""
  IL_000b:  ret
}");
            });
        }

        [Fact]
        public void This()
        {
            var source =
@"using System.Diagnostics;
[DebuggerDisplay(""{F(this)}"")]
class C
{
    static object F(C c)
    {
        return c;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll, assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateTypeContext(runtime, "C");
                AssertEx.AssertEqualToleratingWhitespaceDifferences(
                    CompileExpression(context, "F(this)"),
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object C.F(C)""
  IL_0006:  ret
}");
            });
        }

        [Fact]
        public void Base()
        {
            var source =
@"using System.Diagnostics;
class A
{
    internal object F()
    {
        return 1;
    }
}
[DebuggerDisplay(""{base.F()}"")]
class B : A
{
    new object F()
    {
        return 2;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll, assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateTypeContext(runtime, "B");
                AssertEx.AssertEqualToleratingWhitespaceDifferences(
                    CompileExpression(context, "base.F()"),
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object A.F()""
  IL_0006:  ret
}");
            });
        }

        [Fact]
        public void GenericType()
        {
            var source =
@"using System.Diagnostics;
class A<T> where T : class
{
    [DebuggerDisplay(""{F(default(T), default(U))}"")]
    internal class B<U>
    {
        static object F<X, Y>(X x, Y y) where X : class
        {
            return x ?? (object)y;
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll, assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateTypeContext(runtime, "A.B");
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression("F(default(T), default(U))", out error, testData);
                string actualIL = testData.GetMethodData("<>x<T, U>.<>m0").GetMethodIL();
                AssertEx.AssertEqualToleratingWhitespaceDifferences(
                    actualIL,
    @"{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (T V_0,
  U V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""T""
  IL_0008:  ldloc.0
  IL_0009:  ldloca.s   V_1
  IL_000b:  initobj    ""U""
  IL_0011:  ldloc.1
  IL_0012:  call       ""object A<T>.B<U>.F<T, U>(T, U)""
  IL_0017:  ret
}");
                // Verify generated type is generic, but method is not.
                using (var metadata = ModuleMetadata.CreateFromImage(ImmutableArray.CreateRange(result.Assembly)))
                {
                    var reader = metadata.MetadataReader;
                    var typeDef = reader.GetTypeDef(result.TypeName);
                    reader.CheckTypeParameters(typeDef.GetGenericParameters(), "T", "U");
                    var methodDef = reader.GetMethodDef(typeDef, result.MethodName);
                    reader.CheckTypeParameters(methodDef.GetGenericParameters());
                }
            });
        }

        [Fact]
        public void Usings()
        {
            var source =
@"using System.Diagnostics;
using A = N;
using B = N.C;
namespace N
{
    [DebuggerDisplay(""{typeof(A.C) ?? typeof(B) ?? typeof(C)}"")]
    class C
    {
        void M()
        {
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll, assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateTypeContext(runtime, "N.C");
                string error;
                var testData = new CompilationTestData();
                // Expression compilation should succeed without imports.
                var result = context.CompileExpression("typeof(N.C) ?? typeof(C)", out error, testData);
                Assert.Null(error);
                AssertEx.AssertEqualToleratingWhitespaceDifferences(
                    testData.GetMethodData("<>x.<>m0").GetMethodIL(),
    @"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldtoken    ""N.C""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0018
  IL_000d:  pop
  IL_000e:  ldtoken    ""N.C""
  IL_0013:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0018:  ret
}");
                // Expression compilation should fail using imports since there are no symbols.
                context = CreateTypeContext(runtime, "N.C");
                testData = new CompilationTestData();
                result = context.CompileExpression("typeof(A.C) ?? typeof(B) ?? typeof(C)", out error, testData);
                Assert.Equal("error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)", error);
                testData = new CompilationTestData();
                result = context.CompileExpression("typeof(B) ?? typeof(C)", out error, testData);
                Assert.Equal("error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)", error);
            });
        }

        [Fact]
        public void Usings2()
        {
            var source =
@"using System.Diagnostics;
using A = int;
using B = (int, int);
using C = int[];
namespace N
{
    [DebuggerDisplay(""{typeof(A) ?? typeof(B) ?? typeof(C)}"")]
    class D
    {
        void M()
        {
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll, assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            compilation0.VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using A = int;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using A = int;").WithLocation(2, 1),
                // (3,1): hidden CS8019: Unnecessary using directive.
                // using B = (int, int);
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using B = (int, int);").WithLocation(3, 1),
                // (4,1): hidden CS8019: Unnecessary using directive.
                // using C = int[];
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using C = int[];").WithLocation(4, 1));
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateTypeContext(runtime, "N.D");
                string error;
                var testData = new CompilationTestData();
                // Expression compilation should fail using imports since there are no symbols.
                var result = context.CompileExpression("typeof(A) ?? typeof(B)", out error, testData);
                Assert.Equal("error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)", error);
                testData = new CompilationTestData();
                result = context.CompileExpression("typeof(A) ?? typeof(B) ?? typeof(C)", out error, testData);
                Assert.Equal("error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)", error);
                testData = new CompilationTestData();
                result = context.CompileExpression("typeof(B) ?? typeof(C)", out error, testData);
                Assert.Equal("error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)", error);
            });
        }

        [Fact]
        public void PseudoVariable()
        {
            var source =
@"using System.Diagnostics;
[DebuggerDisplay(""{$ReturnValue}"")]
class C
{
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll, assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateTypeContext(runtime, "C");
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression("$ReturnValue", out error, testData);
                Assert.Equal("error CS0103: The name '$ReturnValue' does not exist in the current context", error);
            });
        }

        [Fact]
        public void LambdaClosedOverThis()
        {
            var source =
@"class C
{
    object o;
    static object F(System.Func<object> f)
    {
        return f();
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll, assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateTypeContext(runtime, "C");
                AssertEx.AssertEqualToleratingWhitespaceDifferences(
    @"{
  // Code size       29 (0x1d)
  .maxstack  3
  IL_0000:  newobj     ""<>x.<>c__DisplayClass0_0..ctor()""
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      ""C <>x.<>c__DisplayClass0_0.<>4__this""
  IL_000c:  ldftn      ""object <>x.<>c__DisplayClass0_0.<<>m0>b__0()""
  IL_0012:  newobj     ""System.Func<object>..ctor(object, System.IntPtr)""
  IL_0017:  call       ""object C.F(System.Func<object>)""
  IL_001c:  ret
}",
                CompileExpression(context, "F(() => this.o)"));
            });
        }

        [Fact]
        public void FormatSpecifiers()
        {
            var source =
@"using System.Diagnostics;
[DebuggerDisplay(""{F, nq}, {F}"")]
class C
{
    object F = ""f"";
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll, assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateTypeContext(runtime, "C");
                // No format specifiers.
                string error;
                var result = context.CompileExpression("F", out error);
                Assert.NotNull(result.Assembly);
                Assert.Null(result.FormatSpecifiers);
                // Format specifiers.
                result = context.CompileExpression("F, nq,ac", out error);
                Assert.NotNull(result.Assembly);
                Assert.Equal(2, result.FormatSpecifiers.Count);
                Assert.Equal("nq", result.FormatSpecifiers[0]);
                Assert.Equal("ac", result.FormatSpecifiers[1]);
            });
        }

        [Fact]
        public void VirtualMethod()
        {
            var source = @"
using System.Diagnostics;

[DebuggerDisplay(""{GetDebuggerDisplay()}"")]
public class Base
{
    protected virtual string GetDebuggerDisplay()
    {
        return ""base"";
    }
}

public class Derived : Base
{
    protected override string GetDebuggerDisplay()
    {
        return ""derived"";
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll, assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateTypeContext(runtime, "Derived");
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression("GetDebuggerDisplay()", out error, testData);
                Assert.Null(error);
                var actualIL = testData.GetMethodData("<>x.<>m0").GetMethodIL();
                var expectedIL =
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  callvirt   ""string Derived.GetDebuggerDisplay()""
  IL_0006:  ret
}";
                AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualIL);
            });
        }

        private static string CompileExpression(EvaluationContext context, string expr)
        {
            string error;
            var testData = new CompilationTestData();
            var result = context.CompileExpression(expr, out error, testData);
            Assert.NotNull(result.Assembly);
            Assert.Null(error);
            return testData.GetMethodData(result.TypeName + "." + result.MethodName).GetMethodIL();
        }
    }
}
