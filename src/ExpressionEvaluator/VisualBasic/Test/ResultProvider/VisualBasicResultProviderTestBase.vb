' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.VisualStudio.Debugger.ComponentInterfaces
Imports Microsoft.VisualStudio.Debugger.Evaluation

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

    Public MustInherit Class VisualBasicResultProviderTestBase : Inherits ResultProviderTestBase

        Public Sub New()
            MyClass.New(New VisualBasicFormatter())
        End Sub

        Private Sub New(formatter As VisualBasicFormatter)
            MyClass.New(New DkmInspectionSession(ImmutableArray.Create(Of IDkmClrFormatter)(formatter), ImmutableArray.Create(Of IDkmClrResultProvider)(New VisualBasicResultProvider(formatter, formatter))))
        End Sub

        Private Sub New(inspectionSession As DkmInspectionSession)
            MyBase.New(inspectionSession, CreateDkmInspectionContext(inspectionSession, DkmEvaluationFlags.None, radix:=10))
        End Sub

        Protected Shared Function GetAssembly(source As String) As Assembly
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib({source}, options:=TestOptions.ReleaseDll)
            Return ReflectionUtilities.Load(comp.EmitToArray())
        End Function

        Protected Shared Function GetAssemblyFromIL(ilSource As String) As Assembly
            Dim ilImage As ImmutableArray(Of Byte) = Nothing
            Dim comp = CompilationUtils.CreateCompilationWithCustomILSource(sources:=<compilation/>, ilSource:=ilSource, options:=TestOptions.ReleaseDll, ilImage:=ilImage)
            Return ReflectionUtilities.Load(ilImage)
        End Function

        Protected Shared Function PointerToString(pointer As IntPtr) As String
            If Environment.Is64BitProcess Then
                Return String.Format("&H{0:X16}", pointer.ToInt64())
            Else
                Return String.Format("&H{0:X8}", pointer.ToInt32())
            End If
        End Function
    End Class

End Namespace