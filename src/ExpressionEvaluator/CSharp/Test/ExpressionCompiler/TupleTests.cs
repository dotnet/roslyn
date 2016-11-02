// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.Utilities;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class TupleTests : ExpressionCompilerTestBase
    {
        [Fact]
        public void Literal()
        {
            var source =
@"class C
{
    static void M()
    {
        (int, int) o;
    }
}";
            var comp = CreateCompilationWithMscorlib(source, new[] { ValueTupleRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                string error;
                var result = context.CompileExpression("(A: 1, B: 2)", out error, testData);
                Assert.Null(error);
                ReadOnlyCollection<byte> customTypeInfo;
                var customTypeInfoId = result.GetCustomTypeInfo(out customTypeInfo);
                ReadOnlyCollection<byte> dynamicFlags;
                ReadOnlyCollection<string> tupleElementNames;
                CustomTypeInfo.Decode(customTypeInfoId, customTypeInfo, out dynamicFlags, out tupleElementNames);
                Assert.Equal(new[] { "A", "B" }, tupleElementNames);
                var methodData = testData.GetMethodData("<>x.<>m0");
                var method = methodData.Method;
                Assert.True(method.ReturnType.IsTupleType);
                Assert.NotNull(GetTupleElementNamesAttributeIfAny(method));
                methodData.VerifyIL(
@"{
  // Code size        8 (0x8)
  .maxstack  2
  .locals init (System.ValueTuple<int, int> V_0) //o
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0007:  ret
}");
            });
        }

        [Fact]
        public void TupleElementNamesAttribute_NotAvailable()
        {
            var source =
@"namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 _1, T2 _2)
        {
            Item1 = _1;
            Item2 = _2;
        }
    }
}
class C
{
    static void M()
    {
        (int, int) o;
    }
}";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                string error;
                var result = context.CompileExpression("(A: 1, B: 2)", out error, testData);
                Assert.Null(error);
                ReadOnlyCollection<byte> customTypeInfo;
                var customTypeInfoId = result.GetCustomTypeInfo(out customTypeInfo);
                Assert.Null(customTypeInfo);
                var methodData = testData.GetMethodData("<>x.<>m0");
                var method = methodData.Method;
                Assert.True(method.ReturnType.IsTupleType);
                Assert.Null(GetTupleElementNamesAttributeIfAny(method));
                methodData.VerifyIL(
@"{
  // Code size        8 (0x8)
  .maxstack  2
  .locals init (System.ValueTuple<int, int> V_0) //o
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0007:  ret
}");
            });
        }

        [Fact]
        public void Local()
        {
            var source =
@"class C
{
    static void M()
    {
        (int A\u1234, int \u1234B) o = (1, 2);
    }
}";
            var comp = CreateCompilationWithMscorlib(source, new[] { ValueTupleRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(1, locals.Count);
                ReadOnlyCollection<byte> customTypeInfo;
                var customTypeInfoId = locals[0].GetCustomTypeInfo(out customTypeInfo);
                ReadOnlyCollection<byte> dynamicFlags;
                ReadOnlyCollection<string> tupleElementNames;
                CustomTypeInfo.Decode(customTypeInfoId, customTypeInfo, out dynamicFlags, out tupleElementNames);
                Assert.Equal(new[] { "A\u1234", "\u1234B" }, tupleElementNames);
                var method = testData.Methods.Single().Value.Method;
                Assert.NotNull(GetTupleElementNamesAttributeIfAny(method));
                Assert.True(method.ReturnType.IsTupleType);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "o", expectedILOpt:
string.Format(@"{{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init ((int A{0}, int {0}B) V_0) //o
  IL_0000:  ldloc.0
  IL_0001:  ret
}}", '\u1234'));
                locals.Free();
            });
        }

        [Fact]
        public void Constant()
        {
            var source =
@"class A<T>
{
     internal class B<U>
    {
    }
}
class C
{
    static (object, object) F;
    static void M()
    {
        const A<(int, int A)>.B<(object B, object)>[] c = null;
    }
}";
            var comp = CreateCompilationWithMscorlib(source, new[] { ValueTupleRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                Assert.Equal(1, locals.Count);
                ReadOnlyCollection<byte> customTypeInfo;
                var customTypeInfoId = locals[0].GetCustomTypeInfo(out customTypeInfo);
                ReadOnlyCollection<byte> dynamicFlags;
                ReadOnlyCollection<string> tupleElementNames;
                CustomTypeInfo.Decode(customTypeInfoId, customTypeInfo, out dynamicFlags, out tupleElementNames);
                Assert.Equal(new[] { null, "A", "B", null }, tupleElementNames);
                var method = (MethodSymbol)testData.Methods.Single().Value.Method;
                Assert.NotNull(GetTupleElementNamesAttributeIfAny(method));
                var returnType = method.ReturnType;
                Assert.False(returnType.IsTupleType);
                Assert.True(returnType.ContainsTuple());
                VerifyLocal(testData, typeName, locals[0], "<>m0", "c", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}");
                locals.Free();
            });
        }
        
        [Fact]
        public void DeclareLocal()
        {
            var source =
@"class C
{
    static void M()
    {
        var x = (1, 2);
    }
}";
            var comp = CreateCompilationWithMscorlib(source, new[] { ValueTupleRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                string error;
                ResultProperties resultProperties;
                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                var result = context.CompileExpression(
                    "(int A, int B) y = x;",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out error,
                    out missingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData);
                Assert.Null(error);
                Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                ReadOnlyCollection<byte> customTypeInfo;
                var customTypeInfoId = result.GetCustomTypeInfo(out customTypeInfo);
                ReadOnlyCollection<byte> dynamicFlags;
                ReadOnlyCollection<string> tupleElementNames;
                CustomTypeInfo.Decode(customTypeInfoId, customTypeInfo, out dynamicFlags, out tupleElementNames);
                Assert.Null(tupleElementNames);
                var methodData = testData.GetMethodData("<>x.<>m0");
                var method = methodData.Method;
                Assert.Null(GetTupleElementNamesAttributeIfAny(method));
                methodData.VerifyIL(
@"{
  // Code size       64 (0x40)
  .maxstack  6
  .locals init (System.ValueTuple<int, int> V_0) //x
  IL_0000:  ldtoken    ""System.ValueTuple<int, int>""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""y""
  IL_000f:  ldstr      ""108766ce-df68-46ee-b761-0dcb7ac805f1""
  IL_0014:  newobj     ""System.Guid..ctor(string)""
  IL_0019:  ldc.i4.5
  IL_001a:  newarr     ""byte""
  IL_001f:  dup
  IL_0020:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=5 <PrivateImplementationDetails>.362A905A18EA2A18A9EB2574618C490DE8A1F5C3""
  IL_0025:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_002a:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_002f:  ldstr      ""y""
  IL_0034:  call       ""(int A, int B) Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<(int A, int B)>(string)""
  IL_0039:  ldloc.0
  IL_003a:  stobj      ""System.ValueTuple<int, int>""
  IL_003f:  ret
}");
            });
        }

        [WorkItem(13589, "https://github.com/dotnet/roslyn/issues/13589")]
        [Fact]
        public void Alias()
        {
            var source =
@"class C
{
    static (int, int) F;
    static void M()
    {
    }
}";
            var comp = CreateCompilationWithMscorlib(source, new[] { ValueTupleRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(
                    runtime,
                    "C.M");
                // (int A, (int, int D) B)[] t;
                var aliasElementNames = new ReadOnlyCollection<string>(new[] { "A", "B", null, "D" });
                var alias = new Alias(
                    DkmClrAliasKind.Variable,
                    "t",
                    "t",
                    "System.ValueTuple`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.ValueTuple`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], System.ValueTuple, Version=4.0.1.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51]][], System.ValueTuple, Version=4.0.1.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51",
                    CustomTypeInfo.PayloadTypeId,
                    CustomTypeInfo.Encode(null, aliasElementNames));
                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                var diagnostics = DiagnosticBag.GetInstance();
                var testData = new CompilationTestData();
                context.CompileGetLocals(
                    locals,
                    argumentsOnly: false,
                    aliases: ImmutableArray.Create(alias),
                    diagnostics: diagnostics,
                    typeName: out typeName,
                    testData: testData);
                diagnostics.Verify();
                diagnostics.Free();
                Assert.Equal(locals.Count, 1);
                ReadOnlyCollection<byte> customTypeInfo;
                var customTypeInfoId = locals[0].GetCustomTypeInfo(out customTypeInfo);
                ReadOnlyCollection<byte> dynamicFlags;
                ReadOnlyCollection<string> tupleElementNames;
                CustomTypeInfo.Decode(customTypeInfoId, customTypeInfo, out dynamicFlags, out tupleElementNames);
                Assert.Equal(aliasElementNames, tupleElementNames);
                var method = testData.Methods.Single().Value.Method;
                Assert.NotNull(GetTupleElementNamesAttributeIfAny(method));
                var returnType = (TypeSymbol)method.ReturnType;
                Assert.False(returnType.IsTupleType);
                Assert.True(((ArrayTypeSymbol)returnType).ElementType.IsTupleType);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "t", expectedILOpt:
@"{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldstr      ""t""
  IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_000a:  castclass  ""(int A, (int, int D) B)[]""
  IL_000f:  ret
}");
                locals.Free();
            });
        }
    }
}
