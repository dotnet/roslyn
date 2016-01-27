' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.PdbUtilities
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests
    Public Class ResultPropertiesTests
        Inherits ExpressionCompilerTestBase

        <Fact>
        Public Sub Category()
            Const source = "
Class C
    Public Property P() As Integer
    Public F As Integer
    Public Function M() As Integer
        Return 0
    End Function

    Sub Test(p As Integer)
        Dim l As Integer
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, methodName:="C.Test")

            For Each expr In {"Me", "Nothing", "1", "F", "p", "l"}
                Assert.Equal(DkmEvaluationResultCategory.Data, GetResultProperties(context, expr).Category)
            Next

            Assert.Equal(DkmEvaluationResultCategory.Method, GetResultProperties(context, "M()").Category)
            Assert.Equal(DkmEvaluationResultCategory.Property, GetResultProperties(context, "Me.P").Category)
        End Sub

        <Fact>
        Public Sub StorageType()
            Const source = "
Class C
    Public Property P() As Integer
    Public F As Integer
    Public Function M() As Integer
        Return 0
    End Function

    Public Shared Property SP() As Integer
    Public Shared SF As Integer
    Public Shared Function SM() As Integer
        Return 0
    End Function

    Sub Test(p As Integer)
        Dim l As Integer
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, methodName:="C.Test")

            For Each expr In {"Me", "Nothing", "1", "P", "F", "M()", "p", "l"}
                Assert.Equal(DkmEvaluationResultStorageType.None, GetResultProperties(context, expr).StorageType)
            Next

            For Each expr In {"SP", "SF", "SM()"}
                Assert.Equal(DkmEvaluationResultStorageType.Static, GetResultProperties(context, expr).StorageType)
            Next
        End Sub

        <Fact>
        Public Sub AccessType()
            Const ilSource = "
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .field private int32 Private
  .field family int32 Protected
  .field assembly int32 Internal
  .field public int32 Public
  .field famorassem int32 ProtectedInternal
  .field famandassem int32 ProtectedAndInternal

  .method public hidebysig instance void 
          Test() cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } // end of method C::.ctor

} // end of class C
"
            Dim exeBytes As ImmutableArray(Of Byte) = Nothing
            Dim pdbBytes As ImmutableArray(Of Byte) = Nothing
            EmitILToArray(ilSource, appendDefaultHeader:=True, includePdb:=True, assemblyBytes:=exeBytes, pdbBytes:=pdbBytes)

            Dim runtime = CreateRuntimeInstance(
                assemblyName:=GetUniqueName(),
                references:=ImmutableArray.Create(MscorlibRef),
                exeBytes:=exeBytes.ToArray(),
                symReader:=SymReaderFactory.CreateReader(pdbBytes))
            Dim context = CreateMethodContext(runtime, methodName:="C.Test")

            Assert.Equal(DkmEvaluationResultAccessType.Private, GetResultProperties(context, "[Private]").AccessType)
            Assert.Equal(DkmEvaluationResultAccessType.Protected, GetResultProperties(context, "[Protected]").AccessType)
            Assert.Equal(DkmEvaluationResultAccessType.Internal, GetResultProperties(context, "[Internal]").AccessType)
            Assert.Equal(DkmEvaluationResultAccessType.Public, GetResultProperties(context, "[Public]").AccessType)

            ' As in dev12.
            Assert.Equal(DkmEvaluationResultAccessType.Internal, GetResultProperties(context, "ProtectedInternal").AccessType)
            Assert.Equal(DkmEvaluationResultAccessType.Internal, GetResultProperties(context, "ProtectedAndInternal").AccessType)

            Assert.Equal(DkmEvaluationResultAccessType.None, GetResultProperties(context, "Nothing").AccessType)
        End Sub

        <Fact>
        Public Sub AccessType_Nested()
            Const source = "
Imports System

Friend Class C
    Public F As Integer

    Sub Test()
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, methodName:="C.Test")

            ' Used the declared accessibility, rather than the effective accessibility.
            Assert.Equal(DkmEvaluationResultAccessType.Public, GetResultProperties(context, "F").AccessType)
        End Sub

        <Fact>
        Public Sub ModifierFlags_Virtual()
            Const source = "
Imports System

Class C
    Public Property P() As Integer
    Public Function M() As Integer
        Return 0    
    End Function

    Public Overridable Property VP() As Integer
    Public Overridable Function VM() As Integer
        Return 0    
    End Function

    Sub Test()
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, methodName:="C.Test")

            ' NOTE: VB doesn't have virtual events

            Assert.Equal(DkmEvaluationResultTypeModifierFlags.None, GetResultProperties(context, "P").ModifierFlags)
            Assert.Equal(DkmEvaluationResultTypeModifierFlags.Virtual, GetResultProperties(context, "VP").ModifierFlags)

            Assert.Equal(DkmEvaluationResultTypeModifierFlags.None, GetResultProperties(context, "M()").ModifierFlags)
            Assert.Equal(DkmEvaluationResultTypeModifierFlags.Virtual, GetResultProperties(context, "VM()").ModifierFlags)
        End Sub

        <Fact>
        Public Sub ModifierFlags_Virtual_Variations()
            Const source = "
Imports System

MustInherit Class Base
    Public MustOverride Property [Overrides]() As Integer
End Class

MustInherit Class Derived : Inherits Base
    Public Overrides Property [Overrides]() As Integer
    Public MustOverride Property [MustOverride]() As Integer

    Sub Test()
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, methodName:="Derived.Test")

            Assert.Equal(DkmEvaluationResultTypeModifierFlags.Virtual, GetResultProperties(context, "[MustOverride]").ModifierFlags)
            Assert.Equal(DkmEvaluationResultTypeModifierFlags.Virtual, GetResultProperties(context, "[Overrides]").ModifierFlags)
        End Sub

        <Fact>
        Public Sub ModifierFlags_Constant()
            Const source = "
Imports System

Class C
    Private F As Integer = 1
    Private Const CF As Integer = 1
    Private Shared ReadOnly SRF = 1

    Sub Test(p As Integer)
        Dim l As Integer = 2
        Const cl As Integer = 2
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, methodName:="C.Test")

            For Each expr In {"Nothing", "1", "1 + 1", "CF", "cl"}
                Assert.Equal(DkmEvaluationResultTypeModifierFlags.Constant, GetResultProperties(context, expr).ModifierFlags)
            Next

            For Each expr In {"Me", "F", "SRF", "p", "l"}
                Assert.Equal(DkmEvaluationResultTypeModifierFlags.None, GetResultProperties(context, expr).ModifierFlags)
            Next
        End Sub

        <Fact>
        Public Sub ModifierFlags_Volatile()
            Const ilSource = "
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .field public int32 F
  .field public int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) VF

  .method public hidebysig instance void 
          Test() cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } // end of method C::.ctor

} // end of class C
"
            Dim exeBytes As ImmutableArray(Of Byte) = Nothing
            Dim pdbBytes As ImmutableArray(Of Byte) = Nothing
            EmitILToArray(ilSource, appendDefaultHeader:=True, includePdb:=True, assemblyBytes:=exeBytes, pdbBytes:=pdbBytes)

            Dim runtime = CreateRuntimeInstance(
                assemblyName:=GetUniqueName(),
                references:=ImmutableArray.Create(MscorlibRef),
                exeBytes:=exeBytes.ToArray(),
                symReader:=SymReaderFactory.CreateReader(pdbBytes))
            Dim context = CreateMethodContext(runtime, methodName:="C.Test")

            Assert.Equal(DkmEvaluationResultTypeModifierFlags.None, GetResultProperties(context, "F").ModifierFlags)
            VerifyErrorResultProperties(context, "VF") ' VB doesn't support volatile
        End Sub

        <Fact>
        Public Sub Assignment()
            Const source = "
Class C
    Public Overridable Property P() As Integer

    Sub Test()
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, methodName:="C.Test")

            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData As New CompilationTestData()
            context.CompileAssignment(
                "P",
                "1",
                NoAliases,
                DebuggerDiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Null(errorMessage)
            Assert.Empty(missingAssemblyIdentities)

            Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect Or DkmClrCompilationResultFlags.ReadOnlyResult, resultProperties.Flags)
            Assert.Equal(Nothing, resultProperties.Category) ' Not Data
            Assert.Equal(Nothing, resultProperties.AccessType) ' Not Public
            Assert.Equal(Nothing, resultProperties.StorageType)
            Assert.Equal(Nothing, resultProperties.ModifierFlags) ' Not Virtual
        End Sub

        <Fact>
        Public Sub LocalDeclaration()
            Const source = "
Class C
    Sub Test()
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, methodName:="C.Test")

            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression(
                "z = 1", ' VB only supports implicit declarations
                DkmEvaluationFlags.None,
                NoAliases,
                DebuggerDiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Null(errorMessage)
            Assert.Empty(missingAssemblyIdentities)

            Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect Or DkmClrCompilationResultFlags.ReadOnlyResult, resultProperties.Flags)
            Assert.Equal(Nothing, resultProperties.Category) ' Not Data
            Assert.Equal(Nothing, resultProperties.AccessType)
            Assert.Equal(Nothing, resultProperties.StorageType)
            Assert.Equal(Nothing, resultProperties.ModifierFlags)
        End Sub

        <Fact>
        Public Sub [Error]()
            Const source = "
Class C
    Sub Test()
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, methodName:="C.Test")

            VerifyErrorResultProperties(context, "AddressOf Test")
            VerifyErrorResultProperties(context, "Missing")
            VerifyErrorResultProperties(context, "C")
        End Sub


        Private Shared Function GetResultProperties(context As EvaluationContextBase, expr As String) As ResultProperties
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            context.CompileExpression(expr, resultProperties, errorMessage)
            Assert.Null(errorMessage)
            Return resultProperties
        End Function

        Private Shared Sub VerifyErrorResultProperties(context As EvaluationContextBase, expr As String)
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            context.CompileExpression(expr, resultProperties, errorMessage)
            Assert.NotNull(errorMessage)
            Assert.Equal(Nothing, resultProperties)
        End Sub
    End Class
End Namespace