' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

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

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodStatementSyntax)(source, expectedOperationTree, expectedDiagnostics, highLevelOperation:=False)
        End Sub

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

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodStatementSyntax)(source, expectedOperationTree, expectedDiagnostics, highLevelOperation:=False)
        End Sub
    End Class
End Namespace
