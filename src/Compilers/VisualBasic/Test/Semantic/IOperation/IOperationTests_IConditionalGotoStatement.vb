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
    IConditionalGotoStatement (Target Symbol: afterif, JumpIfTrue: False) (OperationKind.ConditionalGotoStatement) (Syntax: 'If p < 0 Th ... End If')
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'p < 0')
          Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
          Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If p < 0 Th ... End If')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'p = 0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p = 0')
            Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    ILabelStatement (Label: afterif) (OperationKind.LabelStatement) (Syntax: 'If p < 0 Th ... End If')
      LabeledStatement: null
  ILabelStatement (Label: exit) (OperationKind.LabelStatement) (Syntax: 'End Sub')
    LabeledStatement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
    ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodStatementSyntax)(source, expectedOperationTree, expectedDiagnostics, highLevelOperation:=False)
        End Sub
    End Class
End Namespace
