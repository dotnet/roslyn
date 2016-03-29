// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class PseudoVariableTests : ExpressionCompilerTestBase
    {
        [Fact]
        public void UnrecognizedVariable()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                string error;
                Evaluate(runtime, "C.M", "$v", out error);
                Assert.Equal(error, "error CS0103: The name '$v' does not exist in the current context");
            });
        }

        [Fact]
        public void GlobalName()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            ResultProperties resultProperties;
            string error;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "global::$exception",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Equal(error, "error CS0400: The type or namespace name '$exception' could not be found in the global namespace (are you missing an assembly reference?)");
        }

        [Fact]
        public void QualifiedName()
        {
            var source =
@"class C
{
    void M()
    {
    }
}";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                ResultProperties resultProperties;
                string error;
                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                context.CompileExpression(
                    "this.$exception",
                    DkmEvaluationFlags.TreatAsExpression,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out error,
                    out missingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData: null);
                AssertEx.SetEqual(missingAssemblyIdentities, EvaluationContextBase.SystemCoreIdentity);
                Assert.Equal(error, "error CS1061: 'C' does not contain a definition for '$exception' and no extension method '$exception' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)");
            });
        }

        /// <summary>
        /// Generate call to intrinsic method for $exception,
        /// $stowedexception.
        /// </summary>
        [Fact]
        public void Exception()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var aliases = ImmutableArray.Create(
                    ExceptionAlias(typeof(System.IO.IOException)),
                    ExceptionAlias(typeof(InvalidOperationException), stowed: true));
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression(
                    "(System.Exception)$exception ?? $stowedexception",
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    out error,
                    testData);
                Assert.Null(error);
                Assert.Equal(testData.Methods.Count, 1);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  call       ""System.Exception Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException()""
  IL_0005:  castclass  ""System.IO.IOException""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0018
  IL_000d:  pop
  IL_000e:  call       ""System.Exception Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetStowedException()""
  IL_0013:  castclass  ""System.InvalidOperationException""
  IL_0018:  ret
}");
            });
        }

        [Fact]
        public void ReturnValue()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var aliases = ImmutableArray.Create(
                    ReturnValueAlias(type: typeof(object)),
                    ReturnValueAlias(2, typeof(string)));
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression(
                    "$ReturnValue ?? $ReturnValue2",
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    out error,
                    testData);
                Assert.Equal(testData.Methods.Count, 1);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetReturnValue(int)""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_0015
  IL_0009:  pop
  IL_000a:  ldc.i4.2
  IL_000b:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetReturnValue(int)""
  IL_0010:  castclass  ""string""
  IL_0015:  ret
}");
                // Value type $ReturnValue.
                context = CreateMethodContext(
                    runtime,
                    "C.M");
                aliases = ImmutableArray.Create(
                    ReturnValueAlias(type: typeof(int?)));
                testData = new CompilationTestData();
                result = context.CompileExpression(
                    "((int?)$ReturnValue).HasValue",
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    out error,
                    testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (int? V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetReturnValue(int)""
  IL_0006:  unbox.any  ""int?""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  call       ""bool int?.HasValue.get""
  IL_0013:  ret
}");
            });
        }

        /// <summary>
        /// Negative index should be treated as separate tokens.
        /// </summary>
        [Fact]
        public void ReturnValueNegative()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                string error;
                var testData = Evaluate(
                    runtime,
                    "C.M",
                    "(int)$ReturnValue-2",
                    out error,
                    ReturnValueAlias());
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetReturnValue(int)""
  IL_0006:  unbox.any  ""int""
  IL_000b:  ldc.i4.2
  IL_000c:  sub
  IL_000d:  ret
}");
            });
        }

        /// <summary>
        /// Dev12 syntax "[0-9]+#" not supported.
        /// </summary>
        [WorkItem(1071347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1071347")]
        [Fact]
        public void ObjectId_EarlierSyntax()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                context.CompileExpression(
                    "23#",
                    out error);
                Assert.Equal(error, "error CS2043: 'id#' syntax is no longer supported. Use '$id' instead.");
            });
        }

        [Fact]
        public void ObjectId()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(
                runtime,
                "C.M");
                var aliases = ImmutableArray.Create(
                    ObjectIdAlias(23, typeof(string)),
                    ObjectIdAlias(4, typeof(Type)));
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression(
                    "(object)$23 ?? $4.BaseType",
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    out error,
                    testData);
                Assert.Equal(testData.Methods.Count, 1);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       40 (0x28)
  .maxstack  2
  IL_0000:  ldstr      ""$23""
  IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_000a:  castclass  ""string""
  IL_000f:  dup
  IL_0010:  brtrue.s   IL_0027
  IL_0012:  pop
  IL_0013:  ldstr      ""$4""
  IL_0018:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_001d:  castclass  ""System.Type""
  IL_0022:  callvirt   ""System.Type System.Type.BaseType.get""
  IL_0027:  ret
}");
            });
        }

        [WorkItem(1101017, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1101017")]
        [Fact]
        public void NestedGenericValueType()
        {
            var source =
@"class C
{
    internal struct S<T>
    {
        internal T F;
    }
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var aliases = ImmutableArray.Create(
                    VariableAlias("s", "C+S`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"));
                ResultProperties resultProperties;
                string error;
                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                var testData = new CompilationTestData();
                context.CompileExpression(
                    "s.F + 1",
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out error,
                    out missingAssemblyIdentities,
                    null, // preferredUICulture 
                    testData);
                Assert.Empty(missingAssemblyIdentities);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldstr      ""s""
  IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_000a:  unbox.any  ""C.S<int>""
  IL_000f:  ldfld      ""int C.S<int>.F""
  IL_0014:  ldc.i4.1
  IL_0015:  add
  IL_0016:  ret
}");
            });
        }

        [Fact]
        public void ArrayType()
        {
            var source =
@"class C
{
    object F;
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var aliases = ImmutableArray.Create(
                    VariableAlias("a", "C[]"),
                    VariableAlias("b", "System.Int32[,], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
                ResultProperties resultProperties;
                string error;
                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                var testData = new CompilationTestData();
                context.CompileExpression(
                    "a[b[1, 0]].F",
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out error,
                    out missingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData);
                Assert.Empty(missingAssemblyIdentities);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       44 (0x2c)
  .maxstack  4
  IL_0000:  ldstr      ""a""
  IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_000a:  castclass  ""C[]""
  IL_000f:  ldstr      ""b""
  IL_0014:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0019:  castclass  ""int[,]""
  IL_001e:  ldc.i4.1
  IL_001f:  ldc.i4.0
  IL_0020:  call       ""int[*,*].Get""
  IL_0025:  ldelem.ref
  IL_0026:  ldfld      ""object C.F""
  IL_002b:  ret
}");
            });
        }

        /// <summary>
        /// The assembly-qualified type name may be from an
        /// unrecognized assembly. For instance, if the type was
        /// defined in a previous evaluation, say an anonymous
        /// type (e.g.: evaluate "o" after "var o = new { P = 1 };").
        /// </summary>
        [Fact]
        public void UnrecognizedAssembly()
        {
            var source =
@"struct S<T>
{
    internal T F;
}
class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                string error;
                var testData = new CompilationTestData();

                // Unrecognized type.
                var context = CreateMethodContext(
                    runtime,
                    "C.M");
                var aliases = ImmutableArray.Create(
                    VariableAlias("o", "T, 9BAC6622-86EB-4EC5-94A1-9A1E6D0C24AB, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
                context.CompileExpression(
                    "o.P",
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    out error,
                    testData);
                Assert.Equal(error, "error CS0648: '' is a type not supported by the language");

                // Unrecognized array element type.
                aliases = ImmutableArray.Create(
                    VariableAlias("a", "T[], 9BAC6622-86EB-4EC5-94A1-9A1E6D0C24AB, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
                context.CompileExpression(
                    "a[0].P",
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    out error,
                    testData);
                Assert.Equal(error, "error CS0648: '' is a type not supported by the language");

                // Unrecognized generic type argument.
                aliases = ImmutableArray.Create(
                    VariableAlias("s", "S`1[[T, 9BAC6622-86EB-4EC5-94A1-9A1E6D0C24AB, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]"));
                context.CompileExpression(
                    "s.F",
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    out error,
                    testData);
                Assert.Equal(error, "error CS0648: '' is a type not supported by the language");
            });
        }

        [Fact]
        public void Variables()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                CheckVariable(runtime, "$exception", ExceptionAlias(), valid: true);
                CheckVariable(runtime, "$stowedexception", ExceptionAlias(stowed: true), valid: true);
                CheckVariable(runtime, "$Exception", ExceptionAlias(), valid: false);
                CheckVariable(runtime, "$STOWEDEXCEPTION", ExceptionAlias(stowed: true), valid: false);
                CheckVariable(runtime, "$ReturnValue", ReturnValueAlias(), valid: true);
                CheckVariable(runtime, "$RETURNVALUE", ReturnValueAlias(), valid: false);
                CheckVariable(runtime, "$returnvalue", ReturnValueAlias(), valid: true); // Lowercase $ReturnValue supported.
                CheckVariable(runtime, "$ReturnValue0", ReturnValueAlias(0), valid: true);
                CheckVariable(runtime, "$returnvalue21", ReturnValueAlias(21), valid: true);
                CheckVariable(runtime, "$ReturnValue3A", ReturnValueAlias(0x3a), valid: false);
                CheckVariable(runtime, "$33", ObjectIdAlias(33), valid: true);
                CheckVariable(runtime, "$03", ObjectIdAlias(3), valid: false);
                CheckVariable(runtime, "$3A", ObjectIdAlias(0x3a), valid: false);
                CheckVariable(runtime, "$0", ObjectIdAlias(1), valid: false);
                CheckVariable(runtime, "$", ObjectIdAlias(1), valid: false);
                CheckVariable(runtime, "$Unknown", VariableAlias("x"), valid: false);
            });
        }

        private void CheckVariable(RuntimeInstance runtime, string variableName, Alias alias, bool valid)
        {
            string error;
            var testData = Evaluate(runtime, "C.M", variableName, out error, alias);
            if (valid)
            {
                var expectedNames = new[] { "<>x.<>m0()" };
                var actualNames = testData.Methods.Keys;
                AssertEx.SetEqual(expectedNames, actualNames);
            }
            else
            {
                Assert.Equal(error, string.Format("error CS0103: The name '{0}' does not exist in the current context", variableName));
            }
        }

        [Fact]
        public void CheckViability()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                string error;
                var testData = Evaluate(
                    runtime,
                    "C.M",
                    "$ReturnValue1<object>",
                    out error,
                    ReturnValueAlias(1));

                Assert.Equal(error, "error CS0307: The variable '$ReturnValue1' cannot be used with type arguments");

                testData = Evaluate(
                    runtime,
                    "C.M",
                    "$ReturnValue2()",
                    out error,
                    ReturnValueAlias(2));

                Assert.Equal(error, "error CS0149: Method name expected");
            });
        }

        /// <summary>
        /// $exception may be accessed from closure class.
        /// </summary>
        [Fact]
        public void ExceptionInDisplayClass()
        {
            var source =
@"using System;
class C
{
    static object F(System.Func<object> f)
    {
        return f();
    }
    static void M(object o)
    {
    }
}";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                string error;
                var testData = Evaluate(
                    runtime,
                    "C.M",
                    "F(() => o ?? $exception)",
                    out error,
                    ExceptionAlias());
                testData.GetMethodData("<>x.<>c__DisplayClass0_0.<<>m0>b__0()").VerifyIL(
    @"{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object <>x.<>c__DisplayClass0_0.o""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_000f
  IL_0009:  pop
  IL_000a:  call       ""System.Exception Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException()""
  IL_000f:  ret
}");
            });
        }

        [Fact]
        public void AssignException()
        {
            var source =
@"class C
{
    static void M(System.Exception e)
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var aliases = ImmutableArray.Create(
                    ExceptionAlias());
                ResultProperties resultProperties;
                string error;
                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                var testData = new CompilationTestData();
                context.CompileAssignment(
                    target: "e",
                    expr: "$exception.InnerException ?? $exception",
                    aliases: aliases,
                    formatter: DebuggerDiagnosticFormatter.Instance,
                    resultProperties: out resultProperties,
                    error: out error,
                    missingAssemblyIdentities: out missingAssemblyIdentities,
                    preferredUICulture: EnsureEnglishUICulture.PreferredOrNull,
                    testData: testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  call       ""System.Exception Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException()""
  IL_0005:  callvirt   ""System.Exception System.Exception.InnerException.get""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0013
  IL_000d:  pop
  IL_000e:  call       ""System.Exception Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException()""
  IL_0013:  starg.s    V_0
  IL_0015:  ret
}");
            });
        }

        [Fact]
        public void AssignToException()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                string error;
                Evaluate(runtime, "C.M", "$exception = null", out error, ExceptionAlias());
                Assert.Equal(error, "error CS0131: The left-hand side of an assignment must be a variable, property or indexer");
            });
        }

        [WorkItem(1100849, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1100849")]
        [Fact]
        public void PassByRef()
        {
            var source =
@"class C
{
    static T F<T>(ref T t)
    {
        t = default(T);
        return t;
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.F");
                var aliases = ImmutableArray.Create(
                    ExceptionAlias(),
                    ReturnValueAlias(),
                    ObjectIdAlias(1),
                    VariableAlias("x", typeof(int)));
                string error;

            // $exception
            context.CompileExpression(
                "$exception = null",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                out error);
            Assert.Equal(error, "error CS0131: The left-hand side of an assignment must be a variable, property or indexer");
            context.CompileExpression(
                "F(ref $exception)",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                out error);
            Assert.Equal(error, "error CS1510: A ref or out value must be an assignable variable");

            // Object at address
            context.CompileExpression(
                "@0x123 = null",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                out error);
            Assert.Equal(error, "error CS0131: The left-hand side of an assignment must be a variable, property or indexer");
            context.CompileExpression(
                "F(ref @0x123)",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                out error);
            Assert.Equal(error, "error CS1510: A ref or out value must be an assignable variable");

            // $ReturnValue
            context.CompileExpression(
                "$ReturnValue = null",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                out error);
            Assert.Equal(error, "error CS0131: The left-hand side of an assignment must be a variable, property or indexer");
            context.CompileExpression(
                "F(ref $ReturnValue)",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                out error);
            Assert.Equal(error, "error CS1510: A ref or out value must be an assignable variable");

            // Object id
            context.CompileExpression(
                "$1 = null",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                out error);
            Assert.Equal(error, "error CS0131: The left-hand side of an assignment must be a variable, property or indexer");
            context.CompileExpression(
                "F(ref $1)",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                out error);
            Assert.Equal(error, "error CS1510: A ref or out value must be an assignable variable");

                // Declared variable
                var testData = new CompilationTestData();
                context.CompileExpression(
                    "x = 1",
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    out error,
                    testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
@"{
  // Code size       16 (0x10)
  .maxstack  3
  .locals init (T V_0,
                int V_1)
  IL_0000:  ldstr      ""x""
  IL_0005:  call       ""int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_000a:  ldc.i4.1
  IL_000b:  dup
  IL_000c:  stloc.1
  IL_000d:  stind.i4
  IL_000e:  ldloc.1
  IL_000f:  ret
}");
                testData = new CompilationTestData();
                var result = context.CompileExpression(
                    "F(ref x)",
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    out error,
                    testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
@"{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldstr      ""x""
  IL_0005:  call       ""int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_000a:  call       ""int C.F<int>(ref int)""
  IL_000f:  ret
}");
            });
        }

        [Fact]
        public void ValueType()
        {
            var source =
@"struct S
{
    internal object F;
}
class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(
                runtime,
                "C.M");
                var aliases = ImmutableArray.Create(
                    VariableAlias("s", "S"));
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression(
                    "s.F = 1",
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    out error,
                    testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       25 (0x19)
  .maxstack  3
  .locals init (object V_0)
  IL_0000:  ldstr      ""s""
  IL_0005:  call       ""S Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<S>(string)""
  IL_000a:  ldc.i4.1
  IL_000b:  box        ""int""
  IL_0010:  dup
  IL_0011:  stloc.0
  IL_0012:  stfld      ""object S.F""
  IL_0017:  ldloc.0
  IL_0018:  ret
}");
            });
        }

        [Fact]
        public void CompoundAssignment()
        {
            var source =
@"struct S
{
    internal int F;
}
class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var aliases = ImmutableArray.Create(
                    VariableAlias("s", "S"));
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression(
                    "s.F += 2",
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    out error,
                    testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldstr      ""s""
  IL_0005:  call       ""S Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<S>(string)""
  IL_000a:  ldflda     ""int S.F""
  IL_000f:  dup
  IL_0010:  ldind.i4
  IL_0011:  ldc.i4.2
  IL_0012:  add
  IL_0013:  dup
  IL_0014:  stloc.0
  IL_0015:  stind.i4
  IL_0016:  ldloc.0
  IL_0017:  ret
}");
            });
        }

        /// <summary>
        /// Assembly-qualified type names from the debugger refer to runtime assemblies
        /// which may be different versions than the assembly references in metadata.
        /// </summary>
        [WorkItem(1087458, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087458")]
        [Fact]
        public void DifferentAssemblyVersion()
        {
            var sourceA =
@"public class A<T>
{
}";
            var sourceB =
@"class B<T>
{
}
class C
{
    static void M()
    {
        var o = new A<object>();
    }
}";
            const string assemblyNameA = "397300B0-A";
            const string assemblyNameB = "397300B0-B";

            var publicKeyA = ImmutableArray.CreateRange(new byte[] { 0x00, 0x24, 0x00, 0x00, 0x04, 0x80, 0x00, 0x00, 0x94, 0x00, 0x00, 0x00, 0x06, 0x02, 0x00, 0x00, 0x00, 0x24, 0x00, 0x00, 0x52, 0x53, 0x41, 0x31, 0x00, 0x04, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0xED, 0xD3, 0x22, 0xCB, 0x6B, 0xF8, 0xD4, 0xA2, 0xFC, 0xCC, 0x87, 0x37, 0x04, 0x06, 0x04, 0xCE, 0xE7, 0xB2, 0xA6, 0xF8, 0x4A, 0xEE, 0xF3, 0x19, 0xDF, 0x5B, 0x95, 0xE3, 0x7A, 0x6A, 0x28, 0x24, 0xA4, 0x0A, 0x83, 0x83, 0xBD, 0xBA, 0xF2, 0xF2, 0x52, 0x20, 0xE9, 0xAA, 0x3B, 0xD1, 0xDD, 0xE4, 0x9A, 0x9A, 0x9C, 0xC0, 0x30, 0x8F, 0x01, 0x40, 0x06, 0xE0, 0x2B, 0x95, 0x62, 0x89, 0x2A, 0x34, 0x75, 0x22, 0x68, 0x64, 0x6E, 0x7C, 0x2E, 0x83, 0x50, 0x5A, 0xCE, 0x7B, 0x0B, 0xE8, 0xF8, 0x71, 0xE6, 0xF7, 0x73, 0x8E, 0xEB, 0x84, 0xD2, 0x73, 0x5D, 0x9D, 0xBE, 0x5E, 0xF5, 0x90, 0xF9, 0xAB, 0x0A, 0x10, 0x7E, 0x23, 0x48, 0xF4, 0xAD, 0x70, 0x2E, 0xF7, 0xD4, 0x51, 0xD5, 0x8B, 0x3A, 0xF7, 0xCA, 0x90, 0x4C, 0xDC, 0x80, 0x19, 0x26, 0x65, 0xC9, 0x37, 0xBD, 0x52, 0x81, 0xF1, 0x8B, 0xCD });

            var compilationA1 = CreateCompilation(
                new AssemblyIdentity(assemblyNameA, new Version(1, 1, 1, 1), cultureName: "", publicKeyOrToken: publicKeyA, hasPublicKey: true),
                new[] { sourceA },
                references: new[] { MscorlibRef_v20 },
                options: TestOptions.DebugDll.WithDelaySign(true));

            var compilationB1 = CreateCompilation(
                new AssemblyIdentity(assemblyNameB, new Version(1, 2, 2, 2)),
                new[] { sourceB },
                references: new[] { MscorlibRef_v20, compilationA1.EmitToImageReference() },
                options: TestOptions.DebugDll);

            // Use mscorlib v4.0.0.0 and A v2.1.2.1 at runtime.
            var compilationA2 = CreateCompilation(
                new AssemblyIdentity(assemblyNameA, new Version(2, 1, 2, 1), cultureName: "", publicKeyOrToken: publicKeyA, hasPublicKey: true),
                new[] { sourceA },
                references: new[] { MscorlibRef_v20 },
                options: TestOptions.DebugDll.WithDelaySign(true));

            WithRuntimeInstance(compilationB1, new[] { MscorlibRef, compilationA2.EmitToImageReference() }, runtime =>
            {
                // typeof(Exception), typeof(A<B<object>>), typeof(B<A<object>[]>)
                var context = CreateMethodContext(runtime, "C.M");
                var aliases = ImmutableArray.Create(
                    ExceptionAlias("System.Exception, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
                    ObjectIdAlias(1, "A`1[[B`1[[System.Object, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], 397300B0-B, Version=1.2.2.2, Culture=neutral, PublicKeyToken=null]], 397300B0-A, Version=2.1.2.1, Culture=neutral, PublicKeyToken=1f8a32457d187bf3"),
                    ObjectIdAlias(2, "B`1[[A`1[[System.Object, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]][], 397300B0-A, Version=2.1.2.1, Culture=neutral, PublicKeyToken=1f8a32457d187bf3]], 397300B0-B, Version=1.2.2.2, Culture=neutral, PublicKeyToken=null"));
                string error;
                var testData = new CompilationTestData();

                context.CompileExpression(
                    "(object)$exception ?? (object)$1 ?? $2",
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    out error,
                    testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (A<object> V_0) //o
  IL_0000:  call       ""System.Exception Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException()""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_002b
  IL_0008:  pop
  IL_0009:  ldstr      ""$1""
  IL_000e:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0013:  castclass  ""A<B<object>>""
  IL_0018:  dup
  IL_0019:  brtrue.s   IL_002b
  IL_001b:  pop
  IL_001c:  ldstr      ""$2""
  IL_0021:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0026:  castclass  ""B<A<object>[]>""
  IL_002b:  ret
}");
            });
        }

        /// <summary>
        /// The assembly-qualified type may reference an assembly
        /// outside of the current module and its references.
        /// </summary>
        [WorkItem(1092680, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1092680")]
        [Fact]
        public void TypeOutsideModule()
        {
            var sourceA =
@"using System;
public class A<T>
{
    public static void M(Action f)
    {
        object o;
        try
        {
            f();
        }
        catch (Exception)
        {
        }
    }
}";
            var sourceB =
@"using System;
class E : Exception
{
    internal object F;
}
class B
{
    static void Main()
    {
        A<int>.M(() => { throw new E(); });
    }
}";
            var assemblyNameA = "0A93FF0B-31A2-47C8-B24D-16A2D77AB5C5";
            var compilationA = CreateCompilationWithMscorlib(sourceA, options: TestOptions.DebugDll, assemblyName: assemblyNameA);
            var moduleA = compilationA.ToModuleInstance();

            var assemblyNameB = "9BAC6622-86EB-4EC5-94A1-9A1E6D0C24B9";
            var compilationB = CreateCompilationWithMscorlib(sourceB, options: TestOptions.DebugExe, references: new[] { moduleA.GetReference() }, assemblyName: assemblyNameB);
            var moduleB = compilationB.ToModuleInstance();

            var runtime = CreateRuntimeInstance(new[]
            {
                MscorlibRef.ToModuleInstance() ,
                moduleA,
                moduleB,
                ExpressionCompilerTestHelpers.IntrinsicAssemblyReference.ToModuleInstance()
            });

            var context = CreateMethodContext(runtime, "A.M");

            var aliases = ImmutableArray.Create(
                ExceptionAlias("E, 9BAC6622-86EB-4EC5-94A1-9A1E6D0C24B9, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                ObjectIdAlias(1, "A`1[[B, 9BAC6622-86EB-4EC5-94A1-9A1E6D0C24B9, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], 0A93FF0B-31A2-47C8-B24D-16A2D77AB5C5, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));

            string error;
            var testData = new CompilationTestData();
            context.CompileExpression(
                "$exception",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                out error,
                testData);
            Assert.Null(error);
            testData.GetMethodData("<>x<T>.<>m0").VerifyIL(
@"{
// Code size       11 (0xb)
.maxstack  1
.locals init (object V_0) //o
IL_0000:  call       ""System.Exception Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException()""
IL_0005:  castclass  ""E""
IL_000a:  ret
}");
            ResultProperties resultProperties;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            testData = new CompilationTestData();
            context.CompileAssignment(
                "o",
                "$1",
                aliases,
                DebuggerDiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            Assert.Null(error);
            testData.GetMethodData("<>x<T>.<>m0").VerifyIL(
@"{
// Code size       17 (0x11)
.maxstack  1
.locals init (object V_0) //o
IL_0000:  ldstr      ""$1""
IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
IL_000a:  castclass  ""A<B>""
IL_000f:  stloc.0
IL_0010:  ret
}");
        }

        [WorkItem(1140387, "DevDiv")]
        [Fact]
        public void ReturnValueOfPointerType()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var aliases = ImmutableArray.Create(ReturnValueAlias(type: typeof(int*)));

                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression(
                    "$ReturnValue",
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    out error,
                    testData);
                var methodData = testData.GetMethodData("<>x.<>m0");
                Assert.Equal(SpecialType.System_Int32, ((PointerTypeSymbol)methodData.Method.ReturnType).PointedAtType.SpecialType);
                methodData.VerifyIL(
    @"{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetReturnValue(int)""
  IL_0006:  unbox.any  ""System.IntPtr""
  IL_000b:  call       ""void* System.IntPtr.op_Explicit(System.IntPtr)""
  IL_0010:  ret
}");
            });
        }

        [WorkItem(1140387, "DevDiv")]
        [Fact]
        public void UserVariableOfPointerType()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var aliases = ImmutableArray.Create(VariableAlias("p", typeof(char*)));

                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression(
                    "p",
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    out error,
                    testData);
                var methodData = testData.GetMethodData("<>x.<>m0");
                Assert.Equal(SpecialType.System_Char, ((PointerTypeSymbol)methodData.Method.ReturnType).PointedAtType.SpecialType);
                methodData.VerifyIL(
    @"{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldstr      ""p""
  IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_000a:  unbox.any  ""System.IntPtr""
  IL_000f:  call       ""void* System.IntPtr.op_Explicit(System.IntPtr)""
  IL_0014:  ret
}");
            });
        }

        private CompilationTestData Evaluate(
            RuntimeInstance runtime,
            string methodName,
            string expr,
            out string error,
            params Alias[] aliases)
        {
            var context = CreateMethodContext(runtime, methodName);
            var testData = new CompilationTestData();
            var result = context.CompileExpression(
                expr,
                DkmEvaluationFlags.TreatAsExpression,
                ImmutableArray.Create(aliases),
                out error,
                testData);
            return testData;
        }
    }
}
