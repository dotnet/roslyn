' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

    Public Class TupleTests
        Inherits ExpressionCompilerTestBase

        <Fact>
        Public Sub Literal()
            Const source =
"Class C
    Shared Sub M()
        Dim o As (Integer, Integer)
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, references:={ValueTupleRef}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")
                    Dim errorMessage As String = Nothing
                    Dim testData = New CompilationTestData()
                    Dim result = context.CompileExpression(
                        "(A:=1, B:=2)",
                        DkmEvaluationFlags.TreatAsExpression,
                        NoAliases,
                        errorMessage,
                        testData)
                    Assert.Null(errorMessage)
                    Dim typeInfo As ReadOnlyCollection(Of Byte) = Nothing
                    Dim typeInfoId = result.GetCustomTypeInfo(typeInfo)
                    Assert.NotNull(typeInfo)
                    Dim dynamicFlags As ReadOnlyCollection(Of Byte) = Nothing
                    Dim tupleElementNames As ReadOnlyCollection(Of String) = Nothing
                    CustomTypeInfo.Decode(typeInfoId, typeInfo, dynamicFlags, tupleElementNames)
                    Assert.Equal({"A", "B"}, tupleElementNames)
                    Dim methodData = testData.GetMethodData("<>x.<>m0")
                    Dim method = methodData.Method
                    Assert.True(method.ReturnType.IsTupleType)
                    Assert.NotNull(GetTupleElementNamesAttributeIfAny(method))
                    methodData.VerifyIL(
"{
  // Code size        8 (0x8)
  .maxstack  2
  .locals init (System.ValueTuple(Of Integer, Integer) V_0) //o
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)""
  IL_0007:  ret
}")
                End Sub)
        End Sub

        <Fact>
        Public Sub TupleElementNamesAttribute_NotAvailable()
            Const source =
"Namespace System
    Public Structure ValueTuple(Of T1, T2)
        Public Item1 As T1
        Public Item2 As T2
        Public Sub New(_1 As T1, _2 As T2)
            Item1 = _1
            Item2 = _2
        End Sub
    End Structure
End Namespace
Class C
    Shared Sub M()
        Dim o As (Integer, Integer)
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")
                    Dim errorMessage As String = Nothing
                    Dim testData = New CompilationTestData()
                    Dim result = context.CompileExpression(
                        "(A:=1, B:=2)",
                        DkmEvaluationFlags.TreatAsExpression,
                        NoAliases,
                        errorMessage,
                        testData)
                    Assert.Null(errorMessage)
                    Dim typeInfo As ReadOnlyCollection(Of Byte) = Nothing
                    Dim typeInfoId = result.GetCustomTypeInfo(typeInfo)
                    Assert.Null(typeInfo)
                    Dim methodData = testData.GetMethodData("<>x.<>m0")
                    Dim method = methodData.Method
                    Assert.True(method.ReturnType.IsTupleType)
                    Assert.Null(GetTupleElementNamesAttributeIfAny(method))
                    methodData.VerifyIL(
"{
  // Code size        8 (0x8)
  .maxstack  2
  .locals init (System.ValueTuple(Of Integer, Integer) V_0) //o
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)""
  IL_0007:  ret
}")
                End Sub)
        End Sub

        <WorkItem(13948, "https://github.com/dotnet/roslyn/issues/13948")>
        <Fact>
        Public Sub Local()
            Const source =
"class C
{
    static void M()
    {
        (int A, int B) o = (1, 2);
    }
}"
            Dim comp = CreateCSharpCompilation(source, referencedAssemblies:={MscorlibRef, ValueTupleRef})
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")
                    Dim testData = New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim assembly = context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    Assert.Equal(1, locals.Count)
                    Dim typeInfo As ReadOnlyCollection(Of Byte) = Nothing
                    Dim typeInfoId = locals(0).GetCustomTypeInfo(typeInfo)
                    Dim dynamicFlags As ReadOnlyCollection(Of Byte) = Nothing
                    Dim tupleElementNames As ReadOnlyCollection(Of String) = Nothing
                    CustomTypeInfo.Decode(typeInfoId, typeInfo, dynamicFlags, tupleElementNames)
                    Assert.Equal({"A", "B"}, tupleElementNames)
                    Dim method = testData.Methods.Single().Value.Method
                    Assert.NotNull(GetTupleElementNamesAttributeIfAny(method))
                    Assert.True(method.ReturnType.IsTupleType)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "o", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init ((A As Integer, B As Integer) V_0) //o
  IL_0000:  ldloc.0
  IL_0001:  ret
}")
                    locals.Free()
                End Sub)
        End Sub

        <WorkItem(13948, "https://github.com/dotnet/roslyn/issues/13948")>
        <Fact>
        Public Sub Constant()
            Const source =
"class A<T>
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
}"
            Dim comp = CreateCSharpCompilation(source, referencedAssemblies:={MscorlibRef, ValueTupleRef})
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")
                    Dim testData = New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim assembly = context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    Assert.Equal(1, locals.Count)
                    Dim typeInfo As ReadOnlyCollection(Of Byte) = Nothing
                    Dim typeInfoId = locals(0).GetCustomTypeInfo(typeInfo)
                    Dim dynamicFlags As ReadOnlyCollection(Of Byte) = Nothing
                    Dim tupleElementNames As ReadOnlyCollection(Of String) = Nothing
                    CustomTypeInfo.Decode(typeInfoId, typeInfo, dynamicFlags, tupleElementNames)
                    Assert.Equal({Nothing, "A", "B", Nothing}, tupleElementNames)
                    Dim method = DirectCast(testData.Methods.Single().Value.Method, MethodSymbol)
                    Assert.NotNull(GetTupleElementNamesAttributeIfAny(method))
                    Dim returnType = method.ReturnType
                    Assert.False(returnType.IsTupleType)
                    Assert.True(returnType.ContainsTuple())
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "c", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}")
                    locals.Free()
                End Sub)
        End Sub

        ''' <summary>
        ''' Locals declared in the VB EE do not have an explicit
        ''' type and are statically typed to Object, so tuple
        ''' element names on the value are not preserved.
        ''' </summary>
        <Fact>
        Public Sub DeclareLocal()
            Const source =
"Class C
    Shared Sub M()
        Dim x = (1, 2)
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, references:={ValueTupleRef}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")
                    Dim errorMessage As String = Nothing
                    Dim testData = New CompilationTestData()
                    Dim result = context.CompileExpression(
                        "y = DirectCast(x, (A As Integer, B As Integer))",
                        DkmEvaluationFlags.None,
                        NoAliases,
                        errorMessage,
                        testData)
                    Assert.Null(errorMessage)
                    Dim typeInfo As ReadOnlyCollection(Of Byte) = Nothing
                    Dim typeInfoId = result.GetCustomTypeInfo(typeInfo)
                    Assert.Null(typeInfo)
                    Dim methodData = testData.GetMethodData("<>x.<>m0")
                    Dim method = methodData.Method
                    Assert.Null(GetTupleElementNamesAttributeIfAny(method))
                    methodData.VerifyIL(
"{
  // Code size       43 (0x2b)
  .maxstack  4
  .locals init (System.ValueTuple(Of Integer, Integer) V_0, //x
                System.Guid V_1)
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""y""
  IL_000f:  ldloca.s   V_1
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.1
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""y""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_0028:  ldloc.0
  IL_0029:  stind.ref
  IL_002a:  ret
}")
                End Sub)
        End Sub

        <WorkItem(13589, "https://github.com/dotnet/roslyn/issues/13589")>
        <Fact>
        Public Sub [Alias]()
            Const source =
"Class C
    Shared F As (Integer, Integer)
    Shared Sub M()
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, references:={ValueTupleRef}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim aliasElementNames = New ReadOnlyCollection(Of String)({"A", "B", Nothing, "D"})
                    Dim [alias] = New [Alias](
                        DkmClrAliasKind.Variable,
                        "t",
                        "t",
                        "System.ValueTuple`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.ValueTuple`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], System.ValueTuple, Version=4.0.1.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51]][], System.ValueTuple, Version=4.0.1.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51",
                        CustomTypeInfo.PayloadTypeId,
                        CustomTypeInfo.Encode(Nothing, aliasElementNames))
                    Dim diagnostics = DiagnosticBag.GetInstance()
                    Dim testData = New CompilationTestData()
                    Dim assembly = context.CompileGetLocals(
                        locals,
                        argumentsOnly:=False,
                        aliases:=ImmutableArray.Create([alias]),
                        diagnostics:=diagnostics,
                        typeName:=typeName,
                        testData:=testData)
                    diagnostics.Verify()
                    diagnostics.Free()
                    Assert.Equal(1, locals.Count)
                    Dim typeInfo As ReadOnlyCollection(Of Byte) = Nothing
                    Dim typeInfoId = locals(0).GetCustomTypeInfo(typeInfo)
                    Dim dynamicFlags As ReadOnlyCollection(Of Byte) = Nothing
                    Dim tupleElementNames As ReadOnlyCollection(Of String) = Nothing
                    CustomTypeInfo.Decode(typeInfoId, typeInfo, dynamicFlags, tupleElementNames)
                    Assert.Equal(aliasElementNames, tupleElementNames)
                    Dim method = testData.Methods.Single().Value.Method
                    Assert.NotNull(GetTupleElementNamesAttributeIfAny(method))
                    Dim returnType = DirectCast(method.ReturnType, TypeSymbol)
                    Assert.False(returnType.IsTupleType)
                    Assert.True(DirectCast(returnType, ArrayTypeSymbol).ElementType.IsTupleType)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "t", expectedILOpt:=
"{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldstr      ""t""
  IL_0005:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_000a:  castclass  ""(A As Integer, B As (Integer, D As Integer))()""
  IL_000f:  ret
}")
                    locals.Free()
                End Sub)
        End Sub

    End Class

End Namespace
