// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.Utilities;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
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
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            var result = context.CompileExpression(
                InspectionContextFactory.Empty,
                "int z = 1, F = 2;",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
            testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
@"{
  // Code size       65 (0x41)
  .maxstack  2
  .locals init (object V_0, //y
                bool V_1,
                object V_2)
  IL_0000:  ldtoken    ""int""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""z""
  IL_000f:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string)""
  IL_0014:  ldstr      ""z""
  IL_0019:  call       ""int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_001e:  ldc.i4.1
  IL_001f:  stind.i4
  IL_0020:  ldtoken    ""int""
  IL_0025:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_002a:  ldstr      ""F""
  IL_002f:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string)""
  IL_0034:  ldstr      ""F""
  IL_0039:  call       ""int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_003e:  ldc.i4.2
  IL_003f:  stind.i4
  IL_0040:  ret
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
                methodName: "C.M");
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty.Add("x", typeof(string)).
                    Add("y", typeof(int)).
                    Add("T", typeof(object)).
                    Add("D", "C").
                    Add("F", typeof(int)),
                "(object)x ?? (object)y ?? (object)T ?? (object)F ?? ((C)D).F ?? C.G",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
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
                methodName: "C.M");
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            var result = context.CompileExpression(
                InspectionContextFactory.Empty.Add("c", typeof(char)),
                "*(&c) = 'A'",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
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
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;

            // Expression without ';' as statement.
            var testData = new CompilationTestData();
            var result = context.CompileExpression(InspectionContextFactory.Empty, "3", DkmEvaluationFlags.None, DiagnosticFormatter.Instance, out resultProperties, out error, out missingAssemblyIdentities, EnsureEnglishUICulture.PreferredOrNull, testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Null(error);

            // Expression with ';' as statement.
            testData = new CompilationTestData();
            result = context.CompileExpression(InspectionContextFactory.Empty, "3;", DkmEvaluationFlags.None, DiagnosticFormatter.Instance, out resultProperties, out error, out missingAssemblyIdentities, EnsureEnglishUICulture.PreferredOrNull, testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Null(error);

            // Expression with format specifiers but without ';' as statement.
            testData = new CompilationTestData();
            result = context.CompileExpression(InspectionContextFactory.Empty, "string.Empty, nq", DkmEvaluationFlags.None, DiagnosticFormatter.Instance, out resultProperties, out error, out missingAssemblyIdentities, EnsureEnglishUICulture.PreferredOrNull, testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Null(error);
            AssertEx.SetEqual(result.FormatSpecifiers, new[] { "nq" });

            // Expression with format specifiers with ';' as statement.
            testData = new CompilationTestData();
            result = context.CompileExpression(InspectionContextFactory.Empty, "string.Empty, nq;", DkmEvaluationFlags.None, DiagnosticFormatter.Instance, out resultProperties, out error, out missingAssemblyIdentities, EnsureEnglishUICulture.PreferredOrNull, testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "(1,13): error CS1002: ; expected");
            Assert.Null(result);

            // Assignment without ';' as statement.
            testData = new CompilationTestData();
            result = context.CompileExpression(InspectionContextFactory.Empty, "x = 2", DkmEvaluationFlags.None, DiagnosticFormatter.Instance, out resultProperties, out error, out missingAssemblyIdentities, EnsureEnglishUICulture.PreferredOrNull, testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Null(error);

            // Assignment with ';' as statement.
            testData = new CompilationTestData();
            result = context.CompileExpression(InspectionContextFactory.Empty, "x = 2;", DkmEvaluationFlags.None, DiagnosticFormatter.Instance, out resultProperties, out error, out missingAssemblyIdentities, EnsureEnglishUICulture.PreferredOrNull, testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Null(error);

            // Statement without ';' as statement.
            testData = new CompilationTestData();
            result = context.CompileExpression(InspectionContextFactory.Empty, "int o", DkmEvaluationFlags.None, DiagnosticFormatter.Instance, out resultProperties, out error, out missingAssemblyIdentities, EnsureEnglishUICulture.PreferredOrNull, testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "(1,6): error CS1002: ; expected");

            // Neither statement nor expression as statement.
            testData = new CompilationTestData();
            result = context.CompileExpression(InspectionContextFactory.Empty, "M(;", DkmEvaluationFlags.None, DiagnosticFormatter.Instance, out resultProperties, out error, out missingAssemblyIdentities, EnsureEnglishUICulture.PreferredOrNull, testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "(1,3): error CS1026: ) expected");

            // Statement without ';' as expression.
            testData = new CompilationTestData();
            result = context.CompileExpression(InspectionContextFactory.Empty, "int o", DkmEvaluationFlags.TreatAsExpression, DiagnosticFormatter.Instance, out resultProperties, out error, out missingAssemblyIdentities, EnsureEnglishUICulture.PreferredOrNull, testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "(1,1): error CS1525: Invalid expression term 'int'");

            // Statement with ';' as expression.
            testData = new CompilationTestData();
            result = context.CompileExpression(InspectionContextFactory.Empty, "int o;", DkmEvaluationFlags.TreatAsExpression, DiagnosticFormatter.Instance, out resultProperties, out error, out missingAssemblyIdentities, EnsureEnglishUICulture.PreferredOrNull, testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "(1,1): error CS1525: Invalid expression term 'int'");

            // Neither statement nor expression as expression.
            testData = new CompilationTestData();
            result = context.CompileExpression(InspectionContextFactory.Empty, "M(;", DkmEvaluationFlags.TreatAsExpression, DiagnosticFormatter.Instance, out resultProperties, out error, out missingAssemblyIdentities, EnsureEnglishUICulture.PreferredOrNull, testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "(1,3): error CS1026: ) expected");
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
                methodName: "C.M");
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty.Add("3", typeof(int)),
                "System.ValueType C = (int)$3;",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       52 (0x34)
  .maxstack  2
  IL_0000:  ldtoken    ""System.ValueType""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""C""
  IL_000f:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string)""
  IL_0014:  ldstr      ""C""
  IL_0019:  call       ""System.ValueType Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<System.ValueType>(string)""
  IL_001e:  ldstr      ""3""
  IL_0023:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0028:  unbox.any  ""int""
  IL_002d:  box        ""int""
  IL_0032:  stind.ref
  IL_0033:  ret
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
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "var x = 1;",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldtoken    ""int""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""x""
  IL_000f:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string)""
  IL_0014:  ldstr      ""x""
  IL_0019:  call       ""int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_001e:  ldc.i4.1
  IL_001f:  stind.i4
  IL_0020:  ret
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
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "object o = F();",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
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
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "var y;",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
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
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "var z = null;",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
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
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "var w = M();",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
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
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "T x = default(T), y = x;",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
@"{
  // Code size       95 (0x5f)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldtoken    ""T""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""x""
  IL_000f:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string)""
  IL_0014:  ldstr      ""x""
  IL_0019:  call       ""T Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<T>(string)""
  IL_001e:  ldloca.s   V_0
  IL_0020:  initobj    ""T""
  IL_0026:  ldloc.0
  IL_0027:  stobj      ""T""
  IL_002c:  ldtoken    ""T""
  IL_0031:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0036:  ldstr      ""y""
  IL_003b:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string)""
  IL_0040:  ldstr      ""y""
  IL_0045:  call       ""T Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<T>(string)""
  IL_004a:  ldstr      ""x""
  IL_004f:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0054:  unbox.any  ""T""
  IL_0059:  stobj      ""T""
  IL_005e:  ret
}");
        }

        [WorkItem(1094107)]
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
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "object o = o ?? null;",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            // The compiler reports "CS0165: Use of unassigned local variable 'o'"
            // in flow analysis. But since flow analysis is skipped in the EE,
            // compilation succeeds and references to the local in the initializer
            // are treated as default(T). That matches the legacy EE.
            Assert.Null(error);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       47 (0x2f)
  .maxstack  3
  IL_0000:  ldtoken    ""object""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""o""
  IL_000f:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string)""
  IL_0014:  ldstr      ""o""
  IL_0019:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<object>(string)""
  IL_001e:  ldstr      ""o""
  IL_0023:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0028:  dup
  IL_0029:  brtrue.s   IL_002d
  IL_002b:  pop
  IL_002c:  ldnull
  IL_002d:  stind.ref
  IL_002e:  ret
}");
            testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "string s = s.Substring(0);",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       53 (0x35)
  .maxstack  3
  IL_0000:  ldtoken    ""string""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""s""
  IL_000f:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string)""
  IL_0014:  ldstr      ""s""
  IL_0019:  call       ""string Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<string>(string)""
  IL_001e:  ldstr      ""s""
  IL_0023:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0028:  castclass  ""string""
  IL_002d:  ldc.i4.0
  IL_002e:  callvirt   ""string string.Substring(int)""
  IL_0033:  stind.ref
  IL_0034:  ret
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
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "object x = y, y;",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "error CS0841: Cannot use local variable 'y' before it is declared");
        }

        [WorkItem(1094104)]
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
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "var x = 4;",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "...");
        }

        [WorkItem(1094104)]
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
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "object y = 5;",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "...");
        }

        [WorkItem(1094104)]
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
                methodName: "C.M");
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty.Add("z", typeof(int)),
                "object z = 6;",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
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
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "int a[3];",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "(1,6): error CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.");
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
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "object @class, @this = @class;",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       62 (0x3e)
  .maxstack  2
  IL_0000:  ldtoken    ""object""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""class""
  IL_000f:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string)""
  IL_0014:  ldtoken    ""object""
  IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001e:  ldstr      ""this""
  IL_0023:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string)""
  IL_0028:  ldstr      ""this""
  IL_002d:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<object>(string)""
  IL_0032:  ldstr      ""class""
  IL_0037:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_003c:  stind.ref
  IL_003d:  ret
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
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "const int x = 1;",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            // Legacy EE reports "Invalid expression term 'const'".
            Assert.Null(error);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldtoken    ""int""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""x""
  IL_000f:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string)""
  IL_0014:  ldstr      ""x""
  IL_0019:  call       ""int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_001e:  ldc.i4.1
  IL_001f:  stind.i4
  IL_0020:  ret
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
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "T y = x;",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
@"{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldtoken    ""T""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""y""
  IL_000f:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string)""
  IL_0014:  ldstr      ""y""
  IL_0019:  call       ""T Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<T>(string)""
  IL_001e:  ldarg.0
  IL_001f:  stobj      ""T""
  IL_0024:  ret
}");
        }

        /// <summary>
        /// Should not allow names with '$' prefix.
        /// </summary>
        [WorkItem(1106819)]
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
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;

            // $1
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "var $1 = 1;",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "(1,5): error CS1056: Unexpected character '$'");

            // $exception
            testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "var $exception = 2;",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "(1,5): error CS1056: Unexpected character '$'");

            // $ReturnValue
            testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "var $ReturnValue = 3;",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "(1,5): error CS1056: Unexpected character '$'");

            // $x
            testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "var $x = 4;",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "(1,5): error CS1056: Unexpected character '$'");
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
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "System.Action b = () => { object c = null; };",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       63 (0x3f)
  .maxstack  3
  IL_0000:  ldtoken    ""System.Action""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""b""
  IL_000f:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string)""
  IL_0014:  ldstr      ""b""
  IL_0019:  call       ""System.Action Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<System.Action>(string)""
  IL_001e:  ldsfld     ""System.Action <>x.<>c.<>9__0_0""
  IL_0023:  dup
  IL_0024:  brtrue.s   IL_003d
  IL_0026:  pop
  IL_0027:  ldsfld     ""<>x.<>c <>x.<>c.<>9""
  IL_002c:  ldftn      ""void <>x.<>c.<<>m0>b__0_0()""
  IL_0032:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0037:  dup
  IL_0038:  stsfld     ""System.Action <>x.<>c.<>9__0_0""
  IL_003d:  stind.ref
  IL_003e:  ret
}");
        }

        [WorkItem(1094148)]
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
            ResultProperties resultProperties;
            string error;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var testData = new CompilationTestData();
            context.CompileExpression(InspectionContextFactory.Empty, "while(false) ;", DkmEvaluationFlags.None, DiagnosticFormatter.Instance, out resultProperties, out error, out missingAssemblyIdentities, EnsureEnglishUICulture.PreferredOrNull, testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "error CS8092: Expression or declaration statement expected.");
            testData = new CompilationTestData();
            context.CompileExpression(InspectionContextFactory.Empty, "try { } catch (System.Exception) { }", DkmEvaluationFlags.None, DiagnosticFormatter.Instance, out resultProperties, out error, out missingAssemblyIdentities, EnsureEnglishUICulture.PreferredOrNull, testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Equal(error, "error CS8092: Expression or declaration statement expected.");
        }
    }
}
