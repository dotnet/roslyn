' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub IBlockStatement_SubMethodBlock()
            Dim source = <![CDATA[
Class Program
    Sub Method()'BIND:"Sub Method()"
        If 1 > 2 Then
        End If
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'Sub Method( ... End Sub')
  IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If 1 > 2 Th ... End If')
    Condition: 
      IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperator, Type: System.Boolean, Constant: False) (Syntax: '1 > 2')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
    WhenTrue: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If 1 > 2 Th ... End If')
    WhenFalse: 
      null
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
        <Fact>
        Public Sub IBlockStatement_SubNewBlock()
            Dim source = <![CDATA[
Class Program
    Sub New()'BIND:"Sub New()"
        If 1 > 2 Then
        End If
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'Sub New()'B ... End Sub')
  IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If 1 > 2 Th ... End If')
    Condition: 
      IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperator, Type: System.Boolean, Constant: False) (Syntax: '1 > 2')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
    WhenTrue: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If 1 > 2 Th ... End If')
    WhenFalse: 
      null
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ConstructorBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub IBlockStatement_FunctionMethodBlock()
            Dim source = <![CDATA[
Class Program
    Function Method() As Boolean'BIND:"Function Method() As Boolean"
        If 1 > 2 Then
        End If

        Return True
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (4 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: 'Function Me ... nd Function')
  Locals: Local_1: Method As System.Boolean
  IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If 1 > 2 Th ... End If')
    Condition: 
      IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperator, Type: System.Boolean, Constant: False) (Syntax: '1 > 2')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
    WhenTrue: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If 1 > 2 Th ... End If')
    WhenFalse: 
      null
  IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'Return True')
    ReturnedValue: 
      ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Function')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Function')
    ReturnedValue: 
      ILocalReferenceOperation: Method (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'End Function')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub IBlockStatement_PropertyGetBlock()
            Dim source = <![CDATA[
Class Program
    ReadOnly Property Prop As Integer
        Get'BIND:"Get"
            If 1 > 2 Then
            End If
        End Get
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: 'Get'BIND:"G ... End Get')
  Locals: Local_1: Prop As System.Int32
  IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If 1 > 2 Th ... End If')
    Condition: 
      IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperator, Type: System.Boolean, Constant: False) (Syntax: '1 > 2')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
    WhenTrue: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If 1 > 2 Th ... End If')
    WhenFalse: 
      null
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Get')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Get')
    ReturnedValue: 
      ILocalReferenceOperation: Prop (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'End Get')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42355: Property 'Prop' doesn't return a value on all code paths. Are you missing a 'Return' statement?
        End Get
        ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AccessorBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub IBlockStatement_PropertySetBlock()
            Dim source = <![CDATA[
Class Program
    WriteOnly Property Prop As Integer
        Set(Value As Integer)'BIND:"Set(Value As Integer)"
            If 1 > 2 Then
            End If
        End Set
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'Set(Value A ... End Set')
  IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If 1 > 2 Th ... End If')
    Condition: 
      IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperator, Type: System.Boolean, Constant: False) (Syntax: '1 > 2')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
    WhenTrue: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If 1 > 2 Th ... End If')
    WhenFalse: 
      null
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Set')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Set')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AccessorBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub IBlockStatement_CustomEventAddBlock()
            Dim source = <![CDATA[
Imports System

Class C
    Public Custom Event A As Action
        AddHandler(value As Action)'BIND:"AddHandler(value As Action)"
            If 1 > 2 Then
            End If
        End AddHandler

        RemoveHandler(value As Action)
        End RemoveHandler

        RaiseEvent()
        End RaiseEvent
    End Event
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'AddHandler( ...  AddHandler')
  IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If 1 > 2 Th ... End If')
    Condition: 
      IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperator, Type: System.Boolean, Constant: False) (Syntax: '1 > 2')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
    WhenTrue: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If 1 > 2 Th ... End If')
    WhenFalse: 
      null
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End AddHandler')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End AddHandler')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AccessorBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub IBlockStatement_CustomEventRemoveBlock()
            Dim source = <![CDATA[
Imports System

Class C
    Public Custom Event A As Action
        AddHandler(value As Action)
        End AddHandler

        RemoveHandler(value As Action)'BIND:"RemoveHandler(value As Action)"
            If 1 > 2 Then
            End If
        End RemoveHandler

        RaiseEvent()
        End RaiseEvent
    End Event
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'RemoveHandl ... moveHandler')
  IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If 1 > 2 Th ... End If')
    Condition: 
      IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperator, Type: System.Boolean, Constant: False) (Syntax: '1 > 2')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
    WhenTrue: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If 1 > 2 Th ... End If')
    WhenFalse: 
      null
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End RemoveHandler')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End RemoveHandler')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AccessorBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub IBlockStatement_CustomEventRaiseBlock()
            Dim source = <![CDATA[
Imports System

Class C
    Public Custom Event A As Action
        AddHandler(value As Action)
        End AddHandler

        RemoveHandler(value As Action)
        End RemoveHandler

        RaiseEvent()'BIND:"RaiseEvent()"
            If 1 > 2 Then
            End If
        End RaiseEvent
    End Event
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'RaiseEvent( ...  RaiseEvent')
  IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If 1 > 2 Th ... End If')
    Condition: 
      IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperator, Type: System.Boolean, Constant: False) (Syntax: '1 > 2')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
    WhenTrue: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If 1 > 2 Th ... End If')
    WhenFalse: 
      null
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End RaiseEvent')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End RaiseEvent')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AccessorBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub IBlockStatement_OperatorBlock()
            Dim source = <![CDATA[
Class Program
    Public Shared Operator +(p As Program, i As Integer) As Integer'BIND:"Public Shared Operator +(p As Program, i As Integer) As Integer"
        If 1 > 2 Then
        End If

        Return 0
    End Operator
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (4 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: 'Public Shar ... nd Operator')
  Locals: Local_1: <anonymous local> As System.Int32
  IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If 1 > 2 Th ... End If')
    Condition: 
      IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperator, Type: System.Boolean, Constant: False) (Syntax: '1 > 2')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
    WhenTrue: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If 1 > 2 Th ... End If')
    WhenFalse: 
      null
  IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'Return 0')
    ReturnedValue: 
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Operator')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Operator')
    ReturnedValue: 
      ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'End Operator')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of OperatorBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub IBlockStatement_MustOverrideSubMethodStatement()
            Dim source = "
MustInherit Class Program
    Public MustOverride Sub Method'BIND:""Public MustOverride Sub Method""
End Class"

            VerifyNoOperationTreeForTest(Of MethodStatementSyntax)(source)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub IBlockStatement_InterfaceSub()
            Dim source = "
Interface IProgram
    Sub Method'BIND:""Sub Method""
End Interface"

            VerifyNoOperationTreeForTest(Of MethodStatementSyntax)(source)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub IBlockStatement_InterfaceFunction()
            Dim source = "
Interface IProgram
    Function Method() As Boolean'BIND:""Function Method() As Boolean""
End Interface"

            VerifyNoOperationTreeForTest(Of MethodStatementSyntax)(source)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub IBlockStatement_NormalEvent()
            Dim source = "
Class Program
        Public Event A As System.Action'BIND:""Public Event A As System.Action""
End Class"

            VerifyNoOperationTreeForTest(Of EventStatementSyntax)(source)
        End Sub
    End Class
End Namespace
