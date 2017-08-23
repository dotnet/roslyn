' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase
        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicPropertyReference_MultipleApplicableSymbols()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As C, d As Object)
        Dim x = c(d)'BIND:"c(d)"
    End Sub

    Default ReadOnly Property P1(x As Integer) As Integer
        Get
            Return 1
        End Get
    End Property

    Default ReadOnly Property P1(x As String) As Integer
        Get
            Return 1
        End Get
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicPropertyReferenceExpression (OperationKind.DynamicPropertyReferenceExpression, Type: System.Object) (Syntax: 'c(d)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "P1", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'c')
      Type Arguments(0)
      Instance Receiver: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
  ApplicableSymbols(2):
    Symbol: ReadOnly Property C.P1(x As System.Int32) As System.Int32
    Symbol: ReadOnly Property C.P1(x As System.String) As System.Int32
  Arguments(1):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicPropertyReference_MultipleArgumentsAndApplicableSymbols()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As C, d As Object)
        Dim x = c(d, d)'BIND:"c(d, d)"
    End Sub

    Default ReadOnly Property P1(x As Integer, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property

    Default ReadOnly Property P1(x As String, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicPropertyReferenceExpression (OperationKind.DynamicPropertyReferenceExpression, Type: System.Object) (Syntax: 'c(d, d)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "P1", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'c')
      Type Arguments(0)
      Instance Receiver: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
  ApplicableSymbols(2):
    Symbol: ReadOnly Property C.P1(x As System.Int32, x2 As System.Object) As System.Int32
    Symbol: ReadOnly Property C.P1(x As System.String, x2 As System.Object) As System.Int32
  Arguments(2):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicPropertyReference_ArgumentNames()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As C, d As Object, e As Object)
        Dim x = c(x2:=e, x:=d)'BIND:"c(x2:=e, x:=d)"
    End Sub

    Default ReadOnly Property P1(x As Integer, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property

    Default ReadOnly Property P1(x As String, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicPropertyReferenceExpression (OperationKind.DynamicPropertyReferenceExpression, Type: System.Object) (Syntax: 'c(x2:=e, x:=d)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "P1", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'c')
      Type Arguments(0)
      Instance Receiver: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
  ApplicableSymbols(2):
    Symbol: ReadOnly Property C.P1(x As System.Int32, x2 As System.Object) As System.Int32
    Symbol: ReadOnly Property C.P1(x As System.String, x2 As System.Object) As System.Int32
  Arguments(2):
      IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'e')
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
  ArgumentNames(2):
    "x2"
    "x"
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicPropertyReference_ArgumentRefKinds()
            Dim source = <![CDATA[
Option Strict Off
Imports System.Runtime.InteropServices

Class C
    Private Sub M(c As C, d As Object, e As Object)
        Dim x = c(x2:=e, x:=d)'BIND:"c(x2:=e, x:=d)"
    End Sub

    Default ReadOnly Property P1(x As Integer, <Out> ByRef x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property

    Default ReadOnly Property P1(x As String, <Out> ByRef x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicPropertyReferenceExpression (OperationKind.DynamicPropertyReferenceExpression, Type: System.Object) (Syntax: 'c(x2:=e, x:=d)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "P1", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'c')
      Type Arguments(0)
      Instance Receiver: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
  ApplicableSymbols(2):
    Symbol: ReadOnly Property C.P1(x As System.Int32, x2 As System.Object) As System.Int32
    Symbol: ReadOnly Property C.P1(x As System.String, x2 As System.Object) As System.Int32
  Arguments(2):
      IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'e')
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
  ArgumentNames(2):
    "x2"
    "x"
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30651: property parameters cannot be declared 'ByRef'.
    Default ReadOnly Property P1(x As Integer, <Out> ByRef x2 As Object) As Integer
                                                     ~~~~~
BC30651: property parameters cannot be declared 'ByRef'.
    Default ReadOnly Property P1(x As String, <Out> ByRef x2 As Object) As Integer
                                                    ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicPropertyReference_OverloadResolutionFailure()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As C, d As Object)
        Dim x = c(c, d)'BIND:"c(c, d)"
    End Sub

    Default ReadOnly Property P1(x As Integer, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property

    Default ReadOnly Property P1(x As String, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidExpression (OperationKind.InvalidExpression, Type: System.Int32, IsInvalid) (Syntax: 'c(c, d)')
  Children(3):
      IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'c')
      IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30518: Overload resolution failed because no accessible 'P1' can be called with these arguments:
    'Public ReadOnly Default Property P1(x As Integer, x2 As Object) As Integer': Value of type 'C' cannot be converted to 'Integer'.
    'Public ReadOnly Default Property P1(x As String, x2 As Object) As Integer': Value of type 'C' cannot be converted to 'String'.
        Dim x = c(c, d)'BIND:"c(c, d)"
                ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace

