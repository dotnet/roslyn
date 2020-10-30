// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class DeclarationTests : ExpressionCompilerTestBase
    {
        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        public void Declarations()
        {
            var source =
@"class C
{
    static object F;
    static void M<T>(object x)
    {
        object y;
        if (x == null)
        {
            object z;
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                DkmClrCompilationResultFlags flags;
                CompilationTestData testData;
                CompileDeclaration(context, "int z = 1, F = 2;", out flags, out testData);
                Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
    @"{
  // Code size       87 (0x57)
  .maxstack  4
  .locals init (object V_0, //y
                bool V_1,
                object V_2,
                System.Guid V_3)
  IL_0000:  ldtoken    ""int""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""z""
  IL_000f:  ldloca.s   V_3
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.3
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldtoken    ""int""
  IL_0023:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0028:  ldstr      ""F""
  IL_002d:  ldloca.s   V_3
  IL_002f:  initobj    ""System.Guid""
  IL_0035:  ldloc.3
  IL_0036:  ldnull
  IL_0037:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_003c:  ldstr      ""z""
  IL_0041:  call       ""ref int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_0046:  ldind.i4
  IL_0047:  ldc.i4.1
  IL_0048:  stind.i4
  IL_0049:  ldstr      ""F""
  IL_004e:  call       ""ref int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_0053:  ldind.i4
  IL_0054:  ldc.i4.2
  IL_0055:  stind.i4
  IL_0056:  ret
}");
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        public void DeconstructionDeclaration()
        {
            var source = @"
class C
{
    void Test()
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugDll, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            WithRuntimeInstance(comp, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef, MscorlibRef },
               validator: runtime =>
               {
                   var context = CreateMethodContext(runtime, methodName: "C.Test");

                   ResultProperties resultProperties;
                   string error;
                   var testData = new CompilationTestData();
                   ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                   context.CompileExpression(
                       "(int z1, string z2) = (1, null);",
                       DkmEvaluationFlags.None,
                       NoAliases,
                       DebuggerDiagnosticFormatter.Instance,
                       out resultProperties,
                       out error,
                       out missingAssemblyIdentities,
                       EnsureEnglishUICulture.PreferredOrNull,
                       testData);
                   Assert.Null(error);
                   Assert.Empty(missingAssemblyIdentities);

                   Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult, resultProperties.Flags);
                   Assert.Equal(DkmEvaluationResultCategory.Data, resultProperties.Category); // Data, because it is an expression
                   Assert.Equal(default(DkmEvaluationResultAccessType), resultProperties.AccessType);
                   Assert.Equal(default(DkmEvaluationResultStorageType), resultProperties.StorageType);
                   Assert.Equal(default(DkmEvaluationResultTypeModifierFlags), resultProperties.ModifierFlags);

                   testData.GetMethodData("<>x.<>m0(C)").VerifyIL(@"
{
  // Code size       94 (0x5e)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""int""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""z1""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldtoken    ""string""
  IL_0023:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0028:  ldstr      ""z2""
  IL_002d:  ldloca.s   V_0
  IL_002f:  initobj    ""System.Guid""
  IL_0035:  ldloc.0
  IL_0036:  ldnull
  IL_0037:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_003c:  ldstr      ""z1""
  IL_0041:  call       ""ref int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_0046:  ldind.i4
  IL_0047:  ldc.i4.1
  IL_0048:  stind.i4
  IL_0049:  ldstr      ""z2""
  IL_004e:  call       ""ref string Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<string>(string)""
  IL_0053:  ldind.ref
  IL_0054:  ldnull
  IL_0055:  stind.ref
  IL_0056:  ldc.i4.1
  IL_0057:  ldnull
  IL_0058:  newobj     ""System.ValueTuple<int, string>..ctor(int, string)""
  IL_005d:  ret
}");
               });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        public void DeconstructionDeclarationWithDiscard()
        {
            var source = @"
class C
{
    void Test()
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugDll, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            WithRuntimeInstance(comp, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef, MscorlibRef },
               validator: runtime =>
               {
                   var context = CreateMethodContext(runtime, methodName: "C.Test");

                   ResultProperties resultProperties;
                   string error;
                   var testData = new CompilationTestData();
                   ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                   context.CompileExpression(
                       "(_, string z2) = (1, null);",
                       DkmEvaluationFlags.None,
                       NoAliases,
                       DebuggerDiagnosticFormatter.Instance,
                       out resultProperties,
                       out error,
                       out missingAssemblyIdentities,
                       EnsureEnglishUICulture.PreferredOrNull,
                       testData);
                   Assert.Null(error);
                   Assert.Empty(missingAssemblyIdentities);

                   Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult, resultProperties.Flags);
                   Assert.Equal(DkmEvaluationResultCategory.Data, resultProperties.Category); // Data, because it is an expression
                   Assert.Equal(default(DkmEvaluationResultAccessType), resultProperties.AccessType);
                   Assert.Equal(default(DkmEvaluationResultStorageType), resultProperties.StorageType);
                   Assert.Equal(default(DkmEvaluationResultTypeModifierFlags), resultProperties.ModifierFlags);

                   testData.GetMethodData("<>x.<>m0(C)").VerifyIL(@"
{
  // Code size       51 (0x33)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""string""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""z2""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldstr      ""z2""
  IL_0023:  call       ""ref string Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<string>(string)""
  IL_0028:  ldind.ref
  IL_0029:  ldnull
  IL_002a:  stind.ref
  IL_002b:  ldc.i4.1
  IL_002c:  ldnull
  IL_002d:  newobj     ""System.ValueTuple<int, string>..ctor(int, string)""
  IL_0032:  ret
}
");
               });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        public void ExpressionLocals_ExpressionStatement_01()
        {
            var source =
@"class C
{
    static object F;
    static void M<T>(object x)
    {
        object y;
        if (x == null)
        {
            object z;
        }
    }

    static void Test(object x, out int y)
    {
        y = 1;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                DkmClrCompilationResultFlags flags;
                CompilationTestData testData;
                CompileDeclaration(context, "Test(x, out var z);", out flags, out testData);
                Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
    @"{
  // Code size       48 (0x30)
  .maxstack  4
  .locals init (object V_0, //y
                bool V_1,
                object V_2,
                System.Guid V_3)
  IL_0000:  ldtoken    ""int""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""z""
  IL_000f:  ldloca.s   V_3
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.3
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldarg.0
  IL_001f:  ldstr      ""z""
  IL_0024:  call       ""ref int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_0029:  ldind.i4
  IL_002a:  call       ""void C.Test(object, out int)""
  IL_002f:  ret
}");
            });
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/25702")]
        [WorkItem(13159, "https://github.com/dotnet/roslyn/issues/13159")]
        public void ExpressionLocals_ExpressionStatement_02()
        {
            var source =
@"class C
{
    static object F;
    static void M<T>(object x)
    {
        object y;
        if (x == null)
        {
            object z;
        }
    }

    static void Test(bool x)
    {
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                DkmClrCompilationResultFlags flags;
                CompilationTestData testData;
                CompileDeclaration(context, "Test(x is int z);", out flags, out testData);
                Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
    @"{
  // Code size       69 (0x45)
  .maxstack  4
  .locals init (object V_0, //y
                bool V_1,
                object V_2,
                System.Guid V_3,
                int V_4)
  IL_0000:  ldtoken    ""int""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""z""
  IL_000f:  ldloca.s   V_3
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.3
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldarg.0
  IL_001f:  isinst     ""int""
  IL_0024:  brfalse.s  IL_003e
  IL_0026:  ldarg.0
  IL_0027:  unbox.any  ""int""
  IL_002c:  stloc.s    V_4
  IL_002e:  ldstr      ""z""
  IL_0033:  call       ""int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_0038:  ldloc.s    V_4
  IL_003a:  stind.i4
  IL_003b:  ldc.i4.1
  IL_003c:  br.s       IL_003f
  IL_003e:  ldc.i4.0
  IL_003f:  call       ""void C.Test(bool)""
  IL_0044:  ret
}");
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        public void ExpressionLocals_Assignment_01()
        {
            var source =
@"class C
{
    static object F;
    static void M<T>(object x)
    {
        object y;
        if (x == null)
        {
            object z;
        }
    }

    static object Test(out int x)
    {
        x = 1;
        return x;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                CompilationTestData testData;
                string error;
                testData = new CompilationTestData();
                context.CompileAssignment("x", "Test(out var z)", out error, testData);
                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
    @"{
  // Code size       49 (0x31)
  .maxstack  4
  .locals init (object V_0, //y
                bool V_1,
                object V_2,
                System.Guid V_3)
  IL_0000:  ldtoken    ""int""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""z""
  IL_000f:  ldloca.s   V_3
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.3
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldstr      ""z""
  IL_0023:  call       ""ref int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_0028:  ldind.i4
  IL_0029:  call       ""object C.Test(out int)""
  IL_002e:  starg.s    V_0
  IL_0030:  ret
}");
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        public void ExpressionLocals_LocalDeclarationStatement_01()
        {
            var source =
@"class C
{
    static object F;
    static void M<T>(object x)
    {
        object y;
        if (x == null)
        {
            object z;
        }
    }

    static int Test(object x, out int y)
    {
        y = 1;
        return 0;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                DkmClrCompilationResultFlags flags;
                CompilationTestData testData;
                CompileDeclaration(context, "int z = Test(x, out var F);", out flags, out testData);
                Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
    @"{
  // Code size       90 (0x5a)
  .maxstack  4
  .locals init (object V_0, //y
                bool V_1,
                object V_2,
                System.Guid V_3)
  IL_0000:  ldtoken    ""int""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""z""
  IL_000f:  ldloca.s   V_3
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.3
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldtoken    ""int""
  IL_0023:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0028:  ldstr      ""F""
  IL_002d:  ldloca.s   V_3
  IL_002f:  initobj    ""System.Guid""
  IL_0035:  ldloc.3
  IL_0036:  ldnull
  IL_0037:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_003c:  ldstr      ""z""
  IL_0041:  call       ""ref int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_0046:  ldind.i4
  IL_0047:  ldarg.0
  IL_0048:  ldstr      ""F""
  IL_004d:  call       ""ref int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_0052:  ldind.i4
  IL_0053:  call       ""int C.Test(object, out int)""
  IL_0058:  stind.i4
  IL_0059:  ret
}");
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        public void References()
        {
            var source =
@"class C
{
    delegate void D();
    internal object F;
    static object G;
    static void M<T>(object x)
    {
        object y;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var aliases = ImmutableArray.Create(
                    VariableAlias("x", typeof(string)),
                    VariableAlias("y", typeof(int)),
                    VariableAlias("T", typeof(object)),
                    VariableAlias("D", "C"),
                    VariableAlias("F", typeof(int)));

                string error;
                var testData = new CompilationTestData();
                context.CompileExpression(
                    "(object)x ?? (object)y ?? (object)T ?? (object)F ?? ((C)D).F ?? C.G",
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    out error,
                    testData);

                Assert.Equal(1, testData.GetExplicitlyDeclaredMethods().Length);
                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
@"{
  // Code size       78 (0x4e)
  .maxstack  2
  .locals init (object V_0) //y
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_004d
  IL_0004:  pop
  IL_0005:  ldloc.0
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_004d
  IL_0009:  pop
  IL_000a:  ldstr      ""T""
  IL_000f:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0014:  dup
  IL_0015:  brtrue.s   IL_004d
  IL_0017:  pop
  IL_0018:  ldstr      ""F""
  IL_001d:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0022:  unbox.any  ""int""
  IL_0027:  box        ""int""
  IL_002c:  dup
  IL_002d:  brtrue.s   IL_004d
  IL_002f:  pop
  IL_0030:  ldstr      ""D""
  IL_0035:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_003a:  castclass  ""C""
  IL_003f:  ldfld      ""object C.F""
  IL_0044:  dup
  IL_0045:  brtrue.s   IL_004d
  IL_0047:  pop
  IL_0048:  ldsfld     ""object C.G""
  IL_004d:  ret
}");
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        public void Address()
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
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression(
                    "*(&c) = 'A'",
                    DkmEvaluationFlags.None,
                    ImmutableArray.Create(VariableAlias("c", typeof(char))),
                    out error,
                    testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
    @"{
  // Code size       19 (0x13)
  .maxstack  3
  .locals init (char V_0)
  IL_0000:  ldstr      ""c""
  IL_0005:  call       ""ref char Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<char>(string)""
  IL_000a:  ldind.u2
  IL_000b:  conv.u
  IL_000c:  ldc.i4.s   65
  IL_000e:  dup
  IL_000f:  stloc.0
  IL_0010:  stind.i2
  IL_0011:  ldloc.0
  IL_0012:  ret
}");
            });
        }

        [Fact]
        public void TreatAsExpression()
        {
            var source =
@"class C
{
    static void M(object x)
    {
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                string error;

                // Expression without ';' as statement.
                var result = context.CompileExpression("3", DkmEvaluationFlags.None, NoAliases, out error);
                Assert.Null(error);

                // Expression with ';' as statement.
                result = context.CompileExpression("3;", DkmEvaluationFlags.None, NoAliases, out error);
                Assert.Null(error);

                // Expression with format specifiers but without ';' as statement.
                result = context.CompileExpression("string.Empty, nq", DkmEvaluationFlags.None, NoAliases, out error);
                Assert.Null(error);
                AssertEx.SetEqual(result.FormatSpecifiers, new[] { "nq" });

                // Expression with format specifiers with ';' as statement.
                result = context.CompileExpression("string.Empty, nq;", DkmEvaluationFlags.None, NoAliases, out error);
                Assert.Equal("error CS1073: Unexpected token ','", error);
                Assert.Null(result);

                // Assignment without ';' as statement.
                result = context.CompileExpression("x = 2", DkmEvaluationFlags.None, NoAliases, out error);
                Assert.Null(error);

                // Assignment with ';' as statement.
                result = context.CompileExpression("x = 2;", DkmEvaluationFlags.None, NoAliases, out error);
                Assert.Null(error);

                // Statement without ';' as statement.
                result = context.CompileExpression("int o", DkmEvaluationFlags.None, NoAliases, out error);
                Assert.Equal("error CS1525: Invalid expression term 'int'", error);

                // Neither statement nor expression as statement.
                result = context.CompileExpression("M(;", DkmEvaluationFlags.None, NoAliases, out error);
                Assert.Equal("error CS1026: ) expected", error);

                // Statement without ';' as expression.
                result = context.CompileExpression("int o", DkmEvaluationFlags.TreatAsExpression, NoAliases, out error);
                Assert.Equal("error CS1525: Invalid expression term 'int'", error);

                // Statement with ';' as expression.
                result = context.CompileExpression("int o;", DkmEvaluationFlags.TreatAsExpression, NoAliases, out error);
                Assert.Equal("error CS1525: Invalid expression term 'int'", error);

                // Neither statement nor expression as expression.
                result = context.CompileExpression("M(;", DkmEvaluationFlags.TreatAsExpression, NoAliases, out error);
                Assert.Equal("error CS1026: ) expected", error);
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        public void BaseType()
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
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression(
                    "System.ValueType C = (int)$3;",
                    DkmEvaluationFlags.None,
                    ImmutableArray.Create(ObjectIdAlias(3, typeof(int))),
                    out error,
                    testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
    @"{
  // Code size       63 (0x3f)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""System.ValueType""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""C""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldstr      ""C""
  IL_0023:  call       ""ref System.ValueType Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<System.ValueType>(string)""
  IL_0028:  ldind.ref
  IL_0029:  ldstr      ""$3""
  IL_002e:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0033:  unbox.any  ""int""
  IL_0038:  box        ""int""
  IL_003d:  stind.ref
  IL_003e:  ret
}");
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        public void Var()
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

                DkmClrCompilationResultFlags flags;
                CompilationTestData testData;
                CompileDeclaration(context, "var x = 1;", out flags, out testData);
                Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
    @"{
  // Code size       44 (0x2c)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""int""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""x""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldstr      ""x""
  IL_0023:  call       ""ref int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_0028:  ldind.i4
  IL_0029:  ldc.i4.1
  IL_002a:  stind.i4
  IL_002b:  ret
}");
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        [WorkItem(1087216, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087216")]
        public void Dynamic()
        {
            var source =
@"class C
{
    static void M()
    {
    }
    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var compilation0 = CreateCompilation(source, new[] { CSharpRef }, TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                DkmClrCompilationResultFlags flags;
                CompilationTestData testData;
                CompileDeclaration(context, "dynamic d = 1;", out flags, out testData);
                Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       63 (0x3f)
  .maxstack  7
  IL_0000:  ldtoken    ""object""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""d""
  IL_000f:  ldstr      ""108766ce-df68-46ee-b761-0dcb7ac805f1""
  IL_0014:  newobj     ""System.Guid..ctor(string)""
  IL_0019:  ldc.i4.2
  IL_001a:  newarr     ""byte""
  IL_001f:  dup
  IL_0020:  ldc.i4.0
  IL_0021:  ldc.i4.1
  IL_0022:  stelem.i1
  IL_0023:  dup
  IL_0024:  ldc.i4.1
  IL_0025:  ldc.i4.1
  IL_0026:  stelem.i1
  IL_0027:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_002c:  ldstr      ""d""
  IL_0031:  call       ""ref dynamic Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<dynamic>(string)""
  IL_0036:  ldind.ref
  IL_0037:  ldc.i4.1
  IL_0038:  box        ""int""
  IL_003d:  stind.ref
  IL_003e:  ret
}");
            });
        }

        [Fact]
        public void BindingError_Initializer()
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
                string error;
                context.CompileExpression(
                    "object o = F();",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    out error);
                Assert.Equal("error CS0103: The name 'F' does not exist in the current context", error);
            });
        }

        [Fact]
        public void CannotInferType_NoInitializer()
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
                string error;
                context.CompileExpression(
                    "var y;",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    out error);

                Assert.Equal("error CS0818: Implicitly-typed variables must be initialized", error);
            });
        }

        [Fact]
        public void CannotInferType_Null()
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
                string error;
                context.CompileExpression(
                    "var z = null;",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    out error);
                Assert.Equal("error CS0815: Cannot assign <null> to an implicitly-typed variable", error);
            });
        }

        [Fact]
        public void InferredType_Void()
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
                string error;
                context.CompileExpression(
                    "var w = M();",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    out error);

                Assert.Equal("error CS0815: Cannot assign void to an implicitly-typed variable", error);
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        public void ReferenceInNextDeclaration()
        {
            var source =
@"class C
{
    static void M<T>()
    {
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression(
                    "T x = default(T), y = x;",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    out error,
                    testData);
                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
    @"{
  // Code size      125 (0x7d)
  .maxstack  4
  .locals init (System.Guid V_0,
                T V_1)
  IL_0000:  ldtoken    ""T""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""x""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldtoken    ""T""
  IL_0023:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0028:  ldstr      ""y""
  IL_002d:  ldloca.s   V_0
  IL_002f:  initobj    ""System.Guid""
  IL_0035:  ldloc.0
  IL_0036:  ldnull
  IL_0037:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_003c:  ldstr      ""x""
  IL_0041:  call       ""ref T Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<T>(string)""
  IL_0046:  ldobj      ""T""
  IL_004b:  ldloca.s   V_1
  IL_004d:  initobj    ""T""
  IL_0053:  ldloc.1
  IL_0054:  stobj      ""T""
  IL_0059:  ldstr      ""y""
  IL_005e:  call       ""ref T Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<T>(string)""
  IL_0063:  ldobj      ""T""
  IL_0068:  ldstr      ""x""
  IL_006d:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0072:  unbox.any  ""T""
  IL_0077:  stobj      ""T""
  IL_007c:  ret
}");
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        [WorkItem(1094107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1094107")]
        public void ReferenceInSameDeclaration()
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

                string error;
                var testData = new CompilationTestData();
                context.CompileExpression(
                    "object o = o ?? null;",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    out error,
                    testData);
                // The compiler reports "CS0165: Use of unassigned local variable 'o'"
                // in flow analysis. But since flow analysis is skipped in the EE,
                // compilation succeeds and references to the local in the initializer
                // are treated as default(T). That matches the legacy EE.
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
    @"{
  // Code size       58 (0x3a)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""object""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""o""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldstr      ""o""
  IL_0023:  call       ""ref object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<object>(string)""
  IL_0028:  ldind.ref
  IL_0029:  ldstr      ""o""
  IL_002e:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0033:  dup
  IL_0034:  brtrue.s   IL_0038
  IL_0036:  pop
  IL_0037:  ldnull
  IL_0038:  stind.ref
  IL_0039:  ret
}");
                testData = new CompilationTestData();
                context.CompileExpression(
                    "string s = s.Substring(0);",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    out error,
                    testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
    @"{
  // Code size       64 (0x40)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""string""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""s""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldstr      ""s""
  IL_0023:  call       ""ref string Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<string>(string)""
  IL_0028:  ldind.ref
  IL_0029:  ldstr      ""s""
  IL_002e:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0033:  castclass  ""string""
  IL_0038:  ldc.i4.0
  IL_0039:  callvirt   ""string string.Substring(int)""
  IL_003e:  stind.ref
  IL_003f:  ret
}");
            });
        }

        [Fact]
        public void ReferenceInPreviousDeclaration()
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
                string error;
                context.CompileExpression(
                    "object x = y, y;",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    out error);
                Assert.Equal("error CS0841: Cannot use local variable 'y' before it is declared", error);
            });
        }

        [WorkItem(1094104, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1094104")]
        [Fact(Skip = "1094104")]
        public void Conflict_Parameter()
        {
            var source =
@"class C
{
    static void M(object x)
    {
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                context.CompileExpression(
                    "var x = 4;",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    out error);
                Assert.Equal("...", error);
            });
        }

        [WorkItem(1094104, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1094104")]
        [Fact(Skip = "1094104")]
        public void Conflict_Local()
        {
            var source =
@"class C
{
    static void M()
    {
        object y;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                context.CompileExpression(
                    "object y = 5;",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    out error);
                Assert.Equal("...", error);
            });
        }

        [WorkItem(1094104, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1094104")]
        [Fact(Skip = "1094104")]
        public void Conflict_OtherDeclaration()
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
                string error;
                context.CompileExpression(
                    "object z = 6;",
                    DkmEvaluationFlags.None,
                    ImmutableArray.Create(VariableAlias("z", typeof(int))),
                    out error);
                Assert.Equal("...", error);
            });
        }

        [Fact]
        public void Arguments()
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
                // Local declaration arguments (error only).
                string error;
                context.CompileExpression(
                    "int a[3];",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    out error);
                Assert.Equal("error CS1525: Invalid expression term 'int'", error);
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        public void Keyword()
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
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression(
                    "object @class, @this = @class;",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    out error,
                    testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
    @"{
  // Code size       83 (0x53)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""object""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""class""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldtoken    ""object""
  IL_0023:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0028:  ldstr      ""this""
  IL_002d:  ldloca.s   V_0
  IL_002f:  initobj    ""System.Guid""
  IL_0035:  ldloc.0
  IL_0036:  ldnull
  IL_0037:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_003c:  ldstr      ""this""
  IL_0041:  call       ""ref object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<object>(string)""
  IL_0046:  ldind.ref
  IL_0047:  ldstr      ""class""
  IL_004c:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0051:  stind.ref
  IL_0052:  ret
}");
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        public void Constant()
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
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression(
                    "const int x = 1;",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    out error,
                    testData);
                // Legacy EE reports "Invalid expression term 'const'".
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
    @"{
  // Code size       44 (0x2c)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""int""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""x""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldstr      ""x""
  IL_0023:  call       ""ref int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_0028:  ldind.i4
  IL_0029:  ldc.i4.1
  IL_002a:  stind.i4
  IL_002b:  ret
}");
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        public void Generic()
        {
            var source =
@"class C
{
    static void M<T>(T x)
    {
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression(
                    "T y = x;",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    out error,
                    testData);
                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
    @"{
  // Code size       52 (0x34)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""T""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""y""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldstr      ""y""
  IL_0023:  call       ""ref T Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<T>(string)""
  IL_0028:  ldobj      ""T""
  IL_002d:  ldarg.0
  IL_002e:  stobj      ""T""
  IL_0033:  ret
}");
            });
        }

        /// <summary>
        /// Should not allow names with '$' prefix.
        /// </summary>
        [WorkItem(1106819, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1106819")]
        [Fact]
        public void NoPrefix()
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
                string error;

                // $1
                context.CompileExpression(
                    "var $1 = 1;",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    out error);
                Assert.Equal("error CS1056: Unexpected character '$'", error);

                // $exception
                context.CompileExpression(
                    "var $exception = 2;",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    out error);
                Assert.Equal("error CS1056: Unexpected character '$'", error);

                // $ReturnValue
                context.CompileExpression(
                    "var $ReturnValue = 3;",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    out error);
                Assert.Equal("error CS1056: Unexpected character '$'", error);

                // $x
                context.CompileExpression(
                    "var $x = 4;",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    out error);
                Assert.Equal("error CS1056: Unexpected character '$'", error);
            });
        }

        /// <summary>
        /// Local declarations inside a lambda should
        /// not be considered pseudo-variables.
        /// </summary>
        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        public void Lambda()
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
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression(
                    "System.Action b = () => { object c = null; };",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    out error,
                    testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
    @"{
  // Code size       74 (0x4a)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""System.Action""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""b""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldstr      ""b""
  IL_0023:  call       ""ref System.Action Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<System.Action>(string)""
  IL_0028:  ldind.ref
  IL_0029:  ldsfld     ""System.Action <>x.<>c.<>9__0_0""
  IL_002e:  dup
  IL_002f:  brtrue.s   IL_0048
  IL_0031:  pop
  IL_0032:  ldsfld     ""<>x.<>c <>x.<>c.<>9""
  IL_0037:  ldftn      ""void <>x.<>c.<<>m0>b__0_0()""
  IL_003d:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0042:  dup
  IL_0043:  stsfld     ""System.Action <>x.<>c.<>9__0_0""
  IL_0048:  stind.ref
  IL_0049:  ret
}");
            });
        }

        [WorkItem(1094148, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1094148")]
        [Fact]
        public void OtherStatements()
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
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("while(false) ;", DkmEvaluationFlags.None, NoAliases, out error);
                Assert.Equal("error CS8092: Expression or declaration statement expected.", error);
                testData = new CompilationTestData();
                context.CompileExpression("try { } catch (System.Exception) { }", DkmEvaluationFlags.None, NoAliases, out error);
                Assert.Equal("error CS8092: Expression or declaration statement expected.", error);
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        [WorkItem(3822, "https://github.com/dotnet/roslyn/issues/3822")]
        public void GenericType_Identifier()
        {
            var source = @"
class C
{
    static void M()
    {
    }
}

class Generic<T>
{
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                DkmClrCompilationResultFlags flags;
                CompilationTestData testData;
                CompileDeclaration(context, "Generic<C> g = null;", out flags, out testData);
                Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
    @"{
  // Code size       44 (0x2c)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""Generic<C>""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""g""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldstr      ""g""
  IL_0023:  call       ""ref Generic<C> Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<Generic<C>>(string)""
  IL_0028:  ldind.ref
  IL_0029:  ldnull
  IL_002a:  stind.ref
  IL_002b:  ret
}");
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        [WorkItem(3822, "https://github.com/dotnet/roslyn/issues/3822")]
        public void GenericType_Keyword()
        {
            var source = @"
class C
{
    static void M()
    {
    }
}

class Generic<T>
{
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                DkmClrCompilationResultFlags flags;
                CompilationTestData testData;
                CompileDeclaration(context, "Generic<int> g = null;", out flags, out testData);
                Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
    @"{
  // Code size       44 (0x2c)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""Generic<int>""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""g""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldstr      ""g""
  IL_0023:  call       ""ref Generic<int> Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<Generic<int>>(string)""
  IL_0028:  ldind.ref
  IL_0029:  ldnull
  IL_002a:  stind.ref
  IL_002b:  ret
}");
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        [WorkItem(3822, "https://github.com/dotnet/roslyn/issues/3822")]
        public void PointerType_Identifier()
        {
            var source = @"
class C
{
    static void M()
    {
    }
}

struct S
{
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                DkmClrCompilationResultFlags flags;
                CompilationTestData testData;
                CompileDeclaration(context, "S* s = null;", out flags, out testData);
                Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
    @"{
  // Code size       45 (0x2d)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""S*""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""s""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldstr      ""s""
  IL_0023:  call       ""ref S* Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<S*>(string)""
  IL_0028:  ldind.i
  IL_0029:  ldc.i4.0
  IL_002a:  conv.u
  IL_002b:  stind.i
  IL_002c:  ret
}");
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        [WorkItem(3822, "https://github.com/dotnet/roslyn/issues/3822")]
        public void PointerType_Keyword()
        {
            var source = @"
class C
{
    static void M()
    {
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                DkmClrCompilationResultFlags flags;
                CompilationTestData testData;
                CompileDeclaration(context, "int* p = null;", out flags, out testData);
                Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
    @"{
  // Code size       45 (0x2d)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""int*""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""p""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldstr      ""p""
  IL_0023:  call       ""ref int* Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int*>(string)""
  IL_0028:  ldind.i
  IL_0029:  ldc.i4.0
  IL_002a:  conv.u
  IL_002b:  stind.i
  IL_002c:  ret
}");
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        [WorkItem(3822, "https://github.com/dotnet/roslyn/issues/3822")]
        public void NullableType_Identifier()
        {
            var source = @"
class C
{
    static void M()
    {
    }
}

struct S
{
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                DkmClrCompilationResultFlags flags;
                CompilationTestData testData;
                CompileDeclaration(context, "S? s = null;", out flags, out testData);
                Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
    @"{
  // Code size       60 (0x3c)
  .maxstack  4
  .locals init (System.Guid V_0,
                S? V_1)
  IL_0000:  ldtoken    ""S?""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""s""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldstr      ""s""
  IL_0023:  call       ""ref S? Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<S?>(string)""
  IL_0028:  ldobj      ""S?""
  IL_002d:  ldloca.s   V_1
  IL_002f:  initobj    ""S?""
  IL_0035:  ldloc.1
  IL_0036:  stobj      ""S?""
  IL_003b:  ret
}");
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        [WorkItem(3822, "https://github.com/dotnet/roslyn/issues/3822")]
        public void NullableType_Keyword()
        {
            var source = @"
class C
{
    static void M()
    {
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                DkmClrCompilationResultFlags flags;
                CompilationTestData testData;
                CompileDeclaration(context, "int? n = null;", out flags, out testData);
                Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
    @"{
  // Code size       60 (0x3c)
  .maxstack  4
  .locals init (System.Guid V_0,
                int? V_1)
  IL_0000:  ldtoken    ""int?""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""n""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldstr      ""n""
  IL_0023:  call       ""ref int? Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int?>(string)""
  IL_0028:  ldobj      ""int?""
  IL_002d:  ldloca.s   V_1
  IL_002f:  initobj    ""int?""
  IL_0035:  ldloc.1
  IL_0036:  stobj      ""int?""
  IL_003b:  ret
}");
            });
        }

        private static void CompileDeclaration(EvaluationContext context, string declaration, out DkmClrCompilationResultFlags flags, out CompilationTestData testData)
        {
            string error;
            CompileDeclaration(context, declaration, out flags, out testData, out error);
            Assert.Null(error);
        }

        private static void CompileDeclaration(EvaluationContext context, string declaration, out DkmClrCompilationResultFlags flags, out CompilationTestData testData, out string error)
        {
            testData = new CompilationTestData();

            ResultProperties resultProperties;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var result = context.CompileExpression(
                declaration,
                DkmEvaluationFlags.None,
                NoAliases,
                DebuggerDiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);

            flags = resultProperties.Flags;
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/25702")]
        public void PatternLocals_Assignment_01()
        {
            var source =
@"class C
{
    static object F;
    static void M<T>(object x)
    {
        object y;
        if (x == null)
        {
            object z;
        }
    }

    static object Test(bool x)
    {
        return x;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                CompilationTestData testData;
                string error;
                testData = new CompilationTestData();
                context.CompileAssignment("x", "Test(x is int i)", out error, testData);
                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
    @"{
  // Code size       71 (0x47)
  .maxstack  4
  .locals init (object V_0, //y
                bool V_1,
                object V_2,
                System.Guid V_3,
                int V_4)
  IL_0000:  ldtoken    ""int""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""i""
  IL_000f:  ldloca.s   V_3
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.3
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldarg.0
  IL_001f:  isinst     ""int""
  IL_0024:  brfalse.s  IL_003e
  IL_0026:  ldarg.0
  IL_0027:  unbox.any  ""int""
  IL_002c:  stloc.s    V_4
  IL_002e:  ldstr      ""i""
  IL_0033:  call       ""int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_0038:  ldloc.s    V_4
  IL_003a:  stind.i4
  IL_003b:  ldc.i4.1
  IL_003c:  br.s       IL_003f
  IL_003e:  ldc.i4.0
  IL_003f:  call       ""object C.Test(bool)""
  IL_0044:  starg.s    V_0
  IL_0046:  ret
}");
            });
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/25702")]
        public void PatternLocals_Assignment_02()
        {
            var source =
@"class C
{
    static object F;
    static void M<T>(object x)
    {
        object y;
        if (x == null)
        {
            object z;
        }
    }

    static object Test(bool x)
    {
        return x;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                CompilationTestData testData;
                string error;
                testData = new CompilationTestData();
                context.CompileAssignment("x", "Test(x is string i)", out error, testData);
                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
    @"{
  // Code size       67 (0x43)
  .maxstack  4
  .locals init (object V_0, //y
                bool V_1,
                object V_2,
                System.Guid V_3,
                string V_4)
  IL_0000:  ldtoken    ""string""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""i""
  IL_000f:  ldloca.s   V_3
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.3
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldarg.0
  IL_001f:  isinst     ""string""
  IL_0024:  stloc.s    V_4
  IL_0026:  ldloc.s    V_4
  IL_0028:  brfalse.s  IL_003a
  IL_002a:  ldstr      ""i""
  IL_002f:  call       ""string Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<string>(string)""
  IL_0034:  ldloc.s    V_4
  IL_0036:  stind.ref
  IL_0037:  ldc.i4.1
  IL_0038:  br.s       IL_003b
  IL_003a:  ldc.i4.0
  IL_003b:  call       ""object C.Test(bool)""
  IL_0040:  starg.s    V_0
  IL_0042:  ret
}");
            });
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/25702")]
        public void PatternLocals_Assignment_03()
        {
            var source =
@"class C
{
    static object F;
    static void M<T>(object x)
    {
        object y;
        if (x == null)
        {
            object z;
        }
    }

    static object Test(bool x)
    {
        return x;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                CompilationTestData testData;
                string error;
                testData = new CompilationTestData();
                context.CompileAssignment("x", "Test(x is object i)", out error, testData);
                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
    @"{
  // Code size       57 (0x39)
  .maxstack  4
  .locals init (object V_0, //y
                bool V_1,
                object V_2,
                System.Guid V_3)
  IL_0000:  ldtoken    ""object""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""i""
  IL_000f:  ldloca.s   V_3
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.3
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldarg.0
  IL_001f:  brfalse.s  IL_0030
  IL_0021:  ldstr      ""i""
  IL_0026:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<object>(string)""
  IL_002b:  ldarg.0
  IL_002c:  stind.ref
  IL_002d:  ldc.i4.1
  IL_002e:  br.s       IL_0031
  IL_0030:  ldc.i4.0
  IL_0031:  call       ""object C.Test(bool)""
  IL_0036:  starg.s    V_0
  IL_0038:  ret
}");
            });
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/25702")]
        public void PatternLocals_Assignment_04()
        {
            var source =
@"class C
{
    static object F;
    static void M<T>(int x)
    {
        object y;
        if (x == 1)
        {
            object z;
        }
    }

    static int Test(bool x)
    {
        return 1;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                CompilationTestData testData;
                string error;
                testData = new CompilationTestData();
                context.CompileAssignment("x", "Test(x is int i)", out error, testData);
                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
    @"{
  // Code size       51 (0x33)
  .maxstack  4
  .locals init (object V_0, //y
                bool V_1,
                object V_2,
                System.Guid V_3)
  IL_0000:  ldtoken    ""int""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""i""
  IL_000f:  ldloca.s   V_3
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.3
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldstr      ""i""
  IL_0023:  call       ""int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_0028:  ldarg.0
  IL_0029:  stind.i4
  IL_002a:  ldc.i4.1
  IL_002b:  call       ""int C.Test(bool)""
  IL_0030:  starg.s    V_0
  IL_0032:  ret
}");
            });
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/25702")]
        public void PatternLocals_Assignment_05()
        {
            var source =
@"class C
{
    static object F;
    static void M<T>(int? x)
    {
        object y;
        if (x == 1)
        {
            object z;
        }
    }

    static int? Test(bool x)
    {
        return null;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                CompilationTestData testData;
                string error;
                testData = new CompilationTestData();
                context.CompileAssignment("x", "Test(x is int i)", out error, testData);
                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
    @"{
  // Code size       74 (0x4a)
  .maxstack  4
  .locals init (object V_0, //y
                bool V_1,
                int? V_2,
                int V_3,
                object V_4,
                System.Guid V_5,
                int V_6)
  IL_0000:  ldtoken    ""int""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""i""
  IL_000f:  ldloca.s   V_5
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.s    V_5
  IL_0019:  ldnull
  IL_001a:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001f:  ldarga.s   V_0
  IL_0021:  call       ""bool int?.HasValue.get""
  IL_0026:  brfalse.s  IL_0041
  IL_0028:  ldarga.s   V_0
  IL_002a:  call       ""int int?.GetValueOrDefault()""
  IL_002f:  stloc.s    V_6
  IL_0031:  ldstr      ""i""
  IL_0036:  call       ""int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_003b:  ldloc.s    V_6
  IL_003d:  stind.i4
  IL_003e:  ldc.i4.1
  IL_003f:  br.s       IL_0042
  IL_0041:  ldc.i4.0
  IL_0042:  call       ""int? C.Test(bool)""
  IL_0047:  starg.s    V_0
  IL_0049:  ret
}");
            });
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/25702")]
        public void PatternLocals_LocalDeclarationStatement_01()
        {
            var source =
@"class C
{
    static object F;
    static void M<T>(object x)
    {
        object y;
        if (x == null)
        {
            object z;
        }
    }

    static int Test(bool y)
    {
        return y ? 1 : 0;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                DkmClrCompilationResultFlags flags;
                CompilationTestData testData;
                CompileDeclaration(context, "int z = Test(x is int i);", out flags, out testData);
                Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
    @"{
  // Code size      110 (0x6e)
  .maxstack  4
  .locals init (object V_0, //y
                bool V_1,
                object V_2,
                System.Guid V_3,
                int V_4)
  IL_0000:  ldtoken    ""int""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""z""
  IL_000f:  ldloca.s   V_3
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.3
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldtoken    ""int""
  IL_0023:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0028:  ldstr      ""i""
  IL_002d:  ldloca.s   V_3
  IL_002f:  initobj    ""System.Guid""
  IL_0035:  ldloc.3
  IL_0036:  ldnull
  IL_0037:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_003c:  ldstr      ""z""
  IL_0041:  call       ""int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_0046:  ldarg.0
  IL_0047:  isinst     ""int""
  IL_004c:  brfalse.s  IL_0066
  IL_004e:  ldarg.0
  IL_004f:  unbox.any  ""int""
  IL_0054:  stloc.s    V_4
  IL_0056:  ldstr      ""i""
  IL_005b:  call       ""int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_0060:  ldloc.s    V_4
  IL_0062:  stind.i4
  IL_0063:  ldc.i4.1
  IL_0064:  br.s       IL_0067
  IL_0066:  ldc.i4.0
  IL_0067:  call       ""int C.Test(bool)""
  IL_006c:  stind.i4
  IL_006d:  ret
}");
            });
        }

        [Fact]
        public void DuplicateDeclaration()
        {
            var source =
@"class C
{
    static void M()
    {
        var x = 0;
#line 999
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M", atLineNumber: 999);

                DkmClrCompilationResultFlags flags;
                CompilationTestData testData;
                string error;
                CompileDeclaration(context, "var x = 1;", out flags, out testData, out error);
                Assert.Equal("error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter", error);
            });
        }

        [Fact]
        public void DuplicateDeclarationInOutVar()
        {
            var source =
@"class C
{
    static void F(out double x, out int y) => x = y = 4;

    static void M()
    {
        F(out var x, out var y);
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                DkmClrCompilationResultFlags flags;
                CompilationTestData testData;
                string error;
                CompileDeclaration(context, "F(out var x, out var y)", out flags, out testData, out error);
                Assert.Equal("error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter", error);
            });
        }
    }
}
