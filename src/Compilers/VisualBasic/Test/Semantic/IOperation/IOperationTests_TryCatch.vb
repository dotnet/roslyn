' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatchFinally_Basic()
            Dim source = <![CDATA[
Imports System

Class C
    Private Sub M(i As Integer)
        Try'BIND:"Try"
            i = 0
        Catch ex As Exception When i > 0
            Throw ex
        Finally
            i = 1
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITryStatement (OperationKind.None) (Syntax: 'Try'BIND:"T ... End Try')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Try'BIND:"T ... End Try')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = 0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = 0')
            Left: IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Catch clauses(1):
      ICatchClause (Exception type: , Exception local: ex As System.Exception) (OperationKind.None) (Syntax: 'Catch ex As ... Throw ex')
        Filter: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i > 0')
            Left: IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        Handler: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Catch ex As ... Throw ex')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Throw ex')
              Expression: IThrowExpression (OperationKind.ThrowExpression, Type: System.Exception) (Syntax: 'Throw ex')
                  ILocalReferenceExpression: ex (OperationKind.LocalReferenceExpression, Type: System.Exception) (Syntax: 'ex')
  Finally: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Finally ... i = 1')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = 1')
            Left: IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
