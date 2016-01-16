// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class DynamicTests : ExpressionCompilerTestBase
    {
        [Fact]
        public void Local_Simple()
        {
            var source =
@"class C
{
    static void M()
    {
        dynamic d = 1;
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(comp);
            var context = CreateMethodContext(runtime, "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(1, locals.Count);
            var method = testData.Methods.Single().Value.Method;
            AssertHasDynamicAttribute(method);
            Assert.Equal(TypeKind.Dynamic, method.ReturnType.TypeKind);
            VerifyCustomTypeInfo(locals[0], "d", 0x01);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (dynamic V_0) //d
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
            locals.Free();
        }

        [Fact]
        public void Local_Array()
        {
            var source =
@"class C
{
    static void M()
    {
        dynamic[] d = new dynamic[1];
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(comp);
            var context = CreateMethodContext(runtime, "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(1, locals.Count);
            var method = testData.Methods.Single().Value.Method;
            AssertHasDynamicAttribute(method);
            Assert.Equal(TypeKind.Dynamic, ((ArrayTypeSymbol)method.ReturnType).ElementType.TypeKind);
            VerifyCustomTypeInfo(locals[0], "d", 0x02);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (dynamic[] V_0) //d
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
            locals.Free();
        }

        [Fact]
        public void Local_Generic()
        {
            var source =
@"class C
{
    static void M()
    {
        System.Collections.Generic.List<dynamic> d = null;
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(comp);
            var context = CreateMethodContext(runtime, "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(1, locals.Count);
            var method = testData.Methods.Single().Value.Method;
            AssertHasDynamicAttribute(method);
            Assert.Equal(TypeKind.Dynamic, ((NamedTypeSymbol)method.ReturnType).TypeArguments.Single().TypeKind);
            VerifyCustomTypeInfo(locals[0], "d", 0x02);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (System.Collections.Generic.List<dynamic> V_0) //d
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
            locals.Free();
        }

        [Fact]
        public void LocalConstant_Simple()
        {
            var source =
@"class C
{   
    static void M()
    {
        const dynamic d = null;
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugDll);
            byte[] exeBytes;
            byte[] pdbBytes;
            ImmutableArray<MetadataReference> references;
            comp.EmitAndGetReferences(out exeBytes, out pdbBytes, out references);

            var runtime = CreateRuntimeInstance(ExpressionCompilerUtilities.GenerateUniqueName(), references, exeBytes, SymReaderFactory.CreateReader(pdbBytes, exeBytes));

            var context = CreateMethodContext(runtime, "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(1, locals.Count);
            var method = testData.Methods.Single().Value.Method;
            AssertHasDynamicAttribute(method);
            Assert.Equal(TypeKind.Dynamic, method.ReturnType.TypeKind);
            VerifyCustomTypeInfo(locals[0], "d", 0x01);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}");
            locals.Free();
        }

        [Fact]
        public void LocalConstant_Array()
        {
            var source =
@"class C
{
    static void M()
    {
        const dynamic[] d = null;
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugDll);
            byte[] exeBytes;
            byte[] pdbBytes;
            ImmutableArray<MetadataReference> references;
            comp.EmitAndGetReferences(out exeBytes, out pdbBytes, out references);

            var runtime = CreateRuntimeInstance(ExpressionCompilerUtilities.GenerateUniqueName(), references, exeBytes, SymReaderFactory.CreateReader(pdbBytes, exeBytes));

            var context = CreateMethodContext(runtime, "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(1, locals.Count);
            var method = testData.Methods.Single().Value.Method;
            AssertHasDynamicAttribute(method);
            Assert.Equal(TypeKind.Dynamic, ((ArrayTypeSymbol)method.ReturnType).ElementType.TypeKind);
            VerifyCustomTypeInfo(locals[0], "d", 0x02);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt: @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}");
            locals.Free();
        }

        [Fact]
        public void LocalConstant_Generic()
        {
            var source =
@"class C
{
    static void M()
    {
        const Generic<dynamic> d = null;
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}

class Generic<T>
{
}
";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugDll);
            byte[] exeBytes;
            byte[] pdbBytes;
            ImmutableArray<MetadataReference> references;
            comp.EmitAndGetReferences(out exeBytes, out pdbBytes, out references);

            var runtime = CreateRuntimeInstance(ExpressionCompilerUtilities.GenerateUniqueName(), references, exeBytes, SymReaderFactory.CreateReader(pdbBytes, exeBytes));

            var context = CreateMethodContext(runtime, "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(1, locals.Count);
            var method = testData.Methods.Single().Value.Method;
            AssertHasDynamicAttribute(method);
            Assert.Equal(TypeKind.Dynamic, ((NamedTypeSymbol)method.ReturnType).TypeArguments.Single().TypeKind);
            VerifyCustomTypeInfo(locals[0], "d", 0x02);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedFlags: DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt: @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}");
            locals.Free();
        }

        [WorkItem(4106, "https://github.com/dotnet/roslyn/issues/4106")]
        [Fact]
        public void LocalDuplicateConstantAndNonConstantDynamic()
        {
            var source =
@"class C
{
    static void M()
    {
        {
#line 799
            dynamic a = null;
            const dynamic b = null;
        }
        {
            const dynamic[] a = null;
#line 899
            dynamic[] b = null;
        }
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);

            var context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 799);
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(2, locals.Count);
            VerifyCustomTypeInfo(locals[0], "a", null); // Dynamic info ignored because ambiguous.
            VerifyCustomTypeInfo(locals[1], "b", 0x01);
            locals.Free();

            context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 899);
            testData = new CompilationTestData();
            locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(2, locals.Count);
            VerifyCustomTypeInfo(locals[0], "b", 0x02);
            VerifyCustomTypeInfo(locals[1], "a", null); // Dynamic info ignored because ambiguous.
            locals.Free();
        }

        [WorkItem(4106, "https://github.com/dotnet/roslyn/issues/4106")]
        [Fact]
        public void LocalDuplicateConstantAndNonConstantNonDynamic()
        {
            var source =
@"class C
{
    static void M()
    {
        {
#line 799
            object a = null;
            const dynamic b = null;
        }
        {
            const dynamic[] a = null;
#line 899
            object[] b = null;
        }
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);

            var context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 799);
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(2, locals.Count);
            VerifyCustomTypeInfo(locals[0], "a", null);
            VerifyCustomTypeInfo(locals[1], "b", 0x01);
            locals.Free();

            context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 899);
            testData = new CompilationTestData();
            locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(2, locals.Count);
            VerifyCustomTypeInfo(locals[0], "b", null);
            VerifyCustomTypeInfo(locals[1], "a", null); // Dynamic info ignored because ambiguous.
            locals.Free();
        }

        [WorkItem(4106, "https://github.com/dotnet/roslyn/issues/4106")]
        [Fact]
        public void LocalDuplicateConstantAndConstantDynamic()
        {
            var source =
@"class C
{
    static void M()
    {
        {
            const dynamic a = null;
            const dynamic b = null;
#line 799
            object e = null;
        }
        {
            const dynamic[] a = null;
            const dynamic[] c = null;
#line 899
            object[] e = null;
        }
        {
#line 999
            object e = null;
            const dynamic a = null;
            const dynamic c = null;
        }
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);

            var context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 799);
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(3, locals.Count);
            VerifyCustomTypeInfo(locals[0], "e", null);
            VerifyCustomTypeInfo(locals[1], "a", null); // Dynamic info ignored because ambiguous.
            VerifyCustomTypeInfo(locals[2], "b", 0x01);
            locals.Free();

            context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 899);
            testData = new CompilationTestData();
            locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(3, locals.Count);
            VerifyCustomTypeInfo(locals[0], "e", null);
            VerifyCustomTypeInfo(locals[1], "a", null); // Dynamic info ignored because ambiguous.
            VerifyCustomTypeInfo(locals[2], "c", null); // Dynamic info ignored because ambiguous.
            locals.Free();

            context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 999);
            testData = new CompilationTestData();
            locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(3, locals.Count);
            VerifyCustomTypeInfo(locals[0], "e", null);
            VerifyCustomTypeInfo(locals[1], "a", null); // Dynamic info ignored because ambiguous.
            VerifyCustomTypeInfo(locals[2], "c", null); // Dynamic info ignored because ambiguous.
            locals.Free();
        }

        [WorkItem(4106, "https://github.com/dotnet/roslyn/issues/4106")]
        [Fact]
        public void LocalDuplicateConstantAndConstantNonDynamic()
        {
            var source =
@"class C
{
    static void M()
    {
        {
            const dynamic a = null;
            const object c = null;
#line 799
            object e = null;
        }
        {
            const dynamic[] b = null;
#line 899
            object[] e = null;
        }
        {
            const object[] a = null;
#line 999
            object e = null;
            const dynamic[] c = null;
        }
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);

            var context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 799);
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(3, locals.Count);
            VerifyCustomTypeInfo(locals[0], "e", null);
            VerifyCustomTypeInfo(locals[1], "a", null); // Dynamic info ignored because ambiguous.
            VerifyCustomTypeInfo(locals[2], "c", null);
            locals.Free();

            context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 899);
            testData = new CompilationTestData();
            locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(2, locals.Count);
            VerifyCustomTypeInfo(locals[0], "e", null);
            VerifyCustomTypeInfo(locals[1], "b", 0x02);
            locals.Free();

            context = CreateMethodContext(runtime, methodName: "C.M", atLineNumber: 999);
            testData = new CompilationTestData();
            locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(3, locals.Count);
            VerifyCustomTypeInfo(locals[0], "e", null);
            VerifyCustomTypeInfo(locals[1], "a", null);
            VerifyCustomTypeInfo(locals[2], "c", null); // Dynamic info ignored because ambiguous.
            locals.Free();
        }

        [Fact]
        public void LocalsWithLongAndShortNames()
        {
            var source =
@"class C
{
	static void M()
	{
        const dynamic a123456789012345678901234567890123456789012345678901234567890123 = null; // 64 chars
        const dynamic b = null;
        dynamic c123456789012345678901234567890123456789012345678901234567890123 = null; // 64 chars
        dynamic d = null;
	}
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(runtime, methodName: "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(4, locals.Count);
            VerifyCustomTypeInfo(locals[0], "c123456789012345678901234567890123456789012345678901234567890123", null); // dynamic info dropped
            VerifyCustomTypeInfo(locals[1], "d", 0x01);
            VerifyCustomTypeInfo(locals[2], "a123456789012345678901234567890123456789012345678901234567890123", null); // dynamic info dropped
            VerifyCustomTypeInfo(locals[3], "b", 0x01);
            locals.Free();
        }

        [Fact]
        public void Parameter_Simple()
        {
            var source =
@"class C
{
    static void M(dynamic d)
    {
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, CSharpRef }, TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(comp);
            var context = CreateMethodContext(runtime, "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(1, locals.Count);
            var method = testData.Methods.Single().Value.Method;
            AssertHasDynamicAttribute(method);
            Assert.Equal(TypeKind.Dynamic, method.ReturnType.TypeKind);
            VerifyCustomTypeInfo(locals[0], "d", 0x01);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
            locals.Free();
        }

        [Fact]
        public void Parameter_Array()
        {
            var source =
@"class C
{
    static void M(dynamic[] d)
    {
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, CSharpRef }, TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(comp);
            var context = CreateMethodContext(runtime, "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(1, locals.Count);
            var method = testData.Methods.Single().Value.Method;
            AssertHasDynamicAttribute(method);
            Assert.Equal(TypeKind.Dynamic, ((ArrayTypeSymbol)method.ReturnType).ElementType.TypeKind);
            VerifyCustomTypeInfo(locals[0], "d", 0x02);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
            locals.Free();
        }

        [Fact]
        public void Parameter_Generic()
        {
            var source =
@"class C
{
    static void M(System.Collections.Generic.List<dynamic> d)
    {
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, CSharpRef }, TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(comp);
            var context = CreateMethodContext(runtime, "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(1, locals.Count);
            var method = testData.Methods.Single().Value.Method;
            AssertHasDynamicAttribute(method);
            Assert.Equal(TypeKind.Dynamic, ((NamedTypeSymbol)method.ReturnType).TypeArguments.Single().TypeKind);
            VerifyCustomTypeInfo(locals[0], "d", 0x02);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
            locals.Free();
        }

        [WorkItem(1087216, "DevDiv")]
        [Fact]
        public void ComplexDynamicType()
        {
            var source =
@"class C
{
    static void M(Outer<dynamic[], object[]>.Inner<Outer<object, dynamic>[], dynamic> d)
    {
    }

    static dynamic ForceDynamicAttribute() 
    {
        return null;
    }
}

public class Outer<T, U>
{
    public class Inner<V, W>
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, CSharpRef }, TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(comp);
            var context = CreateMethodContext(runtime, "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(1, locals.Count);
            var method = testData.Methods.Single().Value.Method;
            AssertHasDynamicAttribute(method);
            VerifyCustomTypeInfo(locals[0], "d", 0x04, 0x03);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");

            string error;
            var result = context.CompileExpression("d", out error);
            Assert.Null(error);
            VerifyCustomTypeInfo(result, 0x04, 0x03);

            // Note that the method produced by CompileAssignment returns void
            // so there is never custom type info.
            result = context.CompileAssignment("d", "d", out error);
            Assert.Null(error);
            VerifyCustomTypeInfo(result, null);

            ResultProperties resultProperties;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            testData = new CompilationTestData();
            result = context.CompileExpression(
                "var dd = d;",
                DkmEvaluationFlags.None,
                NoAliases,
                DebuggerDiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Null(error);
            VerifyCustomTypeInfo(result, null);
            Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       57 (0x39)
  .maxstack  7
  IL_0000:  ldtoken    ""Outer<dynamic[], object[]>.Inner<Outer<object, dynamic>[], dynamic>""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""dd""
  IL_000f:  ldstr      ""826d6ec1-dc4b-46af-be05-cd3f1a1fd4ac""
  IL_0014:  newobj     ""System.Guid..ctor(string)""
  IL_0019:  ldc.i4.2
  IL_001a:  newarr     ""byte""
  IL_001f:  dup
  IL_0020:  ldc.i4.0
  IL_0021:  ldc.i4.4
  IL_0022:  stelem.i1
  IL_0023:  dup
  IL_0024:  ldc.i4.1
  IL_0025:  ldc.i4.3
  IL_0026:  stelem.i1
  IL_0027:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_002c:  ldstr      ""dd""
  IL_0031:  call       ""Outer<dynamic[], object[]>.Inner<Outer<object, dynamic>[], dynamic> Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<Outer<dynamic[], object[]>.Inner<Outer<object, dynamic>[], dynamic>>(string)""
  IL_0036:  ldarg.0
  IL_0037:  stind.ref
  IL_0038:  ret
}");
            locals.Free();
        }

        [Fact]
        public void DynamicAliases()
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
            var aliases = ImmutableArray.Create(
                Alias(
                    DkmClrAliasKind.Variable,
                    "d1",
                    "d1",
                    typeof(object).AssemblyQualifiedName,
                    MakeCustomTypeInfo(true)),
                Alias(
                    DkmClrAliasKind.Variable,
                    "d2",
                    "d2",
                    typeof(Dictionary<Dictionary<dynamic, Dictionary<object[], dynamic[]>>, object>).AssemblyQualifiedName,
                    MakeCustomTypeInfo(false, false, true, false, false, false, false, true, false)));
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var diagnostics = DiagnosticBag.GetInstance();

            var testData = new CompilationTestData();
            context.CompileGetLocals(
                locals,
                argumentsOnly: false,
                aliases: aliases,
                diagnostics: diagnostics,
                typeName: out typeName,
                testData: testData);
            diagnostics.Free();
            Assert.Equal(locals.Count, 2);

            VerifyCustomTypeInfo(locals[0], "d1", 0x01);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d1", expectedILOpt:
@"{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldstr      ""d1""
  IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_000a:  ret
}");

            VerifyCustomTypeInfo(locals[1], "d2", 0x84, 0x00); // Note: read flags right-to-left in each byte: 0010 0001 0(000 0000)
            VerifyLocal(testData, typeName, locals[1], "<>m1", "d2", expectedILOpt:
@"{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldstr      ""d2""
  IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_000a:  castclass  ""System.Collections.Generic.Dictionary<System.Collections.Generic.Dictionary<dynamic, System.Collections.Generic.Dictionary<object[], dynamic[]>>, object>""
  IL_000f:  ret
}");
            locals.Free();
        }

        private static CustomTypeInfo MakeCustomTypeInfo(params bool[] flags)
        {
            Assert.NotNull(flags);
            var dynamicFlagsInfo = DynamicFlagsCustomTypeInfo.Create(ImmutableArray.CreateRange(flags));
            return new CustomTypeInfo(DynamicFlagsCustomTypeInfo.PayloadTypeId, dynamicFlagsInfo.GetCustomTypeInfoPayload());
        }

        [Fact]
        public void DynamicAttribute_NotAvailable()
        {
            var source =
@"class C
{
    static void M()
    {
        dynamic d = 1;
    }
}";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(comp);
            var context = CreateMethodContext(runtime, "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(1, locals.Count);
            var method = testData.Methods.Single().Value.Method;
            AssertHasNoDynamicAttribute(method);
            locals.Free();
        }

        private static void AssertHasDynamicAttribute(IMethodSymbol method)
        {
            Assert.Contains(
                "System.Runtime.CompilerServices.DynamicAttribute",
                method.GetSynthesizedAttributes(forReturnType: true).Select(a => a.AttributeClass.ToTestDisplayString()));
        }

        private static void AssertHasNoDynamicAttribute(IMethodSymbol method)
        {
            Assert.DoesNotContain(
                "System.Runtime.CompilerServices.DynamicAttribute",
                method.GetSynthesizedAttributes(forReturnType: true).Select(a => a.AttributeClass.ToTestDisplayString()));
        }

        [Fact]
        public void DynamicCall()
        {
            var source = @"
class C
{
    void M()
    {
        dynamic d = this;
        d.M();
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, CSharpRef }, TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(comp);
            var context = CreateMethodContext(runtime, "C.M");
            var testData = new CompilationTestData();
            string error;
            var result = context.CompileExpression("d.M()", out error, testData);
            Assert.Null(error);
            VerifyCustomTypeInfo(result, 0x01);
            var methodData = testData.GetMethodData("<>x.<>m0");
            Assert.Equal(TypeKind.Dynamic, methodData.Method.ReturnType.TypeKind);
            methodData.VerifyIL(@"
{
  // Code size       77 (0x4d)
  .maxstack  9
  .locals init (dynamic V_0) //d
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0037
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""M""
  IL_000d:  ldnull
  IL_000e:  ldtoken    ""C""
  IL_0013:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0018:  ldc.i4.1
  IL_0019:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001e:  dup
  IL_001f:  ldc.i4.0
  IL_0020:  ldc.i4.0
  IL_0021:  ldnull
  IL_0022:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0027:  stelem.ref
  IL_0028:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_002d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0032:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0037:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_003c:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0041:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0046:  ldloc.0
  IL_0047:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_004c:  ret
}
");
        }

        [WorkItem(1160855, "DevDiv")]
        [Fact]
        public void AwaitDynamic()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class C
{
    dynamic d;

    void M(int p)
    {
        d.Test(); // Force reference to runtime binder.
    }

    static void G(Func<Task<object>> f)
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef, CSharpRef }, TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(comp);
            var context = CreateMethodContext(runtime, "C.M");
            var testData = new CompilationTestData();
            string error;
            var result = context.CompileExpression("G(async () => await d())", out error, testData);
            Assert.Null(error);
            VerifyCustomTypeInfo(result, null);
            var methodData = testData.GetMethodData("<>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()");
            methodData.VerifyIL(@"
{
  // Code size      539 (0x21b)
  .maxstack  10
  .locals init (int V_0,
                object V_1,
                object V_2,
                System.Runtime.CompilerServices.ICriticalNotifyCompletion V_3,
                System.Runtime.CompilerServices.INotifyCompletion V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_0185
    IL_000d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__0""
    IL_0012:  brtrue.s   IL_0044
    IL_0014:  ldc.i4.0
    IL_0015:  ldstr      ""GetAwaiter""
    IL_001a:  ldnull
    IL_001b:  ldtoken    ""C""
    IL_0020:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_0025:  ldc.i4.1
    IL_0026:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_002b:  dup
    IL_002c:  ldc.i4.0
    IL_002d:  ldc.i4.0
    IL_002e:  ldnull
    IL_002f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_0034:  stelem.ref
    IL_0035:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_003a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_003f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__0""
    IL_0044:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__0""
    IL_0049:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
    IL_004e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__0""
    IL_0053:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>o__0.<>p__0""
    IL_0058:  brtrue.s   IL_0084
    IL_005a:  ldc.i4.0
    IL_005b:  ldtoken    ""C""
    IL_0060:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_0065:  ldc.i4.1
    IL_0066:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_006b:  dup
    IL_006c:  ldc.i4.0
    IL_006d:  ldc.i4.0
    IL_006e:  ldnull
    IL_006f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_0074:  stelem.ref
    IL_0075:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Invoke(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_007a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_007f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>o__0.<>p__0""
    IL_0084:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>o__0.<>p__0""
    IL_0089:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
    IL_008e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>o__0.<>p__0""
    IL_0093:  ldarg.0
    IL_0094:  ldfld      ""<>x.<>c__DisplayClass0_0 <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>4__this""
    IL_0099:  ldfld      ""C <>x.<>c__DisplayClass0_0.<>4__this""
    IL_009e:  ldfld      ""dynamic C.d""
    IL_00a3:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
    IL_00a8:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
    IL_00ad:  stloc.2
    IL_00ae:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__2""
    IL_00b3:  brtrue.s   IL_00da
    IL_00b5:  ldc.i4.s   16
    IL_00b7:  ldtoken    ""bool""
    IL_00bc:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_00c1:  ldtoken    ""C""
    IL_00c6:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_00cb:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
    IL_00d0:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_00d5:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__2""
    IL_00da:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__2""
    IL_00df:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
    IL_00e4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__2""
    IL_00e9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__1""
    IL_00ee:  brtrue.s   IL_011f
    IL_00f0:  ldc.i4.0
    IL_00f1:  ldstr      ""IsCompleted""
    IL_00f6:  ldtoken    ""C""
    IL_00fb:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_0100:  ldc.i4.1
    IL_0101:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_0106:  dup
    IL_0107:  ldc.i4.0
    IL_0108:  ldc.i4.0
    IL_0109:  ldnull
    IL_010a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_010f:  stelem.ref
    IL_0110:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_0115:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_011a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__1""
    IL_011f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__1""
    IL_0124:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
    IL_0129:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__1""
    IL_012e:  ldloc.2
    IL_012f:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
    IL_0134:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
    IL_0139:  brtrue.s   IL_019c
    IL_013b:  ldarg.0
    IL_013c:  ldc.i4.0
    IL_013d:  dup
    IL_013e:  stloc.0
    IL_013f:  stfld      ""int <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>1__state""
    IL_0144:  ldarg.0
    IL_0145:  ldloc.2
    IL_0146:  stfld      ""object <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>u__1""
    IL_014b:  ldloc.2
    IL_014c:  isinst     ""System.Runtime.CompilerServices.ICriticalNotifyCompletion""
    IL_0151:  stloc.3
    IL_0152:  ldloc.3
    IL_0153:  brtrue.s   IL_0170
    IL_0155:  ldloc.2
    IL_0156:  castclass  ""System.Runtime.CompilerServices.INotifyCompletion""
    IL_015b:  stloc.s    V_4
    IL_015d:  ldarg.0
    IL_015e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>t__builder""
    IL_0163:  ldloca.s   V_4
    IL_0165:  ldarg.0
    IL_0166:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitOnCompleted<System.Runtime.CompilerServices.INotifyCompletion, <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d>(ref System.Runtime.CompilerServices.INotifyCompletion, ref <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d)""
    IL_016b:  ldnull
    IL_016c:  stloc.s    V_4
    IL_016e:  br.s       IL_017e
    IL_0170:  ldarg.0
    IL_0171:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>t__builder""
    IL_0176:  ldloca.s   V_3
    IL_0178:  ldarg.0
    IL_0179:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ICriticalNotifyCompletion, <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d>(ref System.Runtime.CompilerServices.ICriticalNotifyCompletion, ref <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d)""
    IL_017e:  ldnull
    IL_017f:  stloc.3
    IL_0180:  leave      IL_021a
    IL_0185:  ldarg.0
    IL_0186:  ldfld      ""object <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>u__1""
    IL_018b:  stloc.2
    IL_018c:  ldarg.0
    IL_018d:  ldnull
    IL_018e:  stfld      ""object <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>u__1""
    IL_0193:  ldarg.0
    IL_0194:  ldc.i4.m1
    IL_0195:  dup
    IL_0196:  stloc.0
    IL_0197:  stfld      ""int <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>1__state""
    IL_019c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__3""
    IL_01a1:  brtrue.s   IL_01d3
    IL_01a3:  ldc.i4.0
    IL_01a4:  ldstr      ""GetResult""
    IL_01a9:  ldnull
    IL_01aa:  ldtoken    ""C""
    IL_01af:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_01b4:  ldc.i4.1
    IL_01b5:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_01ba:  dup
    IL_01bb:  ldc.i4.0
    IL_01bc:  ldc.i4.0
    IL_01bd:  ldnull
    IL_01be:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_01c3:  stelem.ref
    IL_01c4:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_01c9:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_01ce:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__3""
    IL_01d3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__3""
    IL_01d8:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
    IL_01dd:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>o.<>p__3""
    IL_01e2:  ldloc.2
    IL_01e3:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
    IL_01e8:  ldnull
    IL_01e9:  stloc.2
    IL_01ea:  stloc.1
    IL_01eb:  leave.s    IL_0206
  }
  catch System.Exception
  {
    IL_01ed:  stloc.s    V_5
    IL_01ef:  ldarg.0
    IL_01f0:  ldc.i4.s   -2
    IL_01f2:  stfld      ""int <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>1__state""
    IL_01f7:  ldarg.0
    IL_01f8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>t__builder""
    IL_01fd:  ldloc.s    V_5
    IL_01ff:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.SetException(System.Exception)""
    IL_0204:  leave.s    IL_021a
  }
  IL_0206:  ldarg.0
  IL_0207:  ldc.i4.s   -2
  IL_0209:  stfld      ""int <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>1__state""
  IL_020e:  ldarg.0
  IL_020f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <>x.<>c__DisplayClass0_0.<<<>m0>b__0>d.<>t__builder""
  IL_0214:  ldloc.1
  IL_0215:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.SetResult(object)""
  IL_021a:  ret
}
");
        }

        [WorkItem(1072296)]
        [Fact]
        public void InvokeStaticMemberInLambda()
        {
            var source = @"
class C
{
    static dynamic x;

    static void Foo(dynamic y)
    {
        System.Action a = () => Foo(x);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, CSharpRef }, TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(comp);

            var context = CreateMethodContext(runtime, "C.Foo");
            var testData = new CompilationTestData();
            string error;
            var result = context.CompileAssignment("a", "() => Foo(x)", out error, testData);
            Assert.Null(error);
            VerifyCustomTypeInfo(result, null);
            testData.GetMethodData("<>x.<>c.<<>m0>b__0_0").VerifyIL(@"
{
  // Code size      106 (0x6a)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> <>x.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0046
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""Foo""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.s   33
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  dup
  IL_002e:  ldc.i4.1
  IL_002f:  ldc.i4.0
  IL_0030:  ldnull
  IL_0031:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0036:  stelem.ref
  IL_0037:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003c:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0041:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> <>x.<>o__0.<>p__0""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> <>x.<>o__0.<>p__0""
  IL_004b:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>>.Target""
  IL_0050:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> <>x.<>o__0.<>p__0""
  IL_0055:  ldtoken    ""<>x""
  IL_005a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005f:  ldsfld     ""dynamic C.x""
  IL_0064:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, dynamic)""
  IL_0069:  ret
}");

            context = CreateMethodContext(runtime, "C.<>c.<Foo>b__1_0");
            testData = new CompilationTestData();
            result = context.CompileExpression("Foo(x)", out error, testData);
            Assert.Null(error);
            VerifyCustomTypeInfo(result, 0x01);
            var methodData = testData.GetMethodData("<>x.<>m0");
            methodData.VerifyIL(@"
{
  // Code size      102 (0x66)
  .maxstack  9
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0005:  brtrue.s   IL_0042
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""Foo""
  IL_000d:  ldnull
  IL_000e:  ldtoken    ""C""
  IL_0013:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0018:  ldc.i4.2
  IL_0019:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001e:  dup
  IL_001f:  ldc.i4.0
  IL_0020:  ldc.i4.s   33
  IL_0022:  ldnull
  IL_0023:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0028:  stelem.ref
  IL_0029:  dup
  IL_002a:  ldc.i4.1
  IL_002b:  ldc.i4.0
  IL_002c:  ldnull
  IL_002d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0032:  stelem.ref
  IL_0033:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0038:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_003d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0042:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0047:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>>.Target""
  IL_004c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>x.<>o__0.<>p__0""
  IL_0051:  ldtoken    ""<>x""
  IL_0056:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005b:  ldsfld     ""dynamic C.x""
  IL_0060:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, dynamic)""
  IL_0065:  ret
}");
        }

        [WorkItem(1095613)]
        [Fact(Skip = "1095613")]
        public void HoistedLocalsLoseDynamicAttribute()
        {
            var source = @"
class C
{
    static void M(dynamic x)
    {
        dynamic y = 3;
        System.Func<dynamic> a = () => x + y;
    }

    static void Foo(int x)
    {
        M(x);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, CSharpRef }, TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(comp);

            var context = CreateMethodContext(runtime, "C.M");
            var testData = new CompilationTestData();
            string error;
            var result = context.CompileExpression("Foo(x)", out error, testData);
            Assert.Null(error);
            VerifyCustomTypeInfo(result, 0x01);
            testData.GetMethodData("<>c.<>m0()").VerifyIL(@"
{
  // Code size      166 (0xa6)
  .maxstack  11
  .locals init (System.Func<dynamic> V_0) //a
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Func<dynamic>>> <>c.<<>m0>o__SiteContainer0.<>p__Site2""
  IL_0005:  brtrue.s   IL_002b
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""System.Func<dynamic>""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldtoken    ""<>c""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0021:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Func<dynamic>>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Func<dynamic>>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0026:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Func<dynamic>>> <>c.<<>m0>o__SiteContainer0.<>p__Site2""
  IL_002b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Func<dynamic>>> <>c.<<>m0>o__SiteContainer0.<>p__Site2""
  IL_0030:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Func<dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Func<dynamic>>>.Target""
  IL_0035:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Func<dynamic>>> <>c.<<>m0>o__SiteContainer0.<>p__Site2""
  IL_003a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>c.<<>m0>o__SiteContainer0.<>p__Site1""
  IL_003f:  brtrue.s   IL_007c
  IL_0041:  ldc.i4.0
  IL_0042:  ldstr      ""Foo""
  IL_0047:  ldnull
  IL_0048:  ldtoken    ""<>c""
  IL_004d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0052:  ldc.i4.2
  IL_0053:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0058:  dup
  IL_0059:  ldc.i4.0
  IL_005a:  ldc.i4.s   33
  IL_005c:  ldnull
  IL_005d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0062:  stelem.ref
  IL_0063:  dup
  IL_0064:  ldc.i4.1
  IL_0065:  ldc.i4.0
  IL_0066:  ldnull
  IL_0067:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006c:  stelem.ref
  IL_006d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0072:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0077:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>c.<<>m0>o__SiteContainer0.<>p__Site1""
  IL_007c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>c.<<>m0>o__SiteContainer0.<>p__Site1""
  IL_0081:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>>.Target""
  IL_0086:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>c.<<>m0>o__SiteContainer0.<>p__Site1""
  IL_008b:  ldtoken    ""<>c""
  IL_0090:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0095:  ldsfld     ""dynamic C.x""
  IL_009a:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, dynamic)""
  IL_009f:  callvirt   ""System.Func<dynamic> System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Func<dynamic>>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_00a4:  stloc.0
  IL_00a5:  ret
}");
            result = context.CompileExpression("Foo(y)", out error, testData);
            Assert.Null(error);
            VerifyCustomTypeInfo(result, 0x01);
            testData.GetMethodData("<>c.<>m0()").VerifyIL(@"
{
  // Code size      166 (0xa6)
  .maxstack  11
  .locals init (System.Func<dynamic> V_0) //a
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Func<dynamic>>> <>c.<<>m0>o__SiteContainer0.<>p__Site2""
  IL_0005:  brtrue.s   IL_002b
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""System.Func<dynamic>""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldtoken    ""<>c""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0021:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Func<dynamic>>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Func<dynamic>>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0026:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Func<dynamic>>> <>c.<<>m0>o__SiteContainer0.<>p__Site2""
  IL_002b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Func<dynamic>>> <>c.<<>m0>o__SiteContainer0.<>p__Site2""
  IL_0030:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Func<dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Func<dynamic>>>.Target""
  IL_0035:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Func<dynamic>>> <>c.<<>m0>o__SiteContainer0.<>p__Site2""
  IL_003a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>c.<<>m0>o__SiteContainer0.<>p__Site1""
  IL_003f:  brtrue.s   IL_007c
  IL_0041:  ldc.i4.0
  IL_0042:  ldstr      ""Foo""
  IL_0047:  ldnull
  IL_0048:  ldtoken    ""<>c""
  IL_004d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0052:  ldc.i4.2
  IL_0053:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0058:  dup
  IL_0059:  ldc.i4.0
  IL_005a:  ldc.i4.s   33
  IL_005c:  ldnull
  IL_005d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0062:  stelem.ref
  IL_0063:  dup
  IL_0064:  ldc.i4.1
  IL_0065:  ldc.i4.0
  IL_0066:  ldnull
  IL_0067:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006c:  stelem.ref
  IL_006d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0072:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0077:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>c.<<>m0>o__SiteContainer0.<>p__Site1""
  IL_007c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>c.<<>m0>o__SiteContainer0.<>p__Site1""
  IL_0081:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>>.Target""
  IL_0086:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>> <>c.<<>m0>o__SiteContainer0.<>p__Site1""
  IL_008b:  ldtoken    ""<>c""
  IL_0090:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0095:  ldsfld     ""dynamic C.x""
  IL_009a:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, System.Type, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, dynamic)""
  IL_009f:  callvirt   ""System.Func<dynamic> System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Func<dynamic>>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_00a4:  stloc.0
  IL_00a5:  ret
}");
        }

        private static void VerifyCustomTypeInfo(LocalAndMethod localAndMethod, string expectedName, params byte[] expectedBytes)
        {
            Assert.Equal(localAndMethod.LocalName, expectedName);
            VerifyCustomTypeInfo(localAndMethod.GetCustomTypeInfo(), expectedBytes);
        }

        private static void VerifyCustomTypeInfo(CompileResult compileResult, params byte[] expectedBytes)
        {
            VerifyCustomTypeInfo(compileResult.GetCustomTypeInfo(), expectedBytes);
        }

        private static void VerifyCustomTypeInfo(CustomTypeInfo customTypeInfo, byte[] expectedBytes)
        {
            Assert.Equal(DynamicFlagsCustomTypeInfo.PayloadTypeId, customTypeInfo.PayloadTypeId);
            Assert.Equal(expectedBytes, customTypeInfo.Payload);
        }
    }
}
