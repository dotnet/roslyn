' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub WithStatement_Basic()
            Dim source = <![CDATA[
Class C
    Public I, J As Integer
End Class

Class D
    Private Sub M(c As C)
        With c'BIND:"With c"
            .I = 0
            .J = 0
        End With

    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWithOperation (OperationKind.None, Type: null) (Syntax: 'With c'BIND ... End With')
  Value: 
    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'With c'BIND ... End With')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '.I = 0')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '.I = 0')
            Left: 
              IFieldReferenceOperation: C.I As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: '.I')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'c')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '.J = 0')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '.J = 0')
            Left: 
              IFieldReferenceOperation: C.J As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: '.J')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'c')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of WithBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub WithStatement_Parent()
            Dim source = <![CDATA[
Class C
    Public I, J As Integer
End Class

Class D
    Private Sub M(c As C)'BIND:"Private Sub M(c As C)"
        With c
            .I = 0
            .J = 0
        End With

    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'Private Sub ... End Sub')
  IWithOperation (OperationKind.None, Type: null) (Syntax: 'With c ... End With')
    Value: 
      IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
    Body: 
      IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'With c ... End With')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '.I = 0')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '.I = 0')
              Left: 
                IFieldReferenceOperation: C.I As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: '.I')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'c')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '.J = 0')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '.J = 0')
              Left: 
                IFieldReferenceOperation: C.J As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: '.J')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'c')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IWithStatement_StaticFieldReferenceAsRValue_NoInstanceReceiver()
            Dim source = <![CDATA[
Imports System

Structure SSS
    Public Shared A As String

    Public Sub New(_a As String)
    End Sub
End Structure

Class Clazz
    Sub TEST(i As Integer)
        With New SSS(Me.ToString())
            Static s As Action = Sub()
                                     .A = ""'BIND:".A"
                                 End Sub
        End With
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldReferenceOperation: SSS.A As System.String (Static) (OperationKind.FieldReference, Type: System.String) (Syntax: '.A')
  Instance Receiver: 
    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                                     .A = ""'BIND:".A"
                                     ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
