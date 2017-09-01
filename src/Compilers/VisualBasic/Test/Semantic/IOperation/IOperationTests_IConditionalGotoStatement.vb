' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub IConditionalGotoStatement_FromIf()
            Dim source = <![CDATA[
Class C
    Sub Method(p As Integer)'BIND:"Sub Method(p As Integer)"
        If p < 0 Then
            p = 0
        End If
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'Sub Method( ... End Sub')
  IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'If p < 0 Th ... End If')
    IConditionalGotoStatement (JumpIfTrue: False, Target: afterif) (OperationKind.ConditionalGotoStatement) (Syntax: 'If p < 0 Th ... End If')
      Condition: IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'p < 0')
          Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If p < 0 Th ... End If')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'p = 0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p = 0')
            Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    ILabeledStatement (Label: afterif) (OperationKind.LabeledStatement) (Syntax: 'If p < 0 Th ... End If')
      Statement: null
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Sub')
    Statement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
    ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodStatementSyntax)(source, expectedOperationTree, expectedDiagnostics, useLoweredTree:=True)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub IConditionalGotoStatement_FromIfElse()
            Dim source = <![CDATA[
Class C
    Sub Method(p As Integer)'BIND:"Sub Method(p As Integer)"
        If p < 0 Then
            p = 0
        Else
            p = 1
        End If
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'Sub Method( ... End Sub')
  IBlockStatement (6 statements) (OperationKind.BlockStatement) (Syntax: 'If p < 0 Th ... End If')
    IConditionalGotoStatement (JumpIfTrue: False, Target: alternative) (OperationKind.ConditionalGotoStatement) (Syntax: 'If p < 0 Th ... End If')
      Condition: IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'p < 0')
          Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If p < 0 Th ... End If')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'p = 0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p = 0')
            Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    IBranchStatement (BranchKind.GoTo, Label: afterif) (OperationKind.BranchStatement) (Syntax: 'If p < 0 Th ... End If')
    ILabeledStatement (Label: alternative) (OperationKind.LabeledStatement) (Syntax: 'If p < 0 Th ... End If')
      Statement: null
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Else ... p = 1')
      IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Else ... p = 1')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'p = 1')
          Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p = 1')
              Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
              Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    ILabeledStatement (Label: afterif) (OperationKind.LabeledStatement) (Syntax: 'If p < 0 Th ... End If')
      Statement: null
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Sub')
    Statement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
    ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodStatementSyntax)(source, expectedOperationTree, expectedDiagnostics, useLoweredTree:=True)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub IConditionalGotoStatement_FromWhile()
            Dim source = <![CDATA[
Class C
    Sub Method(p As Integer)'BIND:"Sub Method(p As Integer)"
        Do While p > 0
            p = 0
        Loop
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'Sub Method( ... End Sub')
  IBlockStatement (6 statements) (OperationKind.BlockStatement) (Syntax: 'Do While p  ... Loop')
    IBranchStatement (BranchKind.GoTo, Label: continue) (OperationKind.BranchStatement) (Syntax: 'Do While p  ... Loop')
    ILabeledStatement (Label: start) (OperationKind.LabeledStatement) (Syntax: 'Do While p  ... Loop')
      Statement: null
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Do While p  ... Loop')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'p = 0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p = 0')
            Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    ILabeledStatement (Label: continue) (OperationKind.LabeledStatement) (Syntax: 'Do While p  ... Loop')
      Statement: null
    IConditionalGotoStatement (JumpIfTrue: True, Target: start) (OperationKind.ConditionalGotoStatement) (Syntax: 'Do While p  ... Loop')
      Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'p > 0')
          Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'Do While p  ... Loop')
      Statement: null
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Sub')
    Statement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
    ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodStatementSyntax)(source, expectedOperationTree, expectedDiagnostics, useLoweredTree:=True)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub IConditionalGotoStatement_FromUntil()
            Dim source = <![CDATA[
Class C
    Sub Method(p As Integer)'BIND:"Sub Method(p As Integer)"
        Do Until p = 0
            p = 0
        Loop
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'Sub Method( ... End Sub')
  IBlockStatement (6 statements) (OperationKind.BlockStatement) (Syntax: 'Do Until p  ... Loop')
    IBranchStatement (BranchKind.GoTo, Label: continue) (OperationKind.BranchStatement) (Syntax: 'Do Until p  ... Loop')
    ILabeledStatement (Label: start) (OperationKind.LabeledStatement) (Syntax: 'Do Until p  ... Loop')
      Statement: null
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Do Until p  ... Loop')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'p = 0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p = 0')
            Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    ILabeledStatement (Label: continue) (OperationKind.LabeledStatement) (Syntax: 'Do Until p  ... Loop')
      Statement: null
    IConditionalGotoStatement (JumpIfTrue: False, Target: start) (OperationKind.ConditionalGotoStatement) (Syntax: 'Do Until p  ... Loop')
      Condition: IBinaryOperatorExpression (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'p = 0')
          Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'Do Until p  ... Loop')
      Statement: null
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Sub')
    Statement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
    ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodStatementSyntax)(source, expectedOperationTree, expectedDiagnostics, useLoweredTree:=True)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub IConditionalGotoStatement_FromDoWhile()
            Dim source = <![CDATA[
Class C
    Sub Method(p As Integer)'BIND:"Sub Method(p As Integer)"
        Do
            p = 0
        Loop While p > 0
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'Sub Method( ... End Sub')
  IBlockStatement (5 statements) (OperationKind.BlockStatement) (Syntax: 'Do ... While p > 0')
    ILabeledStatement (Label: start) (OperationKind.LabeledStatement) (Syntax: 'Do')
      Statement: null
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Do ... While p > 0')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'p = 0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p = 0')
            Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    ILabeledStatement (Label: continue) (OperationKind.LabeledStatement) (Syntax: 'Do')
      Statement: null
    IConditionalGotoStatement (JumpIfTrue: True, Target: start) (OperationKind.ConditionalGotoStatement) (Syntax: 'Do')
      Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'p > 0')
          Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'Do')
      Statement: null
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Sub')
    Statement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
    ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodStatementSyntax)(source, expectedOperationTree, expectedDiagnostics, useLoweredTree:=True)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub IConditionalGotoStatement_FromDoUntil()
            Dim source = <![CDATA[
Class C
    Sub Method(p As Integer)'BIND:"Sub Method(p As Integer)"
        Do
            p = 0
        Loop Until p = 0
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'Sub Method( ... End Sub')
  IBlockStatement (5 statements) (OperationKind.BlockStatement) (Syntax: 'Do ... Until p = 0')
    ILabeledStatement (Label: start) (OperationKind.LabeledStatement) (Syntax: 'Do')
      Statement: null
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Do ... Until p = 0')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'p = 0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p = 0')
            Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    ILabeledStatement (Label: continue) (OperationKind.LabeledStatement) (Syntax: 'Do')
      Statement: null
    IConditionalGotoStatement (JumpIfTrue: False, Target: start) (OperationKind.ConditionalGotoStatement) (Syntax: 'Do')
      Condition: IBinaryOperatorExpression (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'p = 0')
          Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'Do')
      Statement: null
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Sub')
    Statement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
    ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodStatementSyntax)(source, expectedOperationTree, expectedDiagnostics, useLoweredTree:=True)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/21866")>
        Public Sub IConditionalGotoStatement_FromFor()
            Dim source = <![CDATA[
Class C
    Sub Method(p As Integer)'BIND:"Sub Method(p As Integer)"
        For i As Integer = 0 To p
            p = 0
        Next
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'Sub Method( ... End Sub')
  IBlockStatement (9 statements, 2 locals) (OperationKind.BlockStatement) (Syntax: 'For i As In ... Next')
    Locals: Local_1: <anonymous local> As System.Int32
      Local_2: i As System.Int32
    IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '0')
      Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '0')
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
          Right: ISequenceExpression (OperationKind.SequenceExpression) (Syntax: '0')
              SideEffects(1):
                  ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p')
                    Left: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'p')
                    Right: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
              Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    IBranchStatement (BranchKind.GoTo, Label: PostIncrement) (OperationKind.BranchStatement) (Syntax: 'For i As In ... Next')
    ILabeledStatement (Label: start) (OperationKind.LabeledStatement) (Syntax: 'For i As In ... Next')
      Statement: null
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For i As In ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'p = 0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p = 0')
            Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    ILabeledStatement (Label: continue) (OperationKind.LabeledStatement) (Syntax: 'For i As In ... Next')
      Statement: null
    IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For i As In ... Next')
      Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'For i As In ... Next')
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
          Right: IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'For i As In ... Next')
              Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
              Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i As In ... Next')
    ILabeledStatement (Label: PostIncrement) (OperationKind.LabeledStatement) (Syntax: 'For i As In ... Next')
      Statement: null
    IConditionalGotoStatement (JumpIfTrue: True, Target: start) (OperationKind.ConditionalGotoStatement) (Syntax: 'For i As In ... Next')
      Condition: IBinaryOperatorExpression (BinaryOperatorKind.LessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'p')
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
          Right: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'p')
    ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'For i As In ... Next')
      Statement: null
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Sub')
    Statement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
    ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodStatementSyntax)(source, expectedOperationTree, expectedDiagnostics, useLoweredTree:=True)
        End Sub
    End Class
End Namespace
