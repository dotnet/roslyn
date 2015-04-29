// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.Test.Utilities;
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
            VerifyCustomTypeInfo(locals[0], 0x01);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (dynamic V_0) //d
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
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
            VerifyCustomTypeInfo(locals[0], 0x02);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (dynamic[] V_0) //d
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
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
            VerifyCustomTypeInfo(locals[0], 0x02);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (System.Collections.Generic.List<dynamic> V_0) //d
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
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

            var runtime = CreateRuntimeInstance(ExpressionCompilerUtilities.GenerateUniqueName(), references, exeBytes, new SymReader(pdbBytes, exeBytes));

            var context = CreateMethodContext(runtime, "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(1, locals.Count);
            var method = testData.Methods.Single().Value.Method;
            AssertHasDynamicAttribute(method);
            Assert.Equal(TypeKind.Dynamic, method.ReturnType.TypeKind);
            VerifyCustomTypeInfo(locals[0], 0x01);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}");
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

            var runtime = CreateRuntimeInstance(ExpressionCompilerUtilities.GenerateUniqueName(), references, exeBytes, new SymReader(pdbBytes, exeBytes));

            var context = CreateMethodContext(runtime, "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(1, locals.Count);
            var method = testData.Methods.Single().Value.Method;
            AssertHasDynamicAttribute(method);
            Assert.Equal(TypeKind.Dynamic, ((ArrayTypeSymbol)method.ReturnType).ElementType.TypeKind);
            VerifyCustomTypeInfo(locals[0], 0x02);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt: @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}");
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

            var runtime = CreateRuntimeInstance(ExpressionCompilerUtilities.GenerateUniqueName(), references, exeBytes, new SymReader(pdbBytes, exeBytes));

            var context = CreateMethodContext(runtime, "C.M");
            var testData = new CompilationTestData();
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.Equal(1, locals.Count);
            var method = testData.Methods.Single().Value.Method;
            AssertHasDynamicAttribute(method);
            Assert.Equal(TypeKind.Dynamic, ((NamedTypeSymbol)method.ReturnType).TypeArguments.Single().TypeKind);
            VerifyCustomTypeInfo(locals[0], 0x02);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt: @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}");
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
            VerifyCustomTypeInfo(locals[0], 0x01);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
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
            VerifyCustomTypeInfo(locals[0], 0x02);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
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
            VerifyCustomTypeInfo(locals[0], 0x02);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d", expectedILOpt:
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
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
            VerifyCustomTypeInfo(locals[0], 0x04, 0x03);
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
                InspectionContextFactory.Empty,
                "var dd = d;",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Null(error);
            VerifyCustomTypeInfo(result, null);
            Assert.Empty(missingAssemblyIdentities);
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
            var context = CreateMethodContext(runtime, methodName: "C.M");
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var diagnostics = DiagnosticBag.GetInstance();
            var builder = ArrayBuilder<Alias>.GetInstance();
            builder.Add(new Alias(
                AliasKind.DeclaredLocal, 
                "d1", 
                "d1", 
                typeof(object).AssemblyQualifiedName, 
                MakeCustomTypeInfo(true)));
            builder.Add(new Alias(
                AliasKind.DeclaredLocal, 
                "d2", 
                "d2", 
                typeof(Dictionary<Dictionary<dynamic, Dictionary<object[], dynamic[]>>, object>).AssemblyQualifiedName, 
                MakeCustomTypeInfo(false, false, true, false, false, false, false, true, false)));
            var aliases = new ReadOnlyCollection<Alias>(builder.ToArrayAndFree());

            var testData = new CompilationTestData();
            context.CompileGetLocals(
                aliases,
                locals,
                argumentsOnly: false,
                diagnostics: diagnostics,
                typeName: out typeName,
                testData: testData);
            diagnostics.Free();
            Assert.Equal(locals.Count, 2);

            VerifyCustomTypeInfo(locals[0], 0x01);
            VerifyLocal(testData, typeName, locals[0], "<>m0", "d1", expectedILOpt:
@"{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldstr      ""d1""
  IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_000a:  ret
}");

            VerifyCustomTypeInfo(locals[1], 0x84, 0x00); // Note: read flags right-to-left in each byte: 0010 0001 0(000 0000)
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
            var dynamicFlagsInfo = new DynamicFlagsCustomTypeInfo(new BitArray(flags));
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
  IL_000e:  ldtoken    ""<>x""
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
  IL_0012:  ldtoken    ""<>x""
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
  IL_000e:  ldtoken    ""<>x""
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

        private static void VerifyCustomTypeInfo(LocalAndMethod localAndMethod, params byte[] expectedBytes)
        {
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
