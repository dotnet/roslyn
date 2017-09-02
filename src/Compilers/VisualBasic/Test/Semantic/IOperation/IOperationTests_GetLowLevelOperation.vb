' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub GetLowLevelOperation_FromMethod()
            Dim source = <![CDATA[
Class C
    Function Method(p As Integer) As Integer'BIND:"Function Method(p As Integer) As Integer"
        Return p
    End Function
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'Function Me ... nd Function')
  Locals: Local_1: Method As System.Int32
  IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Return p')
    IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Return p')
      Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'Return p')
          Left: ILocalReferenceExpression: Method (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'Return p')
          Right: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
    IBranchStatement (BranchKind.GoTo, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Return p')
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Function')
    Statement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Function')
    ReturnedValue: ILocalReferenceExpression: Method (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'End Function')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodStatementSyntax)(source, expectedOperationTree, expectedDiagnostics, useLoweredTree:=True)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub GetLowLevelOperation_FromConstructor()
            Dim source = <![CDATA[
Class C
    Private _field As Integer = 0

    Public Sub New(p As Integer)'BIND:"Public Sub New(p As Integer)"
        _field = p
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'Public Sub  ... End Sub')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '_field = p')
    Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '_field = p')
        Left: IFieldReferenceExpression: C._field As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: '_field')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: '_field')
        Right: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Sub')
    Statement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
    ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SubNewStatementSyntax)(source, expectedOperationTree, expectedDiagnostics, useLoweredTree:=True)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub GetLowLevelOperation_FromPropertyAccessorGet()
            Dim source = <![CDATA[
Class C
    Private _property As Integer = 0

    Private ReadOnly Property MyProperty() As Integer
        Get'BIND:"Get"
            Return _property
        End Get
    End Property
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'Get'BIND:"G ... End Get')
  Locals: Local_1: MyProperty As System.Int32
  IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Return _property')
    IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Return _property')
      Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'Return _property')
          Left: ILocalReferenceExpression: MyProperty (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'Return _property')
          Right: IFieldReferenceExpression: C._property As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: '_property')
              Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: '_property')
    IBranchStatement (BranchKind.GoTo, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Return _property')
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Get')
    Statement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Get')
    ReturnedValue: ILocalReferenceExpression: MyProperty (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'End Get')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AccessorStatementSyntax)(source, expectedOperationTree, expectedDiagnostics, useLoweredTree:=True)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub GetLowLevelOperation_FromPropertyAccessorSet()
            Dim source = <![CDATA[
Class C
    Private _property As Integer = 0

    Private Property MyProperty() As Integer
        Get
            Return _property
        End Get
        Set'BIND:"Set"
            _property = value
        End Set
    End Property
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'Set'BIND:"S ... End Set')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '_property = value')
    Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '_property = value')
        Left: IFieldReferenceExpression: C._property As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: '_property')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: '_property')
        Right: IParameterReferenceExpression: Value (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'value')
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Set')
    Statement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Set')
    ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AccessorStatementSyntax)(source, expectedOperationTree, expectedDiagnostics, useLoweredTree:=True)
        End Sub
    End Class
End Namespace
