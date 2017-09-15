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
IWithStatement (OperationKind.None) (Syntax: 'With c'BIND ... End With')
  Value: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'With c'BIND ... End With')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '.I = 0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '.I = 0')
            Left: IFieldReferenceExpression: C.I As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: '.I')
                Instance Receiver: IOperation:  (OperationKind.None) (Syntax: 'c')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '.J = 0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '.J = 0')
            Left: IFieldReferenceExpression: C.J As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: '.J')
                Instance Receiver: IOperation:  (OperationKind.None) (Syntax: 'c')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of WithBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
