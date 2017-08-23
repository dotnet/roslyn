' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase
        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocation_Basic()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As Object, d As Object)
        c.M2(d)'BIND:"c.M2(d)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'c.M2(d)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'c.M2')
      Type Arguments(0)
      Instance Receiver: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'c')
  ApplicableSymbols(0)
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
        Public Sub DynamicInvocation_MultipleApplicableSymbols()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(d As Object)
        M2(d)'BIND:"M2(d)"
    End Sub

    Private Sub M2(c As Integer)
    End Sub

    Private Sub M2(c As Long)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'M2(d)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'M2')
      Type Arguments(0)
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'M2')
  ApplicableSymbols(2):
    Symbol: Sub C.M2(c As System.Int32)
    Symbol: Sub C.M2(c As System.Int64)
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
        Public Sub DynamicInvocation_MultipleArgumentsAndApplicableSymbols()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(d As Object, e As Object)
        M2(d, e)'BIND:"M2(d, e)"
    End Sub

    Private Sub M2(c As Integer, d As Object)
    End Sub

    Private Sub M2(c As Long, d As Object)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'M2(d, e)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'M2')
      Type Arguments(0)
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'M2')
  ApplicableSymbols(2):
    Symbol: Sub C.M2(c As System.Int32, d As System.Object)
    Symbol: Sub C.M2(c As System.Int64, d As System.Object)
  Arguments(2):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
      IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'e')
  ArgumentNames(0)
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocation_ArgumentNames()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(d As Object, e As Object)
        M2(d:=d, c:=e)'BIND:"M2(d:=d, c:=e)"
    End Sub

    Private Sub M2(c As Integer, d As Object)
    End Sub

    Private Sub M2(c As Long, d As Object)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'M2(d:=d, c:=e)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'M2')
      Type Arguments(0)
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'M2')
  ApplicableSymbols(2):
    Symbol: Sub C.M2(c As System.Int32, d As System.Object)
    Symbol: Sub C.M2(c As System.Int64, d As System.Object)
  Arguments(2):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
      IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'e')
  ArgumentNames(2):
    "d"
    "c"
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocation_ArgumentRefKinds()
            Dim source = <![CDATA[
Option Strict Off
Imports System.Runtime.InteropServices

Class C
    Private Sub M(d As Object, e As Object)
        M2(d, e)'BIND:"M2(d, e)"
    End Sub

    Private Sub M2(<Out> ByRef c As Integer, ByRef d As Object)
        c = 0
    End Sub

    Private Sub M2(<Out> ByRef c As Long, ByRef d As Object)
        c = 0
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'M2(d, e)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'M2')
      Type Arguments(0)
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'M2')
  ApplicableSymbols(2):
    Symbol: Sub C.M2(ByRef c As System.Int32, ByRef d As System.Object)
    Symbol: Sub C.M2(ByRef c As System.Int64, ByRef d As System.Object)
  Arguments(2):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
      IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'e')
  ArgumentNames(0)
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocation_OverloadResolutionFailure()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(d As Object)
        M2(d)'BIND:"M2(d)"
    End Sub

    Private Sub M2()
    End Sub

    Private Sub M2(c1 As Integer, c2 As Long)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidExpression (OperationKind.InvalidExpression, Type: System.Void, IsInvalid) (Syntax: 'M2(d)')
  Children(2):
      IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'M2')
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30516: Overload resolution failed because no accessible 'M2' accepts this number of arguments.
        M2(d)'BIND:"M2(d)"
        ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace

