' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.ObjectModel
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

    Public Class TupleTests
        Inherits ExpressionCompilerTestBase

        <WorkItem(13948, "https://github.com/dotnet/roslyn/issues/13948")>
        <Fact(Skip:="13948")>
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
  .locals init ((int A, int B) V_0) //o
  IL_0000:  ldloc.0
  IL_0001:  ret
}")
                    locals.Free()
                End Sub)
        End Sub

        <WorkItem(13948, "https://github.com/dotnet/roslyn/issues/13948")>
        <Fact(Skip:="13948")>
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

    End Class

End Namespace
