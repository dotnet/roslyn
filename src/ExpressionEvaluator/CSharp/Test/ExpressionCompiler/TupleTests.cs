// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.Utilities;
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
            var comp = CreateCompilationWithMscorlib40(source, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, new[] { ValueTupleRef, SystemRuntimeFacadeRef, MscorlibRef }, runtime =>
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
                var method = (MethodSymbol)methodData.Method;
                Assert.True(method.ReturnType.IsTupleType);
                CheckAttribute(result.Assembly, method, AttributeDescription.TupleElementNamesAttribute, expected: true);
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
        public void DuplicateValueTupleBetweenMscorlibAndLibrary()
        {
            var versionTemplate = @"[assembly: System.Reflection.AssemblyVersion(""{0}.0.0.0"")]";

            var corlib_cs = @"
namespace System
{
    public class Object { }
    public struct Int32 { }
    public struct Boolean { }
    public class String { }
    public class ValueType { }
    public struct Void { }
    public class Attribute { }
}

namespace System.Reflection
{
    public class AssemblyVersionAttribute : Attribute
    {
        public AssemblyVersionAttribute(String version) { }
    }
}";
            string valuetuple_cs = @"
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 item1, T2 item2) => (Item1, Item2) = (item1, item2);
    }
}";
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            var corlibWithoutVT = CreateEmptyCompilation(new[] { Parse(String.Format(versionTemplate, "1") + corlib_cs, options: parseOptions) }, assemblyName: "corlib");
            corlibWithoutVT.VerifyDiagnostics();
            var corlibWithoutVTRef = corlibWithoutVT.EmitToImageReference();

            var corlibWithVT = CreateEmptyCompilation(new[] { Parse(String.Format(versionTemplate, "2") + corlib_cs + valuetuple_cs, options: parseOptions) }, assemblyName: "corlib");
            corlibWithVT.VerifyDiagnostics();

            var source =
