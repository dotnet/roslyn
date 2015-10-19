' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class AccessibilityTests
        Inherits ExpressionCompilerTestBase

        ''' <summary>
        ''' Do not allow calling accessors directly.
        ''' (This Is consistent with the native EE.)
        ''' </summary>
        <Fact>
        Public Sub NotReferencable()
            Const source = "
Class C
    ReadOnly Property P As Object
        Get
            Return Nothing
        End Get
    End Property

    Sub M()
    End Sub
End Class
"

            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M")

            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            context.CompileExpression(
                "Me.get_P()",
                DkmEvaluationFlags.TreatAsExpression,
                NoAliases,
                DebuggerDiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData:=Nothing)
            Assert.Equal("error BC30456: 'get_P' is not a member of 'C'.", errorMessage)
        End Sub

        <Fact>
        Public Sub ParametersAndReturnType_PrivateType()
            Const source = "
Class A
    Private Structure S
    End Structure
End Class

Class B
    Shared Function F(Of T)(t1 As T) As T
        Return t1
    End Function

    Shared Sub M()
    End Sub
End Class
"
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="B.M",
                expr:="F(New A.S())")
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (A.S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""A.S""
  IL_0008:  ldloc.0
  IL_0009:  call       ""Function B.F(Of A.S)(A.S) As A.S""
  IL_000e:  ret
}
")
        End Sub

        <Fact>
        Public Sub ParametersAndReturnType_DifferentCompilation()
            Const source = "
Class A
    Private Structure S
    End Structure
End Class

Class B
    Shared Function F(Of T)(t1 As T) As T
        Return t1
    End Function

    Shared Sub M()
    End Sub
End Class
"
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="B.M",
                expr:="F(New With { .P = 1 })")

            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  newobj     ""Sub VB$AnonymousType_0(Of Integer)..ctor(Integer)""
  IL_0006:  call       ""Function B.F(Of <anonymous type: P As Integer>)(<anonymous type: P As Integer>) As <anonymous type: P As Integer>""
  IL_000b:  ret
}
")
        End Sub

        ''' <summary>
        ''' As during regular compilation, we emit calls to the most-derived method.
        ''' (In contrast, regular C# compilation emits calls to the least-derived method.)
        ''' </summary>
        <Fact>
        Public Sub ProtectedAndFriendVirtualCalls()
            Const source = "
Friend Class A
    Protected Overridable Function M(o As Integer) As Integer
        Return o
    End Function

    Friend Overridable ReadOnly Property P As Integer
        Get
            Return 0
        End Get
    End Property
End Class

Friend Class B
    Inherits A

    Protected Overrides Function M(o As Integer) As Integer
        Return o
    End Function
End Class

Friend Class C
    Inherits B

    Friend Overrides ReadOnly Property P As Integer
        Get
            Return 0
        End Get
    End Property

    Public Function Test() As Integer
        Return Me.M(Me.P)
    End Function
End Class
"

            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(
                runtime,
                methodName:="C.Test")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("Me.M(Me.P)", errorMessage, testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (Integer V_0) //Test
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  callvirt   ""Function C.get_P() As Integer""
  IL_0007:  callvirt   ""Function B.M(Integer) As Integer""
  IL_000c:  ret
}")
        End Sub

        <Fact>
        Public Sub InferredTypeArguments_DifferentCompilation()
            Const source = "
Class C
    Shared Function F(Of T, U)(t1 As T, u1 As U) As Object
        Return t1
    End Function

    Shared x As Object = New With { .A = 1 }

    Shared Sub M()
    End Sub
End Class
"
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="F(New With { .A = 2 }, New With { .B = 3 })") ' New and existing types
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldc.i4.2
  IL_0001:  newobj     ""Sub VB$AnonymousType_0(Of Integer)..ctor(Integer)""
  IL_0006:  ldc.i4.3
  IL_0007:  newobj     ""Sub VB$AnonymousType_1(Of Integer)..ctor(Integer)""
  IL_000c:  call       ""Function C.F(Of <anonymous type: A As Integer>, <anonymous type: B As Integer>)(<anonymous type: A As Integer>, <anonymous type: B As Integer>) As Object""
  IL_0011:  ret
}
")
        End Sub

    End Class
End Namespace