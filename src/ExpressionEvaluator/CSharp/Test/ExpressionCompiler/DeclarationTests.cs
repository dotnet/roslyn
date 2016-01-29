// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
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
        [Fact]
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
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            DkmClrCompilationResultFlags flags;
            CompilationTestData testData;
            CompileDeclaration(context, "int z = 1, F = 2;", out flags, out testData);
            Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
            testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
@"{
  // Code size       85 (0x55)
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
  IL_0023:  call       ""int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_0028:  ldc.i4.1
  IL_0029:  stind.i4
  IL_002a:  ldtoken    ""int""
  IL_002f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0034:  ldstr      ""F""
  IL_0039:  ldloca.s   V_3
  IL_003b:  initobj    ""System.Guid""
  IL_0041:  ldloc.3
  IL_0042:  ldnull
  IL_0043:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_0048:  ldstr      ""F""
  IL_004d:  call       ""int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_0052:  ldc.i4.2
  IL_0053:  stind.i4
  IL_0054:  ret
}");
        }

        [Fact]
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
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                "C.M");
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
            Assert.Equal(testData.Methods.Count, 1);
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
        }

        [Fact]
        public void Address()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                "C.M");
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
  // Code size       18 (0x12)
  .maxstack  3
  .locals init (char V_0)
  IL_0000:  ldstr      ""c""
  IL_0005:  call       ""char Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<char>(string)""
  IL_000a:  conv.u
  IL_000b:  ldc.i4.s   65
  IL_000d:  dup
  IL_000e:  stloc.0
  IL_000f:  stind.i2
  IL_0010:  ldloc.0
  IL_0011:  ret
}");
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
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
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
            Assert.Equal(error, "error CS1073: Unexpected token ','");
            Assert.Null(result);

            // Assignment without ';' as statement.
            result = context.CompileExpression("x = 2", DkmEvaluationFlags.None, NoAliases, out error);
            Assert.Null(error);

            // Assignment with ';' as statement.
            result = context.CompileExpression("x = 2;", DkmEvaluationFlags.None, NoAliases, out error);
            Assert.Null(error);

            // Statement without ';' as statement.
            result = context.CompileExpression("int o", DkmEvaluationFlags.None, NoAliases, out error);
            Assert.Equal(error, "error CS1525: Invalid expression term 'int'");

            // Neither statement nor expression as statement.
            result = context.CompileExpression("M(;", DkmEvaluationFlags.None, NoAliases, out error);
            Assert.Equal(error, "error CS1026: ) expected");

            // Statement without ';' as expression.
            result = context.CompileExpression("int o", DkmEvaluationFlags.TreatAsExpression, NoAliases, out error);
            Assert.Equal(error, "error CS1525: Invalid expression term 'int'");

            // Statement with ';' as expression.
            result = context.CompileExpression("int o;", DkmEvaluationFlags.TreatAsExpression, NoAliases, out error);
            Assert.Equal(error, "error CS1525: Invalid expression term 'int'");

            // Neither statement nor expression as expression.
            result = context.CompileExpression("M(;", DkmEvaluationFlags.TreatAsExpression, NoAliases, out error);
            Assert.Equal(error, "error CS1026: ) expected");
        }

        [Fact]
        public void BaseType()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                "C.M");
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
  // Code size       62 (0x3e)
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
  IL_0023:  call       ""System.ValueType Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<System.ValueType>(string)""
  IL_0028:  ldstr      ""$3""
  IL_002d:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0032:  unbox.any  ""int""
  IL_0037:  box        ""int""
  IL_003c:  stind.ref
  IL_003d:  ret
}");
        }

        [Fact]
        public void Var()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            DkmClrCompilationResultFlags flags;
            CompilationTestData testData;
            CompileDeclaration(context, "var x = 1;", out flags, out testData);
            Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       43 (0x2b)
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
  IL_0023:  call       ""int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_0028:  ldc.i4.1
  IL_0029:  stind.i4
  IL_002a:  ret
}");
        }

        [WorkItem(1087216, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087216")]
        [Fact]
        public void Dynamic()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            DkmClrCompilationResultFlags flags;
            CompilationTestData testData;
            CompileDeclaration(context, "dynamic d = 1;", out flags, out testData);
            Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       58 (0x3a)
  .maxstack  7
  IL_0000:  ldtoken    ""object""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""d""
  IL_000f:  ldstr      ""826d6ec1-dc4b-46af-be05-cd3f1a1fd4ac""
  IL_0014:  newobj     ""System.Guid..ctor(string)""
  IL_0019:  ldc.i4.1
  IL_001a:  newarr     ""byte""
  IL_001f:  dup
  IL_0020:  ldc.i4.0
  IL_0021:  ldc.i4.1
  IL_0022:  stelem.i1
  IL_0023:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_0028:  ldstr      ""d""
  IL_002d:  call       ""dynamic Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<dynamic>(string)""
  IL_0032:  ldc.i4.1
  IL_0033:  box        ""int""
  IL_0038:  stind.ref
  IL_0039:  ret
}");
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
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            string error;
            context.CompileExpression(
                "object o = F();",
                DkmEvaluationFlags.None,
                NoAliases,
                out error);
            Assert.Equal(error, "error CS0103: The name 'F' does not exist in the current context");
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
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            string error;
            context.CompileExpression(
                "var y;",
                DkmEvaluationFlags.None,
                NoAliases,
                out error);
            Assert.Equal(error, "error CS0818: Implicitly-typed variables must be initialized");
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
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            string error;
            context.CompileExpression(
                "var z = null;",
                DkmEvaluationFlags.None,
                NoAliases,
                out error);
            Assert.Equal(error, "error CS0815: Cannot assign <null> to an implicitly-typed variable");
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
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            string error;
            context.CompileExpression(
                "var w = M();",
                DkmEvaluationFlags.None,
                NoAliases,
                out error);
            Assert.Equal(error, "error CS0815: Cannot assign void to an implicitly-typed variable");
        }

        [Fact]
        public void ReferenceInNextDeclaration()
        {
            var source =
@"class C
{
    static void M<T>()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
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
  // Code size      115 (0x73)
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
  IL_001e:  ldstr      ""x""
  IL_0023:  call       ""T Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<T>(string)""
  IL_0028:  ldloca.s   V_1
  IL_002a:  initobj    ""T""
  IL_0030:  ldloc.1
  IL_0031:  stobj      ""T""
  IL_0036:  ldtoken    ""T""
  IL_003b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0040:  ldstr      ""y""
  IL_0045:  ldloca.s   V_0
  IL_0047:  initobj    ""System.Guid""
  IL_004d:  ldloc.0
  IL_004e:  ldnull
  IL_004f:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_0054:  ldstr      ""y""
  IL_0059:  call       ""T Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<T>(string)""
  IL_005e:  ldstr      ""x""
  IL_0063:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0068:  unbox.any  ""T""
  IL_006d:  stobj      ""T""
  IL_0072:  ret
}");
        }

        [WorkItem(1094107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1094107")]
        [Fact]
        public void ReferenceInSameDeclaration()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
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
  // Code size       57 (0x39)
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
  IL_0023:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<object>(string)""
  IL_0028:  ldstr      ""o""
  IL_002d:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0032:  dup
  IL_0033:  brtrue.s   IL_0037
  IL_0035:  pop
  IL_0036:  ldnull
  IL_0037:  stind.ref
  IL_0038:  ret
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
  // Code size       63 (0x3f)
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
  IL_0023:  call       ""string Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<string>(string)""
  IL_0028:  ldstr      ""s""
  IL_002d:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0032:  castclass  ""string""
  IL_0037:  ldc.i4.0
  IL_0038:  callvirt   ""string string.Substring(int)""
  IL_003d:  stind.ref
  IL_003e:  ret
}");
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
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            string error;
            context.CompileExpression(
                "object x = y, y;",
                DkmEvaluationFlags.None,
                NoAliases,
                out error);
            Assert.Equal(error, "error CS0841: Cannot use local variable 'y' before it is declared");
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
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            string error;
            context.CompileExpression(
                "var x = 4;",
                DkmEvaluationFlags.None,
                NoAliases,
                out error);
            Assert.Equal(error, "...");
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
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            string error;
            context.CompileExpression(
                "object y = 5;",
                DkmEvaluationFlags.None,
                NoAliases,
                out error);
            Assert.Equal(error, "...");
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
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                "C.M");
            string error;
            context.CompileExpression(
                "object z = 6;",
                DkmEvaluationFlags.None,
                ImmutableArray.Create(VariableAlias("z", typeof(int))),
                out error);
            Assert.Equal(error, "...");
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
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            // Local declaration arguments (error only).
            string error;
            context.CompileExpression(
                "int a[3];",
                DkmEvaluationFlags.None,
                NoAliases,
                out error);
            Assert.Equal(error, "error CS1525: Invalid expression term 'int'");
        }

        [Fact]
        public void Keyword()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
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
  // Code size       82 (0x52)
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
  IL_0041:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<object>(string)""
  IL_0046:  ldstr      ""class""
  IL_004b:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0050:  stind.ref
  IL_0051:  ret
}");
        }

        [Fact]
        public void Constant()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
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
  // Code size       43 (0x2b)
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
  IL_0023:  call       ""int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_0028:  ldc.i4.1
  IL_0029:  stind.i4
  IL_002a:  ret
}");
        }

        [Fact]
        public void Generic()
        {
            var source =
@"class C
{
    static void M<T>(T x)
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
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
  // Code size       47 (0x2f)
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
  IL_0023:  call       ""T Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<T>(string)""
  IL_0028:  ldarg.0
  IL_0029:  stobj      ""T""
  IL_002e:  ret
}");
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
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            string error;

            // $1
            context.CompileExpression(
                "var $1 = 1;",
                DkmEvaluationFlags.None,
                NoAliases,
                out error);
            Assert.Equal(error, "error CS1056: Unexpected character '$'");

            // $exception
            context.CompileExpression(
                "var $exception = 2;",
                DkmEvaluationFlags.None,
                NoAliases,
                out error);
            Assert.Equal(error, "error CS1056: Unexpected character '$'");

            // $ReturnValue
            context.CompileExpression(
                "var $ReturnValue = 3;",
                DkmEvaluationFlags.None,
                NoAliases,
                out error);
            Assert.Equal(error, "error CS1056: Unexpected character '$'");

            // $x
            context.CompileExpression(
                "var $x = 4;",
                DkmEvaluationFlags.None,
                NoAliases,
                out error);
            Assert.Equal(error, "error CS1056: Unexpected character '$'");
        }

        /// <summary>
        /// Local declarations inside a lambda should
        /// not be considered pseudo-variables.
        /// </summary>
        [Fact]
        public void Lambda()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
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
  // Code size       73 (0x49)
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
  IL_0023:  call       ""System.Action Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<System.Action>(string)""
  IL_0028:  ldsfld     ""System.Action <>x.<>c.<>9__0_0""
  IL_002d:  dup
  IL_002e:  brtrue.s   IL_0047
  IL_0030:  pop
  IL_0031:  ldsfld     ""<>x.<>c <>x.<>c.<>9""
  IL_0036:  ldftn      ""void <>x.<>c.<<>m0>b__0_0()""
  IL_003c:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0041:  dup
  IL_0042:  stsfld     ""System.Action <>x.<>c.<>9__0_0""
  IL_0047:  stind.ref
  IL_0048:  ret
}");
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
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            string error;
            var testData = new CompilationTestData();
            context.CompileExpression("while(false) ;", DkmEvaluationFlags.None, NoAliases, out error);
            Assert.Equal(error, "error CS8092: Expression or declaration statement expected.");
            testData = new CompilationTestData();
            context.CompileExpression("try { } catch (System.Exception) { }", DkmEvaluationFlags.None, NoAliases, out error);
            Assert.Equal(error, "error CS8092: Expression or declaration statement expected.");
        }

        [WorkItem(3822, "https://github.com/dotnet/roslyn/issues/3822")]
        [Fact]
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
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            var runtime = CreateRuntimeInstance(comp);
            var context = CreateMethodContext(runtime, "C.M");
            DkmClrCompilationResultFlags flags;
            CompilationTestData testData;
            CompileDeclaration(context, "Generic<C> g = null;", out flags, out testData);
            Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       43 (0x2b)
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
  IL_0023:  call       ""Generic<C> Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<Generic<C>>(string)""
  IL_0028:  ldnull
  IL_0029:  stind.ref
  IL_002a:  ret
}");
        }

        [WorkItem(3822, "https://github.com/dotnet/roslyn/issues/3822")]
        [Fact]
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
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            var runtime = CreateRuntimeInstance(comp);
            var context = CreateMethodContext(runtime, "C.M");
            DkmClrCompilationResultFlags flags;
            CompilationTestData testData;
            CompileDeclaration(context, "Generic<int> g = null;", out flags, out testData);
            Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       43 (0x2b)
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
  IL_0023:  call       ""Generic<int> Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<Generic<int>>(string)""
  IL_0028:  ldnull
  IL_0029:  stind.ref
  IL_002a:  ret
}");
        }

        [WorkItem(3822, "https://github.com/dotnet/roslyn/issues/3822")]
        [Fact]
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
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            var runtime = CreateRuntimeInstance(comp);
            var context = CreateMethodContext(runtime, "C.M");
            DkmClrCompilationResultFlags flags;
            CompilationTestData testData;
            CompileDeclaration(context, "S* s = null;", out flags, out testData);
            Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       44 (0x2c)
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
  IL_0023:  call       ""S* Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<S*>(string)""
  IL_0028:  ldc.i4.0
  IL_0029:  conv.u
  IL_002a:  stind.i
  IL_002b:  ret
}");
        }

        [WorkItem(3822, "https://github.com/dotnet/roslyn/issues/3822")]
        [Fact]
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
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            var runtime = CreateRuntimeInstance(comp);
            var context = CreateMethodContext(runtime, "C.M");
            DkmClrCompilationResultFlags flags;
            CompilationTestData testData;
            CompileDeclaration(context, "int* p = null;", out flags, out testData);
            Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       44 (0x2c)
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
  IL_0023:  call       ""int* Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int*>(string)""
  IL_0028:  ldc.i4.0
  IL_0029:  conv.u
  IL_002a:  stind.i
  IL_002b:  ret
}");
        }

        [WorkItem(3822, "https://github.com/dotnet/roslyn/issues/3822")]
        [Fact]
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
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            var runtime = CreateRuntimeInstance(comp);
            var context = CreateMethodContext(runtime, "C.M");
            DkmClrCompilationResultFlags flags;
            CompilationTestData testData;
            CompileDeclaration(context, "S? s = null;", out flags, out testData);
            Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       55 (0x37)
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
  IL_0023:  call       ""S? Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<S?>(string)""
  IL_0028:  ldloca.s   V_1
  IL_002a:  initobj    ""S?""
  IL_0030:  ldloc.1
  IL_0031:  stobj      ""S?""
  IL_0036:  ret
}");
        }

        [WorkItem(3822, "https://github.com/dotnet/roslyn/issues/3822")]
        [Fact]
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
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            var runtime = CreateRuntimeInstance(comp);
            var context = CreateMethodContext(runtime, "C.M");
            DkmClrCompilationResultFlags flags;
            CompilationTestData testData;
            CompileDeclaration(context, "int? n = null;", out flags, out testData);
            Assert.Equal(flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       55 (0x37)
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
  IL_0023:  call       ""int? Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int?>(string)""
  IL_0028:  ldloca.s   V_1
  IL_002a:  initobj    ""int?""
  IL_0030:  ldloc.1
  IL_0031:  stobj      ""int?""
  IL_0036:  ret
}");
        }

        private static void CompileDeclaration(EvaluationContext context, string declaration, out DkmClrCompilationResultFlags flags, out CompilationTestData testData)
        {
            testData = new CompilationTestData();

            ResultProperties resultProperties;
            string error;
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
            Assert.Null(error);
            Assert.Empty(missingAssemblyIdentities);

            flags = resultProperties.Flags;
        }
    }
}