@"class C
{
    static (int, int) M()
    {
        (int, int) t = (1, 2);
        return t;
    }
}
";
            var app = CreateEmptyCompilation(source + valuetuple_cs, references: new[] { corlibWithoutVTRef }, parseOptions: parseOptions, options: TestOptions.DebugDll);
            app.VerifyDiagnostics();

            // Create EE context with app assembly (including ValueTuple) and a more recent corlib (also including ValueTuple)
            var runtime = CreateRuntimeInstance(new[] { app.ToModuleInstance(), corlibWithVT.ToModuleInstance() });
            var evalContext = CreateMethodContext(runtime, "C.M");
            string error;
            var testData = new CompilationTestData();
            var compileResult = evalContext.CompileExpression("(1, 2)", out error, testData);
            Assert.Null(error);

            using (ModuleMetadata block = ModuleMetadata.CreateFromStream(new MemoryStream(compileResult.Assembly)))
            {
                var reader = block.MetadataReader;

                var appRef = app.Assembly.Identity.Name;
                AssertEx.SetEqual(new[] { "corlib 2.0", appRef + " 0.0" }, reader.DumpAssemblyReferences());

                AssertEx.SetEqual(new[] {
                        "Object, System, AssemblyReference:corlib",
                        "ValueTuple`2, System, AssemblyReference:" + appRef // ValueTuple comes from app, not corlib
                    },
                    reader.DumpTypeReferences());
            }
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
            var comp = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugDll);
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
                var method = (MethodSymbol)methodData.Method;
                Assert.True(method.ReturnType.IsTupleType);
                CheckAttribute(result.Assembly, method, AttributeDescription.TupleElementNamesAttribute, expected: false);
                methodData.VerifyIL(
@" {
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
            var comp = CreateCompilationWithMscorlib40(source, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, new[] { ValueTupleRef, SystemRuntimeFacadeRef, MscorlibRef }, runtime =>
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
                var method = (MethodSymbol)testData.GetExplicitlyDeclaredMethods().Single().Value.Method;
                CheckAttribute(assembly, method, AttributeDescription.TupleElementNamesAttribute, expected: true);
                Assert.True(method.ReturnType.IsTupleType);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "o", expectedILOpt:
string.Format(@"{{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (System.ValueTuple<int, int> V_0) //o
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
            var comp = CreateCompilationWithMscorlib40(source, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, new[] { ValueTupleRef, SystemRuntimeFacadeRef, MscorlibRef }, runtime =>
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
                var method = (MethodSymbol)testData.GetExplicitlyDeclaredMethods().Single().Value.Method;
                CheckAttribute(assembly, method, AttributeDescription.TupleElementNamesAttribute, expected: true);
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13803")]
        public void LongTupleLocalElement_NoNames()
        {
            var source =
@"class C
{
    static void M()
    {
        var x = (1, 2, 3, 4, 5, 6, 7, 8);
    }
}";
            var comp = CreateCompilationWithMscorlib40(source, new[] { SystemRuntimeFacadeRef, ValueTupleRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, new[] { MscorlibRef, SystemCoreRef, SystemRuntimeFacadeRef, ValueTupleRef }, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                string error;
                context.CompileExpression("x.Item4 + x.Item8", out error, testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>> V_0) //x
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Item4""
  IL_0006:  ldloc.0
  IL_0007:  ldfld      ""System.ValueTuple<int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Rest""
  IL_000c:  ldfld      ""int System.ValueTuple<int>.Item1""
  IL_0011:  add
  IL_0012:  ret
}");
            });
        }

        [Fact]
        public void LongTupleLocalElement_Names()
        {
            var source =
@"class C
{
    static void M()
    {
        var x = (1, 2, Three: 3, Four: 4, 5, 6, 7, Eight: 8);
    }
}";
            var comp = CreateCompilationWithMscorlib40(source, new[] { SystemRuntimeFacadeRef, ValueTupleRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, new[] { MscorlibRef, SystemCoreRef, SystemRuntimeFacadeRef, ValueTupleRef }, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                string error;
                context.CompileExpression("x.Item8 + x.Eight", out error, testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>> V_0) //x
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""System.ValueTuple<int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Rest""
  IL_0006:  ldfld      ""int System.ValueTuple<int>.Item1""
  IL_000b:  ldloc.0
  IL_000c:  ldfld      ""System.ValueTuple<int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Rest""
  IL_0011:  ldfld      ""int System.ValueTuple<int>.Item1""
  IL_0016:  add
  IL_0017:  ret
}");
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
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
            var comp = CreateCompilationWithMscorlib40(source, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, new[] { ValueTupleRef, SystemRuntimeFacadeRef, MscorlibRef }, runtime =>
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
                Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult, resultProperties.Flags);
                ReadOnlyCollection<byte> customTypeInfo;
                var customTypeInfoId = result.GetCustomTypeInfo(out customTypeInfo);
                ReadOnlyCollection<byte> dynamicFlags;
                ReadOnlyCollection<string> tupleElementNames;
                CustomTypeInfo.Decode(customTypeInfoId, customTypeInfo, out dynamicFlags, out tupleElementNames);
                Assert.Null(tupleElementNames);
                var methodData = testData.GetMethodData("<>x.<>m0");
                var method = (MethodSymbol)methodData.Method;
                CheckAttribute(result.Assembly, method, AttributeDescription.TupleElementNamesAttribute, expected: false);
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
  IL_0020:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=5 <PrivateImplementationDetails>.845151BC3876B3B783409FD71AF3665D783D8036161B4A2D2ACD27E1A0FCEDF7""
  IL_0025:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_002a:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_002f:  ldstr      ""y""
  IL_0034:  call       ""System.ValueTuple<int, int> Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<System.ValueTuple<int, int>>(string)""
  IL_0039:  ldloc.0
  IL_003a:  stobj      ""System.ValueTuple<int, int>""
  IL_003f:  ret
}");
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/13589")]
        public void AliasElement()
        {
            var source =
@"class C
{
    static (int, int) F;
    static void M()
    {
    }
}";
            var comp = CreateCompilationWithMscorlib40(source, new[] { ValueTupleLegacyRef, SystemRuntimeFacadeRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, new[] { ValueTupleLegacyRef, SystemRuntimeFacadeRef, MscorlibRef }, runtime =>
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
                var assembly = context.CompileGetLocals(
                    locals,
                    argumentsOnly: false,
                    aliases: ImmutableArray.Create(alias),
                    diagnostics: diagnostics,
                    typeName: out typeName,
                    testData: testData);
                diagnostics.Verify();
                diagnostics.Free();
                Assert.Equal(1, locals.Count);
                ReadOnlyCollection<byte> customTypeInfo;
                var customTypeInfoId = locals[0].GetCustomTypeInfo(out customTypeInfo);
                ReadOnlyCollection<byte> dynamicFlags;
                ReadOnlyCollection<string> tupleElementNames;
                CustomTypeInfo.Decode(customTypeInfoId, customTypeInfo, out dynamicFlags, out tupleElementNames);
                Assert.Equal(aliasElementNames, tupleElementNames);
                var method = (MethodSymbol)testData.GetExplicitlyDeclaredMethods().Single().Value.Method;
                CheckAttribute(assembly, method, AttributeDescription.TupleElementNamesAttribute, expected: true);
                var returnType = (TypeSymbol)method.ReturnType;
                Assert.False(returnType.IsTupleType);
                Assert.True(((ArrayTypeSymbol)returnType).ElementType.IsTupleType);
                VerifyLocal(testData, typeName, locals[0], "<>m0", "t", expectedILOpt:
@"{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldstr      ""t""
  IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_000a:  castclass  ""System.ValueTuple<int, System.ValueTuple<int, int>>[]""
  IL_000f:  ret
}");
                locals.Free();
            });
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/13803")]
        public void AliasElement_NoNames()
        {
            var source =
@"class C
{
    static (int, int) F;
    static void M()
    {
    }
}";
            var comp = CreateCompilationWithMscorlib40(source, new[] { SystemRuntimeFacadeRef, ValueTupleLegacyRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, new[] { MscorlibRef, SystemCoreRef, SystemRuntimeFacadeRef, ValueTupleLegacyRef }, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var alias = new Alias(
                    DkmClrAliasKind.Variable,
                    "x",
                    "x",
                    "System.ValueTuple`8[" +
                        "[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]," +
                        "[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]," +
                        "[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]," +
                        "[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]," +
                        "[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]," +
                        "[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]," +
                        "[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]," +
                        "[System.ValueTuple`2[" +
                            "[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]," +
                            "[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], " +
                        "System.ValueTuple, Version=4.0.1.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51]], " +
                    "System.ValueTuple, Version=4.0.1.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51",
                    Guid.Empty,
                    null);
                ResultProperties resultProperties;
                string error;
                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                var testData = new CompilationTestData();
                context.CompileExpression(
                    "x.Item4 + x.Item8",
                    DkmEvaluationFlags.TreatAsExpression,
                    ImmutableArray.Create(alias),
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out error,
                    out missingAssemblyIdentities,
                    null,
                    testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       47 (0x2f)
  .maxstack  2
  IL_0000:  ldstr      ""x""
  IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_000a:  unbox.any  ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int>>""
  IL_000f:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int>>.Item4""
  IL_0014:  ldstr      ""x""
  IL_0019:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_001e:  unbox.any  ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int>>""
  IL_0023:  ldfld      ""System.ValueTuple<int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int>>.Rest""
  IL_0028:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_002d:  add
  IL_002e:  ret
}");
            });
        }
    }
}
